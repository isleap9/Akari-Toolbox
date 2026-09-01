---
phase: 02-gaming-tweaks
reviewed: 2026-09-01T00:00:00Z
depth: standard
files_reviewed: 44
files_reviewed_list:
  - src/AkariToolbox.App/AkariToolbox.App.csproj
  - src/AkariToolbox.App/App.xaml.cs
  - src/AkariToolbox.App/MainWindow.xaml.cs
  - src/AkariToolbox.App/Resources/GamingScripts/cpp.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/directx.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/driverclean-auto.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/driverclean-manual.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/driverinstalldebloat-amd.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/driverinstalldebloat-intel.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/driverinstalldebloat-nvidia.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/driverinstalllatest-amd.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/driverinstalllatest-intel.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/driverinstalllatest-nvidia.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/nvidiasettings-default.ps1
  - src/AkariToolbox.App/Resources/GamingScripts/nvidiasettings-recommended.ps1
  - src/AkariToolbox.App/Services/GamingDropdownService.cs
  - src/AkariToolbox.App/Services/IGamingDropdownService.cs
  - src/AkariToolbox.App/Services/ITweakHandler.cs
  - src/AkariToolbox.App/Services/TweakCategory.cs
  - src/AkariToolbox.App/Services/TweakHandlerRegistration.cs
  - src/AkariToolbox.App/Services/TweakHandlers/BcdeditDismTweaks.cs
  - src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs
  - src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs
  - src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs
  - src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchA.cs
  - src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchB.cs
  - src/AkariToolbox.App/Services/TweakHandlers/ServiceBackedTweaks.cs
  - src/AkariToolbox.App/Services/TweakHandlers/WifiTweakHandler.cs
  - src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs
  - src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs
  - src/AkariToolbox.App/ViewModels/HomeViewModel.cs
  - src/AkariToolbox.App/Views/GamingTweaksPage.xaml
  - src/AkariToolbox.App/Views/GamingTweaksPage.xaml.cs
  - src/AkariToolbox.Framework/AkariToolbox.Framework.csproj
  - src/AkariToolbox.Framework/Fixtures/exit7.ps1
  - src/AkariToolbox.Framework/Services/IRegistryService.cs
  - src/AkariToolbox.Framework/Services/IScriptRunner.cs
  - src/AkariToolbox.Framework/Services/RegistryService.cs
  - src/AkariToolbox.Framework/Services/ScriptRunner.cs
  - src/AkariToolbox.Tests/GamingDropdownServiceTests.cs
  - src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs
  - src/AkariToolbox.Tests/GamingWindowsTweaksTests.cs
  - src/AkariToolbox.Tests/ScriptRunnerEmbeddedTests.cs
  - src/AkariToolbox.Tests/TweakCatalogTests.cs
  - src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs
findings:
  critical: 2
  warning: 4
  info: 0
  total: 6
status: issues_found
---

# Phase 02: Code Review Report

**Reviewed:** 2026-09-01
**Depth:** standard
**Files Reviewed:** 44
**Status:** issues_found

## Summary

Reviewed the Gaming Tweaks phase: 11 new `ITweakHandler` implementations (GPU/graphics
handlers in `GamingGraphicsTweaks.cs`, Windows/power/network handlers in
`GamingWindowsTweaks.cs`), the two D-09 registry dropdowns (`GamingDropdownService`), the
`GamingTweaksViewModel`/page wiring, the new `IScriptRunner.RunEmbeddedScriptAsync` /
`IRegistryService.DeleteSubKeyTree`/`CreateSubKey` primitives, the 12 embedded D-06
driver-install PowerShell scripts, and the associated tests.

The D-06 accepted-risk disclosure requirement is correctly implemented: every network-
dependent one-shot script launch logs an explicit unsigned-binary warning via
`ILogConsoleService` immediately before invoking `RunEmbeddedScriptAsync`
(`GamingTweaksViewModel.RunD06ScriptAsync`), and every embedded `.ps1` resource is
byte-identical (aside from expected admin/silent-mode boilerplate shared across all of
them) to what its own doc comments describe — no findings there.

Two BLOCKER-level correctness bugs were found, both involving an operation's success being
assumed rather than verified before either (a) an irreversible destructive action is taken,
or (b) the app's own tracked state is updated to say an operation succeeded. Both directly
contradict this app's stated core value ("every tweak... must apply correctly, report
accurate state, and be safely revertible"). Four warnings cover state-read inconsistencies
between sibling handlers and a test-coverage gap.

## Critical Issues

### CR-01: PowerPlanTweakHandler deletes existing power schemes without verifying their export succeeded

**File:** `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs:491-505`
**Issue:**
`EnableInternal()`'s own doc comment (lines 373-379) states this handler was "deliberately
hardened beyond the source script" specifically so that every pre-existing power scheme is
exported *before* it is deleted, avoiding the source script's destructive
`-restoredefaultschemes` behavior. In the actual implementation, every `powercfg` call in
this sequence discards its exit code:

```csharp
foreach (var guid in existingSchemeGuids)
{
    var exportPath = Path.Combine(exportDir, $"{guid}.pow");
    scriptRunner.RunProcessAsync("powercfg", $"-export \"{exportPath}\" {guid}").GetAwaiter().GetResult();
}

scriptRunner.RunProcessAsync("powercfg", $"/duplicatescheme {UltimatePerformanceBaseGuid} {CustomSchemeGuid}").GetAwaiter().GetResult();
scriptRunner.RunProcessAsync("powercfg", $"/setactive {CustomSchemeGuid}").GetAwaiter().GetResult();

foreach (var guid in existingSchemeGuids)
{
    scriptRunner.RunProcessAsync("powercfg", $"/delete {guid}").GetAwaiter().GetResult();
}
```

`IScriptRunner.RunProcessAsync` never throws — per its own contract it returns `-1` and
only *logs* failures (timeout, missing binary, non-zero exit, etc.). If any `-export` call
fails for any reason (scheme in use, disk full, permission issue, `powercfg` not on PATH in
a locked-down environment, etc.), nothing in `EnableInternal()` detects it — the code
proceeds straight to `/delete {guid}` for that same GUID a few lines later, permanently
destroying a power scheme the user never asked this app to touch, with no backup on disk.
`DisableInternal()` compounds this: it only checks whether *any* `.pow` file exists in the
export directory (`exportedFiles.Length > 0`, line 532) before deciding "restore from
backup" — it does not verify every originally-enumerated scheme was actually exported, so a
partially-failed export silently produces a partially-restorable backup that looks complete
to the code.

This is exactly the failure mode the class's own doc comment says was engineered around;
as implemented, a single transient `powercfg` failure turns "install a custom gaming power
plan" into irreversible loss of the user's other power schemes.

**Fix:** Check the exit code of every `-export` call and abort the whole enable operation
(without proceeding to `/duplicatescheme`/`/delete`) if any export fails, logging via
`ILogConsoleService` (already injected into this handler) so the user knows why the
operation was aborted. Similarly, verify `/duplicatescheme` and `/setactive` succeeded
before deleting original schemes:

```csharp
foreach (var guid in existingSchemeGuids)
{
    var exportPath = Path.Combine(exportDir, $"{guid}.pow");
    var exitCode = await scriptRunner.RunProcessAsync("powercfg", $"-export \"{exportPath}\" {guid}");
    if (exitCode != 0 || !File.Exists(exportPath))
    {
        log.Log($"[POWER-PLAN] Export of scheme {guid} failed (exit {exitCode}) — aborting before any delete.");
        return;
    }
}
```

---

### CR-02: DefenderTweakHandler marks Defender re-enabled even when SYSTEM-level restoration fails

**File:** `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs:150-173`
**Issue:**
On the re-enable path, `restoreOk` (whether native SYSTEM impersonation actually succeeded
in restoring the 16 Defender service `Start` values) is checked only to decide whether to
*log* an error — it does not gate what happens next:

```csharp
var restoreOk = ElevationService.RunAsSystem(() => { ... }, log.Log);

if (!restoreOk)
{
    log.Log("[DEFENDER] ERROR: Could not acquire SYSTEM to restore Defender services.");
}

registry.DeleteValue(RegistryHive.CurrentUser, DefenderStateKey, DefenderStateValue);
log.Log("[DEFENDER] Defender re-enabled. Restart required.");
```

`DeleteValue` here clears the app's own `DisableDefender` state flag unconditionally.
`GetState()` (line 80-81) reads that exact flag to decide whether the "Disable Defender"
toggle should show on/off. So when `restoreOk` is `false` — meaning the Defender services
were almost certainly *not* actually restored to `Start=2` — the toggle will nonetheless
read back as "not disabled" (i.e. the UI reports Defender as protecting the system again)
the next time state is read, and `SetDefenderAsync` completes without throwing, so
`ITweakCatalog`'s fault-based UI-correction path (used by both
`AkariOSTweaksViewModel`/`GamingTweaksViewModel`'s `OnTweakItemPropertyChanged`) never
fires either. The user is left believing antivirus protection was restored when it may not
have been — a false-assurance bug in a security-relevant control, in an app whose stated
core value is "report accurate state."

The disable path has the analogous issue in miniature: when Tamper Protection is detected
as ON (lines 101-108), the method logs the reason and returns — again without throwing —
so `SetState`/`SetStateAsync` reports success even though nothing was applied, and the
toggle stays showing "on" (disabled) until the page is fully reloaded and `GetState()` is
re-read from scratch.

**Fix:** Make both failure paths propagate a fault the catalog / ViewModel can react to
(e.g. `throw new InvalidOperationException(...)` after logging, instead of a bare `return`)
so `OnTweakItemPropertyChanged`'s existing CR-04 real-state-correction logic runs, and so
the `DefenderStateKey` flag is only cleared when `restoreOk` is actually `true`:

```csharp
if (!restoreOk)
{
    log.Log("[DEFENDER] ERROR: Could not acquire SYSTEM to restore Defender services.");
    throw new InvalidOperationException("Failed to restore Defender services as SYSTEM.");
}

registry.DeleteValue(RegistryHive.CurrentUser, DefenderStateKey, DefenderStateValue);
```

## Warnings

### WR-01: NetAdapterPowerSavingsTweakHandler.GetState() reads only the first adapter while SetState writes every adapter

**File:** `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs:205-217`
**Issue:** `SetState` loops over every adapter returned by
`NetworkAdapterEnumeration.GetAdapterSubKeys` and writes `PnPCapabilities`/the 13
`RegSzValueNames` to each one. `GetState`, however, only inspects
`GetAdapterSubKeys(registry).FirstOrDefault()` and reports the toggle's state from that one
adapter alone. This is inconsistent with the sibling handlers added in the same phase for
the same "every detected adapter" semantics — `HdcpTweakHandler`, `P0StateTweakHandler`,
and `IntelSettingsTweakHandler` (`GamingGraphicsTweaks.cs`) all use
`adapters.All(...)` for their `GetState()`. If a machine has more than one network adapter
and they diverge (a new adapter is plugged in after the toggle was last applied, or a write
to one adapter fails partway through a prior `SetState` call), the toggle will silently
misreport the true aggregate state, reading only whichever adapter happens to enumerate
first.
**Fix:** Use `.All(...)` across every adapter subkey, matching the pattern already
established by `HdcpTweakHandler`/`P0StateTweakHandler`/`IntelSettingsTweakHandler` in this
same phase, or explicitly document (as `DevicePowerSavingsTweakHandler` does at line 142)
why a single representative read is intentional here.

### WR-02: WriteCacheFlushTweakHandler.GetState() uses `.Any(...)` instead of `.All(...)`

**File:** `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs:274-275`
**Issue:**
```csharp
public bool GetState() =>
    DiskMatches().Any(diskPath => registry.GetValue(RegistryHive.LocalMachine, diskPath, "CacheIsPowerProtected") is int v && v == 1);
```
On a machine with multiple SCSI/NVMe disks, this reports the toggle as "on" as soon as a
single disk has `CacheIsPowerProtected=1`, even if the rest were never toggled (or were
independently reverted by Windows/another tool). This is the opposite aggregation
semantics from every other multi-target Gaming handler in this phase
(`HdcpTweakHandler`/`P0StateTweakHandler`/`IntelSettingsTweakHandler` all require `.All`
targets to agree before reporting "on"), with no doc comment explaining the deliberate
choice of `.Any` here.
**Fix:** Either switch to `.All(...)` for consistency with the rest of the phase's
multi-adapter handlers, or add a doc comment explaining why "at least one disk protected"
is the intended semantics for this specific tweak.

### WR-03: No behavior test coverage for HdcpTweakHandler

**File:** `src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs` (entire file); handler at
`src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs:37-76`
**Issue:** `HdcpTweakHandler`'s own doc comment describes it as "first Gaming-category
handler, proves the vertical slice" for this phase, yet `GamingGraphicsTweaksTests.cs`
contains `GetState`/`SetState` test coverage for `P0StateTweakHandler`, `MsiModeTweakHandler`,
`IntelSettingsTweakHandler`, and `AmdSettingsTweakHandler`, but zero tests exercising
`HdcpTweakHandler.GetState()`/`SetState()` behavior (writing `RMHdcpKeyglobZero`, the
all-adapters-must-agree read semantics, the zero-adapters-returns-false case). It is only
indirectly exercised via `TweakHandlerOrderingTests`' DI-registration/ordering assertions,
which check that the handler resolves and sorts correctly but never call `GetState`/
`SetState` at all.
**Fix:** Add `HdcpTweakHandler` tests mirroring the existing `P0StateTweakHandler` tests in
the same file (exact/no-adapter/mixed-adapter `GetState`, `SetState` true/false per-adapter
writes).

### WR-04: RunEmbeddedScriptAsync's missing-resource failure bypasses ILogConsoleService

**File:** `src/AkariToolbox.Framework/Services/ScriptRunner.cs:109-118`;
called from `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs:165-169`
**Issue:** Every other failure path in `ScriptRunner` (`RunProcessAsync`,
`RunProcessCaptureOutputAsync`) is wrapped in `try`/`catch` and logs via
`ILogConsoleService` before returning a sentinel value — this is called out in this
project's own doc comments as the app's "core value statement" (no background operation's
output is ever silently swallowed). `RunEmbeddedScriptAsync`, however, throws
`FileNotFoundException` *before* entering its `try` block when the requested resource
suffix cannot be found in any loaded assembly:
```csharp
var (asm, name) = FindEmbeddedResource(resourceSuffix)
    ?? throw new FileNotFoundException($"Embedded resource not found: {resourceSuffix}");
```
`GamingTweaksViewModel.RunD06ScriptAsync` (the sole caller for all 12 D-06 buttons) does not
wrap this call in a `try`/`catch` either, so this exception would propagate out of the
`[RelayCommand]`-generated async command unlogged to the in-app log dock, surfacing only via
`App.xaml.cs`'s generic `TaskScheduler.UnobservedTaskException` handler (which logs to the
file logger only, not the visible `ILogConsoleService` dock the user is looking at). All 12
current `resourceSuffix` values were verified to match the `.csproj`'s
`EmbeddedResource Include` entries exactly, so this is not currently reachable — but it is a
latent trap for the next resource added to this list (a typo would fail silently from the
user's perspective).
**Fix:** Wrap the resource lookup in `RunEmbeddedScriptAsync` in the same try/catch pattern
used by the other two methods (log via `ILogConsoleService`, return a sentinel exit code
instead of throwing), or add a `try`/`catch` around the call site in
`GamingTweaksViewModel.RunD06ScriptAsync` that logs via `_log` before rethrowing/swallowing.

---

_Reviewed: 2026-09-01_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
