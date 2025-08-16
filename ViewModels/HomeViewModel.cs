using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public HomeViewModel()
        {
            // 임시 데이터 3개
            RecentMindLogs.Add(new MindLogPreview
            {
                DateText = "2025.08.08 (금)",
                Summary = "작은 실수 하나가 자꾸 마음에 남아요"
            });
            RecentMindLogs.Add(new MindLogPreview
            {
                DateText = "2025.08.06 (수)",
                Summary = "오늘은 내가 참 잘했다는 느낌이었어요"
            });
            RecentMindLogs.Add(new MindLogPreview
            {
                DateText = "2025.08.04 (일)",
                Summary = "새로운 시작이 설레면서도 두려워요"
            });
        }
    }


    /*최근 기록 리스틔의 목록 3개*/
    //public ObservableCollection<MindLogPreview> RecentMindLogs { get; }
    //= new ObservableCollection<MindLogPreview>(
    //    allLogs.OrderByDescending(x => x.Date).Take(3));


}
