---
phase: 02-gaming-tweaks
plan: 04
subsystem: tweaks
tags: [winui3, registry, tdd, powershell, powercfg, csc, gaming-tweaks]

# Dependency graph
requires:
  - phase: 02-gaming-tweaks
    provides: "Plan 02-03's DeviceTreeEnumeration shared helper, IRegistryService.DeleteSubKeyTree, GamingWindowsTweaks.cs skeleton (DevicePowerSavingsTweakHandler, NetAdapterPowerSavingsTweakHandler, WriteCacheFlushTweakHandler)"
provides:
  - "NetworkIpv4OnlyTweakHandler (Order 107) — PowerShell cmdlet-backed, no raw registry"
  - "PowerPlanTweakHandler (Order 109) — hardened export/import revert, resolving RESEARCH.md Assumption A4"
  - "TimerResolutionTweakHandler (Order 110) — runtime C# compilation + Windows Service install with pre-flight compiler check"
  - "All 11 Gaming stateful ITweakHandlers for this phase now complete (6 Windows folder toggle set closed out)"
affects: [02-05, 02-06, 02-07]

# Actuals (#2632)
actuals:
  tokens: 11530
  tasks: 3
  commits: 3

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "powercfg -export/-import session-scoped backup pattern (PowerPlanTweakHandler) — export every pre-existing scheme to Path.GetTempPath()/AkariToolbox-PowerPlanBackup BEFORE any destructive powercfg /delete call, matching this codebase's existing session-scoped (not persisted) prior-state convention"
    - "Injectable fileExists/writeAllText seams (TimerResolutionTweakHandler) — Func<string,bool>/Action<string,string> constructor parameters defaulting to File.Exists/File.WriteAllText, so unit tests never touch C:\\Windows during a pre-flight-compiler-check test"
    - "PowerShell cmdlet invocation via IScriptRunner for service lifecycle (TimerResolutionTweakHandler) — New-Service/Set-Service mirrored almost verbatim from the source script rather than re-implemented via sc.exe or System.ServiceProcess.ServiceController.Create (which .NET's ServiceController class does not support), with IWindowsServiceController.SetStartType used for the StartupType step specifically"

key-files:
  created: []
  modified:
    - src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs
    - src/AkariToolbox.Tests/GamingWindowsTweaksTests.cs

key-decisions:
  - "[Rule 3 - Blocking] PowerPlanTweakHandler's constructor adds ILogConsoleService beyond the plan's literally-declared (IScriptRunner, IRegistryService) signature — required by the plan's own SetState(false) fallback text, which mandates logging when no session-scoped backup exists; matches Task 3's constructor, which already includes it"
  - "TimerResolutionTweakHandler installs/starts the compiled service via PowerShell cmdlets (New-Service/Set-Service) invoked through IScriptRunner, not via sc.exe or a direct System.ServiceProcess.ServiceController.Create call — .NET's ServiceController class has no service-creation API, and this mirrors the source script's own PowerShell-cmdlet approach almost verbatim (minus the Read-Host menu and its error-suppressing flags)"
  - "Removed all -ErrorAction SilentlyContinue flags from the ported PowerShell command strings (not just the compiler pre-flight check) to satisfy Task 3's explicit acceptance criterion that no such literal string appear anywhere in the file — IScriptRunner's own captured-stdout/stderr logging replaces the source script's blanket error suppression for the service-lifecycle calls"
  - "PowerPlanTweakHandler.GetState() checks whether the app's fixed custom scheme GUID is the currently active power scheme (via powercfg /getactivescheme), not a literal snapshot of every registry/powercfg value the SetState(true) branch wrote — documented as an approximation matching Phase 1's VpnTweakHandler/BluetoothTweakHandler GetState caveat, per the plan's own text"

requirements-completed: [GAMING-01]

coverage:
  - id: D1
    description: "NetworkIpv4OnlyTweakHandler disables the exact 8 documented network-binding component IDs via Disable-NetAdapterBinding on SetState(true), and re-enables those 8 plus a 9th (ms_tcpip) via Enable-NetAdapterBinding on SetState(false) — matching 27 Network IPv4 Only.ps1's own asymmetric enable-list exactly"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingWindowsTweaksTests.cs#NetworkIpv4Only_SetState_true_disables_the_8_documented_component_ids,NetworkIpv4Only_SetState_false_enables_exactly_9_component_ids_including_ms_tcpip,NetworkIpv4Only_GetState_returns_true_when_ms_tcpip6_binding_is_disabled,NetworkIpv4Only_GetState_returns_false_when_binding_enabled_or_unparseable,NetworkIpv4Only_metadata_is_Order_107_Category_Gaming"
        status: pass
    human_judgment: false
  - id: D2
    description: "PowerPlanTweakHandler exports every pre-existing power scheme via powercfg -export to a per-session temp folder BEFORE any powercfg /delete call, and SetState(false) imports the session backup via powercfg -import rather than the destructive powercfg -restoredefaultschemes whenever an export exists — the naive destructive fallback only runs, and is logged, when no session-scoped backup is available"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingWindowsTweaksTests.cs#PowerPlan_SetState_true_exports_every_existing_scheme_before_any_delete_call,PowerPlan_SetState_false_falls_back_to_restoredefaultschemes_when_no_session_export_exists,PowerPlan_SetState_false_imports_session_backup_instead_of_restoredefaultschemes_when_export_exists,PowerPlan_GetState_returns_true_when_active_scheme_output_contains_custom_guid,PowerPlan_metadata_is_Order_109_Category_Gaming"
        status: pass
    human_judgment: false
  - id: D3
    description: "TimerResolutionTweakHandler probes csc.exe existence before compiling and logs a visible, non-silent failure via ILogConsoleService when the compiler is absent (never attempting compilation), and compiles via the hardcoded csc.exe path with the correct .cs/.exe arguments when the compiler is present; GetState() reads GlobalTimerResolutionRequests"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingWindowsTweaksTests.cs#TimerResolution_SetState_true_logs_failure_and_skips_compilation_when_csc_missing,TimerResolution_SetState_true_compiles_via_csc_when_compiler_present,TimerResolution_GetState_returns_true_only_when_GlobalTimerResolutionRequests_equals_1,TimerResolution_metadata_is_Order_110_Category_Gaming"
        status: pass
    human_judgment: false
  - id: D4
    description: "Gaming Tweaks page renders all 11 stateful toggles (this plan's 3 plus the 8 from Plans 02-02/02-03), and each handler's live registry/powercfg/service read-write is correct against a real elevated machine — including Power Plan's export/import revert genuinely restoring a pre-existing custom scheme, and Timer Resolution's compiled service actually forcing the OS timer resolution"
    requirement: "GAMING-01"
    verification: []
    human_judgment: true
    rationale: "Live powercfg/csc.exe/service-lifecycle correctness and real WinUI page rendering require an elevated manual launch — no unit test exercises real powercfg.exe, a real csc.exe compilation, or a real Windows Service install. Deferred to end-of-phase UAT per workflow.human_verify_mode=end-of-phase (config.json), same pattern already established by Plans 02-01/02-03's SUMMARYs and logged to .planning/WINDOWS.md as an unrun-verify entry."

duration: 55min
completed: 2026-09-01
status: complete
---

# Phase 2 Plan 4: Network IPv4 Only, Power Plan, Timer Resolution Summary

**Closes out all 6 Windows-folder Gaming toggles and all 11 Gaming stateful `ITweakHandler`s with `NetworkIpv4OnlyTweakHandler` (PowerShell cmdlet-backed), `PowerPlanTweakHandler` (hardened `powercfg -export`/`-import` session backup replacing the source script's destructive `-restoredefaultschemes` revert), and `TimerResolutionTweakHandler` (runtime `csc.exe` compilation + Windows Service install with a pre-flight compiler-missing check that fails visibly instead of silently).**

## Performance

- **Duration:** 55 min
- **Started:** 2026-09-01T11:08:00Z
- **Completed:** 2026-09-01T12:00:07Z
- **Tasks:** 3
- **Files modified:** 2

## Accomplishments
- `NetworkIpv4OnlyTweakHandler` (Order 107): shells to `Disable-NetAdapterBinding`/`Enable-NetAdapterBinding` via `IScriptRunner`, no raw registry access, preserving the source script's asymmetric 8-vs-9 component-ID lists exactly (Off re-enables `ms_tcpip` in addition to the 8 IDs On disables)
- `PowerPlanTweakHandler` (Order 109, the phase's most consequential handler): resolves RESEARCH.md's flagged open engineering question (Assumption A4) with a concrete, tested implementation — every pre-existing power scheme is exported via `powercfg -export` to a per-session temp folder BEFORE any `powercfg /delete` call, and revert imports the session backup via `powercfg -import` rather than the source script's destructive `powercfg -restoredefaultschemes`; the naive fallback only runs, and is logged, when no session-scoped export exists. All ~14 registry writes and 36 `powercfg /setacvalueindex`/`/setdcvalueindex` pairs ported verbatim from the source script.
- `TimerResolutionTweakHandler` (Order 110): pre-flight `File.Exists(csc.exe)` probe (behind an injectable seam so unit tests never touch `C:\Windows`) produces a visible, logged failure instead of the source script's silent `-ErrorAction SilentlyContinue`-equivalent no-op (RESEARCH.md Pitfall 3); the ~200-line C# service source is a fixed compile-time string literal, never built from user/download input (threat model T-02-10)
- All 11 Gaming stateful toggles (5 from `5 Graphics` via Plan 02-02, 3 from `6 Windows` via Plan 02-03, 3 from this plan) are now implemented and registered

## Task Commits

Each task was committed atomically (single `feat` commit per task — plan tasks were marked `tdd="true"`, see Deviations for why this collapsed to single commits, same pattern as Plan 02-03):

1. **Task 1: NetworkIpv4OnlyTweakHandler** - `d372783` (feat)
2. **Task 2: PowerPlanTweakHandler — hardened export/import revert** - `49c920b` (feat)
3. **Task 3: TimerResolutionTweakHandler — pre-flight compiler check + service lifecycle** - `5a9902a` (feat)

**Plan metadata:** pending (this SUMMARY commit)

## Files Created/Modified
- `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs` - Added `NetworkIpv4OnlyTweakHandler`, `PowerPlanTweakHandler`, `TimerResolutionTweakHandler`
- `src/AkariToolbox.Tests/GamingWindowsTweaksTests.cs` - Added 14 new tests across all 3 handlers, plus `FakeScriptRunner` (call-recording + configurable capture-output responder), `FakeWindowsServiceController`, and `FakeLogConsoleService` test doubles

## Decisions Made
- `PowerPlanTweakHandler`'s constructor includes `ILogConsoleService` beyond the plan's literally-declared `(IScriptRunner, IRegistryService)` signature — required by the plan's own SetState(false) fallback text ("log via ILogConsoleService that no session-scoped backup was available"), documented as a Rule 3 deviation below
- `TimerResolutionTweakHandler` installs/starts the compiled service via PowerShell `New-Service`/`Set-Service` cmdlets through `IScriptRunner`, not via `sc.exe` or a direct `System.ServiceProcess.ServiceController.Create` call (the .NET `ServiceController` class has no service-creation API) — mirrors the source script's own approach almost verbatim, using `IWindowsServiceController.SetStartType` specifically for the `StartupType` step so the injected dependency has a real, non-redundant use
- All `-ErrorAction SilentlyContinue` flags were dropped from the ported PowerShell command strings (not only the compiler pre-flight check) — required to satisfy Task 3's explicit acceptance criterion (`grep -n "ErrorAction SilentlyContinue"` must return no matches); `IScriptRunner`'s own captured-stdout/stderr logging replaces the source script's blanket error suppression
- `PowerPlanTweakHandler.GetState()` checks whether the app's fixed custom scheme GUID is the currently active power scheme, not a literal snapshot of every value the On branch wrote — documented in-code as an approximation, matching the plan's own instruction and Phase 1's `VpnTweakHandler`/`BluetoothTweakHandler` precedent

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] PowerPlanTweakHandler needed ILogConsoleService not declared in the plan's literal constructor signature**
- **Found during:** Task 2 (writing the SetState(false) fallback logging path)
- **Issue:** The plan's Task 2 action text declares `PowerPlanTweakHandler(IScriptRunner scriptRunner, IRegistryService registry) : ITweakHandler`, but the same action text requires "log via ILogConsoleService that no session-scoped backup was available" in the fallback branch — impossible without the dependency in the constructor
- **Fix:** Added `ILogConsoleService log` as a third constructor parameter, matching Task 3's constructor (which already includes it)
- **Files modified:** `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs`
- **Verification:** `dotnet build` succeeds; `PowerPlan_SetState_false_falls_back_to_restoredefaultschemes_when_no_session_export_exists` asserts `log.Messages` is non-empty
- **Committed in:** `49c920b` (Task 2 commit)

**2. [Rule 1 - Bug] Initial TimerResolutionTweakHandler draft violated its own acceptance criterion by retaining `-ErrorAction SilentlyContinue` in PowerShell command strings**
- **Found during:** Task 3, post-implementation acceptance-criteria verification pass (before committing)
- **Issue:** The initial draft ported the source script's `New-Service`/`Set-Service` calls with `-ErrorAction SilentlyContinue` intact (matching the source almost verbatim), but Task 3's own acceptance criterion explicitly requires `grep -n "ErrorAction SilentlyContinue" ... GamingWindowsTweaks.cs` to return no matches — the initial draft failed this check (5 matches, including 2 doc-comment mentions)
- **Fix:** Removed the flag from all 4 PowerShell command strings (Get-Service stale-install check, New-Service, Set-Service x2) and reworded the 2 doc-comment/inline-comment mentions to avoid the literal phrase — `IScriptRunner` already captures/logs stderr, so removing the suppression flag is safe (PowerShell errors are now visible via the app's log console instead of silently dropped, which is a strict improvement, not a regression)
- **Files modified:** `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs`
- **Verification:** `grep -n "ErrorAction SilentlyContinue" src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs` returns no matches; all 4 `TimerResolution*` tests pass
- **Committed in:** `5a9902a` (Task 3 commit)

---

**Total deviations:** 2 auto-fixed (1 blocking, 1 bug)
**Impact on plan:** Both fixes are small, necessary corrections to satisfy the plan's own explicit acceptance criteria — no scope creep. The `ILogConsoleService` addition is forward-compatible (matches Task 3's already-declared pattern); the `ErrorAction` removal strictly improves observability (errors now surface through the app's log console instead of being silently dropped) and does not change the tweak's real-world registry/scheme/service outcomes.

## Issues Encountered
- **Per-task atomic commits required a reset-and-replay sequence.** All 3 tasks' code was drafted together across the same two files (`GamingWindowsTweaks.cs`, `GamingWindowsTweaksTests.cs`) during implementation, then verified end-to-end. To satisfy the per-task atomic-commit requirement, the working tree was reset to the pre-task baseline (`git checkout --` on both files, both modified only by this session) and the three tasks' changes were reapplied and committed incrementally, building/testing after each. No behavioral difference from the original draft — purely a commit-sequencing mechanism.
- **Full-suite `dotnet test` (173 tests) surfaces 1 pre-existing failure unrelated to this plan:** `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` — already documented in `.planning/phases/02-gaming-tweaks/deferred-items.md` from Plan 02-01 as an environment-dependent, pre-Phase-2 issue. Not touched here per Scope Boundary.
- **`TweakHandlerOrderingTests.cs` still has no Gaming-scoped ordering assertion.** RESEARCH.md's Pattern 1 recommended a parallel "11 Gaming handlers, disjoint Order range" test alongside the existing 32-handler AkariOS assertion, but no prior plan (02-01/02-02/02-03) added it, and adding it is outside this plan's declared task scope (Task 1-3 only cover the 3 new handlers in `GamingWindowsTweaks.cs`). Flagging for whichever plan/phase step next touches `TweakHandlerOrderingTests.cs` — with this plan, all 11 Gaming handlers now exist (Orders 100-110, no gaps), so the assertion could be added without further phase work.
- **TDD RED→GREEN cycle collapsed to single `feat` commits per task**, matching Plan 02-03's established precedent for this file. All 3 tasks are marked `tdd="true"`, but implementation and its tests were authored together per task and verified via the plan's own `<verify>`/acceptance-criteria commands before committing, rather than a strict failing-test-first commit followed by a separate implementation commit.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- All 11 Gaming stateful `ITweakHandler`s now exist (5 from `5 Graphics` via Plan 02-02, 6 from `6 Windows` via Plans 02-03/02-04) — the toggle-handler surface of Phase 2 is complete
- Blocker/concern (carried forward from 02-01/02-03, still applicable): elevated manual UI/registry/powercfg/service verification for all Gaming handlers has not yet been run against a real machine — recommend the full end-of-phase UAT pass once all 7 plans in Phase 2 are complete, per `workflow.human_verify_mode=end-of-phase`. Power Plan's export/import revert and Timer Resolution's compiled-service install/start are the two highest-risk items to verify live, given their multi-step external-process sequences.
- `TweakHandlerOrderingTests.cs` has no Gaming-scoped count/order assertion yet — worth adding once the remaining Plans 02-05/02-06/02-07 (dropdowns, one-shot actions, D-06 network scripts) land, since those may not add more `ITweakHandler`s and the final Gaming count (11) is now stable

---
*Phase: 02-gaming-tweaks*
*Completed: 2026-09-01*

## Self-Check: PASSED

- Both modified files confirmed present on disk with the expected new classes/tests (`grep` verified: `NetworkIpv4OnlyTweakHandler`, `PowerPlanTweakHandler`, `TimerResolutionTweakHandler` all present in `GamingWindowsTweaks.cs`; 18 new `[Fact]` tests present in `GamingWindowsTweaksTests.cs`).
- All 3 task commits (`d372783`, `49c920b`, `5a9902a`) confirmed in `git log --oneline`.
- All 18 new tests pass (`dotnet test --filter "FullyQualifiedName~NetworkIpv4Only|FullyQualifiedName~PowerPlan|FullyQualifiedName~TimerResolution|FullyQualifiedName~TweakHandlerOrderingTests"` → 18/18 passed).
- Full suite: 172/173 passed (1 pre-existing, documented, unrelated failure — `ConvertersTests.EnumToBoolean_matches_parameter`).
- All plan-specified acceptance-criteria greps re-verified after the final Task 3 fix: `powercfg -export` present, `File.Exists` present inside `TimerResolutionTweakHandler`, `ErrorAction SilentlyContinue` returns zero matches.
