---
phase: 01-trusted-catalog-foundation
plan: 01-01
subsystem: gaming-catalog
tags: [winui3, mvvm-toolkit, registry, snapshot-journal, xunit, gaming-tweaks]

# Dependency graph
requires: []
provides:
  - GamingCatalogEntry descriptor catalog (8 seeds, 4 groups) with title-only prefix-first search
  - GamingJournalStore snapshot journal with reverse-replay revert
  - GamingRunner UI-free bulk apply loop with stop-at-first-failure prefix journal
  - Gaming Catalog page + VM wired into shell nav, footer progress, restore-point offer, one-click revert
  - 18 xUnit facts covering catalog integrity, search ranking, journal round-trip, bulk semantics
affects: [01-02-search-copy-states-gates, phase-2-tweak-waves, phase-3-polish]

# Actuals (#2632) — pairs with the plan's `estimate` to calibrate future estimates.
# Same estimateTokens scale (chars/4 over the realized diff), never a harness token count.
actuals:
  tokens: 18300
  tasks: 3
  commits: 3

# Tech tracking
tech-stack:
  added: []
  patterns: [descriptor-catalog-with-footprint, snapshot-before-apply-journal, ui-free-runner-with-completed-prefix]

key-files:
  created:
    - src/AppTemplate.App/Tweaks/GamingCatalog.cs
    - src/AppTemplate.App/Tweaks/GamingJournal.cs
    - src/AppTemplate.App/Tweaks/GamingRunner.cs
    - src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs
    - src/AppTemplate.App/Views/GamingCatalogPage.xaml
    - src/AppTemplate.App/Views/GamingCatalogPage.xaml.cs
    - src/AppTemplate.Tests/GamingCatalogTests.cs
  modified:
    - src/AppTemplate.App/Tweaks/GamingActions.cs
    - src/AppTemplate.App/App.xaml.cs
    - src/AppTemplate.App/MainWindow.xaml.cs
    - src/AppTemplate.Tests/AppTemplate.Tests.csproj
    - src/AppTemplate.Tests/ConvertersTests.cs

key-decisions:
  - "Kept the prior run's GamingRunner.cs split (UI-free static runner) instead of folding the loop into the VM — it makes stop-at-first-failure unit-testable"
  - "Added an App ProjectReference to the test project (was Framework-only) so UI-free domain logic is testable"
  - "Made the EnumToBoolean UnsetValue assertion host-robust instead of assuming a bare testhost"

patterns-established:
  - "Descriptor catalog: GamingCatalogEntry record with FromNative/FromSharedRow factories beside AkariTweakCatalog"
  - "Snapshot journal: dedicated versioned JSON file, atomic temp-plus-move writes, corrupt degrades to empty, absent Kind null means delete-on-revert"
  - "Bulk runner: sequential capture-plus-apply per row, stop at first failure, completed-prefix journal, reverse replay with continue-on-error"
  - "Page wiring: transient VM plus page in BuildHost, one NavItems entry, page root Binding with DataContext, xBind only in DataTemplate"

requirements-completed: [BRW-01, BRW-02, SFT-01]

# Coverage metadata (#1602)
coverage:
  - id: D1
    description: "Gaming catalog seeds preemption, network-opt, game-mode, game-dvr plus 4 shared-row cross-references with zero duplicated .reg text"
    requirement: "BRW-01"
    verification:
      - kind: unit
        ref: "src/AppTemplate.Tests/GamingCatalogTests.cs#Catalog_ids_are_unique_and_stable + Catalog_shared_row_ids_resolve_against_control_panel_rows"
        status: pass
    human_judgment: false
  - id: D2
    description: "Every row renders What/Why/Risk/Reverts-by inline, one sentence each, no implementation locations named"
    requirement: "BRW-02"
    verification:
      - kind: unit
        ref: "src/AppTemplate.Tests/GamingCatalogTests.cs#Catalog_every_entry_has_four_non_empty_explanation_lines + Catalog_explanations_name_no_implementation_locations"
        status: pass
    human_judgment: false
  - id: D3
    description: "Title-only prefix-first search, first-character trigger, 8-item cap, tap-to-filter without ticking"
    requirement: "BRW-01"
    verification:
      - kind: unit
        ref: "src/AppTemplate.Tests/GamingCatalogTests.cs#Suggest_prefix_matches_rank_above_substring_matches + Suggest_matches_titles_only_not_explanations + Suggest_triggers_from_first_character_and_caps_at_eight"
        status: pass
    human_judgment: false
  - id: D4
    description: "Journal captures full footprints and reverse-replays them, including absent-to-delete semantics"
    requirement: "SFT-01"
    verification:
      - kind: unit
        ref: "src/AppTemplate.Tests/GamingCatalogTests.cs#Journal_capture_then_mutate_then_revert_restores_sentinel + Journal_absent_value_reverts_by_deleting_not_zero_filling + Journal_multi_value_row_captures_every_footprint_value + Journal_revert_replays_in_reverse_apply_order"
        status: pass
    human_judgment: false
  - id: D5
    description: "Bulk apply acts on ticked rows with footer progress, stops at first failure, ticks stay sticky, restore offered with Skip, one-click revert"
    requirement: "SFT-01"
    verification:
      - kind: unit
        ref: "src/AppTemplate.Tests/GamingCatalogTests.cs#Runner_stop_at_first_failure_leaves_completed_prefix_journal + Runner_success_applies_all_and_reports_progress"
        status: pass
    human_judgment: false
  - id: D6
    description: "Catalog page reachable from shell nav, four groups render with ticked rows, reboot badges inline, empty state on zero matches"
    requirement: "BRW-01"
    verification:
      - kind: manual_procedural
        ref: "build-and-run.ps1 then open Gaming Catalog from nav, tick rows, apply, revert"
        status: unknown
    human_judgment: true
    rationale: "XAML rendering, nav presence, and live registry read-back need a human on an elevated Windows desktop"

# Metrics
duration: ~1h
completed: 2026-10-04
status: complete
---

# Phase 01 Plan 01: Tracer Catalog Slice Summary

**Gaming catalog tracer: 8-row descriptor catalog with snapshot journal, bulk runner, catalog page, and 18 passing xUnit facts**

## Performance

- **Duration:** ~1h (single execution session, re-execute-from-scratch keeping partial files)
- **Started:** 2026-10-04 (continuation of 2026-10-03 prior run)
- **Completed:** 2026-10-04
- **Tasks:** 3 of 3
- **Files modified:** 12 (7 created for catalog/journal/page, 1 created for tests, 4 extended/edited)

## Accomplishments

- GamingCatalogEntry descriptor with GamingGroup enum, FromNative/FromSharedRow factories, and 8 seeds (preemption, game-mode, network-opt, game-dvr natives plus 4 shared-row cross-references by ID, fail-fast on rename)
- Game Mode and Game DVR natives carved out of the fso key set with interactive-user HKCU resolution; fso toggle untouched
- GamingJournalStore snapshot journal (versioned JSON, atomic writes, corrupt degrades to empty) capturing full footprints before each apply and reverse-replaying with continue-on-error
- UI-free GamingRunner bulk loop: sequential capture-plus-apply, stop at first failure, completed-prefix journal, determinate footer progress, sticky ticks
- Gaming Catalog page in shell nav with AutoSuggestBox search (title-only, prefix-first, cap 8, tap-to-filter), four group cards with inline explanations and Requires-restart badges, Apply selected (N), one-click Revert all changes, restore-point offer with Skip path
- 18 GamingCatalog xUnit facts green; full suite 117/117 green; app build Errors 0 Warnings 0

## Task Commits

Each task was committed atomically:

1. **Task 1: GamingCatalogEntry descriptor plus Game Mode/DVR natives plus seed list** - `1588414` (feat, prior run — verified, not re-committed)
2. **Task 2: GamingJournal snapshot file plus catalog page, VM, and shell registration** - `b0cbbfc` (feat)
3. **Task 3: xUnit coverage for catalog integrity, search ranking, and journal round-trip** - `1446516` (test)

**Plan metadata:** SUMMARY commit follows (docs: complete plan)

## Files Created/Modified

- `src/AppTemplate.App/Tweaks/GamingCatalog.cs` - GamingGroup enum plus GamingCatalogEntry record, factories, Suggest/MatchesFilter, 8-row All seed list (Task 1, committed `1588414`)
- `src/AppTemplate.App/Tweaks/GamingActions.cs` - ReadGameMode/SetGameMode, ReadGameDvr/SetGameDvr, EnumerateNetworkFootprint (Task 1, committed `1588414`)
- `src/AppTemplate.App/Tweaks/GamingJournal.cs` - GamingJournalStore plus JournalValue/JournalEntry records, Capture/MarkApplied/RevertAll, full-body .reg footprint parser (Task 2)
- `src/AppTemplate.App/Tweaks/GamingRunner.cs` - UI-free sequential bulk runner with BulkProgress/BulkResult (Task 2, extra file beyond plan file list)
- `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs` - Search state, per-group filtered collections, ApplySelectedAsync, RevertAllAsync, GamingCatalogRowItem co-located (Task 2)
- `src/AppTemplate.App/Views/GamingCatalogPage.xaml` - Top bar plus four group cards plus empty state, page-root Binding with DataContext (Task 2)
- `src/AppTemplate.App/Views/GamingCatalogPage.xaml.cs` - 3-line code-behind plus AutoSuggestBox handlers (Task 2)
- `src/AppTemplate.App/App.xaml.cs` - AddTransient GamingCatalogViewModel plus GamingCatalogPage (Task 2)
- `src/AppTemplate.App/MainWindow.xaml.cs` - Gaming Catalog NavItems entry after Gaming Tweaks (Task 2, was missing from partial files)
- `src/AppTemplate.Tests/GamingCatalogTests.cs` - 18 facts: integrity, ranking, journal round-trip, bulk semantics (Task 3)
- `src/AppTemplate.Tests/AppTemplate.Tests.csproj` - ProjectReference to AppTemplate.App (Task 3)
- `src/AppTemplate.Tests/ConvertersTests.cs` - Host-robust UnsetValue assertion (Task 3, collateral fix)

## Decisions Made

- Kept the prior run's `GamingRunner.cs` split (UI-free static runner) instead of folding the bulk loop into the VM: stop-at-first-failure and completed-prefix semantics become unit-testable without UI types. The plan's file list did not name it; treating it as a kept structural choice, documented as deviation 2.
- Added an `AppTemplate.App` ProjectReference to the test project (it referenced Framework only): the catalog, journal, and runner live in App, and the tests must exercise them. Verified the WinUI app reference builds and runs UI-free tests under `dotnet test`.
- Made the `EnumToBoolean` UnsetValue assertion host-robust (accept resolved-UnsetValue or COMException) instead of assuming a bare testhost: loading the App assembly activates WinRT statics in testhost. Preserves the original intent (invalid input never yields a bindable enum value).
- Committed on `main` per the explicit sequential-mode dispatch (no worktree isolation), consistent with the prior Task 1 commit on `main`. Left orchestrator-owned `.planning/ROADMAP.md`, `.planning/STATE.md`, and untracked plan artifacts untouched.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Fixed `GamingCatalog.All` reference that does not exist**
- **Found during:** Task 2 (build verification of kept partial files)
- **Issue:** `GamingCatalogViewModel.cs:118` referenced `GamingCatalog.All`; the catalog class is `GamingCatalogEntry` (no `GamingCatalog` type exists). Build failed CS0103 plus cascading WMC errors.
- **Fix:** One-line change to `GamingCatalogEntry.All`.
- **Files modified:** `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs`
- **Verification:** `dotnet build` returns Errors 0 Warnings 0.
- **Committed in:** `b0cbbfc` (part of Task 2 commit)

**2. [Rule 3 - Blocking] Added missing shell nav entry for the catalog page**
- **Found during:** Task 2 (reconciling partial files against the plan's file list)
- **Issue:** Partial files included App.xaml.cs DI lines but `MainWindow.xaml.cs` had no `Gaming Catalog` NavItems entry, so the page was unreachable — a plan done-criterion ("opens from nav") failure.
- **Fix:** One-line `new("Gaming Catalog", "\uE7FC", typeof(GamingCatalogPage))` entry after Gaming Tweaks (applied via script because the `\u` glyph escape defeats literal text matching).
- **Files modified:** `src/AppTemplate.App/MainWindow.xaml.cs`
- **Verification:** Build green; entry verified in NavItems list.
- **Committed in:** `b0cbbfc` (part of Task 2 commit)

**3. [Rule 3 - Blocking] Added App ProjectReference so catalog tests compile**
- **Found during:** Task 3 (test creation)
- **Issue:** `AppTemplate.Tests.csproj` referenced Framework only; `GamingCatalogEntry`/`GamingJournalStore`/`GamingRunner` live in `AppTemplate.App`, so no test could touch them.
- **Fix:** Added `<ProjectReference Include="..\AppTemplate.App\AppTemplate.App.csproj" />`. No new packages; zero additional dependencies.
- **Files modified:** `src/AppTemplate.Tests/AppTemplate.Tests.csproj`
- **Verification:** `dotnet test --filter GamingCatalog` passes 18/18 with zero warnings after the xUnit2009 fix below.
- **Committed in:** `1446516` (part of Task 3 commit)

**4. [Rule 1 - Bug] Fixed xUnit2009 analyzer warning in new tests**
- **Found during:** Task 3 (first filtered test run: 18/18 passed with 1 warning)
- **Issue:** `Assert.True(title.StartsWith(...))` triggers xUnit2009; the build bar is Errors 0 Warnings 0.
- **Fix:** Replaced with `Assert.StartsWith(...)`.
- **Files modified:** `src/AppTemplate.Tests/GamingCatalogTests.cs`
- **Verification:** Filtered run shows zero warnings.
- **Committed in:** `1446516` (part of Task 3 commit)

**5. [Rule 1 - Bug] Fixed `EnumToBoolean_matches_parameter` failure caused by the new App reference**
- **Found during:** Task 3 (full-suite run: 116/117, one pre-existing-test failure)
- **Issue:** `ConvertersTests` asserted `DependencyProperty.UnsetValue` access throws COMException in testhost. Loading the App assembly (new reference) activates WinRT statics, so no exception is thrown — and each `UnsetValue` access returns a fresh projection wrapper, so identity comparison is meaningless too.
- **Fix:** Host-robust helper: attempt ConvertBack, assert the result is non-null and never a bindable enum value; still accept COMException for a bare host. Original intent preserved (invalid input never writes back).
- **Files modified:** `src/AppTemplate.Tests/ConvertersTests.cs`
- **Verification:** Full suite 117/117 green.
- **Committed in:** `1446516` (part of Task 3 commit)

---

**Total deviations:** 5 auto-fixed (2 Rule 1 bugs in kept partial files, 2 Rule 3 blocking gaps, 1 Rule 1 collateral test failure)
**Impact on plan:** All auto-fixes were necessary for correctness or to unblock planned work. No scope creep: GamingRunner.cs was kept (not added) from the prior run; the test-project reference adds zero packages.

## Issues Encountered

- PowerShell execution environment (not bash): `head`, `sed`, and `[ -f ... ]` guards from the protocol do not parse. Adapted with PowerShell-native equivalents (`Select-Object -First/Last`, `Select-String`, `Test-Path`). No impact on outputs.
- Edit-tool escape handling: `\uE7FC` glyph literals in `MainWindow.xaml.cs` cannot round-trip through text matching (decoded to the PUA character). Worked around with a placeholder edit plus a scripted regex replacement; final diff verified as one clean line.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Tracer foundation proven: descriptor plus journal plus runner architecture verified on real registry semantics (HKCU scratch round-trips) and fakes; later tweaks ride this foundation per D-01.
- Manual UAT still open (coverage D6): open Gaming Catalog from nav on an elevated desktop, search by title, tick rows, apply with footer progress, confirm sticky ticks, one-click revert with prior values restored by live read-back.
- Plan 01-02 hardens search, copy, states, and gates on top of this slice.

## Self-Check: PASSED

- All created files exist on disk (GamingCatalog.cs, GamingJournal.cs, GamingRunner.cs, GamingCatalogViewModel.cs, GamingCatalogPage.xaml/.xaml.cs, GamingCatalogTests.cs).
- All commits exist: `1588414`, `b0cbbfc`, `1446516` verified via `git log`.
- Build bar: app Errors 0 Warnings 0; filtered tests 18/18; full suite 117/117.

---
*Phase: 01-trusted-catalog-foundation*
*Completed: 2026-10-04*
