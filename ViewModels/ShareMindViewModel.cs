using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.ViewModels
{
    public partial class ShareMindViewModel : ObservableObject
    {
        [ObservableProperty] private string content = string.Empty;
        [ObservableProperty] private bool isPublic = true;

        // 작성 중 여부
        [ObservableProperty] private bool isDirty;

        partial void OnContentChanged(string value)
        {
            // 공백만 있으면 작성 중 아님
            IsDirty = !string.IsNullOrWhiteSpace(value);
        }

        // 등록 후 정리할 때 호출
        public void ClearAfterSubmit()
        {
            Content = string.Empty;
            IsDirty = false;
        }

        // 예시: 등록 커맨드
        public IRelayCommand SubmitCommand => new RelayCommand(() =>
        {
            // 저장 로직...
            ClearAfterSubmit();
        });
    }
}
