---
phase: 01-foundation-akari-os-tweaks
plan: 02
subsystem: system-primitives
tags: [dispatcher-marshaling, service-controller, process-runner, winui3, logging]

requires:
  - phase: 01-foundation-akari-os-tweaks
    provides: "AkariToolbox solution, IRegistryService, ITweakHandler/ITweakCatalog stack, AkariOSTweaksViewModel (Plan 01-01)"
provides:
  - "ILogConsoleService/LogConsoleService - in-memory, dispatcher-safe log console with no dedup, backed by a collapsible dock panel in MainWindow"
  - "IWindowsServiceController/WindowsServiceController - named Start-DWORD registry wrapper for the 4 service-backed tweak handlers in Plan 01-05"
  - "IScriptRunner/ScriptRunner - process runner streaming stdout/stderr to ILogConsoleService, with timeout/kill and exception handling"
  - "System.ServiceProcess.ServiceController 10.0.11 package reference, ready for a future phase to extend IWindowsServiceController with live status"
  - "AkariOSTweaksViewModel now logs both read (GetStateAsync) and write-back (SetStateAsync) tweak failures via ILogConsoleService instead of Debug.WriteLine-only"
affects: [Plan 01-05 (service-backed and bcdedit/DISM tweak handlers), Plan 01-06 (Defender/PostInstall workflow)]

actuals:
  tokens: 5200
  tasks: 3
  commits: 4

tech-stack:
  added: [System.ServiceProcess.ServiceController 10.0.11]
  patterns:
    - "Nullable-dispatcher constructor injection for dispatcher-marshaled services: LogConsoleService(DispatcherQueue? dispatcher) short-circuits to a synchronous Lines.Add when dispatcher is null or HasThreadAccess is true, making the append/no-dedup behavior unit-testable headless (a plain xunit thread has no real DispatcherQueue) while still marshaling correctly in production via a DI factory lambda that captures DispatcherQueue.GetForCurrentThread() at first resolution"
    - "Named primitive wrapping a narrower primitive for future extensibility: IWindowsServiceController is a thin pass-through over IRegistryService's Start-DWORD reads/writes today, so Plan 01-05's handlers depend on it by name and a future phase can add live ServiceControllerStatus/start-stop-restart without touching those call sites"
    - "IScriptRunner ports ToolService.RunProcess 1:1 (OutputDataReceived/ErrorDataReceived -> ILogConsoleService.Log, Task.WhenAny timeout + process.Kill(entireProcessTree: true), catch-all returning -1) without porting RunScript's embedded-resource extraction, since no .ps1 resources are embedded yet in Phase 1 (reachability check: no Phase 1 tweak needs one)"

key-files:
  created:
    - src/AkariToolbox.Framework/Services/ILogConsoleService.cs
    - src/AkariToolbox.Framework/Services/LogConsoleService.cs
    - src/AkariToolbox.Framework/Services/IWindowsServiceController.cs
    - src/AkariToolbox.Framework/Services/WindowsServiceController.cs
    - src/AkariToolbox.Framework/Services/IScriptRunner.cs
    - src/AkariToolbox.Framework/Services/ScriptRunner.cs
    - src/AkariToolbox.Tests/LogConsoleServiceTests.cs
    - src/AkariToolbox.Tests/ScriptRunnerTests.cs
  modified:
    - src/AkariToolbox.Framework/ServiceCollectionExtensions.Primitives.cs (registered all 3 new primitives in AddAkariSystemPrimitives, the one existing call site)
    - src/AkariToolbox.App/MainWindow.xaml (added collapsible Expander log dock, default-expanded, bound to LogConsole.Lines)
    - src/AkariToolbox.App/MainWindow.xaml.cs (added ILogConsoleService constructor param and LogConsole property)
    - Directory.Packages.props (added System.ServiceProcess.ServiceController 10.0.11)
    - src/AkariToolbox.App/AkariToolbox.App.csproj (referenced the new package)
    - src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs (constructor-injects ILogConsoleService, replaced both Debug.WriteLine failure logs with ILogConsoleService.Log)

key-decisions:
  - "LogConsoleService's DispatcherQueue parameter is nullable (DispatcherQueue? dispatcher), not the plan's literally-stated non-nullable DispatcherQueue - this is what makes the append/no-dedup behavior (must-have truth #1/#2) unit-testable headless: DispatcherQueue.GetForCurrentThread() returns null on a plain xunit thread, and the null-dispatcher path appends synchronously instead of marshaling. Production DI still captures a real DispatcherQueue via a factory lambda in AddAkariSystemPrimitives."
  - "AddAkariSystemPrimitives registers ILogConsoleService via a factory lambda (services.AddSingleton<ILogConsoleService>(_ => new LogConsoleService(DispatcherQueue.GetForCurrentThread()))) rather than the plan's literal services.AddSingleton<ILogConsoleService, LogConsoleService>() shorthand, because DispatcherQueue is not itself registered in the DI container and the generic overload cannot supply it. Functionally identical outcome (same registration call site, same lifetime)."
  - "Both AkariOSTweaksViewModel failure paths (GetStateAsync read failures and SetStateAsync write-back failures) were switched from Debug.WriteLine to ILogConsoleService.Log, not only the write-back path the plan's action text explicitly names - the plan's own prohibition text covers 'a background tweak-state read, registry write, or script run' equally, so leaving the read-failure path on Debug-only would have left a silent-swallow gap the plan's own must-have language forbids."

patterns-established:
  - "Pattern 3: nullable-dispatcher constructor seam for headless-testable dispatcher-marshaled services (see tech-stack.patterns above) - any future MainWindow-bound observable service can reuse this shape."

requirements-completed: [APP-05]

coverage:
  - id: D1
    description: "ILogConsoleService.Lines appends every Log() call without dedup, dispatcher-marshaled via RunOnUIThreadAsync when a real dispatcher has no thread access"
    requirement: APP-05
    verification:
      - kind: unit
        ref: "AkariToolbox.Tests.LogConsoleServiceTests (3 tests: single append, no-dedup on repeated identical messages, background-thread call does not throw and appends)"
        status: pass
      - kind: build
        ref: "dotnet test src/AkariToolbox.Tests/AkariToolbox.Tests.csproj --filter \"FullyQualifiedName~LogConsole\" -> 3/3 pass"
        status: pass
    human_judgment: false
  - id: D2
    description: "Collapsible log dock (Expander wrapping a ListView bound to LogConsole.Lines) renders in MainWindow, default-expanded, and can be collapsed/expanded without crashing"
    requirement: APP-05
    verification:
      - kind: grep
        ref: "grep -n Expander src/AkariToolbox.App/MainWindow.xaml -> Expander present, x:Bind to LogConsole.Lines, IsExpanded=True"
        status: pass
      - kind: build
        ref: "dotnet build AkariToolbox.slnx -c Debug -> 0 errors (XAML compiles, x:Bind resolves)"
        status: pass
    human_judgment: true
    rationale: "Visual confirmation that the log dock actually renders at the bottom of the running elevated window, collapses/expands via its chevron without crashing, and that flipping the WiFi tweak toggle from Plan 01-01 still works with the dock present requires an interactive desktop session this headless worktree executor does not have. The XAML compiles and the x:Bind path is verified structurally; live rendering is deferred to a human per this plan's own human-check verify step."
  - id: D3
    description: "System.ServiceProcess.ServiceController 10.0.11 added to Directory.Packages.props and referenced in AkariToolbox.App.csproj; dotnet build restores and succeeds"
    requirement: APP-05
    verification:
      - kind: build
        ref: "dotnet build AkariToolbox.slnx -c Debug -> Build succeeded, 0 errors, package restored with no NU1101/NU1102 errors"
        status: pass
    human_judgment: false
  - id: D4
    description: "IWindowsServiceController wraps the Start registry DWORD (GetStartType/SetStartType) over IRegistryService, registered in AddAkariSystemPrimitives and resolvable via DI"
    requirement: APP-05
    verification:
      - kind: build
        ref: "dotnet build AkariToolbox.slnx -c Debug -> 0 errors; registration compiles against the existing AddSingleton<IRegistryService, RegistryService>() dependency chain"
        status: pass
    human_judgment: false
  - id: D5
    description: "IScriptRunner.RunProcessAsync captures stdout/stderr line-by-line, forwards every non-empty line to ILogConsoleService.Log, handles timeout (kill + -1) and exceptions (-1) without throwing"
    requirement: APP-05
    verification:
      - kind: unit
        ref: "AkariToolbox.Tests.ScriptRunnerTests (2 tests: cmd.exe /c echo hello -> exit 0 and Lines contains \"hello\"; 1ms timeout against a long-running ping -> exit -1, no throw)"
        status: pass
    human_judgment: false
  - id: D6
    description: "AkariOSTweaksViewModel's tweak read/write-back failures are now visibly logged via ILogConsoleService instead of Debug-only, closing the silent-swallow prohibition end-to-end"
    requirement: APP-05
    verification:
      - kind: grep
        ref: "grep -n \"Debug.WriteLine\" src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs -> no match (exit code 1)"
        status: pass
      - kind: build
        ref: "dotnet build AkariToolbox.slnx -c Debug -> 0 errors, 0 warnings"
        status: pass
    human_judgment: false

duration: 45min
completed: 2026-08-31
status: complete
---

# Phase 01 Plan 02: System Primitives - Log Console, Windows Service Controller, Script Runner Summary

**Dispatcher-safe log console dock (nullable-dispatcher seam for headless testability), a thin Start-DWORD `IWindowsServiceController` registry wrapper, and an `IScriptRunner` process runner that streams every captured stdout/stderr line to the log console instead of ever silently swallowing it.**

## Performance
- **Duration:** ~45min
- **Started:** 2026-08-31T16:34:00Z (approx, immediately following Plan 01-01 merge)
- **Completed:** 2026-08-31T17:18:36Z
- **Tasks:** 3 completed
- **Files modified:** 14 (8 created, 6 modified)

## Accomplishments
- Built `ILogConsoleService`/`LogConsoleService`: an `ObservableCollection<string> Lines` that appends unconditionally (no dedup, matching the predecessor's `TxtLog.AppendText`), dispatcher-marshaled via `RunOnUIThreadAsync` when called off the UI thread, with a nullable-dispatcher constructor seam that makes the append/no-dedup behavior unit-testable headless
- Added a collapsible `Expander` log dock to `MainWindow.xaml`, default-expanded, bound to `LogConsole.Lines`
- Added `System.ServiceProcess.ServiceController` 10.0.11 as a central package version, referenced from `AkariToolbox.App`, restoring and building cleanly
- Built `IWindowsServiceController`/`WindowsServiceController`: a named, self-documenting wrapper over `IRegistryService`'s `Start` DWORD reads/writes, so the 4 service-backed tweak handlers in Plan 01-05 depend on it by name rather than `IRegistryService` directly
- Built `IScriptRunner`/`ScriptRunner`: ported `ToolService.RunProcess` 1:1 (stdout/stderr streaming to the log console, `Task.WhenAny` timeout with `process.Kill(entireProcessTree: true)`, catch-all exception handling returning `-1`), deliberately not porting the embedded-resource `RunScript` extraction since no Phase 1 tweak needs it yet
- Closed the silent-swallow prohibition end-to-end in `AkariOSTweaksViewModel`: both the read (`GetStateAsync`) and write-back (`SetStateAsync`) failure paths now call `ILogConsoleService.Log` instead of `Debug.WriteLine`
- All 3 new primitives registered in the single `AddAkariSystemPrimitives()` call site, per this plan's own constraint not to add a second registration method

## Task Commits
1. **Task 1 (RED): failing test for log console** - `e5c8699` (test)
2. **Task 1 (GREEN): ILogConsoleService, LogConsoleService, MainWindow log dock** - `ff6165e` (feat)
3. **Task 2: IWindowsServiceController + ServiceController package** - `15c44bf` (feat)
4. **Task 3: IScriptRunner + AkariOSTweaksViewModel logging** - `2518202` (feat)

**Plan metadata:** commit pending (this SUMMARY.md + REQUIREMENTS.md, committed immediately after this file)

## Files Created/Modified
- `src/AkariToolbox.Framework/Services/ILogConsoleService.cs`, `LogConsoleService.cs` - log console primitive
- `src/AkariToolbox.App/MainWindow.xaml(.cs)` - collapsible log dock, `LogConsole` property
- `src/AkariToolbox.Framework/Services/IWindowsServiceController.cs`, `WindowsServiceController.cs` - Start-DWORD wrapper
- `Directory.Packages.props`, `src/AkariToolbox.App/AkariToolbox.App.csproj` - `System.ServiceProcess.ServiceController` 10.0.11
- `src/AkariToolbox.Framework/Services/IScriptRunner.cs`, `ScriptRunner.cs` - process runner
- `src/AkariToolbox.Framework/ServiceCollectionExtensions.Primitives.cs` - registered all 3 primitives
- `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs` - `ILogConsoleService`-backed read/write-back failure logging
- `src/AkariToolbox.Tests/LogConsoleServiceTests.cs`, `ScriptRunnerTests.cs` - must-have verification tests

## Decisions Made
- **`LogConsoleService`'s dispatcher parameter is nullable, not the plan's literal non-nullable `DispatcherQueue`.** `DispatcherQueue.GetForCurrentThread()` returns `null` on a plain xunit test thread (no real UI dispatcher exists there), so a nullable parameter with a synchronous-append fallback path is what makes the append/no-dedup must-have truths unit-testable headless at all, exactly as the plan's own behavior block anticipated ("if DispatcherQueue cannot be unit-tested headless..."). Production DI still captures and uses a real `DispatcherQueue` via a factory lambda.
- **DI registration for `ILogConsoleService` uses a factory lambda, not the plan's literal `AddSingleton<ILogConsoleService, LogConsoleService>()` shorthand.** The generic two-type overload cannot supply the constructor's `DispatcherQueue?` argument since `DispatcherQueue` itself isn't registered in the container; the factory lambda achieves the identical registration outcome (singleton, same call site) while actually compiling.
- **Both tweak-state failure paths in `AkariOSTweaksViewModel` were switched to `ILogConsoleService.Log`, not only the write-back path the plan's action text names.** The plan's own prohibition explicitly covers "a background tweak-state read, registry write, or script run" — leaving the read-failure path on `Debug.WriteLine` would have left exactly the silent-swallow gap the plan's must-have truths forbid.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking issue] `ILogConsoleService`'s DI registration needed a factory lambda, not the literal `AddSingleton<TInterface, TImpl>()` call**
- **Found during:** Task 1, while extending `AddAkariSystemPrimitives`
- **Issue:** `LogConsoleService`'s constructor requires a `DispatcherQueue?` argument the DI container cannot supply automatically (no `DispatcherQueue` registration exists), so the plan's literally-stated `services.AddSingleton<ILogConsoleService, LogConsoleService>();` would fail to resolve at runtime.
- **Fix:** Registered via `services.AddSingleton<ILogConsoleService>(_ => new LogConsoleService(DispatcherQueue.GetForCurrentThread()));` — same call site, same singleton lifetime, functionally equivalent outcome.
- **Files modified:** `src/AkariToolbox.Framework/ServiceCollectionExtensions.Primitives.cs`
- **Verification:** `dotnet build AkariToolbox.slnx -c Debug` succeeds; `MainWindow` (which constructor-injects `ILogConsoleService`) resolves and builds cleanly.
- **Commit:** `ff6165e`

**2. [Rule 2 - Missing critical functionality] Read-path (`GetStateAsync`) tweak failures were also silently Debug-only**
- **Found during:** Task 3, while updating the write-back catch block per the plan's action text
- **Issue:** The plan's action text names only the write-back (`SetStateAsync`) catch block for the `ILogConsoleService.Log` swap, but the constructor's `GetStateAsync` continuation had an identical `Debug.WriteLine`-only failure path — leaving it as-is would violate this plan's own prohibition ("MUST NOT silently swallow ... a background tweak-state read") the moment the write-back path was fixed but the read path was not.
- **Fix:** Replaced both `Debug.WriteLine` calls (read-path and write-back-path) with `ILogConsoleService.Log($"[TWEAK ERROR] ...")`.
- **Files modified:** `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs`
- **Verification:** `grep -n "Debug.WriteLine" src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs` returns no match; `dotnet build` succeeds with 0 warnings, 0 errors.
- **Commit:** `2518202`

**Total deviations:** 2 (1 Rule 3 blocking-issue fix, 1 Rule 2 missing-critical-functionality addition). **Impact:** Both were necessary to satisfy the plan's own must-have truths/prohibitions or to make the code actually compile and resolve at runtime; neither expands scope beyond what the plan already specifies.

## Issues Encountered

None beyond the two deviations above. The pre-existing, unrelated `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` failure (documented in `.planning/phases/01-foundation-akari-os-tweaks/deferred-items.md` from Plan 01-01) is still present and out of scope for this plan — confirmed via a full test run (110/111 passing, only that pre-existing failure).

## User Setup Required

None - no external service configuration required.

**Human verification still needed** (see Coverage D2 `human_judgment: true`): launch `dotnet run --project src/AkariToolbox.App -c Debug` from an elevated terminal and confirm:
1. A log panel (Expander labeled "Log") is visible at the bottom of the window by default
2. It can be collapsed and re-expanded via its chevron without crashing
3. Flipping the WiFi tweak toggle (from Plan 01-01) still works with the log dock present (no crash), even though nothing calls `Log()` yet from that path — Plans 01-05/01-06 wire real tweak/script output through `ILogConsoleService`/`IScriptRunner` next

## Next Phase Readiness

All three system primitives (`ILogConsoleService`, `IWindowsServiceController`, `IScriptRunner`) are registered, injectable, and unit-tested. Plans 01-05 and 01-06 can now:
- Inject `IWindowsServiceController` into service-backed tweak handlers (Bluetooth, and the other 3 service-loop tweaks) instead of calling `IRegistryService` directly
- Inject `IScriptRunner` for bcdedit/DISM calls (Plan 01-05) and the Defender/`powershell.exe -File` workflow (Plan 01-06), with all output automatically visible in the log dock
- Inject `ILogConsoleService` directly wherever a handler needs to report something outside the tweak read/write cycle

No further primitive-layer work is required before Plans 01-03 through 01-06 proceed.

## Self-Check: PASSED

All created files verified present on disk (ILogConsoleService.cs, LogConsoleService.cs, IWindowsServiceController.cs,
WindowsServiceController.cs, IScriptRunner.cs, ScriptRunner.cs, LogConsoleServiceTests.cs, ScriptRunnerTests.cs).
All commit hashes verified present in git log (e5c8699, ff6165e, 15c44bf, 2518202).

---
*Phase: 01-foundation-akari-os-tweaks*
*Completed: 2026-08-31*
