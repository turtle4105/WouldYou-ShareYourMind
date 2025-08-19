using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// 추가
using NAudio.Wave;
using System.Timers; // System.Timers.Timer 사용

namespace WouldYou_ShareMind.Services
{
    public sealed class AudioService : IAudioService
    {
        private WaveOutEvent? _output;
        private AudioFileReader? _reader;
        private System.Timers.Timer? _fadeTimer;   // ← Timer 풀네임

        private WaveInEvent? _waveIn;
        public event Action<double>? RmsUpdated;

        // --- 유틸: Clamp (Math.Clamp 미지원 환경 대비) ---
        private static float Clamp01(float v)
        {
            if (v < 0f) return 0f;
            if (v > 1f) return 1f;
            return v;
        }

        // 현재 재생 볼륨(0~1)
        public float Volume
        {
            get => _reader?.Volume ?? 1f;
            set
            {
                if (_reader != null)
                    _reader.Volume = Clamp01(value); // Math.Clamp 대신
            }
        }

        // 시작 볼륨을 지정해서 재생(무음 → 페이드인 용)
        public void Play(string filePath, float startVolume)
        {
            Stop(); // 중복 재생 방지

            _reader = new AudioFileReader(filePath);
            _reader.Volume = Clamp01(startVolume);

            _output = new WaveOutEvent();
            _output.Init(_reader);
            _output.Play();
        }

        // 목표 볼륨까지 지정 시간(ms) 동안 선형 페이드
        public void FadeTo(float targetVolume, int durationMs = 1500)
        {
            if (_reader == null) return;

            if (_fadeTimer != null) { _fadeTimer.Stop(); _fadeTimer.Dispose(); _fadeTimer = null; }

            float start = _reader.Volume;
            float target = Clamp01(targetVolume);
            if (durationMs <= 0)
            {
                _reader.Volume = target;
                return;
            }

            int interval = 50; // ms
            int steps = Math.Max(1, durationMs / interval);
            int tick = 0;
            float delta = (target - start) / steps;

            var t = new System.Timers.Timer(interval);
            t.AutoReset = true;
            t.Elapsed += (s, e) =>
            {
                // null-forgiving(!) 대신 널 체크로 안전하게
                if (_reader == null)
                {
                    t.Stop();
                    return;
                }

                tick++;
                _reader.Volume = Clamp01(start + delta * tick);
                if (tick >= steps)
                {
                    t.Stop();
                }
            };
            _fadeTimer = t;
            _fadeTimer.Start();
        }

        // 부드럽게 줄이며 정지
        public void StopWithFade(int durationMs = 1200)
        {
            if (_reader == null || _output == null)
            {
                Stop();
                return;
            }

            if (_fadeTimer != null) { _fadeTimer.Stop(); _fadeTimer.Dispose(); _fadeTimer = null; }

            int interval = 50;
            int steps = Math.Max(1, durationMs / interval);
            int tick = 0;
            float start = _reader.Volume;
            float delta = start / steps;

            var t = new System.Timers.Timer(interval);
            t.AutoReset = true;
            t.Elapsed += (s, e) =>
            {
                if (_reader == null)
                {
                    t.Stop();
                    return;
                }

                tick++;
                float next = start - delta * tick;
                _reader.Volume = (next <= 0f) ? 0f : next;

                if (tick >= steps || _reader.Volume <= 0.01f)
                {
                    t.Stop();
                    Stop(); // 실제 정지/해제
                }
            };
            _fadeTimer = t;
            _fadeTimer.Start();
        }

        // ===== 기존 재생 API =====
        public void Play(string filePath)
        {
            Stop(); // 중복 재생 방지

            _reader = new AudioFileReader(filePath);
            _output = new WaveOutEvent();
            _output.Init(_reader);
            _output.Play();
        }

        public void Pause() => _output?.Pause();

        public void Stop()
        {
            _output?.Stop();
            _output?.Dispose();
            _output = null;

            _reader?.Dispose();
            _reader = null;
        }

        // ===== 마이크 캡처(라이트) =====
        public void StartMicCapture(int deviceNumber = 0)
        {
            StopMicCapture();

            _waveIn = new WaveInEvent
            {
                DeviceNumber = deviceNumber,
                WaveFormat = new WaveFormat(16000, 16, 1),  // 16kHz, mono, 16-bit PCM
                BufferMilliseconds = 100
            };
            _waveIn.DataAvailable += OnMicData;
            _waveIn.StartRecording();
        }

        private void OnMicData(object? sender, WaveInEventArgs e)
        {
            int samples = e.BytesRecorded / 2; // 16-bit
            if (samples <= 0) return;

            double sumSq = 0;
            for (int i = 0; i < samples; i++)
            {
                short s = BitConverter.ToInt16(e.Buffer, i * 2);
                double v = s / 32768.0;  // -1.0 ~ 1.0
                sumSq += v * v;
            }
            double rms = Math.Sqrt(sumSq / samples);
            RmsUpdated?.Invoke(rms);
        }

        public void StopMicCapture()
        {
            if (_waveIn is null) return;

            _waveIn.DataAvailable -= OnMicData;
            _waveIn.StopRecording();
            _waveIn.Dispose();
            _waveIn = null;
        }

        public void Dispose()
        {
            if (_fadeTimer != null) { _fadeTimer.Stop(); _fadeTimer.Dispose(); _fadeTimer = null; }
            Stop();
            StopMicCapture();
        }
    }
}
