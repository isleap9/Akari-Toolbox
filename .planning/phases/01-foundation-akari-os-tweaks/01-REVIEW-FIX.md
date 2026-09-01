---
phase: 01-foundation-akari-os-tweaks
fixed_at: 2026-09-01T00:00:00Z
review_path: .planning/phases/01-foundation-akari-os-tweaks/01-REVIEW.md
iteration: 1
findings_in_scope: 7
fixed: 6
skipped: 1
status: partial
---

# Phase 01: Code Review Fix Report

**Fixed at:** 2026-09-01T00:00:00Z
**Source review:** .planning/phases/01-foundation-akari-os-tweaks/01-REVIEW.md
**Iteration:** 1

**Summary:**
- Findings in scope: 7 (CR-01, CR-02, CR-03, CR-04, WR-01, WR-02, WR-03)
- Fixed: 6
- Skipped: 1 (WR-02 — requires a live Windows machine)

## Fixed Issues

### CR-01: Defender's fire-and-forget `SetState` defeats `TweakCatalog`'s per-key serialization

**Files modified:** `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs`
**Commit:** `4df3c18`
**Applied fix:** `DefenderTweakHandler.SetState` now blocks for the duration of the operation
(`SetDefenderAsync(disable).GetAwaiter().GetResult()`), matching every other handler's
contract, so `TweakCatalog.SetStateAsync`'s per-key semaphore actually serializes
concurrent Defender toggles instead of releasing almost instantly while the real work
continues in the background. This was addressed jointly with CR-03 as part of replacing
the elevation mechanism (see CR-03 below) — the native SYSTEM-impersonation path no
longer needs the fire-and-forget generated-.bat/RunOnce indirection that motivated the
original shape.

### CR-02: Async initial state load can silently revert a user's just-applied tweak

**Files modified:** `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs`
**Commit:** `768845d`
**Applied fix:** `PropertyChanged` is now subscribed only after the initial live-state
read's continuation sets `item.IsOn`, not before. Subscribing up front let a stale,
still-in-flight initial-load continuation race a user's early toggle through the
write-through pipeline in `OnTweakItemPropertyChanged` and silently revert a tweak the
user had just applied.

### CR-03: Elevated/TrustedInstaller executables (`MinSudo.exe`, `PowerRun.exe`) are never integrity-verified before execution

**Files modified:** `src/AkariToolbox.App/Services/ElevationService.cs` (new),
`src/AkariToolbox.App/Services/DefenderPhase2Scheduler.cs` (new),
`src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs`,
`src/AkariToolbox.App/Services/IPostInstallService.cs`,
`src/AkariToolbox.App/Services/PostInstallService.cs`
**Commit:** `4df3c18`
**Applied fix:** Per explicit project-owner direction, ported the native
P/Invoke-based SYSTEM-impersonation implementation (`ElevationService.RunAsSystem`) and
the RunOnce-based phase-2 scheduler (`DefenderPhase2Scheduler`) from the sibling
`Akari-Tool` repo, eliminating the `MinSudo.exe`/`PowerRun.exe` external-binary
dependency for the Defender workflow entirely rather than adding SHA256 pins for those
two binaries. No external executable is launched any more to gain SYSTEM/
TrustedInstaller rights for Defender's disable/re-enable/phase-2 paths — this removes
the unverified-elevated-binary-execution risk outright instead of merely gating it.
`DefenderScheduleCleanup`, `DefenderBuildServiceBat`, and
`DefenderRunAsTrustedInstallerAsync` were deleted (superseded by
`DefenderPhase2Scheduler.ScheduleRunOnce()` + `DefenderTweakHandler.RunPhase2Native`
and a direct `ElevationService.RunAsSystem` call in the re-enable path, respectively).
`IPostInstallService`/`PostInstallService` had `MinSudoPath`/`PowerRunPath`/
`MinSudoPresent`/`PowerRunPresent`/`EnsureMinSudoAsync` removed (confirmed via grep that
nothing else in `src/` referenced them); `EnsureDefenderFilesAsync`'s readiness check now
only requires `NoDefender.cab` + `DisableDefender.ps1`. The general ~130-entry
`AllFiles` Downloads-page manifest (Phase 4/DOWNLOADS-01 scope) was left untouched —
`Tweaks/MinSudo.exe`/`Tweaks/PowerRun.exe` remain listed there as general-purpose
downloadable tools, just no longer special-cased by Defender.

Note: `DefenderTweakHandler.RunPhase2Native` is implemented as directed (equivalent to
the reference `DefenderService.RunPhase2Native`) but wiring an actual
`--defender-phase2` command-line handler into `App.xaml.cs`'s startup path was not part
of the explicit fix instructions for this pass and was left out of scope — the
`RunOnce` entry is scheduled correctly by `DefenderPhase2Scheduler.ScheduleRunOnce()`,
but nothing yet parses `--defender-phase2` on relaunch and calls `RunPhase2Native`. This
should be tracked as a follow-up before the Defender phase-2 flow is exercised on a real
machine.

### CR-04: Failed tweak writes leave the toggle showing the requested state instead of the real state

**Files modified:** `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs`
**Commit:** `768845d`
**Applied fix:** On a faulted `SetStateAsync`, `OnTweakItemPropertyChanged` now looks up
the handler for the tweak's key, re-reads its real live state via `TryGetStateAsync`, and
reflects that value on the toggle — unsubscribing/resubscribing `PropertyChanged` around
the correction so setting `item.IsOn` back to the real value doesn't re-trigger another
write-through.

### WR-01: `OpenRealUserHive` leaks a process token handle (and the `Process` object) on every call

**Files modified:** `src/AkariToolbox.Framework/Services/RegistryService.cs`
**Commit:** `0c70637`
**Applied fix:** The `Process` object from `GetProcessesByName` is now disposed via
`using`, and the native token handle from `OpenProcessToken` is closed via a
`CloseHandle` P/Invoke in a `finally` block, so the handle is released even if
`WindowsIdentity` construction throws.

### WR-03: Leftover "TEMPORARY" debug smoke-test button shipped in the production shell

**Files modified:** `src/AkariToolbox.App/MainWindow.xaml`,
`src/AkariToolbox.App/MainWindow.xaml.cs`
**Commit:** `d64a1d9`
**Applied fix:** `PickerSmokeTestButton`/`OnPickerSmokeTestClick` removed entirely
(rather than gated behind `#if DEBUG`), along with the now-unused
`IFilePickerService` constructor dependency it was the sole consumer of. The Expander
header now shows only the "Log" label. Note: 01-VERIFICATION.md's manual test item #5
(picker smoke test button) is now moot for future UAT since the button no longer
exists — this was expected and documented as acceptable in the fix instructions.

## Skipped Issues

### WR-02: Defender's pinned SHA256 hashes are explicitly noted as unverified against a real machine

**File:** `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs:24-37`
**Reason:** Requires a live Windows machine to confirm the pinned SHA256 against the
actual downloaded bytes — already tracked as a human-verification item. No live Windows
test machine was available during this automated fix pass, and guessing new hash values
would be worse than leaving the existing trust-on-first-use pins in place with their
existing caveat comment.
**Original issue:** The comment above the two `Expected*Sha256` constants states no live
Windows test machine was available when the hashes were pinned, so they should be
re-confirmed against the actual local files during the Task 2 human real-machine check.
If either constant is wrong, the integrity gate will fail closed for every user, every
time, with only a generic "Integrity check failed" log line to go on.

## Verification

All fixes were built and verified inside an isolated git worktree
(`.claude/worktrees/rf-01-1762-1788242001`, branch `gsd-reviewfix/01-1762`), not the main
checkout — the numbers below are reproducible by rebuilding from the commits listed
above on `main` after the worktree's commits were fast-forwarded in.

- `dotnet build AkariToolbox.slnx -c Debug` — succeeded, 0 errors, 1 pre-existing warning
  (`MVVMTK0045` on `TweakItem._isOn`, unrelated to this fix pass) after every commit.
- `dotnet test src/AkariToolbox.Tests/AkariToolbox.Tests.csproj -c Debug` — 117/118 passed
  both before and after all fixes. The one failure,
  `ConvertersTests.EnumToBoolean_matches_parameter`, was confirmed pre-existing and
  unrelated (a `DispatcherQueue`/COM-activation environment dependency) by re-running the
  suite against the unmodified tree (`git stash` before/after) — it fails identically
  with or without this pass's changes.
- Grepped `src/` for `MinSudo|PowerRun|DefenderScheduleCleanup|DefenderBuildServiceBat|DefenderRunAsTrustedInstallerAsync|EnsureMinSudoAsync`
  after the CR-01/CR-03 fix — no remaining references outside the edited files.
- Grepped `src/AkariToolbox.Tests/` for the same removed members plus `MainWindow`/
  `IFilePickerService`/`DefenderTweakHandler`/`IPostInstallService` before making the
  CR-01/CR-03 and WR-03 changes — no test needed updating.

---

_Fixed: 2026-09-01T00:00:00Z_
_Fixer: Claude (gsd-code-fixer)_
_Iteration: 1_
