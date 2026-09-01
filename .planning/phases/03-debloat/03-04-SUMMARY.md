---
phase: 03-debloat
plan: 04
subsystem: ui
tags: [winui3, powershell, debloat, embedded-resources, xunit]

# Dependency graph
requires:
  - phase: 03-debloat
    provides: "DebloatCatalog/DebloatViewModel/DebloatPage architecture and the proven telemetry.ps1/telemetry-undo.ps1 embedded-script wiring pattern (03-01-PLAN.md, extended through 03-02/03-03)"
provides:
  - "5 direct-carry embedded Explorer & UI Run+Undo script pairs (endtask, folderdiscovery, removehomeandgallery, rightclickmenu, widgets), byte-for-byte from the predecessor"
  - "oosu.ps1 — the Tools category's single Run-only action (O&O ShutUp10++ downloader/launcher), byte-for-byte from the predecessor, no Undo counterpart"
  - "DebloatCatalogTests.ExplorerUi_category_all_actions_have_resolvable_resources / Tools_category_action_has_resolvable_run_resource_and_no_undo — closing regression locks for both categories"
affects: [03-05, 03-06, 03-07]

# Actuals (#2632)
actuals:
  tokens: 3800
  tasks: 2
  commits: 2

# Tech tracking
tech-stack:
  added: []
  patterns: []

key-files:
  created:
    - src/AkariToolbox.App/Resources/DebloatScripts/endtask.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/endtask-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/removehomeandgallery.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/removehomeandgallery-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/rightclickmenu.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/rightclickmenu-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/widgets.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/widgets-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/oosu.ps1
  modified:
    - src/AkariToolbox.App/AkariToolbox.App.csproj
    - src/AkariToolbox.Tests/DebloatCatalogTests.cs

key-decisions: []

patterns-established: []

requirements-completed: [DEBLOAT-01, DEBLOAT-02]

coverage:
  - id: D1
    description: "5 direct-carry Explorer & UI scripts (End Task, Folder Discovery, Explorer Home, Right-Click Classic, Widgets) embedded byte-for-byte and resolvable via the assembly manifest"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#ExplorerUi_category_all_actions_have_resolvable_resources"
        status: pass
    human_judgment: false
  - id: D2
    description: "oosu.ps1 (Tools category, O&O ShutUp10++ downloader) embedded byte-for-byte; run resource resolves, no Undo resource exists (matches DebloatCatalog's UndoResourceSuffix: null for this row)"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Tools_category_action_has_resolvable_run_resource_and_no_undo"
        status: pass
    human_judgment: false
  - id: D3
    description: "All 6 newly wired actions (5 Explorer & UI + 1 Tools) resolve their embedded resource suffixes in the assembly manifest — both categories are now architecturally complete"
    requirement: "DEBLOAT-02"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#ExplorerUi_category_all_actions_have_resolvable_resources"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Tools_category_action_has_resolvable_run_resource_and_no_undo"
        status: pass
    human_judgment: false
  - id: D4
    description: "Manual smoke check: run each of the 6 newly wired actions' Run (and Undo where present) buttons on the live Debloat page and confirm real output streams with no error"
    verification: []
    human_judgment: true
    rationale: "Requires an interactive, elevated Windows GUI session; this plan executed in a headless parallel worktree with no display/elevation available. dotnet build/test confirm the code compiles and resource resolution is unit-tested, but live-process/visual confirmation needs a human or the phase verifier (matches 03-01/03-02/03-03-SUMMARY.md's D4 precedent)."

# Metrics
duration: 15min
completed: 2026-09-01
status: complete
---

# Phase 3 Plan 04: Explorer & UI and Tools Category Completion Summary

**11 new embedded Run/Undo script pairs (5 Explorer & UI actions plus the single Tools action, O&O ShutUp10++) close out 4 of the phase's 5 Debloat categories, byte-for-byte from the predecessor with no transformation needed.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-09-01T23:20:00Z
- **Completed:** 2026-09-01T23:33:38Z
- **Tasks:** 2
- **Files modified:** 13

## Accomplishments
- Embedded 5 direct-carry Explorer & UI script pairs (End Task, Folder Discovery, Explorer Home, Right-Click Classic, Widgets) byte-for-byte from the predecessor's `Scripts/` folder — 10 new `.ps1` files, all wired into `AkariToolbox.App.csproj`'s existing Debloat `<ItemGroup>`
- Embedded `oosu.ps1` (Tools category, O&O ShutUp10++ downloader/launcher) byte-for-byte — the category's only action, run-only with no Undo counterpart, matching `DebloatCatalog.cs`'s already-declared `UndoResourceSuffix: null` for the "oosu" row
- Added `ExplorerUi_category_all_actions_have_resolvable_resources` and `Tools_category_action_has_resolvable_run_resource_and_no_undo` to `DebloatCatalogTests.cs`, closing the regression lock for both categories' Run/Undo resource resolution
- Full App project builds clean (0 errors, 4 pre-existing unrelated warnings); `DebloatCatalogTests` suite is green (10/10, including both new facts)
- 4 of 5 Debloat categories now fully complete (22 of 28 actions); only Cleanup remains (03-05/03-06)

## Task Commits

Each task was committed atomically:

1. **Task 1: Embed the 5 Explorer & UI scripts plus the 1 Tools script** - `2ac55d1` (feat)
2. **Task 2: Lock Explorer & UI and Tools resource resolution** - `a1f70b8` (test)

## Files Created/Modified
- `src/AkariToolbox.App/Resources/DebloatScripts/endtask.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/removehomeandgallery.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/rightclickmenu.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/widgets.ps1` / `-undo.ps1` - Direct carry, byte-for-byte
- `src/AkariToolbox.App/Resources/DebloatScripts/oosu.ps1` - Direct carry, byte-for-byte, run-only (Tools category)
- `src/AkariToolbox.App/AkariToolbox.App.csproj` - 11 new `<EmbeddedResource>` entries appended to the existing Debloat `<ItemGroup>`
- `src/AkariToolbox.Tests/DebloatCatalogTests.cs` - Two new facts: `ExplorerUi_category_all_actions_have_resolvable_resources`, `Tools_category_action_has_resolvable_run_resource_and_no_undo`

## Decisions Made
- Followed the plan exactly: all 11 files carried byte-for-byte from the predecessor's `Scripts/` folder with no transformation — no branch-extraction complexity in this plan (unlike 03-02's windowsai.ps1 or 03-03's disablebitlocker.ps1), since none of these 6 actions are D-11 risk-classified or sourced from the "Ultimate" collection. `DebloatCatalog.cs` required no changes — its Explorer & UI and Tools rows (including "oosu"'s `UndoResourceSuffix: null`) were already correctly declared in 03-01-PLAN.md.
- No `.sln` file exists in this repo (pre-existing, documented repo characteristic per 03-01/03-02/03-03-SUMMARY.md); built via the repo-root `AkariToolbox.slnx` file and tested via `AkariToolbox.Tests.csproj` directly instead of the plan's literal `cd src && dotnet build AkariToolbox.sln` command — the established functional equivalent from prior plans in this phase.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- The plan's overall `<verification>` item 3 (manual smoke check: run each of the 6 newly wired actions' Run/Undo buttons on the live Debloat page and confirm real streamed output) could not be performed — this plan executed in a headless parallel git worktree with no interactive display or elevation available. Flagged as `human_judgment: true` coverage item D4 above for the phase verifier / a human to confirm, matching 03-01/03-02/03-03-SUMMARY.md's D4 precedent.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Explorer & UI (5/5) and Tools (1/1) categories are now 100% architecturally complete; `DebloatCatalog`/`DebloatViewModel`/`DebloatPage` require no further changes for these categories.
- 4 of 5 Debloat categories are now fully complete (22 of 28 actions) — only Cleanup remains, split across 03-05/03-06 due to its extraction complexity (bloatware-remove/bloatware-installall, removeonedrive, edgesettings, edgewebview).
- Manual elevated-GUI verification (live Run/Undo streamed output for all 6 newly wired Explorer & UI + Tools actions) is still outstanding and should be performed by a human or the phase verifier before Phase 3 is considered fully proven end-to-end.

---
*Phase: 03-debloat*
*Completed: 2026-09-01*

## Self-Check: PASSED

All 11 created `.ps1` files confirmed present via `git ls-files`; both task commits (`2ac55d1`, `a1f70b8`) confirmed in `git log --oneline`.
