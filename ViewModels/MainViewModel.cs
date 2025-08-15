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

        private readonly Func<Type, object> _vmFactory;
        public MainViewModel(Func<Type, object> vmFactory)
        {
            _vmFactory = vmFactory;
            Navigate<HomeViewModel>();
        }

        public void Navigate<TVM>() where TVM : class =>
            NavigateWith<TVM>(_ => { });

        public void NavigateWith<TVM>(Action<TVM> init) where TVM : class
        {
            var vm = (TVM)_vmFactory(typeof(TVM));
            init(vm);
            ShowNavBar = typeof(TVM) != typeof(RecvPopupViewModel);
            CurrentView = vm;
        }

        [RelayCommand] private void GoHome() => Navigate<HomeViewModel>();
        [RelayCommand] private void GoShare() => Navigate<ShareMindViewModel>();
        [RelayCommand] private void GoSleep() => Navigate<SleepModeViewModel>();
        [RelayCommand] private void GoArchive() => Navigate<ArchiveViewModel>();
        [RelayCommand] private void GoSetting() => Navigate<SettingViewModel>();
    }
}
