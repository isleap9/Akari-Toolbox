using System.Collections.ObjectModel;
using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// Landing page: welcome, restore-point safety card, This PC info (native WMI),
/// the RECOMMENDED PATH jump grid (Akari-Tool's 18 + Individual Tweaks order),
/// desktop-shortcut + about-link rows, and the QUICK ACCESS cards.
/// </summary>
public partial class HomeViewModel : ViewModelBase, INavigationAware
{
    private readonly INavigationService _navigation;
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;

    public string ApplicationTitle { get; }

    public ObservableCollection<HomeCard> Tools { get; } = [];

    [ObservableProperty]
    private string _osInfo = "...";

    [ObservableProperty]
    private string _cpuInfo = "...";

    [ObservableProperty]
    private string _gpuInfo = "...";

    [ObservableProperty]
    private string _uptimeInfo = "...";

    [ObservableProperty]
    private string _privilegesInfo = "...";

    [ObservableProperty]
    private string _dotNetInfo = "...";

    [ObservableProperty]
    private double _memoryPercent;

    [ObservableProperty]
    private string _memoryText = "...";

    [ObservableProperty]
    private double _drivePercent;

    [ObservableProperty]
    private string _driveText = "...";

    [ObservableProperty]
    private bool _isBusy;

    public HomeViewModel(INavigationService navigation, IInfoBarService infoBar, IStatusService status)
    {
        _navigation = navigation;
        _infoBar = infoBar;
        _status = status;
        ApplicationTitle = "Akari Toolbox";

        Tools.Add(new HomeCard(
            "Gaming Tweaks",
            "Preemption, HDCP, network tuning, GPU presets and utility launchers.",
            "\uE7FC",
            typeof(AkariToolbox.Views.GamingPage)));

        Tools.Add(new HomeCard(
            "Akari OS Tweaks",
            "Live system switches: power, privacy, services, Explorer and more.",
            "\uE713",
            typeof(AkariToolbox.Views.AkariTweaksPage)));

        Tools.Add(new HomeCard("1  Check", "BIOS and PC stability checks.", "\uE73E", typeof(AkariToolbox.Views.CheckPage)));
        Tools.Add(new HomeCard("2  Refresh", "Reset, reinstall and driver refresh.", "\uE895", typeof(AkariToolbox.Views.RefreshPage)));
        Tools.Add(new HomeCard("3  Setup", "System, licensing, apps and updates.", "\uE90F", typeof(AkariToolbox.Views.SetupPage)));
        Tools.Add(new HomeCard("4  Installers", "Apps, launchers and GPU tools.", "\uE896", typeof(AkariToolbox.Views.InstallersPage)));
        Tools.Add(new HomeCard("5  Graphics", "Drivers and GPU settings.", "\uE714", typeof(AkariToolbox.Views.GraphicsPage)));
        Tools.Add(new HomeCard("6  Windows", "Appearance, debloat and performance.", "\uE71D", typeof(AkariToolbox.Views.WindowsPage)));
        Tools.Add(new HomeCard("7  Hardware", "Display, input and network.", "\uE783", typeof(AkariToolbox.Views.HardwarePage)));
        Tools.Add(new HomeCard("8  Advanced", "Security, services and rendering.", "\uEA18", typeof(AkariToolbox.Views.AdvancedPage)));
        Tools.Add(new HomeCard("Individual Tweaks", "Scheduling and granular settings.", "\uE8FD", typeof(AkariToolbox.Views.TweaksPage)));

        foreach (HomeCard card in Tools)
            card.Open = Navigate;
    }

    private void Navigate(HomeCard card) => _navigation.NavigateTo(card.TargetPage);

    /// <summary>Native restore point (WMI SystemRestore). Slow  off-thread with the page locked.</summary>
    [RelayCommand]
    private async Task CreateRestorePointAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start("Creating restore point...");
        try
        {
            await Task.Run(() => RestorePointActions.CreateRestorePoint("Akari Toolbox"));
            _infoBar.Show("Restore point  created", "You can roll back from System Protection.");
        }
        catch (Exception ex)
        {
            _infoBar.Show("Restore point  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    /// <summary>Desktop shortcut to this app, flagged run-as-admin (Akari-Tool's byte-flip).</summary>
    [RelayCommand]
    private void CreateDesktopShortcut()
    {
        try
        {
            string? exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe))
                throw new InvalidOperationException("Could not resolve the app path.");

            NativeOps.CreateShortcut(NativeOps.DesktopShortcut("Akari Toolbox"), exe, runAsAdmin: true);
            _infoBar.Show("Desktop shortcut  created", "Akari Toolbox is now on your Desktop.");
        }
        catch (Exception ex)
        {
            _infoBar.Show("Desktop shortcut  failed", ex.Message);
        }
    }

    [RelayCommand]
    private void OpenGuide() => NativeOps.Launch("https://youtu.be/zwPEDXteJYQ");

    [RelayCommand]
    private void OpenGitHub() => NativeOps.Launch("https://github.com/FR33THYFR33THY/Ultimate");

    public void OnNavigatedTo(object? parameter)
    {
        // Reads are slow  refresh off-thread so navigation never stutters.
        _ = Task.Run(() =>
        {
            OsInfo = Safe(SystemInfo.OsSummary);
            CpuInfo = Safe(SystemInfo.CpuName);
            GpuInfo = Safe(SystemInfo.GpuNames);
            UptimeInfo = Safe(SystemInfo.Uptime);
            PrivilegesInfo = Safe(SystemInfo.Privileges);
            DotNetInfo = Safe(SystemInfo.DotNetVersion);
            (double memPct, string memText) = SystemInfo.MemoryUse();
            MemoryPercent = memPct;
            MemoryText = memText;
            (double drvPct, string drvText) = SystemInfo.SystemDriveUse();
            DrivePercent = drvPct;
            DriveText = drvText;
        });
    }

    public void OnNavigatedFrom()
    {
    }

    private static string Safe(Func<string> read)
    {
        try { return read(); }
        catch { return "Unknown"; }
    }
}

/// <summary>One QUICK ACCESS card (or RECOMMENDED PATH button) on the home page.</summary>
public sealed partial class HomeCard : ObservableObject
{
    public HomeCard(string title, string description, string glyph, Type targetPage)
    {
        Title = title;
        Description = description;
        Glyph = glyph;
        TargetPage = targetPage;
    }

    public string Title { get; }
    public string Description { get; }
    public string Glyph { get; }
    public Type TargetPage { get; }

    /// <summary>Set by the view model so the tile navigates on click. WPF reached the page's
    /// OpenCardCommand through RelativeSource AncestorType=Page; WinUI DataTemplates have no
    /// equivalent, so the command lives on the card and delegates to the same navigation.</summary>
    internal Action<HomeCard> Open { get; set; } = _ => { };

    [RelayCommand]
    private void OpenCard() => Open(this);
}
