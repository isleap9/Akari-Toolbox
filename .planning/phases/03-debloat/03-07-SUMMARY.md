---
phase: 03-debloat
plan: 07
subsystem: ui
tags: [winui3, mvvm, xaml, xbind, xunit, debloat]

# Dependency graph
requires:
  - phase: 03-debloat (waves 1-6)
    provides: "The complete 28-action/5-category Debloat catalog (DebloatCatalog), DebloatViewModel Run/Undo dispatch, and DebloatActionItem row model — this plan is the phase-closing integration/polish pass over that assembled work."
provides:
  - "DebloatActionItem.UndoDownloadsUnverifiedBinary bindable field, wired from DebloatCatalog through DebloatViewModel to the page"
  - "Per-row risk captions on DebloatPage.xaml — visible before a user clicks Run, closing RESEARCH.md Pitfall 5's UX gap"
  - "DebloatViewModel-level regression lock (CategoryGroups sequence/counts, total action count) — the ViewModel-layer analog to TweakHandlerOrderingTests, proven at the layer the page actually binds to"
affects: [04-downloads, 04-misc]

# Actuals (#2632)
actuals:
  tokens: 1505
  tasks: 2
  commits: 3

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Risk disclosure via x:Bind bool-to-Visibility caption TextBlocks (no converter needed), mirroring the existing HasUndo/IsRunning binding pattern from 03-01"

key-files:
  created: []
  modified:
    - src/AkariToolbox.App/Models/DebloatActionItem.cs
    - src/AkariToolbox.App/ViewModels/DebloatViewModel.cs
    - src/AkariToolbox.App/Views/DebloatPage.xaml
    - src/AkariToolbox.Tests/DebloatCatalogTests.cs

key-decisions:
  - "Task 2's two regression-lock facts intentionally pass immediately on first run (no RED failure) — they lock down catalog/ViewModel behavior already implemented across Waves 1-6, matching the plan's own framing as a 01-07/02-07-style closing integration pass, not new feature TDD."
  - "Used AkariToolbox.slnx from the repo root for build/test verification (not src/AkariToolbox.sln, which does not exist) — same correction 03-06-SUMMARY.md documented for the same plan-authored verify-command typo."

patterns-established: []

requirements-completed: [DEBLOAT-01, DEBLOAT-02, DEBLOAT-03]

coverage:
  - id: D1
    description: "DebloatActionItem exposes UndoDownloadsUnverifiedBinary, populated from the catalog by DebloatViewModel, without disturbing the pre-existing RequiresConfirmation field"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#DebloatViewModel_populates_UndoDownloadsUnverifiedBinary_from_catalog"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#DebloatViewModel_populates_RequiresConfirmation_from_catalog_unchanged"
        status: pass
    human_judgment: false
  - id: D2
    description: "DebloatPage.xaml shows a caution caption per row for D-11-confirmation-gated actions and for actions whose Undo downloads an unverified binary, purely via x:Bind (no code-behind logic added)"
    requirement: "DEBLOAT-03"
    verification:
      - kind: unit
        ref: "dotnet build AkariToolbox.slnx -c Debug"
        status: pass
    human_judgment: true
    rationale: "Visual rendering of the two new caption TextBlocks (correct row placement, correct caption text per D-11/D-10 classification, no caption on the other 23 rows) can only be confirmed by launching the elevated app and viewing the Debloat page — not exercisable from this non-Windows-elevated execution context."
  - id: D3
    description: "DebloatViewModel's CategoryGroups matches the predecessor's exact 5-category sequence and [8,8,6,5,1] counts (28 total), locked at the ViewModel layer the page actually binds to"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#DebloatViewModel_CategoryGroups_matches_predecessors_exact_category_sequence_and_counts"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#DebloatViewModel_total_action_count_is_28"
        status: pass
    human_judgment: false
  - id: D4
    description: "Full DebloatCatalogTests suite (03-01 through 03-07, 44 facts) passes and the solution builds clean"
    requirement: "DEBLOAT-02"
    verification:
      - kind: unit
        ref: "dotnet test AkariToolbox.Tests --filter FullyQualifiedName~DebloatCatalogTests"
        status: pass
    human_judgment: false

duration: 20min
completed: 2026-09-02
status: complete
---

# Phase 3 Plan 7: Debloat Phase Close — Risk Captions and Regression Lock Summary

**Per-row risk-disclosure captions (RequiresConfirmation/UndoDownloadsUnverifiedBinary) bound into DebloatPage.xaml via x:Bind, plus a DebloatViewModel-level closing regression lock — completing DEBLOAT-01/02/03.**

## Performance

- **Duration:** ~20 min
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments
- Added `DebloatActionItem.UndoDownloadsUnverifiedBinary`, wired from `DebloatAction` through `DebloatViewModel`'s `CategoryGroups` projection (the field already existed on `DebloatAction`/was already read in `ExecuteAsync`'s accepted-risk log line, but was never surfaced to the bindable row item until this plan)
- Added two `x:Bind`-driven caption `TextBlock`s to `DebloatPage.xaml` — a caution caption for D-11 confirmation-gated rows (BitLocker, Bloatware, Edge & WebView Remove, Hibernation, OneDrive) and a separate caption for D-10 unverified-download-on-Undo rows (Edge & WebView Remove, Edge Debloat) — closing RESEARCH.md's Pitfall 5 UX gap with zero code-behind logic added
- Added a `DebloatViewModel`-level regression lock (`CategoryGroups` exact category sequence/counts, total-28 count) — the ViewModel-layer analog to `TweakHandlerOrderingTests`'s "resolve exactly N, no gaps" pattern, proven at the layer the page actually binds to (not just the catalog layer covered by 03-01/03-06)
- Full `DebloatCatalogTests` suite: 44 facts pass (0 failed), `dotnet build AkariToolbox.slnx -c Debug` clean

## Task Commits

Each task was committed atomically (TDD RED/GREEN for Task 1; regression-lock test for Task 2):

1. **Task 1 RED: failing tests for risk caption bindings** - `010b03e` (test) — confirmed RED: build failed with `CS1061` (`DebloatActionItem` has no `UndoDownloadsUnverifiedBinary` member)
2. **Task 1 GREEN: per-row risk captions implementation** - `09b5da2` (feat) — model field, ViewModel wiring, XAML captions; confirmed GREEN: build clean, 42/42 tests pass
3. **Task 2: phase-closing DebloatViewModel regression lock** - `50bce18` (test) — passed immediately (locks already-correct behavior from Waves 1-6, not new feature TDD); 44/44 tests pass

**Plan metadata:** SUMMARY commit (this plan's docs commit, added next)

## Files Created/Modified
- `src/AkariToolbox.App/Models/DebloatActionItem.cs` - Added `UndoDownloadsUnverifiedBinary` bindable bool field
- `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs` - Wired `UndoDownloadsUnverifiedBinary` from `DebloatAction` into the `DebloatActionItem` object initializer
- `src/AkariToolbox.App/Views/DebloatPage.xaml` - Added two caption `TextBlock`s (RequiresConfirmation, UndoDownloadsUnverifiedBinary) inside the row `DataTemplate`, `x:Bind` only, no converters
- `src/AkariToolbox.Tests/DebloatCatalogTests.cs` - Added 4 new `[Fact]`s: 2 for Task 1's risk-caption data binding, 2 for Task 2's phase-closing regression lock

## Decisions Made
- Task 2's regression-lock facts were written to pass immediately rather than fail-then-pass — they lock down behavior already correctly implemented across Waves 1-6 (per this plan's own framing as a 01-07/02-07-style closing integration pass), so there was no production code change to drive with a RED phase for that task.
- Used `AkariToolbox.slnx` (repo root) for all build/test verification, not `src/AkariToolbox.sln` as literally written in the plan's `<verify>` blocks — the solution file lives at the repo root as `.slnx`, not under `src/` as `.sln`. Same typo 03-06-SUMMARY.md documented; not a code issue.

## Deviations from Plan

None - plan executed exactly as written (build-command path correction is a `<verify>` typo fix, already precedented in 03-06-SUMMARY.md, not a functional deviation).

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- DEBLOAT-01, DEBLOAT-02, and DEBLOAT-03 are all fully satisfied. Phase 3 (Debloat) is feature-complete: 28/28 actions functional across 5 categories, streamed output proven, zero-logic code-behind proven, D-11 confirmation gate proven, D-10 accepted-risk disclosure proven both in logs and now in the UI.
- Recommended before considering the phase fully UAT-verified: a manual pass on a live elevated Windows machine — visit the Debloat page, confirm all 28 rows render across 5 categories, confirm the 5 D-11 rows and 2 unverified-download rows show their new captions (Edge & WebView Remove shows both), spot-check 3-4 actions across categories (at least one Run-only, one Run+Undo, one D-11-gated) execute end-to-end with streamed log output. Not exercisable from this non-Windows-elevated execution context (same limitation 03-06-SUMMARY.md and prior phase-closing plans documented).
- Phase 4 (Downloads/Misc) can proceed; no blockers carried forward from this plan.

## Self-Check: PASSED

All 4 modified source files found on disk; SUMMARY.md found on disk; all 4 commit hashes (010b03e, 09b5da2, 50bce18, b055707) found in `git log`.

---
*Phase: 03-debloat*
*Completed: 2026-09-02*
