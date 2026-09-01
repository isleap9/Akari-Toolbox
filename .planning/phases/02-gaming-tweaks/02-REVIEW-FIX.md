---
phase: 02-gaming-tweaks
fixed_at: 2026-09-01T00:00:00Z
review_path: .planning/phases/02-gaming-tweaks/02-REVIEW.md
iteration: 1
findings_in_scope: 6
fixed: 6
skipped: 0
status: all_fixed
---

# Phase 02: Code Review Fix Report

**Fixed at:** 2026-09-01
**Source review:** .planning/phases/02-gaming-tweaks/02-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 6 (2 critical, 4 warning — `fix_scope: all`, 0 info findings existed)
- Fixed: 6
- Skipped: 0

**Verification environment:** All fixes were applied, built, and tested inside an isolated
git worktree (`gsd-reviewfix/02-769`, fast-forwarded onto `main` after completion) — not the
main checkout. `dotnet build AkariToolbox.slnx -c Debug` succeeded with 0 errors (3
pre-existing MVVMTK0045 warnings, unrelated to this fix pass). `dotnet test
src/AkariToolbox.Tests/AkariToolbox.Tests.csproj -c Debug` ran 199 tests: 198 passed, 1 failed
(`AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` — the pre-existing flaky
COMException test called out in the fix instructions, unrelated to any change here). These
numbers are reproducible by checking out the resulting commits on `main` and re-running the
same two commands from the repo root.

## Fixed Issues

### CR-01: PowerPlanTweakHandler deletes existing power schemes without verifying their export succeeded

**Files modified:** `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs`, `src/AkariToolbox.Tests/GamingWindowsTweaksTests.cs`
**Commits:** `7341e96` (production fix), `2c3618b` (test-infra follow-up)
**Status:** fixed: requires human verification (logic-correctness change — see verification_strategy note below)
**Applied fix:** `PowerPlanTweakHandler.EnableInternal()` now checks the exit code of every
`powercfg -export` call, plus `/duplicatescheme` and `/setactive`, and additionally verifies
the exported `.pow` file actually landed on disk. Any failure logs a specific reason via
`ILogConsoleService` and returns immediately, before the `/delete` loop runs — matching the
review's exact fix guidance (verify export success, verify duplicate/setactive success,
abort before any delete). The existing `PowerPlan_SetState_true_exports_every_existing_
scheme_before_any_delete_call` test started failing after this change because its
`FakeScriptRunner` test double never actually wrote a file for `-export` calls; it now
writes the export destination file (and gained an `ExitCodeResponder` hook for future
export-failure test coverage) so the new `File.Exists` check exercises its real success
path instead of always failing against the in-memory fake.

### CR-02: DefenderTweakHandler marks Defender re-enabled even when SYSTEM-level restoration fails

**File modified:** `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs`
**Commit:** `612c9cf`
**Status:** fixed: requires human verification (logic-correctness change — see verification_strategy note below)
**Applied fix:** Both failure paths described in the finding — Tamper Protection detected ON
during disable, and failed SYSTEM-level service restoration during re-enable — now log the
existing error message and then `throw new InvalidOperationException(...)` instead of a bare
`return`. The `DefenderStateKey` registry flag is only cleared when `restoreOk` is actually
`true`. Because `SetDefenderAsync`'s existing outer `catch (Exception ex)` swallowed *all*
exceptions (converting them to a log line with no rethrow), a bare `throw` inside the two
failure branches would have been silently caught and discarded by that same outer catch —
defeating the fix's purpose. Adapted the fix accordingly: added a `catch
(InvalidOperationException) { throw; }` clause before the generic catch, so these two
specific, deliberately-thrown faults propagate through `SetState` -> `TweakCatalog.
SetStateAsync`'s `Task` (which does not swallow exceptions) -> `OnTweakItemPropertyChanged`'s
existing `task.IsFaulted` real-state-correction path, while other genuinely unexpected
exceptions keep their prior log-and-swallow behavior.

### WR-01: NetAdapterPowerSavingsTweakHandler.GetState() reads only the first adapter while SetState writes every adapter

**File modified:** `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs`
**Commit:** `b82fe78`
**Applied fix:** `GetState()` now enumerates every adapter subkey and requires `.All(...)` of
them to report `PnPCapabilities == 24`, matching `SetState`'s every-adapter write and the
`.All(...)` aggregation semantics already used by `HdcpTweakHandler`/`P0StateTweakHandler`/
`IntelSettingsTweakHandler` in this same phase. No existing test exercised the previous
single-adapter read, so no test updates were needed.

### WR-02: WriteCacheFlushTweakHandler.GetState() uses `.Any(...)` instead of `.All(...)`

**File modified:** `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs`
**Commit:** `d2638dd`
**Applied fix:** Chose the `.All(...)` consistency option from the finding's two offered
fixes (switch to `.All` vs. document the `.Any` choice), matching every other multi-target
Gaming handler in this phase. `GetState()` now requires every matched disk to report
`CacheIsPowerProtected == 1` before reporting the tweak "on", with a zero-disks guard
returning `false` (matching the sibling handlers' empty-enumeration convention). No existing
test exercised `GetState()` for this handler, so no test updates were needed.

### WR-03: No behavior test coverage for HdcpTweakHandler

**File modified:** `src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs`
**Commit:** `7907beb`
**Applied fix:** Added 7 new tests mirroring the existing `P0StateTweakHandler` tests in the
same file: `GetState` exact-match (all adapters agree), mixed-adapter (one off), one-adapter-
absent, and no-adapters-found cases; `SetState(true)`/`SetState(false)` per-adapter writes;
and an `Order`/`Category`/`Key` metadata assertion. All 7 pass.

### WR-04: RunEmbeddedScriptAsync's missing-resource failure bypasses ILogConsoleService

**File modified:** `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs`
**Commit:** `3a4b9cf`
**Applied fix:** Chose the call-site option from the finding's two offered fixes (wrap
`RunEmbeddedScriptAsync` itself vs. wrap the call site in `GamingTweaksViewModel.
RunD06ScriptAsync`) rather than changing `ScriptRunner.RunEmbeddedScriptAsync`'s own
throwing contract — that contract is explicitly documented in `IScriptRunner`'s XML doc
comment and directly covered by an existing test
(`RunEmbeddedScriptAsync_missing_resource_throws_FileNotFoundException` in
`ScriptRunnerEmbeddedTests.cs`), so changing it to swallow-and-return-sentinel would have
broken a deliberately-tested contract elsewhere in the codebase. `RunD06ScriptAsync` (the
sole caller for all 12 D-06 buttons) now awaits the call inside a `try`/`catch
(FileNotFoundException)` and logs the failure via `_log` (the visible in-app log dock)
instead of letting it propagate out of the `[RelayCommand]`-generated async command
unlogged.

## Skipped Issues

None — all 6 in-scope findings were fixed.

---

_Fixed: 2026-09-01_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
