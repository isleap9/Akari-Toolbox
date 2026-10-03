<!-- GSD:project-start source:PROJECT.md -->

## Project

**Akari Toolbox — Gaming Optimizer Push**

Akari Toolbox is a WinUI 3 desktop app that applies the FR33THY Ultimate Windows optimization set through native registry / service / driver / WMI operations (no scripts or bundled binaries). This project pushes it further: a Winhance-style, gaming-focused optimizer — searchable categories with per-tweak explanations — built to be the most powerful safe Windows gaming optimization tool.

**Core Value:** Gamers get measurably smoother, lower-latency play from a one-pass safe tune where every tweak is explained, visible, and reversible.

### Constraints

- **Platform**: Windows 10/11 x64, administrator required — every tweak writes HKLM / services / drivers
- **Packaging**: Unpackaged (`WindowsPackageType=None`), self-contained (`WindowsAppSDKSelfContained=true`)
- **Stack**: .NET 10 (`net10.0-windows10.0.26100.0`), Windows App SDK 2.3.1, CommunityToolkit.Mvvm 8.4.2 — versions pinned in `Directory.Packages.props`
- **Safety**: Fully reversible line — restore point + per-tweak rollback; never break updates, Store, or core drivers
- **Native ops only**: No scripts or bundled binaries ever executed
- **Build bar**: `Errors: 0 Warnings: 0`; kill `AkariToolbox.App` before rebuild (apphost lock)
- **XAML**: Page-level `{Binding}` (page-level x:Bind dies with WMC9999); x:Bind only in DataTemplate; `SystemInfo` WMI reads must dispose collections + objects

<!-- GSD:project-end -->

<!-- GSD:stack-start source:codebase/STACK.md -->

## Technology Stack

## Languages

- C# (latest LangVersion, nullable + implicit usings enabled) - all app, framework, and test code in `src/AppTemplate.App/`, `src/AppTemplate.Framework/`, `src/AppTemplate.Tests/`
- XAML (WinUI 3 / Windows App SDK dialect) - all views in `src/AppTemplate.App/Views/*.xaml`, `src/AppTemplate.App/Views/Shared/*.xaml`, `src/AppTemplate.App/MainWindow.xaml`, `src/AppTemplate.App/App.xaml`
- PowerShell 5.1/7 (build/ops scripts only, never executed by the app at runtime except via generated `powershell.exe` invocations) - `build-and-run.ps1`, `check-parity.ps1`, `verify-pages.ps1`, `shot-pages.ps1`, `port-windows-page.ps1`
- MSBuild XML (project / props / manifest configuration) - `Directory.Build.props`, `Directory.Packages.props`, `src/AppTemplate.App/AppTemplate.App.csproj`, `src/AppTemplate.Framework/AppTemplate.Framework.csproj`, `src/AppTemplate.Tests/AppTemplate.Tests.csproj`, `src/AppTemplate.App/app.manifest`
- JSON (settings + NuGet/dev config) - runtime `%LOCALAPPDATA%\AkariToolbox\settings.json` (see `src/AppTemplate.Framework/Services/FileSettingsStorage.cs`), `global.json`, `nuget.config`
- Embedded data blobs treated as text resources (`.reg`, `.xml`, `.nip`, GPU profiles) - `src/AppTemplate.App/Tweaks/Data/**/*` loaded via `src/AppTemplate.App/Tweaks/NativeOps.cs` (`LoadWindowsData` / `LoadGraphicsData` / `LoadAdvancedData`)

## Runtime

- .NET SDK 10.0.400 pinned via `global.json` (`rollForward: latestMinor`, `allowPrerelease: false`)
- Target framework `net10.0-windows10.0.26100.0` declared in `Directory.Build.props`
- Windows-only: `UseWinUI=true`, `TargetFramework` is Windows-specific, `RuntimeIdentifier` `win-x64` / `win-x86` / `win-arm64` (platform-conditional in `Directory.Build.props`); app project pins `PlatformTarget=x64` + `RuntimeIdentifier=win-x64` in `src/AppTemplate.App/AppTemplate.App.csproj`
- Requires Windows 10/11 x64 and elevation (`requireAdministrator` in `src/AppTemplate.App/app.manifest`)
- NuGet with Central Package Management (`ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true` in `Directory.Packages.props`)
- Single feed: `nuget.org` only (`nuget.config` clears then adds `https://api.nuget.org/v3/index.json`)
- Lockfile: missing (no `packages.lock.json` committed; restore is floating within centrally pinned versions)

## Frameworks

- Windows App SDK 2.3.1 (`Microsoft.WindowsAppSDK`) - WinUI 3 XAML runtime, windowing, imaging (`Windows.Graphics.Imaging` used in `src/AppTemplate.App/Tweaks/NativeOps.cs`), app lifecycle (`Microsoft.Windows.AppLifecycle` single-instance in `src/AppTemplate.App/App.xaml.cs`)
- Windows SDK Build Tools 10.0.26100.4654 (`Microsoft.Windows.SDK.BuildTools`, MSIX variant 1.7.251221100) - XAML compiler / packaging tooling (unpackaged mode, `WindowsPackageType=None`)
- CommunityToolkit.Mvvm 8.4.2 - `[ObservableProperty]` / `[RelayCommand]` MVVM in all `src/AppTemplate.App/ViewModels/*.cs` and `src/AppTemplate.Framework/ViewModels/*.cs`
- Microsoft.Extensions.Hosting 10.0.11 + DependencyInjection 10.0.11 (+ Abstractions) - generic host built in `src/AppTemplate.App/App.xaml.cs` (`BuildHost()`), DI registrations for all pages/view models/services/navigation
- Microsoft.Extensions.Logging 10.0.11 (+ Abstractions, Debug provider) + custom `FileLoggerProvider` in `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs` - daily rolling file logs under `%LOCALAPPDATA%\AkariToolbox\logs\`
- Microsoft.Extensions.Configuration 10.0.11 + Json 10.0.11 - host configuration pipeline (declared centrally in `Directory.Packages.props`; hosting pulls it in transitively)
- Microsoft.Xaml.Behaviors.WinUI.Managed 3.0.1 - `InvokeCommandOnLoadedBehavior`, `ListViewScrollIntoViewBehavior`, `TextBoxSelectAllOnFocusBehavior` in `src/AppTemplate.Framework/Behaviors/*.cs`
- System.Management 10.0.12 - WMI reads (`Win32_OperatingSystem`, `Win32_Processor`, `Win32_VideoController`, `Win32_ComputerSystem`) in `src/AppTemplate.App/Tweaks/SystemInfo.cs` and `src/AppTemplate.App/Tweaks/WindowsActions.System.cs`
- xUnit 2.9.3 + `xunit.runner.visualstudio` 3.1.4 + `Microsoft.NET.Test.Sdk` 17.14.1 - test runner for `src/AppTemplate.Tests/`
- Moq 4.20.72 - mocking `IDialogService`, `ISettingsService`, etc. in `src/AppTemplate.Tests/`
- MSBuild / `dotnet` CLI (SDK 10.0.400) - `dotnet build src/AppTemplate.App/AppTemplate.App.csproj -c Debug|Release`
- Custom XAML workaround target `SetAkariXamlLocalAssembly` (Before `MarkupCompilePass2`) in `src/AppTemplate.App/AppTemplate.App.csproj` - fixes page-level `x:Bind` / `x:DataType` WMC9999 resolution
- PowerShell helper scripts: `build-and-run.ps1` (kill apphost lock, build with zero-error/zero-warning gate, launch), `verify-pages.ps1` (UIA click-through of all 13 nav items), `check-parity.ps1` (WPF text parity diff), `port-windows-page.ps1`, `shot-pages.ps1`

## Key Dependencies

- `Microsoft.WindowsAppSDK` 2.3.1 - entire UI + `AppInstance.FindOrRegisterForKey("AkariToolbox")` single-instance + `WindowNative.GetWindowHandle` in `src/AppTemplate.App/App.xaml.cs`
- `CommunityToolkit.Mvvm` 8.4.2 - source-generated observable properties/commands; `NoWarn` includes `MVVMTK0045` in `src/AppTemplate.App/AppTemplate.App.csproj` (self-contained, not AOT)
- `Microsoft.Extensions.Hosting` 10.0.11 - composes `AddMvvmFramework()` (see `src/AppTemplate.Framework/ServiceCollectionExtensions.cs`) + app-level registrations in `src/AppTemplate.App/App.xaml.cs`
- `System.Management` 10.0.12 - hardware identity and This-PC card; disposal discipline is load-bearing (see `src/AppTemplate.App/Tweaks/SystemInfo.cs`)
- `Microsoft.Extensions.Logging.Debug` 10.0.11 - debug-trace sink alongside file logger in `src/AppTemplate.App/App.xaml.cs`
- `Microsoft.Extensions.Configuration.Json` 10.0.11 - host config stage (no `appsettings.json` shipped; settings persist via `FileSettingsStorage` JSON instead)
- `Microsoft.Xaml.Behaviors.WinUI.Managed` 3.0.1 - XAML behaviors wired in views
- `Microsoft.Windows.SDK.BuildTools` 10.0.26100.4654 - build-time XAML compilation
- `System.Text.Json` (in-box .NET) - `settings.json` serialization in `src/AppTemplate.Framework/Services/FileSettingsStorage.cs`, NVIDIA driver lookup payload in `src/AppTemplate.App/Tweaks/DriverActions.cs`
- `System.Net.Http` (`HttpClient`, in-box) - singleton `Http` in `src/AppTemplate.App/Tweaks/NativeOps.cs` (`Download`, `HttpGetString`, `DownloadWithHeaders`, 10-min timeout)

## Configuration

- No `.env` files, no user secrets, no cloud config. Existence check: no `.env*`, `credentials.*`, or `secrets/` present in repo root.
- Runtime settings file created on first run: `%LOCALAPPDATA%\AkariToolbox\settings.json` via `src/AppTemplate.Framework/Services/FileSettingsStorage.cs` (key/value string dictionary, atomic temp-file + move writes). Instantiated as `new FileSettingsStorage("AkariToolbox")` in `src/AppTemplate.App/App.xaml.cs`.
- Log directory created on first run: `%LOCALAPPDATA%\AkariToolbox\logs\` via `FileLoggerProvider` in `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs` (1 MB cap per file, newest 10 kept, `app-yyyyMMdd.log` naming).
- Manifest elevation: `src/AppTemplate.App/app.manifest` sets `requestedExecutionLevel level="requireAdministrator"`; all file pickers therefore use Win32 `GetOpenFileNameW` (`src/AppTemplate.App/Tweaks/NativeOps.cs` `PickFile`) / `Microsoft.Win32.OpenFileDialog` instead of WinUI 3 pickers.
- Packaging: `WindowsPackageType=None` + `WindowsAppSDKSelfContained=true` (unpackaged, self-contained, no Windows App Runtime install needed) in `src/AppTemplate.App/AppTemplate.App.csproj`.
- `global.json` - SDK pin (10.0.400)
- `Directory.Build.props` - TFM, LangVersion, nullable, WinUI flag, Windows SDK version, platform/RID mapping, default `x64`
- `Directory.Packages.props` - all NuGet versions centrally (WindowsAppSDK, BuildTools, MVVM, Extensions, Behaviors, xunit/Moq/Test SDK, System.Management)
- `nuget.config` - clears feeds, restores from nuget.org only
- `AppTemplate.slnx` - solution listing `src/AppTemplate.App`, `src/AppTemplate.Framework`, `src/AppTemplate.Tests`
- `src/AppTemplate.App/app.manifest` - elevation + Win10/11 compatibility + PerMonitorV2 DPI
- `src/AppTemplate.App/AppTemplate.App.csproj` - `WinExe`, assembly `AkariToolbox.App`, namespace `AkariToolbox.*`, explicit `Compile`/`Page` globs (no default items), `EmbeddedResource` for `Tweaks/Data/**/*`, XAML local-assembly workaround target

## Platform Requirements

- Windows 10/11 x64 with .NET 10.0.400 SDK installed
- Ability to run elevated builds (app manifest requires admin; apphost file lock requires kill-before-rebuild, handled by `build-and-run.ps1`)
- NuGet access to `https://api.nuget.org/v3/index.json`
- PowerShell for `build-and-run.ps1` / `verify-pages.ps1` / `check-parity.ps1`
- Build gate is `Errors: 0 Warnings: 0` (XAML diagnostics parsed from build log, not just MSBuild exit code)
- Windows 10/11 x64, administrator privileges (every tweak writes HKLM / services / drivers / WMI)
- Unpackaged self-contained single exe: `src/AppTemplate.App/bin/x64/Debug|Release/net10.0-windows10.0.26100.0/win-x64/AkariToolbox.App.exe` (display name "Akari Toolbox")
- No external runtime install (Windows App SDK self-contained), no installer, no MSIX
- Writes only to: Windows Registry (HKLM/HKCU), `%SystemRoot%\Temp` staging, `%LOCALAPPDATA%\AkariToolbox\` (settings + logs), Start Menu / Desktop shortcuts

<!-- GSD:stack-end -->

<!-- GSD:conventions-start source:CONVENTIONS.md -->

## Conventions

## Naming Patterns

- One public type per file; file name matches the type: `src/AppTemplate.Framework/Navigation/FrameNavigationService.cs` → `FrameNavigationService`
- Partial-class splits use a `.Suffix` segment: `src/AppTemplate.App/Tweaks/WindowsActions.System.cs`, `src/AppTemplate.App/Tweaks/AdvancedActions.Services.cs`, `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs`, `src/AppTemplate.App/Tweaks/GraphicsActions.Nip.cs`
- Test files are `<Area>Tests.cs` in `src/AppTemplate.Tests/`: `ViewModelTests.cs`, `ServicesTests.cs`, `ConvertersTests.cs`, `NavigationTests.cs`, `MessagingTests.cs`, `LoggingTests.cs`, `SettingsServiceTests.cs`, `FileSettingsStorageTests.cs`, `CollectionsTests.cs`, `ObjectExtensionsTests.cs`
- Views are `<Name>Page.xaml` + `<Name>Page.xaml.cs` in `src/AppTemplate.App/Views/`; view models are `<Name>ViewModel.cs` in `src/AppTemplate.App/ViewModels/` (e.g. `GamingPage.xaml.cs` ↔ `GamingViewModel.cs`)
- Tweak ports are `<Area>Actions.cs` statics in `src/AppTemplate.App/Tweaks/` (e.g. `GamingActions.cs`, `GraphicsActions.cs`, `CheckActions.cs`); catalog data is `AkariTweakCatalog.cs` / `ControlPanelRows.cs`
- Use `PascalCase` for all methods, including private ones: `ReadControlPanelIndex()`, `ApplyControlPanelAsync()`, `BuildButtons()`, `RefreshFromSystem()` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Use an `Async` suffix for awaitable methods: `CreateRestorePointAsync()`, `ApplyRowAsync()`, `ApplyControlPanelAsync()`, `LoadMoreItemsAsync()` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.Framework/Collections/IncrementalLoadingCollection.cs`)
- Use `On<Property>Changed` partials for generated-property side effects: `OnIsBusyChanged(bool value)`, `OnDisablePreemptionChanged(bool value)`, `OnControlPanelIndexChanged(int value)` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Use `Safe` / `SafeNullable` wrappers for fallible live-state probes: `Safe(SystemInfo.OsSummary)`, `SafeNullable(GamingActions.ReadSvcHost)` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Test methods read as `UnitUnderTest_condition_expectedOutcome` in `snake_case` segments: `ViewModelBase_title_raises_property_changed`, `SettingsService_returns_default_on_corrupt_json`, `RangeObservableCollection_AddRange_raises_single_notification` (`src/AppTemplate.Tests/ViewModelTests.cs`, `src/AppTemplate.Tests/SettingsServiceTests.cs`, `src/AppTemplate.Tests/CollectionsTests.cs`)
- Backing fields for `[ObservableProperty]` are `_camelCase` and generate a `PascalCase` property: `[ObservableProperty] private bool _isBusy;` → `IsBusy` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Constructor-injected dependencies go to `private readonly` fields named after the service: `private readonly IInfoBarService _infoBar;`, `private readonly IStatusService _status;`, `private readonly INavigationService _navigation;` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`)
- Page-lifecycle guards use short flags: `_loading` (suppress change-handlers while hydrating), `_busy`/`IsBusy` (reentrancy lock) (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Test locals favor descriptive nouns: `changed`, `received`, `notifications`, `storage`, `service` (`src/AppTemplate.Tests/ViewModelTests.cs`, `src/AppTemplate.Tests/ServicesTests.cs`)
- Interfaces always carry an `I` prefix and live in the same file as (or next to) their default implementation under `Services/`: `ISettingsService`/`SettingsService`, `IThemeService`/`ThemeService`, `IInfoBarService`/`InfoBarService` (`src/AppTemplate.Framework/Services/ISettingsService.cs`, `src/AppTemplate.Framework/ServiceCollectionExtensions.cs`)
- View models are `public partial class <Name>ViewModel : ViewModelBase, INavigationAware`; item/tile models are `public sealed partial class <Name> : ObservableObject` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Pages are `public sealed partial class <Name>Page : Page`; WinUI service impls are `sealed` (`src/AppTemplate.App/Views/HomePage.xaml.cs`, `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`)
- Tweak operations derive from `TweakAction` with `sealed` leaf types and static stateless `*Actions` facades: `RegistrySetAction`, `RunProcessAction`, `GamingActions` (`src/AppTemplate.App/Tweaks/TweakAction.cs`)
- Value equality uses `record` / `record struct`: `NavigationEntry`, `ThemeChangedMessage`, `NavigationRequestedMessage` (`src/AppTemplate.Framework/Navigation/NavigationEntry.cs`, `src/AppTemplate.Framework/Messaging/Messages.cs`)
- Test doubles are `private sealed` / `internal sealed` helpers at the bottom of the test file: `TestViewModel`, `TestValidatableViewModel`, `UserProfile`, `MemorySettingsStorage` usage (`src/AppTemplate.Tests/ViewModelTests.cs`, `src/AppTemplate.Tests/SettingsServiceTests.cs`)

## Code Style

- No `.editorconfig`, no StyleCop/Roslyn-analyzer packages, no Prettier/ESLint (C# repo). Formatting is by convention, not enforced by a config file
- Language/build baselines set in `Directory.Build.props`: `TargetFramework net10.0-windows10.0.26100.0`, `LangVersion latest`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors false`; SDK pinned in `global.json` (`10.0.400`, `rollForward latestMinor`)
- Indent 4 spaces, Allman braces, one statement per line; expression-bodied members only for trivial one-liners (`=> func(value)`, `=> value is true ? ... : ...`) (`src/AppTemplate.Framework/ObjectExtensions.cs`, `src/AppTemplate.Framework/Converters/BooleanToVisibilityConverter.cs`)
- Primary constructors are used for small immutable carriers: `public sealed class RegistrySetAction(string path, string name, ...) : TweakAction`, `public sealed class CpSectionGroup(string title)` (`src/AppTemplate.App/Tweaks/TweakAction.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Collection expressions (`[]`, `[1, 2, 3]`) are preferred over `new List<T> { ... }` for literals and empties: `public ObservableCollection<HomeCard> Tools { get; } = [];`, `collection.AddRange([1, 2, 3])` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.Tests/CollectionsTests.cs`)
- No lint gate. The build gate is `build-and-run.ps1`, which requires `Errors: 0 Warnings: 0` — treat compiler warnings as failures even though `TreatWarningsAsErrors` is `false` in `Directory.Build.props`
- Kill `AkariToolbox.App` before rebuilding (the script does this) or the apphost file lock fails the build

## Import Organization

- No `using` aliases and no `global using` files. `ImplicitUsings` is enabled (per `Directory.Build.props`), so `System`, `System.Threading.Tasks`, `System.IO`, `System.Collections.Generic` and similar are never imported explicitly — do not add them
- All namespaces are file-scoped: `namespace AkariToolbox.ViewModels;` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`), `namespace AppTemplate.Framework.Converters;` (`src/AppTemplate.Framework/Converters/BooleanToVisibilityConverter.cs`), `namespace AppTemplate.Tests;` (every file in `src/AppTemplate.Tests/`)

## Error Handling

- Guard every public entry point with `ArgumentNullException.ThrowIfNull(...)` or `?? throw new ArgumentNullException(nameof(...))`, and range-check numerics explicitly. Do this in constructors, `SetFrame`, `NavigateTo`, collection mutators, provider constructors:
- Never let a tweak or background probe crash navigation. Wrap native/registry/WMI reads in `Safe`/`SafeNullable` helpers that return a fallback, and wrap every apply in `try/catch (Exception ex)` that reports through the InfoBar and rolls state back:
- Converters throw `NotSupportedException` from `ConvertBack` when the direction is meaningless (`src/AppTemplate.Framework/Converters/DateToStringConverter.cs`, `src/AppTemplate.Framework/Converters/MathConverter.cs`, `src/AppTemplate.Framework/Converters/NullToVisibilityConverter.cs`); only parse-tolerant paths swallow (`EnumToBooleanConverter` catches `ArgumentException` and falls through to `UnsetValue`)
- Corrupt persisted data degrades to defaults, never throws: `SettingsService.GetAsync` catches `JsonException` and returns `defaultValue`; `FileSettingsStorage` returns `null` on corrupt/missing files (`src/AppTemplate.Framework/Services/ISettingsService.cs`)
- Logging and shutdown paths must never throw: `FileLoggerProvider.Write` / `Prune` swallow and reset the writer; `App.Shutdown`, `OnUnobservedTaskException` (`e.SetObserved()`), and the `OnUnhandledException` dialog path all have empty `catch {}` fallbacks (`src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`, `src/AppTemplate.App/App.xaml.cs`)
- App-startup safe-boot flows catch per-flow and show a native message box, then `Environment.Exit(0)` (`src/AppTemplate.App/App.xaml.cs`):

## Logging

- Resolve `ILogger<App>` from DI at the failure site; never store a static logger: `Services?.GetService<ILogger<App>>()?.LogError(e.Exception, "Unhandled application exception")` (`src/AppTemplate.App/App.xaml.cs`)
- Log with message templates (`LogInformation("Hello {Name}", "World")`), not interpolation; log exceptions as the first arg (`LogError(ex, "Failed to do thing")`) (`src/AppTemplate.Tests/LoggingTests.cs`, `src/AppTemplate.App/App.xaml.cs`)
- Line format is `{timestamp:yyyy-MM-dd HH:mm:ss.fff} [{LEVEL}] {Category}: {message}` plus a second line with the full `exception.ToString()` (type + message + stack) when present (`src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`)
- User-facing operation feedback goes to `IInfoBarService.Show(...)` / `IStatusService.Start(...)/Complete(...)`, not to the log. Every VM run site pairs them: success → `Show("{title}  applied", note)`; failure → `Show("{title}  failed", ex.Message)` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Files roll per day (`app-yyyyMMdd.log`, then `.1`, `.2`, … past the size cap, default 1 MiB / 10 files) under `%LocalAppData%\AkariToolbox\logs`; do not change the naming scheme (`src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`, `src/AppTemplate.App/App.xaml.cs`)

## Comments

- Every public type and member gets a `/// <summary>` — one line for obvious members, `<para>` blocks for lifetime/threading contracts (`FileLoggerProvider`, `FrameNavigationService`, `ViewModelBase`, `ValidatableViewModelBase`)
- Explain *why*, especially WPF→WinUI port constraints, directly above the code: `// Safe-boot re-entry flows MUST be checked before single-instance registration`, `// Reads are slow — refresh off-thread so navigation never stutters`, `// Roll the box back so it never lies about the real state` (`src/AppTemplate.App/App.xaml.cs`, `src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Use `// ---- Section dividers ----` with `//` comment blocks to group toggles / dropdowns / button grids inside large VMs (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Document `x:Bind` workarounds where they bite: item commands live on the item class because `DataTemplate` has no `RelativeSource AncestorType=Page`; page-level `x:Bind` cannot resolve its root type (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `OPENCODE.md`)
- C# XML doc comments. Use `<see cref="..."/>` and `<c>...</c>` liberally to cross-reference the related action/catalog/VM (`GamingViewModel` references `GamingActions` and `GraphicsActions`; `TweakAction` documents the `reg add` equivalence per factory)
- Keep `<param name="...">` text on helpers whose units are non-obvious (registry roots, hex formats, `maxFileSizeBytes`)

## Function Design

## Module Design

## MVVM Toolkit Source-Generator Idioms (binding)

- Mark every view model and item class `partial` and put `using CommunityToolkit.Mvvm.ComponentModel;` + `using CommunityToolkit.Mvvm.Input;` at the top. Without `partial`, `[ObservableProperty]` / `[RelayCommand]` silently do nothing useful
- Declare state as `private` backing fields with `[ObservableProperty]`; never hand-write the property unless custom logic is needed (the one exception is `TweaksViewModel.IsBusy`, hand-written over `_busy` because it fans out via `SetRowsBusy`):
- Declare actions as `private` methods with `[RelayCommand]`; `CreateRestorePointAsync` generates `CreateRestorePointCommand`, `OpenCard` generates `OpenCardCommand` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`)
- Mirror derived UI state in `On<Prop>Changed` partials and raise dependents manually: `partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));`, `partial void OnIsExpandedChanged(bool value) => OnPropertyChanged(nameof(Header));` (`src/AppTemplate.App/ViewModels/GamingViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`)
- Guard every generated-property change handler with `if (_loading) return;` during `OnNavigatedTo` hydration, plus bounds checks for index handlers (`if (_loading || value < 0 || ...) return;`)

## XAML Binding Conventions

- Page roots use `{Binding}` (page-level `x:Bind` cannot resolve its root type — WMC9999); use `x:Bind` only inside `DataTemplate` with `x:DataType`. Item commands live on the item class (`HomeCard.OpenCardCommand`, `GamingButton.RunToolCommand`, `CpRowItem.ApplyCommand`) — never `RelativeSource AncestorType` (`OPENCODE.md`, `src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/GamingViewModel.cs`)
- Pages expose a typed `ViewModel` property plus `DataContext = ViewModel`, and a `Strings` accessor for `x:Bind` function bindings (`src/AppTemplate.App/Views/HomePage.xaml.cs`)
- `x:Bind` has no negation — use the `InvertedBooleanConverter` / `InvertedBooleanToVisibilityConverter` (`src/AppTemplate.Framework/Converters/InvertedBooleanConverter.cs`). Converters cannot be used with `x:Bind` on `MainWindow` (Window is not a `FrameworkElement`) — use `x:Bind` function bindings instead (`OPENCODE.md`)
- Layout uses native WinUI styling only: system theme resources, native text styles, `Spacing`/`ColumnSpacing`; no custom theme dictionaries, no hardcoded colors. `ToggleSwitch` rows set `MinWidth="0"`; tile grids use fixed `ItemWidth` (`OPENCODE.md`)

<!-- GSD:conventions-end -->

<!-- GSD:architecture-start source:ARCHITECTURE.md -->

## Architecture

## System Overview

```text

```

## Component Responsibilities

| Component | Responsibility | File |
|-----------|----------------|------|
| App composition root | Builds generic `IHost`, registers DI, handles safe-boot headless flows, single-instance, global exception logging, theme/culture init | `src/AppTemplate.App/App.xaml.cs` |
| Shell window | Custom title bar, `NavigationView` (12 `NavItems` + 1 footer item), `ContentFrame`, global `InfoBar` binding, `IStatusService` footer, Mica/theme handling | `src/AppTemplate.App/MainWindow.xaml`, `src/AppTemplate.App/MainWindow.xaml.cs` |
| Navigation entry model | Immutable shell menu row (label, glyph, page type) | `src/AppTemplate.App/NavigationItem.cs` |
| Pages (13) | Declarative XAML layout; thin code-behind resolving VM + `LocalizedStrings` from `App.Services` and setting `DataContext` | `src/AppTemplate.App/Views/*.xaml`, `src/AppTemplate.App/Views/*.xaml.cs` |
| Page view models (13) | `[ObservableProperty]` state + `[RelayCommand]` verbs; own `IsBusy` guard, wrap work in `IStatusService.Start/Complete`, report via `IInfoBarService`/`IDialogService` | `src/AppTemplate.App/ViewModels/*.cs` |
| Tweak action hierarchy | Declarative native ops: `RegDword/Qword/String/ExpandString/Binary/BinaryHex`, `RegDeleteValue/Key`, `Run`, `Launch`, `Open`, `Custom` | `src/AppTemplate.App/Tweaks/TweakAction.cs` |
| Toggle catalog | Live-state toggles with `read`/`apply` delegates + `HKCU\Software\AkariTool` write-through (e.g. 32-item OS catalog, 169 control-panel rows) | `src/AppTemplate.App/Tweaks/TweakToggle.cs`, `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs`, `src/AppTemplate.App/Tweaks/ControlPanelRows.cs`, `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs`, `src/AppTemplate.App/Tweaks/ControlPanelActions.cs` |
| Per-page action modules | Static native ports per destination: Check/Refresh/Setup/Installer/Graphics/Gaming/Windows/Hardware/Advanced/Driver/RestorePoint | `src/AppTemplate.App/Tweaks/CheckActions.cs`, `src/AppTemplate.App/Tweaks/RefreshActions.cs`, `src/AppTemplate.App/Tweaks/SetupActions.cs`, `src/AppTemplate.App/Tweaks/InstallerActions.cs`, `src/AppTemplate.App/Tweaks/GraphicsActions.cs`, `src/AppTemplate.App/Tweaks/GraphicsActions.Nip.cs`, `src/AppTemplate.App/Tweaks/GamingActions.cs`, `src/AppTemplate.App/Tweaks/WindowsActions.cs`, `src/AppTemplate.App/Tweaks/WindowsActions.System.cs`, `src/AppTemplate.App/Tweaks/HardwareActions.cs`, `src/AppTemplate.App/Tweaks/AdvancedActions.cs`, `src/AppTemplate.App/Tweaks/AdvancedActions.Services.cs`, `src/AppTemplate.App/Tweaks/DriverActions.cs`, `src/AppTemplate.App/Tweaks/RestorePointActions.cs` |
| Native helpers | winget install/uninstall, `.lnk` via `WScript.Shell` COM, process launch, path expansion, cached-exe launching | `src/AppTemplate.App/Tweaks/NativeOps.cs` |
| System inventory | WMI `Win32_OperatingSystem/Processor/VideoController/ComputerSystem` + kernel32/DriveInfo reads with per-process caching; must dispose `ManagementObjectCollection` + each `ManagementObject` | `src/AppTemplate.App/Tweaks/SystemInfo.cs` |
| Persisted toggle state | `HKCU\Software\AkariTool` read/write helpers | `src/AppTemplate.App/Tweaks/AkariToolState.cs` |
| Embedded data | GPU profiles and similar payloads copied as `EmbeddedResource` | `src/AppTemplate.App/Tweaks/Data/**` (e.g. `src/AppTemplate.App/Tweaks/Data/Windows/SetTimerResolutionService.cs`) |
| Localization accessor | `ResourceManager`-backed `Get(key)`/indexer + `INotifyPropertyChanged.Refresh()` on culture change for `x:Bind` re-evaluation | `src/AppTemplate.App/Services/LocalizedStrings.cs` |
| Headless dialog fallback | Blocking Win32 `MessageBoxW` for safe-boot flows and `Tweaks` layer (no `XamlRoot` needed) | `src/AppTemplate.App/Helpers/NativeMessageBox.cs` |
| Navigation service | Typed DI navigation over a WinUI `Frame` with own back/forward stacks and `INavigationAware` callbacks | `src/AppTemplate.Framework/Navigation/FrameNavigationService.cs`, `src/AppTemplate.Framework/Navigation/INavigationService.cs`, `src/AppTemplate.Framework/Navigation/NavigationEntry.cs`, `src/AppTemplate.Framework/Navigation/INavigationAware.cs` |
| Framework services | Settings, theme, culture, dialogs, window, file-picker, info-bar, status singletons wired by `AddMvvmFramework` | `src/AppTemplate.Framework/ServiceCollectionExtensions.cs`, `src/AppTemplate.Framework/Services/*.cs` |
| Messaging | `WeakReferenceMessenger.Default` + `ThemeChangedMessage`, `CultureChangedMessage`, `ShowInfoBarMessage`, `NavigationRequestedMessage`, `UserActionMessage` | `src/AppTemplate.Framework/Messaging/Messages.cs` |

## Pattern Overview

- Composition root owns everything: `App.BuildHost()` registers framework singletons, transient VMs/pages, `Func<XamlRoot?>` and `Func<IntPtr>` adapters, and the `INavigationService` page factory (`src/AppTemplate.App/App.xaml.cs:134-185`).
- Pages are DI-created, never `new`'d by XAML: `FrameNavigationService(pageType => (Page)ActivatorUtilities.CreateInstance(sp, pageType))` (`src/AppTemplate.App/App.xaml.cs:177-178`).
- ViewModels never touch UI types: all dialogs go through `IDialogService`, all toasts through `IInfoBarService`, all progress through `IStatusService`, all navigation through `INavigationService`/`NavigationRequestedMessage`.
- Domain logic is static and UI-free: `Tweaks/*Actions.cs` expose `Action`/`Func` units; `TweakAction` subclasses express registry/process/shell ops declaratively; `TweakToggle` pairs a live `read` with an `apply(on/off)`.
- Messenger is the cross-cutting bus: theme changes (`ThemeChangedMessage` → `MainWindow.ApplyTheme`), culture changes (`CultureChangedMessage` → `LocalizedStrings.Refresh`), decoupled navigation (`NavigationRequestedMessage` → `MainWindow` handler).

## Layers

- Purpose: App lifecycle, DI graph, single-instance, headless flows, global error handling
- Location: `src/AppTemplate.App/App.xaml.cs`, `src/AppTemplate.App/MainWindow.xaml(.cs)`, `src/AppTemplate.App/App.xaml`
- Contains: `IHost` builder, `AppInstance.FindOrRegisterForKey("AkariToolbox")`, `--ddu-auto/--defender-*/--services-*` early-exit branches, `UnhandledException`/`AppDomain`/`UnobservedTask` handlers, `FileLoggerProvider` wiring
- Depends on: Framework services, app VMs/pages, `Tweaks` finish-methods
- Used by: OS launcher (entry point); `Views/*.xaml.cs` reach back via `App.Services`
- Purpose: Declarative layout only; code-behind is a 3-line VM + strings resolver
- Location: `src/AppTemplate.App/Views/`
- Contains: 13 `*Page.xaml` + `*Page.xaml.cs` pairs, `src/AppTemplate.App/Views/Shared/PageStyles.xaml`
- Depends on: ViewModels, `Services/LocalizedStrings`, framework converters/behaviors
- Used by: `MainWindow` `ContentFrame` via `INavigationService`
- Canonical code-behind shape (`src/AppTemplate.App/Views/CheckPage.xaml.cs:9-26`): ctor takes typed VM, pulls `LocalizedStrings` from `App.Services`, sets `DataContext = ViewModel`, exposes both as `x:Bind`-able properties.
- Purpose: Bindable state + commands + run orchestration (`IsBusy` + `Start/Complete` + InfoBar/dialog reporting)
- Location: `src/AppTemplate.App/ViewModels/`
- Contains: 13 page VMs, all `ViewModelBase` + `INavigationAware`; `HomeCard`/`CpTabGroup`/`CpSectionGroup`/`CpRowItem` item types live alongside (e.g. `HomeViewModel`, `TweaksViewModel`)
- Depends on: `Tweaks/*Actions`, framework `IInfoBarService`/`IStatusService`/`IDialogService`/`INavigationService`
- Used by: Pages (via `DataContext` + typed `ViewModel` property)
- Canonical run pattern (`src/AppTemplate.App/ViewModels/CheckViewModel.cs:86-109`): guard `IsBusy` → `_status.Start(title)` → `await Task.Run(work)` → guidance dialog + InfoBar → `finally { IsBusy=false; _status.Complete(); }`.
- Purpose: All real Windows mutations as native .NET/Win32 — no scripts, no bundled binaries
- Location: `src/AppTemplate.App/Tweaks/`
- Contains: `TweakAction` hierarchy, `TweakToggle`, per-page `*Actions.cs` statics, `NativeOps`, `SystemInfo`, `AkariToolState`, `ControlPanelRows(.Generated)`, `Tweaks/Data/**`
- Depends on: `Microsoft.Win32.Registry`, `System.Diagnostics.Process`, `System.Management`, `Helpers/NativeMessageBox`
- Used by: ViewModels and headless `App.OnLaunched` finish-methods (`DriverActions.FinishDdu`, `WindowsActions.FinishDefenderOptimize/Default`, `AdvancedActions.FinishDefenderDisable/Enable`, `FinishServicesOff/Default`)
- Purpose: App-agnostic navigation, settings/theme/culture/dialog/window/file-picker/info-bar/status, converters, collections, behaviors, logging, messaging, VM bases
- Location: `src/AppTemplate.Framework/`
- Contains: `Navigation/`, `Services/`, `Messaging/`, `Converters/` (14 converters), `Collections/`, `Behaviors/`, `Logging/FileLoggerProvider.cs`, `ViewModels/ViewModelBase.cs`, `ViewModels/ValidatableViewModelBase.cs`, `ServiceCollectionExtensions.cs`, `ObjectExtensions.cs`
- Depends on: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.*`, WinUI types only at the service-implementation edge
- Used by: App composition root (`AddMvvmFramework`) and all VMs
- Purpose: Localization lookup and Win32 fallback dialogs
- Location: `src/AppTemplate.App/Services/LocalizedStrings.cs`, `src/AppTemplate.App/Helpers/NativeMessageBox.cs`, `src/AppTemplate.App/Resources/Resources.resx`, `src/AppTemplate.App/Resources/Resources.zh-CN.resx`
- Depends on: Embedded `.resx` manifest resources, `user32.dll MessageBoxW`
- Used by: Every page (`Strings` property), every culture/theme change, headless flows

## Data Flow

### Primary Request Path

### Toggle Apply Path (live-state switches)

### Theme / Culture Propagation

- Ephemeral page state lives in VM `[ObservableProperty]` fields; shell-global `InfoBar`/`Status` are singleton services bound with `x:Bind` in `MainWindow.xaml`.
- Persisted state is split: user prefs in `%LOCALAPPDATA%\AkariToolbox\settings.json` (`FileSettingsStorage("AkariToolbox")`), toggle memory in `HKCU\Software\AkariTool` (`AkariToolState`), logs in `%LOCALAPPDATA%\AkariToolbox\logs\` (`FileLoggerProvider`).
- Navigation state is an explicit in-memory back/forward stack inside `FrameNavigationService` (`_backStack`/`_forwardStack`), not the `Frame` journal — `Frame.Content` is assigned directly (`src/AppTemplate.Framework/Navigation/FrameNavigationService.cs:67-70`).

## Key Abstractions

- Purpose: Declarative unit of one native mutation so catalogs read like the scripts they replaced
- Examples: `src/AppTemplate.App/Tweaks/TweakAction.cs`, all `Tweaks/*Actions.cs` call sites
- Pattern: Abstract `Apply()` + static factories (`RegDword`, `RegDeleteKey`, `Run`, `Launch`, `Open`, `Custom`); sealed subclasses `RegistrySetAction`, `RegistryDeleteValueAction`, `RegistryDeleteKeyAction`, `RunProcessAction`, `ShellOpenAction`, `DelegateAction`
- Purpose: Live-state on/off switch that always reflects reality and persists its choice
- Examples: `src/AppTemplate.App/Tweaks/TweakToggle.cs`, `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs`, `src/AppTemplate.App/Tweaks/ControlPanelRows.cs`
- Pattern: Immutable record-style class `(key, title, description, stateKey, Func<bool> read, Action<bool> apply)`; `read` probes registry/service state, `apply` mutates + write-throughs
- Purpose: Testable typed navigation with VM lifecycle hooks, decoupled from WinUI `Frame` journal
- Examples: `src/AppTemplate.Framework/Navigation/FrameNavigationService.cs`, `src/AppTemplate.Framework/Navigation/INavigationService.cs`, `src/AppTemplate.Framework/Navigation/INavigationAware.cs`
- Pattern: `Func<Type, Page>` factory injected from DI; `NavigateTo<T>/NavigateTo(Type)` pushes `NavigationEntry`, swaps `Frame.Content`, calls `OnNavigatedTo/OnNavigatedFrom` on `DataContext`; `Navigated` event drives shell selection sync
- Purpose: Overlapping runs share one global progress footer without flicker
- Examples: `src/AppTemplate.Framework/Services/IStatusService.cs:22-69`, `src/AppTemplate.App/MainWindow.xaml:79-103`
- Pattern: `Interlocked` ref-count (`Start` increments, `Complete` decrements, `IsActive` clears at zero); every VM run site wraps work with `Start/Complete`; item-level runners delegate to parent paths
- Purpose: Let VMs show dialogs without knowing windows or roots; overlapping requests queue
- Examples: `src/AppTemplate.Framework/Services/IDialogService.cs:49-150`
- Pattern: `Func<XamlRoot?>` provider registered in `App.BuildHost` (`src/AppTemplate.App/App.xaml.cs:180`); `SemaphoreSlim(1,1)` gate serializes `ShowAsync`; `NativeMessageBox` covers headless/no-root paths

## Entry Points

- Location: `src/AppTemplate.App/App.xaml.cs:49-132`
- Triggers: OS process start (unpackaged `AkariToolbox.App.exe`, `requireAdministrator` manifest, x64-only)
- Responsibilities: Headless-flag check → single-instance register → host build → `MainWindow.Activate()` → culture/theme init
- Location: `src/AppTemplate.App/App.xaml.cs:59-94`
- Triggers: Reboot with `--ddu-auto`, `--ddu-manual`, `--defender-optimize`, `--defender-default`, `--defender-disable`, `--defender-enable`, `--services-off`, `--services-on` (read via `Environment.GetCommandLineArgs()` because `AppInstance` activation args carry no `Arguments` on WinAppSDK 2.3.1 unpackaged)
- Responsibilities: Call the matching `Finish*` method (`DriverActions`/`WindowsActions`/`AdvancedActions`), warn via `NativeMessageBox` on failure, `Environment.Exit(0)` — never creates a window or registers single-instance
- Location: `src/AppTemplate.App/MainWindow.xaml.cs:45-48`
- Triggers: `MainWindow` construction
- Responsibilities: Bind `INavigationService` to `ContentFrame`, always open `HomePage` first, sync `NavigationView.SelectedItem` on every `Navigated` event

## Architectural Constraints

- **Threading:** UI thread owns XAML/VM property changes; all long work runs via `await Task.Run(work)` inside VMs (e.g. `src/AppTemplate.App/ViewModels/CheckViewModel.cs:95`) and marshals back for dialogs/InfoBar. `DialogService` serializes with `SemaphoreSlim`; `StatusService` ref-count uses `Interlocked`. WMI reads in `SystemInfo` are synchronous and cached per process — never call them on the UI thread without `Task.Run`.
- **Global state:** `App.Services` static provider (`src/AppTemplate.App/App.xaml.cs:27`) for XAML/code-behind escape hatches; `WeakReferenceMessenger.Default` singleton (`src/AppTemplate.Framework/ServiceCollectionExtensions.cs:30`); `StatusService._count` ref-count; `SystemInfo` `_osCache/_cpuCache/_gpuCache` process caches; single-instance key `AkariToolbox`.
- **Circular imports:** None by construction — dependency direction is `Views → ViewModels → Tweaks → (Registry/Process/WMI)` and `App → Framework`; Framework never references App. `Views/*.xaml.cs` reaching back to `App.Services` is the only upward edge and is intentional (service-locator for XAML-created roots).
- **Elevation:** `app.manifest` requires administrator; therefore WinUI 3 file pickers are unusable elevated — file picking goes through `Microsoft.Win32.OpenFileDialog` instead (see `README.md` app notes).
- **XAML binding:** Page-level `x:Bind` cannot resolve its root type (WMC9999), so pages use `{Binding}` with `DataContext = ViewModel`, and `x:Bind` only inside `DataTemplate` (`x:DataType`); `MainWindow` converters cannot use `IValueConverter` lookup (Window is not a `FrameworkElement`) — shell uses `x:Bind` function bindings `StatusText(...)`/`StatusFooterVisibility(...)` instead.
- **Testhost:** UI types cannot be instantiated under `dotnet test` (`new Page()`/`new Frame()` throw `COMException`); tests cover only non-XAML framework logic.

## Anti-Patterns

### Instantiating pages directly instead of via navigation

### Running tweak work on the UI thread

### Showing dialogs or message boxes from the Tweaks layer with XAML APIs

## Error Handling

- Per-command catch: `catch (Exception ex) { _infoBar.Show("<Title> failed", ex.Message); }` with `finally { IsBusy=false; _status.Complete(); }` — see `src/AppTemplate.App/ViewModels/CheckViewModel.cs:39-51`.
- Domain swallows where scripts did: `NativeOps.Winget` catches and ignores to mirror PowerShell `try {} catch {}` (`src/AppTemplate.App/Tweaks/NativeOps.cs:32-51`); `SystemInfo` WMI readers catch and return `"Unknown"`.
- Global last resort: `App.OnUnhandledException` logs via `ILogger<App>` and shows `IDialogService.ShowErrorAsync` with the logs folder path before `Shutdown()`; `OnAppDomainUnhandledException`/`OnUnobservedTaskException` log and (for tasks) `SetObserved()` — `src/AppTemplate.App/App.xaml.cs:187-227`.

## Cross-Cutting Concerns

<!-- GSD:architecture-end -->

<!-- GSD:skills-start source:skills/ -->

## Project Skills

No project skills found. Add skills to any of: `.claude/skills/`, `.agents/skills/`, `.cursor/skills/`, `.github/skills/`, or `.codex/skills/` with a `SKILL.md` index file.
<!-- GSD:skills-end -->

<!-- GSD:workflow-start source:GSD defaults -->

## GSD Workflow Enforcement

Before using Edit, Write, or other file-changing tools, start work through a GSD command so planning artifacts and execution context stay in sync.

Use these entry points:

- `/gsd-quick` for small fixes, doc updates, and ad-hoc tasks
- `/gsd-debug` for investigation and bug fixing
- `/gsd-execute-phase` for planned phase work

Do not make direct repo edits outside a GSD workflow unless the user explicitly asks to bypass it.
<!-- GSD:workflow-end -->

<!-- GSD:profile-start -->

## Developer Profile

> Profile not yet configured. Run `/gsd-profile-user` to generate your developer profile.
> This section is managed by `generate-claude-profile` -- do not edit manually.
<!-- GSD:profile-end -->
