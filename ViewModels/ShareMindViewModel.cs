using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using WouldYou_ShareMind.Services;

namespace WouldYou_ShareMind.ViewModels;

public partial class ShareMindViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
    private string content = string.Empty;

    [ObservableProperty] private bool isPublic = true;
    [ObservableProperty] private bool isDirty;

    // 제너레이터 대신 수동 구현 (알림 포함)
    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                SubmitCommand.NotifyCanExecuteChanged();  // CanExecute 새로고침
        }
    }

    private readonly MainViewModel _shell;
    private readonly IDbService _db;
    private readonly IEmotionService _emotion;

    public ShareMindViewModel(MainViewModel shell, IDbService db, IEmotionService emotion)
    {
        _shell = shell;
        _db = db;
        _emotion = emotion;
    }

    private bool CanSubmit() => !IsBusy && !string.IsNullOrWhiteSpace(Content);

    partial void OnContentChanged(string value) => IsDirty = !string.IsNullOrWhiteSpace(value);

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (!CanSubmit()) return;

        IsBusy = true;
        try
        {
            var text = Content?.Trim();
            if (string.IsNullOrWhiteSpace(text))
                return;

            // 우선 DB에 저장(응답은 NULL) → 새 행 id 확보
            var id = await _db.InsertMindAsync(text, aiReply: null, isLetGo: false);

            // AI 호출 (실패해도 기록은 남아있음)
            var ai = await _emotion.AnalyzeAsync(text, IsPublic);

            // 같은 행에 응답 덧입히기
            if (!string.IsNullOrWhiteSpace(ai))
                await _db.UpdateMindAiReplyAsync(id, ai);

            // 팝업에 응답 표시
            _shell.Navigate<RecvPopupViewModel>(
                NavTab.Home,
                skipDiscardCheck: true,
                init: vm => vm.Message = ai
            );

            // 입력 초기화
            ClearAfterSubmit();
        }
        finally
        {
            IsBusy = false;
        }
    }


    public void ClearAfterSubmit()
    {
        Content = string.Empty;
        IsDirty = false;
    }
}
