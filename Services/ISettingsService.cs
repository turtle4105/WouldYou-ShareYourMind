using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WouldYou_ShareMind.Models;

namespace WouldYou_ShareMind.Services
{
    /// <summary>
    /// 앱 전역 설정(JSON).
    /// </summary>
    public interface ISettingsService
    {
        AppSettings Settings { get; }

        /// <summary>디스크에서 로드(없으면 무시)</summary>
        void Load();

        /// <summary>디스크에 저장</summary>
        Task SaveAsync();
    }
}
