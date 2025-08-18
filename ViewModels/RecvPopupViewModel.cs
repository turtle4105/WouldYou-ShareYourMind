using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.ViewModels
{
    public partial class RecvPopupViewModel : ObservableObject
    {
        [ObservableProperty] private string message = string.Empty; // 팝업 TextBlock에 바인딩

        //public string Title => "마음이 우주에 닿았어요.";
        //[ObservableProperty] private string? message;
        //private readonly MainViewModel _main;
        //public RecvPopupViewModel(MainViewModel main) => _main = main;
        //[RelayCommand] private void GoSleep() => _main.Navigate<SleepModeViewModel>();
        //[RelayCommand] private void Close() => _main.Navigate<HomeViewModel>();
    }
}
