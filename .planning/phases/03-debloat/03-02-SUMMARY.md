---
phase: 03-debloat
plan: 02
subsystem: ui
tags: [winui3, powershell, debloat, embedded-resources, xunit]

# Dependency graph
requires:
  - phase: 03-debloat
    provides: "DebloatCatalog/DebloatViewModel/DebloatPage architecture and the proven telemetry.ps1/telemetry-undo.ps1 embedded-script wiring pattern (03-01-PLAN.md)"
provides:
  - "6 direct-carry embedded Privacy & Telemetry Run+Undo script pairs (activityhistory, locationtracking, ps7telemetry, consumerfeatures, disablebgapps, storesearch), byte-for-byte from the predecessor"
  - "windowsai.ps1/windowsai-undo.ps1 — D-14 branch-extracted from the 'Ultimate' collection's 9 Copilot.ps1, Copilot-only scope, self-elevation/menu scaffolding stripped"
  - "DebloatCatalogTests.PrivacyTelemetry_category_all_actions_have_resolvable_resources — closing regression lock for the full 8-action Privacy & Telemetry category"
affects: [03-03, 03-04, 03-05, 03-06, 03-07]

# Actuals (#2632)
actuals:
  tokens: 3900
  tasks: 3
  commits: 3

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Menu/self-elevation stripping for 'Ultimate' collection source scripts: drop the WindowsPrincipal self-elevation block and console-cosmetic lines, keep the $progresspreference silent line, then copy only the chosen switch-branch body verbatim from its Clear-Host through its own exit — same technique 03-06-PLAN.md will reuse for its own 3 branch-extracted replacements"

key-files:
  created:
    - src/AkariToolbox.App/Resources/DebloatScripts/activityhistory.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/activityhistory-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/locationtracking.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/windowsai.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/windowsai-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/disablebgapps.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/disablebgapps-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/storesearch.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/storesearch-undo.ps1
  modified:
    - src/AkariToolbox.App/AkariToolbox.App.csproj
    - src/AkariToolbox.Tests/DebloatCatalogTests.cs

key-decisions:
  - "windowsai.ps1/windowsai-undo.ps1 sourced from the 'Ultimate' collection's 9 Copilot.ps1 (D-14) rather than the predecessor's own WindowsAI.ps1/WindowsAI-Undo.ps1 pair — narrower, Copilot-only scope, per plan and catalog description already reflecting this"

patterns-established:
  - "Branch-extraction of self-elevating console-menu 'Ultimate' scripts into embedded resources: strip elevation/menu scaffolding, keep silent-progress line, copy one switch branch body verbatim per output file"

requirements-completed: [DEBLOAT-01, DEBLOAT-02]

coverage:
  - id: D1
    description: "6 direct-carry Privacy & Telemetry scripts (Activity History, Location Tracking, PS7 Telemetry, Consumer Features, Background Apps, Store Search) embedded byte-for-byte and resolvable via the assembly manifest"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#PrivacyTelemetry_category_all_actions_have_resolvable_resources"
        status: pass
    human_judgment: false
  - id: D2
    description: "windowsai.ps1/windowsai-undo.ps1 branch-extracted from 9 Copilot.ps1 (D-14), self-elevation and Read-Host menu loop stripped, both branches end in exit"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#PrivacyTelemetry_category_all_actions_have_resolvable_resources"
        status: pass
      - kind: other
        ref: "grep -v '^#' windowsai.ps1/windowsai-undo.ps1 | grep -c Read-Host (both 0)"
        status: pass
    human_judgment: false
  - id: D3
    description: "All 8 Privacy & Telemetry actions (Run+Undo) resolve their embedded resource suffixes in the assembly manifest — category is architecturally complete"
    requirement: "DEBLOAT-02"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#PrivacyTelemetry_category_all_actions_have_resolvable_resources"
        status: pass
    human_judgment: false
  - id: D4
    description: "Clicking each of the 7 newly wired actions' Run and Undo buttons on the live Debloat page streams real script output with no error — requires interactive, elevated GUI verification not available in this headless build worktree"
    verification: []
    human_judgment: true
    rationale: "Manual smoke check (launch elevated app, click Run/Undo for each of the 7 newly wired actions, observe streamed log output) requires an interactive Windows GUI session; this plan executed in a headless parallel worktree with no display/elevation available. dotnet build/test confirm the code compiles and resource resolution is unit-tested, but visual/live-process confirmation needs a human or the phase verifier (matches 03-01-SUMMARY.md's D4 precedent)."

# Metrics
duration: 25min
completed: 2026-09-01
status: complete
---

# Phase 3 Plan 02: Privacy & Telemetry Category Completion Summary

**7 remaining Privacy & Telemetry Run+Undo script pairs embedded (6 byte-for-byte predecessor carries, 1 D-14 branch-extracted Copilot-only replacement), closing the category to 8/8 functional actions with a locking regression test.**

## Performance

- **Duration:** 25 min
- **Started:** 2026-09-01T21:35:00Z
- **Completed:** 2026-09-01T22:00:00Z
- **Tasks:** 3
- **Files modified:** 16

## Accomplishments
- Embedded 6 direct-carry Privacy & Telemetry script pairs (Activity History, Location Tracking, PS7 Telemetry, Consumer Features, Background Apps, Store Search) byte-for-byte from the predecessor's `Scripts/` folder — 12 new `.ps1` files, all wired into `AkariToolbox.App.csproj`'s existing Debloat `<ItemGroup>`
- Branch-extracted `windowsai.ps1`/`windowsai-undo.ps1` (D-14) from the "Ultimate" collection's `9 Copilot.ps1` — stripped the self-elevation block, console-cosmetic lines, and the `Read-Host` menu-selection loop, keeping only branch 1 ("Copilot: Off") verbatim for Run and branch 2 ("Copilot: Default") verbatim for Undo
- Added `PrivacyTelemetry_category_all_actions_have_resolvable_resources` to `DebloatCatalogTests.cs`, closing the regression lock for all 8 Privacy & Telemetry actions' Run+Undo resource resolution
- Full App project builds clean (0 errors); `DebloatCatalogTests` suite is green (7/7, including the new fact); full test suite shows only the pre-existing, unrelated `ConvertersTests.EnumToBoolean_matches_parameter` environment failure (documented in 03-01-SUMMARY.md, untouched by this plan)

## Task Commits

Each task was committed atomically:

1. **Task 1: Embed the 6 remaining direct-carry Privacy & Telemetry scripts** - `2390442` (feat)
2. **Task 2: Extract "Windows AI — Disable" branches from 9 Copilot.ps1 (D-14)** - `2b2f33c` (feat)
3. **Task 3: Lock Privacy & Telemetry resource resolution** - `9071fa0` (test)

## Files Created/Modified
- `src/AkariToolbox.App/Resources/DebloatScripts/activityhistory.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/locationtracking.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/disablebgapps.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/storesearch.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/windowsai.ps1` / `-undo.ps1` - D-14 branch-extracted from `9 Copilot.ps1`, Copilot-only scope
- `src/AkariToolbox.App/AkariToolbox.App.csproj` - 14 new `<EmbeddedResource>` entries appended to the existing Debloat `<ItemGroup>`
- `src/AkariToolbox.Tests/DebloatCatalogTests.cs` - New `PrivacyTelemetry_category_all_actions_have_resolvable_resources` fact

## Decisions Made
- Followed the plan exactly for source selection: 6 pairs carried byte-for-byte from the predecessor's `Scripts/` folder with no transformation; `windowsai.ps1`/`windowsai-undo.ps1` branch-extracted from `9 Copilot.ps1` per D-14 (already reflected in `DebloatCatalog.cs`'s Description column from 03-01-PLAN.md, so no catalog changes were needed here).

## Deviations from Plan

None - plan executed exactly as written. No `.sln` file exists in this repo (a pre-existing, documented repo characteristic per 03-01-SUMMARY.md); built/tested `AkariToolbox.App.csproj`/`AkariToolbox.Tests.csproj` directly instead of the plan's literal `dotnet build AkariToolbox.sln` command, which is the established functional equivalent from the prior plan in this same phase.

## Issues Encountered
- Same pre-existing, unrelated `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` failure documented in 03-01-SUMMARY.md (headless build environment doesn't throw the expected `COMException` from `DependencyProperty.UnsetValue`). Untouched by this plan, out of scope per the deviation rules' scope boundary.
- The plan's overall `<verification>` item 3 (manual elevated-GUI smoke check: Run/Undo each of the 7 newly wired actions, observe streamed output) could not be performed — this plan executed in a headless parallel git worktree with no interactive display or elevation available. Flagged as `human_judgment: true` coverage item D4 above for the phase verifier / a human to confirm, matching 03-01-SUMMARY.md's D4 precedent.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Privacy & Telemetry category is now 100% architecturally complete (8/8 actions with resolvable Run+Undo embedded resources); `DebloatCatalog`/`DebloatViewModel`/`DebloatPage` require no further changes — Wave 2-6 plans (03-03 through 03-06) only need to add `<EmbeddedResource>` entries plus `.ps1` files for their own categories, following this plan's and 03-01's established pattern.
- The 03-06-PLAN.md branch-extraction technique for self-elevating "Ultimate" collection scripts is now proven twice (this plan's `windowsai.ps1`/`windowsai-undo.ps1` is the second instance after 02-06's driver-tools scripts) — safe to reuse without re-deriving the approach.
- Manual elevated-GUI verification (Run/Undo streamed output for all 7 newly wired actions) is still outstanding and should be performed by a human or the phase verifier before Phase 3 is considered fully proven end-to-end.

---
*Phase: 03-debloat*
*Completed: 2026-09-01*

## Self-Check: PASSED

All 14 created `.ps1` files confirmed present via `git ls-files`; all 3 task commits (`2390442`, `2b2f33c`, `9071fa0`) confirmed in `git log --oneline --all`.
