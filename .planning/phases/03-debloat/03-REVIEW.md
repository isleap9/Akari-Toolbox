---
phase: 03-debloat
reviewed: 2026-09-02T00:00:00Z
depth: standard
files_reviewed: 10
files_reviewed_list:
  - src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs
  - src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/storesearch-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/wpbt-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery-undo.ps1
  - src/AkariToolbox.App/Services/DebloatCatalog.cs
  - src/AkariToolbox.App/Models/DebloatAction.cs
  - src/AkariToolbox.Tests/DebloatCatalogTests.cs
findings:
  critical: 3
  warning: 2
  info: 1
  total: 6
status: issues_found
---

# Phase 03: Code Review Report (Plan 03-08 gap closure)

**Reviewed:** 2026-09-02T00:00:00Z
**Depth:** standard
**Files Reviewed:** 10
**Status:** issues_found

## Summary

Reviewed the 03-08 gap-closure change set: 6 fixed `*-undo.ps1` scripts, the new
`DebloatScriptRegressionTests.cs` live-system suite, and the `storesearch`
confirmation-gate wiring in `DebloatCatalog.cs`/`DebloatAction.cs`.

The registry/env-var/ACL **target correctness** of all 6 undo scripts checks out — I
diffed each undo script against its paired Run script's actual writes
(`locationtracking.ps1`, `consumerfeatures.ps1`, `storesearch.ps1`, `ps7telemetry.ps1`,
`wpbt.ps1`, `folderdiscovery.ps1`) and every hive, key path, value name, and value now
matches. The `storesearch` confirmation-gate wiring (`DebloatCatalog.cs`,
`DebloatAction.cs`, `DebloatCatalogTests.cs`) is internally consistent — flag flipped,
doc comment updated, and the 6-key regression lock updated together.

However, the new regression test suite has real problems that undercut its stated
purpose. Its elevation-skip guard produces **false "Passed" results** (not "Skipped")
under non-elevated `dotnet test` runs — exactly the failure mode its own doc comment
claims to avoid. And two of its `finally` blocks do **not** actually restore all the
state the scripts under test mutate, so running these tests on an elevated machine
permanently alters real registry values that were never captured/restored — directly
contradicting the class's documented guarantee ("every fact that mutates real machine
state restores the pre-test value in a finally block regardless of pass/fail outcome").

## Critical Issues

### CR-01: Elevation-skip guard produces a false "Passed" result under non-elevated `dotnet test`, defeating the suite's stated purpose

**File:** `src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs:73-77` (and identically at 119-123, 159-163, 190-194, 219-223 — 5 of 8 new facts)
**Issue:**
```csharp
if (!IsElevated())
{
    Console.WriteLine("SKIPPED (requires elevation): ...");
    return;
}
```
This is a plain early `return` inside an xUnit v2 `[Fact]` (project pins `xunit 2.9.3`,
`Directory.Packages.props:35` — no `Xunit.SkippableFact` package referenced). xUnit v2
has no dynamic-skip mechanism reachable this way; a `[Fact]` method that returns
normally without an assertion failure is reported as **Passed**, not **Skipped**. The
`Console.WriteLine` reason is only visible if someone opens the per-test output — CI
dashboards, PR gates, and `dotnet test` summary counts will show these tests green.

This is precisely the failure mode the class's own doc comment says it avoids:
> "self-skip, with a printed reason, when the test process is not elevated — rather
> than failing the default `dotnet test` run or fabricating a false pass."

It fabricates exactly that false pass. Since a normal CI runner (and most local
`dotnet test` invocations) is not elevated, 5 of the 8 new regression facts — the ones
actually proving the CR-01/CR-02/CR-04/CR-05/CR-06 fixes this plan exists to lock in —
will silently verify nothing in the default pipeline while still reporting success.
The suite's protection is real only when a human remembers to elevate their shell and
run tests manually (as the 03-08 commit messages describe doing), which is not an
enforced, repeatable gate.

**Fix:** Add `Xunit.SkippableFact` (small, standard package for this exact xUnit v2
gap) and convert the guard to a real skip that reports as Skipped, not Passed:
```csharp
using Xunit; // SkippableFact, Skip

[SkippableFact]
public async Task LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values()
{
    Skip.IfNot(IsElevated(), "requires elevation");
    ...
}
```
If adding a package is out of scope, at minimum call a shared helper that throws a
distinctly-named exception and document in CI config that these tests require an
elevated runner — but the current silent-pass behavior should not ship.

### CR-02: LocationTracking tests' `finally` block leaves the 4th HKLM value (`SensorPermissionState`) permanently mutated — the class's "always restores" guarantee is false for this fact

**File:** `src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs:70-154`
**Issue:** `locationtracking.ps1`/`locationtracking-undo.ps1` each write **4** HKLM
values (confirmed by reading both scripts):
1. `ConsentStore\location!Value`
2. `Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}!SensorPermissionState`
3. `lfsvc\Service\Configuration!Status`
4. `SYSTEM\Maps!AutoUpdateEnabled`

Both `LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values` (line
71) and `LocationTracking_undo_alone_is_safe_and_idempotent_without_a_prior_run` (line
117) capture `before*` and restore in `finally` for only 3 of the 4
(`consentStorePath`, `lfsvcPath`, `mapsPath` — lines 79-85, 103-113). The 4th key
(`Sensor\Overrides\{GUID}!SensorPermissionState`) is never read before the test, never
restored after it, yet is written by both the Run script (to `0`) and the Undo script
(to `1`) that these tests actually execute. Any pre-existing value at that key (e.g.,
`0` from a corporate policy, or absent entirely) is silently and permanently
overwritten to `1` on every elevated run of this test, including passing runs.

**Fix:** Capture and restore the 4th value the same way the other 3 are handled:
```csharp
const string sensorOverridePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}";
var beforeSensor = ReadHklm(sensorOverridePath, "SensorPermissionState");
...
finally
{
    if (beforeSensor is not null) WriteHklm(sensorOverridePath, "SensorPermissionState", beforeSensor, RegistryValueKind.DWord);
    else DeleteHklmIfPresent(sensorOverridePath, "SensorPermissionState");
    // ...existing 3 restores...
}
```
If the key is intentionally excluded because it's unreliable across SKUs/VMs (as the
"three guaranteed" test name suggests), the test should not exercise a script path that
writes it without restoring it — either restore it defensively (guard on
`beforeSensor is not null` as above so a missing key isn't force-created) or document
explicitly why leaving it mutated is accepted.

### CR-03: ConsumerFeatures test's `finally` block only restores 1 of 9 registry values the Undo script under test actually writes

**File:** `src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs:156-185`
**Issue:** The test captures/restores only `DisableWindowsConsumerFeatures` (lines
165-166, 181-184). But `consumerfeatures-undo.ps1` (reviewed alongside) unconditionally
writes **8 additional HKCU values** every time it runs, regardless of what
`consumerfeatures.ps1` touched:
```
Explorer\Advanced!ShowSyncProviderNotifications
ContentDeliveryManager!ContentDeliveryAllowed
ContentDeliveryManager!OemPreInstalledAppsEnabled
ContentDeliveryManager!PreInstalledAppsEnabled
ContentDeliveryManager!PreInstalledAppsEverEnabled
ContentDeliveryManager!SilentInstallAppsEnabled
ContentDeliveryManager!SoftLandingEnabled
ContentDeliveryManager!SubscribedContentEnabled
```
This test invokes `consumerfeatures-undo.ps1` (line 176) with no attempt to snapshot or
restore any of these 8 values. On an elevated dev/CI machine, running this single test
permanently flips all 8 "suggested apps / content delivery / tips" settings to enabled
under the executing user's HKCU hive — whatever they were before (possibly
deliberately disabled by the developer, or by another tool) — and leaves them that way
after the test finishes, pass or fail. This directly contradicts the class doc comment:
"Every fact that mutates real machine state restores the pre-test value in a `finally`
block regardless of pass/fail outcome."

**Fix:** Either snapshot/restore all 9 values the script actually touches, or (simpler,
since these 8 are a documented "supplementary step" per the 03-08 commit message rather
than the value this fact is asserting on) avoid invoking the full undo script against
live state for this assertion and instead assert against the embedded resource content
for the supplementary keys (the same pattern already used for the storesearch icacls
fix in `StoreSearch_undo_script_contains_the_icacls_remove_deny_fix`), reserving the
live run/undo round-trip for the one value this fact is actually about:
```csharp
var beforeShowSync = ReadHkcu(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowSyncProviderNotifications");
var beforeContentDelivery = ReadHkcu(@"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager", "ContentDeliveryAllowed");
// ...one before/restore pair per value actually written...
```

## Warnings

### WR-01: `consumerfeatures-undo.ps1` writes 8 registry values its paired Run script never touched — scope creep beyond "reverse exactly what Run wrote"

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1:6-19`
**Issue:** `consumerfeatures.ps1` (the paired Run script) writes exactly one value:
`HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent!DisableWindowsConsumerFeatures`.
The undo script's CR-level fix (lines 2-5) correctly reverses that one value, but the
pre-existing loop (lines 6-19, kept per the 03-08 commit message as a "supplementary
step") additionally force-writes 8 unrelated `ContentDeliveryManager`/`Explorer\Advanced`
values to `1` every time a user clicks Undo on this single toggle — values this tool
never disabled via the Run direction and that may reflect the user's own independent
configuration (e.g., a user or a different tool/policy had `ContentDeliveryAllowed=0`
for reasons unrelated to this toggle). Clicking Undo on "Consumer Features" would
silently re-enable content delivery / suggested apps / tips features that were never
part of what Run disabled, which is surprising and not reversible via the Run action
(Run only touches the policy key, so re-running Run does not re-disable the 8 values
Undo just enabled).
**Fix:** Either scope the undo script to exactly the one value Run wrote (matching the
"exact reversal" contract the other 5 scripts in this same plan were just fixed to
follow), or make the broader restoration explicit and intentional by renaming/documenting
it as "restore Windows out-of-box consumer-features defaults" rather than presenting it
as a 1:1 undo of this Run script.

### WR-02: `locationtracking-undo.ps1` has no per-key existence guard or `-ErrorAction`, unlike the sibling `wpbt-undo.ps1`/`folderdiscovery-undo.ps1` fixed in the same commit

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1:2-5`
**Issue:** All 4 `Set-ItemProperty` calls assume every target key already exists.
`Set-ItemProperty` does not create a missing registry key (only a missing *value* under
an existing key); if the key doesn't exist (plausible for
`Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}` on machines/VMs without the
sensor stack initialized), PowerShell emits a non-terminating "Cannot find path" error
and silently skips that one write — the script still exits 0 (confirmed via
`ScriptRunner.RunProcessAsync` returning the raw `powershell.exe` exit code with no
`$LASTEXITCODE` inspection), so the tool reports success while one of the 4 values was
never actually reverted. This is asymmetric with the same commit's `wpbt-undo.ps1` and
`folderdiscovery-undo.ps1`, which both use `-ErrorAction SilentlyContinue` +
`Remove-ItemProperty`'s natural idempotency to make partial/missing state a documented
no-op rather than an unguarded assumption.
**Fix:** Add an existence guard (matching `consumerfeatures-undo.ps1`'s
`If (!(Test-Path $key.Path)) { New-Item ... }` pattern already used elsewhere in this
same file set) or `-ErrorAction SilentlyContinue` per line, so a missing key is a
documented no-op instead of a silent partial failure:
```powershell
if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}") {
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}" -Name "SensorPermissionState" -Value 1 -Type DWord -Force
}
```

## Info

### IN-01: Unnecessary no-op diagnostic call in the icacls round-trip test

**File:** `src/AkariToolbox.Tests/DebloatScriptRegressionTests.cs:296-297`
**Issue:** `await runner.RunProcessCaptureOutputAsync("icacls", $"\"{scratchPath}\"");`
is called with the comment "no assertion needed" — it exercises nothing the subsequent
deny/remove assertions don't already cover and adds process-spawn overhead with no
verification value.
**Fix:** Remove the baseline call, or if it's meant to prove the scratch file is
readable by `icacls` before the real assertions run, assert on its output too (e.g.
`Assert.DoesNotContain("Everyone", baselineOutput)`) so it earns its place as a test
step rather than a silent no-op.

---

_Reviewed: 2026-09-02T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
