---
phase: 01-trusted-catalog-foundation
plan: 01-03
subsystem: gaming-catalog
tags: [gaming-journal, revert-safety, first-capture-wins, xunit, registry]

# Dependency graph
requires:
  - phase: 01-01
    provides: GamingJournalStore capture/MarkApplied/RevertAll plus scratch-key journal round-trip facts
provides:
  - First-capture-wins MarkApplied guard so sticky-tick re-runs keep the original revert baseline
  - Apply-apply-revert regression fact proving the true original restores
affects: [phase-2-tweak-waves, verify-work-uat]

# Actuals (#2632) — pairs with the plan's `estimate` to calibrate future estimates.
# Same estimateTokens scale (chars/4 over the realized diff), never a harness token count.
actuals:
  tokens: 1200
  tasks: 1
  commits: 1

# Tech tracking
tech-stack:
  added: []
  patterns: [first-capture-wins-journal-guard]

key-files:
  created: []
  modified:
    - src/AppTemplate.App/Tweaks/GamingJournal.cs
    - src/AppTemplate.Tests/GamingCatalogTests.cs

key-decisions:
  - "First-capture-wins (not replace): a persisted entry for the catalog ID is kept and later captures are dropped, because the UI encourages sticky-tick re-runs and the original prior must survive them"
  - "Pending slot still cleared on the dropped path so the in-memory map cannot leak a stale capture into a later row"

patterns-established:
  - "Journal guard: LoadLocked, check existing entry by CatalogId, early-return before SaveLocked when present"

requirements-completed: [SFT-01]

# Coverage metadata (#1602)
coverage:
  - id: D1
    description: "MarkApplied is first-capture-wins; apply-apply-revert on a scratch Sentinel restores the first original"
    requirement: "SFT-01"
    verification:
      - kind: unit
        ref: "src/AppTemplate.Tests/GamingCatalogTests.cs#Journal_rerun_keeps_first_capture_so_revert_restores_original"
        status: pass
    human_judgment: false
  - id: D2
    description: "Single-run journal round-trips unaffected; build bar holds and full suite is green"
    requirement: "SFT-01"
    verification:
      - kind: unit
        ref: "dotnet build src/AppTemplate.App/AppTemplate.App.csproj -c Debug => 0 Warning(s) 0 Error(s); dotnet test => 119/119 Passed (20/20 GamingCatalog)"
        status: pass
    human_judgment: false

# Metrics
duration: ~10min
completed: 2026-10-04
status: complete
---

# Phase 01 Plan 03: CR-01 Gap Closure Summary

**First-capture-wins journal guard plus apply-apply-revert regression test — the sticky-tick re-run can no longer clobber the revert baseline**

## Performance

- **Duration:** ~10 min (single inline session)
- **Started:** 2026-10-04
- **Completed:** 2026-10-04
- **Tasks:** 1 of 1
- **Files modified:** 2

## Accomplishments

- `GamingJournalStore.MarkApplied` is now first-capture-wins: after removing the pending capture and loading the persisted doc, it returns before `SaveLocked` when an entry for the catalog ID already exists (pending slot still cleared), so the original baseline survives re-runs
- Doc comment corrected: no longer claims re-capture replaces the snapshot; states persisted entries are kept and later captures dropped
- New regression fact `Journal_rerun_keeps_first_capture_so_revert_restores_original` performs capture-mutate-mark twice then `RevertAll` and asserts the live Sentinel reads back the first original `0xDEADBEEF` (not the intermediate applied value), using the file's existing scratch-key pattern (HKCU `AkariToolboxTests` + temp journal, `DropScratch` cleanup)
- Gap proof green: GamingCatalog filter 20/20, full suite 119/119, app build 0 Warning(s) 0 Error(s)

## Task Commits

Each task was committed atomically:

1. **Task 1: First-capture-wins MarkApplied guard plus apply-twice-then-revert regression test** - `495a7e6` (fix)

**Plan metadata:** SUMMARY commit follows (docs: complete plan)

## Files Created/Modified

- `src/AppTemplate.App/Tweaks/GamingJournal.cs` - First-capture-wins guard in `MarkApplied` plus corrected doc comment; `Capture`, `RevertAll`, `Clear`, `LoadLocked`, `SaveLocked`, and schema/version untouched
- `src/AppTemplate.Tests/GamingCatalogTests.cs` - `Journal_rerun_keeps_first_capture_so_revert_restores_original` regression fact; suite now 20 GamingCatalog facts, 119 total

## Decisions Made

- First-capture-wins over replace: the UI's own success copy ("Ticks stay selected for re-run") encourages the apply-apply path, so the journal must treat the first capture as the revert authority. The dropped-capture path still clears `_pending` for the id to avoid leaking a stale in-memory entry.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. PowerShell shell (not bash): used `Select-Object` instead of `tail` for build/test output. No impact on outputs.

## Threat Model Review (T-01-05 / T-01-SC)

- **T-01-05 (journal tampering, mitigate):** Guard skips `SaveLocked` only when a persisted entry for the catalog ID exists, so re-run captures can never overwrite the original baseline; regression test asserts apply-twice-then-revert restores the first original.
- **T-01-SC (package installs, mitigate):** Zero installs; gate not triggered.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- CR-01 closed: verification truth 6 (revert restores captured priors) now holds on the re-run path; the SFT-01 revert-half trust anchor is restored for Phase 2 tweak waves.
- Open manual items unchanged: in-app UAT of the four phase success criteria on an elevated desktop should explicitly include an apply-apply-revert cycle; plus the `verify-pages.ps1 -Exe <real path>` sweep covering all 14 nav items.
- Known stubs: none. Full-suite: 119/119. Build bar: Errors 0 Warnings 0.

## Self-Check: PASSED

- `MarkApplied` contains the existing-entry check on `CatalogId` returning before `SaveLocked`; doc comment no longer contains `replaces its snapshot` (grep clean).
- New fact `Journal_rerun_keeps_first_capture_so_revert_restores_original` present, performing capture-mutate-mark twice then `RevertAll` asserting the first original.
- No other production behavior changed (`Capture`/`RevertAll`/`Clear`/`LoadLocked`/`SaveLocked`/schema untouched).
- GamingCatalog filter 20/20 including the new fact; full suite 119/119; app build 0 Warning(s) 0 Error(s).

---
*Phase: 01-trusted-catalog-foundation*
*Completed: 2026-10-04*
