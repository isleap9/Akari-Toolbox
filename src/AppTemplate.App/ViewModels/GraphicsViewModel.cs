using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// 5  Graphics page: the full Ultimate "5 Graphics" category. GPU-setting
/// toggles reuse <see cref="GraphicsActions"/>, driver flows run through
/// <see cref="DriverActions"/> (DDU reboots into safe mode and back).
/// </summary>
public partial class GraphicsViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private bool _loading;

    [ObservableProperty]
    private bool _nvidiaSettings;

    [ObservableProperty]
    private bool _amdSettings;

    [ObservableProperty]
    private bool _intelSettings;

    [ObservableProperty]
    private bool _disableHdcp;

    [ObservableProperty]
    private bool _p0State;

    [ObservableProperty]
    private bool _msiMode;

    [ObservableProperty]
    private bool _isBusy;

    public GraphicsViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
    }

    public void OnNavigatedTo(object? parameter)
    {
        _loading = true;
        try
        {
            NvidiaSettings = Safe(GraphicsActions.ReadNvidiaSettings);
            AmdSettings = Safe(GraphicsActions.ReadAmdSettings);
            IntelSettings = Safe(GraphicsActions.ReadIntelSettings);
            DisableHdcp = Safe(GraphicsActions.ReadHdcp);
            P0State = Safe(GraphicsActions.ReadP0State);
            MsiMode = Safe(GraphicsActions.ReadMsiMode);
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

    partial void OnNvidiaSettingsChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("NVIDIA Settings", value,
            GraphicsActions.NvidiaSettings, GraphicsActions.ReadNvidiaSettings,
            v => NvidiaSettings = v);
    }

    partial void OnAmdSettingsChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("AMD Settings", value,
            GraphicsActions.AmdSettings, GraphicsActions.ReadAmdSettings,
            v => AmdSettings = v);
    }

    partial void OnIntelSettingsChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Intel Settings", value,
            GraphicsActions.IntelSettings, GraphicsActions.ReadIntelSettings,
            v => IntelSettings = v);
    }

    partial void OnDisableHdcpChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("HDCP", value,
            hdcpOff => GraphicsActions.Hdcp(off: hdcpOff),
            GraphicsActions.ReadHdcp,
            v => DisableHdcp = v);
    }

    partial void OnP0StateChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("P0 State", value,
            GraphicsActions.P0State, GraphicsActions.ReadP0State,
            v => P0State = v);
    }

    partial void OnMsiModeChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("MSI Mode", value,
            GraphicsActions.MsiMode, GraphicsActions.ReadMsiMode,
            v => MsiMode = v);
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

    [RelayCommand]
    private Task DduAuto() => RunAsync("DDU Auto", () => DriverActions.PrepareDdu(auto: true),
        "Rebooting into safe mode  DDU runs automatically, then reboots back.");

    [RelayCommand]
    private Task DduManual() => RunAsync("DDU Manual", () => DriverActions.PrepareDdu(auto: false),
        "Rebooting into safe mode  run DDU yourself, then reboot back.");

    [RelayCommand]
    private Task InstallNvidiaDriver() => RunAsync("NVIDIA Driver",
        () => DriverActions.InstallNvidiaDriver(debloat: false));

    [RelayCommand]
    private Task InstallAmdDriver() => RunAsync("AMD Driver", DriverActions.InstallAmdDriver);

    [RelayCommand]
    private void InstallIntelDriver()
    {
        DriverActions.OpenIntelDriverSearch();
        string? picked = DriverActions.PickIntelDriver();
        if (string.IsNullOrEmpty(picked))
            return;
        _ = RunAsync("Intel Driver", () => DriverActions.InstallIntelDriver(picked));
    }

    [RelayCommand]
    private Task InstallNvidiaDriverAndSettings() => RunAsync("NVIDIA Driver + Settings",
        () =>
        {
            DriverActions.InstallNvidiaDriver(debloat: false);
            GraphicsActions.NvidiaSettings(true);
        });

    [RelayCommand]
    private Task InstallAmdDriverAndSettings() => RunAsync("AMD Driver + Settings",
        () =>
        {
            DriverActions.InstallAmdDriver();
            GraphicsActions.AmdSettings(true);
        });

    [RelayCommand]
    private void InstallIntelDriverAndSettings()
    {
        DriverActions.OpenIntelDriverSearch();
        string? picked = DriverActions.PickIntelDriver();
        if (string.IsNullOrEmpty(picked))
            return;
        _ = RunAsync("Intel Driver + Settings", () =>
        {
            DriverActions.InstallIntelDriver(picked);
            GraphicsActions.IntelSettings(true);
        });
    }

    [RelayCommand]
    private Task InstallNvidiaDebloatAndSettings() => RunAsync("NVIDIA Debloat + Settings",
        () =>
        {
            DriverActions.InstallNvidiaDriver(debloat: true);
            GraphicsActions.NvidiaSettings(true);
        });

    [RelayCommand]
    private Task InstallDirectX() => RunAsync("DirectX", GraphicsActions.InstallDirectX);

    [RelayCommand]
    private Task InstallCpp() => RunAsync("C++ Runtimes", GraphicsActions.InstallCppRuntimes);

    [RelayCommand]
    private void OpenResolution() => Run("Resolution", GraphicsActions.OpenResolutionRefreshRate);

    [RelayCommand]
    private void OpenHags() => Run("HAGS", GraphicsActions.OpenHagsWindowed);

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
