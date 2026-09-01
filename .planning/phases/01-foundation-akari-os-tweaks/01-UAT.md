---
status: testing
phase: 01-foundation-akari-os-tweaks
source: [01-VERIFICATION.md]
started: 2026-09-01T01:10:00Z
updated: 2026-09-01T01:10:00Z
---

## Current Test

number: 1
name: Elevated launch, UAC prompt, Mica shell render
expected: |
  A UAC elevation prompt appears (or the process is already elevated with no c1010001
  manifest-merge build error); window title bar reads "Akari Toolbox"; shell renders
  with a visible Mica backdrop.
awaiting: user response

## Tests

### 1. Elevated launch, UAC prompt, Mica shell render
expected: A UAC elevation prompt appears (or process is already elevated with no c1010001 manifest-merge build error); window title bar reads "Akari Toolbox"; shell renders with a visible Mica backdrop.
result: [pending]

### 2. Home dashboard + nav sidebar
expected: Exactly 5 cards render on Home (Akari OS Tweaks, Gaming Tweaks, Debloat, Downloads, Misc); only Akari OS Tweaks is clickable and navigates; the other 4 show a visible "Coming soon" badge and produce no navigation/press feedback on click. Nav sidebar shows 6 entries (Home + 5 destinations) with only Home and Akari OS Tweaks enabled/clickable.
result: [pending]

### 3. 32-tweak live state + real prior-state revert
expected: 32 toggle rows render on Akari OS Tweaks; each reflects live registry/service state on load. Toggling "Disable WiFi" on sets `HKLM\SYSTEM\CurrentControlSet\Services\WlanSvc\Start` to 4; manually setting `WlanSvc\Start=3` via `reg add` before toggling on, then toggling off, restores 3 (not a hardcoded 2). Spot-check 2-3 more tweaks (Bluetooth/bthserv, Print Spooler/Spooler, Process Mitigation/FeatureSettingsOverride) against `reg query` output on load.
result: [pending]

### 4. Log dock behavior
expected: An Expander log panel is visible by default at the bottom of the window, collapses/re-expands without crashing, and flipping tweaks does not crash the app with the dock present.
result: [pending]

### 5. Elevation-safe picker smoke test
expected: Clicking the picker smoke-test button in the log dock header opens the native file-open dialog without a COMException/E_FAIL crash under elevation; picking a file logs its path, cancelling logs "cancelled"; rapid double-click does not open two dialogs.
result: [pending]

### 6. Defender two-phase workflow
expected: With Tamper Protection ON, flipping "Disable Defender" shows the exact 4-line Tamper Protection guidance with no partial state mutation. With Tamper Protection OFF and the pinned SHA256 constants confirmed against the actually-downloaded `C:\PostInstall` files, flipping the toggle copies `NoDefender.cab`, opens a second elevated PowerShell prompt, logs "Phase 1 complete. Please restart now.", and creates a RunOnce value `AkariDefenderCleanup` under `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce`. Separately, corrupting one byte of `NoDefender.cab` (or passing a wrong pinned hash) causes the toggle to refuse to proceed with an integrity-check-failed log line.
result: [pending]

## Summary

total: 6
passed: 0
issues: 0
pending: 6
skipped: 0
blocked: 0

## Gaps

None recorded yet — this file tracks the 6 human-verification items `01-VERIFICATION.md` could not run in the automated worktree environment (no live elevated Windows desktop session). See `01-VERIFICATION.md` for the full structural-verification evidence backing each item, and `01-REVIEW.md` for 4 Critical / 3 Warning / 3 Info code-review findings (CR-01 through CR-04 bear directly on tests 3 and 6 above — the Defender fire-and-forget concurrency issue, the initial-load/toggle race, incomplete SHA256 coverage on `MinSudo.exe`/`PowerRun.exe`, and no UI revert on write failure) that remain unfixed and should be weighed alongside these manual results.
