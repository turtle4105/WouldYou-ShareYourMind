using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Threading.Tasks;
using System.Windows;
using WouldYou_ShareMind.Services;
using NAudio.Wave;
using System.Diagnostics;

namespace WouldYou_ShareMind.ViewModels
{
    public partial class SleepModeViewModel : ObservableObject, IDisposable
    {
        // === 데모 모드 플래그/타이머 ===
        private bool _demoMode = true;                     // ← 데모만 보여줄 땐 true
        private System.Timers.Timer? _demoTimer;
        private int _demoSec = 0;                          // 경과 시간(초)


        private readonly IAudioService _audio;
        private readonly IDbService _db;

        private long _sleepLogId;
        private readonly BreathingDetector _detector = new(maLen: 20);
        private DateTime _lastFeatureAtUtc = DateTime.MinValue;
        private bool _autoStopping;

        // ★ UI 바인딩
        [ObservableProperty] private string statusText = "대기 중";
        [ObservableProperty] private double lastBpm;
        [ObservableProperty] private double lastCv;

        // ★ 캘리브레이션 & 게이팅 파라미터
        private bool _calibrating = true;
        private int _calibCount = 0;
        private double _noiseFloor = 0.0;
        private const int CalibSamples = 120;   // 12s @10Hz
        private const double SnrThresh = 0.002; // 0.004~0.010 사이에서 튜닝
        private DateTime _lastDiagAt = DateTime.MinValue;   // ★ 1초 간격 진단 로그 타이밍용


        public SleepModeViewModel(IAudioService audio, IDbService db)
        {
            _audio = audio;
            _db = db;
            _audio.RmsUpdated += OnRms;
        }

        private void StartBackgroundAudio()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var file = System.IO.Path.Combine(baseDir, "Resources", "audio", "interstellar_plasma.mp3");

            if (_audio is AudioService a)
            {
                a.Play(file, startVolume: 0f);
                a.FadeTo(0.6f, durationMs: 1800);
            }
            else
            {
                _audio.Play(file);
            }
        }


        private void StartDemoLoop()
        {
            _demoTimer?.Stop(); _demoTimer?.Dispose();
            _demoSec = 0;

            _demoTimer = new System.Timers.Timer(1000); // 1초
            _demoTimer.AutoReset = true;
            _demoTimer.Elapsed += async (s, e) =>
            {
                _demoSec++;

                // 1) 값 생성: 11.5±1.5 bpm, CV 0.22±0.05 정도로 천천히 출렁이게
                //    (보기 좋게 느리게 변하는 사인파)
                double bpm = 11.5 + 1.5 * Math.Sin(2 * Math.PI * (_demoSec % 20) / 20.0);
                double cv = 0.22 + 0.05 * Math.Sin(2 * Math.PI * (_demoSec % 30) / 30.0 + 1.3);

                // 2) UI 반영
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    LastBpm = Math.Round(bpm, 1);
                    LastCv = Math.Round(cv, 2);

                    // 상태 텍스트도 단계적으로 변경 (연출)
                    if (_demoSec < 15) StatusText = "깊게 숨을 들이쉬어 보세요…";
                    else if (_demoSec < 120) StatusText = "잠에 가까워지고 있어요…";
                    else StatusText = "꿈나라에 도착했습니다";
                });

                // 3) 10초마다 DB 로깅(기존 시그니처 사용)
                if (_sleepLogId != 0 && _demoSec % 10 == 0)
                {
                    string badge = (_demoSec < 15) ? "Awake" : (_demoSec < 35) ? "Drowsy" : "Asleep";
                    double? stability = Math.Clamp(1.0 - cv, 0.0, 1.0);

                    await _db.InsertBreathingLogAsync(
                        durationSec: 10,
                        sampleRate: 10,
                        brBpm: bpm,
                        stability: stability,
                        badge: badge
                    );
                }

                // 4) 50초쯤에 자동 종료 연출
                if (_demoSec == 50)
                {
                    _demoTimer!.Stop();
                    _ = Application.Current?.Dispatcher.Invoke(async () =>
                    {
                        _autoStopping = true;
                        if (_audio is AudioService a) a.StopWithFade(1200);
                        await StopAsync();
                    });
                }
            };
            _demoTimer.Start();

            // ★ 바로 첫 화면에도 값 세팅 (0.0이 안 보이게)
            Application.Current?.Dispatcher.Invoke(() =>
            {
                LastBpm = 11.5;
                LastCv = 0.22;
                StatusText = "깊게 숨을 들이쉬어 보세요…";
            });
        }

        public async Task StartAsync()
        {
            StartBackgroundAudio();

            // 데모 모드면: UI/데모타이머만 시작하고 바로 반환
            if (_demoMode)
            {
                StartDemoLoop();
                StatusText = "깊게 숨을 들이쉬어 보세요…";
                return;                       // ★ 마이크 캡처/캘리브레이션 시작 안 함
            }

            // 1) 입력 장치 나열
            Console.WriteLine($"[Mic] DeviceCount = {WaveInEvent.DeviceCount}");
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var cap = WaveInEvent.GetCapabilities(i);
                Console.WriteLine($"[Mic]{i}: {cap.ProductName}");
            }

            // 2) 원하는 장치 고르기 (간단 키워드)
            int deviceIndex = 0; // 기본값
            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var name = WaveInEvent.GetCapabilities(i).ProductName?.ToLowerInvariant() ?? "";
                if (name.Contains("headset") || name.Contains("usb") || name.Contains("ear") || name.Contains("mic"))
                {
                    deviceIndex = i;
                    break;
                }
            }
            Console.WriteLine($"[Mic] 선택된 입력 장치 index={deviceIndex}");

            // 3) 선택된 장치로 캡처 시작
            _audio.StartMicCapture(deviceNumber: deviceIndex);

            // ★ 캘리브레이션 초기화
            _detector.Reset();
            _lastFeatureAtUtc = DateTime.UtcNow;
            _calibrating = true; _calibCount = 0; _noiseFloor = 0.0;
            StatusText = "환경 캘리브레이션 중…";
        }

        public async Task StopAsync()
        {
            _audio.StopMicCapture();

            if (_audio is AudioService a) a.StopWithFade(1200);
            else _audio.Stop();

            if (_sleepLogId != 0)
            {
                await _db.UpdateSleepLogEndAsync(_sleepLogId);
                _sleepLogId = 0;
            }

            StatusText = _autoStopping ? "잠들어서 자동 종료" : "사용자 종료";
            _autoStopping = false;
        }

        private async void OnRms(double rms)
        {
            if (_demoMode) return;

            try
            {
                // ── 1) 캘리브레이션 ────────────────────────────────
                if (_calibrating)
                {
                    _noiseFloor += rms; _calibCount++;
                    if (_calibCount >= CalibSamples)
                    {
                        _noiseFloor /= _calibCount;
                        _calibrating = false;
                        Application.Current?.Dispatcher.Invoke(() =>
                            StatusText = $"잠 유도 중… (baseline={_noiseFloor:F4})");
                    }
                    else
                    {
                        // ★ 매 1초 상태 안내
                        if ((DateTime.UtcNow - _lastDiagAt).TotalSeconds >= 1)
                        {
                            _lastDiagAt = DateTime.UtcNow;
                            Application.Current?.Dispatcher.Invoke(() =>
                                StatusText = $"환경 캘리브레이션 중… {(CalibSamples - _calibCount) / 10.0:F1}s");
                        }
                    }
                    return;
                }

                // ── 2) SNR 게이팅 ─────────────────────────────────
                double snr = rms - _noiseFloor;

                // ★ 1초마다 핵심 수치 콘솔 출력
                if ((DateTime.UtcNow - _lastDiagAt).TotalSeconds >= 1)
                {
                    _lastDiagAt = DateTime.UtcNow;
                    Console.WriteLine($"[BREATH] rms={rms:F4} floor={_noiseFloor:F4} snr={snr:F4}");
                }

                if (snr < SnrThresh)
                {
                    // ★ 사용자에게 이유 표시
                    Application.Current?.Dispatcher.Invoke(() =>
                        StatusText = "신호가 너무 약해요(마이크를 가까이).");
                    return;
                }

                // ── 3) 특징 추정 ───────────────────────────────────
                var (ready, state) = _detector.Push(rms);

                // 물리 범위 필터: 6~24 bpm
                double? bpmValue = _detector.LastBpm;
                if (bpmValue is < 6 or > 24) bpmValue = null;

                double? cvValue = bpmValue.HasValue ? _detector.LastCv : null;

                // ★ 진단: 감지 실패 이유 안내
                if (ready && !bpmValue.HasValue)
                {
                    // 진폭·피크 부족 가능성
                    Application.Current?.Dispatcher.Invoke(() =>
                        StatusText = "신호는 감지되지만 호흡 패턴이 불안정해요(조금 더 규칙적으로 숨 쉬어보세요).");
                }

                // UI 갱신
                if (bpmValue.HasValue && cvValue.HasValue)
                {
                    var bpm = Math.Round(bpmValue.Value, 1);
                    var cv = Math.Round(cvValue.Value, 2);
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        LastBpm = bpm;
                        LastCv = cv;
                    });
                }

                if (!ready) return;

                // ── 4) 10초 로깅 ───────────────────────────────────
                var now = DateTime.UtcNow;
                if ((now - _lastFeatureAtUtc).TotalSeconds >= 10 && _sleepLogId != 0)
                {
                    _lastFeatureAtUtc = now;

                    double? stability = (cvValue.HasValue) ? Math.Clamp(1.0 - cvValue.Value, 0.0, 1.0) : (double?)null;

                    string badge = state switch
                    {
                        SleepState.Awake => "Awake",
                        SleepState.Drowsy => "Drowsy",
                        SleepState.Asleep => "Asleep",
                        _ => null
                    };

                    await _db.InsertBreathingLogAsync(
                        durationSec: 10,
                        sampleRate: 10,
                        brBpm: bpmValue,
                        stability: stability,
                        badge: badge
                    );
                }

                // 상태 텍스트
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    StatusText = state switch
                    {
                        SleepState.Awake => "깊게, 규칙적으로 숨을 들이쉬어 보세요…",
                        SleepState.Drowsy => "좋아요. 호흡이 안정되고 있어요…",
                        SleepState.Asleep => "꿈나라에 도착했습니다",
                        _ => StatusText
                    };
                });

                // 수면 감지 시 자동 종료(동일)
                if (state == SleepState.Asleep && !_autoStopping)
                {
                    _autoStopping = true;
                    if (_audio is AudioService a) a.StopWithFade(1500);
                    await StopAsync();
                }
            }
            catch { /* 로깅 생략 */ }
        }


        public void Dispose()
        {
            _audio.RmsUpdated -= OnRms;
            _ = StopAsync();
        }
    }
}
