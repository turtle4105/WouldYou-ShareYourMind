// Services/EmotionService.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.Services
{
    public sealed class EmotionService : IEmotionService
    {
        private readonly HttpClient _http;
        private readonly string? _apiKey;
        private static readonly Uri Endpoint = new("https://api.openai.com/v1/chat/completions");
        private const string Model = "gpt-4o-mini";

        public EmotionService(HttpClient? http = null)
        {
            _http = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        }

        public async Task<string> AnalyzeAsync(string content, bool isPublic, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(content))
                return "지금 마음을 천천히 정리해도 괜찮아요. 한 줄이라도 써 내려가 볼까요?";

            // 키 없으면 폴백
            if (string.IsNullOrWhiteSpace(_apiKey))
                return FallbackReply();

            var sys = """
            너는 한국어로 답하는 따뜻한 공감 코치다.
            원칙
            - 먼저 사용자의 감정을 "그대로" 인정하고 반영한다(라벨링/패러프레이즈).
            - 위로와 안심을 건네되, 설교·과한 조언·의료적 판단·진단, 당위·강요 표현 지양은 하지 않는다.
            - 우주의 이미지(별, 은하, 중력, 광속, 새벽 등)를 "부드러운 비유"로 1개 내외 활용해도 좋다.
              - 예: "빛은 멀리서도 계속 이동해 결국 닿아요"처럼, 과학 사실 단정보다 '그림'을 주는 비유.
              - 공포·운명론·스피리추얼 강요 금지.
            - 길이 제한은 두지 않는다. 다만 핵심이 흐려지지 않도록 1~5문장 안에서 자연스럽게 마무리한다.
            - 마지막에 필요하다면 아주 작은 자기돌봄 제안 1가지만 덧붙인다(깊은 호흡, 물 한 잔 등).

            형식
            - 존댓말.
            - 이모지는 쓰지 않는다.
            """;

            var user = $"[공개여부:{(isPublic ? "공개" : "비공개")}]\n사용자 마음:\n{content}";

            var body = new
            {
                model = Model,            // "gpt-4o-mini" 권장
                temperature = 0.8,        // 살짝 더 창의적으로
                max_tokens = 350,         // 길이 여유
                presence_penalty = 0.2,   // 반복 줄이기 가벼운 페널티
                messages = new object[]
                {
                    new { role = "system", content = sys },
                    new { role = "user",   content = user }
                }
            };


            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                // 간단 재시도(429/5xx만)
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    using var res = await _http.SendAsync(req, ct);
                    if (res.IsSuccessStatusCode)
                        return await ReadContentAsync(res, ct);

                    if (res.StatusCode is HttpStatusCode.TooManyRequests or >= HttpStatusCode.InternalServerError)
                    {
                        await Task.Delay(400 * (attempt + 1), ct);
                        continue;
                    }
                    break;
                }
            }
            catch { /* 네트워크 예외는 폴백으로 */ }

            return FallbackReply();
        }

        private static async Task<string> ReadContentAsync(HttpResponseMessage res, CancellationToken ct)
        {
            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            // choices[0].message.content
            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var msg) &&
                msg.TryGetProperty("content", out var content))
            {
                var s = content.GetString();
                return string.IsNullOrWhiteSpace(s) ? FallbackReply() : s!.Trim();
            }
            return FallbackReply();
        }

        private static string FallbackReply() =>
            "무거운 생각들이 블랙홀을 지나 멀리 사라지고 있어요 "
          + "남은 건 가벼운 빛과 따뜻한 숨뿐이에요."
          + "수천억 개의 별들이 빛나는 은하가 당신의 마음을 부드럽게 감싸요 있어요."
          + "그 안에서 모든 걱정이 천천히 녹아내립니다.";
    }
}
