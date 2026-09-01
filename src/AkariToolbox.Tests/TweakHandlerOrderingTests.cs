using AkariToolbox.App.Services;
using AkariToolbox.App.ViewModels;
using AkariToolbox.Framework;
using AkariToolbox.Framework.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Full-catalog regression coverage for Phase 1's closing invariant (TWEAKS-01): the
/// assembled DI container must resolve exactly 32 <see cref="ITweakHandler"/>s, with
/// unique, gap-free <see cref="ITweakHandler.Order"/> values spanning [0..31] that match
/// the predecessor's exact key sequence. Also covers the per-handler error-resilience
/// path (T-01-16): a throwing handler must not crash the Tweaks page.
/// </summary>
public class TweakHandlerOrderingTests
{
    /// <summary>The predecessor's exact 32-tweak sequence, ordered by <see cref="ITweakHandler.Order"/>.</summary>
    private static readonly string[] ExpectedKeySequence =
    [
        "wifi", "tsx", "actioncenter", "dep", "clipboard", "bluetooth", "bootmenu", "vpn",
        "ntfsenc", "fso", "notifications", "prefetch", "cdrom", "spooler", "nolazy", "uacadmin",
        "vr", "uac", "startmenu", "hyperv", "vbs", "wallpaperq", "mpo", "transparency",
        "lockscreen", "animations", "dcom", "nvme", "largecache", "sysprofile", "defender", "mitigation",
    ];

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddMvvmFramework();
        services.AddAkariSystemPrimitives();
        services.AddTweakHandlers();

        // AddAkariSystemPrimitives' ILogConsoleService factory calls
        // DispatcherQueue.GetForCurrentThread(), which requires a real WinRT-activated
        // process (production always resolves it from the MainWindow's UI thread). A
        // plain xunit test host has no such activation context and the WinRT call throws
        // a COMException, not merely returning null. Re-registering ILogConsoleService
        // here (DI resolves the LAST registration for a non-collection service) swaps in
        // a headless-safe instance for this test's DI graph only — production wiring in
        // App.xaml.cs is untouched.
        services.AddSingleton<ILogConsoleService>(new LogConsoleService(dispatcher: null));

        return services.BuildServiceProvider();
    }

    [Fact]
    public void Resolving_ITweakHandler_yields_exactly_32_handlers()
    {
        using var provider = BuildProvider();

        var handlers = provider.GetServices<ITweakHandler>()
            .Where(h => h.Category == TweakCategory.AkariOS)
            .ToList();

        Assert.Equal(32, handlers.Count);
    }

    [Fact]
    public void Handler_order_values_span_0_to_31_with_no_gaps_or_duplicates()
    {
        using var provider = BuildProvider();

        var orders = provider.GetServices<ITweakHandler>()
            .Where(h => h.Category == TweakCategory.AkariOS)
            .Select(h => h.Order)
            .OrderBy(o => o)
            .ToList();

        Assert.Equal(Enumerable.Range(0, 32).ToList(), orders);
    }

    [Fact]
    public void Handlers_sorted_by_order_match_predecessors_exact_key_sequence()
    {
        using var provider = BuildProvider();

        var keys = provider.GetServices<ITweakHandler>()
            .Where(h => h.Category == TweakCategory.AkariOS)
            .OrderBy(h => h.Order)
            .Select(h => h.Key)
            .ToList();

        Assert.Equal(ExpectedKeySequence, keys);
    }

    [Fact]
    public async Task TryGetStateAsync_catches_throwing_handler_logs_and_defaults_to_false()
    {
        // Headless-safe: TryGetStateAsync is internal and static, taking catalog/log/handler
        // as plain parameters, so this test never constructs AkariOSTweaksViewModel itself —
        // its constructor calls DispatcherQueue.GetForCurrentThread(), which throws a
        // COMException outside a real WinRT-activated UI thread (e.g. a plain xunit host),
        // rather than returning null as it would off-UI-thread inside a real running app.
        var log = new LogConsoleService(dispatcher: null);
        var catalog = new FakeTweakCatalog();
        catalog.ThrowOnGetState("throwing");
        var throwingHandler = new FakeTweakHandler("throwing", initialState: true);

        var result = await AkariOSTweaksViewModel.TryGetStateAsync(catalog, log, throwingHandler);

        Assert.False(result);
        Assert.Contains(log.Lines, line =>
            line.Contains("throwing", StringComparison.Ordinal) &&
            line.Contains("GetState failed", StringComparison.Ordinal));
    }

    /// <summary>
    /// A minimal fake <see cref="ITweakCatalog"/> whose <see cref="GetStateAsync"/> can be
    /// made to fault for a given key, for exercising the error-resilience path in isolation.
    /// </summary>
    private sealed class FakeTweakCatalog : ITweakCatalog
    {
        private readonly HashSet<string> _throwingKeys = new();

        public IReadOnlyList<ITweakHandler> Handlers { get; } = [];

        public void ThrowOnGetState(string key) => _throwingKeys.Add(key);

        public Task<bool> GetStateAsync(string key) =>
            _throwingKeys.Contains(key)
                ? Task.FromException<bool>(new InvalidOperationException($"{key} boom"))
                : Task.FromResult(false);

        public Task SetStateAsync(string key, bool enabled) => Task.CompletedTask;
    }
}
