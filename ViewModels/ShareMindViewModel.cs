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
            var ai = await _emotion.AnalyzeAsync(Content, IsPublic);
            await _db.InsertMindAsync(Content.Trim(), ai, isLetGo: false);

            _shell.Navigate<RecvPopupViewModel>(
                NavTab.Home,
                skipDiscardCheck: true,
                init: vm => vm.Message = ai // 아래 2)에서 추가할 속성
            );

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
