using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// 추가
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;

namespace WouldYou_ShareMind.Services
{
    /// <summary>
    /// 외부 공감 API 호출 + 폴백.
    /// - HttpClient를 멤버로 재사용(소켓 고갈 방지)
    /// - 엔드포인트/키가 없거나 오류가 나면 폴백 문구 반환
    /// </summary>
    public sealed class EmotionService : IEmotionService, IDisposable
    {
        private readonly ISettingsService _settings;
        private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public EmotionService(ISettingsService settings) => _settings = settings;

        public async Task<string> GetEmpathyAsync(string userText, CancellationToken ct = default)
        {
            try
            {
                var endpoint = _settings.Settings.EmpathyEndpoint;
                var apiKey = _settings.Settings.ApiKey;

                if (!string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(apiKey))
                {
                    // ⚠️ 실제 API 스키마에 맞게 수정하세요.
                    var payload = new { text = userText, apiKey };
                    using var resp = await _http.PostAsJsonAsync(endpoint!, payload, ct);
                    resp.EnsureSuccessStatusCode();

                    var dto = await resp.Content.ReadFromJsonAsync<EmpathyDto>(cancellationToken: ct);
                    if (!string.IsNullOrWhiteSpace(dto?.reply))
                        return dto!.reply!;
                }

                // 폴백 문구(네트워크/키 없음/스키마 불일치 등)
                return "당신의 감정을 안전하게 들었어요. 지금 이 순간을 함께 버텨볼게요.";
            }
            catch
            {
                return "지금은 연결이 원활하지 않지만, 당신의 이야기는 충분히 소중해요.";
            }
        }

        public void Dispose() => _http.Dispose();

        // 응답 스키마 예시(실서비스에 맞춰 수정)
        private sealed record EmpathyDto(string reply);
    }
}
