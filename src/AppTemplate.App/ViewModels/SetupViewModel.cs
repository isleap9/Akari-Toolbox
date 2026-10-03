using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// 3  Setup page: native ports of Ultimate "3 Setup"  BitLocker, Memory
/// Compression (combobox), Background Apps, Edge and Store toggles, plus the
/// settings/licensing opener buttons.
/// </summary>
public partial class SetupViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private readonly IDialogService _dialogs;
    private bool _loading;

    [ObservableProperty]
    private bool _bitLockerOff;

    [ObservableProperty]
    private int _memCompIndex;

    [ObservableProperty]
    private bool _backgroundAppsOff;

    [ObservableProperty]
    private bool _edgeOptimized;

    [ObservableProperty]
    private bool _storeOptimized;

    [ObservableProperty]
    private bool _isBusy;

    public string[] MemCompOptions => SetupActions.MemCompOptions;

    public SetupViewModel(IInfoBarService infoBar, IStatusService status, IDialogService dialogs)
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
            BitLockerOff = Safe(() => !SetupActions.ReadBitLockerOn());
            MemCompIndex = Safe(SetupActions.ReadMemCompEnabled) ? 1 : 0;
            BackgroundAppsOff = Safe(SetupActions.ReadBackgroundAppsOff);
            EdgeOptimized = Safe(SetupActions.ReadEdgeOptimized);
            StoreOptimized = Safe(SetupActions.ReadStoreOptimized);
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

    partial void OnBitLockerOffChanged(bool value)
    {
        if (_loading)
            return;
        ApplyToggle("BitLocker", value,
            off =>
            {
                if (off)
                    SetupActions.SetBitLockerOff();
                else
                    SetupActions.OpenBitLockerPanel();
            },
            () => !SetupActions.ReadBitLockerOn(),
            v => BitLockerOff = v);
    }

    partial void OnMemCompIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= SetupActions.MemCompOptions.Length)
            return;
        _ = ApplyMemCompAsync(value);
    }

    private async Task ApplyMemCompAsync(int index)
    {
        if (IsBusy)
            return;

        // "Check" is momentary: show the live state, then fall back to it.
        if (index == 2)
        {
            IsBusy = true;
        _status.Start("Checking memory compression...");
            try
            {
                string status = await Task.Run(() => SetupActions.ReadMemCompStatus());
                await _dialogs.ShowInfoAsync(
                    "Memory Compression",
                    string.IsNullOrWhiteSpace(status) ? "MemoryCompression state unavailable." : status.Trim());
            }
            catch (Exception ex)
            {
                _infoBar.Show("Memory Compression  failed", ex.Message);
            }
            finally
            {
                _loading = true;
                try { MemCompIndex = Safe(SetupActions.ReadMemCompEnabled) ? 1 : 0; }
                finally { _loading = false; }
                IsBusy = false;
            _status.Complete();
            }
            return;
        }

        IsBusy = true;
        _status.Start($"Memory Compression {SetupActions.MemCompOptions[index]}...");
        try
        {
            await Task.Run(() =>
            {
                if (index == 0)
                    SetupActions.SetMemCompOff();
                else
                    SetupActions.SetMemCompOn();
            });
            _infoBar.Show($"Memory Compression {SetupActions.MemCompOptions[index]}  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { MemCompIndex = Safe(SetupActions.ReadMemCompEnabled) ? 1 : 0; }
            finally { _loading = false; }
            _infoBar.Show("Memory Compression  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    partial void OnBackgroundAppsOffChanged(bool value)
    {
        if (_loading)
            return;
        ApplyToggle("Background Apps", value,
            off =>
            {
                if (off)
                    SetupActions.SetBackgroundAppsOff();
                else
                    SetupActions.SetBackgroundAppsDefault();
            },
            SetupActions.ReadBackgroundAppsOff,
            v => BackgroundAppsOff = v);
    }

    partial void OnEdgeOptimizedChanged(bool value)
    {
        if (_loading)
            return;
        _ = ApplyEdgeAsync(value);
    }

    private async Task ApplyEdgeAsync(bool optimize)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start(optimize ? "Optimizing Edge..." : "Restoring Edge...");
        try
        {
            await Task.Run(() =>
            {
                if (optimize)
                    SetupActions.OptimizeEdge();
                else
                    SetupActions.RestoreEdgeDefault();
            });
            _infoBar.Show(optimize ? "Edge  optimized" : "Edge  restored", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { EdgeOptimized = Safe(SetupActions.ReadEdgeOptimized); }
            finally { _loading = false; }
            _infoBar.Show("Edge  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    partial void OnStoreOptimizedChanged(bool value)
    {
        if (_loading)
            return;
        ApplyToggle("Store", value,
            optimize =>
            {
                if (optimize)
                    SetupActions.OptimizeStore();
                else
                    SetupActions.RestoreStoreDefault();
            },
            SetupActions.ReadStoreOptimized,
            v => StoreOptimized = v);
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
    private void OpenDateLanguage() => Run("Date & Language", SetupActions.OpenDateLanguage);

    [RelayCommand]
    private void OpenStartupApps() => Run("Startup Apps", SetupActions.OpenStartupApps);

    [RelayCommand]
    private void OpenStartupTaskManager() => Run("Startup Task Manager", SetupActions.OpenStartupTaskManager);

    [RelayCommand]
    private void PauseUpdates() => Run("Pause Updates", SetupActions.PauseUpdates);

    [RelayCommand]
    private void OpenKeys() => Run("Keys", SetupActions.OpenActivationKeys);

    [RelayCommand]
    private void OpenActivation() => Run("Activation", SetupActions.OpenActivation);

    [RelayCommand]
    private void ConvertToPro() => Run("Convert Home To Pro", SetupActions.ConvertHomeToPro);

    private void Run(string title, Action work)
    {
        try
        {
            work();
            _infoBar.Show($"{title}  done", "Follow the native dialog if one opened.");
        }
        catch (Exception ex)
        {
            _infoBar.Show($"{title}  failed", ex.Message);
        }
    }
}
