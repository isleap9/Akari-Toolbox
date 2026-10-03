using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// 6  Windows page: native ports of Ultimate "6 Windows"  Optimize/Default
/// pairs as live-state toggles, the Start Menu layout as a combobox, one-shot
/// rows (checks, openers, installers) as buttons.
/// </summary>
public partial class WindowsViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private bool _loading;

    [ObservableProperty] private bool _startMenuClean;
    [ObservableProperty] private int _startMenuLayoutIndex;
    [ObservableProperty] private bool _contextMenuClean;
    [ObservableProperty] private bool _themeBlack;
    [ObservableProperty] private bool _signoutWallpaperBlack;
    [ObservableProperty] private bool _accountPicturesBlack;
    [ObservableProperty] private bool _widgetsOff;
    [ObservableProperty] private bool _copilotOff;
    [ObservableProperty] private bool _gameBarOff;
    [ObservableProperty] private bool _edgeUninstalled;
    [ObservableProperty] private bool _notepadOn;
    [ObservableProperty] private bool _controlPanelOptimized;
    [ObservableProperty] private bool _devicePowerOff;
    [ObservableProperty] private bool _networkPowerOff;
    [ObservableProperty] private bool _ipv4Only;
    [ObservableProperty] private bool _writeCacheOff;
    [ObservableProperty] private bool _powerPlanOn;
    [ObservableProperty] private bool _timerResolutionOn;
    [ObservableProperty] private bool _uacOff;
    [ObservableProperty] private bool _defenderOptimized;
    [ObservableProperty] private bool _isBusy;

    public string[] StartMenuLayoutOptions => WindowsActions.StartMenuLayoutOptions;

    public WindowsViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
    }

    public void OnNavigatedTo(object? parameter)
    {
        _loading = true;
        try
        {
            StartMenuClean = Safe(WindowsActions.ReadStartMenuClean);
            StartMenuLayoutIndex = SafeIndex(WindowsActions.ReadStartMenuLayout);
            ContextMenuClean = Safe(WindowsActions.ReadContextMenuClean);
            ThemeBlack = Safe(WindowsActions.ReadThemeBlack);
            SignoutWallpaperBlack = Safe(WindowsActions.ReadSignoutWallpaperBlack);
            AccountPicturesBlack = Safe(WindowsActions.ReadAccountPicturesBlack);
            WidgetsOff = Safe(WindowsActions.ReadWidgetsOff);
            CopilotOff = Safe(WindowsActions.ReadCopilotOff);
            GameBarOff = Safe(WindowsActions.ReadGameBarOff);
            EdgeUninstalled = Safe(WindowsActions.ReadEdgeUninstalled);
            NotepadOn = Safe(WindowsActions.ReadNotepadOn);
            ControlPanelOptimized = Safe(WindowsActions.ReadControlPanelOptimized);
            DevicePowerOff = Safe(WindowsActions.ReadDevicePowerOff);
            NetworkPowerOff = Safe(WindowsActions.ReadNetworkPowerOff);
            Ipv4Only = Safe(WindowsActions.ReadIpv4Only);
            WriteCacheOff = Safe(WindowsActions.ReadWriteCacheOff);
            PowerPlanOn = Safe(WindowsActions.ReadPowerPlanOn);
            TimerResolutionOn = Safe(WindowsActions.ReadTimerResolutionOn);
            UacOff = Safe(WindowsActions.ReadUacOff);
            DefenderOptimized = Safe(WindowsActions.ReadDefenderOptimized);
        }
        finally
        {
            _loading = false;
        }
    }

    public void OnNavigatedFrom()
    {
    }

    private static bool Safe(Func<bool> read)
    {
        try { return read(); }
        catch { return false; }
    }

    private static int SafeIndex(Func<int> read)
    {
        try { return Math.Max(0, read()); }
        catch { return 0; }
    }

    partial void OnStartMenuCleanChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Start Menu & Taskbar", value,
            clean =>
            {
                if (clean) WindowsActions.StartMenuTaskbarClean();
                else WindowsActions.StartMenuTaskbarDefault();
            },
            WindowsActions.ReadStartMenuClean, v => StartMenuClean = v);
    }

    partial void OnStartMenuLayoutIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= WindowsActions.StartMenuLayoutOptions.Length)
            return;
        _ = ApplyStartMenuLayoutAsync(value);
    }

    private async Task ApplyStartMenuLayoutAsync(int index)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{WindowsActions.StartMenuLayoutOptions[index]}...");
        try
        {
            await Task.Run(() => WindowsActions.ApplyStartMenuLayout(index));
            _infoBar.Show($"{WindowsActions.StartMenuLayoutOptions[index]}  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { StartMenuLayoutIndex = SafeIndex(WindowsActions.ReadStartMenuLayout); }
            finally { _loading = false; }
            _infoBar.Show("Start Menu Layout  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    partial void OnContextMenuCleanChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Context Menu", value,
            clean =>
            {
                if (clean) WindowsActions.ContextMenuClean();
                else WindowsActions.ContextMenuDefault();
            },
            WindowsActions.ReadContextMenuClean, v => ContextMenuClean = v);
    }

    partial void OnThemeBlackChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Theme", value,
            black =>
            {
                if (black) WindowsActions.ApplyThemeBlack();
                else WindowsActions.ApplyThemeDefault();
            },
            WindowsActions.ReadThemeBlack, v => ThemeBlack = v);
    }

    partial void OnSignoutWallpaperBlackChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Sign-out Wallpaper", value,
            black =>
            {
                if (black) WindowsActions.SignoutWallpaperBlack();
                else WindowsActions.SignoutWallpaperDefault();
            },
            WindowsActions.ReadSignoutWallpaperBlack, v => SignoutWallpaperBlack = v);
    }

    partial void OnAccountPicturesBlackChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Account Pictures", value,
            black =>
            {
                if (black) WindowsActions.UserAccountPicturesBlack();
                else WindowsActions.UserAccountPicturesDefault();
            },
            WindowsActions.ReadAccountPicturesBlack, v => AccountPicturesBlack = v);
    }

    partial void OnWidgetsOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Widgets", value,
            off =>
            {
                if (off) WindowsActions.WidgetsOff();
                else WindowsActions.WidgetsDefault();
            },
            WindowsActions.ReadWidgetsOff, v => WidgetsOff = v);
    }

    partial void OnCopilotOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Copilot", value,
            off =>
            {
                if (off) WindowsActions.CopilotOff();
                else WindowsActions.CopilotDefault();
            },
            WindowsActions.ReadCopilotOff, v => CopilotOff = v);
    }

    partial void OnGameBarOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Game Bar", value,
            off =>
            {
                if (off) WindowsActions.GameBarOff();
                else WindowsActions.GameBarDefault();
            },
            WindowsActions.ReadGameBarOff, v => GameBarOff = v);
    }

    partial void OnEdgeUninstalledChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Edge", value,
            uninstall =>
            {
                if (uninstall) WindowsActions.EdgeUninstall();
                else WindowsActions.EdgeReinstallDefault();
            },
            WindowsActions.ReadEdgeUninstalled,
            v => EdgeUninstalled = v);
    }

    partial void OnNotepadOnChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Notepad", value,
            on =>
            {
                if (on) WindowsActions.NotepadSettingsOn();
                else WindowsActions.NotepadSettingsDefault();
            },
            WindowsActions.ReadNotepadOn, v => NotepadOn = v);
    }

    partial void OnControlPanelOptimizedChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Control Panel", value,
            optimize =>
            {
                if (optimize) WindowsActions.ControlPanelOptimize();
                else WindowsActions.ControlPanelDefault();
            },
            WindowsActions.ReadControlPanelOptimized, v => ControlPanelOptimized = v);
    }

    partial void OnDevicePowerOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Device Power Savings", value,
            off =>
            {
                if (off) WindowsActions.DeviceManagerPowerOff();
                else WindowsActions.DeviceManagerPowerDefault();
            },
            WindowsActions.ReadDevicePowerOff, v => DevicePowerOff = v);
    }

    partial void OnNetworkPowerOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Adapter Power Savings", value,
            off =>
            {
                if (off) WindowsActions.NetworkAdapterPowerOff();
                else WindowsActions.NetworkAdapterPowerDefault();
            },
            WindowsActions.ReadNetworkPowerOff, v => NetworkPowerOff = v);
    }

    partial void OnIpv4OnlyChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("IPv4 Only", value,
            only =>
            {
                if (only) WindowsActions.NetworkIpv4Only();
                else WindowsActions.NetworkBindingsDefault();
            },
            WindowsActions.ReadIpv4Only, v => Ipv4Only = v);
    }

    partial void OnWriteCacheOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Write Cache Flushing", value,
            off =>
            {
                if (off) WindowsActions.WriteCacheFlushingOff();
                else WindowsActions.WriteCacheFlushingDefault();
            },
            WindowsActions.ReadWriteCacheOff, v => WriteCacheOff = v);
    }

    partial void OnPowerPlanOnChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Power Plan", value,
            on =>
            {
                if (on) WindowsActions.PowerPlanOn();
                else WindowsActions.PowerPlanDefault();
            },
            WindowsActions.ReadPowerPlanOn, v => PowerPlanOn = v);
    }

    partial void OnTimerResolutionOnChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Timer Resolution", value,
            on =>
            {
                if (on) WindowsActions.TimerResolutionOn();
                else WindowsActions.TimerResolutionDefault();
            },
            WindowsActions.ReadTimerResolutionOn, v => TimerResolutionOn = v);
    }

    partial void OnUacOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("UAC", value,
            off =>
            {
                if (off) WindowsActions.SetUacOff();
                else WindowsActions.SetUacDefault();
            },
            WindowsActions.ReadUacOff,
            v => UacOff = v);
    }

    partial void OnDefenderOptimizedChanged(bool value)
    {
        if (_loading) return;
        _ = ApplyDefenderAsync(value);
    }

    private async Task ApplyDefenderAsync(bool optimize)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start(optimize ? "Optimizing Defender..." : "Restoring Defender...");
        try
        {
            // Arms the safe-boot RunOnce and reboots  the payload finishes there.
            await Task.Run(() =>
            {
                if (optimize)
                    WindowsActions.ApplyDefenderOptimize();
                else
                    WindowsActions.ApplyDefenderDefault();
            });
        }
        catch (Exception ex)
        {
            _loading = true;
            try { DefenderOptimized = Safe(WindowsActions.ReadDefenderOptimized); }
            finally { _loading = false; }
            _infoBar.Show("Defender  failed", ex.Message);
            IsBusy = false;
            _status.Complete();
        }
    }

    private async void ApplyToggle(
        string title, bool target,
        Action<bool> apply, Func<bool> read,
        Action<bool> rollback)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{title}...");
        try
        {
            await Task.Run(() => apply(target));
            _infoBar.Show($"{title}  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { rollback(Safe(read)); }
            finally { _loading = false; }
            _infoBar.Show($"{title}  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    //  One-shot rows 

    [RelayCommand] private void OpenShortcuts() => Run("Start Menu Shortcuts", WindowsActions.CreateStartMenuShortcuts);
    [RelayCommand] private void OpenGameMode() => Run("Game Mode", WindowsActions.OpenGameMode);
    [RelayCommand] private void OpenPointer() => Run("Pointer Precision", WindowsActions.OpenPointerPrecision);
    [RelayCommand] private void OpenScaling() => Run("Scaling", WindowsActions.OpenScalingSettings);
    [RelayCommand] private void OpenSound() => Run("Sound", WindowsActions.OpenSoundPanel);
    [RelayCommand] private Task RunLoudnessEq() => RunAsync("Loudness EQ", WindowsActions.LoudnessEqUnhide);
    [RelayCommand] private void OpenCoreIsolation() => Run("Core Isolation", WindowsActions.OpenCoreIsolation);
    [RelayCommand] private Task RunAutoruns() => RunAsync("Autoruns", WindowsActions.AutorunsCheck);
    [RelayCommand] private void RunCleanup() => Run("Cleanup", WindowsActions.Cleanup);
    [RelayCommand] private Task RunRestorePoint() => RunAsync("Restore Point", WindowsActions.CreateRestorePoint);

    [RelayCommand] private Task RemoveBloatware() => RunAsync("Bloatware", WindowsActions.RemoveAllBloatware,
        "Removal takes a while  the page stays locked until it finishes.");
    [RelayCommand] private void CheckBloatware() => Run("Installed Apps", WindowsActions.CheckInstalledApps);
    [RelayCommand] private Task InstallStore() => RunAsync("Store", WindowsActions.InstallStore);
    [RelayCommand] private Task InstallWinget() => RunAsync("winget", WindowsActions.InstallWinget);
    [RelayCommand] private Task InstallAllUwp() => RunAsync("UWP Apps", WindowsActions.InstallAllUwpApps);
    [RelayCommand] private void OpenUwpFeatures() => Run("UWP Features", WindowsActions.OpenUwpFeatures);
    [RelayCommand] private void OpenLegacyFeatures() => Run("Legacy Features", WindowsActions.OpenLegacyFeatures);
    [RelayCommand] private Task InstallOneDrive() => RunAsync("OneDrive", WindowsActions.InstallOneDrive);
    [RelayCommand] private Task InstallRdc() => RunAsync("Remote Desktop", WindowsActions.InstallRemoteDesktop);
    [RelayCommand] private Task InstallSnipping() => RunAsync("Snipping Tool", WindowsActions.InstallSnippingTool);

    [RelayCommand] private void CheckLegacyApps() => Run("Legacy Apps", WindowsActions.OpenLegacyAppsCheck);
    [RelayCommand] private void CheckLegacyFeatures() => Run("Legacy Features", WindowsActions.OpenLegacyFeaturesCheck);
    [RelayCommand] private void CheckUwpApps() => Run("UWP Apps", WindowsActions.OpenUwpAppsCheck);
    [RelayCommand] private void CheckUwpFeatures() => Run("UWP Features", WindowsActions.OpenUwpFeatures);
    [RelayCommand] private void CheckTaskManager() => Run("Task Manager", WindowsActions.OpenTaskManager);

    private void Run(string title, Action work)
    {
        try
        {
            work();
        }
        catch (Exception ex)
        {
            _infoBar.Show($"{title}  failed", ex.Message);
        }
    }

    private async Task RunAsync(string title, Action work, string? note = null)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{title}...");
        try
        {
            await Task.Run(work);
            _infoBar.Show($"{title}  done", note ?? "Restart to apply.");
        }
        catch (Exception ex)
        {
            _infoBar.Show($"{title}  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }
}
