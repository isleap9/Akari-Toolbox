using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AkariToolbox.App.Models;
using AkariToolbox.App.Services;
using AkariToolbox.Framework.Navigation;
using AkariToolbox.Framework.Services;
using AkariToolbox.Framework.ViewModels;

namespace AkariToolbox.App.ViewModels;

/// <summary>
/// Drives the Downloads page — the winget-backed app-installer catalog (DOWNLOADS-02, D-01)
/// plus the silent PostInstall auto-mirror trigger (DOWNLOADS-01, D-06) wired into page
/// navigation. Ported from the predecessor's <c>DownloadsViewModel</c>, replacing its WPF
/// <c>ICollectionView</c>/<c>CollectionViewSource</c> filtering with a manually rebuilt
/// <see cref="ObservableCollection{T}"/> (<see cref="FilteredApps"/>) — WinUI 3 has no
/// <c>ICollectionView</c> equivalent.
///
/// Selection state lives on the master <see cref="AppItem"/> list (not <see cref="FilteredApps"/>),
/// so a selection made before narrowing the search/category filter survives the narrowing —
/// matching the predecessor's behavior where <c>AppItem.IsSelected</c> is independent of the
/// filtered <c>ICollectionView</c>.
/// </summary>
public partial class DownloadsViewModel : ViewModelBase, INavigationAware
{
    private readonly IAppCatalog _catalog;
    private readonly IAppInstallerService _installer;
    private readonly IPostInstallService _postInstall;
    private readonly ILogConsoleService _log;
    private readonly List<AppItem> _allApps;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public DownloadsViewModel(IAppCatalog catalog, IAppInstallerService installer, IPostInstallService postInstall, ILogConsoleService log)
    {
        _catalog = catalog;
        _installer = installer;
        _postInstall = postInstall;
        _log = log;
        Title = "Downloads";

        _allApps = catalog.Apps.Select(a => new AppItem
        {
            Name = a.Name,
            Description = a.Description,
            Category = a.Category,
            WingetId = a.WingetId,
        }).ToList();

        FilteredApps = [];
        ApplyFilter();
    }

    /// <summary>The 5 catalog categories plus "All", matching the predecessor's exact set.</summary>
    public IReadOnlyList<string> Categories { get; } = ["All", "Browsers", "Comms", "Dev", "Gaming", "Utilities"];

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _statusText = "";

    /// <summary>Filtered/searched view bound to the page's <c>ItemsRepeater</c>.</summary>
    public ObservableCollection<AppItem> FilteredApps { get; }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedCategoryChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = SearchText?.Trim() ?? "";
        var matches = _allApps.Where(item =>
            (SelectedCategory == "All" || item.Category == SelectedCategory)
            && (query.Length == 0
                || item.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Description.Contains(query, StringComparison.OrdinalIgnoreCase)));

        FilteredApps.Clear();
        foreach (var item in matches)
        {
            FilteredApps.Add(item);
        }
    }

    /// <summary>
    /// Fire-and-forget per D-06 — <see cref="INavigationAware.OnNavigatedTo"/> stays
    /// synchronous per the interface's contract, so the actual PostInstall check runs on a
    /// discarded task. <see cref="EnsurePostInstallSilentlyAsync"/> never lets an exception
    /// escape, so a network failure can never crash the page or block navigation.
    /// </summary>
    public void OnNavigatedTo(object? parameter) => _ = EnsurePostInstallSilentlyAsync();

    public void OnNavigatedFrom()
    {
        // No-op — Downloads has no per-visit teardown.
    }

    private async Task EnsurePostInstallSilentlyAsync()
    {
        try
        {
            var ok = await _postInstall.EnsurePostInstallAsync();
            _log.Log(ok
                ? "[DOWNLOADS] PostInstall mirror check complete."
                : "[DOWNLOADS] PostInstall mirror check completed with failures — see log above.");
        }
        catch (Exception ex)
        {
            _log.Log($"[DOWNLOADS] PostInstall mirror check failed: {ex.Message}");
        }
    }

    [RelayCommand]
    private void SelectCategory(string category) => SelectedCategory = category;

    [RelayCommand]
    private async Task InstallSelectedAsync()
    {
        var selected = _allApps.Where(a => a.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        foreach (var item in selected)
        {
            var definition = _catalog.Apps.First(a => a.Name == item.Name);
            var gate = _locks.GetOrAdd(item.Name, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync();
            try
            {
                item.IsInstalling = true;
                StatusText = $"Installing {item.Name}...";

                var success = await _installer.InstallAsync(definition);

                StatusText = success ? $"{item.Name} installed." : $"{item.Name} failed to install.";
            }
            finally
            {
                item.IsInstalling = false;
                gate.Release();
            }
        }
    }
}
