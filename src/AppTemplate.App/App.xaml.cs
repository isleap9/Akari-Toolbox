using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using AkariToolbox.Helpers;
using AkariToolbox.Tweaks;
using AkariToolbox.ViewModels;
using AkariToolbox.Views;
using AkariToolbox.Services;
using AppTemplate.Framework;
using AppTemplate.Framework.Logging;
using AppTemplate.Framework.Messaging;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;

namespace AkariToolbox;

public partial class App : Application
{
    private IHost? _host;

    /// <summary>Global service provider, usable from XAML bindings and non-DI code.</summary>
    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>The primary application window.</summary>
    public static MainWindow? MainWindow { get; private set; }

    public static string AppName => "Akari Toolbox";

    public static string AppVersion =>
        typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

    /// <summary>Folder and file used for persisted JSON settings.</summary>
    public static string SettingsFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AkariToolbox");

    public static string SettingsFilePath => Path.Combine(SettingsFolder, "settings.json");

    public App()
    {
        InitializeComponent();
        UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        // Safe-boot re-entry flows MUST be checked before single-instance registration:
        // they run headless (no window) and exit. Args come from the command line 
        // AppInstance activation args carry no Arguments on WinAppSDK 2.3.1 unpackaged.
        string[] cmd = Environment.GetCommandLineArgs();
        bool Has(string flag) => cmd.Any(a => a.Equals(flag, StringComparison.OrdinalIgnoreCase));

        if (Has("--ddu-auto") || Has("--ddu-manual"))
        {
            bool auto = Has("--ddu-auto");
            try { DriverActions.FinishDdu(auto); }
            catch (Exception ex) { NativeMessageBox.ShowWarning(ex.Message, "DDU Clean"); }
            Environment.Exit(0);
            return;
        }

        if (Has("--defender-optimize") || Has("--defender-default"))
        {
            bool optimize = Has("--defender-optimize");
            try
            {
                if (optimize) WindowsActions.FinishDefenderOptimize();
                else WindowsActions.FinishDefenderDefault();
            }
            catch (Exception ex) { NativeMessageBox.ShowWarning(ex.Message, "Defender"); }
            Environment.Exit(0);
            return;
        }

        if (Has("--defender-disable") || Has("--defender-enable") ||
            Has("--services-off") || Has("--services-on"))
        {
            try
            {
                if (Has("--defender-disable")) AdvancedActions.FinishDefenderDisable();
                else if (Has("--defender-enable")) AdvancedActions.FinishDefenderEnable();
                else if (Has("--services-off")) AdvancedActions.FinishServicesOff();
                else AdvancedActions.FinishServicesDefault();
            }
            catch (Exception ex) { NativeMessageBox.ShowWarning(ex.Message, "Advanced"); }
            Environment.Exit(0);
            return;
        }

        // Normal launch: single-instance.
        var mainInstance = AppInstance.FindOrRegisterForKey("AkariToolbox");
        if (!mainInstance.IsCurrent)
        {
            var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
            mainInstance.RedirectActivationToAsync(activation).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        mainInstance.Activated += (_, _) => MainWindow?.Activate();

        _host = BuildHost();
        Services = _host.Services;

        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var messenger = Services.GetRequiredService<IMessenger>();
        var localizer = Services.GetRequiredService<LocalizedStrings>();
        messenger.Register<CultureChangedMessage>(localizer, (r, _) => ((LocalizedStrings)r).Refresh());

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Closed += (_, _) => Shutdown();
        MainWindow.Activate();

        DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
        {
            var cultureService = Services.GetRequiredService<ICultureService>();
            var themeService = Services.GetRequiredService<IThemeService>();

            await cultureService.InitializeAsync();
            await themeService.InitializeAsync();

            MainWindow?.ApplyTheme(themeService.CurrentTheme);
        });
    }

    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();
        builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(SettingsFolder, "logs")));

        builder.Services.AddMvvmFramework();
        builder.Services.AddSingleton<LocalizedStrings>();
        builder.Services.AddSingleton<ISettingsStorage>(new FileSettingsStorage("AkariToolbox"));
        builder.Services.AddSingleton<MainWindow>();

        // AkariToolbox view models (smallest-first registration; all transient).
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<AkariTweaksViewModel>();
        builder.Services.AddTransient<InstallersViewModel>();
        builder.Services.AddTransient<TweaksViewModel>();
        builder.Services.AddTransient<HardwareViewModel>();
        builder.Services.AddTransient<RefreshViewModel>();
        builder.Services.AddTransient<CheckViewModel>();
        builder.Services.AddTransient<GamingViewModel>();
        builder.Services.AddTransient<GamingCatalogViewModel>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<SetupViewModel>();
        builder.Services.AddTransient<GraphicsViewModel>();
        builder.Services.AddTransient<AdvancedViewModel>();
        builder.Services.AddTransient<WindowsViewModel>();

        // Pages (created through DI).
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<AkariTweaksPage>();
        builder.Services.AddTransient<GamingPage>();
        builder.Services.AddTransient<GamingCatalogPage>();
        builder.Services.AddTransient<CheckPage>();
        builder.Services.AddTransient<RefreshPage>();
        builder.Services.AddTransient<SetupPage>();
        builder.Services.AddTransient<InstallersPage>();
        builder.Services.AddTransient<GraphicsPage>();
        builder.Services.AddTransient<WindowsPage>();
        builder.Services.AddTransient<HardwarePage>();
        builder.Services.AddTransient<AdvancedPage>();
        builder.Services.AddTransient<TweaksPage>();
        builder.Services.AddTransient<SettingsPage>();

        builder.Services.AddSingleton<INavigationService>(sp =>
            new FrameNavigationService(pageType => (Page)ActivatorUtilities.CreateInstance(sp, pageType)));

        builder.Services.AddSingleton(sp => new Func<XamlRoot?>(() => MainWindow?.Content?.XamlRoot));
        builder.Services.AddSingleton(sp => new Func<IntPtr>(() =>
            MainWindow is null ? IntPtr.Zero : WinRT.Interop.WindowNative.GetWindowHandle(MainWindow)));

        return builder.Build();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Services?.GetService<ILogger<App>>()?.LogError(e.Exception, "Unhandled application exception");

        if (MainWindow?.Content?.XamlRoot is null)
        {
            return;
        }

        e.Handled = true;

        DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
        {
            try
            {
                var dialogService = Services!.GetRequiredService<IDialogService>();
                await dialogService.ShowErrorAsync(
                    "Something went wrong",
                    $"The app ran into an unexpected error and needs to close.{Environment.NewLine}{Environment.NewLine}Details were logged to:{Environment.NewLine}{Path.Combine(SettingsFolder, "logs")}");
            }
            catch
            {
            }
            finally
            {
                Shutdown();
            }
        });
    }

    private void OnAppDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        Services?.GetService<ILogger<App>>()?.LogError(exception, "AppDomain unhandled exception");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Services?.GetService<ILogger<App>>()?.LogError(e.Exception, "Unobserved task exception");
        e.SetObserved();
    }

    /// <summary>Disposes the DI host (flushing loggers) and terminates the app.</summary>
    private void Shutdown()
    {
        try
        {
            _host?.Dispose();
        }
        catch
        {
        }

        _host = null;
        Exit();
    }
}
