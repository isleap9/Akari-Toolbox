using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// 2  Refresh page: native ports of Ultimate "2 Refresh" (recovery, local
/// account, reinstall media, unattend generator, driver-updates toggle,
/// network-driver search, restart-to-BIOS).
/// </summary>
public partial class RefreshViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private bool _loading;

    [ObservableProperty]
    private bool _driverUpdatesBlocked;

    [ObservableProperty]
    private bool _isBusy;

    public RefreshViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
    }

    public void OnNavigatedTo(object? parameter)
    {
        _loading = true;
        try
        {
            DriverUpdatesBlocked = Safe(RefreshActions.ReadDriverUpdatesBlocked);
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

    partial void OnDriverUpdatesBlockedChanged(bool value)
    {
        if (_loading)
            return;
        _ = ApplyDriverBlockAsync(value);
    }

    private async Task ApplyDriverBlockAsync(bool block)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start(block ? "Blocking driver updates..." : "Unblocking driver updates...");
        try
        {
            await Task.Run(() =>
            {
                if (block)
                    RefreshActions.BlockDriverUpdates();
                else
                    RefreshActions.UnblockDriverUpdates();
            });
            _infoBar.Show(block ? "Driver updates  blocked" : "Driver updates  unblocked",
                "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { DriverUpdatesBlocked = Safe(RefreshActions.ReadDriverUpdatesBlocked); }
            finally { _loading = false; }
            _infoBar.Show("Driver updates  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    [RelayCommand]
    private void OpenRecovery() => Run("Recovery", RefreshActions.OpenRecovery);

    [RelayCommand]
    private void OpenLocalAccount() => Run("Local Account", RefreshActions.OpenLocalUsers);

    [RelayCommand]
    private void OpenAutounattend() => Run("Autounattend", RefreshActions.OpenUnattendGenerator);

    [RelayCommand]
    private Task ReinstallW10() => RunAsync("Reinstall W10", RefreshActions.InstallAndRunMct10);

    [RelayCommand]
    private Task ReinstallW11() => RunAsync("Reinstall W11", RefreshActions.InstallAndRunMct11);

    [RelayCommand]
    private void OpenNetworkDriver() => Run("Network Driver", RefreshActions.SearchNetworkDriver);

    [RelayCommand]
    private void RestartToBios() => Run("Restart to BIOS", RefreshActions.RestartToBios);

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
            _infoBar.Show($"{title}  launched", "Follow the tool's own steps.");
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
