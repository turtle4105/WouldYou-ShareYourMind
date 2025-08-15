using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.Services
{
    /// <summary>
    /// 수면 음악 재생 + 마이크 캡처(라이트: RMS 이벤트).
    /// </summary>
    public interface IAudioService : IDisposable
    {
        // 수면 오디오
        void Play(string filePath);
        void Pause();
        void Stop();

        // 마이크 캡처(간단 RMS 산출)
        void StartMicCapture(int deviceNumber = 0);
        void StopMicCapture();

        // 마이크 RMS 업데이트(0~1 근사). UI에서 게이지 등으로 사용
        event Action<double>? RmsUpdated;
    }
}
