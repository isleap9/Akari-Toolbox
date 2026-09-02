using AkariToolbox.App.Models;
using AkariToolbox.App.Services;
using AkariToolbox.App.ViewModels;
using AkariToolbox.Framework.Services;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Closing regression-test lock for DOWNLOADS-01/DOWNLOADS-02/D-06 (04-01-PLAN.md Task 2),
/// modeled on <see cref="DebloatCatalogTests"/>'s fake-based style. Locks the 29-app/5-category
/// catalog shape, the filter/search predicate, the D-06 fire-and-forget PostInstall wiring, and
/// the exit-code-aware install command for all later Wave 2 plans to build on without
/// re-litigating it.
///
/// Not sealed and members are not made <c>private</c> in a way that blocks extension — Plan
/// 04-03 extends this class with more test methods for the expanded 42-app catalog, matching
/// <see cref="DebloatCatalogTests"/>'s own cross-wave-extension precedent.
/// </summary>
public class DownloadsCatalogTests
{
    private static readonly string[] ExpectedCategorySequence =
    [
        "Browsers", "Comms", "Dev", "Gaming", "Utilities",
    ];

    private static readonly int[] ExpectedCategoryCounts = [11, 4, 6, 4, 4];

    [Fact]
    public void Catalog_has_exactly_29_apps_in_5_categories_with_predecessor_counts()
    {
        var catalog = new AppCatalog();

        Assert.Equal(29, catalog.Apps.Count);

        var groups = catalog.Apps.GroupBy(a => a.Category).ToList();

        Assert.Equal(ExpectedCategorySequence, groups.Select(g => g.Key));
        Assert.Equal(ExpectedCategoryCounts, groups.Select(g => g.Count()));
    }

    [Fact]
    public void Catalog_app_names_and_winget_ids_are_unique()
    {
        var catalog = new AppCatalog();

        var names = catalog.Apps.Select(a => a.Name).ToList();
        Assert.Equal(names.Distinct().Count(), names.Count);

        var wingetIds = catalog.Apps.Select(a => a.WingetId).Where(id => !string.IsNullOrEmpty(id)).ToList();
        Assert.Equal(wingetIds.Distinct().Count(), wingetIds.Count);
    }

    [Fact]
    public void FilteredApps_narrows_to_selected_category()
    {
        var viewModel = new DownloadsViewModel(
            new AppCatalog(), new FakeAppInstallerService(), new FakePostInstallService(), new LogConsoleService(dispatcher: null));

        viewModel.SelectedCategory = "Gaming";

        Assert.Equal(4, viewModel.FilteredApps.Count);
        Assert.All(viewModel.FilteredApps, a => Assert.Equal("Gaming", a.Category));
    }

    [Fact]
    public void FilteredApps_narrows_by_case_insensitive_description_search()
    {
        var viewModel = new DownloadsViewModel(
            new AppCatalog(), new FakeAppInstallerService(), new FakePostInstallService(), new LogConsoleService(dispatcher: null));

        // "TELEMETRY" (uppercase) matches exactly 2 descriptions ("Ungoogled Chromium",
        // "LibreWolf") case-insensitively — proves both the category-independent narrowing
        // and the case-insensitivity of the search predicate.
        viewModel.SearchText = "TELEMETRY";

        Assert.Equal(2, viewModel.FilteredApps.Count);
        Assert.All(viewModel.FilteredApps, a =>
            Assert.Contains("telemetry", a.Description, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OnNavigatedTo_invokes_EnsurePostInstallAsync_exactly_once()
    {
        var postInstall = new FakePostInstallService();
        var viewModel = new DownloadsViewModel(
            new AppCatalog(), new FakeAppInstallerService(), postInstall, new LogConsoleService(dispatcher: null));

        viewModel.OnNavigatedTo(null);

        Assert.Equal(1, postInstall.EnsureCallCount);
    }

    [Fact]
    public void OnNavigatedTo_swallows_EnsurePostInstallAsync_exception_without_throwing()
    {
        var postInstall = new FakePostInstallService { ThrowOnEnsure = true };
        var viewModel = new DownloadsViewModel(
            new AppCatalog(), new FakeAppInstallerService(), postInstall, new LogConsoleService(dispatcher: null));

        var exception = Record.Exception(() => viewModel.OnNavigatedTo(null));

        Assert.Null(exception);
        Assert.Equal(1, postInstall.EnsureCallCount);
    }

    [Fact]
    public async Task InstallSelectedCommand_calls_installer_once_per_selected_app_and_resets_IsInstalling()
    {
        var installer = new FakeAppInstallerService();
        var viewModel = new DownloadsViewModel(
            new AppCatalog(), installer, new FakePostInstallService(), new LogConsoleService(dispatcher: null));

        var chrome = viewModel.FilteredApps.First(a => a.Name == "Google Chrome");
        var firefox = viewModel.FilteredApps.First(a => a.Name == "Mozilla Firefox");
        chrome.IsSelected = true;
        firefox.IsSelected = true;

        await viewModel.InstallSelectedCommand.ExecuteAsync(null);

        Assert.Equal(2, installer.Calls.Count);
        Assert.Contains("Google Chrome", installer.Calls);
        Assert.Contains("Mozilla Firefox", installer.Calls);
        Assert.False(chrome.IsInstalling);
        Assert.False(firefox.IsInstalling);
    }

    [Fact]
    public async Task InstallSelectedCommand_does_nothing_when_no_apps_selected()
    {
        var installer = new FakeAppInstallerService();
        var viewModel = new DownloadsViewModel(
            new AppCatalog(), installer, new FakePostInstallService(), new LogConsoleService(dispatcher: null));

        await viewModel.InstallSelectedCommand.ExecuteAsync(null);

        Assert.Empty(installer.Calls);
    }

    /// <summary>Tracks <see cref="EnsurePostInstallAsync"/> calls and can be told to throw.</summary>
    private sealed class FakePostInstallService : IPostInstallService
    {
        public int EnsureCallCount { get; private set; }

        public bool ThrowOnEnsure { get; set; }

        public string LocalRoot => @"C:\FakePostInstall";

        public bool IsFullyInstalled => true;

        public Task<bool> EnsurePostInstallAsync()
        {
            EnsureCallCount++;
            if (ThrowOnEnsure)
            {
                throw new InvalidOperationException("Simulated PostInstall failure.");
            }

            return Task.FromResult(true);
        }

        public Task<bool> VerifyFileSha256Async(string filePath, string expectedHexSha256) => Task.FromResult(true);
    }

    /// <summary>Records every <see cref="InstallAsync"/> call's app name and returns a settable result.</summary>
    private sealed class FakeAppInstallerService : IAppInstallerService
    {
        public List<string> Calls { get; } = [];

        public bool InstallResult { get; set; } = true;

        public Task<bool> InstallAsync(AppDefinition app)
        {
            Calls.Add(app.Name);
            return Task.FromResult(InstallResult);
        }
    }
}
