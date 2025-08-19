using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WouldYou_ShareMind.ViewModels;

namespace WouldYou_ShareMind.Views
{
    /// <summary>
    /// SleepModeView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SleepModeView : UserControl
    {
        public SleepModeView()
        {
            InitializeComponent();
            Console.WriteLine("SleepModeView 생성");
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SleepModeViewModel vm)
                await vm.StartAsync();     // 진입 즉시 자동 재생
        }

        private async void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SleepModeViewModel vm)
                await vm.StopAsync();      // 화면 떠날 때 정리(오디오/DB)
        }
    }
}
