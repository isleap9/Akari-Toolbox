---
status: complete
phase: 02-gaming-tweaks
source: [02-VERIFICATION.md]
started: 2026-09-01T16:50:00Z
updated: 2026-09-01T17:18:00Z
---

## Current Test

[testing complete]

## Tests

### 1. Launch the app elevated. Navigate to Gaming Tweaks from both the Home card and the nav sidebar entry.
expected: Page renders exactly 11 toggle rows, 2 dropdowns, 2 display-settings shortcut buttons, and the D-06 driver-tools button section (12 buttons). Akari OS Tweaks page still shows exactly 32 toggles, unaffected.
result: pass

### 2. Spot-check at least 4 registry-backed toggles against real system state via a separate elevated terminal: GPU HDCP Override vs RMHdcpKeyglobZero, GPU MSI Mode vs MSISupported on a real GPU's PnP instance path, High-Precision Timer Resolution vs GlobalTimerResolutionRequests, and one of AmdSettings/IntelSettings/DevicePowerSavings/NetAdapterPowerSavings/WriteCacheFlush/NetworkIpv4Only. Flip a non-destructive toggle (e.g. GPU P0 State) on then off and confirm it round-trips to the real prior value both times.
expected: Each toggle's on-screen state matches live reg query / Get-NetAdapterBinding output on load; flipping off restores the documented off-value (delete or explicit 0), not a stale/cached UI value.
result: pass

### 3. Toggle 'Gaming Power Plan' on, confirm a custom scheme is created and pre-existing schemes are exported (check the temp export folder / powercfg /L output), then toggle it off and confirm powercfg -import restores the original schemes rather than falling back to the destructive powercfg -restoredefaultschemes path.
expected: All pre-existing power schemes survive an on/off round-trip intact; PowerPlanTweakHandler.EnableInternal() (CR-01 fix) verifies every powercfg -export exit code and confirms the file landed on disk before any /delete call runs.
result: pass

### 4. Toggle 'High-Precision Timer Resolution' on. Confirm the compiled service installs, starts, and GlobalTimerResolutionRequests is set. Toggle off and confirm the service is stopped/deleted and the registry value is removed. On a machine where csc.exe is deliberately unavailable/renamed, confirm the log dock shows a visible failure message rather than a silent no-op.
expected: Service lifecycle (install/start/stop/delete) and the pre-flight csc.exe probe both behave as coded; no silent failure.
result: pass

### 5. Change the SvcHost Split Threshold dropdown to a non-Default preset, confirm via reg query that the exact preset value was written. Select 'Default' and confirm the value is deleted (reg query reports ERROR: value not found), not set to a literal number. Repeat for Win32 Priority Separation (always writes a real hex value, no delete case).
expected: Dropdown selections write through to the registry exactly as GamingDropdownService's bounds-validated logic specifies; Default deletes rather than writing a guessed/legacy literal.
result: pass

### 6. Click one of the D-06 driver-tools buttons (e.g. 'DirectX', the shortest-running script) and confirm the risk-disclosure log line ('downloaded binary is NOT SHA256/signature-verified...') appears in the visible log dock before any download activity begins.
expected: Log line fires first, then the embedded script runs to completion or a clear failure is logged.
result: pass

### 7. On the Defender toggle (Phase 1, but touched by this phase's code review): trigger the re-enable path when SYSTEM-level service restoration is made to fail (e.g. simulate token/impersonation failure), and confirm the app throws/logs an error and does NOT clear its own DisableDefender flag — i.e. the toggle does not silently report 'Defender re-enabled' when it wasn't.
expected: InvalidOperationException propagates to OnTweakItemPropertyChanged's real-state-correction path; DefenderStateKey registry flag is only cleared when restoreOk is true (CR-02 fix).
result: pass

## Summary

total: 7
passed: 7
issues: 0
pending: 0
skipped: 0
blocked: 0

## Gaps
