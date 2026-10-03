using System.Collections.ObjectModel;
using AppTemplate.Framework.Services;
using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// Gaming Tweaks page  the old app's page ported to the DI/async shell. Two live-state
/// toggles (preemption, network) plus the Ultimate "5 Graphics" NVIDIA / AMD toggles,
/// the Services combobox, and the Tools button grid. Every
/// former .bat action runs natively (see <see cref="GamingActions"/>), the Ultimate
/// "5 Graphics" scripts are ported in <see cref="GraphicsActions"/>, and every former
/// .exe resolves under C:\PostInstall and reports the expected path when the user has
/// not yet supplied their own copy.
/// </summary>
public partial class GamingViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private bool _loading;

    [ObservableProperty]
    private bool _disablePreemption;

    [ObservableProperty]
    private bool _networkOptimization;

    //  Ultimate "5 Graphics" NVIDIA toggles (scripts 5, 8, 9, 10) 

    [ObservableProperty]
    private bool _nvidiaSettings;

    [ObservableProperty]
    private bool _disableHdcp;

    [ObservableProperty]
    private bool _p0State;

    [ObservableProperty]
    private bool _msiMode;

    //  Ultimate "5 Graphics" AMD toggle (script 6) 

    [ObservableProperty]
    private bool _amdSettings;

    /// <summary>"AkariOS Default" / "Windows Default"  the old Companion's Services
    /// combobox, now driven by the native Control Panel Settings port (Ultimate
    /// "6 Windows" script 22). Lives on this page, where the old section was.</summary>
    public string[] ControlPanelOptions => ControlPanelActions.Options;

    [ObservableProperty]
    private int _controlPanelIndex;

    /// <summary>True while an apply is in flight; the page disables its controls.</summary>
    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<GamingButton> ToolButtons { get; } = [];

    /// <summary>Mirrors the page-wide IsBusy onto every tool tile, matching the WPF
    /// page-level IsEnabled binding that the DataTemplate cannot reach.</summary>
    partial void OnIsBusyChanged(bool value)
    {
        foreach (GamingButton button in ToolButtons)
            button.IsBusy = value;
    }

    public GamingViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
        BuildButtons();
    }

    // 
    // Navigation  read live state on every visit
    // 

    public void OnNavigatedTo(object? parameter)
    {
        _loading = true;
        try
        {
            DisablePreemption = Safe(GamingActions.ReadPreemption);
            NetworkOptimization = Safe(GamingActions.ReadNetworkOptimization);

            NvidiaSettings = Safe(GraphicsActions.ReadNvidiaSettings);
            DisableHdcp = Safe(GraphicsActions.ReadHdcp);
            P0State = Safe(GraphicsActions.ReadP0State);
            MsiMode = Safe(GraphicsActions.ReadMsiMode);

            AmdSettings = Safe(GraphicsActions.ReadAmdSettings);

            ControlPanelIndex = ReadControlPanelIndex();
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Live Control Panel preset  combobox index. Mixed (or unreadable) state
    /// falls back to the last applied choice so the box always shows something valid.</summary>
    private static int ReadControlPanelIndex()
    {
        try
        {
            return ControlPanelActions.ReadPreset() switch
            {
                ControlPanelPreset.AkariDefault => 0,
                ControlPanelPreset.WindowsDefault => 1,
                _ => AkariToolState.GetInt("ControlPanelPreset", 0),
            };
        }
        catch
        {
            return AkariToolState.GetInt("ControlPanelPreset", 0);
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

    // 
    // Toggles
    // 

    partial void OnDisablePreemptionChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Disable Preemption (NVIDIA)", value,
            GamingActions.SetPreemption, GamingActions.ReadPreemption,
            v => DisablePreemption = v, "Restart to apply.");
    }

    partial void OnP0StateChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("P0 State", value,
            GraphicsActions.P0State, GraphicsActions.ReadP0State,
            v => P0State = v, "Restart to apply.");
    }

    partial void OnNvidiaSettingsChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("NVIDIA Settings", value,
            GraphicsActions.NvidiaSettings, GraphicsActions.ReadNvidiaSettings,
            v => NvidiaSettings = v, "Restart to apply.");
    }

    partial void OnDisableHdcpChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("HDCP", value,
            hdcpOff => GraphicsActions.Hdcp(off: hdcpOff),
            GraphicsActions.ReadHdcp,
            v => DisableHdcp = v, "Restart to apply.");
    }

    partial void OnMsiModeChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("MSI Mode", value,
            GraphicsActions.MsiMode, GraphicsActions.ReadMsiMode,
            v => MsiMode = v, "Restart to apply.");
    }

    partial void OnAmdSettingsChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("AMD Settings", value,
            GraphicsActions.AmdSettings, GraphicsActions.ReadAmdSettings,
            v => AmdSettings = v, "Restart to apply.");
    }

    partial void OnNetworkOptimizationChanged(bool value)
    {
        if (_loading) return;
        ApplyToggle("Network Optimization", value,
            enable =>
            {
                if (enable) GamingActions.ApplyNetworkOptimization();
                else GamingActions.RevertNetworkOptimization();
            },
            GamingActions.ReadNetworkOptimization,
            v => NetworkOptimization = v, "A reboot may be required.");
    }

    private async void ApplyToggle(
        string title, bool target,
        Action<bool> apply, Func<bool> read,
        Action<bool> rollback, string note)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{title}...");
        try
        {
            await Task.Run(() => apply(target));
            _infoBar.Show($"{title}  applied", note);
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

    // 
    // Dropdowns
    // 

    partial void OnControlPanelIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= ControlPanelActions.Options.Length)
            return;
        _ = ApplyControlPanelAsync(value);
    }

    /// <summary>Long apply (reg imports, TrustedInstaller, hive load)  runs on a
    /// background thread with the page locked, like the toggles.</summary>
    private async Task ApplyControlPanelAsync(int index)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{ControlPanelActions.Options[index]}...");
        try
        {
            await Task.Run(() =>
            {
                if (index == 0)
                    ControlPanelActions.ApplyAkariDefault();
                else
                    ControlPanelActions.ApplyWindowsDefault();
            });
            AkariToolState.SetInt("ControlPanelPreset", index);
            _infoBar.Show($"{ControlPanelActions.Options[index]}  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            // Roll the box back so it never lies about the real state.
            _loading = true;
            try { ControlPanelIndex = ReadControlPanelIndex(); }
            finally { _loading = false; }

            _infoBar.Show($"{ControlPanelActions.Options[index]}  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    // 
    // Button grids
    // 

    private void BuildButtons()
    {
        //  Useful Tools 
        ToolButtons.Add(Tool("Autoruns", @"Tweaks\Autoruns.exe"));
        ToolButtons.Add(Tool("Devmanview", @"Tweaks\DevManView.exe"));
        ToolButtons.Add(Tool("Serviwin", @"Tweaks\serviwin.exe"));
        ToolButtons.Add(Tool("InSpectre", @"Mitigations\InSpectre.exe"));
        ToolButtons.Add(Tool("MouseTester", @"Tweaks\Mouse Polling Test\MouseTester.exe"));
        ToolButtons.Add(Tool("MinSudo", @"Tweaks\MinSudo.exe"));
        ToolButtons.Add(Tool("CRU", @"Tweaks\CRU\CRU.exe"));
        ToolButtons.Add(Action("AUTO DSCP", RunAutoDscp));
        ToolButtons.Add(Tool("MSI Util", @"Tweaks\MSI Mode Utility.exe"));
        ToolButtons.Add(Tool("DISM++", @"Tweaks\Dism++x64.exe"));
        ToolButtons.Add(Tool("Dev. Cleanup", @"Tweaks\DeviceCleanup.exe"));
        ToolButtons.Add(Tool("Interrupt AFPT", @"Tweaks\Interrupt Affinity Policy Tool.exe"));
        ToolButtons.Add(Tool("HIDUSB", @"Tweaks\hidusbf\DRIVER\Setup.exe"));
        ToolButtons.Add(Tool("MeasureSleep", @"Tweaks\MeasureSleep.exe"));
        ToolButtons.Add(Tool("Process Explorer", @"Tweaks\Process explorer\Process Explorer.exe"));
        ToolButtons.Add(Tool("ReservedCPUSets", @"Tweaks\ReservedCpuSets.exe"));

        // Ultimate "5 Graphics" ports (GraphicsActions): scripts 11, 13 and 14.
        ToolButtons.Add(Action("DirectX", GraphicsActions.InstallDirectX));
        ToolButtons.Add(Action("Resolution Refresh Rate", GraphicsActions.OpenResolutionRefreshRate));
        ToolButtons.Add(Action("HAGS Windowed", GraphicsActions.OpenHagsWindowed));
    }

    /// <summary>A native action button: runs off-thread, snackbars success/failure.</summary>
    private GamingButton Action(string label, Action run, string? note = null) =>
        new(label, async () =>
        {
            _status.Start($"{label}...");
            try
            {
                await Task.Run(run);
                _infoBar.Show(note is null ? $"{label}  done" : $"{label}  applied",
                    note ?? "Done.");
            }
            catch (Exception ex)
            {
                _infoBar.Show($"{label}  failed", ex.Message);
            }
            finally
            {
                _status.Complete();
            }
        });

    /// <summary>
    /// A tool button: launches a user-supplied file under C:\PostInstall when present,
    /// otherwise snackbars the expected path (files are provided by the user, not us).
    /// </summary>
    private GamingButton Tool(string label, string relativePath) =>
        new(label, () =>
        {
            string? error = null; _infoBar.Show("Launcher", $"Would launch {relativePath}");
            if (error is not null)
                _infoBar.Show($"{label}  not available", error);
            return Task.CompletedTask;
        });

    private void RunAutoDscp()
    {
        // Interactive (prompts + dialogs), so keep it on the UI thread.
        try
        {
            bool applied = GamingActions.AutoDscpFse();
            if (applied)
                _infoBar.Show("AUTO DSCP & FSE  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _infoBar.Show("AUTO DSCP & FSE  failed", ex.Message);
        }
    }
}

/// <summary>
/// One button in the Tools grid. WPF reached the page's RunButtonCommand through
/// RelativeSource AncestorType=Page; WinUI DataTemplates have no equivalent, so the command
/// lives on the tile and delegates to the same Run path.
/// </summary>
public sealed partial class GamingButton : ObservableObject
{
    public GamingButton(string label, Func<Task> run)
    {
        Label = label;
        Run = run;
    }

    public string Label { get; }

    internal Func<Task> Run { get; }

    /// <summary>True while another action holds the page lock.</summary>
    [ObservableProperty]
    private bool _isBusy;

    public bool IsEnabled => !IsBusy;

    [RelayCommand]
    private Task RunTool() => Run();

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));
}
