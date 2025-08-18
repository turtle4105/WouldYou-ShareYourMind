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

    }
}
