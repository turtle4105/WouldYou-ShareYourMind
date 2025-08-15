using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WouldYou_ShareMind.Services;
using WouldYou_ShareMind.ViewModels;

namespace WouldYou_ShareMind
{
    public partial class App : Application
    {
        /// <summary>앱 전역에서 꺼내 쓸 수 있는 DI 컨테이너</summary>
        public static IServiceProvider Services { get; private set; } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // ===============================================================
            // [BOOT] 1) Build DI Container
            // ===============================================================
            var sc = new ServiceCollection();

            // --- Singleton: 앱 전역 공유 서비스들 ---
            sc.AddSingleton<IDbService, DbService>();
            sc.AddSingleton<ISettingsService, SettingsService>();
            sc.AddSingleton<IEmotionService, EmotionService>();
            sc.AddSingleton<IAudioService, AudioService>();

            // MainViewModel은 전역 내비/팝업 상태를 가지므로 Singleton 권장
            sc.AddSingleton<MainViewModel>();

            // --- Transient: 화면 VM들은 요청 시마다 새로 생성 ---
            sc.AddTransient<HomeViewModel>();
            sc.AddTransient<ShareMindViewModel>();
            sc.AddTransient<SleepModeViewModel>();
            sc.AddTransient<ArchiveViewModel>();
            sc.AddTransient<SettingViewModel>();
            sc.AddTransient<RecvPopupViewModel>();

            // MainViewModel에서 CurrentView 전환 시 사용할 VM 팩토리
            sc.AddSingleton<Func<Type, object>>(sp => t => sp.GetRequiredService(t));

            Services = sc.BuildServiceProvider();

            // ===============================================================
            // [BOOT] 2) DbService.Init() -> SQLite 파일/테이블 생성(없으면)
            //         3) SettingsService.Load()
            // ===============================================================
            Services.GetRequiredService<IDbService>().Init();
            Services.GetRequiredService<ISettingsService>().Load();

            // ===============================================================
            // [BOOT] 4) MainWindow(DataContext=MainViewModel)
            //            -> (옵션) MainVM.Navigate<HomeVM>()
            // ===============================================================
            var mainVM = Services.GetRequiredService<MainViewModel>();

            var win = new MainWindow
            {
                DataContext = mainVM
            };
            win.Show();

            // 만약 MainViewModel 생성자에서 초기 네비를 하지 않았다면 여기서 홈으로 이동
            mainVM.Navigate<HomeViewModel>();
        }
    }
}
