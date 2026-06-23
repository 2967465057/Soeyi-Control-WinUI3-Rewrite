using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SoeyiWinUI_v2.Services;
using SoeyiWinUI_v2.ViewModels;
using SoeyiWinUI_v2.Views;
using System;
using System.Runtime.InteropServices;

namespace SoeyiWinUI_v2;

public partial class App : Application
{
    [DllImport("user32.dll")]
    private static extern bool ShowWindowW(nint hWnd, int nCmdShow);
    internal Window? _w;
    public static Action<bool>? _titleBarThemeUpdater;
    public App() { 
    InitializeComponent(); 
    this.UnhandledException += (s, e) => {
        File.AppendAllText(@"D:\AutoClaw\Auto\soeyi-xaml-crash.log", $"XAML CRASH: {e.Exception}`n{e.Message}`n");
        e.Handled = true;
    };
}

    private bool _forceClose = false;
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            var sp = new ServiceCollection()
                .AddSingleton<ConfigService>()
                .AddSingleton<ThemeService>()
                .AddSingleton<DeviceService>()
                .AddSingleton<HardwareMonitorService>()
                .AddTransient<MainViewModel>()
                .BuildServiceProvider();
            var vm = sp.GetRequiredService<MainViewModel>();
            var page = new MainPage(vm);

            _w = new Window { Title = "SOEYI WinUI" };
            // Set app icon
            var iconPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets", "Soeyi64.ico");
            if (System.IO.File.Exists(iconPath)) _w.AppWindow.SetIcon(iconPath);
            _w.Content = page;

            // Wire tray menu actions (after window exists)
            MainPage._trayShowAction = () => { _w.DispatcherQueue.TryEnqueue(() => _w.AppWindow.Show(true)); };
            MainPage._trayExitAction = () =>
            {
                _w.DispatcherQueue.TryEnqueue(() =>
                {
                    page.RemoveTrayIcon();
                    _forceClose = true;
                    _w.Close();
                });
            };

            _w.AppWindow.Closing += async (_, args) =>
            {
                if (_forceClose) return;
                args.Cancel = true;

                var (isSet, exit) = vm.GetClosePreference();
                var shouldExit = await page.ShowCloseDialog(isSet, exit);
                if (shouldExit)
                {
                    page.RemoveTrayIcon();
                    _forceClose = true;
                    _w.Close();
                }
                else
                {
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_w);
                    page.CreateTrayIcon(hwnd, _w.AppWindow);
                    // Defer hide to next dispatcher tick — avoids race with closing flow
                    _w.DispatcherQueue.TryEnqueue(() => _w.AppWindow.Hide());
                }
            };

            _w.Activate();

            // Merge custom title bar into single bar
            _w.ExtendsContentIntoTitleBar = true;
            _w.SetTitleBar(page.TitleBarElement);
            // Title bar button colors - dark text for light mode visibility
            _w.AppWindow.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 51, 51, 51);
            _w.AppWindow.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 153, 153, 153);
            _w.AppWindow.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255, 0, 0, 0);
            _w.AppWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(20, 0, 0, 0);

            // Expose for theme switching
            _titleBarThemeUpdater = isDark =>
            {
                _w.DispatcherQueue.TryEnqueue(() =>
                {
                    _w.AppWindow.TitleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255,
                        isDark ? (byte)230 : (byte)51, isDark ? (byte)230 : (byte)51, isDark ? (byte)240 : (byte)51);
                    _w.AppWindow.TitleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255,
                        isDark ? (byte)130 : (byte)153, isDark ? (byte)130 : (byte)153, isDark ? (byte)140 : (byte)153);
                    _w.AppWindow.TitleBar.ButtonHoverForegroundColor = Windows.UI.Color.FromArgb(255,
                        isDark ? (byte)255 : (byte)0, isDark ? (byte)255 : (byte)0, isDark ? (byte)255 : (byte)0);
                    _w.AppWindow.TitleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(
                        isDark ? (byte)40 : (byte)20, (byte)255, (byte)255, (byte)255);
                });
            };
            // Apply immediately for current (light) theme
            _titleBarThemeUpdater(false);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(@"D:\AutoClaw\Auto\soeyi-crash.log", $"CRASH: {ex}");
        }
    }
}
