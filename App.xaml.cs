// App.xaml.cs
using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WouldYou_ShareMind.Services;
using WouldYou_ShareMind.ViewModels;

namespace WouldYou_ShareMind
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = default!;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var sc = new ServiceCollection();

                // Services (Singleton)
                sc.AddSingleton<IDbService, DbService>();
                sc.AddSingleton<ISettingsService, SettingsService>();
                sc.AddSingleton<IEmotionService, EmotionService>();
                sc.AddSingleton<IAudioService, AudioService>();

                // ViewModels
                sc.AddSingleton<MainViewModel>();
                sc.AddTransient<HomeViewModel>();
                sc.AddTransient<ShareMindViewModel>();
                sc.AddTransient<SleepModeViewModel>();
                sc.AddTransient<ArchiveViewModel>();
                sc.AddTransient<SettingViewModel>();
                sc.AddTransient<RecvPopupViewModel>();

                // VM Factory
                sc.AddSingleton<Func<Type, object>>(sp => t => sp.GetRequiredService(t));

                Services = sc.BuildServiceProvider();

                // DB/설정 초기화
                Services.GetRequiredService<IDbService>().Init();
                Services.GetRequiredService<ISettingsService>().Load();

                // 메인 윈도우 띄우기
                var mainVM = Services.GetRequiredService<MainViewModel>();
                var win = new MainWindow { DataContext = mainVM };
                win.Show();

                // 홈으로 진입
                mainVM.Navigate<HomeViewModel>(ViewModels.NavTab.Home);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Startup Error");
                Shutdown();
            }
        }
    }
}
