---
phase: 04-downloads-misc
plan: 01
subsystem: ui
tags: [winui3, mvvm, winget, downloads, communitytoolkit-mvvm, dependency-injection]

# Dependency graph
requires:
  - phase: 01-foundation
    provides: DI/navigation/settings/theming framework (WinUI-3-MVVM-Framework), ITweakHandler/ITweakCatalog pattern, ILogConsoleService, IScriptRunner
  - phase: 03-debloat
    provides: DebloatCatalog/DebloatViewModel/DebloatPage as the direct architectural template this plan copies (record-array catalog, ItemsRepeater card XAML, ConcurrentDictionary per-key SemaphoreSlim lock pattern)
provides:
  - AppDefinition/AppItem models split (definition vs. display state)
  - IAppCatalog/AppCatalog — compiled-in 29-app winget catalog (11 Browsers, 4 Comms, 6 Dev, 4 Gaming, 4 Utilities)
  - IAppInstallerService/AppInstallerService — winget shell-out with exit-code-aware success/failure (fixes predecessor's swallowed-exit-code bug)
  - DownloadsViewModel — search/category filter, multi-select install, D-06 fire-and-forget PostInstall auto-trigger on navigation
  - DownloadsPage/.xaml.cs — full page UI wired into DI/navigation
  - Downloads entry enabled in MainWindow NavItems and HomeViewModel Cards
affects: [04-03 (appends 13 more catalog rows + hardening scripts on top of this base), misc-page-plans]

# Actuals (#2632)
actuals:
  tokens: 8634
  tasks: 2
  commits: 2

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AppDefinition (immutable catalog row) / AppItem (mutable ObservableObject display row) split, mirroring DebloatAction/DebloatActionItem"
    - "Per-app ConcurrentDictionary<string, SemaphoreSlim> install lock, mirroring DebloatViewModel's per-key lock pattern"
    - "Fire-and-forget async work from a synchronous INavigationAware.OnNavigatedTo, wrapped in try/catch so a background failure can never crash the page (D-06)"

key-files:
  created:
    - src/AkariToolbox.App/Models/AppDefinition.cs
    - src/AkariToolbox.App/Models/AppItem.cs
    - src/AkariToolbox.App/Services/IAppCatalog.cs
    - src/AkariToolbox.App/Services/AppCatalog.cs
    - src/AkariToolbox.App/Services/IAppInstallerService.cs
    - src/AkariToolbox.App/Services/AppInstallerService.cs
    - src/AkariToolbox.App/ViewModels/DownloadsViewModel.cs
    - src/AkariToolbox.App/Views/DownloadsPage.xaml
    - src/AkariToolbox.App/Views/DownloadsPage.xaml.cs
    - src/AkariToolbox.Tests/DownloadsCatalogTests.cs
  modified:
    - src/AkariToolbox.App/Services/TweakHandlerRegistration.cs
    - src/AkariToolbox.App/App.xaml.cs
    - src/AkariToolbox.App/MainWindow.xaml.cs
    - src/AkariToolbox.App/ViewModels/HomeViewModel.cs

key-decisions:
  - "Kept the winget install path (D-01, per project decision) rather than reviving the predecessor's direct-CDN-download pattern — matches PROJECT.md's 'keep the downloads the same' guidance"
  - "Fixed the predecessor's swallowed-exit-code bug in AppInstallerService.InstallAsync by checking the process exit code, not just catching exceptions (closes T-04-03)"
  - "Selection state lives on the master AppItem list, not the filtered ObservableCollection, so a selection survives search/category filtering — matches the predecessor's ICollectionView-independent AppItem.IsSelected behavior"

patterns-established:
  - "AppDefinition/AppItem split for future catalog-driven pages that need both an immutable data row and mutable per-row UI state"

requirements-completed: [DOWNLOADS-01, DOWNLOADS-02]

coverage:
  - id: D1
    description: "Downloads page reachable from Home dashboard card and nav sidebar, renders the 29-app winget catalog grouped/filterable by category and search"
    requirement: "DOWNLOADS-02"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DownloadsCatalogTests.cs#Catalog_has_exactly_29_apps_in_5_categories_with_predecessor_counts"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/DownloadsCatalogTests.cs#FilteredApps_narrows_to_selected_category"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/DownloadsCatalogTests.cs#FilteredApps_narrows_by_case_insensitive_description_search"
        status: pass
    human_judgment: true
    rationale: "Automated tests prove the ViewModel's filtering logic and catalog shape, but actual page navigation/rendering (Home card click, nav sidebar click, visual layout) requires a human to run the elevated app and visually confirm — no UI test harness exists in this project."
  - id: D2
    description: "Install Selected shells out to winget with --silent --accept-package-agreements --accept-source-agreements per selected app, and non-zero exit code is treated as failure"
    requirement: "DOWNLOADS-02"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DownloadsCatalogTests.cs#InstallSelectedCommand_calls_installer_once_per_selected_app_and_resets_IsInstalling"
        status: pass
    human_judgment: true
    rationale: "The exit-code-aware AppInstallerService logic is unit tested with a fake IScriptRunner, but a real winget install against the live network/package store requires manual verification (no CI runner has winget or admin rights)."
  - id: D3
    description: "PostInstall mirror silently triggers once per Downloads page visit, exception-safe, never crashing the page or blocking navigation"
    requirement: "DOWNLOADS-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DownloadsCatalogTests.cs#OnNavigatedTo_invokes_EnsurePostInstallAsync_exactly_once"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/DownloadsCatalogTests.cs#OnNavigatedTo_swallows_EnsurePostInstallAsync_exception_without_throwing"
        status: pass
    human_judgment: false

duration: ~25min
completed: 2026-09-02
status: complete
---

# Phase 4 Plan 1: Downloads Architecture End-to-End Summary

**Winget-backed 29-app installer catalog (11 Browsers/4 Comms/6 Dev/4 Gaming/4 Utilities) with exit-code-aware install and a D-06 silent PostInstall auto-mirror trigger on page navigation, wired end-to-end from Home/nav through DI.**

## Performance

- **Duration:** ~25 min
- **Completed:** 2026-09-02T16:10:54Z
- **Tasks:** 2
- **Files modified:** 14 (10 created, 4 modified)

## Accomplishments
- Ported the predecessor's 29-app winget catalog verbatim (`AppDefinition`/`AppCatalog`) split into an immutable definition row and a mutable `AppItem` display row
- Fixed the predecessor's swallowed-exit-code bug: `AppInstallerService.InstallAsync` now returns `false` on any non-zero winget/script exit code, not just on a thrown exception
- Built `DownloadsViewModel` with case-insensitive name/description search, category filtering, multi-select install (selection survives filter changes), and the D-06 fire-and-forget `EnsurePostInstallAsync()` trigger wired into `OnNavigatedTo` — exception-safe by design
- Built `DownloadsPage.xaml`/`.xaml.cs` matching `DebloatPage`'s established card/ItemsRepeater shape
- Enabled the "Downloads" entry in both `MainWindow.NavItems` and `HomeViewModel.Cards` (was `IsEnabled: false` in both)
- Registered `IAppCatalog`/`IAppInstallerService`/`DownloadsViewModel` in the existing DI extension points, no new registration methods needed

## Task Commits

Each task was committed atomically:

1. **Task 1: Downloads architecture end-to-end — 29-app catalog, winget install, D-06 auto-trigger** - `fdbf10b` (feat)
2. **Task 2: Regression tests — 29-app catalog shape, filter/search, D-06 wiring, install exit-code handling** - `40920d1` (test)

_Note: this plan's tasks are typed `tracer`/`auto`, not TDD, so each is a single commit._

## Files Created/Modified
- `src/AkariToolbox.App/Models/AppDefinition.cs` - Immutable catalog row record (Name/Description/Category/WingetId + 3 optional D-03/D-04 fields, unused this plan)
- `src/AkariToolbox.App/Models/AppItem.cs` - Mutable ObservableObject display row (IsSelected/IsInstalling)
- `src/AkariToolbox.App/Services/IAppCatalog.cs` / `AppCatalog.cs` - 29-app compiled-in catalog
- `src/AkariToolbox.App/Services/IAppInstallerService.cs` / `AppInstallerService.cs` - Winget shell-out, exit-code-aware
- `src/AkariToolbox.App/ViewModels/DownloadsViewModel.cs` - Search/filter/install/D-06 trigger logic
- `src/AkariToolbox.App/Views/DownloadsPage.xaml` / `.xaml.cs` - Page UI
- `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs` - Registers `IAppCatalog`/`IAppInstallerService`
- `src/AkariToolbox.App/App.xaml.cs` - Registers `DownloadsViewModel`
- `src/AkariToolbox.App/MainWindow.xaml.cs` - Downloads nav entry now points to `DownloadsPage`, enabled
- `src/AkariToolbox.App/ViewModels/HomeViewModel.cs` - Downloads home card now points to `DownloadsPage`, enabled
- `src/AkariToolbox.Tests/DownloadsCatalogTests.cs` - 8 regression tests locking catalog shape, filter/search, D-06 wiring, install behavior

## Decisions Made
- Kept the winget install path per D-01 (project decision to keep downloads the same as the predecessor) rather than reintroducing any direct-download mechanism for the base 29 apps
- Chose to keep `SelectCategoryCommand` in the ViewModel for parity/future use even though the XAML wires the category filter through a two-way-bound `ComboBox` directly (the plan's action text specifies a `ComboBox`, not category buttons)
- Sequential (not parallel) install loop in `InstallSelectedAsync`, matching the predecessor's `foreach` — the per-app `SemaphoreSlim` lock guards against re-entrant double-install of the same app rather than enabling parallelism

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## Known Stubs

None - all catalog entries, install logic, and navigation wiring are fully functional (winget calls are real process invocations, not mocked outside of tests).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 04-03 (Wave 2) can append 13 more catalog rows and hardening scripts directly on top of `AppCatalog`/`DownloadsCatalogTests` without further ViewModel/View/DI/Nav changes — this plan proved the full vertical slice.
- `T-04-01` (PostInstall download tampering) remains open per this plan's threat model — SHA256 verification is Plan 04-02's scope (D-07); the silent trigger wired here is functionally complete but not yet integrity-verified end-to-end.
- Manual smoke check (Home → Downloads card, or nav → Downloads, rendering all 29 apps and one PostInstall log line on first visit) is deferred to end-of-phase human verification per `human_verify_mode: end-of-phase` in config.json.

---
*Phase: 04-downloads-misc*
*Completed: 2026-09-02*
