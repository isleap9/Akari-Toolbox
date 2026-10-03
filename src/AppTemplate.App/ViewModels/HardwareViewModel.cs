using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// 7  Hardware page: native ports of Ultimate "7 Hardware"  scaling combobox,
/// polling-cap toggle, polling tests, controller overclock and guide links.
/// </summary>
public partial class HardwareViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private readonly IDialogService _dialogs;
    private bool _loading;

    [ObservableProperty]
    private int _scalingIndex;

    [ObservableProperty]
    private bool _pollingCapOff;

    [ObservableProperty]
    private bool _isBusy;

    public string[] ScalingOptions => HardwareActions.ScalingOptions;

    public HardwareViewModel(IInfoBarService infoBar, IStatusService status, IDialogService dialogs)
    {
        _infoBar = infoBar;
        _status = status;
        _dialogs = dialogs;
    }

    public void OnNavigatedTo(object? parameter)
    {
        _loading = true;
        try
        {
            ScalingIndex = SafeIndex(HardwareActions.ReadScaling);
            PollingCapOff = Safe(HardwareActions.ReadPollingCapOff);
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
        try { return Math.Clamp(read(), 0, HardwareActions.ScalingOptions.Length - 1); }
        catch { return 0; }
    }

    partial void OnScalingIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= HardwareActions.ScalingOptions.Length)
            return;
        ApplyOption("Scaling", value,
            () => HardwareActions.ApplyScaling(value),
            HardwareActions.ReadScaling,
            v => ScalingIndex = v);
    }

    partial void OnPollingCapOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Polling Cap", value,
            off =>
            {
                if (off) HardwareActions.SetPollingCapOff();
                else HardwareActions.SetPollingCapDefault();
            },
            HardwareActions.ReadPollingCapOff,
            v => PollingCapOff = v);
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

    private async void ApplyOption(
        string title, int target,
        Action apply, Func<int> read,
        Action<int> rollback)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{title}...");
        try
        {
            await Task.Run(apply);
            _infoBar.Show($"{title} {HardwareActions.ScalingOptions[target]}  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { rollback(SafeIndex(read)); }
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
    private async Task OpenMonitor()
    {
        try
        {
            HardwareActions.OpenMonitorOptimization();
            await _dialogs.ShowInfoAsync(
                "Monitor Optimizations",
                "Monitor optimizations:\n- Enable overclock mode\n- Run highest refresh rate\n" +
                "- Disable adaptive brightness and variable back light\n- Turn off variable refresh rate, adaptive sync and g-sync\n" +
                "- Adjust color, brightness and sharpening to your preference\n- Max overdrive without causing overshoot or reducing motion clarity");
        }
        catch (Exception ex)
        {
            _infoBar.Show("Monitor  failed", ex.Message);
        }
    }

    [RelayCommand]
    private void OpenMouseTest() => Run("Mouse Test", HardwareActions.OpenMousePollingTest);

    [RelayCommand]
    private Task RunControllerTest() => RunAsync("Controller Test", HardwareActions.InstallControllerPollingTest);

    [RelayCommand]
    private Task RunControllerOverclock() => RunAsync("Controller Overclock", HardwareActions.InstallControllerOverclock);

    [RelayCommand]
    private void OpenBufferbloat() => Run("Bufferbloat", HardwareActions.OpenBufferbloatTest);

    [RelayCommand]
    private void OpenBuildGuide() => Run("Build Guide", HardwareActions.OpenPcBuildGuide);

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

    private async Task RunAsync(string title, Action work)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{title}...");
        try
        {
            await Task.Run(work);
            _infoBar.Show($"{title}  done", "Restart to apply.");
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
