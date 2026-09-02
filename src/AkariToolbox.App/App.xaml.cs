using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using AkariToolbox.App.Services;
using AkariToolbox.App.Services.TweakHandlers;
using AkariToolbox.Framework.Logging;
using AkariToolbox.App.ViewModels;
using AkariToolbox.Framework;
using AkariToolbox.Framework.Messaging;
using AkariToolbox.Framework.Navigation;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App;

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
        // Headless post-reboot relaunch: DefenderPhase2Scheduler.ScheduleRunOnce() schedules
        // "<exe>" --defender-phase2 <token> to run once at next login (see DefenderTweakHandler
        // CR-01/CR-03 fix). Handle it before anything else — no single-instance registration,
        // no DI host, no window — run the native SYSTEM-impersonation cleanup and exit. The
        // token is verified inside RunPhase2Native itself (T-01-17 security-audit fix); a
        // missing/wrong token here still routes through the same headless path so an invalid
        // attempt is logged and rejected without ever falling through to a normal GUI launch.
        var launchArgs = Environment.GetCommandLineArgs();
        var phase2ArgIndex = Array.FindIndex(
            launchArgs, a => string.Equals(a, "--defender-phase2", StringComparison.OrdinalIgnoreCase));
        if (phase2ArgIndex >= 0)
        {
            var token = phase2ArgIndex + 1 < launchArgs.Length ? launchArgs[phase2ArgIndex + 1] : null;
            RunDefenderPhase2Headless(token);
            Environment.Exit(0);
            return;
        }

        base.OnLaunched(args);

        // Single-instance: only the first process becomes the primary instance.
        // Duplicate launches forward their activation to it and exit silently.
        var mainInstance = AppInstance.FindOrRegisterForKey("AkariToolbox");
        if (!mainInstance.IsCurrent)
        {
            var activation = AppInstance.GetCurrent().GetActivatedEventArgs();
            mainInstance.RedirectActivationToAsync(activation).GetAwaiter().GetResult();
            Environment.Exit(0);
            return;
        }

        // When a duplicate launch is redirected here, bring the existing window to the front.
        mainInstance.Activated += (_, _) => MainWindow?.Activate();

        _host = BuildHost();
        Services = _host.Services;

        // Log crashes that happen off the UI thread (WinUI's UnhandledException only
        // covers the UI thread).
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        var messenger = Services.GetRequiredService<IMessenger>();

        // Re-resolve localized strings whenever the culture changes.
        var localizer = Services.GetRequiredService<LocalizedStrings>();
        messenger.Register<CultureChangedMessage>(localizer, (r, _) => ((LocalizedStrings)r).Refresh());

        // Create and show the main window (it wires itself into navigation and theme).
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

    /// <summary>
    /// Runs <see cref="DefenderTweakHandler.RunPhase2Native"/> headlessly (no DI host, no
    /// window) and logs to a plain file, since the DI-backed <c>ILogConsoleService</c> isn't
    /// available in this no-UI relaunch path. Called only when this process was launched
    /// with <c>--defender-phase2</c> by the RunOnce entry <c>DefenderPhase2Scheduler</c>
    /// scheduled at the end of phase 1. <paramref name="token"/> is forwarded as-is (including
    /// <c>null</c> if it was missing) — <see cref="DefenderTweakHandler.RunPhase2Native"/>
    /// itself verifies it before doing anything (T-01-17 security-audit fix).
    /// </summary>
    private static void RunDefenderPhase2Headless(string? token)
    {
        var logDir = Path.Combine(SettingsFolder, "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "defender-phase2.log");

        try
        {
            using var writer = new StreamWriter(logPath, append: true) { AutoFlush = true };
            void Log(string message) => writer.WriteLine($"{DateTime.Now:O} {message}");

            Log("[PHASE2] Headless relaunch detected (--defender-phase2). Starting native phase 2...");
            DefenderTweakHandler.RunPhase2Native(token, Log);
            Log("[PHASE2] Headless relaunch complete.");
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(logPath, $"{DateTime.Now:O} [PHASE2] FATAL — {ex}{Environment.NewLine}");
            }
            catch
            {
                // Logging itself failed — nothing more we can do in a headless, no-UI path.
            }
        }
    }

    private static IHost BuildHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();
        builder.Logging.AddProvider(new FileLoggerProvider(Path.Combine(SettingsFolder, "logs")));

        // Framework services (settings, theme, culture, dialogs, windows, pickers, info bar).
        builder.Services.AddMvvmFramework();

        // System primitives (registry, and later service-controller/script-runner/log-console).
        builder.Services.AddAkariSystemPrimitives();

        // Tweak handlers, auto-discovered via assembly scan, plus the orchestrating catalog.
        builder.Services.AddTweakHandlers();

        // App services.
        builder.Services.AddSingleton<LocalizedStrings>();

        // Persist settings under the app's own folder.
        builder.Services.AddSingleton<ISettingsStorage>(new FileSettingsStorage("AkariToolbox"));

        // Main window.
        builder.Services.AddSingleton<MainWindow>();

        // View models.
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();
        builder.Services.AddTransient<AkariOSTweaksViewModel>();
        builder.Services.AddTransient<GamingTweaksViewModel>();
        builder.Services.AddTransient<DebloatViewModel>();
        builder.Services.AddTransient<DownloadsViewModel>();

        // Navigation: pages are created through the DI container.
        builder.Services.AddSingleton<INavigationService>(sp =>
            new FrameNavigationService(pageType => (Page)ActivatorUtilities.CreateInstance(sp, pageType)));

        // Infrastructure providers consumed by framework services.
        builder.Services.AddSingleton(sp => new Func<XamlRoot?>(() => MainWindow?.Content?.XamlRoot));
        builder.Services.AddSingleton(sp => new Func<Microsoft.UI.WindowId>(() =>
            MainWindow is null
                ? default
                : Microsoft.UI.Win32Interop.GetWindowIdFromWindow(WinRT.Interop.WindowNative.GetWindowHandle(MainWindow))));

        return builder.Build();
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Services?.GetService<ILogger<App>>()?.LogError(e.Exception, "Unhandled application exception");

        // If the window isn't shown yet there is nowhere to display a dialog,
        // so fall back to the normal OS crash handling.
        if (MainWindow?.Content?.XamlRoot is null)
        {
            return;
        }

        // Suppress termination so the dialog can be shown, then exit deliberately.
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
                // Never re-enter the crash handler from the dialog itself.
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
            // A failing service Dispose must never prevent the app from exiting.
        }

        _host = null;
        Exit();
    }
}
