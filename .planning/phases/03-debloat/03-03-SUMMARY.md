---
phase: 03-debloat
plan: 03
subsystem: ui
tags: [winui3, powershell, debloat, embedded-resources, xunit, bitlocker]

# Dependency graph
requires:
  - phase: 03-debloat
    provides: "DebloatCatalog/DebloatViewModel/DebloatPage architecture, the proven telemetry.ps1/telemetry-undo.ps1 embedded-script wiring pattern (03-01-PLAN.md), and the branch-extraction technique for self-elevating 'Ultimate' collection console-menu scripts (03-02-PLAN.md's windowsai.ps1/windowsai-undo.ps1)"
provides:
  - "7 direct-carry embedded System & Performance Run+Undo script pairs (visualeffects, services, deliveryoptimization, hibernation, storagesense, wpbt, utc), byte-for-byte from the predecessor"
  - "disablebitlocker.ps1/disablebitlocker-undo.ps1 — D-12/D-13 branch-extracted from the 'Ultimate' collection's 3 Setup/1 BitLocker.ps1, self-elevation/menu/blocking-Pause scaffolding stripped, Undo intentionally settings-panel-only (no Enable-BitLocker call)"
  - "DebloatCatalogTests.SystemPerformance_category_all_actions_have_resolvable_resources — closing regression lock for the full 8-action System & Performance category"
affects: [03-04, 03-05, 03-06, 03-07]

# Actuals (#2632)
actuals:
  tokens: 4200
  tasks: 3
  commits: 3

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Blocking-input stripping for 'Ultimate' collection branches that call Pause before exit (not just Read-Host menu loops, as in 03-02's Copilot extraction): Pause internally waits on user input, which hangs forever against IScriptRunner's non-redirected-stdin child process — same hang class as 03-06's Pitfall 2. Drop the Pause call, keep the following exit."

key-files:
  created:
    - src/AkariToolbox.App/Resources/DebloatScripts/visualeffects.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/visualeffects-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/services.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/services-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/deliveryoptimization.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/deliveryoptimization-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/hibernation.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/hibernation-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/storagesense.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/storagesense-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/wpbt.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/wpbt-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/utc.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/utc-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/disablebitlocker.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/disablebitlocker-undo.ps1
  modified:
    - src/AkariToolbox.App/AkariToolbox.App.csproj
    - src/AkariToolbox.Tests/DebloatCatalogTests.cs

key-decisions:
  - "disablebitlocker.ps1/disablebitlocker-undo.ps1 sourced from the 'Ultimate' collection's 3 Setup/1 BitLocker.ps1 (D-12/D-13) rather than the predecessor's own DisableBitLocker.ps1/DisableBitLocker-Undo.ps1 pair — Undo branch only opens the BitLocker Control Panel for manual re-enable, does not call Enable-BitLocker; an explicit, already-made, accepted capability reduction versus the predecessor, not a bug introduced here"

patterns-established:
  - "Blocking-Pause stripping for 'Ultimate' collection branches (distinct from 03-02's Read-Host menu-loop stripping): both disablebitlocker branches called Pause immediately before their own exit; dropped per the same hang-avoidance reasoning as 03-06's Pitfall 2"

requirements-completed: [DEBLOAT-01, DEBLOAT-02]

coverage:
  - id: D1
    description: "7 direct-carry System & Performance scripts (Visual Effects, Services, Delivery Optimization, Hibernation, Storage Sense, WPBT, Set Time to UTC) embedded byte-for-byte and resolvable via the assembly manifest"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#SystemPerformance_category_all_actions_have_resolvable_resources"
        status: pass
    human_judgment: false
  - id: D2
    description: "disablebitlocker.ps1/disablebitlocker-undo.ps1 branch-extracted from 1 BitLocker.ps1 (D-12/D-13): self-elevation, console-cosmetic lines, and menu/Read-Host loop stripped; blocking Pause call stripped from both branches; both end in exit; Undo contains no Enable-BitLocker call (D-13 settings-panel-only scope)"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#SystemPerformance_category_all_actions_have_resolvable_resources"
        status: pass
      - kind: other
        ref: "grep -v '^#' disablebitlocker.ps1/disablebitlocker-undo.ps1 | grep -c '^Pause$' (both 0); grep -c Enable-BitLocker disablebitlocker-undo.ps1 (0)"
        status: pass
    human_judgment: false
  - id: D3
    description: "All 8 System & Performance actions (Run+Undo) resolve their embedded resource suffixes in the assembly manifest — category is architecturally complete"
    requirement: "DEBLOAT-02"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#SystemPerformance_category_all_actions_have_resolvable_resources"
        status: pass
    human_judgment: false
  - id: D4
    description: "Clicking Run on 'BitLocker — Disable' or 'Hibernation — Disable' shows the IDialogService confirmation prompt before anything runs (D-11), and clicking each of the 8 newly wired actions' Run/Undo buttons on the live Debloat page streams real script output with no error — requires interactive, elevated GUI verification not available in this headless build worktree"
    verification: []
    human_judgment: true
    rationale: "Manual smoke checks (confirmation-dialog-before-execution for BitLocker/Hibernation, live Run/Undo streamed output for all 8 System & Performance actions, BitLocker Control Panel opening for both Run and Undo) require an interactive, elevated Windows GUI session; this plan executed in a headless parallel worktree with no display/elevation available. dotnet build/test confirm the code compiles and resource resolution is unit-tested (including the existing, unmodified D-11 confirmation-gate unit test DebloatViewModel_confirmation_gates_only_run_direction), but visual/live-process confirmation needs a human or the phase verifier (matches 03-01-SUMMARY.md's and 03-02-SUMMARY.md's D4 precedent)."

# Metrics
duration: 20min
completed: 2026-09-01
status: complete
---

# Phase 3 Plan 03: System & Performance Category Completion Summary

**8/8 System & Performance Run+Undo script pairs embedded (7 byte-for-byte predecessor carries plus BitLocker's D-12/D-13 branch-extracted replacement with its blocking Pause call stripped), closing the second of 5 Debloat categories with a locking regression test.**

## Performance

- **Duration:** 20 min
- **Started:** 2026-09-01T21:40:00Z
- **Completed:** 2026-09-01T22:00:00Z
- **Tasks:** 3
- **Files modified:** 18

## Accomplishments
- Embedded 7 direct-carry System & Performance script pairs (Visual Effects, Services, Delivery Optimization, Hibernation, Storage Sense, WPBT, Set Time to UTC) byte-for-byte from the predecessor's `Scripts/` folder — 14 new `.ps1` files, all wired into `AkariToolbox.App.csproj`'s existing Debloat `<ItemGroup>`
- Branch-extracted `disablebitlocker.ps1`/`disablebitlocker-undo.ps1` (D-12/D-13) from the "Ultimate" collection's `3 Setup/1 BitLocker.ps1` — stripped the self-elevation block, console-cosmetic lines, the `Read-Host` menu-selection loop, AND the blocking `Pause` call that precedes each branch's `exit` (a hang risk `windowsai.ps1`'s Copilot extraction in 03-02 did not have to handle); confirmed Undo contains no `Enable-BitLocker` call, matching D-13's explicit settings-panel-only scope
- Added `SystemPerformance_category_all_actions_have_resolvable_resources` to `DebloatCatalogTests.cs`, closing the regression lock for all 8 System & Performance actions' Run+Undo resource resolution
- Full App project builds clean (0 errors); `DebloatCatalogTests` suite is green (8/8, including the new fact)

## Task Commits

Each task was committed atomically:

1. **Task 1: Embed the 7 remaining direct-carry System & Performance scripts** - `258aa02` (feat)
2. **Task 2: Extract "BitLocker — Disable" branches from 1 BitLocker.ps1 (D-12/D-13)** - `d167919` (feat)
3. **Task 3: Lock System & Performance resource resolution** - `a6e2d2d` (test)

## Files Created/Modified
- `src/AkariToolbox.App/Resources/DebloatScripts/visualeffects.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/services.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/deliveryoptimization.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/hibernation.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/storagesense.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/wpbt.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/utc.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/disablebitlocker.ps1` / `-undo.ps1` - D-12/D-13 branch-extracted from `1 BitLocker.ps1`, blocking Pause stripped, Undo settings-panel-only
- `src/AkariToolbox.App/AkariToolbox.App.csproj` - 16 new `<EmbeddedResource>` entries appended to the existing Debloat `<ItemGroup>` (Task 1: 14, Task 2: 2)
- `src/AkariToolbox.Tests/DebloatCatalogTests.cs` - New `SystemPerformance_category_all_actions_have_resolvable_resources` fact

## Decisions Made
- Followed the plan exactly for source selection: 7 pairs carried byte-for-byte from the predecessor's `Scripts/` folder with no transformation; `disablebitlocker.ps1`/`disablebitlocker-undo.ps1` branch-extracted from `1 BitLocker.ps1` per D-12/D-13 (catalog data — key, name, description, `RequiresConfirmation=true` — already reflects this from 03-01-PLAN.md, so no `DebloatCatalog.cs` changes were needed here).
- Split the `AkariToolbox.App.csproj` `<EmbeddedResource>` additions across Task 1 (14 direct-carry lines) and Task 2 (2 BitLocker lines) to keep each task's commit scoped to only the files that task's `<files>` list names, even though both edits landed in the same existing `<ItemGroup>`.

## Deviations from Plan

None - plan executed exactly as written. No `.sln` file exists in this repo (a pre-existing, documented repo characteristic per 03-01-SUMMARY.md/03-02-SUMMARY.md); built the solution via the repo-root `AkariToolbox.slnx` file and tested via `AkariToolbox.Tests.csproj` directly instead of the plan's literal `cd src && dotnet build AkariToolbox.sln` command, which is the established functional equivalent from the prior plans in this same phase.

## Issues Encountered
- Same pre-existing, unrelated `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` failure documented in 03-01-SUMMARY.md/03-02-SUMMARY.md (headless build environment doesn't throw the expected `COMException` from `DependencyProperty.UnsetValue`). Untouched by this plan, out of scope per the deviation rules' scope boundary. Not re-run here since the filtered `DebloatCatalogTests` test command doesn't include it, but noted for completeness.
- The plan's overall `<verification>` items 3-4 (manual elevated-GUI smoke checks: confirmation dialog before BitLocker/Hibernation execution; BitLocker Run disabling encryption and opening Control Panel; BitLocker Undo opening Control Panel without re-encrypting) could not be performed — this plan executed in a headless parallel git worktree with no interactive display or elevation available. Flagged as `human_judgment: true` coverage item D4 above for the phase verifier / a human to confirm, matching 03-01-SUMMARY.md's and 03-02-SUMMARY.md's D4 precedent.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- System & Performance category is now 100% architecturally complete (8/8 actions with resolvable Run+Undo embedded resources); `DebloatCatalog`/`DebloatViewModel`/`DebloatPage` require no further changes — remaining Wave plans (03-04 through 03-07) only need to add `<EmbeddedResource>` entries plus `.ps1` files for their own categories, following this plan's and 03-01/03-02's established pattern.
- The branch-extraction technique for self-elevating "Ultimate" collection scripts is now proven a third time (this plan's `disablebitlocker.ps1`/`disablebitlocker-undo.ps1` after 03-02's `windowsai.ps1`/`windowsai-undo.ps1` and 02-06's driver-tools scripts), with a new wrinkle documented (blocking `Pause` calls, not just `Read-Host` menu loops, must also be stripped to avoid hangs against `IScriptRunner`).
- Manual elevated-GUI verification (D-11 confirmation-dialog-before-execution for BitLocker/Hibernation, and Run/Undo streamed output for all 8 System & Performance actions) is still outstanding and should be performed by a human or the phase verifier before Phase 3 is considered fully proven end-to-end.

---
*Phase: 03-debloat*
*Completed: 2026-09-01*

## Self-Check: PASSED

All 16 created `.ps1` files confirmed present via `git ls-files`; all 3 task commits (`258aa02`, `d167919`, `a6e2d2d`) confirmed in `git log --oneline`.
