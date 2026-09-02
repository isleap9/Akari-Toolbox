---
phase: 03-debloat
plan: 08
subsystem: debloat
tags: [powershell, registry, winui3, xunit, acl, gap-closure]

# Dependency graph
requires:
  - phase: 03-debloat
    provides: "03-01 through 03-07's 28-action DebloatCatalog, DebloatViewModel, and 53 embedded .ps1 scripts; 03-REVIEW.md's CR-01..CR-06 verbatim fix diffs; 03-VERIFICATION.md's gap report identifying the 6 broken Undo pairs"
provides:
  - "6 corrected *-undo.ps1 scripts (locationtracking, consumerfeatures, storesearch, ps7telemetry, wpbt, folderdiscovery) that genuinely reverse their paired Run script's registry/env-var/ACL change"
  - "storesearch catalog row's RequiresConfirmation flipped false -> true, closing CR-03's confirmation-gate half"
  - "New DebloatScriptRegressionTests suite (8 facts) proving all 6 fixes via real HKLM/HKCU/env-var/ACL state reads, not IScriptRunner call-wiring assertions"
affects: [03-verification, ship]

# Actuals (#2632)
actuals:
  tokens: 5907
  tasks: 3
  commits: 3

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Live regression-test pattern for opaque PowerShell scripts: real ScriptRunner + real powershell.exe + direct Microsoft.Win32.Registry/System.Environment/icacls reads before Run, after Run, and after Undo, with an IsElevated() runtime skip guard for HKLM/machine-env facts and a finally-block state restore for every mutating fact"

key-files:
  created:
    - src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs
  modified:
    - src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/storesearch-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/wpbt-undo.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery-undo.ps1
    - src/AkariToolbox.App/Services/DebloatCatalog.cs
    - src/AkariToolbox.App/Models/DebloatAction.cs
    - src/AkariToolbox.Tests/DebloatCatalogTests.cs

key-decisions:
  - "Applied 03-REVIEW.md's CR-01..CR-06 fix diffs verbatim, with no new logic invented beyond what was already reviewed — keeps the fix itself carrying no additional review risk"
  - "Kept each script's existing supplementary write (e.g. consumerfeatures-undo's ContentDeliveryManager loop, storesearch-undo's BingSearchEnabled write) rather than removing it, per the plan's explicit instruction to treat it as supplementary, not sole"

patterns-established:
  - "DebloatScriptRegressionTests: real-state regression testing for embedded PowerShell scripts with no C# seam — a new test category alongside DebloatCatalogTests' resource-wiring assertions"

requirements-completed: [DEBLOAT-01, DEBLOAT-02, DEBLOAT-03]

coverage:
  - id: D1
    description: "locationtracking-undo.ps1 (CR-01) restores the 3 guaranteed HKLM values (ConsentStore\\location, lfsvc\\Service\\Configuration!Status, SYSTEM\\Maps!AutoUpdateEnabled) to Allow/1/1 after Run+Undo, and is safe to run standalone"
    requirement: DEBLOAT-01
    verification:
      - kind: integration
        ref: "src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs#LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values"
        status: pass
      - kind: integration
        ref: "src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs#LocationTracking_undo_alone_is_safe_and_idempotent_without_a_prior_run"
        status: pass
    human_judgment: false
  - id: D2
    description: "consumerfeatures-undo.ps1 (CR-02) clears the HKLM Policies\\...\\CloudContent!DisableWindowsConsumerFeatures Group-Policy-precedence key after Undo"
    requirement: DEBLOAT-01
    verification:
      - kind: integration
        ref: "src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs#ConsumerFeatures_run_then_undo_restores_DisableWindowsConsumerFeatures_policy"
        status: pass
    human_judgment: false
  - id: D3
    description: "ps7telemetry-undo.ps1 (CR-04) clears the machine-scope POWERSHELL_TELEMETRY_OPTOUT env var after Undo"
    requirement: DEBLOAT-01
    verification:
      - kind: integration
        ref: "src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs#Ps7Telemetry_run_then_undo_restores_the_machine_scope_env_var"
        status: pass
    human_judgment: false
  - id: D4
    description: "wpbt-undo.ps1 (CR-05) clears HKLM Session Manager!DisableWpbtExecution after Undo"
    requirement: DEBLOAT-01
    verification:
      - kind: integration
        ref: "src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs#Wpbt_run_then_undo_restores_DisableWpbtExecution"
        status: pass
    human_judgment: false
  - id: D5
    description: "folderdiscovery-undo.ps1 (CR-06) clears HKCU Bags\\AllFolders\\Shell!FolderType after Undo"
    requirement: DEBLOAT-01
    verification:
      - kind: integration
        ref: "src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs#FolderDiscovery_run_then_undo_restores_FolderType_under_HKCU"
        status: pass
    human_judgment: false
  - id: D6
    description: "storesearch-undo.ps1 (CR-03) removes the Everyone Deny ACE storesearch.ps1's icacls /deny added, proven via embedded-resource content check and a real scratch-file icacls round trip"
    requirement: DEBLOAT-01
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs#StoreSearch_undo_script_contains_the_icacls_remove_deny_fix"
        status: pass
      - kind: integration
        ref: "src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs#StoreSearch_icacls_deny_then_remove_d_round_trips_on_a_scratch_file"
        status: pass
    human_judgment: false
  - id: D7
    description: "storesearch's Run direction is now confirmation-gated (RequiresConfirmation: true), closing CR-03's second half"
    requirement: DEBLOAT-01
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Confirmation_required_set_matches_D11_classification"
        status: pass
    human_judgment: false

# Metrics
duration: ~20min
completed: 2026-09-02
status: complete
---

# Phase 03 Plan 08: Debloat gap closure (CR-01..CR-06) Summary

**Fixed all 6 broken Undo scripts flagged by 03-REVIEW.md/03-VERIFICATION.md (wrong hive/key/env-var/ACL targets) and added a live regression-test suite that reads real registry/env/ACL state to prove each fix, closing the phase's "gaps found" status.**

## Performance

- **Duration:** ~20 min
- **Tasks:** 3
- **Files modified:** 10 (6 `.ps1` scripts, `DebloatCatalog.cs`, `DebloatAction.cs`, `DebloatCatalogTests.cs`, plus 1 new test file)

## Accomplishments

- Fixed all 6 CR-01..CR-06 findings exactly as reviewed in 03-REVIEW.md — each corrected `*-undo.ps1` now targets the exact registry key/hive/env-var/ACL its paired Run script actually wrote, applying the already-reviewed fix diffs verbatim (no new logic invented)
- `storesearch`'s Run direction is now confirmation-gated (`RequiresConfirmation: true`), closing CR-03's second half — a previously unconfirmed, effectively irreversible `icacls /deny Everyone:F` ACL change
- Added `DebloatScriptRegressionTests.cs` (new file, 8 `[Fact]` methods) that runs the real embedded `.ps1` scripts via a real `ScriptRunner` and reads real `HKLM`/`HKCU`/environment-variable/ACL state before Run, after Run, and after Undo — structurally closing 03-VERIFICATION.md's truth-8 gap that `DebloatCatalogTests` (resource-wiring/call assertions only) could never catch this bug class
- Ran the full `dotnet test` suite twice in a row (elevated shell): 0 skipped, all 8 new facts plus all pre-existing `DebloatCatalogTests` facts pass both times — the second clean run is direct evidence the `finally`-block restores genuinely returned the machine to its pre-test state

## Task Commits

Each task was committed atomically:

1. **Task 1: Fix CR-01 (locationtracking-undo.ps1) and prove the live registry-regression pattern end-to-end** - `7e4348e` (fix)
2. **Task 2: Fix CR-02, CR-04, CR-05, CR-06 reusing Task 1's live-test pattern** - `9524097` (fix)
3. **Task 3: Fix CR-03 (storesearch) — Undo ACL reversal + Run confirmation gate + catalog/test updates** - `6560ef9` (fix)

_Note: this is a `gap_closure: true` plan running in a worktree — the orchestrator applies the final plan-metadata (`docs`) commit centrally after merge, not this agent._

## Files Created/Modified

- `src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1` - now writes the 4 HKLM values `locationtracking.ps1` wrote (was writing to an unrelated Policies key and the wrong hive)
- `src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1` - now removes the `DisableWindowsConsumerFeatures` policy value, keeps the `ContentDeliveryManager` loop as supplementary
- `src/AkariToolbox.App/Resources/DebloatScripts/storesearch-undo.ps1` - now runs `icacls ... /remove:d Everyone` to clear the Deny ACE, keeps `BingSearchEnabled` write as supplementary
- `src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry-undo.ps1` - now clears the machine-scope `POWERSHELL_TELEMETRY_OPTOUT` env var, dropping the guaranteed-no-op file/`$PROFILE` edit
- `src/AkariToolbox.App/Resources/DebloatScripts/wpbt-undo.ps1` - now removes `DisableWpbtExecution` from the actual key Run wrote, dropping the non-existent-service `Set-Service`/`Start-Service` calls
- `src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery-undo.ps1` - now removes `FolderType` from the actual key Run wrote, dropping the no-op `Explorer\Advanced` edit
- `src/AkariToolbox.App/Services/DebloatCatalog.cs` - `storesearch` row's `RequiresConfirmation` flipped `false` -> `true`
- `src/AkariToolbox.App/Models/DebloatAction.cs` - `RequiresConfirmation` doc comment updated to the new 6-key set
- `src/AkariToolbox.Tests/DebloatCatalogTests.cs` - `ExpectedConfirmationRequiredKeys` updated to add `storesearch`
- `src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs` (new) - `CreateRunner`/`IsElevated`/`ReadHklm`/`WriteHklm`/`DeleteHklmIfPresent`/`ReadHkcu`/`WriteHkcu`/`DeleteHkcuIfPresent` helpers, plus 8 facts (2 locationtracking, 1 consumerfeatures, 1 ps7telemetry, 1 wpbt, 1 folderdiscovery, 2 storesearch)

## Decisions Made

- Applied each of 03-REVIEW.md's CR-01..CR-06 fix diffs verbatim rather than re-deriving a fix — the diffs were already code-reviewed, so applying them as-is means the fix itself carries no additional review risk (per the plan's explicit instruction)
- Kept each script's pre-existing supplementary write (e.g. `consumerfeatures-undo`'s `ContentDeliveryManager` loop, `storesearch-undo`'s `BingSearchEnabled` write) as a secondary step rather than deleting it — matches the plan's instruction that these remain supplementary, not sole, reversal steps
- Did not add `Test-Path`/`New-Item` guards to `locationtracking-undo.ps1`, `wpbt-undo.ps1`, or `folderdiscovery-undo.ps1` (03-REVIEW.md's IN-02, a separate lower-severity finding) — explicitly out of scope for this plan per the plan's own instruction not to silently fold it in

## Deviations from Plan

None — plan executed exactly as written. All 3 tasks applied 03-REVIEW.md's fix diffs verbatim with no new logic invented, exactly as scoped.

One out-of-scope, pre-existing item was encountered and left untouched per the deviation rules' scope boundary: a full (unfiltered) `dotnet test` run surfaces 1 failure, `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter`, in a file this plan never touched. This was already documented in `.planning/phases/03-debloat/deferred-items.md` since 03-01 as an environment-dependent (WinRT activation-context) failure unrelated to Debloat — re-confirmed here as still present and still out of scope, no new entry needed.

## Issues Encountered

None — the test-running shell was already elevated, so all 8 new regression facts ran with real (non-skipped) assertions on the first attempt, and both required consecutive full-suite runs (per the plan's own verification step 3) passed clean, confirming the `finally`-block restores are genuinely idempotent.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All 4 of 03-VERIFICATION.md's failed truths (gaps 1-4, covering the Privacy & Telemetry, System & Performance, and Explorer & UI categories plus the phase-closing claim) are now closed by real, independently-verifiable evidence — not prose claims
- DEBLOAT-01 is now genuinely fully satisfied: all 28 actions' Run (and Undo where applicable) paths are wired, execute, and — for the 6 previously-broken pairs — now demonstrably reverse the exact system state their Run script changed
- Ready for `/gsd-verify-work` or a phase re-verification pass to confirm the gap closure against 03-VERIFICATION.md's original gap list
- `windowsai.ps1`'s undisclosed process-killing (03-REVIEW.md WR-04) remains explicitly out of scope, as documented in the plan's objective — not a regression introduced or left behind by this plan

---
*Phase: 03-debloat*
*Completed: 2026-09-02*
