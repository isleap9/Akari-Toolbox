---
status: complete
phase: 01-foundation-akari-os-tweaks
source: [01-VERIFICATION.md]
started: 2026-09-01T01:10:00Z
updated: 2026-09-01T09:00:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Elevated launch, UAC prompt, Mica shell render
expected: A UAC elevation prompt appears (or process is already elevated with no c1010001 manifest-merge build error); window title bar reads "Akari Toolbox"; shell renders with a visible Mica backdrop.
result: pass

### 2. Home dashboard + nav sidebar
expected: Exactly 5 cards render on Home (Akari OS Tweaks, Gaming Tweaks, Debloat, Downloads, Misc); only Akari OS Tweaks is clickable and navigates; the other 4 show a visible "Coming soon" badge and produce no navigation/press feedback on click. Nav sidebar shows 6 entries (Home + 5 destinations) with only Home and Akari OS Tweaks enabled/clickable.
result: pass

### 3. 32-tweak live state + real prior-state revert
expected: 32 toggle rows render on Akari OS Tweaks; each reflects live registry/service state on load. Toggling "Disable WiFi" on sets `HKLM\SYSTEM\CurrentControlSet\Services\WlanSvc\Start` to 4; manually setting `WlanSvc\Start=3` via `reg add` before toggling on, then toggling off, restores 3 (not a hardcoded 2). Spot-check 2-3 more tweaks (Bluetooth/bthserv, Print Spooler/Spooler, Process Mitigation/FeatureSettingsOverride) against `reg query` output on load.
result: pass

### 4. Log dock behavior
expected: An Expander log panel is visible by default at the bottom of the window, collapses/re-expands without crashing, and flipping tweaks does not crash the app with the dock present.
result: pass

### 5. Elevation-safe picker smoke test
expected: SUPERSEDED — the debug picker smoke-test button this test targeted was deliberately removed from the shipped shell (01-REVIEW-FIX.md WR-03, 2026-09-01) rather than fixed, since it was leftover debug scaffolding never meant to ship. There is no longer a button to click; the underlying `Microsoft.Windows.Storage.Pickers` elevation-safe picker implementation itself (APP-04) has no other UI consumer in Phase 1 and will get real coverage when Phase 4 wires an actual picker-using feature.
result: skipped
reason: "Test target (debug smoke-test button) was removed by the WR-03 code-review fix, not exercised as originally written."

### 6. Defender two-phase workflow
expected: |
  Updated 2026-09-01 to match the CR-01/CR-03 native-elevation rewrite and the
  follow-up PostInstall-removal fix (see 01-REVIEW-FIX.md and the two
  post-review commits) — this supersedes the original wording's references to
  pinned SHA256 constants and `C:\PostInstall`, which no longer exist.

  With Tamper Protection ON, flipping "Disable Defender" shows the exact
  4-line Tamper Protection guidance with no partial state mutation. With
  Tamper Protection OFF, flipping the toggle extracts the embedded
  NoDefender.cab + DisableDefender.ps1 to temp, copies the cab to
  `C:\Windows\NoDefender.cab`, opens a second elevated PowerShell prompt to
  install it, schedules a `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce`
  value named `AkariDefenderPhase2` (relaunches the app with
  `--defender-phase2`), and logs "Phase 1 complete. Please restart now." On
  reboot/next login, the app relaunches headlessly, runs the native
  SYSTEM-impersonation phase 2 (Defender service keys → Start=4, real-time
  monitoring disabled, SmartScreen takeover, CI/DeviceGuard keys, scheduled
  tasks disabled — logged to `%LocalAppData%\AkariToolbox\logs\defender-phase2.log`),
  clears the RunOnce entry, and exits without showing a window. Re-enabling
  restores the Defender package and writes every service key back to Start=2
  via native SYSTEM impersonation (no MinSudo.exe/PowerRun.exe involved
  anywhere in either direction).
result: pass

## Summary

total: 6
passed: 5
issues: 0
pending: 0
skipped: 1
blocked: 0

## Gaps

None. User confirmed full manual pass on real hardware after the CR-01/CR-02/CR-03/CR-04/WR-01/WR-03 code-review fixes and the follow-up PostInstall-removal change (native SYSTEM-impersonation Defender workflow, embedded cab/ps1, race-condition and revert-on-fault fixes, handle-leak fix, debug button removed). WR-02 (confirming the two now-embedded files' bytes) was independently closed during the fix pass by hashing the embedded `NoDefender.cab` against the previously-pinned SHA256 (exact match) and reading the embedded `DisableDefender.ps1` directly. Test 5 is recorded as skipped/superseded rather than failed — its target no longer exists by design, not by defect.
