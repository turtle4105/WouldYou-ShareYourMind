using System;
using System.Collections.Generic;
using System.Linq;

namespace WouldYou_ShareMind.Services
{
    public enum SleepState { Awake, Drowsy, Asleep }

    public sealed class BreathingDetector
    {
        private readonly int _fs = 10;        // 10 samples/sec (100ms)
        private readonly int _winSec = 15;    // 분석창 15초
        private readonly int _maLen;          // 이동평균 길이(15~25 권장)
        private readonly Queue<double> _buf = new();     // 저역통과 후 winSec 창
        private readonly Queue<double> _maBuf = new();   // 이동평균용
        private readonly Queue<double> _ampHist = new(); // 5분 진폭 히스토리

        private const int FiveMinSamples = 5 * 60 * 10;  // 5분 @10Hz

        // ★ 튜닝 상수
        private const int Refractory = 10;        // 1.0s @10Hz (가짜 연속 피크 억제, 기존 8)
        private const double MinProm = 0.0025;   // 피크 유의도(이웃 샘플 대비 최소 높이차)
        private const double MinAmp = 0.008;    // winSec 창 절대 진폭 바닥(너무 조용하면 무효)

        // ★ 필드 추가
        private double _noiseFloor = 0.01;   // 초기 바닥
        private const double FloorDecay = 0.990;  // 느리게 감소
        private const double FloorRise = 0.9990; // 매우 느리게 상승
        private const double MinFloor = 0.0025;  // 절대 최소치

        private double _emaSnr = 0.0;
        private const double EmaAlpha = 0.3; // 0.2~0.4 사이 튜닝


        public SleepState State { get; private set; } = SleepState.Awake;

        // 최근 특징값(바인딩/로그 편의)
        public double? LastBpm { get; private set; }
        public double? LastCv { get; private set; }
        public double? LastAmp { get; private set; }

        // 전이 유지 시간(초)
        private double _drowsyHoldSec = 0;
        private double _asleepHoldSec = 0;

        public BreathingDetector(int maLen = 20) { _maLen = Math.Max(3, maLen); }

        public (bool ready, SleepState state) Push(double rms)
        {
            // --- 0) 바닥 노이즈 추적 + SNR/EMA 계산 (가장 먼저!) ---
            if (rms > _noiseFloor)
                _noiseFloor = _noiseFloor * FloorRise + rms * (1 - FloorRise);
            else
                _noiseFloor = _noiseFloor * FloorDecay + rms * (1 - FloorDecay);

            if (_noiseFloor < MinFloor) _noiseFloor = MinFloor;

            double snr = rms - _noiseFloor;
            if (snr < 0) snr = 0;

            // 지수평활 SNR
            _emaSnr = EmaAlpha * snr + (1 - EmaAlpha) * _emaSnr;

            // --- 1) 이동평균(저역통과) : rms 대신 emaSnr 사용 ---
            _maBuf.Enqueue(_emaSnr);
            if (_maBuf.Count > _maLen) _maBuf.Dequeue();
            double ma = _maBuf.Average();

            // --- 2) winSec 버퍼 ---
            _buf.Enqueue(ma);
            if (_buf.Count > _winSec * _fs) _buf.Dequeue();

            if (_buf.Count < _winSec * _fs)
            {
                LastBpm = LastCv = LastAmp = null;
                return (false, State);
            }

            var arr = _buf.ToArray();

            // --- 3) 피크 검출 + 유의도(prominence) ---
            var cand = new List<int>();
            for (int i = 1; i < arr.Length - 1; i++)
            {
                if (arr[i] > arr[i - 1] && arr[i] > arr[i + 1])
                {
                    double prom = arr[i] - Math.Max(arr[i - 1], arr[i + 1]); // using System;
                    if (prom >= MinProm) cand.Add(i);
                }
            }

            // 3-1) 불응기 적용
            var peaks = new List<int>();
            int last = -9999;
            foreach (var p in cand)
            {
                if (p - last >= Refractory) { peaks.Add(p); last = p; }
            }

            // --- 4) IBI → BPM, CV ---
            double? bpm = null, cv = null;
            if (peaks.Count >= 3)
            {
                var ibis = new List<double>();
                for (int i = 1; i < peaks.Count; i++)
                    ibis.Add((peaks[i] - peaks[i - 1]) / (double)_fs);

                double meanIbi = ibis.Average();
                if (meanIbi > 1e-6)
                {
                    double sdIbi = Math.Sqrt(ibis.Sum(x => (x - meanIbi) * (x - meanIbi)) / ibis.Count);
                    bpm = 60.0 / meanIbi;
                    cv = sdIbi / meanIbi;
                }
            }

            // --- 5) winSec 진폭 및 5분 베이스라인 대비 ---
            double amp = arr.Max() - arr.Min();
            LastAmp = amp;

            _ampHist.Enqueue(amp);
            if (_ampHist.Count > FiveMinSamples) _ampHist.Dequeue();
            double baseline = _ampHist.Count > 0 ? _ampHist.Average() : amp;
            bool ampLow = amp <= baseline * 0.4;

            // 5-1) 절대 진폭 바닥 → 무효
            if (amp < MinAmp)
            {
                LastBpm = LastCv = null;
                return (true, State);
            }

            // --- 6) 물리 범위 밖 BPM 무효 ---
            if (bpm is < 6 or > 24) bpm = null;

            // --- 7) 임계 조건(전이) ---
            bool bpmOk = bpm is > 9 and < 14;
            bool cvOk = cv is < 0.25;
            bool cvWeak = cv is < 0.35;

            // --- 8) 상태 전이 ---
            switch (State)
            {
                case SleepState.Awake:
                    if (bpmOk && cvWeak)
                    {
                        _drowsyHoldSec += 1.0 / _fs;
                        if (_drowsyHoldSec >= 20)
                        {
                            State = SleepState.Drowsy;
                            _asleepHoldSec = 0;
                        }
                    }
                    else _drowsyHoldSec = 0;
                    break;

                case SleepState.Drowsy:
                    if (bpmOk && cvOk && ampLow)
                    {
                        _asleepHoldSec += 1.0 / _fs;
                        if (_asleepHoldSec >= 30)
                            State = SleepState.Asleep;
                    }
                    else
                    {
                        _asleepHoldSec = Math.Max(0, _asleepHoldSec - (2.0 / _fs));
                    }
                    break;

                case SleepState.Asleep:
                    // 깨움 판정은 보수적으로(옵션)
                    break;
            }

            // --- 9) 최종 특징값 노출 ---
            LastBpm = bpm;
            LastCv = bpm.HasValue ? cv : null;

            return (true, State);
        }

        public void Reset()
        {
            _buf.Clear(); _maBuf.Clear(); _ampHist.Clear();
            _drowsyHoldSec = _asleepHoldSec = 0;
            LastBpm = LastCv = LastAmp = null;
            State = SleepState.Awake;
        }
    }
}
