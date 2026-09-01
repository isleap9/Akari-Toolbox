---
phase: 03-debloat
plan: 05
subsystem: debloat
tags: [winui3, powershell, embedded-resources, catalog, xunit]

# Dependency graph
requires:
  - phase: 03-debloat (03-04)
    provides: Explorer & UI + Tools categories fully embedded; DebloatCatalog and DebloatCatalogTests conventions established
provides:
  - "3 of Cleanup's 6 actions (Disk Cleanup, Temporary Files, OneDrive Remove) fully functional with embedded PowerShell scripts"
  - "removeonedrive/removeonedrive-undo.ps1 pair backing the D-11 confirmation-gated OneDrive Remove action"
affects: [03-06 (remaining 3 Cleanup actions: bloatware, edgesettings, edgewebview)]

# Actuals (#2632)
actuals:
  tokens: 1552
  tasks: 2
  commits: 2

# Tech tracking
tech-stack:
  added: []
  patterns: [embedded-ps1-resource-per-action, DebloatCatalogTests-per-category-resource-lock]

key-files:
  created:
    - src/AkariToolbox.App/Resources/DebloatScripts/diskcleanup.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/tempfiles.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/removeonedrive.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/removeonedrive-undo.ps1
  modified:
    - src/AkariToolbox.App/AkariToolbox.App.csproj
    - src/AkariToolbox.Tests/DebloatCatalogTests.cs

key-decisions:
  - "Build/test commands use AkariToolbox.slnx at repo root, not src/AkariToolbox.sln as the plan's <verify> literally states — the plan's verify command predates the .slnx migration; corrected inline, no scope change to the plan's intent"

patterns-established: []

requirements-completed: [DEBLOAT-01, DEBLOAT-02]

coverage:
  - id: D1
    description: "Disk Cleanup and Temporary Files embedded as Run-only actions (no Undo), OneDrive Remove embedded as a Run+Undo pair behind its D-11 confirmation gate"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Cleanup_direct_carry_actions_have_resolvable_resources"
        status: pass
      - kind: unit
        ref: "dotnet build AkariToolbox.slnx -c Debug"
        status: pass
    human_judgment: false
  - id: D2
    description: "OneDrive Remove's confirmation gate and actual Run/Undo execution behavior in a live app instance"
    requirement: "DEBLOAT-02"
    verification: []
    human_judgment: true
    rationale: "Confirmation-dialog UX and live OneDriveSetup.exe uninstall/reinstall behavior require running the elevated app on a real Windows machine with OneDrive installed — not exercisable via unit tests, which only assert resource resolution."

duration: 12min
completed: 2026-09-01
status: complete
---

# Phase 3 Plan 5: Cleanup Direct Carries (Disk Cleanup, Temp Files, OneDrive Remove) Summary

**Embedded 3 byte-for-byte predecessor PowerShell scripts (Disk Cleanup, Temporary Files, OneDrive Remove/Undo) as the Cleanup category's "easy half," extending DebloatCatalogTests with a dedicated resource-resolution lock.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-09-01T23:29:00Z
- **Completed:** 2026-09-01T23:41:27Z
- **Tasks:** 2
- **Files modified:** 6

## Accomplishments
- `diskcleanup.ps1` and `tempfiles.ps1` embedded byte-for-byte, Run-only (matches `DebloatCatalog`'s `UndoResourceSuffix: null` for both keys)
- `removeonedrive.ps1`/`removeonedrive-undo.ps1` embedded as a Run+Undo pair, giving the D-11-confirmation-gated "OneDrive — Remove" action (already wired in 03-01-PLAN.md's catalog) a real backing script for the first time
- New `Cleanup_direct_carry_actions_have_resolvable_resources` xUnit fact locks resource resolution for all 3 direct-carry Cleanup actions without touching the 3 remaining actions deferred to 03-06

## Task Commits

Each task was committed atomically:

1. **Task 1: Embed Disk Cleanup, Temporary Files, and OneDrive Remove scripts** - `2888baa` (feat)
2. **Task 2: Lock the 3 direct-carry Cleanup actions' resource resolution** - `6336c47` (test)

**Plan metadata:** committed by orchestrator after worktree merge (this plan runs in worktree isolation mode — STATE.md/ROADMAP.md updates deferred)

## Files Created/Modified
- `src/AkariToolbox.App/Resources/DebloatScripts/diskcleanup.ps1` - Runs `cleanmgr.exe /d C: /VERYLOWDISK` then DISM component cleanup, byte-for-byte carry from `AkariOS-Companion/Scripts/DiskCleanup.ps1`
- `src/AkariToolbox.App/Resources/DebloatScripts/tempfiles.ps1` - Clears `%TEMP%`, `%SystemRoot%\Temp`, and Prefetch, byte-for-byte carry from `TempFiles.ps1`
- `src/AkariToolbox.App/Resources/DebloatScripts/removeonedrive.ps1` - Runs `OneDriveSetup.exe /uninstall`, cleans leftover folders/service, byte-for-byte carry from `RemoveOneDrive.ps1`
- `src/AkariToolbox.App/Resources/DebloatScripts/removeonedrive-undo.ps1` - Reinstalls via `winget install Microsoft.OneDrive`, re-enables `OneSyncSvc`, byte-for-byte carry from `RemoveOneDrive-Undo.ps1`
- `src/AkariToolbox.App/AkariToolbox.App.csproj` - 4 new `<EmbeddedResource Include="Resources\DebloatScripts\...\.ps1" />` entries appended to the existing Debloat `<ItemGroup>`
- `src/AkariToolbox.Tests/DebloatCatalogTests.cs` - New `Cleanup_direct_carry_actions_have_resolvable_resources` fact

## Decisions Made
- The plan's `<verify>` commands literally say `cd src && dotnet build AkariToolbox.sln`, but the repo uses `AkariToolbox.slnx` (new-style solution file) at repo root, not `src/AkariToolbox.sln`. Ran `dotnet build AkariToolbox.slnx -c Debug` and `dotnet test AkariToolbox.Tests --filter ...` from repo root instead — same intent, corrected path, no scope change. This is a pre-existing discrepancy in the plan text (the `.slnx` migration predates this plan), not a deviation in the deviation-rules sense.

## Deviations from Plan

None - plan executed exactly as written (aside from the verify-command path correction noted above, which is a mechanical build-tooling detail, not a functional deviation).

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- 3 of Cleanup's 6 actions are now fully functional (25 of 28 debloat actions phase-wide have real embedded scripts)
- Only the 3 D-03/D-06/D-08 replacement actions (Unwanted Apps — Remove, Microsoft Edge — Debloat, Microsoft Edge — Remove) remain, explicitly deferred to 03-06-PLAN.md per this plan's objective — no blockers for that plan
- `DebloatCatalogTests` suite: 11 facts total, all green

---
*Phase: 03-debloat*
*Completed: 2026-09-01*
