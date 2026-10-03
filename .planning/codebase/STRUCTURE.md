# Codebase Structure

**Analysis Date:** 2026-10-03

## Directory Layout

```
Akari-Toolbox/
├── src/
│   ├── AppTemplate.App/        # The app (display name "Akari Toolbox")
│   │   ├── Views/              # 13 pages (*.xaml + *.xaml.cs) + Shared/
│   │   ├── ViewModels/         # 13 page view models
│   │   ├── Tweaks/             # Native action ports + Data/
│   │   ├── Services/           # LocalizedStrings
│   │   ├── Helpers/            # NativeMessageBox + misc
│   │   ├── Resources/          # .resx localization (neutral + zh-CN)
│   │   ├── Assets/             # AkariLogo.ico/.png
│   │   ├── App.xaml(.cs)       # DI host + launch flows
│   │   ├── MainWindow.xaml(.cs)# Shell (NavigationView + InfoBar + footer)
│   │   └── NavigationItem.cs   # Shell menu row record
│   ├── AppTemplate.Framework/  # Shared MVVM shell (no app references)
│   │   ├── Navigation/         # INavigationService + Frame impl + entries
│   │   ├── Services/           # Settings/theme/culture/dialog/window/picker/info/status
│   │   ├── Messaging/          # Messenger message records
│   │   ├── Converters/         # 14 IValueConverter implementations
│   │   ├── Collections/        # Observable collection helpers
│   │   ├── Behaviors/          # XAML attached behaviors
│   │   ├── Logging/            # FileLoggerProvider
│   │   ├── ViewModels/         # ViewModelBase + ValidatableViewModelBase
│   │   └── Threading/          # DispatcherQueue helpers
│   └── AppTemplate.Tests/      # xUnit tests (framework logic only)
├── .planning/codebase/         # This mapping output
├── AppTemplate.slnx            # Solution (XML solution format)
├── Directory.Build.props       # Shared MSBuild (TFM, nullable, WinUI, platforms)
├── Directory.Packages.props    # Central package management
├── global.json                 # .NET SDK pin (10.0.400)
├── nuget.config                # NuGet sources
├── build-and-run.ps1           # Kill, build, launch helper
├── check-parity.ps1            # WPF text-parity diff script
├── verify-pages.ps1            # UIA page-click verification script
├── port-windows-page.ps1       # Page porting helper
└── shot-pages.ps1              # Screenshot helper
```

## Directory Purposes

**`src/AppTemplate.App`:**
- Purpose: The entire product — shell, pages, VMs, domain actions, localization
- Contains: XAML pages, `.cs` code-behind/VMs/actions, `.resx`, app manifest, project file with explicit `Compile`/`Page` globs
- Key files: `src/AppTemplate.App/App.xaml.cs`, `src/AppTemplate.App/MainWindow.xaml`, `src/AppTemplate.App/MainWindow.xaml.cs`, `src/AppTemplate.App/AppTemplate.App.csproj`, `src/AppTemplate.App/app.manifest`

**`src/AppTemplate.App/Views`:**
- Purpose: One `*Page.xaml` + `*Page.xaml.cs` pair per destination; presentation only
- Contains: `HomePage`, `AkariTweaksPage`, `GamingPage`, `CheckPage`, `RefreshPage`, `SetupPage`, `InstallersPage`, `GraphicsPage`, `WindowsPage`, `HardwarePage`, `AdvancedPage`, `TweaksPage`, `SettingsPage`, plus `Shared/PageStyles.xaml`
- Key files: `src/AppTemplate.App/Views/HomePage.xaml`, `src/AppTemplate.App/Views/TweaksPage.xaml`, `src/AppTemplate.App/Views/Shared/PageStyles.xaml`

**`src/AppTemplate.App/ViewModels`:**
- Purpose: One page VM per page (`HomeViewModel`, `AkariTweaksViewModel`, `GamingViewModel`, `CheckViewModel`, `RefreshViewModel`, `SetupViewModel`, `InstallersViewModel`, `GraphicsViewModel`, `WindowsViewModel`, `HardwareViewModel`, `AdvancedViewModel`, `TweaksViewModel`, `SettingsViewModel`) plus item types (`HomeCard`, `CpTabGroup`, `CpSectionGroup`, `CpRowItem`)
- Contains: `[ObservableProperty]` state + `[RelayCommand]` verbs + `INavigationAware` refresh logic
- Key files: `src/AppTemplate.App/ViewModels/HomeViewModel.cs`, `src/AppTemplate.App/ViewModels/CheckViewModel.cs`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs`

**`src/AppTemplate.App/Tweaks`:**
- Purpose: The domain — every Windows mutation as native code, plus the toggle catalogs
- Contains: `TweakAction.cs` (action hierarchy), `TweakToggle.cs`, `AkariTweakCatalog.cs` (32 OS tweaks), `ControlPanelRows.cs` + `ControlPanelRows.Generated.cs` (169 rows), `ControlPanelActions.cs`, per-page `*Actions.cs` statics (`Check`, `Refresh`, `Setup`, `Installer`, `Graphics` + `Graphics.Nip`, `Gaming`, `Windows` + `Windows.System`, `Hardware`, `Advanced` + `Advanced.Services`, `Driver`, `RestorePoint`), `NativeOps.cs`, `SystemInfo.cs`, `AkariToolState.cs`, `Data/**` embedded payloads
- Key files: `src/AppTemplate.App/Tweaks/TweakAction.cs`, `src/AppTemplate.App/Tweaks/NativeOps.cs`, `src/AppTemplate.App/Tweaks/SystemInfo.cs`, `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs`

**`src/AppTemplate.App/Services`:**
- Purpose: Single app service — localization accessor
- Contains: `LocalizedStrings.cs`
- Key files: `src/AppTemplate.App/Services/LocalizedStrings.cs`

**`src/AppTemplate.App/Helpers`:**
- Purpose: UI-free Win32 helpers usable from headless flows and the Tweaks layer
- Contains: `NativeMessageBox.cs`
- Key files: `src/AppTemplate.App/Helpers/NativeMessageBox.cs`

**`src/AppTemplate.App/Resources`:**
- Purpose: Neutral + translated UI strings backing `LocalizedStrings`
- Contains: `Resources.resx`, `Resources.zh-CN.resx`
- Key files: `src/AppTemplate.App/Resources/Resources.resx`

**`src/AppTemplate.App/Assets`:**
- Purpose: Window icon + title-bar logo, copied to output
- Contains: `AkariLogo.ico`, `AkariLogo.png`
- Key files: `src/AppTemplate.App/Assets/AkariLogo.ico`

**`src/AppTemplate.Framework/Navigation`:**
- Purpose: DI-friendly typed navigation with back/forward stacks and VM lifecycle
- Contains: `INavigationService.cs`, `FrameNavigationService.cs`, `NavigationEntry.cs`, `INavigationAware.cs`
- Key files: `src/AppTemplate.Framework/Navigation/FrameNavigationService.cs`

**`src/AppTemplate.Framework/Services`:**
- Purpose: Singleton shell services + their interfaces
- Contains: `ISettingsStorage.cs` + `FileSettingsStorage.cs`, `ISettingsService.cs`, `ICultureService.cs`, `IThemeService.cs`, `IDialogService.cs`, `IWindowService.cs`, `IFilePickerService.cs`, `IInfoBarService.cs`, `IStatusService.cs` (interface + `StatusService` impl in one file)
- Key files: `src/AppTemplate.Framework/Services/IStatusService.cs`, `src/AppTemplate.Framework/Services/IDialogService.cs`

**`src/AppTemplate.Framework/Messaging`:**
- Purpose: Decoupled cross-VM/shell events
- Contains: `Messages.cs` (`ThemeChangedMessage`, `CultureChangedMessage`, `ShowInfoBarMessage`, `NavigationRequestedMessage`, `UserActionMessage`)
- Key files: `src/AppTemplate.Framework/Messaging/Messages.cs`

**`src/AppTemplate.Framework/Converters`:**
- Purpose: 14 `{Binding}` converters (bool/visibility, null, count, collection, enum, date, math, passthrough, inverted variants, `BoolToValueConverter`)
- Contains: One file per converter
- Key files: `src/AppTemplate.Framework/Converters/BooleanToVisibilityConverter.cs`, `src/AppTemplate.Framework/Converters/InvertedBooleanConverter.cs`

**`src/AppTemplate.Framework/Collections`:**
- Purpose: Observable collection utilities for grouped/incremental lists
- Contains: `RangeObservableCollection.cs`, `ObservableGroupCollection.cs`, `IncrementalLoadingCollection.cs`

**`src/AppTemplate.Framework/Behaviors`:**
- Purpose: Reusable XAML behaviors
- Contains: `TextBoxSelectAllOnFocusBehavior.cs`, `ListViewScrollIntoViewBehavior.cs`, `InvokeCommandOnLoadedBehavior.cs`

**`src/AppTemplate.Framework/Logging` / `Threading` / `ViewModels`:**
- Purpose: Daily rolling file logger; `DispatcherQueue` await helpers; `ViewModelBase` (`ObservableObject` + `Title`) and `ValidatableViewModelBase`
- Key files: `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`, `src/AppTemplate.Framework/ViewModels/ViewModelBase.cs`, `src/AppTemplate.Framework/ServiceCollectionExtensions.cs`, `src/AppTemplate.Framework/ObjectExtensions.cs`

**`src/AppTemplate.Tests`:**
- Purpose: xUnit coverage of framework logic that does not require XAML objects
- Contains: `CollectionsTests.cs`, `ConvertersTests.cs`, `FileSettingsStorageTests.cs`, `LoggingTests.cs`, `MessagingTests.cs`, `NavigationTests.cs`, `ObjectExtensionsTests.cs`, `ServicesTests.cs`, `SettingsServiceTests.cs`, `ViewModelTests.cs`, `AssemblyInfo.cs`
- Key files: `src/AppTemplate.Tests/NavigationTests.cs`, `src/AppTemplate.Tests/ServicesTests.cs`

## Key File Locations

**Entry Points:**
- `src/AppTemplate.App/App.xaml.cs`: `OnLaunched` — headless flags → single-instance → host → `MainWindow.Activate()`; `BuildHost()` DI graph; global exception handlers + `Shutdown()`
- `src/AppTemplate.App/MainWindow.xaml(.cs)`: shell definition + nav items + `ContentFrame` binding + status footer; always navigates to `HomePage` first
- `src/AppTemplate.App/app.manifest`: `requireAdministrator` elevation requirement

**Configuration:**
- `global.json`: .NET SDK pin `10.0.400`
- `Directory.Build.props`: `net10.0-windows10.0.26100.0`, `Nullable enable`, `ImplicitUsings enable`, `UseWinUI true`, platform/RID mapping
- `Directory.Packages.props`: central package versions (WindowsAppSDK, Toolkit.Mvvm, Extensions, xUnit/Moq)
- `nuget.config`: feed sources
- `src/AppTemplate.App/AppTemplate.App.csproj`: unpackaged + self-contained + x64-only flags, `SetAkariXamlLocalAssembly` MarkupCompilePass2 fix, explicit `Compile`/`Page`/`EmbeddedResource` globs, `ProjectReference` to Framework
- `%LOCALAPPDATA%\AkariToolbox\settings.json` (runtime, not in repo): user prefs via `FileSettingsStorage("AkariToolbox")`

**Core Logic:**
- `src/AppTemplate.App/Tweaks/TweakAction.cs`: registry/process/shell action hierarchy + factories
- `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs`: 32-toggle OS catalog
- `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs`: generated 169-row data backing `TweaksPage`
- `src/AppTemplate.App/ViewModels/*.cs`: per-page orchestration (start busy/status → `Task.Run` → dialog/InfoBar → complete)

**Testing:**
- `src/AppTemplate.Tests/*.cs`: framework unit tests; `src/AppTemplate.Tests/AppTemplate.Tests.csproj` test project

**Scripts / Verification:**
- `build-and-run.ps1`: kill `AkariToolbox.App` (apphost lock) → build (`Errors: 0 Warnings: 0`) → launch; `-NoLaunch` / `-Release` switches
- `check-parity.ps1`: diffs `Text=`/`Content=` strings vs. WPF original
- `verify-pages.ps1`: UIA click-through of all 13 nav items
- `shot-pages.ps1`, `port-windows-page.ps1`: screenshot + porting helpers

## Naming Conventions

**Files:**
- Pages: `<Name>Page.xaml` + `<Name>Page.xaml.cs` — e.g. `src/AppTemplate.App/Views/CheckPage.xaml`
- Page VMs: `<Name>ViewModel.cs` matching the page — e.g. `src/AppTemplate.App/ViewModels/CheckViewModel.cs`
- Domain modules: `<Area>Actions.cs` statics — e.g. `src/AppTemplate.App/Tweaks/GamingActions.cs`; multi-file areas use suffix splits: `WindowsActions.System.cs`, `GraphicsActions.Nip.cs`, `AdvancedActions.Services.cs`
- Generated data: `<Name>.Generated.cs` — e.g. `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs`
- Framework services: `I<Name>Service.cs` interface, impl either co-located in the same file (e.g. `IStatusService.cs` contains `StatusService`) or a matching `<Name>Service.cs`
- Tests: `<Subject>Tests.cs` — e.g. `src/AppTemplate.Tests/NavigationTests.cs`

**Directories:**
- `PascalCase` singular nouns per layer: `Views/`, `ViewModels/`, `Tweaks/`, `Services/`, `Helpers/`, `Resources/`, `Assets/`, `Navigation/`, `Converters/`, `Collections/`, `Behaviors/`, `Logging/`, `Messaging/`, `Threading/`, `Data/`
- Namespaces follow folders: `AkariToolbox.Views`, `AkariToolbox.ViewModels`, `AkariToolbox.Tweaks`, `AkariToolbox.Services`, `AkariToolbox.Helpers`, `AppTemplate.Framework.<Folder>`

## Where to Add New Code

**New Feature:**
- Primary code: new `*Actions.cs` static module in `src/AppTemplate.App/Tweaks/` for the domain op + new `[RelayCommand]` on the owning page VM in `src/AppTemplate.App/ViewModels/` + XAML controls in the matching `src/AppTemplate.App/Views/<Name>Page.xaml`
- Tests: framework-touching logic → `src/AppTemplate.Tests/`; UI/tweak logic is not unit-tested (testhost cannot instantiate XAML) — verify with `verify-pages.ps1` / `check-parity.ps1` instead

**New Component/Module:**
- Implementation: new page = 4 files — `src/AppTemplate.App/Views/<Name>Page.xaml`, `src/AppTemplate.App/Views/<Name>Page.xaml.cs` (VM + `LocalizedStrings` resolver, `DataContext = ViewModel`), `src/AppTemplate.App/ViewModels/<Name>ViewModel.cs` (`ViewModelBase` + `INavigationAware`), plus registration of VM + page as `AddTransient` in `src/AppTemplate.App/App.xaml.cs:BuildHost()` and a `NavigationItem` entry in `src/AppTemplate.App/MainWindow.xaml.cs:NavItems`
- Shared UI resource: `src/AppTemplate.App/Views/Shared/PageStyles.xaml`
- Reusable MVVM infra (must not reference the app): `src/AppTemplate.Framework/` + registration in `src/AppTemplate.Framework/ServiceCollectionExtensions.cs`

**Utilities:**
- Shared helpers: Windows-only UI-free helpers → `src/AppTemplate.App/Helpers/` (e.g. `NativeMessageBox.cs`); tweak primitives → extend `src/AppTemplate.App/Tweaks/TweakAction.cs` factories or `src/AppTemplate.App/Tweaks/NativeOps.cs`; generic extensions → `src/AppTemplate.Framework/ObjectExtensions.cs` or `src/AppTemplate.Framework/Threading/DispatcherQueueExtensions.cs`

## Special Directories

**`src/AppTemplate.App/bin/` + `src/AppTemplate.App/obj/` (also under Framework/Tests):**
- Purpose: Build output + intermediate XAML assemblies
- Generated: Yes
- Committed: No (gitignored build artifacts)

**`src/AppTemplate.App/Tweaks/Data/`:**
- Purpose: Embedded payload files (e.g. GPU profiles) compiled via `<EmbeddedResource Include="Tweaks\Data\**\*">`
- Generated: No
- Committed: Yes

**`.planning/`:**
- Purpose: GSD planning state + these codebase maps (`.planning/codebase/`)
- Generated: Partially (agent-written docs)
- Committed: Yes (orchestrator commits)

---

*Structure analysis: 2026-10-03*
