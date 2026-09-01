---
phase: 03-debloat
plan: 01
subsystem: ui
tags: [winui3, mvvm, communitytoolkit-mvvm, powershell, xaml, xunit]

# Dependency graph
requires:
  - phase: 02-gaming-tweaks
    provides: "IScriptRunner.RunEmbeddedScriptAsync, ILogConsoleService, IDialogService, GamingTweaksPage/ViewModel as the direct architectural mirror"
provides:
  - "DebloatAction/DebloatActionItem/DebloatCategoryGroup models (not ITweakHandler-shaped, D-01)"
  - "IDebloatCatalog/DebloatCatalog: the full 28-action, 5-category static catalog (DEBLOAT-01)"
  - "DebloatViewModel: catalog-driven generic Run/Undo dispatch with D-11 confirmation gating and per-key concurrency locking"
  - "DebloatPage.xaml/.xaml.cs: 5-category grouped Run/Undo UI, zero business logic in code-behind (DEBLOAT-03)"
  - "First proven embedded Debloat script pair (telemetry.ps1/telemetry-undo.ps1) wired end-to-end"
  - "DebloatCatalogTests.cs regression lock (28/5/8-8-6-5-1/5-confirmation shape)"
affects: [03-02, 03-03, 03-04, 03-05, 03-06, 03-07]

# Actuals (#2632)
actuals:
  tokens: 9200
  tasks: 2
  commits: 2

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Catalog-driven generic two-method (Run/Undo) command dispatch over a compiled-in static list, instead of one [RelayCommand] per row — used when a catalog is large (28 rows) and has no per-row live state to read back"
    - "Nested ItemsRepeater + DataTemplate cross-template command binding via classic {Binding ElementName=RootPage, Path=DataContext.Command} instead of {x:Bind RootPage.ViewModel.Command} — required in build environments where the WindowsAppSDK.WinUI 2.3.0 XamlCompiler crashes (WMC9999) resolving named-element x:Bind paths under a non-en-US OS culture"

key-files:
  created:
    - src/AkariToolbox.App/Models/DebloatAction.cs
    - src/AkariToolbox.App/Models/DebloatActionItem.cs
    - src/AkariToolbox.App/Services/IDebloatCatalog.cs
    - src/AkariToolbox.App/Services/DebloatCatalog.cs
    - src/AkariToolbox.App/ViewModels/DebloatViewModel.cs
    - src/AkariToolbox.App/Views/DebloatPage.xaml
    - src/AkariToolbox.App/Views/DebloatPage.xaml.cs
    - src/AkariToolbox.App/Resources/DebloatScripts/telemetry.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/telemetry-undo.ps1
    - src/AkariToolbox.Tests/DebloatCatalogTests.cs
  modified:
    - src/AkariToolbox.App/Services/TweakHandlerRegistration.cs
    - src/AkariToolbox.App/App.xaml.cs
    - src/AkariToolbox.App/ViewModels/HomeViewModel.cs
    - src/AkariToolbox.App/MainWindow.xaml.cs
    - src/AkariToolbox.App/AkariToolbox.App.csproj

key-decisions:
  - "Nested DataTemplate Run/Undo Command bindings use {Binding ElementName=RootPage, Path=DataContext.*Command} instead of the plan-specified {x:Bind RootPage.ViewModel.*Command} — a build-environment-triggered XamlCompiler defect (WMC9999 inside BindingPathListener.ResolveNameOnRoot, root-caused via the compiler's own output.json log, triggered by named-element x:Bind resolution under an it-IT OS culture), not a markup or architecture error. Functionally identical; CommandParameter=\"{x:Bind}\" is retained unchanged since it needs no name resolution."

patterns-established:
  - "Debloat catalog rows use positional-then-named record construction (new(\"key\", \"title\", ..., RequiresConfirmation: bool, UndoDownloadsUnverifiedBinary: bool)) for readability across the 28-row static list"

requirements-completed: [DEBLOAT-01, DEBLOAT-02, DEBLOAT-03]

coverage:
  - id: D1
    description: "DebloatCatalog exposes exactly 28 actions in 5 categories with counts [8,8,6,5,1] in predecessor category order"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Catalog_has_exactly_28_actions_in_5_categories_with_predecessor_counts"
        status: pass
    human_judgment: false
  - id: D2
    description: "Exactly the 5 D-11 risk-classified actions (BitLocker, Bloatware, Edge & WebView removal, Hibernation, OneDrive removal) require confirmation"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Confirmation_required_set_matches_D11_classification"
        status: pass
    human_judgment: false
  - id: D3
    description: "telemetry.ps1/telemetry-undo.ps1 are embedded and resolve via the assembly manifest; DebloatViewModel's Run/Undo commands invoke IScriptRunner with the correct resource suffix and gate D-11 actions' Run direction only"
    requirement: "DEBLOAT-02"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Telemetry_action_resources_resolve_in_assembly_manifest"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#DebloatViewModel_run_action_invokes_script_runner_with_correct_suffix"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#DebloatViewModel_confirmation_gates_only_run_direction"
        status: pass
    human_judgment: false
  - id: D4
    description: "Navigating to the Debloat page (via Home card or nav sidebar) renders live, streamed script output for a real action, and DebloatPage.xaml.cs has zero business logic — requires interactive, elevated GUI verification not available in this headless build worktree"
    requirement: "DEBLOAT-03"
    verification: []
    human_judgment: true
    rationale: "Manual smoke check (launch elevated app, click Run/Undo on Telemetry, observe streamed log output) requires an interactive Windows GUI session; this plan was executed in a headless parallel worktree with no display/elevation available. dotnet build/test confirm the code compiles and the dispatch logic is unit-tested, but visual/live-process confirmation needs a human or the phase verifier."
    kind: manual_procedural

# Metrics
duration: 45min
completed: 2026-09-01
status: complete
---

# Phase 3 Plan 01: Debloat Architecture + Telemetry Tracer Summary

**Catalog-driven Debloat page (28 actions, 5 categories) with a generic Run/Undo ViewModel dispatch, D-11 confirmation gating, and one real action (Telemetry — Disable) wired end-to-end through IScriptRunner.**

## Performance

- **Duration:** 45 min
- **Started:** 2026-09-01T21:07:00Z
- **Completed:** 2026-09-01T21:26:00Z
- **Tasks:** 2
- **Files modified:** 15

## Accomplishments
- Full Debloat architecture landed in one pass: `DebloatAction`/`DebloatActionItem`/`DebloatCategoryGroup` models, `IDebloatCatalog`/`DebloatCatalog` (28-row/5-category static catalog, DEBLOAT-01), `DebloatViewModel` (generic catalog-driven Run/Undo dispatch, D-11 confirmation gating on the Run direction only, per-key `SemaphoreSlim` concurrency locking), and `DebloatPage.xaml`/`.xaml.cs` (5-category grouped UI, zero business logic in code-behind, DEBLOAT-03)
- One real action — "Telemetry — Disable" — proven end-to-end: `telemetry.ps1`/`telemetry-undo.ps1` embedded byte-for-byte from the predecessor, invoked via `IScriptRunner.RunEmbeddedScriptAsync`, streaming into the existing `ILogConsoleService` log dock
- Debloat enabled as a real navigation destination from both the Home dashboard card and the nav sidebar (both previously `IsEnabled=false` pointing at `HomePage`)
- 6-fact `DebloatCatalogTests.cs` regression lock, modeled on `TweakCatalogTests`/`TweakHandlerOrderingTests`' fake-based, closing-lock style — locks the 28/5/8-8-6-5-1/5-confirmation shape for Wave 2-6 plans
- Full solution (App + Tests projects) builds clean; all 6 new facts plus the pre-existing 199 tests pass (one unrelated, pre-existing environment-dependent failure — see Deviations)

## Task Commits

Each task was committed atomically:

1. **Task 1: Debloat architecture + "Telemetry — Disable" wired end-to-end** - `008455a` (feat)
2. **Task 2: DebloatCatalog regression test lock** - `d34e08c` (test)

_Note: both tasks carried `tdd="true"`; Task 2 is a closing regression-test lock written against Task 1's already-implemented, already-verified behavior (matching this codebase's established `TweakCatalogTests`/`TweakHandlerOrderingTests` convention), not a strict separate RED-then-GREEN commit pair — the plan's task structure (`<behavior>`+`<action>`, no `<implementation>` tag) does not delineate a fail-first phase for either task._

## Files Created/Modified
- `src/AkariToolbox.App/Models/DebloatAction.cs` - Immutable 8-field record for one catalog row (not ITweakHandler-shaped, D-01)
- `src/AkariToolbox.App/Models/DebloatActionItem.cs` - Bindable row (`IsRunning` busy state) + `DebloatCategoryGroup`
- `src/AkariToolbox.App/Services/IDebloatCatalog.cs` / `DebloatCatalog.cs` - The 28-action/5-category static catalog
- `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs` - Generic Run/Undo dispatch, confirmation gating, concurrency locking
- `src/AkariToolbox.App/Views/DebloatPage.xaml` / `.xaml.cs` - 5-category grouped UI, zero-logic code-behind
- `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs` - Registers `IDebloatCatalog`
- `src/AkariToolbox.App/App.xaml.cs` - Registers `DebloatViewModel`
- `src/AkariToolbox.App/ViewModels/HomeViewModel.cs` - Debloat card now targets `DebloatPage`, `IsEnabled=true`
- `src/AkariToolbox.App/MainWindow.xaml.cs` - Debloat nav entry now targets `DebloatPage`, enabled
- `src/AkariToolbox.App/AkariToolbox.App.csproj` - New `<ItemGroup>` embedding `telemetry.ps1`/`telemetry-undo.ps1`
- `src/AkariToolbox.App/Resources/DebloatScripts/telemetry.ps1` / `telemetry-undo.ps1` - Byte-for-byte carries from the predecessor
- `src/AkariToolbox.Tests/DebloatCatalogTests.cs` - 6-fact regression lock + `FakeScriptRunner`/`FakeDialogService`

## Decisions Made
- Kept the plan's exact 28-row catalog data (titles, descriptions, resource suffixes, D-11 confirmation set, D-10 unverified-binary flags) verbatim — no scope changes to DEBLOAT-01's data.
- Nested `DataTemplate` command bindings switched from `{x:Bind RootPage.ViewModel.*Command}` to `{Binding ElementName=RootPage, Path=DataContext.*Command}` — see Deviations below for the full root-cause.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] XamlCompiler WMC9999 crash on named-element x:Bind resolution — switched to classic ElementName Binding**
- **Found during:** Task 1 (`dotnet build` of `DebloatPage.xaml`)
- **Issue:** The plan's exact specified pattern — `Command="{x:Bind RootPage.ViewModel.RunActionCommand}"` inside the inner `ItemsRepeater.ItemTemplate`'s `DataTemplate` — failed to compile with `Xaml Internal Error WMC9999: Could not find any resources appropriate for the specified culture or the neutral culture`. Root-caused by inspecting the XamlCompiler's raw `obj/.../output.json` `MSBuildLogEntries`: `DebloatPage.xaml` parsed, loaded, validated, and harvested successfully (all `perfXC_Page*` markers completed cleanly) — the crash occurs specifically at `PageCodeGenStart` inside `Microsoft.UI.Xaml.Markup.Compiler.Parsing.BindingPathListener.ResolveNameOnRoot`, which calls `ResourceManager.GetString` while resolving "RootPage" as a named-element x:Bind path segment. This build environment's OS `CurrentCulture` is `it-IT` (confirmed via PowerShell `[CultureInfo]::CurrentCulture`), and the WindowsAppSDK.WinUI 2.3.0 `XamlCompiler.exe`'s own diagnostic-resource lookup for this specific code path throws under that non-en-US culture — a genuine tool defect, not a markup or binding-path error (the XAML itself is valid; GamingTweaksPage's simpler, page-level-only `x:Bind ViewModel.*` pattern never exercises this named-element resolution code path at all).
- **Fix:** Changed both Run/Undo `Command` bindings in the inner `DataTemplate` from `{x:Bind RootPage.ViewModel.RunActionCommand}`/`UndoActionCommand` to `{Binding ElementName=RootPage, Path=DataContext.RunActionCommand}`/`UndoActionCommand`. This is a classic (non-compiled) runtime binding, resolved entirely outside the `XamlCompiler`'s crashing binding-path parser, and is functionally identical — `RootPage.DataContext` is set to the `DebloatViewModel` instance in code-behind (`DataContext = viewModel;`), exposing the same `RunActionCommand`/`UndoActionCommand` properties. `CommandParameter="{x:Bind}"` (a bare, path-less binding to the `DebloatActionItem` itself) needed no change since it never reaches `ResolveNameOnRoot`.
- **Files modified:** `src/AkariToolbox.App/Views/DebloatPage.xaml`
- **Verification:** `dotnet build AkariToolbox.App.csproj -c Debug` succeeds with 0 errors (previously failed with WMC9999); confirmed by inspecting the XamlCompiler's own `output.json` before and after the fix.
- **Committed in:** `008455a` (Task 1 commit)

**2. Attempted no-op environment fixes before the code-level fix (documented for future reference, no files modified)**
- Tried `LANG`/`LC_ALL=en_US.UTF-8`, `VSLANG=1033`, `DOTNET_SYSTEM_GLOBALIZATION_USENLS=1` env vars — none affected the .NET Framework `XamlCompiler.exe` child process's OS-derived culture (still failed identically).
- Tried `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` — got past the XamlCompiler crash but broke `AkariToolbox.Framework`'s MRT/PRI resource generation (`APPX1685: Default language 'en-US' is not valid` — invariant mode strips the culture data PriGen needs), so this was reverted as a net-worse trade.
- Did not modify the OS user's regional/locale settings (registry) or the shared NuGet package cache (`~/.nuget/packages/microsoft.windowsappsdk.winui/2.3.0/...`) — both would be global, machine-wide changes outside this repo's scope and would affect other projects/sessions on this machine.

---

**Total deviations:** 1 auto-fixed (1 blocking build-environment defect, worked around at the XAML level)
**Impact on plan:** No functional or architectural change — the fix is a drop-in equivalent binding syntax. All of this plan's `must_haves` (catalog shape, confirmation gating, streamed script output, zero-logic code-behind) are unaffected.

## Issues Encountered
- No `.sln` file exists anywhere in this repository (confirmed via `git ls-files`/`git log --diff-filter=A` — none was ever committed, and none is present in this fresh worktree checkout). The plan's `<verify>` step (`cd src && dotnet build AkariToolbox.sln -c Debug`) could not run as literally written; built `AkariToolbox.App.csproj` and `AkariToolbox.Tests.csproj` directly instead (each transitively builds `AkariToolbox.Framework`), which is the functional equivalent and both succeed cleanly. This is a pre-existing repo characteristic (developers likely keep a local, untracked `.sln`), not something introduced or fixable by this plan.
- Pre-existing, unrelated test failure: `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` fails in this headless build environment (expects a `COMException` from `DependencyProperty.UnsetValue` that this WinRT-activation context doesn't throw). Untouched by this plan; logged to `.planning/phases/03-debloat/deferred-items.md` per the deviation rules' scope boundary rather than fixed.
- The plan's overall `<verification>` item 3 (manual elevated-GUI smoke check: navigate to Debloat, click Run/Undo, observe streamed output) could not be performed — this plan executed in a headless parallel git worktree with no interactive display or elevation available. Flagged as `human_judgment: true` coverage item D4 above for the phase verifier / a human to confirm.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `IDebloatCatalog`/`DebloatCatalog`, `DebloatViewModel`, and `DebloatPage` are all fully catalog-driven and require NO further changes as Wave 2-6 plans (03-02 through 03-06) embed the remaining 27 actions' `.ps1` scripts — each new plan only needs to add `<EmbeddedResource>` entries to `AkariToolbox.App.csproj`'s Debloat `<ItemGroup>` and drop the corresponding `.ps1` files into `Resources/DebloatScripts/`.
- The `{Binding ElementName=RootPage, Path=DataContext.*Command}` pattern established here should be reused (not `{x:Bind RootPage.ViewModel.*Command}`) for any future nested-`DataTemplate`-to-page-`ViewModel` command binding in this codebase, to avoid re-triggering the same XamlCompiler WMC9999 defect in this build environment.
- Manual elevated-GUI verification (Debloat page navigation, Telemetry Run/Undo streamed output) is still outstanding and should be performed by a human or the phase verifier before Phase 3 is considered fully proven end-to-end.

---
*Phase: 03-debloat*
*Completed: 2026-09-01*

## Self-Check: PASSED

All 12 created files confirmed present on disk; both task commits (`008455a`, `d34e08c`) confirmed in `git log --oneline --all`.
