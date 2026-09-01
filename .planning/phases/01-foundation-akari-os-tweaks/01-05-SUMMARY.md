---
phase: 01-foundation-akari-os-tweaks
plan: 05
subsystem: tweaks
tags: [winui3, registry, service-controller, bcdedit, dism, tweak-handler]

# Dependency graph
requires:
  - phase: 01-foundation-akari-os-tweaks (plan 01-02)
    provides: IWindowsServiceController, IScriptRunner, IRegistryService primitives
provides:
  - ClipboardTweakHandler, BluetoothTweakHandler, CdromTweakHandler, PrintSpoolerTweakHandler (service-backed)
  - DepTweakHandler, BootMenuTweakHandler, HyperVTweakHandler, VrTweakHandler (bcdedit/DISM-hybrid)
  - IScriptRunner.RunProcessCaptureOutputAsync (new primitive member for output-parse read strategy)
affects: [tweak-catalog, akari-os-tweaks-page]

# Actuals (#2632)
actuals:
  tokens: 3950
  tasks: 2
  commits: 2

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "bcdedit-output-parse read strategy: GetState() spawns `bcdedit /enum {current}` via IScriptRunner.RunProcessCaptureOutputAsync, finds the relevant field line, parses its value — used when no registry key mirrors the bcdedit-only setting (D-12)"
    - "registry-portion-only read strategy: for hybrid write-side tweaks (bcdedit+DISM+registry), GetState() reads only the registry-backed representative key, never re-spawning bcdedit/DISM on a read (avoids process-spawn-per-toggle-load)"

key-files:
  created:
    - src/AkariToolbox.App/Services/TweakHandlers/ServiceBackedTweaks.cs
    - src/AkariToolbox.App/Services/TweakHandlers/BcdeditDismTweaks.cs
  modified:
    - src/AkariToolbox.Framework/Services/IScriptRunner.cs
    - src/AkariToolbox.Framework/Services/ScriptRunner.cs

key-decisions:
  - "Added IScriptRunner.RunProcessCaptureOutputAsync (buffers stdout into a StringBuilder, logs the joined result once at the end) rather than reusing RunProcessAsync's per-line streaming, since dep/bootmenu need the full captured text to parse, not just an exit code"
  - "CdromTweakHandler constructor-injects IRegistryService directly (not IWindowsServiceController) — documented exception because its enable path re-creates the entire IMAPI2 key including a Parameters subkey, which needs subkey creation beyond a simple Start DWord toggle"
  - "Preserved the predecessor's HyperVTweakHandler asymmetry verbatim: disable path writes DeviceGuard policy keys under SOFTWARE\\Policies\\..., enable path writes under SYSTEM\\CurrentControlSet\\Control\\... — not 'fixed' despite looking inconsistent"

patterns-established:
  - "bcdedit-output-parse: DepTweakHandler.FindLine is a shared internal static helper reused by BootMenuTweakHandler to avoid duplicating the line-scan/trim logic"

requirements-completed: [TWEAKS-01, TWEAKS-03]

coverage:
  - id: D1
    description: "4 service-backed tweak handlers (clipboard, bluetooth, cdrom, spooler) read/write live state via IWindowsServiceController, except cdrom's documented IRegistryService exception"
    requirement: "TWEAKS-01"
    verification:
      - kind: other
        ref: "dotnet build AkariToolbox.slnx -c Debug"
        status: pass
      - kind: other
        ref: "grep -c HasState|SaveState|ClearState src/AkariToolbox.App/Services/TweakHandlers/ServiceBackedTweaks.cs (returns 0)"
        status: pass
    human_judgment: false
  - id: D2
    description: "4 bcdedit/DISM-hybrid tweak handlers (dep, bootmenu, hyperv, vr) — dep/bootmenu read via live bcdedit output parse, hyperv/vr read via registry-portion only"
    requirement: "TWEAKS-03"
    verification:
      - kind: other
        ref: "dotnet build AkariToolbox.slnx -c Debug"
        status: pass
      - kind: other
        ref: "grep -c HasState|SaveState|ClearState src/AkariToolbox.App/Services/TweakHandlers/BcdeditDismTweaks.cs (returns 0)"
        status: pass
      - kind: other
        ref: "grep -c \"/enum {current}\" src/AkariToolbox.App/Services/TweakHandlers/BcdeditDismTweaks.cs (returns 2)"
        status: pass
    human_judgment: true
    rationale: "Correct live behavior of bcdedit/DISM-backed toggles (actual boot config / feature state changes) can only be confirmed on a real Windows machine with admin rights — not verifiable by a build-only automated check."

duration: 20min
completed: 2026-09-01
status: complete
---

# Phase 1 Plan 5: Service-Backed and bcdedit/DISM-Hybrid Tweak Handlers Summary

**8 ITweakHandler implementations closing out TWEAKS-01/TWEAKS-03 for every tweak whose live state needs IWindowsServiceController, IRegistryService directly (cdrom), or a bcdedit-output-parse/registry-portion-only read strategy.**

## Performance

- **Duration:** 20 min
- **Started:** 2026-08-31T23:52:00Z
- **Completed:** 2026-09-01T00:12:09Z
- **Tasks:** 2
- **Files modified:** 4 (2 created, 2 modified)

## Accomplishments
- `ServiceBackedTweaks.cs`: `ClipboardTweakHandler`, `BluetoothTweakHandler`, `PrintSpoolerTweakHandler` read/write via `IWindowsServiceController`; `CdromTweakHandler` uses `IRegistryService` directly (documented exception for IMAPI2 key re-creation)
- `BcdeditDismTweaks.cs`: `DepTweakHandler` and `BootMenuTweakHandler` derive `GetState()` by parsing live `bcdedit /enum {current}` output; `HyperVTweakHandler` and `VrTweakHandler` derive `GetState()` from their registry portion only, never re-spawning bcdedit/DISM on a read
- Added `IScriptRunner.RunProcessCaptureOutputAsync` — a companion to the existing exit-code-only `RunProcessAsync`, needed so dep/bootmenu can parse bcdedit's stdout

## Task Commits

Each task was committed atomically:

1. **Task 1: Service-backed tweaks (clipboard, bluetooth, cdrom, spooler)** - `77cfcd3` (feat)
2. **Task 2: bcdedit/DISM-hybrid tweaks (dep, bootmenu, hyperv, vr)** - `0baa2a1` (feat)

**Plan metadata:** committed separately (this SUMMARY + REQUIREMENTS.md)

## Files Created/Modified
- `src/AkariToolbox.App/Services/TweakHandlers/ServiceBackedTweaks.cs` - 4 service-backed tweak handlers
- `src/AkariToolbox.App/Services/TweakHandlers/BcdeditDismTweaks.cs` - 4 bcdedit/DISM-hybrid tweak handlers
- `src/AkariToolbox.Framework/Services/IScriptRunner.cs` - added `RunProcessCaptureOutputAsync` member
- `src/AkariToolbox.Framework/Services/ScriptRunner.cs` - implemented `RunProcessCaptureOutputAsync`

## Decisions Made
- `RunProcessCaptureOutputAsync` buffers stdout into a `StringBuilder` and logs the joined result once at completion, rather than each line individually like `RunProcessAsync` — matches the plan's explicit instruction and keeps the "no output silently swallowed" core value intact while still giving callers the full text to parse.
- `CdromTweakHandler` constructor-injects `IRegistryService` directly per the plan's explicit exception — its enable path needs subkey creation (`Parameters`) that `IWindowsServiceController`'s `Start`-DWord-only surface doesn't support.
- Preserved `HyperVTweakHandler`'s predecessor asymmetry (disable path writes `SOFTWARE\Policies\...`, enable path writes `SYSTEM\CurrentControlSet\Control\...`) verbatim, per explicit plan instruction not to "fix" it.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- All 8 non-pure-registry `ITweakHandler`s for Phase 1's 32-tweak set now exist, auto-discovered exactly like Plans 01-04's 22 pure-registry handlers — no shared-file edits needed.
- Live/on-device verification of bcdedit/DISM/service-controller behavior (real state toggles) still needs a manual pass on an actual Windows 10/11 machine with admin rights — flagged under `human_judgment: true` in the coverage block for D2, since a build-only check cannot confirm actual boot-config or feature-state changes.
- No blockers for subsequent plans in this phase.

## Self-Check: PASSED

- FOUND: src/AkariToolbox.App/Services/TweakHandlers/ServiceBackedTweaks.cs
- FOUND: src/AkariToolbox.App/Services/TweakHandlers/BcdeditDismTweaks.cs
- FOUND: .planning/phases/01-foundation-akari-os-tweaks/01-05-SUMMARY.md
- FOUND commit: 77cfcd3 (Task 1)
- FOUND commit: 0baa2a1 (Task 2)
- FOUND commit: 91563a7 (SUMMARY)

---
*Phase: 01-foundation-akari-os-tweaks*
*Completed: 2026-09-01*
