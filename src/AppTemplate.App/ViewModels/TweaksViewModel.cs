using System.Collections.ObjectModel;
using AkariToolbox.Tweaks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;

namespace AkariToolbox.ViewModels;

/// <summary>
/// Individual Tweaks page: the two scheduling comboboxes (moved off Gaming
/// Tweaks) plus the 169 granular Control Panel rows (Akari-Tool Render-CpTweaks
/// data, grouped Debloat / Appearance / Tweaks / System like the original).
/// Revertible rows are live-state toggles; one-way rows are Apply buttons.
/// </summary>
public partial class TweaksViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private bool _loading;
    private bool _busy;

    public string[] SvcHostOptions { get; } = { "Default (380000 KB)", "4 GB", "8 GB", "16 GB", "32 GB", "64 GB", "128 GB", "256 GB" };

    public string[] Win32PriorityOptions { get; } = { "26 (Hex) (Recommended)", "2A (Hex)", "28 (Hex)", "16 (Hex)", "06 (Hex)" };

    [ObservableProperty]
    private int _svcHostIndex;

    [ObservableProperty]
    private int _win32PriorityIndex;

    /// <summary>True while an apply is in flight; the page disables its controls.</summary>
    public bool IsBusy
    {
        get => _busy;
        private set => SetProperty(ref _busy, value);
    }

    public ObservableCollection<CpTabGroup> TabGroups { get; } = [];

    private static readonly string[] TabOrder = { "Debloat", "Appearance", "Tweaks", "System" };

    public TweaksViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
        Title = "Tweaks";
        BuildRowGroups();
    }

    public void OnNavigatedTo(object? parameter)
    {
        _loading = true;
        try
        {
            int? svcLive = SafeNullable(GamingActions.ReadSvcHost);
            SvcHostIndex = MatchIndex(svcLive is null ? -1 : svcLive.Value,
                GamingActions.SvcHostValues.Select(v => (long)v).ToArray(),
                AkariToolState.GetInt("SvcHostSplitThreshold", 0));

            int? win32Live = SafeNullable(GamingActions.ReadWin32Priority);
            Win32PriorityIndex = MatchIndex(win32Live ?? -1,
                GamingActions.Win32Values.Select(v => (long)v).ToArray(),
                AkariToolState.GetInt("Win32PrioritySeparation", 0));

            foreach (CpTabGroup tab in TabGroups)
            {
                foreach (CpSectionGroup section in tab.Sections)
                {
                    foreach (CpRowItem row in section.Rows)
                        row.RefreshFromSystem();
                }
            }
        }
        finally
        {
            _loading = false;
        }
    }

    public void OnNavigatedFrom()
    {
    }

    private static int? SafeNullable(Func<int?> read)
    {
        try { return read(); }
        catch { return null; }
    }

    /// <summary>Map a live registry value to its option index; fall back to the persisted index.</summary>
    private static int MatchIndex(long live, long[] values, int fallback)
    {
        int i = Array.IndexOf(values, live);
        return i >= 0 ? i : Math.Clamp(fallback, 0, values.Length - 1);
    }

    partial void OnSvcHostIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= GamingActions.SvcHostValues.Length)
            return;
        try
        {
            GamingActions.ApplySvcHost(GamingActions.SvcHostValues[value]);
            AkariToolState.SetInt("SvcHostSplitThreshold", value);
            _infoBar.Show("SvcHostSplitThreshold  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _infoBar.Show("SvcHostSplitThreshold  failed", ex.Message);
        }
    }

    partial void OnWin32PriorityIndexChanged(int value)
    {
        if (_loading || value < 0 || value >= GamingActions.Win32Values.Length)
            return;
        try
        {
            GamingActions.ApplyWin32Priority(GamingActions.Win32Values[value]);
            AkariToolState.SetInt("Win32PrioritySeparation", value);
            _infoBar.Show("Win32 Priority Separation  applied",
                $"Set to {Win32PriorityOptions[value]}.");
        }
        catch (Exception ex)
        {
            _infoBar.Show("Win32 Priority Separation  failed", ex.Message);
        }
    }

    //  Granular rows 

    private void BuildRowGroups()
    {
        foreach (string tab in TabOrder)
        {
            CpTweakRow[] tabRows = ControlPanelRows.All.Where(r => r.Tab == tab).ToArray();
            if (tabRows.Length == 0)
                continue;
            var group = new CpTabGroup($"{tab} ({tabRows.Length} settings)");
            foreach (var section in tabRows.GroupBy(r => r.Section))
            {
                var sectionGroup = new CpSectionGroup(section.Key);
                foreach (CpTweakRow row in section)
                    sectionGroup.Rows.Add(new CpRowItem(row, ApplyRowAsync, () => _loading));
                group.Sections.Add(sectionGroup);
            }
            TabGroups.Add(group);
        }
    }

    [RelayCommand]
    private Task RunRow(CpRowItem? row) =>
        row is null || !row.IsOneWay ? Task.CompletedTask : ApplyRowAsync(row, true);

    internal async Task ApplyRowAsync(CpRowItem row, bool optimize)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{row.Row.Title}...");
        SetRowsBusy(true);
        try
        {
            await Task.Run(() => ControlPanelRows.ApplyRow(row.Row, optimize));
            if (row.IsRevertible)
                AkariToolState.SetInt("CpRow:" + row.Row.Id, optimize ? 1 : 0);
            _infoBar.Show($"{row.Row.Title}  applied", "Restart to apply.");
        }
        catch (Exception ex)
        {
            _loading = true;
            try { row.RefreshFromSystem(); }
            finally { _loading = false; }
            _infoBar.Show($"{row.Row.Title}  failed", ex.Message);
        }
        finally
        {
            SetRowsBusy(false);
            IsBusy = false;
            _status.Complete();
        }
    }

    /// <summary>Locks or unlocks every row's controls, matching the WPF page's page-wide IsBusy.</summary>
    private void SetRowsBusy(bool value)
    {
        foreach (CpTabGroup group in TabGroups)
            foreach (CpSectionGroup section in group.Sections)
                foreach (CpRowItem row in section.Rows)
                    row.IsBusy = value;
    }

    //  Granular row groups 
}

/// <summary>One tab group (Debloat / Appearance / Tweaks / System) with a
/// collapsible header, like the Akari-Tool expanders (start collapsed).</summary>
public sealed partial class CpTabGroup : ObservableObject
{
    public string Title { get; }

    public ObservableCollection<CpSectionGroup> Sections { get; } = [];

    [ObservableProperty]
    private bool _isExpanded;

    public string Header => Title + (IsExpanded ? "  " : "  ");

    public CpTabGroup(string title)
    {
        Title = title;
    }

    [RelayCommand]
    private void Toggle() => IsExpanded = !IsExpanded;

    partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Header));
}

/// <summary>One section card inside a tab group.</summary>
public sealed class CpSectionGroup(string title)
{
    public string Title { get; } = title;
    public ObservableCollection<CpRowItem> Rows { get; } = [];
}

/// <summary>One granular row: a live-state toggle, or an Apply button when one-way.</summary>
public sealed partial class CpRowItem : ObservableObject
{
    private readonly Func<CpRowItem, bool, Task> _apply;
    private readonly Func<bool> _isLoading;

    public CpTweakRow Row { get; }

    public bool IsRevertible => Row.Revertible;

    public bool IsOneWay => !Row.Revertible;

    [ObservableProperty]
    private bool _isOn;

    /// <summary>Set while a tweak is being applied, so the row's toggle and Apply button lock.</summary>
    [ObservableProperty]
    private bool _isBusy;

    public CpRowItem(CpTweakRow row, Func<CpRowItem, bool, Task> apply, Func<bool> isLoading)
    {
        Row = row;
        _apply = apply;
        _isLoading = isLoading;
    }

    /// <summary>False while an apply is in flight, so the row's controls disable themselves.</summary>
    public bool IsEnabled => !IsBusy;

    /// <summary>
    /// Applies this row's one-way tweak. WPF reached the page's RunRowCommand through
    /// RelativeSource AncestorType=Page; WinUI DataTemplates have no equivalent, so the
    /// command lives on the row and delegates to the same ApplyRowAsync path.
    /// </summary>
    [RelayCommand]
    private Task ApplyAsync() => IsOneWay ? _apply(this, true) : Task.CompletedTask;

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));

    /// <summary>Live probe first, persisted choice as fallback.</summary>
    public void RefreshFromSystem()
    {
        try
        {
            bool? live = ControlPanelRows.ReadRowOptimized(Row);
            IsOn = live ?? AkariToolState.GetInt("CpRow:" + Row.Id, 0) == 1;
        }
        catch
        {
            IsOn = AkariToolState.GetInt("CpRow:" + Row.Id, 0) == 1;
        }
    }

    partial void OnIsOnChanged(bool value)
    {
        if (_isLoading())
            return;
        _ = _apply(this, value);
    }
}
