---
phase: 03-debloat
reviewed: 2026-09-02T00:07:54Z
depth: standard
files_reviewed: 66
files_reviewed_list:
  - src/AkariToolbox.App/AkariToolbox.App.csproj
  - src/AkariToolbox.App/App.xaml.cs
  - src/AkariToolbox.App/MainWindow.xaml.cs
  - src/AkariToolbox.App/Models/DebloatAction.cs
  - src/AkariToolbox.App/Models/DebloatActionItem.cs
  - src/AkariToolbox.App/Resources/DebloatScripts/activityhistory-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/activityhistory.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/bloatware-installall.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/bloatware-remove.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/deliveryoptimization-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/deliveryoptimization.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/disablebgapps-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/disablebgapps.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/disablebitlocker-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/disablebitlocker.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/diskcleanup.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/edgesettings-default.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/edgesettings-optimize.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/edgewebview-default.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/edgewebview-uninstall.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/endtask-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/endtask.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/hibernation-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/hibernation.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/locationtracking.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/oosu.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/removehomeandgallery-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/removehomeandgallery.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/removeonedrive-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/removeonedrive.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/rightclickmenu-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/rightclickmenu.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/services-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/services.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/storagesense-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/storagesense.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/storesearch-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/storesearch.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/telemetry-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/telemetry.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/tempfiles.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/utc-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/utc.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/visualeffects-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/visualeffects.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/widgets-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/widgets.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/windowsai-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/windowsai.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/wpbt-undo.ps1
  - src/AkariToolbox.App/Resources/DebloatScripts/wpbt.ps1
  - src/AkariToolbox.App/Services/DebloatCatalog.cs
  - src/AkariToolbox.App/Services/IDebloatCatalog.cs
  - src/AkariToolbox.App/Services/TweakHandlerRegistration.cs
  - src/AkariToolbox.App/ViewModels/DebloatViewModel.cs
  - src/AkariToolbox.App/ViewModels/HomeViewModel.cs
  - src/AkariToolbox.App/Views/DebloatPage.xaml
  - src/AkariToolbox.App/Views/DebloatPage.xaml.cs
  - src/AkariToolbox.Tests/DebloatCatalogTests.cs
findings:
  critical: 6
  warning: 7
  info: 3
  total: 16
status: issues_found
---

# Phase 03: Code Review Report

**Reviewed:** 2026-09-02T00:07:54Z
**Depth:** standard
**Files Reviewed:** 66
**Status:** issues_found

## Summary

Reviewed the Debloat feature end-to-end: the C# catalog/ViewModel/View layer and all 52
embedded PowerShell scripts (26 Run/Undo pairs plus 2 run-only actions). The C# layer
(`DebloatCatalog`, `DebloatViewModel`, `DebloatPage`) is clean, well-tested, and matches its
own documented design (D-01/D-02/D-11). The scripts, however, contain a systemic and
serious problem: for at least six of the twenty-six Run/Undo pairs, the "Undo" script does
**not** reverse the registry/filesystem change the paired "Run" script actually made — it
either targets a different registry hive/key/value than the one the Run script touched, or
it manipulates artifacts (files, env vars) that the Run script never created. Given this
project's stated core value — "every tweak... must apply correctly, report accurate state,
and (where applicable) be **safely revertible**" — these are correctness bugs in the
project's highest-priority guarantee, not cosmetic issues. Several other scripts have
undisclosed destructive side effects (forced process termination, an irreversible file ACL
deny) that aren't gated by the app's existing confirmation/risk-flag mechanisms. These are
listed below as Critical/Blocker findings. A smaller set of Warning/Info findings covers
gaps in the C# exception handling and UI affordances.

## Critical Issues

### CR-01: locationtracking-undo.ps1 does not reverse locationtracking.ps1's changes (wrong hive, missing keys)

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1:1-10`
**Issue:** `locationtracking.ps1` writes 4 values, all under `HKLM:`:
`HKLM:\...\ConsentStore\location!Value="Deny"`,
`HKLM:\...\Sensor\Overrides\{BFA794E4-...}!SensorPermissionState=0`,
`HKLM:\SYSTEM\...\lfsvc\Service\Configuration!Status=0`, and
`HKLM:\SYSTEM\Maps!AutoUpdateEnabled=0`.
The undo script never touches any of these. Instead it writes to
`HKLM:\SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors!DisableLocation=0` (a key the
Run script never set) and `HKCU:\...\ConsentStore\location!Value="Allow"` — note **HKCU**,
not the **HKLM** key the Run script actually changed. After Run + Undo, the location
service (`lfsvc`) remains disabled, the sensor permission override remains denied, and Maps
auto-update remains off — the feature is left in the "disabled" state even though the user
clicked Undo.
**Fix:**
```powershell
# Enable Location Tracking - Undo
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location" -Name "Value" -Value "Allow" -Force
Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}" -Name "SensorPermissionState" -Value 1 -Type DWord -Force
Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration" -Name "Status" -Value 1 -Type DWord -Force
Set-ItemProperty -Path "HKLM:\SYSTEM\Maps" -Name "AutoUpdateEnabled" -Value 1 -Type DWord -Force
Write-Host "Location tracking enabled."
```

### CR-02: consumerfeatures-undo.ps1 never clears the policy set by consumerfeatures.ps1

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1:1-16`
**Issue:** `consumerfeatures.ps1` sets exactly one value:
`HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent!DisableWindowsConsumerFeatures=1`.
The undo script sets 8 unrelated `HKCU:\...\ContentDeliveryManager` values instead, and
never removes/resets `DisableWindowsConsumerFeatures`. Because the Run script wrote a
`Policies` (Group-Policy-style) key, it takes precedence over the per-user
`ContentDeliveryManager` values the undo script actually touches — so after Undo, consumer
features remain disabled and the action is not reversible.
**Fix:** Have the undo script remove/reset the actual key the Run script wrote:
```powershell
$path = "HKLM:\SOFTWARE\Policies\Microsoft\Windows\CloudContent"
If (Test-Path $path) {
    Remove-ItemProperty -Path $path -Name "DisableWindowsConsumerFeatures" -Force -ErrorAction SilentlyContinue
}
Write-Host "Consumer Features restored to default."
```
(the existing `ContentDeliveryManager` block can stay as a supplementary step, but it must
not be the *only* thing the undo does).

### CR-03: storesearch Undo does not restore the ACL deny applied by storesearch.ps1, and Run has no confirmation gate for a destructive/irreversible ACL change

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/storesearch.ps1:2`, `src/AkariToolbox.App/Resources/DebloatScripts/storesearch-undo.ps1:1-9`, `src/AkariToolbox.App/Services/DebloatCatalog.cs:42-43`
**Issue:** `storesearch.ps1` runs
`icacls "...\Microsoft.WindowsStore_8wekyb3d8bbwe\LocalState\store.db" /deny Everyone:F`,
an explicit Deny ACE that blocks **all** access to that file for **all** security
principals (Deny ACEs take precedence over Allow ACEs in Windows ACL evaluation), which can
make the file — and the Store's search index it backs — permanently inaccessible until an
administrator manually resets its ACL. `storesearch-undo.ps1` does not run `icacls ... /remove:d Everyone`
or otherwise touch that file at all; it instead sets an unrelated
`HKCU:\...\Search!BingSearchEnabled=1` value. The action is presented to the user as
reversible (`HasUndo` renders an "Undo" button, per `DebloatCatalog.cs:42-43`
`storesearch`/`storesearch-undo` pair), but clicking it leaves the file permanently
ACL-locked. Additionally `RequiresConfirmation` is `false` for this action even though it
performs an effectively irreversible filesystem permission change.
**Fix:** Make the undo script actually remove the deny ACE it caused:
```powershell
icacls "$Env:LocalAppData\Packages\Microsoft.WindowsStore_8wekyb3d8bbwe\LocalState\store.db" /remove:d Everyone
```
and set `RequiresConfirmation: true` for the `storesearch` catalog entry given the ACL
change is not trivially reversible by an average user.

### CR-04: ps7telemetry-undo.ps1 never unsets the environment variable ps7telemetry.ps1 sets

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry.ps1:2`, `src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry-undo.ps1:1-10`
**Issue:** `ps7telemetry.ps1` sets the machine-wide environment variable
`POWERSHELL_TELEMETRY_OPTOUT=1`. The undo script never calls
`SetEnvironmentVariable` at all — it deletes a file (`$PSHOME\pwsh.exe.blocktel`) the Run
script never created and edits `$PROFILE` content that the Run script never wrote. Both
operations are guaranteed no-ops against artifacts that don't exist, and the actual
telemetry opt-out remains set forever after Undo, despite the success message
"PowerShell 7 telemetry re-enabled (opt-in)."
**Fix:**
```powershell
# Enable PowerShell 7 Telemetry - Undo
[System.Environment]::SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", $null, "Machine")
Write-Host "PowerShell 7 telemetry re-enabled (opt-in)."
```

### CR-05: wpbt-undo.ps1 never clears the DisableWpbtExecution value wpbt.ps1 sets

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/wpbt.ps1:1-4`, `src/AkariToolbox.App/Resources/DebloatScripts/wpbt-undo.ps1:1-9`
**Issue:** `wpbt.ps1` sets
`HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager!DisableWpbtExecution=1`. The undo
script removes a `Start` value under `HKLM:\SYSTEM\CurrentControlSet\Services\WPBT` (a key
the Run script never touched, and Windows has no such service to begin with) and attempts
`Set-Service`/`Start-Service -Name "WPBT"`, which will simply fail silently
(`-ErrorAction SilentlyContinue`) since no such service exists. `DisableWpbtExecution`
is never reset, so WPBT execution remains disabled permanently after Undo.
**Fix:**
```powershell
# Enable WPBT (Windows Platform Binary Table) - Undo
$path = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager"
Remove-ItemProperty -Path $path -Name "DisableWpbtExecution" -Force -ErrorAction SilentlyContinue
Write-Host "WPBT (Windows Platform Binary Table) enabled."
```

### CR-06: folderdiscovery-undo.ps1 never resets the FolderType value folderdiscovery.ps1 sets

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery.ps1:1-4`, `src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery-undo.ps1:1-9`
**Issue:** `folderdiscovery.ps1` sets
`HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell!FolderType="NotSpecified"`.
The undo script instead creates (if missing) then immediately removes
`FolderContentsMode` under a completely different path,
`HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced` — a value the Run
script never set in the first place, so the `Remove-ItemProperty` call is a guaranteed
no-op. `FolderType` under `Bags\AllFolders\Shell` is never touched, so automatic folder
discovery remains disabled after Undo.
**Fix:**
```powershell
# Enable Folder Discovery - Undo
$path = "HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell"
Remove-ItemProperty -Path $path -Name "FolderType" -Force -ErrorAction SilentlyContinue
Write-Host "Folder Discovery enabled (default restored)."
```

## Warnings

### WR-01: DebloatViewModel only catches FileNotFoundException — other script-launch failures are invisible to the user

**File:** `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs:107-113`
**Issue:** `ExecuteAsync` catches only `FileNotFoundException` around
`_scriptRunner.RunEmbeddedScriptAsync(...)`. `ScriptRunner.RunEmbeddedScriptAsync`
(`src/AkariToolbox.Framework/Services/ScriptRunner.cs:120-135`) can also throw
`IOException`/`UnauthorizedAccessException`/etc. from `File.Create`/`CopyToAsync` while
extracting the temp `.ps1` file — those exceptions are outside `RunProcessAsync`'s own
internal try/catch and are not `FileNotFoundException`, so they propagate out of
`ExecuteAsync` uncaught. Since this method is invoked via `AsyncRelayCommand` from a XAML
`Command` binding (not awaited by the caller), an uncaught exception here becomes an
unobserved task exception — only logged to the file logger via
`App.OnUnobservedTaskException`, never surfaced in the log dock or to the user, and
`item.IsRunning` is never reset to `false` in that path either since the `finally` block
still runs, but the user gets no visible error message at all (only `_log.Log` in the
`FileNotFoundException` branch is user-visible).
**Fix:** Broaden the catch (or catch `Exception` generically) and log it, mirroring the
existing pattern:
```csharp
catch (Exception ex)
{
    _log.Log($"[DEBLOAT] ERROR: {action.Title}{(isUndo ? " (Undo)" : "")} failed to launch — {ex.Message}");
}
```

### WR-02: Run/Undo buttons aren't disabled while an action IsRunning — rapid double-click can queue a duplicate execution

**File:** `src/AkariToolbox.App/Views/DebloatPage.xaml:71-79`
**Issue:** Neither the "Run" nor "Undo" `Button` binds `IsEnabled` to `!IsRunning` (or to
`RunActionCommand.CanExecute`). `DebloatViewModel.ExecuteAsync` does serialize concurrent
calls for the same action via a per-key `SemaphoreSlim` (`DebloatViewModel.cs:93-94`), but
that only prevents *overlap* — it does not prevent a second invocation from being queued at
all. A user double-clicking "Run" on, e.g., "OneDrive — Remove" or "BitLocker — Disable"
before the first click's confirmation dialog closes can queue two full runs of the same
script back-to-back (the second waits on the semaphore, then runs again), each preceded by
its own confirmation dialog.
**Fix:** Bind `IsEnabled="{x:Bind IsRunning, Mode=OneWay, Converter={StaticResource BoolNegationConverter}}"` (or add a `CanRun`/`CanUndo` observable property) to both buttons.

### WR-03: oosu.ps1 downloads and launches an unverified binary elevated with no risk flag or accepted-risk logging

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/oosu.ps1:7-9`, `src/AkariToolbox.App/Services/DebloatCatalog.cs:91-92`, `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs:99-102`
**Issue:** `oosu.ps1` downloads `OOSU10.exe` over HTTPS and immediately `Start-Process`es
it with no SHA256/signature verification — the exact "highest-severity failure mode"
this project's own tech-stack documentation calls out (executing a tampered/corrupted
downloaded binary under admin rights). The two other places in this app that accept this
same risk (`edgesettings`/`edgewebview` Undo) are flagged via
`DebloatAction.UndoDownloadsUnverifiedBinary`, which drives both a UI caption
(`DebloatPage.xaml:57-62`) and an accepted-risk log line
(`DebloatViewModel.cs:99-102`). `DebloatCatalog`'s `oosu` entry sets no such flag (the model
only has `UndoDownloadsUnverifiedBinary`, not an equivalent for the Run direction), so a
user running "O&O ShutUp10++ — Run" gets no caution caption and no accepted-risk log entry
at all, even though it carries the identical risk.
**Fix:** Add a `RunDownloadsUnverifiedBinary` flag to `DebloatAction`/`DebloatActionItem`
(or generalize the existing flag to cover both directions) and wire it through the same UI
caption + `_log.Log(...)` accepted-risk line used for the Undo case.

### WR-04: windowsai.ps1 force-kills unrelated running processes without disclosure or confirmation

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/windowsai.ps1:8-10`, `src/AkariToolbox.App/Services/DebloatCatalog.cs:35-37`
**Issue:** "Windows AI — Disable" is documented in the catalog as "Removes the Copilot
AppX package and disables it via registry policy." The script it runs also force-terminates
`OneDrive`, `OneDrive.Sync.Service`, `msedge`/`msedgewebview2`, `SearchHost`/`Search`,
`WidgetService`/`Widgets`, `RuntimeBroker`, `GameBar`, and several other unrelated
processes (line 8-10) before touching Copilot at all. None of this is mentioned in the
catalog `Description`, and `RequiresConfirmation` is `false`, so a user can lose unsaved
work in open Edge tabs or interrupt an in-progress OneDrive sync with a single un-confirmed
click that they believe only "disables Copilot."
**Fix:** Either scope the process-kill list down to processes that actually need to release
a lock on the Copilot package (if any), or set `RequiresConfirmation: true` and update the
description to disclose the side effect.

### WR-05: edgewebview-default.ps1 (Undo for "Microsoft Edge — Remove") silently re-applies the unrelated "Edge Settings — Debloat" policy set

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/edgewebview-default.ps1:41-80`, `src/AkariToolbox.App/Resources/DebloatScripts/edgesettings-optimize.ps1:14-49`
**Issue:** After reinstalling Edge/WebView2, `edgewebview-default.ps1` goes on to apply the
*exact* policy block from `edgesettings-optimize.ps1` (ublock-origin forcelist,
`HardwareAccelerationModeEnabled=0`, `BackgroundModeEnabled=0`, `StartupBoostEnabled=0`,
removal of Edge logon/RunOnce entries, Edge services, scheduled tasks, and the IE-to-Edge
BHO). A user who clicks "Undo" on "Microsoft Edge — Remove" (a separate catalog entry from
"Microsoft Edge — Debloat") did not opt into the Debloat policy set, yet it is applied as an
undisclosed side effect.
**Fix:** Drop the debloat-policy block from `edgewebview-default.ps1` (just reinstall Edge
+ WebView2 and stop there), or explicitly document/surface in the catalog description that
Undo also re-applies the Edge debloat policy set.

### WR-06: widgets-undo.ps1's package restoration is a guaranteed no-op but reports "attempted" without indicating failure

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/widgets.ps1:3-4`, `src/AkariToolbox.App/Resources/DebloatScripts/widgets-undo.ps1:1-14`
**Issue:** `widgets.ps1` removes both AppX packages with
`Remove-AppxPackage -AllUsers`, which fully removes the package payload from the system
(not just a per-user deprovision). `widgets-undo.ps1` then does
`Get-AppxPackage -AllUsers -Name $pkg | Add-AppxPackage`, but since the package was fully
removed there is nothing left for `Get-AppxPackage` to return, so the pipeline is always
empty and `Add-AppxPackage` never runs. Only the unrelated `WidgetService` startup type is
actually restored. The script still prints "Widgets restoration attempted." with no
indication that the restoration itself always fails, so a user has no way to know Widgets
was not actually brought back.
**Fix:** Detect the empty-pipeline case and log a clear message (e.g. "Widgets packages
could not be restored automatically — reinstall from the Microsoft Store"), or attempt the
Store-based reprovisioning path (`Add-AppxPackage -RegisterByFamilyName` /
DISM `Add-ProvisionedAppxPackage`) that can work after a full removal.

### WR-07: utc.ps1 / utc-undo.ps1 write RealTimeIsUniversal with inconsistent registry value types

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/utc.ps1:5`, `src/AkariToolbox.App/Resources/DebloatScripts/utc-undo.ps1:9`
**Issue:** `utc.ps1` writes `RealTimeIsUniversal` as `-Type QWord`; `utc-undo.ps1` writes
the same value name as `-Type DWord`. Functionally the value still gets overwritten either
way (so the tweak works), but the registry value's on-disk type flips between Run and Undo,
which is an inconsistency likely to confuse anyone diffing the registry or writing
additional tooling against this key later. Windows documents `RealTimeIsUniversal` as
`REG_DWORD`; the `-Type QWord` in the Run script also looks like the more likely mistake of
the pair.
**Fix:** Use the same `-Type DWord` in both scripts.

## Info

### IN-01: bloatware-installall.ps1 / windowsai-undo.ps1 reinstall logic silently does nothing when packages were fully removed

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/bloatware-installall.ps1:17-18`, `src/AkariToolbox.App/Resources/DebloatScripts/windowsai-undo.ps1:7-10`
**Issue:** Both scripts reinstall via
`Get-AppxPackage -AllUsers | Foreach {Add-AppxPackage -Register ... $_.InstallLocation\AppXManifest.xml}`.
This only works for packages that are still discoverable/on-disk (e.g. deprovisioned but
not deleted); for packages whose files were fully removed, the pipeline silently produces
nothing to re-register. Same root cause as WR-06 but for the higher-blast-radius
"Unwanted Apps — Remove" (bloatware) and "Windows AI — Disable" actions.
**Fix:** Consider documenting this as a known limitation in the catalog description (as
already done for the D-10 unverified-binary risk), since users may reasonably expect
"Undo"/"Reinstall" to be complete.

### IN-02: Several scripts call Set-ItemProperty without a Test-Path/New-Item guard, unlike the majority pattern in the same script set

**File:** `src/AkariToolbox.App/Resources/DebloatScripts/locationtracking.ps1:2-5`, `src/AkariToolbox.App/Resources/DebloatScripts/wpbt.ps1:2-3`, `src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery.ps1:2-3`
**Issue:** Most scripts in this set wrap `Set-ItemProperty` with
`If (!(Test-Path $path)) { New-Item -Path $path -Force | Out-Null }` first. These three
scripts call `Set-ItemProperty` directly against paths that are not guaranteed to
pre-exist on every Windows edition/build. If the path is missing, `Set-ItemProperty` emits
a non-terminating error (silently, since none of these calls use `-ErrorAction Stop`) and
the script still prints its unconditional "disabled"/success `Write-Host` message,
misleading the user about whether the tweak actually applied.
**Fix:** Add the same `Test-Path`/`New-Item` guard used elsewhere in this file set for
consistency and correctness.

### IN-03: MainWindow.LoadAppIcon returns null! for a non-nullable ImageSource property

**File:** `src/AkariToolbox.App/MainWindow.xaml.cs:66-72`
**Issue:** `AppIconSource` is declared as non-nullable `ImageSource` but
`LoadAppIcon()` returns `null!` when `Assets/AppIcon.png` doesn't exist at
`AppContext.BaseDirectory`, using the null-forgiving operator to bypass the compiler's
nullable check rather than making the property genuinely nullable. Any future consumer of
`AppIconSource` that trusts its declared non-nullability could NRE if the asset is ever
missing from a deployment.
**Fix:** Declare `public ImageSource? AppIconSource { get; }` and update any XAML/consumer
to tolerate `null`, or fall back to an embedded default icon instead of `null!`.

---

_Reviewed: 2026-09-02T00:07:54Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
