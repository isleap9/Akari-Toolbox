<!-- refreshed: 2026-10-03 -->
# Architecture

**Analysis Date:** 2026-10-03

## System Overview

```text
┌─────────────────────────────────────────────────────────────┐
│                      Shell / Composition Root                │
│  `src/AppTemplate.App/App.xaml.cs` + `src/AppTemplate.App/  │
│   MainWindow.xaml(.cs)` — Host builder, single-instance,    │
│   NavigationView, InfoBar, status footer, Mica backdrop      │
├──────────────────┬──────────────────┬───────────────────────┤
│   Views (13)     │  ViewModels (13) │  Tweaks (domain)       │
│  `Views/*.xaml`  │  `ViewModels/    │  `Tweaks/*Actions.cs`  │
│  + `.xaml.cs`    │   *.cs`          │  `TweakAction.cs`,      │
│                  │                  │  `TweakToggle.cs`      │
└────────┬─────────┴────────┬─────────┴──────────┬────────────┘
         │                  │                     │
         ▼                  ▼                     ▼
┌─────────────────────────────────────────────────────────────┐
│                    Framework (MVVM shell)                    │
│         `src/AppTemplate.Framework/`                         │
│  Navigation + Services + Messaging + Converters +            │
│  Collections + Behaviors + Logging + ViewModels              │
└─────────────────────────────────────────────────────────────┘
         │
         ▼
┌─────────────────────────────────────────────────────────────┐
│  Windows OS (registry / services / WMI / processes / winget) │
│  `Tweaks/NativeOps.cs`, `Tweaks/SystemInfo.cs`,              │
│  `%LOCALAPPDATA%\AkariToolbox\settings.json` + `logs/`       │
└─────────────────────────────────────────────────────────────┘
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

**Overall:** Layered WinUI 3 MVVM with Generic Host DI — thin View / stateful ViewModel / static native-action domain layer, over a reusable framework shell.

**Key Characteristics:**
- Composition root owns everything: `App.BuildHost()` registers framework singletons, transient VMs/pages, `Func<XamlRoot?>` and `Func<IntPtr>` adapters, and the `INavigationService` page factory (`src/AppTemplate.App/App.xaml.cs:134-185`).
- Pages are DI-created, never `new`'d by XAML: `FrameNavigationService(pageType => (Page)ActivatorUtilities.CreateInstance(sp, pageType))` (`src/AppTemplate.App/App.xaml.cs:177-178`).
- ViewModels never touch UI types: all dialogs go through `IDialogService`, all toasts through `IInfoBarService`, all progress through `IStatusService`, all navigation through `INavigationService`/`NavigationRequestedMessage`.
- Domain logic is static and UI-free: `Tweaks/*Actions.cs` expose `Action`/`Func` units; `TweakAction` subclasses express registry/process/shell ops declaratively; `TweakToggle` pairs a live `read` with an `apply(on/off)`.
- Messenger is the cross-cutting bus: theme changes (`ThemeChangedMessage` → `MainWindow.ApplyTheme`), culture changes (`CultureChangedMessage` → `LocalizedStrings.Refresh`), decoupled navigation (`NavigationRequestedMessage` → `MainWindow` handler).

## Layers

**Shell / Composition Root:**
- Purpose: App lifecycle, DI graph, single-instance, headless flows, global error handling
- Location: `src/AppTemplate.App/App.xaml.cs`, `src/AppTemplate.App/MainWindow.xaml(.cs)`, `src/AppTemplate.App/App.xaml`
- Contains: `IHost` builder, `AppInstance.FindOrRegisterForKey("AkariToolbox")`, `--ddu-auto/--defender-*/--services-*` early-exit branches, `UnhandledException`/`AppDomain`/`UnobservedTask` handlers, `FileLoggerProvider` wiring
- Depends on: Framework services, app VMs/pages, `Tweaks` finish-methods
- Used by: OS launcher (entry point); `Views/*.xaml.cs` reach back via `App.Services`

**Presentation (Views):**
- Purpose: Declarative layout only; code-behind is a 3-line VM + strings resolver
- Location: `src/AppTemplate.App/Views/`
- Contains: 13 `*Page.xaml` + `*Page.xaml.cs` pairs, `src/AppTemplate.App/Views/Shared/PageStyles.xaml`
- Depends on: ViewModels, `Services/LocalizedStrings`, framework converters/behaviors
- Used by: `MainWindow` `ContentFrame` via `INavigationService`
- Canonical code-behind shape (`src/AppTemplate.App/Views/CheckPage.xaml.cs:9-26`): ctor takes typed VM, pulls `LocalizedStrings` from `App.Services`, sets `DataContext = ViewModel`, exposes both as `x:Bind`-able properties.

**Presentation logic (ViewModels):**
- Purpose: Bindable state + commands + run orchestration (`IsBusy` + `Start/Complete` + InfoBar/dialog reporting)
- Location: `src/AppTemplate.App/ViewModels/`
- Contains: 13 page VMs, all `ViewModelBase` + `INavigationAware`; `HomeCard`/`CpTabGroup`/`CpSectionGroup`/`CpRowItem` item types live alongside (e.g. `HomeViewModel`, `TweaksViewModel`)
- Depends on: `Tweaks/*Actions`, framework `IInfoBarService`/`IStatusService`/`IDialogService`/`INavigationService`
- Used by: Pages (via `DataContext` + typed `ViewModel` property)
- Canonical run pattern (`src/AppTemplate.App/ViewModels/CheckViewModel.cs:86-109`): guard `IsBusy` → `_status.Start(title)` → `await Task.Run(work)` → guidance dialog + InfoBar → `finally { IsBusy=false; _status.Complete(); }`.

**Domain (Tweaks):**
- Purpose: All real Windows mutations as native .NET/Win32 — no scripts, no bundled binaries
- Location: `src/AppTemplate.App/Tweaks/`
- Contains: `TweakAction` hierarchy, `TweakToggle`, per-page `*Actions.cs` statics, `NativeOps`, `SystemInfo`, `AkariToolState`, `ControlPanelRows(.Generated)`, `Tweaks/Data/**`
- Depends on: `Microsoft.Win32.Registry`, `System.Diagnostics.Process`, `System.Management`, `Helpers/NativeMessageBox`
- Used by: ViewModels and headless `App.OnLaunched` finish-methods (`DriverActions.FinishDdu`, `WindowsActions.FinishDefenderOptimize/Default`, `AdvancedActions.FinishDefenderDisable/Enable`, `FinishServicesOff/Default`)

**Framework shell (reusable MVVM infrastructure):**
- Purpose: App-agnostic navigation, settings/theme/culture/dialog/window/file-picker/info-bar/status, converters, collections, behaviors, logging, messaging, VM bases
- Location: `src/AppTemplate.Framework/`
- Contains: `Navigation/`, `Services/`, `Messaging/`, `Converters/` (14 converters), `Collections/`, `Behaviors/`, `Logging/FileLoggerProvider.cs`, `ViewModels/ViewModelBase.cs`, `ViewModels/ValidatableViewModelBase.cs`, `ServiceCollectionExtensions.cs`, `ObjectExtensions.cs`
- Depends on: `CommunityToolkit.Mvvm`, `Microsoft.Extensions.*`, WinUI types only at the service-implementation edge
- Used by: App composition root (`AddMvvmFramework`) and all VMs

**Cross-cutting app services/helpers:**
- Purpose: Localization lookup and Win32 fallback dialogs
- Location: `src/AppTemplate.App/Services/LocalizedStrings.cs`, `src/AppTemplate.App/Helpers/NativeMessageBox.cs`, `src/AppTemplate.App/Resources/Resources.resx`, `src/AppTemplate.App/Resources/Resources.zh-CN.resx`
- Depends on: Embedded `.resx` manifest resources, `user32.dll MessageBoxW`
- Used by: Every page (`Strings` property), every culture/theme change, headless flows

## Data Flow

### Primary Request Path

1. Launch → `App.OnLaunched` (`src/AppTemplate.App/App.xaml.cs:49-132`): check headless flags first (`--ddu-auto`, `--defender-optimize`, `--services-off`, …) and exit, else `FindOrRegisterForKey("AkariToolbox")`, `BuildHost()`, resolve `MainWindow`, `Activate()`, then async `ICultureService`/`IThemeService` init + `ApplyTheme`.
2. Shell nav → `MainWindow` ctor (`src/AppTemplate.App/MainWindow.xaml.cs:22-53`): `_navigation.SetFrame(ContentFrame)`, `NavigateTo<HomePage>()`, subscribe `Navigated` → `RefreshShellState`, register `ThemeChangedMessage`/`NavigationRequestedMessage` handlers.
3. User action → VM `[RelayCommand]` (e.g. `src/AppTemplate.App/ViewModels/CheckViewModel.cs:53-54`): `RunAsync(title, CheckActions.InstallAndRunOcct, guidance)` — sets `IsBusy`, `IStatusService.Start`, `await Task.Run(work)` off UI thread.
4. Domain op → static `Tweaks` method applies `TweakAction`s: registry writes via `RegistrySetAction.Apply` (`src/AppTemplate.App/Tweaks/TweakAction.cs:104-112`), hidden `bcdedit/powercfg/sc` via `RunProcessAction`, user-visible targets via `Launch`/`ShellOpenAction`, WMI escapes via `DelegateAction` + `NativeOps`/`SystemInfo`.
5. Completion → guidance `IDialogService.ShowInfoAsync` + `IInfoBarService.Show(done/failed)` + `IStatusService.Complete()` → shell footer returns to "Ready" (`src/AppTemplate.App/MainWindow.xaml:79-103`).

### Toggle Apply Path (live-state switches)

1. `OnNavigatedTo` refreshes every row from the live system (`row.RefreshFromSystem()` in `src/AppTemplate.App/ViewModels/TweaksViewModel.cs:53-81`); combo boxes map live registry values to option indexes via `MatchIndex`.
2. User flips a `TweakToggle`: `Apply(bool)` runs immediately (service `Start` value change, registry write, `bcdedit`) and write-throughs `HKCU\Software\AkariTool\<StateKey>` via `AkariToolState`.
3. Failures surface as `IInfoBarService.Show("<title> failed", ex.Message)` — domain exceptions never crash the shell; global handlers log + show a friendly dialog (`src/AppTemplate.App/App.xaml.cs:187-227`).

### Theme / Culture Propagation

1. `IThemeService`/`ICultureService` change → publish `ThemeChangedMessage` / `CultureChangedMessage` (`src/AppTemplate.Framework/Messaging/Messages.cs:7-10`).
2. `MainWindow` applies `RootElement.RequestedTheme` + title-bar colors (`src/AppTemplate.App/MainWindow.xaml.cs:104-115`); `LocalizedStrings.Refresh()` raises `PropertyChanged(string.Empty)` so `x:Bind Strings.Get("Key")` re-evaluates (`src/AppTemplate.App/Services/LocalizedStrings.cs:48-52`).

**State Management:**
- Ephemeral page state lives in VM `[ObservableProperty]` fields; shell-global `InfoBar`/`Status` are singleton services bound with `x:Bind` in `MainWindow.xaml`.
- Persisted state is split: user prefs in `%LOCALAPPDATA%\AkariToolbox\settings.json` (`FileSettingsStorage("AkariToolbox")`), toggle memory in `HKCU\Software\AkariTool` (`AkariToolState`), logs in `%LOCALAPPDATA%\AkariToolbox\logs\` (`FileLoggerProvider`).
- Navigation state is an explicit in-memory back/forward stack inside `FrameNavigationService` (`_backStack`/`_forwardStack`), not the `Frame` journal — `Frame.Content` is assigned directly (`src/AppTemplate.Framework/Navigation/FrameNavigationService.cs:67-70`).

## Key Abstractions

**TweakAction:**
- Purpose: Declarative unit of one native mutation so catalogs read like the scripts they replaced
- Examples: `src/AppTemplate.App/Tweaks/TweakAction.cs`, all `Tweaks/*Actions.cs` call sites
- Pattern: Abstract `Apply()` + static factories (`RegDword`, `RegDeleteKey`, `Run`, `Launch`, `Open`, `Custom`); sealed subclasses `RegistrySetAction`, `RegistryDeleteValueAction`, `RegistryDeleteKeyAction`, `RunProcessAction`, `ShellOpenAction`, `DelegateAction`

**TweakToggle:**
- Purpose: Live-state on/off switch that always reflects reality and persists its choice
- Examples: `src/AppTemplate.App/Tweaks/TweakToggle.cs`, `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs`, `src/AppTemplate.App/Tweaks/ControlPanelRows.cs`
- Pattern: Immutable record-style class `(key, title, description, stateKey, Func<bool> read, Action<bool> apply)`; `read` probes registry/service state, `apply` mutates + write-throughs

**FrameNavigationService + INavigationAware:**
- Purpose: Testable typed navigation with VM lifecycle hooks, decoupled from WinUI `Frame` journal
- Examples: `src/AppTemplate.Framework/Navigation/FrameNavigationService.cs`, `src/AppTemplate.Framework/Navigation/INavigationService.cs`, `src/AppTemplate.Framework/Navigation/INavigationAware.cs`
- Pattern: `Func<Type, Page>` factory injected from DI; `NavigateTo<T>/NavigateTo(Type)` pushes `NavigationEntry`, swaps `Frame.Content`, calls `OnNavigatedTo/OnNavigatedFrom` on `DataContext`; `Navigated` event drives shell selection sync

**StatusService (ref-counted footer):**
- Purpose: Overlapping runs share one global progress footer without flicker
- Examples: `src/AppTemplate.Framework/Services/IStatusService.cs:22-69`, `src/AppTemplate.App/MainWindow.xaml:79-103`
- Pattern: `Interlocked` ref-count (`Start` increments, `Complete` decrements, `IsActive` clears at zero); every VM run site wraps work with `Start/Complete`; item-level runners delegate to parent paths

**DialogService (serialized, XamlRoot-free VMs):**
- Purpose: Let VMs show dialogs without knowing windows or roots; overlapping requests queue
- Examples: `src/AppTemplate.Framework/Services/IDialogService.cs:49-150`
- Pattern: `Func<XamlRoot?>` provider registered in `App.BuildHost` (`src/AppTemplate.App/App.xaml.cs:180`); `SemaphoreSlim(1,1)` gate serializes `ShowAsync`; `NativeMessageBox` covers headless/no-root paths

## Entry Points

**Normal GUI launch:**
- Location: `src/AppTemplate.App/App.xaml.cs:49-132`
- Triggers: OS process start (unpackaged `AkariToolbox.App.exe`, `requireAdministrator` manifest, x64-only)
- Responsibilities: Headless-flag check → single-instance register → host build → `MainWindow.Activate()` → culture/theme init

**Headless safe-boot re-entry:**
- Location: `src/AppTemplate.App/App.xaml.cs:59-94`
- Triggers: Reboot with `--ddu-auto`, `--ddu-manual`, `--defender-optimize`, `--defender-default`, `--defender-disable`, `--defender-enable`, `--services-off`, `--services-on` (read via `Environment.GetCommandLineArgs()` because `AppInstance` activation args carry no `Arguments` on WinAppSDK 2.3.1 unpackaged)
- Responsibilities: Call the matching `Finish*` method (`DriverActions`/`WindowsActions`/`AdvancedActions`), warn via `NativeMessageBox` on failure, `Environment.Exit(0)` — never creates a window or registers single-instance

**Shell navigation root:**
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

**What happens:** `new CheckPage(...)` in a VM or helper, bypassing the navigation stack and lifecycle.
**Why it's wrong:** `DataContext`-based `OnNavigatedTo/OnNavigatedFrom` never fire, live toggle state never refreshes, and the shell selection desyncs from `CurrentPageType`.
**Do this instead:** Publish `NavigationRequestedMessage(PageType)` or inject `INavigationService` and call `NavigateTo<T>()` — see `src/AppTemplate.App/ViewModels/HomeViewModel.cs` tile navigation and `src/AppTemplate.App/MainWindow.xaml.cs:51-52`.

### Running tweak work on the UI thread

**What happens:** Calling `CheckActions.*` / registry / `winget` / WMI synchronously from a `[RelayCommand]` without `Task.Run`, or forgetting the `IsBusy` guard.
**Why it's wrong:** The shell freezes (some flows download/run OCCT/CPU-Z/FurMark), re-entrancy corrupts toggle state, and the status footer never shows.
**Do this instead:** Copy the `RunAsync` pattern in `src/AppTemplate.App/ViewModels/CheckViewModel.cs:86-109` — `IsBusy` guard + `_status.Start` + `await Task.Run(work)` + dialog/InfoBar + `finally Complete()`.

### Showing dialogs or message boxes from the Tweaks layer with XAML APIs

**What happens:** A static `*Actions` method constructs `ContentDialog` or touches `XamlRoot`.
**Why it's wrong:** The domain layer is UI-free and also runs headless (safe-boot flows have no window); it throws or deadlocks without a root.
**Do this instead:** Return `bool`/throw and let the VM present via `IDialogService`; for genuinely root-less paths use `src/AppTemplate.App/Helpers/NativeMessageBox.cs` like `src/AppTemplate.App/App.xaml.cs:63-91` does.

## Error Handling

**Strategy:** VM-local try/catch → InfoBar for failures, guidance dialogs for success-with-next-steps; global log-and-show-friendly-dialog for anything escaping to the app domain.

**Patterns:**
- Per-command catch: `catch (Exception ex) { _infoBar.Show("<Title> failed", ex.Message); }` with `finally { IsBusy=false; _status.Complete(); }` — see `src/AppTemplate.App/ViewModels/CheckViewModel.cs:39-51`.
- Domain swallows where scripts did: `NativeOps.Winget` catches and ignores to mirror PowerShell `try {} catch {}` (`src/AppTemplate.App/Tweaks/NativeOps.cs:32-51`); `SystemInfo` WMI readers catch and return `"Unknown"`.
- Global last resort: `App.OnUnhandledException` logs via `ILogger<App>` and shows `IDialogService.ShowErrorAsync` with the logs folder path before `Shutdown()`; `OnAppDomainUnhandledException`/`OnUnobservedTaskException` log and (for tasks) `SetObserved()` — `src/AppTemplate.App/App.xaml.cs:187-227`.

## Cross-Cutting Concerns

**Logging:** `Microsoft.Extensions.Logging` + `FileLoggerProvider` (daily rolling files, ~1 MB cap, newest 10 kept) under `%LOCALAPPDATA%\AkariToolbox\logs\`, plus `Debug` provider; unhandled exceptions always logged with context (`src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`, `src/AppTemplate.App/App.xaml.cs:138-140`).
**Validation:** `ValidatableViewModelBase` in framework (`src/AppTemplate.Framework/ViewModels/ValidatableViewModelBase.cs`); page VMs validate via live-read fallbacks (`MatchIndex` with persisted fallback in `src/AppTemplate.App/ViewModels/TweaksViewModel.cs:93-98`) rather than blocking input.
**Authentication:** None (local admin desktop app) — privilege is enforced by OS elevation (`requireAdministrator` manifest) and surfaced read-only on Home via `SystemInfo` privileges read.

---

*Architecture analysis: 2026-10-03*
