---
phase: 03-debloat
fixed_at: 2026-09-02T00:00:00Z
review_path: .planning/phases/03-debloat/03-REVIEW.md
iteration: 1
findings_in_scope: 5
fixed: 5
skipped: 0
status: all_fixed
---

# Phase 03: Code Review Fix Report (Plan 03-08 gap closure)

**Fixed at:** 2026-09-02T00:00:00Z
**Source review:** .planning/phases/03-debloat/03-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 5 (fix_scope: critical_warning — CR-* and WR-*; IN-01 excluded)
- Fixed: 5
- Skipped: 0

**Verification environment note:** This session's shell was running elevated (confirmed via
`WindowsPrincipal.IsInRole(Administrator)` before starting). All fixes were built with
`dotnet build src/AkariToolbox.App/AkariToolbox.App.csproj -c Debug -p:Platform=x64` and
verified with `dotnet test src/AkariToolbox.Tests/AkariToolbox.Tests.csproj -c Debug
-p:Platform=x64` inside an isolated git worktree
(`.claude/worktrees/rf-03-3411-1788332802`, now removed). Because the shell was elevated,
the elevation-gated facts in `DebloatScriptRegressionTests` executed for real (not
skipped) during verification, which is what exposed and confirmed the CR-02/CR-03 registry
mutation bugs in practice, not just in theory.

## Fixed Issues

### CR-01: Elevation-skip guard produces a false "Passed" result under non-elevated `dotnet test`

**Files modified:** `Directory.Packages.props`, `src/AkariToolbox.Tests/AkariToolbox.Tests.csproj`, `src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs`
**Commit:** `6d7104d`
**Applied fix:** Added the `Xunit.SkippableFact` package (version 1.5.85, compatible with
the project's pinned `xunit 2.9.3` — requires `xunit.extensibility.execution >= 2.4.0`) to
`Directory.Packages.props` and referenced it in the test project. Converted the 5 facts
that previously used a bare `if (!IsElevated()) { Console.WriteLine(...); return; }` guard
(`LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values`,
`LocationTracking_undo_alone_is_safe_and_idempotent_without_a_prior_run`,
`ConsumerFeatures_run_then_undo_restores_DisableWindowsConsumerFeatures_policy`,
`Ps7Telemetry_run_then_undo_restores_the_machine_scope_env_var`,
`Wpbt_run_then_undo_restores_DisableWpbtExecution`) to `[SkippableFact]` with
`Skip.IfNot(IsElevated(), "requires elevation")` as the first statement. A non-elevated
`dotnet test` run now genuinely reports these 5 facts as **Skipped**, not **Passed**.
Verified during this session (elevated shell): all 8 facts (5 SkippableFact + 3 unaffected
Fact) ran for real and passed — `Passed: 8, Skipped: 0` in an isolated filter run, and
`Passed: 250, Failed: 1 (pre-existing, unrelated), Skipped: 0` in the full suite.

### CR-02: LocationTracking tests' `finally` block leaves `SensorPermissionState` permanently mutated

**Files modified:** `src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs`
**Commit:** `cf5bec5`
**Applied fix:** Added capture/restore of the 4th HKLM value
(`Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}!SensorPermissionState`) to both
`LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values` and
`LocationTracking_undo_alone_is_safe_and_idempotent_without_a_prior_run`, matching the
existing pattern used for the other 3 values (`ReadHklm` before, assert during, guarded
`WriteHklm`/`DeleteHklmIfPresent` in `finally`). Verified by running both facts on this
elevated machine — both pass, and the sensor override key is correctly restored to its
pre-test state afterward (confirmed by re-reading the key post-run).

### CR-03: ConsumerFeatures test's `finally` block only restores 1 of 9 registry values

**Files modified:** `src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs`
**Commit:** `c3f8036`
**Applied fix:** Extended
`ConsumerFeatures_run_then_undo_restores_DisableWindowsConsumerFeatures_policy` to
snapshot (before) and restore (in `finally`) all 8 HKCU
`Explorer\Advanced`/`ContentDeliveryManager` values that `consumerfeatures-undo.ps1`
unconditionally writes, in addition to the 1 HKLM policy value already handled — 9 values
total. Also added assertions confirming all 8 supplementary values read back `1` after
Undo, since the test now exercises (and must prove correct restoration of) the full
Run+Undo cycle's real effect. Verified by running the fact on this elevated machine — it
passes and all 9 values are correctly restored to their pre-test state afterward.

### WR-01: `consumerfeatures-undo.ps1` writes 8 registry values its paired Run script never touched

**Files modified:** `src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1`
**Commit:** `da6f453`
**Applied fix:** Checked `.planning/phases/03-debloat/03-08-PLAN.md` and
`03-08-SUMMARY.md` for prior rationale before deciding between "trim" and "document as
intentional" (per the finding's own guidance). Found an explicit, documented decision from
the 03-08 gap-closure plan: *"Keep the existing 8-value `HKCU:\...\ContentDeliveryManager`
loop unchanged below it as a supplementary (not sole) step"* (03-08-PLAN.md:168,
corroborated by 03-08-SUMMARY.md:45/152/165). Given this documented intent, did not trim
the script (which would also contradict the CR-03 fix just applied, which now asserts the
8 values are set after Undo). Instead applied the finding's alternative remedy: added a
4-line comment above the `$regKeys` block explicitly stating the block is an intentional
supplementary restoration of out-of-box content-delivery/suggested-apps defaults, not a
1:1 reversal of `consumerfeatures.ps1`'s single policy-key write — so a future reader
doesn't mistake it for scope creep or a bug. No behavior change; comment-only.

### WR-02: `locationtracking-undo.ps1` has no per-key existence guard or `-ErrorAction`

**Files modified:** `src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1`
**Commit:** `708dc4b`
**Applied fix:** Wrapped each of the 4 `Set-ItemProperty` calls in a `Test-Path` guard and
added `-ErrorAction SilentlyContinue`, matching the pattern already used in the sibling
`wpbt-undo.ps1`/`folderdiscovery-undo.ps1` scripts fixed in the same 03-08 commit. A
missing key (e.g., `Sensor\Overrides\{GUID}` on a machine/VM without the sensor stack
initialized) is now a documented no-op instead of a silent partial write with a non-
terminating error the tool never inspects. Verified: PowerShell AST parser
(`[System.Management.Automation.Language.Parser]::ParseFile`) reports no syntax errors,
and both `LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values` /
`LocationTracking_undo_alone_is_safe_and_idempotent_without_a_prior_run` pass on this
elevated machine (all 4 keys exist here, so this exercises the guarded-write path, not the
missing-key no-op path — the no-op path itself is only reachable on a machine lacking one
of these keys, which this dev machine is not).

## Skipped Issues

None — all 5 in-scope findings (CR-01, CR-02, CR-03, WR-01, WR-02) were fixed. IN-01 was
excluded per `fix_scope: critical_warning` (Info-tier findings out of scope for this run).

---

_Fixed: 2026-09-02T00:00:00Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
