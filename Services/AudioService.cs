using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// 추가
using NAudio.Wave;

namespace WouldYou_ShareMind.Services
{
    /// <summary>
    /// - NAudio 기반 재생/캡처
    /// - 재생: AudioFileReader + WaveOutEvent
    /// - 캡처: WaveInEvent(16kHz mono) → RMS 계산 이벤트 발행
    /// </summary>
    public sealed class AudioService : IAudioService
    {
        private WaveOutEvent? _output;
        private AudioFileReader? _reader;

        private WaveInEvent? _waveIn;
        public event Action<double>? RmsUpdated;

        // ===== 수면 오디오 재생 =====
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
                DeviceNumber = deviceNumber,                // 설정에서 선택 가능
                WaveFormat = new WaveFormat(16000, 16, 1),  // 16kHz, mono, 16-bit PCM
                BufferMilliseconds = 100
            };
            _waveIn.DataAvailable += OnMicData;
            _waveIn.StartRecording();
        }

        private void OnMicData(object? sender, WaveInEventArgs e)
        {
            // 간단 RMS(루트-평균-제곱) 계산 → 0~1 근사 값
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
            Stop();
            StopMicCapture();
        }
    }
}
