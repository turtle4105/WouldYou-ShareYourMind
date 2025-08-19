using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Threading.Tasks;
using WouldYou_ShareMind.Services;

namespace WouldYou_ShareMind.ViewModels
{
    public partial class SleepModeViewModel : ObservableObject, IDisposable
    {
        private readonly IAudioService _audio;
        private readonly IDbService _db;

        private long _sleepLogId;

        public SleepModeViewModel(IAudioService audio, IDbService db)
        {
            _audio = audio;
            _db = db;

            // 버튼이 없으므로 생성자에서 바로 시작하지 말고
            // View Loaded에서 StartAsync 호출(중복 방지/생명주기 안정성↑)
        }

        public async Task StartAsync()
        {
            // 이미 실행 중이면 무시
            if (_sleepLogId != 0) return;

            // DB: 수면 세션 시작(duration은 임시로 20; 필요 시 설정값 바인딩)
            _sleepLogId = await _db.InsertSleepLogStartAsync(durationMin: 20);

            // SleepModeViewModel.cs - StartAsync() 내부
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var file = System.IO.Path.Combine(baseDir, "Resources", "audio", "interstellar_plasma.mp3");

            if (_audio is AudioService a)
            {
                a.Play(file, startVolume: 0f);   // 0으로 시작
                a.FadeTo(0.6f, durationMs: 1800); // 1.8초 페이드‑인
            }
            else
            {
                _audio.Play(file); // fallback
            }

            // 마이크 캡처 시작
            _audio.StartMicCapture();

        }

        public async Task StopAsync()
        {
            // 마이크/오디오 정리
            _audio.StopMicCapture();
            _audio.Stop();   // 페이드 없이 즉시 정지


            // DB: 세션 종료시간/실사용시간 업데이트
            if (_sleepLogId != 0)
            {
                await _db.UpdateSleepLogEndAsync(_sleepLogId);
                _sleepLogId = 0;
            }
        }

        public void Dispose() => _ = StopAsync();
    }
}
