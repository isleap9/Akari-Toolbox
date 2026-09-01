using AkariToolbox.App.Models;
using AkariToolbox.App.Services;
using AkariToolbox.App.ViewModels;
using AkariToolbox.Framework.Services;
using Microsoft.UI.Xaml.Controls;
using Xunit;
using AppEntry = AkariToolbox.App.App;

namespace AkariToolbox.Tests;

/// <summary>
/// Closing regression-test lock for DEBLOAT-01/DEBLOAT-02/D-11 (03-01-PLAN.md Task 2),
/// modeled on <see cref="TweakCatalogTests"/>'s fake-based style and
/// <see cref="TweakHandlerOrderingTests"/>'s "closing regression-test lock" style. Locks
/// the 28-action/5-category/5-confirmation-required shape for all later Wave 2-6 plans to
/// build on without re-litigating it.
/// </summary>
public class DebloatCatalogTests
{
    private static readonly string[] ExpectedCategorySequence =
    [
        "Privacy & Telemetry", "System & Performance", "Cleanup", "Explorer & UI", "Tools",
    ];

    private static readonly int[] ExpectedCategoryCounts = [8, 8, 6, 5, 1];

    private static readonly string[] ExpectedConfirmationRequiredKeys =
    [
        "disablebitlocker", "hibernation", "bloatware", "removeonedrive", "edgewebview",
    ];

    [Fact]
    public void Catalog_has_exactly_28_actions_in_5_categories_with_predecessor_counts()
    {
        var catalog = new DebloatCatalog();

        Assert.Equal(28, catalog.Actions.Count);

        var groups = catalog.Actions.GroupBy(a => a.Category).ToList();

        Assert.Equal(ExpectedCategorySequence, groups.Select(g => g.Key));
        Assert.Equal(ExpectedCategoryCounts, groups.Select(g => g.Count()));
    }

    [Fact]
    public void Catalog_action_keys_are_unique()
    {
        var catalog = new DebloatCatalog();

        var keys = catalog.Actions.Select(a => a.Key).ToList();

        Assert.Equal(keys.Distinct().Count(), keys.Count);
    }

    [Fact]
    public void Confirmation_required_set_matches_D11_classification()
    {
        var catalog = new DebloatCatalog();

        var requiredKeys = catalog.Actions.Where(a => a.RequiresConfirmation).Select(a => a.Key).ToList();

        Assert.Equal(ExpectedConfirmationRequiredKeys.OrderBy(k => k), requiredKeys.OrderBy(k => k));
        Assert.All(catalog.Actions.Where(a => !ExpectedConfirmationRequiredKeys.Contains(a.Key)), a => Assert.False(a.RequiresConfirmation));
    }

    [Fact]
    public void Telemetry_action_resources_resolve_in_assembly_manifest()
    {
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();

        Assert.Contains(resourceNames, n => n.EndsWith("telemetry.ps1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(resourceNames, n => n.EndsWith("telemetry-undo.ps1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PrivacyTelemetry_category_all_actions_have_resolvable_resources()
    {
        var catalog = new DebloatCatalog();
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();

        var privacyTelemetryActions = catalog.Actions.Where(a => a.Category == "Privacy & Telemetry");

        Assert.All(privacyTelemetryActions, action =>
        {
            Assert.Contains(resourceNames, n => n.EndsWith(action.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(action.UndoResourceSuffix);
            Assert.Contains(resourceNames, n => n.EndsWith(action.UndoResourceSuffix!, StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void SystemPerformance_category_all_actions_have_resolvable_resources()
    {
        var catalog = new DebloatCatalog();
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();

        var systemPerformanceActions = catalog.Actions.Where(a => a.Category == "System & Performance");

        Assert.All(systemPerformanceActions, action =>
        {
            Assert.Contains(resourceNames, n => n.EndsWith(action.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(action.UndoResourceSuffix);
            Assert.Contains(resourceNames, n => n.EndsWith(action.UndoResourceSuffix!, StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void ExplorerUi_category_all_actions_have_resolvable_resources()
    {
        var catalog = new DebloatCatalog();
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();

        var explorerUiActions = catalog.Actions.Where(a => a.Category == "Explorer & UI");

        Assert.All(explorerUiActions, action =>
        {
            Assert.Contains(resourceNames, n => n.EndsWith(action.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(action.UndoResourceSuffix);
            Assert.Contains(resourceNames, n => n.EndsWith(action.UndoResourceSuffix!, StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public void Tools_category_action_has_resolvable_run_resource_and_no_undo()
    {
        var catalog = new DebloatCatalog();
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();

        var oosuAction = catalog.Actions.Single(a => a.Key == "oosu");

        Assert.Contains(resourceNames, n => n.EndsWith(oosuAction.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));
        Assert.Null(oosuAction.UndoResourceSuffix);
    }

    [Fact]
    public void Cleanup_direct_carry_actions_have_resolvable_resources()
    {
        var catalog = new DebloatCatalog();
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();

        var diskCleanupAction = catalog.Actions.Single(a => a.Key == "diskcleanup");
        var tempFilesAction = catalog.Actions.Single(a => a.Key == "tempfiles");
        var removeOneDriveAction = catalog.Actions.Single(a => a.Key == "removeonedrive");

        Assert.Contains(resourceNames, n => n.EndsWith(diskCleanupAction.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));
        Assert.Null(diskCleanupAction.UndoResourceSuffix);

        Assert.Contains(resourceNames, n => n.EndsWith(tempFilesAction.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));
        Assert.Null(tempFilesAction.UndoResourceSuffix);

        Assert.Contains(resourceNames, n => n.EndsWith(removeOneDriveAction.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(removeOneDriveAction.UndoResourceSuffix);
        Assert.Contains(resourceNames, n => n.EndsWith(removeOneDriveAction.UndoResourceSuffix!, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Cleanup_replacement_actions_have_resolvable_resources()
    {
        var catalog = new DebloatCatalog();
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();

        var replacementActions = new[] { "bloatware", "edgesettings", "edgewebview" }
            .Select(key => catalog.Actions.Single(a => a.Key == key));

        Assert.All(replacementActions, action =>
        {
            Assert.Contains(resourceNames, n => n.EndsWith(action.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(action.UndoResourceSuffix);
            Assert.Contains(resourceNames, n => n.EndsWith(action.UndoResourceSuffix!, StringComparison.OrdinalIgnoreCase));
        });
    }

    /// <summary>
    /// Data source for <see cref="All_28_catalog_actions_have_resolvable_run_resources"/> —
    /// every one of the 28 <see cref="DebloatCatalog"/> actions, one theory case each.
    /// </summary>
    public static IEnumerable<object[]> AllCatalogActions() =>
        new DebloatCatalog().Actions.Select(a => new object[] { a });

    [Theory]
    [MemberData(nameof(AllCatalogActions))]
    public void All_28_catalog_actions_have_resolvable_run_resources(DebloatAction action)
    {
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();

        Assert.Contains(resourceNames, n => n.EndsWith(action.RunResourceSuffix, StringComparison.OrdinalIgnoreCase));

        if (action.UndoResourceSuffix is not null)
        {
            Assert.Contains(resourceNames, n => n.EndsWith(action.UndoResourceSuffix, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task DebloatViewModel_run_action_invokes_script_runner_with_correct_suffix()
    {
        var scriptRunner = new FakeScriptRunner();
        var viewModel = new DebloatViewModel(new DebloatCatalog(), new LogConsoleService(dispatcher: null), scriptRunner, new FakeDialogService());
        var telemetryItem = viewModel.CategoryGroups.SelectMany(g => g.Actions).First(a => a.Key == "telemetry");

        await viewModel.RunActionCommand.ExecuteAsync(telemetryItem);

        Assert.Equal(["telemetry.ps1"], scriptRunner.Calls);

        await viewModel.UndoActionCommand.ExecuteAsync(telemetryItem);

        Assert.Equal(["telemetry.ps1", "telemetry-undo.ps1"], scriptRunner.Calls);
    }

    [Fact]
    public async Task DebloatViewModel_confirmation_gates_only_run_direction()
    {
        var scriptRunner = new FakeScriptRunner();
        var dialogService = new FakeDialogService { ConfirmResult = false };
        var viewModel = new DebloatViewModel(new DebloatCatalog(), new LogConsoleService(dispatcher: null), scriptRunner, dialogService);
        var bitlockerItem = viewModel.CategoryGroups.SelectMany(g => g.Actions).First(a => a.Key == "disablebitlocker");

        await viewModel.RunActionCommand.ExecuteAsync(bitlockerItem);

        Assert.Equal(1, dialogService.ConfirmAsyncCallCount);
        Assert.Empty(scriptRunner.Calls);

        await viewModel.UndoActionCommand.ExecuteAsync(bitlockerItem);

        Assert.Equal(1, dialogService.ConfirmAsyncCallCount);
        Assert.Equal(["disablebitlocker-undo.ps1"], scriptRunner.Calls);
    }

    [Fact]
    public void DebloatViewModel_CategoryGroups_matches_predecessors_exact_category_sequence_and_counts()
    {
        var viewModel = new DebloatViewModel(new DebloatCatalog(), new LogConsoleService(dispatcher: null), new FakeScriptRunner(), new FakeDialogService());

        Assert.Equal(ExpectedCategorySequence, viewModel.CategoryGroups.Select(g => g.Name));
        Assert.Equal(ExpectedCategoryCounts, viewModel.CategoryGroups.Select(g => g.Actions.Count));
    }

    [Fact]
    public void DebloatViewModel_total_action_count_is_28()
    {
        var viewModel = new DebloatViewModel(new DebloatCatalog(), new LogConsoleService(dispatcher: null), new FakeScriptRunner(), new FakeDialogService());

        Assert.Equal(28, viewModel.CategoryGroups.Sum(g => g.Actions.Count));
    }

    [Fact]
    public void DebloatViewModel_populates_UndoDownloadsUnverifiedBinary_from_catalog()
    {
        var viewModel = new DebloatViewModel(new DebloatCatalog(), new LogConsoleService(dispatcher: null), new FakeScriptRunner(), new FakeDialogService());
        var items = viewModel.CategoryGroups.SelectMany(g => g.Actions).ToList();

        var expectedTrueKeys = new[] { "edgewebview", "edgesettings" };

        Assert.All(items.Where(i => expectedTrueKeys.Contains(i.Key)), i => Assert.True(i.UndoDownloadsUnverifiedBinary));
        Assert.All(items.Where(i => !expectedTrueKeys.Contains(i.Key)), i => Assert.False(i.UndoDownloadsUnverifiedBinary));
    }

    [Fact]
    public void DebloatViewModel_populates_RequiresConfirmation_from_catalog_unchanged()
    {
        var viewModel = new DebloatViewModel(new DebloatCatalog(), new LogConsoleService(dispatcher: null), new FakeScriptRunner(), new FakeDialogService());
        var items = viewModel.CategoryGroups.SelectMany(g => g.Actions).ToList();

        Assert.All(items.Where(i => ExpectedConfirmationRequiredKeys.Contains(i.Key)), i => Assert.True(i.RequiresConfirmation));
        Assert.All(items.Where(i => !ExpectedConfirmationRequiredKeys.Contains(i.Key)), i => Assert.False(i.RequiresConfirmation));
    }

    /// <summary>Records every <see cref="RunEmbeddedScriptAsync"/> call's resource suffix.</summary>
    private sealed class FakeScriptRunner : IScriptRunner
    {
        public List<string> Calls { get; } = [];

        public Task<int> RunProcessAsync(string fileName, string arguments, TimeSpan? timeout = null) => Task.FromResult(0);

        public Task<string> RunProcessCaptureOutputAsync(string fileName, string arguments, TimeSpan? timeout = null) => Task.FromResult("");

        public Task<int> RunEmbeddedScriptAsync(string resourceSuffix, string? arguments = null, TimeSpan? timeout = null)
        {
            Calls.Add(resourceSuffix);
            return Task.FromResult(0);
        }
    }

    /// <summary>A settable fake <see cref="IDialogService"/> — only <see cref="ConfirmAsync"/> needs real logic.</summary>
    private sealed class FakeDialogService : IDialogService
    {
        public bool ConfirmResult { get; set; } = true;

        public int ConfirmAsyncCallCount { get; private set; }

        public Task<ContentDialogResult> ShowAsync(string title, string content, string primaryText = "OK", string? secondaryText = null, string? cancelText = null) =>
            Task.FromResult(ContentDialogResult.Primary);

        public Task<bool> ConfirmAsync(string title, string message, string confirmText = "Confirm", string? cancelText = "Cancel")
        {
            ConfirmAsyncCallCount++;
            return Task.FromResult(ConfirmResult);
        }

        public Task ShowInfoAsync(string title, string message) => Task.CompletedTask;

        public Task ShowErrorAsync(string title, string message) => Task.CompletedTask;

        public Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog) => Task.FromResult(ContentDialogResult.Primary);

        public Task<ContentDialogResult> ShowDialogAsync<T>(object viewModel, string title, string primaryText, string? secondaryText = null, string? cancelText = null)
            where T : Microsoft.UI.Xaml.UIElement, new() =>
            Task.FromResult(ContentDialogResult.Primary);
    }
}
