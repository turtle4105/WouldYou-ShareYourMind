using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WouldYou_ShareMind.ViewModels
{
    public partial class SleepModeViewModel : ObservableObject
    {
        [ObservableProperty] private bool isPaused;   // true = 일시정지 상태
        [ObservableProperty] private bool isPlaying = true;

        public SleepModeViewModel()
        {
            // 진입 시 재생 시작(오디오/노이즈 시작 지점에서 훅 연결)
            // StartPlayback();
        }

        [RelayCommand]
        private void TogglePause()
        {
            if (IsPaused)
            {
                // ResumePlayback();
                IsPaused = false;
                IsPlaying = true;
            }
            else
            {
                // PausePlayback();
                IsPaused = true;
                IsPlaying = false;
            }
        }

        [RelayCommand]
        private void Stop()
        {
            // StopPlayback();  // 오디오 정지 및 자원 해제
            IsPaused = false;
            IsPlaying = false;
            System.Windows.MessageBox.Show("수면 유도 모드를 종료했어요.", "종료");
        }
    }
}
