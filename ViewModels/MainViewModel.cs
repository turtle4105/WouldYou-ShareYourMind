using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WouldYou_ShareMind.ViewModels
{

    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty] private object? currentView;
        [ObservableProperty] private bool showNavBar = true;
        [ObservableProperty] private NavTab activeTab;

        private readonly Func<Type, object> _vmFactory;
        public MainViewModel(Func<Type, object> vmFactory)
        {
            _vmFactory = vmFactory;
            Navigate<HomeViewModel>(NavTab.Home);
        }

        // 공통 확인 함수
        private bool ConfirmDiscardIfNeeded(NavTab targetTab)
        {
            // 같은 탭으로 이동이면 굳이 확인하지 않음
            if (ActiveTab == targetTab) return true;

            // 현재 화면이 ShareMind이고 작성 중이라면 경고
            if (CurrentView is ShareMindViewModel sm && sm.IsDirty)
            {
                var result = MessageBox.Show(
                    "작성 중인 내용이 있어요.\n이동하면 지금까지 작성한 내용은 사라집니다.\n계속 이동할까요?",
                    "",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                return result == MessageBoxResult.Yes;
            }
            return true;
        }

        // 네비게이션 헬퍼
        public void Navigate<TVM>(NavTab tab, bool skipDiscardCheck = false, Action<TVM>? init = null)
            where TVM : class
        {
            if (!skipDiscardCheck && !ConfirmDiscardIfNeeded(tab)) return;

            var vm = (TVM)_vmFactory(typeof(TVM));
            init?.Invoke(vm);

            ShowNavBar = typeof(TVM) != typeof(RecvPopupViewModel);
            ActiveTab = tab;
            CurrentView = vm;
        }

        // 커맨드들 그대로 사용 (내부에서 Navigate 호출)
        [RelayCommand] private void GoHome() => Navigate<HomeViewModel>(NavTab.Home);
        [RelayCommand] private void GoShare() => Navigate<ShareMindViewModel>(NavTab.Share);
        [RelayCommand] private void GoArchive() => Navigate<ArchiveViewModel>(NavTab.Archive);
        [RelayCommand] private void GoSleep() => Navigate<SleepModeViewModel>(NavTab.Sleep);
        [RelayCommand] private void GoSetting() => Navigate<SettingViewModel>(NavTab.Setting);
    }
}
