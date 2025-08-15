using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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

        public void Navigate<TVM>(NavTab tab) where TVM : class
        {
            var vm = (TVM)_vmFactory(typeof(TVM));
            ShowNavBar = typeof(TVM) != typeof(RecvPopupViewModel);
            ActiveTab = tab;
            CurrentView = vm;
        }

        [RelayCommand] private void GoHome() => Navigate<HomeViewModel>(NavTab.Home);
        [RelayCommand] private void GoShare() => Navigate<ShareMindViewModel>(NavTab.Share);
        [RelayCommand] private void GoArchive() => Navigate<ArchiveViewModel>(NavTab.Archive);
        [RelayCommand] private void GoSleep() => Navigate<SleepModeViewModel>(NavTab.Sleep);
        [RelayCommand] private void GoSetting() => Navigate<SettingViewModel>(NavTab.Setting);
    }
}
