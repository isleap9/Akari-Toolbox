# Testing Patterns

**Analysis Date:** 2026-10-03

## Test Framework

**Runner:**
- xUnit 2.9.3 with `Microsoft.NET.Test.Sdk` 17.14.1 and `xunit.runner.visualstudio` 3.1.4
- Config: `src/AppTemplate.Tests/AppTemplate.Tests.csproj` (no `xunit.runner.json`; parallelization is disabled in code — see `src/AppTemplate.Tests/AssemblyInfo.cs`)
- Central package pinning in `Directory.Packages.props` (`ManagePackageVersionsCentrally`, `CentralPackageTransitivePinningEnabled`); test project targets the shared `net10.0-windows10.0.26100.0` from `Directory.Build.props`

**Assertion Library:**
- xUnit asserts only (`Assert.Equal`, `Assert.True/False`, `Assert.Null/NotNull`, `Assert.Contains`, `Assert.Single/Empty`, `Assert.Same/NotEqual`, `Assert.Throws<T>`). No FluentAssertions/Shouldly

**Run Commands:**
```bash
dotnet test AppTemplate.slnx                                    # Run all tests (solution file at repo root)
dotnet test src/AppTemplate.Tests/AppTemplate.Tests.csproj     # Run only the unit-test project
dotnet test --filter "FullyQualifiedName~ConvertersTests"      # Run one class
dotnet test -c Release                                          # Release-configuration run
```

## Test File Organization

**Location:**
- Separate project, never co-located: all tests live in `src/AppTemplate.Tests/`; production code in `src/AppTemplate.Framework/` and `src/AppTemplate.App/`

**Naming:**
- Test classes are `<Area>Tests` (plural): `ViewModelTests`, `ServicesTests`, `ConvertersTests`, `NavigationTests`, `MessagingTests`, `LoggingTests`, `SettingsServiceTests`, `FileSettingsStorageTests`, `CollectionsTests`, `ObjectExtensionsTests`
- Test methods are `<Unit>_<condition>_<expected>` in lower_snake segments after the unit name: `ThemeService_set_same_theme_is_noop`, `IncrementalLoadingCollection_StopLoading_blocks_further_loads`, `MathConverter_guards_invalid_input`, `MemorySettingsStorage_write_null_removes_key`

**Structure:**
```
src/AppTemplate.Tests/
├── AssemblyInfo.cs            # [assembly: CollectionBehavior(DisableTestParallelization = true)]
├── ViewModelTests.cs          # ViewModelBase + ValidatableViewModelBase
├── ServicesTests.cs           # Theme/Culture/InfoBar services + DI registration
├── SettingsServiceTests.cs    # MemorySettingsStorage + SettingsService incl. corrupt-JSON path
├── FileSettingsStorageTests.cs# File-backed storage round-trip, temp-file, corrupt/missing file
├── NavigationTests.cs         # FrameNavigationService guards + record equality
├── MessagingTests.cs          # Message records + WeakReferenceMessenger send/unregister
├── LoggingTests.cs             # FileLoggerProvider write/rotate/retain
├── ConvertersTests.cs         # All 15 IValueConverter implementations
├── CollectionsTests.cs        # Range/Group/IncrementalLoading collections
└── ObjectExtensionsTests.cs   # Let / IfNotNull helpers
```

## Test Structure

**Suite Organization:**
```csharp
// src/AppTemplate.Tests/SettingsServiceTests.cs
namespace AppTemplate.Tests;

public class SettingsServiceTests
{
    [Fact]
    public async Task SettingsService_returns_default_on_corrupt_json()
    {
        var storage = new MemorySettingsStorage();
        await storage.WriteAsync("key", "not-json{{");
        var service = new SettingsService(storage);

        Assert.Equal(-1, await service.GetAsync<int>("key", -1));
    }

    private sealed class UserProfile
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
```
- One flat class per area, no nesting, no base class (except `IDisposable` for temp-directory cleanup). Arrange/Act/Assert inline in each test; shared helpers go at the file bottom as `private`/`internal sealed` types

**Patterns:**
- Setup pattern: construct the real SUT directly with `new` plus a lightweight fake (`new SettingsService(new MemorySettingsStorage())`, `new ThemeService(_settings, _messenger)`); class-level fixtures via the constructor + `readonly` fields when several tests share them (`ServicesTests` builds `_storage`/`_settings` once per test-class instance)
- Teardown pattern: implement `IDisposable.Dispose()` with a swallow-all `try/catch` around directory deletion for temp-folder tests; wrap messenger registrations in `try/finally` with `_messenger.Unregister<T>(this)` (`src/AppTemplate.Tests/LoggingTests.cs`, `src/AppTemplate.Tests/ServicesTests.cs`)
- Assertion pattern: single-behavior asserts with exact values (`Assert.Equal("Hello", viewModel.Title)`), event-count asserts for notifications (`Assert.Equal(1, notifications)`), and `Assert.Contains(nameof(...), changed)` for `PropertyChanged`/`ErrorsChanged` contract checks (`src/AppTemplate.Tests/ViewModelTests.cs`, `src/AppTemplate.Tests/CollectionsTests.cs`)

## Mocking

**Framework:** Moq 4.20.72 is referenced in `src/AppTemplate.Tests/AppTemplate.Tests.csproj` but **no test uses `Mock<>` today** — do not introduce Moq for cases the existing fake patterns already cover.

**Patterns:**
- Prefer the real implementation with a fake boundary over a mock. The canonical fakes:
```csharp
// src/AppTemplate.Tests/ServicesTests.cs — MemorySettingsStorage is the ISettingsStorage fake
private readonly MemorySettingsStorage _storage = new();
private readonly ISettingsService _settings;
public ServicesTests() { _settings = new SettingsService(_storage); }
```
```csharp
// src/AppTemplate.Tests/MessagingTests.cs — the real shared messenger, unregistered in finally
_messenger.Register<UserActionMessage>(this, (_, m) => received = m);
try { _messenger.Send(new UserActionMessage("refresh")); /* assert */ }
finally { _messenger.Unregister<UserActionMessage>(this); }
```
- Lambda page factories stand in for UI: `_ => throw new InvalidOperationException()` for guard tests, `_ => null!` for the factory-returns-null path (`src/AppTemplate.Tests/NavigationTests.cs`)
- Queued delegates feed paged loaders: `new Queue<IEnumerable<string>>()` drained by `(_, _) => Task.FromResult(pages.Dequeue())` (`src/AppTemplate.Tests/CollectionsTests.cs`)

**What to Mock:**
- Nothing currently. If you must isolate a new WinUI-bound dependency (dialogs, file pickers, windows), hand-roll a minimal interface fake in the test file — matching the existing style — rather than adding `Mock<>` setups

**What NOT to Mock:**
- Do not mock value records/messages, converters, collections, `SettingsService`, `FileLoggerProvider`, or `WeakReferenceMessenger.Default` — every current test exercises the real code and asserts real behavior, including cross-test-global messenger delivery (which is why parallelization is disabled)

## Fixtures and Factories

**Test Data:**
```csharp
// Inline literals per test — no shared builders (src/AppTemplate.Tests/ConvertersTests.cs)
var converter = new BoolToValueConverter { TrueValue = "Yes", FalseValue = "No" };
Assert.Equal("Yes", Convert(converter, true));

// Private static helper to hide WinRT boilerplate (src/AppTemplate.Tests/ConvertersTests.cs)
private static object? Convert(Microsoft.UI.Xaml.Data.IValueConverter converter, object? value, object? parameter = null)
    => converter.Convert(value, typeof(object), parameter, "en-US");

// Per-test temp isolation with Guid names (src/AppTemplate.Tests/LoggingTests.cs)
private readonly string _directory = Path.Combine(
    Path.GetTempPath(), "FileLoggerTests", Guid.NewGuid().ToString("N"));
```
- `FileSettingsStorageTests` isolates under `%LocalAppData%\FileSettingsStorageTests.<Guid>` so the real `%LocalAppData%` layout is exercised without colliding (`src/AppTemplate.Tests/FileSettingsStorageTests.cs`)

**Location:**
- Fixtures live at the bottom of the test file that uses them (`UserProfile` in `SettingsServiceTests.cs`, `TestViewModel`/`TestValidatableViewModel` in `ViewModelTests.cs`, `AbstractTestPage` at the top of `NavigationTests.cs`). No shared `Fixtures/` or `Helpers/` directory exists — do not create one for a single-file need

## Coverage

**Requirements:** None enforced — no coverlet thresholds, no `--collect:"XPlat Code Coverage"` in scripts, no coverage gate in `build-and-run.ps1`.

**View Coverage:**
```bash
dotnet test /p:CollectCoverage=true --collect:"XPlat Code Coverage"   # ad-hoc only; not wired into any script
```

## Test Types

**Unit Tests:**
- Everything in `src/AppTemplate.Tests/`: pure-logic xUnit tests over `AppTemplate.Framework` (converters, collections, settings, navigation guards, messaging records, logging, validation base classes). They run headless via `dotnet test` and must stay UI-thread-free
- Guard-clause tests are first-class: every public constructor/factory has a `rejects_null` / `rejects_invalid` test asserting `ArgumentNullException` / `ArgumentException` with `ParamName` where relevant (`src/AppTemplate.Tests/NavigationTests.cs`, `src/AppTemplate.Tests/CollectionsTests.cs`)
- Record-equality tests pin value semantics: `Assert.Equal(new ThemeChangedMessage(AppTheme.Dark), ...)` / `Assert.NotEqual(...)` for messages and `NavigationEntry` (`src/AppTemplate.Tests/MessagingTests.cs`, `src/AppTemplate.Tests/NavigationTests.cs`)

**Integration Tests:**
- `FileSettingsStorageTests` (real `%LocalAppData%` JSON file, atomic `.tmp` write, corrupt-file tolerance) and `LoggingTests` (real rolling log files, size rotation, retention pruning) are the integration boundary — real filesystem, temp-isolated, `IDisposable` cleanup
- `ServicesTests.AddMvvmFramework_registers_services` resolves the full DI graph from a real `ServiceCollection` and asserts every singleton registration (`src/AppTemplate.Tests/ServicesTests.cs`)

**E2E Tests:**
- No automated E2E framework (no Playwright/WinAppDriver). Page-level verification is manual via repo scripts, not `dotnet test`:
  - `verify-pages.ps1` — clicks all 13 nav items via UIA and reports per-page content counts
  - `check-parity.ps1` — diffs every `Text=`/`Content=` string between each WPF page and its port
  - `shot-pages.ps1` — screenshot helper

## Common Patterns

**Async Testing:**
```csharp
// Async tests return Task and await the SUT — never .Result/.Wait (src/AppTemplate.Tests/SettingsServiceTests.cs)
[Fact]
public async Task MemorySettingsStorage_round_trips_values()
{
    var storage = new MemorySettingsStorage();
    await storage.WriteAsync("key", "value");
    Assert.Equal("value", await storage.ReadAsync("key"));
}
```
- Multi-step async flows assert each page: `LoadMoreItemsAsync` → count → collection contents → `HasMoreItems` flag, ending with the guarded call after exhaustion (`src/AppTemplate.Tests/CollectionsTests.cs`)

**Error Testing:**
```csharp
// Guard clauses: exact exception type, plus ParamName/message where it pins the contract
Assert.Throws<ArgumentNullException>(() => new FrameNavigationService(null!));
var ex = Assert.Throws<ArgumentException>(() => service.NavigateTo(typeof(string)));
Assert.Equal("pageType", ex.ParamName);

// Degradation (not exceptions): corrupt input returns defaults
Assert.Equal(-1, await service.GetAsync<int>("key", -1));   // corrupt JSON → default
Assert.Null(await storage.ReadAsync("Theme"));              // corrupt file → null

// Untestable-in-host WinRT statics are asserted as throws, with the why-comment kept
Assert.Throws<COMException>(() => converter.ConvertBack(false, typeof(TestEnum), "Second", "en-US"));
Assert.Throws<NotSupportedException>(() => converter.ConvertBack(1, typeof(object), "+", "en-US"));
```

**Notable test-only idioms to preserve:**
- `[assembly: CollectionBehavior(DisableTestParallelization = true)]` in `src/AppTemplate.Tests/AssemblyInfo.cs` — required because tests share `WeakReferenceMessenger.Default`; do not re-enable parallelization without scoping the messenger per test
- Reading a log file while the provider still holds it must use `FileShare.ReadWrite | FileShare.Delete` (never `File.ReadAllText`) — copy the `ReadAll` helper in `src/AppTemplate.Tests/LoggingTests.cs` for any new log-content test
- `[Theory]` + `[InlineData]` covers severity/operator variants in one method instead of N facts: `InfoBarService_show_variants_set_severity` (4 severities), `MathConverter_supported_operators` (`-`, `*`, `%`, `=`, `!=`) (`src/AppTemplate.Tests/ServicesTests.cs`, `src/AppTemplate.Tests/ConvertersTests.cs`)

## Gaps (what has no tests)

- `src/AppTemplate.App/ViewModels/*.cs` (13 app VMs: `Home`, `Gaming`, `Check`, `Refresh`, `Setup`, `Installers`, `Graphics`, `Windows`, `Hardware`, `Advanced`, `Tweaks`, `AkariTweaks`, `Settings`) — `IsBusy`/`_loading` discipline, `ApplyToggle` rollback, and `OnNavigatedTo` hydration are untested; new VM logic should add tests using the `ServicesTests` pattern (real services + `MemorySettingsStorage`, real messenger with `try/finally` unregister)
- `src/AppTemplate.App/Tweaks/*Actions*.cs`, `TweakAction.cs`, `SystemInfo.cs`, `NativeOps.cs` — native registry/process/WMI paths untested (admin + machine-state dependent); keep them out of `dotnet test` and cover via `verify-pages.ps1` manual runs
- `src/AppTemplate.App/Views/*.xaml*`, `MainWindow.xaml.cs`, `App.xaml.cs` startup/single-instance/safe-boot flows — UI-only, covered manually by `verify-pages.ps1` / `check-parity.ps1`
- `DispatcherQueueExtensions`, `DialogService`, `FilePickerService`, `WindowService`, `StatusService`, `Behaviors/*`, `RangeObservableCollection` beyond basics — no dedicated tests; `StatusService` is especially notable since every VM depends on its `Start`/`Complete` pairing

---

*Testing analysis: 2026-10-03*
