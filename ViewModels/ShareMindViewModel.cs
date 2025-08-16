using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.ViewModels;

public partial class ShareMindViewModel : ObservableObject
{
    // Content가 바뀌면 SubmitCommand의 CanExecute를 자동 갱신
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string content = string.Empty;

    [ObservableProperty] private bool isPublic = true;
    [ObservableProperty] private bool isDirty;

    private readonly MainViewModel _shell;
    public ShareMindViewModel(MainViewModel shell) => _shell = shell;

    private bool CanSubmit() => !string.IsNullOrWhiteSpace(Content);

    partial void OnContentChanged(string value)
    {
        IsDirty = !string.IsNullOrWhiteSpace(value);
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private void Submit()
    {
        // TODO: 저장/전송
        // 제출 흐름에서는 확인 팝업을 띄우지 않음
        // (원하면 여기서 IsDirty를 false로 초기화도 가능)
        IsDirty = false; // 선택사항: 제출 후 초안 상태 해제

        _shell.Navigate<RecvPopupViewModel>(
            NavTab.Home,
            skipDiscardCheck: true,
            init: vm => { /* 팝업에 데이터 넘길 때 설정 */ }
        );
    }

    public void ClearAfterSubmit()
    {
        Content = string.Empty;
        IsDirty = false;
    }
}



