using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.Models
{
    /// <summary>
    /// 앱 설정 데이터 모델 (순수 DTO)
    /// </summary>
    public sealed class AppSettings
    {
        public string? ApiKey { get; set; }
        public string? EmpathyEndpoint { get; set; }
        public string? SleepAudioPath { get; set; }
        public int? MicDeviceNumber { get; set; }
    }
}
