using System.Collections.ObjectModel;
using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// 8  Advanced page: native ports of Ultimate "8 Advanced"  security toggles,
/// service/memory toggles, rendering comboboxes/toggles and per-app launchers.
/// </summary>
public partial class AdvancedViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private bool _loading;

    [ObservableProperty] private bool _defenderDisabled;
    [ObservableProperty] private bool _firewallDisabled;
    [ObservableProperty] private bool _spectreDisabled;
    [ObservableProperty] private bool _depDisabled;
    [ObservableProperty] private bool _downloadWarningDisabled;
    [ObservableProperty] private bool _mmAgentOff;
    [ObservableProperty] private int _reBarIndex;
    [ObservableProperty] private bool _mpoOff;
    [ObservableProperty] private int _flipIndex;
    [ObservableProperty] private int _composedFlipIndex;
    [ObservableProperty] private bool _ulpsOff;
    [ObservableProperty] private bool _whqlBypass;
    [ObservableProperty] private bool _keyboardOff;
    [ObservableProperty] private bool _servicesOff;
    [ObservableProperty] private bool _shellSearchOff;
    [ObservableProperty] private bool _nvmeFaster;
    [ObservableProperty] private bool _isBusy;

    public string[] ReBarOptions => AdvancedActions.ReBarOptions;
    public string[] FlipOptions => AdvancedActions.FlipOptions;
    public string[] ComposedFlipOptions => AdvancedActions.ComposedFlipOptions;

    public string[] PriorityOptions { get; } =
        { "Real Time", "High", "Above Normal", "Normal", "Below Normal", "Low" };

    private static readonly string[] PriorityArgs =
        { "realtime", "high", "abovenormal", "normal", "belownormal", "low" };

    /// <summary>
    /// Priority Launcher tiles. WPF bound the page's LaunchPriorityCommand through
    /// RelativeSource AncestorType=Page; WinUI DataTemplates have no equivalent, so the command
    /// lives on the tile and delegates to the same LaunchPriority path.
    /// </summary>
    public ObservableCollection<PriorityButton> PriorityButtons { get; } = [];

    public AdvancedViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
        foreach (string label in PriorityOptions)
            PriorityButtons.Add(new PriorityButton(label, LaunchPriority));
    }

    /// <summary>Mirrors the page-wide IsBusy onto the launcher tiles, matching the WPF
    /// template's page-level IsEnabled binding.</summary>
    partial void OnIsBusyChanged(bool value)
    {
        foreach (PriorityButton button in PriorityButtons)
            button.IsBusy = value;
    }

    public void OnNavigatedTo(object? parameter)
    {
        _loading = true;
        try
        {
            DefenderDisabled = Safe(AdvancedActions.ReadDefenderDisabled);
            FirewallDisabled = Safe(AdvancedActions.ReadFirewallDisabled);
            SpectreDisabled = Safe(AdvancedActions.ReadSpectreDisabled);
            DepDisabled = Safe(AdvancedActions.ReadDepDisabled);
            DownloadWarningDisabled = Safe(AdvancedActions.ReadDownloadWarningDisabled);
            MmAgentOff = Safe(AdvancedActions.ReadMmAgentOff);
            ReBarIndex = AkariToolState.GetInt("ReBar", 0);
            MpoOff = Safe(AdvancedActions.ReadMpoOff);
            FlipIndex = SafeIndex(AdvancedActions.ReadFlipMode, AdvancedActions.FlipOptions.Length);
            ComposedFlipIndex = SafeIndex(AdvancedActions.ReadComposedFlip, AdvancedActions.ComposedFlipOptions.Length);
            UlpsOff = Safe(AdvancedActions.ReadUlpsOff);
            WhqlBypass = Safe(AdvancedActions.ReadWhqlBypass);
            KeyboardOff = Safe(AdvancedActions.ReadKeyboardOff);
            ServicesOff = SafeServices();
            ShellSearchOff = Safe(AdvancedActions.ReadShellSearchOff);
            NvmeFaster = Safe(AdvancedActions.ReadNvmeFaster);
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

    private static int SafeIndex(Func<int> read, int length)
    {
        try { return Math.Clamp(read(), 0, length - 1); }
        catch { return 0; }
    }

    private static bool SafeServices()
    {
        try
        {
            return AdvancedActions.ReadServicesOff() ??
                AkariToolState.GetInt("ServicesPreset", 0) == 0;
        }
        catch
        {
            return AkariToolState.GetInt("ServicesPreset", 0) == 0;
        }
    }

    partial void OnDefenderDisabledChanged(bool value)
    {
        if (_loading) return;
        _ = ApplySafeBootAsync("Defender", value,
            () =>
            {
                if (value) AdvancedActions.ApplyDefenderDisable();
                else AdvancedActions.ApplyDefenderEnable();
            },
            () => AdvancedActions.ReadDefenderDisabled() == value,
            v => DefenderDisabled = v);
    }

    partial void OnFirewallDisabledChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Firewall", value,
            disabled =>
            {
                if (disabled) AdvancedActions.SetFirewallDisabled();
                else AdvancedActions.SetFirewallEnabled();
            },
            AdvancedActions.ReadFirewallDisabled, v => FirewallDisabled = v);
    }

    partial void OnSpectreDisabledChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Spectre / Meltdown", value,
            disabled =>
            {
                if (disabled) AdvancedActions.SetSpectreDisabled();
                else AdvancedActions.SetSpectreEnabled();
            },
            AdvancedActions.ReadSpectreDisabled, v => SpectreDisabled = v);
    }

    partial void OnDepDisabledChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("DEP", value,
            disabled =>
            {
                if (disabled) AdvancedActions.SetDepDisabled();
                else AdvancedActions.SetDepEnabled();
            },
            AdvancedActions.ReadDepDisabled, v => DepDisabled = v);
    }

    partial void OnDownloadWarningDisabledChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Download Warning", value,
            disabled =>
            {
                if (disabled) AdvancedActions.SetDownloadWarningDisabled();
                else AdvancedActions.SetDownloadWarningEnabled();
            },
            AdvancedActions.ReadDownloadWarningDisabled, v => DownloadWarningDisabled = v);
    }

    partial void OnMmAgentOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("MMAgent", value,
            off =>
            {
                if (off) AdvancedActions.MmAgentOff();
                else AdvancedActions.MmAgentDefault();
            },
            AdvancedActions.ReadMmAgentOff, v => MmAgentOff = v);
    }

    partial void OnReBarIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= AdvancedActions.ReBarOptions.Length)
            return;
        _ = ApplyReBarAsync(value);
    }

    private async Task ApplyReBarAsync(int index)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{AdvancedActions.ReBarOptions[index]}...");
        try
        {
            await Task.Run(() => AdvancedActions.ApplyReBar(index));
            AkariToolState.SetInt("ReBar", index);
            _infoBar.Show($"{AdvancedActions.ReBarOptions[index]}  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { ReBarIndex = AkariToolState.GetInt("ReBar", 0); }
            finally { _loading = false; }
            _infoBar.Show("ReBar  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    partial void OnMpoOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("MPO", value,
            off =>
            {
                if (off) AdvancedActions.MpoOff();
                else AdvancedActions.MpoOn();
            },
            AdvancedActions.ReadMpoOff, v => MpoOff = v);
    }

    partial void OnFlipIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= AdvancedActions.FlipOptions.Length)
            return;
        ApplyOption("Fullscreen Mode", value,
            () =>
            {
                if (value == 1) AdvancedActions.ApplyFlipFse();
                else AdvancedActions.ApplyFlipFso();
            },
            () => AdvancedActions.ReadFlipMode(),
            v => FlipIndex = v);
    }

    partial void OnComposedFlipIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= AdvancedActions.ComposedFlipOptions.Length)
            return;
        ApplyOption("Composed Flip", value,
            () => AdvancedActions.ApplyComposedFlip(value == 1),
            () => AdvancedActions.ReadComposedFlip(),
            v => ComposedFlipIndex = v);
    }

    partial void OnUlpsOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("ULPS", value,
            off => AdvancedActions.SetUlps(off),
            AdvancedActions.ReadUlpsOff, v => UlpsOff = v);
    }

    partial void OnWhqlBypassChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("WHQL Bypass", value,
            on => AdvancedActions.SetWhqlBypass(on),
            AdvancedActions.ReadWhqlBypass, v => WhqlBypass = v);
    }

    partial void OnKeyboardOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Keyboard Shortcuts", value,
            off =>
            {
                if (off) AdvancedActions.SetKeyboardShortcutsOff();
                else AdvancedActions.SetKeyboardShortcutsDefault();
            },
            AdvancedActions.ReadKeyboardOff, v => KeyboardOff = v);
    }

    partial void OnServicesOffChanged(bool value)
    {
        if (_loading) return;
        _ = ApplySafeBootAsync("Services", value,
            () =>
            {
                if (value) AdvancedActions.ApplyServicesOff();
                else AdvancedActions.ApplyServicesDefault();
                AkariToolState.SetInt("ServicesPreset", value ? 0 : 1);
            },
            () => (AdvancedActions.ReadServicesOff() ?? value) == value,
            v => ServicesOff = v);
    }

    partial void OnShellSearchOffChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Search & Shell", value,
            off =>
            {
                if (off) AdvancedActions.ShellSearchOff();
                else AdvancedActions.ShellSearchDefault();
            },
            AdvancedActions.ReadShellSearchOff, v => ShellSearchOff = v);
    }

    partial void OnNvmeFasterChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("NVMe Driver", value,
            faster =>
            {
                if (faster) AdvancedActions.NvmeFasterDriver();
                else AdvancedActions.NvmeDefaultDriver();
            },
            AdvancedActions.ReadNvmeFaster, v => NvmeFaster = v);
    }

    /// <summary>Safe-boot flows reboot the machine  no infobar, the app exits.</summary>
    private async Task ApplySafeBootAsync(
        string title, bool target, Action arm, Func<bool> verify, Action<bool> rollback)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{title}...");
        try
        {
            await Task.Run(arm);
        }
        catch (Exception ex)
        {
            _loading = true;
            try { rollback(Safe(verify)); }
            finally { _loading = false; }
            _infoBar.Show($"{title}  failed", ex.Message);
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
            _infoBar.Show($"{title}  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { rollback(SafeIndex(read, 10)); }
            finally { _loading = false; }
            _infoBar.Show($"{title}  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    //  Per-app launchers 

    [RelayCommand]
    private void LaunchSmtHtOff() => LaunchPicked("SMT / HT Off", AdvancedActions.LaunchSmtHtOff);

    [RelayCommand]
    private void LaunchCore1Thread1Off() => LaunchPicked("Core 1 & Thread 1 Off", AdvancedActions.LaunchCore1Thread1Off);

    /// <summary>Launches the picked app at the priority this tile names.</summary>
    private void LaunchPriority(string label)
    {
        int i = Array.IndexOf(PriorityOptions, label);
        if (i < 0)
            i = 1;
        string priority = PriorityArgs[i];
        LaunchPicked($"Priority {PriorityOptions[i]}",
            file => AdvancedActions.LaunchWithPriority(file, priority));
    }

    private void LaunchPicked(string title, Action<string> launch)
    {
        try
        {
            string? file = AdvancedActions.PickApp();
            if (string.IsNullOrEmpty(file))
                return;
            launch(file);
            _infoBar.Show($"{title}  launched", file);
        }
        catch (Exception ex)
        {
            _infoBar.Show($"{title}  failed", ex.Message);
        }
    }

    [RelayCommand]
    private void RestartToBios() => Run("Restart to BIOS", NativeOps.RestartToBios);

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
}

/// <summary>One tile in the Priority Launcher grid.</summary>
public sealed partial class PriorityButton : ObservableObject
{
    private readonly Action<string> _launch;

    public PriorityButton(string label, Action<string> launch)
    {
        Label = label;
        _launch = launch;
    }

    public string Label { get; }

    /// <summary>True while another action holds the page lock.</summary>
    [ObservableProperty]
    private bool _isBusy;

    public bool IsEnabled => !IsBusy;

    [RelayCommand]
    private void Launch() => _launch(Label);

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));
}
