using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.MediaFoundation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// DB를 위한 추가 using
using WouldYou_ShareMind.Services;

namespace WouldYou_ShareMind.ViewModels
{


    public class MindLogPreview
    {
        public string DateText { get; set; } = "";
        public string Summary { get; set; } = "";
    }

    public partial class HomeViewModel : ObservableObject
    {
        [ObservableProperty]
        private string spotlightMessage =
            "은하의 회전처럼, 모든 건 다시 빛으로 돌아올 거예요.";  // 초기 디폴트

        public ObservableCollection<MindLogPreview> RecentMindLogs { get; } =
            new ObservableCollection<MindLogPreview>();

        private readonly IDbService _db;
        public HomeViewModel(IDbService db)
        {
           _db = db;
            Console.WriteLine("[HOME] ctor");
            _ = LoadAsync();   // 생성 시 데이터 불러오기

        }

        private async Task LoadAsync()
        {
            Console.WriteLine("[HOME] LoadAsync start");

            RecentMindLogs.Clear();

            var dtos = await _db.GetRecentMindAsync(limit: 3);

            var ko = CultureInfo.GetCultureInfo("ko-KR");
            foreach (var dto in dtos)
            {
                var dateTxt = dto.CreatedAt == DateTime.MinValue
                    ? ""
                    : $"{dto.CreatedAt:yyyy.MM.dd} ({ko.DateTimeFormat.GetDayName(dto.CreatedAt.DayOfWeek)[0]})";

                // 요약: content 우선, 비어있으면 ai_reply 사용
                var raw = string.IsNullOrWhiteSpace(dto.Content) ? (dto.AiReply ?? "") : dto.Content;
                var summary = raw.Replace("\r", " ").Replace("\n", " ").Trim();
                if (summary.Length > 28) summary = summary[..28] + "…";

                RecentMindLogs.Add(new MindLogPreview
                {
                    DateText = dateTxt,
                    Summary = summary
                });
            }

            Console.WriteLine($"[HOME] list bound count: {RecentMindLogs.Count}");
        }
    }


}
