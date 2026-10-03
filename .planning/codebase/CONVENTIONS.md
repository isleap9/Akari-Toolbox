# Coding Conventions

**Analysis Date:** 2026-10-03

## Naming Patterns

**Files:**
- One public type per file; file name matches the type: `src/AppTemplate.Framework/Navigation/FrameNavigationService.cs` → `FrameNavigationService`
- Partial-class splits use a `.Suffix` segment: `src/AppTemplate.App/Tweaks/WindowsActions.System.cs`, `src/AppTemplate.App/Tweaks/AdvancedActions.Services.cs`, `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs`, `src/AppTemplate.App/Tweaks/GraphicsActions.Nip.cs`
- Test files are `<Area>Tests.cs` in `src/AppTemplate.Tests/`: `ViewModelTests.cs`, `ServicesTests.cs`, `ConvertersTests.cs`, `NavigationTests.cs`, `MessagingTests.cs`, `LoggingTests.cs`, `SettingsServiceTests.cs`, `FileSettingsStorageTests.cs`, `CollectionsTests.cs`, `ObjectExtensionsTests.cs`
- Views are `<Name>Page.xaml` + `<Name>Page.xaml.cs` in `src/AppTemplate.App/Views/`; view models are `<Name>ViewModel.cs` in `src/AppTemplate.App/ViewModels/` (e.g. `GamingPage.xaml.cs` ↔ `GamingViewModel.cs`)
- Tweak ports are `<Area>Actions.cs` statics in `src/AppTemplate.App/Tweaks/` (e.g. `GamingActions.cs`, `GraphicsActions.cs`, `CheckActions.cs`); catalog data is `AkariTweakCatalog.cs` / `ControlPanelRows.cs`

**Functions:**
- Use `PascalCase` for all methods, including private ones: `ReadControlPanelIndex()`, `ApplyControlPanelAsync()`, `BuildButtons()`, `RefreshFromSystem()` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Use an `Async` suffix for awaitable methods: `CreateRestorePointAsync()`, `ApplyRowAsync()`, `ApplyControlPanelAsync()`, `LoadMoreItemsAsync()` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.Framework/Collections/IncrementalLoadingCollection.cs`)
- Use `On<Property>Changed` partials for generated-property side effects: `OnIsBusyChanged(bool value)`, `OnDisablePreemptionChanged(bool value)`, `OnControlPanelIndexChanged(int value)` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Use `Safe` / `SafeNullable` wrappers for fallible live-state probes: `Safe(SystemInfo.OsSummary)`, `SafeNullable(GamingActions.ReadSvcHost)` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Test methods read as `UnitUnderTest_condition_expectedOutcome` in `snake_case` segments: `ViewModelBase_title_raises_property_changed`, `SettingsService_returns_default_on_corrupt_json`, `RangeObservableCollection_AddRange_raises_single_notification` (`src/AppTemplate.Tests/ViewModelTests.cs`, `src/AppTemplate.Tests/SettingsServiceTests.cs`, `src/AppTemplate.Tests/CollectionsTests.cs`)

**Variables:**
- Backing fields for `[ObservableProperty]` are `_camelCase` and generate a `PascalCase` property: `[ObservableProperty] private bool _isBusy;` → `IsBusy` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Constructor-injected dependencies go to `private readonly` fields named after the service: `private readonly IInfoBarService _infoBar;`, `private readonly IStatusService _status;`, `private readonly INavigationService _navigation;` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`)
- Page-lifecycle guards use short flags: `_loading` (suppress change-handlers while hydrating), `_busy`/`IsBusy` (reentrancy lock) (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Test locals favor descriptive nouns: `changed`, `received`, `notifications`, `storage`, `service` (`src/AppTemplate.Tests/ViewModelTests.cs`, `src/AppTemplate.Tests/ServicesTests.cs`)

**Types:**
- Interfaces always carry an `I` prefix and live in the same file as (or next to) their default implementation under `Services/`: `ISettingsService`/`SettingsService`, `IThemeService`/`ThemeService`, `IInfoBarService`/`InfoBarService` (`src/AppTemplate.Framework/Services/ISettingsService.cs`, `src/AppTemplate.Framework/ServiceCollectionExtensions.cs`)
- View models are `public partial class <Name>ViewModel : ViewModelBase, INavigationAware`; item/tile models are `public sealed partial class <Name> : ObservableObject` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Pages are `public sealed partial class <Name>Page : Page`; WinUI service impls are `sealed` (`src/AppTemplate.App/Views/HomePage.xaml.cs`, `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`)
- Tweak operations derive from `TweakAction` with `sealed` leaf types and static stateless `*Actions` facades: `RegistrySetAction`, `RunProcessAction`, `GamingActions` (`src/AppTemplate.App/Tweaks/TweakAction.cs`)
- Value equality uses `record` / `record struct`: `NavigationEntry`, `ThemeChangedMessage`, `NavigationRequestedMessage` (`src/AppTemplate.Framework/Navigation/NavigationEntry.cs`, `src/AppTemplate.Framework/Messaging/Messages.cs`)
- Test doubles are `private sealed` / `internal sealed` helpers at the bottom of the test file: `TestViewModel`, `TestValidatableViewModel`, `UserProfile`, `MemorySettingsStorage` usage (`src/AppTemplate.Tests/ViewModelTests.cs`, `src/AppTemplate.Tests/SettingsServiceTests.cs`)

## Code Style

**Formatting:**
- No `.editorconfig`, no StyleCop/Roslyn-analyzer packages, no Prettier/ESLint (C# repo). Formatting is by convention, not enforced by a config file
- Language/build baselines set in `Directory.Build.props`: `TargetFramework net10.0-windows10.0.26100.0`, `LangVersion latest`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors false`; SDK pinned in `global.json` (`10.0.400`, `rollForward latestMinor`)
- Indent 4 spaces, Allman braces, one statement per line; expression-bodied members only for trivial one-liners (`=> func(value)`, `=> value is true ? ... : ...`) (`src/AppTemplate.Framework/ObjectExtensions.cs`, `src/AppTemplate.Framework/Converters/BooleanToVisibilityConverter.cs`)
- Primary constructors are used for small immutable carriers: `public sealed class RegistrySetAction(string path, string name, ...) : TweakAction`, `public sealed class CpSectionGroup(string title)` (`src/AppTemplate.App/Tweaks/TweakAction.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Collection expressions (`[]`, `[1, 2, 3]`) are preferred over `new List<T> { ... }` for literals and empties: `public ObservableCollection<HomeCard> Tools { get; } = [];`, `collection.AddRange([1, 2, 3])` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.Tests/CollectionsTests.cs`)

**Linting:**
- No lint gate. The build gate is `build-and-run.ps1`, which requires `Errors: 0 Warnings: 0` — treat compiler warnings as failures even though `TreatWarningsAsErrors` is `false` in `Directory.Build.props`
- Kill `AkariToolbox.App` before rebuilding (the script does this) or the apphost file lock fails the build

## Import Organization

**Order:**
1. `System.*` namespaces first
2. Third-party packages (`CommunityToolkit.*`, `Microsoft.*`)
3. Repo namespaces (`AppTemplate.Framework.*`, `AkariToolbox.*`)
4. Example (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`):
```csharp
using System.Collections.ObjectModel;
using AppTemplate.Framework.Services;
using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
```

**Path Aliases:**
- No `using` aliases and no `global using` files. `ImplicitUsings` is enabled (per `Directory.Build.props`), so `System`, `System.Threading.Tasks`, `System.IO`, `System.Collections.Generic` and similar are never imported explicitly — do not add them
- All namespaces are file-scoped: `namespace AkariToolbox.ViewModels;` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`), `namespace AppTemplate.Framework.Converters;` (`src/AppTemplate.Framework/Converters/BooleanToVisibilityConverter.cs`), `namespace AppTemplate.Tests;` (every file in `src/AppTemplate.Tests/`)

## Error Handling

**Patterns:**
- Guard every public entry point with `ArgumentNullException.ThrowIfNull(...)` or `?? throw new ArgumentNullException(nameof(...))`, and range-check numerics explicitly. Do this in constructors, `SetFrame`, `NavigateTo`, collection mutators, provider constructors:
```csharp
// src/AppTemplate.Framework/Navigation/FrameNavigationService.cs
_pageFactory = pageFactory ?? throw new ArgumentNullException(nameof(pageFactory));
ArgumentNullException.ThrowIfNull(pageType);
if (!typeof(Page).IsAssignableFrom(pageType))
    throw new ArgumentException($"{pageType} is not a Page.", nameof(pageType));
```
```csharp
// src/AppTemplate.Framework/Collections/IncrementalLoadingCollection.cs
_loadMore = loadMore ?? throw new ArgumentNullException(nameof(loadMore));
_pageSize = pageSize > 0 ? pageSize : throw new ArgumentOutOfRangeException(nameof(pageSize));
```
- Never let a tweak or background probe crash navigation. Wrap native/registry/WMI reads in `Safe`/`SafeNullable` helpers that return a fallback, and wrap every apply in `try/catch (Exception ex)` that reports through the InfoBar and rolls state back:
```csharp
// src/AppTemplate.App/ViewModels/GamingViewModel.cs
private async void ApplyToggle(string title, bool target, Action<bool> apply,
    Func<bool> read, Action<bool> rollback, string note)
{
    if (IsBusy) return;
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
```
- Converters throw `NotSupportedException` from `ConvertBack` when the direction is meaningless (`src/AppTemplate.Framework/Converters/DateToStringConverter.cs`, `src/AppTemplate.Framework/Converters/MathConverter.cs`, `src/AppTemplate.Framework/Converters/NullToVisibilityConverter.cs`); only parse-tolerant paths swallow (`EnumToBooleanConverter` catches `ArgumentException` and falls through to `UnsetValue`)
- Corrupt persisted data degrades to defaults, never throws: `SettingsService.GetAsync` catches `JsonException` and returns `defaultValue`; `FileSettingsStorage` returns `null` on corrupt/missing files (`src/AppTemplate.Framework/Services/ISettingsService.cs`)
- Logging and shutdown paths must never throw: `FileLoggerProvider.Write` / `Prune` swallow and reset the writer; `App.Shutdown`, `OnUnobservedTaskException` (`e.SetObserved()`), and the `OnUnhandledException` dialog path all have empty `catch {}` fallbacks (`src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`, `src/AppTemplate.App/App.xaml.cs`)
- App-startup safe-boot flows catch per-flow and show a native message box, then `Environment.Exit(0)` (`src/AppTemplate.App/App.xaml.cs`):
```csharp
try { DriverActions.FinishDdu(auto); }
catch (Exception ex) { NativeMessageBox.ShowWarning(ex.Message, "DDU Clean"); }
```

## Logging

**Framework:** `Microsoft.Extensions.Logging` (`ILogger<T>`) with Debug + a custom rolling-file provider (`FileLoggerProvider`). No Serilog/NLog.

**Patterns:**
- Resolve `ILogger<App>` from DI at the failure site; never store a static logger: `Services?.GetService<ILogger<App>>()?.LogError(e.Exception, "Unhandled application exception")` (`src/AppTemplate.App/App.xaml.cs`)
- Log with message templates (`LogInformation("Hello {Name}", "World")`), not interpolation; log exceptions as the first arg (`LogError(ex, "Failed to do thing")`) (`src/AppTemplate.Tests/LoggingTests.cs`, `src/AppTemplate.App/App.xaml.cs`)
- Line format is `{timestamp:yyyy-MM-dd HH:mm:ss.fff} [{LEVEL}] {Category}: {message}` plus a second line with the full `exception.ToString()` (type + message + stack) when present (`src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`)
- User-facing operation feedback goes to `IInfoBarService.Show(...)` / `IStatusService.Start(...)/Complete(...)`, not to the log. Every VM run site pairs them: success → `Show("{title}  applied", note)`; failure → `Show("{title}  failed", ex.Message)` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Files roll per day (`app-yyyyMMdd.log`, then `.1`, `.2`, … past the size cap, default 1 MiB / 10 files) under `%LocalAppData%\AkariToolbox\logs`; do not change the naming scheme (`src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`, `src/AppTemplate.App/App.xaml.cs`)

## Comments

**When to Comment:**
- Every public type and member gets a `/// <summary>` — one line for obvious members, `<para>` blocks for lifetime/threading contracts (`FileLoggerProvider`, `FrameNavigationService`, `ViewModelBase`, `ValidatableViewModelBase`)
- Explain *why*, especially WPF→WinUI port constraints, directly above the code: `// Safe-boot re-entry flows MUST be checked before single-instance registration`, `// Reads are slow — refresh off-thread so navigation never stutters`, `// Roll the box back so it never lies about the real state` (`src/AppTemplate.App/App.xaml.cs`, `src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Use `// ---- Section dividers ----` with `//` comment blocks to group toggles / dropdowns / button grids inside large VMs (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Document `x:Bind` workarounds where they bite: item commands live on the item class because `DataTemplate` has no `RelativeSource AncestorType=Page`; page-level `x:Bind` cannot resolve its root type (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `OPENCODE.md`)

**JSDoc/TSDoc:**
- C# XML doc comments. Use `<see cref="..."/>` and `<c>...</c>` liberally to cross-reference the related action/catalog/VM (`GamingViewModel` references `GamingActions` and `GraphicsActions`; `TweakAction` documents the `reg add` equivalence per factory)
- Keep `<param name="...">` text on helpers whose units are non-obvious (registry roots, hex formats, `maxFileSizeBytes`)

## Function Design

**Size:** Keep methods short and single-purpose. Large VMs stay readable by splitting one concern per partial file (`WindowsActions.cs` + `WindowsActions.System.cs`) and one concern per `partial void OnXChanged` handler rather than a mega-apply method. `TweakAction.Apply()` implementations are 3–6 lines each (`src/AppTemplate.App/Tweaks/TweakAction.cs`).

**Parameters:** Prefer explicit small parameter lists; bundle only when the tuple is the domain concept (`(RegistryKey baseKey, string subPath) Resolve(string fullPath)`). Pass `read`/`rollback` delegates into shared helpers (`ApplyToggle(title, target, apply, read, rollback, note)`) instead of duplicating try/catch per toggle (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`). Async work takes no `CancellationToken` today — long applies run to completion under the `IsBusy` lock.

**Return Values:** Return `Task` (not `void`) from commands except fire-and-forget `async void` toggle bridges that are reentrancy-guarded (`ApplyToggle` is `async void`; `ApplyRowAsync`/`ApplyControlPanelAsync` return `Task`). Return `bool` from validators (`ValidateAll()`), `string?` from `GetError(propertyName)` (null = valid), `bool?` from live-state row probes (null = unknown → persisted fallback) (`src/AppTemplate.Framework/ViewModels/ValidatableViewModelBase.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`).

## Module Design

**Exports:** One namespace per folder, matching the folder path: `AkariToolbox.ViewModels`, `AkariToolbox.Views`, `AkariToolbox.Tweaks`, `AkariToolbox.Services`, `AppTemplate.Framework.{Converters,Collections,Logging,Messaging,Navigation,Services,ViewModels}`. Framework namespaces use the `AppTemplate.*` root; app namespaces use the `AkariToolbox.*` root even though the folders live under `src/AppTemplate.App/`.

**Barrel Files:** None — no barrel/`index` re-export files. Import the defining namespace directly (e.g. `using AppTemplate.Framework.Services;` for `IInfoBarService`).

## MVVM Toolkit Source-Generator Idioms (binding)

- Mark every view model and item class `partial` and put `using CommunityToolkit.Mvvm.ComponentModel;` + `using CommunityToolkit.Mvvm.Input;` at the top. Without `partial`, `[ObservableProperty]` / `[RelayCommand]` silently do nothing useful
- Declare state as `private` backing fields with `[ObservableProperty]`; never hand-write the property unless custom logic is needed (the one exception is `TweaksViewModel.IsBusy`, hand-written over `_busy` because it fans out via `SetRowsBusy`):
```csharp
// src/AppTemplate.App/ViewModels/GamingViewModel.cs
[ObservableProperty]
private bool _disablePreemption;   // generates DisablePreemption + DisablePreemptionChanged partial
```
- Declare actions as `private` methods with `[RelayCommand]`; `CreateRestorePointAsync` generates `CreateRestorePointCommand`, `OpenCard` generates `OpenCardCommand` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`)
- Mirror derived UI state in `On<Prop>Changed` partials and raise dependents manually: `partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));`, `partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Header));` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Guard every generated-property change handler with `if (_loading) return;` during `OnNavigatedTo` hydration, plus bounds checks for index handlers (`if (_loading || value < 0 || ...) return;`)

## XAML Binding Conventions

- Page roots use `{Binding}` (page-level `x:Bind` cannot resolve its root type — WMC9999); use `x:Bind` only inside `DataTemplate` with `x:DataType`. Item commands live on the item class (`HomeCard.OpenCardCommand`, `GamingButton.RunToolCommand`, `CpRowItem.ApplyCommand`) — never `RelativeSource AncestorType` (`OPENCODE.md`, `src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Pages expose a typed `ViewModel` property plus `DataContext = ViewModel`, and a `Strings` accessor for `x:Bind` function bindings (`src/AppTemplate.App/Views/HomePage.xaml.cs`)
- `x:Bind` has no negation — use the `InvertedBooleanConverter` / `InvertedBooleanToVisibilityConverter` (`src/AppTemplate.Framework/Converters/InvertedBooleanConverter.cs`). Converters cannot be used with `x:Bind` on `MainWindow` (Window is not a `FrameworkElement`) — use `x:Bind` function bindings instead (`OPENCODE.md`)
- Layout uses native WinUI styling only: system theme resources, native text styles, `Spacing`/`ColumnSpacing`; no custom theme dictionaries, no hardcoded colors. `ToggleSwitch` rows set `MinWidth="0"`; tile grids use fixed `ItemWidth` (`OPENCODE.md`)

---

*Convention analysis: 2026-10-03*
