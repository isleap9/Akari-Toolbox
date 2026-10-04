using System.Collections.ObjectModel;
using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// Gaming Catalog page: the searchable four-group catalog (D-02/D-03) where every
/// row carries an inline explanation (D-10). Bulk apply acts on ticked rows only
/// (D-11) with footer progress (D-13), stops at the first failure (D-14), keeps
/// ticks sticky (D-12), offers a restore point with a Skip path (D-15), and
/// reverts with one click and no confirmation (D-16). Keystrokes only filter the
/// in-memory descriptor list — live reads happen on navigation and after runs.
/// </summary>
public partial class GamingCatalogViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private readonly IDialogService _dialogs;
    private readonly GamingJournalStore _journal = GamingJournalStore.Default;
    private bool _loading;

    private readonly Dictionary<GamingGroup, List<GamingCatalogRowItem>> _allRows = new();

    public ObservableCollection<GamingCatalogGroup> Groups { get; } = [];

    public ObservableCollection<string> Suggestions { get; } = [];

    /// <summary>True while an apply or revert is in flight; the page disables its controls.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Current search text (drives the empty-state copy).</summary>
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    /// <summary>True when a non-empty search matches zero rows.</summary>
    [ObservableProperty]
    private bool _hasNoResults;

    /// <summary>Empty-state body: the query plus the name-only hint.</summary>
    public string NoResultsDetail =>
        $"No tweaks match \"{SearchQuery}\". Try a different term — search matches tweak names only.";

    private int SelectedCount => _allRows.Values.SelectMany(rows => rows).Count(r => r.IsSelected);

    /// <summary>Apply button label with the live tick count (zero-one-many state).</summary>
    public string ApplySelectedLabel => $"Apply selected ({SelectedCount})";

    /// <summary>Apply is enabled only with at least one tick and no run in flight.</summary>
    public bool CanApply => SelectedCount > 0 && !IsBusy;

    public GamingCatalogViewModel(
        IInfoBarService infoBar, IStatusService status, IDialogService dialogs)
    {
        _infoBar = infoBar;
        _status = status;
        _dialogs = dialogs;
        Title = "Gaming Catalog";
        BuildRows();
        ApplyFilter();
    }

    public void OnNavigatedTo(object? parameter)
    {
        _loading = true;
        try
        {
            // Reads are live-state probes — refresh off the keystroke path so
            // navigation never stutters on slow registry reads.
            foreach (GamingCatalogRowItem row in _allRows.Values.SelectMany(rows => rows))
                row.RefreshState();
            ApplyFilter();
        }
        finally
        {
            _loading = false;
        }
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>Mirrors the page-wide IsBusy onto every row, like the Tweaks page fan-out.</summary>
    partial void OnIsBusyChanged(bool value)
    {
        foreach (GamingCatalogRowItem row in _allRows.Values.SelectMany(rows => rows))
            row.IsBusy = value;
        OnPropertyChanged(nameof(CanApply));
    }

    partial void OnSearchQueryChanged(string value) => OnPropertyChanged(nameof(NoResultsDetail));

    /// <summary>Keystroke path: title-only prefix-first suggestions plus list filter.</summary>
    public void UpdateSearch(string query)
    {
        SearchQuery = query ?? string.Empty;
        Suggestions.Clear();
        foreach (GamingCatalogEntry entry in GamingCatalogEntry.Suggest(SearchQuery))
            Suggestions.Add(entry.Title);
        ApplyFilter();
    }

    /// <summary>Tapping a suggestion filters the row list; it never ticks a row (D-09).</summary>
    public void ChooseSuggestion(string suggestion) => UpdateSearch(suggestion);

    private void BuildRows()
    {
        foreach (GamingGroup group in Enum.GetValues<GamingGroup>())
        {
            var view = new GamingCatalogGroup(group);
            var items = new List<GamingCatalogRowItem>();
            foreach (GamingCatalogEntry entry in GamingCatalogEntry.All.Where(e => e.Group == group))
            {
                var item = new GamingCatalogRowItem(entry, () => _loading, OnSelectionChanged);
                items.Add(item);
                view.Rows.Add(item);
            }
            _allRows[group] = items;
            Groups.Add(view);
        }
    }

    private void OnSelectionChanged()
    {
        if (_loading)
            return;
        OnPropertyChanged(nameof(ApplySelectedLabel));
        OnPropertyChanged(nameof(CanApply));
    }

    private void ApplyFilter()
    {
        foreach (GamingCatalogGroup group in Groups)
        {
            group.Rows.Clear();
            foreach (GamingCatalogRowItem row in _allRows[group.Group]
                .Where(r => GamingCatalogEntry.MatchesFilter(r.Entry, SearchQuery)))
                group.Rows.Add(row);
            group.IsVisible = group.Rows.Count > 0;
        }
        HasNoResults = !string.IsNullOrWhiteSpace(SearchQuery)
            && Groups.All(g => g.Rows.Count == 0);
        OnPropertyChanged(nameof(ApplySelectedLabel));
        OnPropertyChanged(nameof(CanApply));
    }

    /// <summary>
    /// Bulk apply over ticked rows: restore-point offer (never blocking), then the
    /// sequential capture-plus-apply loop with footer progress. Stops at the first
    /// failure; ticks stay sticky in both outcomes.
    /// </summary>
    [RelayCommand]
    private async Task ApplySelectedAsync()
    {
        if (IsBusy)
            return;

        List<GamingCatalogEntry> ticked = _allRows.Values.SelectMany(rows => rows)
            .Where(r => r.IsSelected).Select(r => r.Entry).ToList();
        if (ticked.Count == 0)
        {
            _infoBar.Show("Nothing selected", "Tick at least one tweak first.");
            return;
        }

        // Restore point is offered, not forced (D-15): any failure downgrades to
        // a warning and apply continues — the journal is the real safety net.
        try
        {
            var choice = await _dialogs.ShowAsync(
                "Create a restore point first?",
                "A restore point lets Windows roll back system files if anything goes wrong. " +
                "Your tweaks are already journaled for one-click revert, so this is optional.",
                "Create restore point and continue", "Skip and apply");
            // The dialog contract returns a WinUI result enum; compare by name so
            // this view model never names a UI type directly.
            if (choice.ToString() == "Primary")
            {
                try
                {
                    await Task.Run(() => RestorePointActions.CreateRestorePoint("Akari Toolbox"));
                    _infoBar.Show("Restore point  created", "You can roll back from System Protection.");
                }
                catch (Exception ex)
                {
                    _infoBar.Show("Restore point  skipped", ex.Message);
                }
            }
        }
        catch
        {
            // No dialog host (or any dialog failure) never blocks apply.
        }

        IsBusy = true;
        _status.Start("Applying gaming tweaks…");
        try
        {
            var progress = new Progress<GamingRunner.BulkProgress>(p =>
                _status.Report(100.0 * p.Done / Math.Max(1, p.Total),
                    $"Applying {p.Done} of {p.Total}: {p.Title}"));
            GamingRunner.BulkResult result = await GamingRunner.RunAsync(ticked, _journal, progress);

            foreach (GamingCatalogRowItem row in _allRows.Values.SelectMany(rows => rows))
                row.RefreshState();

            if (result.FailedEntry is null)
                _infoBar.Show($"{result.Done} tweaks applied", "Ticks stay selected for re-run.");
            else
                _infoBar.Show(
                    $"\"{result.FailedEntry.Title}\" failed — the run stopped before applying the rest. " +
                    "Fix or untick it, then re-run.",
                    $"Details: {result.FailedMessage}");
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }

    /// <summary>One-click revert with no confirmation (D-16): reverse-replays the
    /// journal with continue-on-error, then refreshes live state.</summary>
    [RelayCommand]
    private async Task RevertAllAsync()
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start("Reverting gaming tweaks…");
        try
        {
            IReadOnlyList<RevertResult> results = await Task.Run(() => _journal.RevertAll());

            foreach (GamingCatalogRowItem row in _allRows.Values.SelectMany(rows => rows))
                row.RefreshState();

            if (results.Count == 0)
            {
                _infoBar.Show("Nothing to revert", "No journaled changes found.");
                return;
            }

            RevertResult[] failures = results.Where(r => !r.Ok).ToArray();
            if (failures.Length == 0)
                _infoBar.Show($"{results.Count} tweaks reverted", "Prior values were restored from the journal.");
            else
                _infoBar.Show(
                    $"{results.Count - failures.Length} of {results.Count} tweaks reverted",
                    string.Join("; ", failures.Select(f => $"{f.CatalogId}: {f.Error}")));
        }
        catch (Exception ex)
        {
            _infoBar.Show("Revert  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }
}

/// <summary>One of the four catalog groups with its filtered rows.</summary>
public sealed partial class GamingCatalogGroup : ObservableObject
{
    public GamingGroup Group { get; }

    public string Title => GamingCatalogEntry.GroupTitle(Group);

    public ObservableCollection<GamingCatalogRowItem> Rows { get; } = [];

    [ObservableProperty]
    private bool _isVisible = true;

    public GamingCatalogGroup(GamingGroup group)
    {
        Group = group;
    }
}

/// <summary>
/// One catalog row: the entry plus a TwoWay tick. No item command is needed —
/// the CheckBox binds to <see cref="IsSelected"/> directly (unlike button rows,
/// which need commands because DataTemplates have no page-ancestor binding).
/// Ticks are never mutated by apply/revert paths (D-12).
/// </summary>
public sealed partial class GamingCatalogRowItem : ObservableObject
{
    private readonly Func<bool> _isLoading;
    private readonly Action _onSelectionChanged;

    public GamingCatalogEntry Entry { get; }

    [ObservableProperty]
    private bool _isSelected;

    /// <summary>Set while a run is in flight, so the row's CheckBox locks.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Live applied state, refreshed on navigation and after runs.</summary>
    [ObservableProperty]
    private bool _isApplied;

    public GamingCatalogRowItem(
        GamingCatalogEntry entry, Func<bool> isLoading, Action onSelectionChanged)
    {
        Entry = entry;
        _isLoading = isLoading;
        _onSelectionChanged = onSelectionChanged;
    }

    /// <summary>False while a run is in flight, so the row's CheckBox disables itself.</summary>
    public bool IsEnabled => !IsBusy;

    /// <summary>State label beside the tick.</summary>
    public string AppliedLabel => IsApplied ? "Applied" : "Not applied";

    /// <summary>Inline explanation lines (D-10): labeled, one sentence each.</summary>
    public string WhatLine => $"What it does: {Entry.WhatItDoes}";

    /// <summary>Inline explanation lines (D-10): labeled, one sentence each.</summary>
    public string WhyLine => $"Why it helps gaming: {Entry.WhyItHelpsGaming}";

    /// <summary>Inline explanation lines (D-10): labeled, one sentence each.</summary>
    public string RiskLine => $"Risk: {Entry.Risk}";

    /// <summary>Inline explanation lines (D-10): labeled, one sentence each.</summary>
    public string RevertsLine => $"Reverts by: {Entry.RevertsBy}";

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));

    partial void OnIsAppliedChanged(bool value) => OnPropertyChanged(nameof(AppliedLabel));

    partial void OnIsSelectedChanged(bool value)
    {
        if (!_isLoading())
            _onSelectionChanged();
    }

    /// <summary>Live probe with a safe fallback, so a probe never crashes navigation.</summary>
    public void RefreshState()
    {
        try
        {
            IsApplied = Entry.Read();
        }
        catch
        {
            IsApplied = false;
        }
    }
}
