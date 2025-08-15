using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.Services
{
    /// <summary>
    /// 공감/인정 API 호출 추상화.
    /// </summary>
    public interface IEmotionService
    {
        //사용자 입력을 보내 공감 응답을 받아온다(실패 시 폴백)
        Task<string> GetEmpathyAsync(string userText, CancellationToken ct = default);
    }
}
