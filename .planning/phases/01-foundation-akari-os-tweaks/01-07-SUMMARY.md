---
phase: 01-foundation-akari-os-tweaks
plan: 07
subsystem: testing
tags: [xunit, dependency-injection, winui3, tweak-handlers, regression-test]

# Dependency graph
requires:
  - phase: 01-foundation-akari-os-tweaks
    provides: All 32 ITweakHandler implementations (Plans 01-01, 01-04, 01-05, 01-06), AkariOSTweaksViewModel/TweakCatalog wiring
provides:
  - "Automated regression test (TweakHandlerOrderingTests) locking the 32-handler count, Order [0..31] uniqueness/no-gaps invariant, and exact predecessor key sequence"
  - "AkariOSTweaksViewModel.TryGetStateAsync: per-handler error-resilience seam that catches a throwing GetState(), logs via ILogConsoleService, and defaults the toggle to false"
  - "InternalsVisibleTo(AkariToolbox.Tests) on AkariToolbox.App for future internal test seams"
  - "Full-catalog re-verification that the D-03 anti-pattern grep, APP-03 theme-absence check, and MicaBackdrop presence all hold across the assembled 32-tweak tree"
affects: [02-gaming-tweaks, 03-debloat, verification, ship]

# Actuals (#2632)
actuals:
  tokens: 2600
  tasks: 2
  commits: 2

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Static internal test seam (TryGetStateAsync(catalog, log, handler)) used instead of an instance method, because constructing AkariOSTweaksViewModel requires DispatcherQueue.GetForCurrentThread() to succeed — which throws a COMException in a headless xunit host rather than returning null, unlike calling it off a UI thread inside a real running app."
    - "DI-registration-override test pattern: re-registering a singleton (ILogConsoleService) after AddAkariSystemPrimitives() in a test-only ServiceCollection to swap out a WinRT-dependent factory for a headless-safe instance, relying on 'last registration wins' resolution semantics — production wiring (App.xaml.cs) is untouched."

key-files:
  created:
    - src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs
    - src/AkariToolbox.App/AssemblyInfo.cs
  modified:
    - src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs
    - src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs

key-decisions:
  - "Made TryGetStateAsync internal and static (catalog/log/handler as parameters) rather than an internal instance method, specifically to let tests exercise the error-resilience path without ever constructing AkariOSTweaksViewModel (whose constructor's DispatcherQueue.GetForCurrentThread() call is not headless-testable in this environment)."
  - "Reworded a code comment in DefenderTweakHandler.cs (SaveState/ClearState -> prose) that was a false-positive match against Task 2's D-03 regression grep — it referenced the predecessor's old method names for context, not actual anti-pattern code, but the grep is meant to be a zero-noise backstop for future CI runs."

patterns-established:
  - "Full-catalog invariant tests belong in a dedicated *OrderingTests file resolved via the real AddMvvmFramework+AddAkariSystemPrimitives+AddTweakHandlers DI graph, with only WinRT-dependent singletons (ILogConsoleService) swapped for headless-safe fakes."

requirements-completed: [APP-03, APP-05, TWEAKS-01, TWEAKS-03]

coverage:
  - id: D1
    description: "Exactly 32 ITweakHandler implementations resolve from the real DI container, with unique Order values spanning [0..31] with no gaps or duplicates, matching the predecessor's exact 32-tweak key sequence."
    requirement: "TWEAKS-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs#Resolving_ITweakHandler_yields_exactly_32_handlers"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs#Handler_order_values_span_0_to_31_with_no_gaps_or_duplicates"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs#Handlers_sorted_by_order_match_predecessors_exact_key_sequence"
        status: pass
    human_judgment: false
  - id: D2
    description: "A handler whose GetState() throws during the per-item async read is caught and logged via ILogConsoleService, defaulting the corresponding toggle to IsOn=false instead of crashing page load."
    requirement: "TWEAKS-03"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs#TryGetStateAsync_catches_throwing_handler_logs_and_defaults_to_false"
        status: pass
    human_judgment: false
  - id: D3
    description: "Full-catalog D-03 anti-pattern regression grep (HasState/SaveState/ClearState, HKCU\\Software\\AkariTool) is clean outside the documented Defender exception; no WPF-UI theme files exist under src/; MainWindow.xaml.cs still sets SystemBackdrop = new MicaBackdrop() exactly once."
    requirement: "APP-03"
    verification:
      - kind: other
        ref: "grep -rn \"HasState|SaveState|ClearState\" src/AkariToolbox.App/Services/TweakHandlers/ src/AkariToolbox.App/Services/TweakCatalog.cs"
        status: pass
      - kind: other
        ref: "grep -rln \"Themes/Colors.xaml|Themes/Controls.xaml\" src/"
        status: pass
      - kind: other
        ref: "grep -n \"SystemBackdrop = new MicaBackdrop()\" src/AkariToolbox.App/MainWindow.xaml.cs"
        status: pass
      - kind: other
        ref: "dotnet build AkariToolbox.slnx -c Debug"
        status: pass
    human_judgment: false
  - id: D4
    description: "Elevated manual launch: Akari OS Tweaks page shows exactly 32 rows; 3 spot-checked tweaks (Bluetooth, Print Spooler, Process Mitigation) match `reg query` output; a non-destructive tweak round-trips its real registry value on toggle-on/off; Home (5 cards/1 enabled) and nav (5 entries/2 enabled) render unchanged."
    requirement: "APP-05"
    verification: []
    human_judgment: true
    rationale: "Requires a live elevated Windows session with real registry state and visual confirmation of the rendered UI — not automatable from this worktree execution environment. Deferred to end-of-phase UAT per workflow.human_verify_mode=end-of-phase (config.json); also recorded in .planning/WINDOWS.md as an unrun-verify entry."

duration: 15min
completed: 2026-09-01
status: complete
---

# Phase 1 Plan 07: Full-Catalog Verification & Resilience Polish Summary

**32-handler ordering regression test (xUnit + real DI resolution) plus an AkariOSTweaksViewModel.TryGetStateAsync seam that catches a throwing handler's GetState() and defaults its toggle to false instead of crashing page load.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-09-01T00:26:00Z (approx, base checkout dbdb4c1)
- **Completed:** 2026-09-01T00:40:51Z
- **Tasks:** 2
- **Files modified:** 4 (2 created, 2 modified)

## Accomplishments
- Added `TweakHandlerOrderingTests` (4 tests) proving the fully-assembled 32-handler catalog resolves via the real `AddMvvmFramework()+AddAkariSystemPrimitives()+AddTweakHandlers()` DI graph with exactly 32 handlers, `Order` values spanning `[0..31]` with no gaps/duplicates, and the predecessor's exact key sequence when sorted.
- Extracted `AkariOSTweaksViewModel.TryGetStateAsync` (internal, static) so a throwing `ITweakHandler.GetState()` is caught, logged via `ILogConsoleService` (`"[TWEAK] {key} GetState failed: {message}"`), and the corresponding toggle defaults to `IsOn=false` — closing threat T-01-16 (a single handler failure could previously risk crashing the whole 32-tweak page).
- Re-verified the full-catalog D-03 anti-pattern backstop (no `HasState`/`SaveState`/`ClearState` outside the documented Defender exception), APP-03 (no WPF-UI theme files, `MicaBackdrop` intact), and a clean full-solution `dotnet build`.
- Fixed a false-positive in the D-03 regression grep: a code comment in `DefenderTweakHandler.cs` referenced the predecessor's old `SaveState`/`ClearState` method names for historical context, which literally matched the anti-pattern grep despite containing no actual anti-pattern code — reworded so the grep is a genuinely zero-noise backstop going forward.

## Task Commits

Each task was committed atomically:

1. **Task 1: Order-uniqueness regression test + per-handler error resilience** - `256d31d` (test)
2. **Task 2: Full-catalog D-03/APP-03 verification pass** - `d8862fd` (fix — deviation, see below)

**Plan metadata:** (this commit) `docs(01-07): complete full-catalog verification plan`

## Files Created/Modified
- `src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs` - 4 xUnit tests: 32-handler count, Order [0..31] uniqueness, exact key sequence, throwing-handler resilience
- `src/AkariToolbox.App/AssemblyInfo.cs` - `[assembly: InternalsVisibleTo("AkariToolbox.Tests")]` so the test project can call the new internal seam
- `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs` - Extracted `TryGetStateAsync(ITweakCatalog, ILogConsoleService, ITweakHandler)` as an internal static helper; constructor's per-handler read loop now routes through it
- `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs` - Reworded one comment to remove a literal `SaveState`/`ClearState` string match (no behavior change)

## Decisions Made
- `TryGetStateAsync` is **static**, not an instance method, taking `catalog`/`log`/`handler` as explicit parameters. This lets tests call it directly without ever constructing `AkariOSTweaksViewModel` — whose constructor calls `DispatcherQueue.GetForCurrentThread()`, which throws a `COMException` ("ClassFactory cannot supply requested class") in this headless xunit host rather than returning `null` as it would off a UI thread inside a real running WinUI app. This is an environment-specific WinRT-activation constraint, not a change to production behavior (the constructor still calls it the same way it always did).
- The ordering test's DI-container helper (`BuildProvider()`) re-registers `ILogConsoleService` with a headless-safe `LogConsoleService(dispatcher: null)` *after* calling `AddAkariSystemPrimitives()`, relying on .NET DI's "last registration wins" resolution rule for non-collection services. This is a test-only override — `AddAkariSystemPrimitives()`'s production factory (`DispatcherQueue.GetForCurrentThread()` captured on the real UI thread) is unchanged.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] `DispatcherQueue.GetForCurrentThread()` throws (not null) in the headless test host, blocking the planned instance-method test seam**
- **Found during:** Task 1 (writing the throwing-handler resilience test)
- **Issue:** The plan's suggested approach — extract `TryGetStateAsync` as an internal *instance* method and construct `AkariOSTweaksViewModel` with a fake `ITweakCatalog` to test it — failed because the constructor unconditionally calls `DispatcherQueue.GetForCurrentThread()`, which throws `COMException: ClassFactory cannot supply requested class` in this environment's xunit host (no WinRT activation context), rather than returning `null` as the codebase's existing doc comments assumed for off-UI-thread/headless scenarios.
- **Fix:** Made `TryGetStateAsync` `internal static`, taking `ITweakCatalog`/`ILogConsoleService`/`ITweakHandler` as explicit parameters instead of reading instance fields, so it can be called directly without constructing a `AkariOSTweaksViewModel` instance at all.
- **Files modified:** `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs`, `src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs`
- **Verification:** `dotnet test --filter "FullyQualifiedName~TweakHandlerOrdering"` — 4/4 pass
- **Committed in:** `256d31d` (Task 1 commit)

**2. [Rule 3 - Blocking] `AddAkariSystemPrimitives()`'s `ILogConsoleService` factory also calls `DispatcherQueue.GetForCurrentThread()`, blocking the ordering tests' real-DI-container resolution**
- **Found during:** Task 1 (writing the 32-handler-count/order tests)
- **Issue:** Resolving `IEnumerable<ITweakHandler>` from a container built via `AddMvvmFramework()+AddAkariSystemPrimitives()+AddTweakHandlers()` constructs `DefenderTweakHandler`, which depends on `ILogConsoleService` — whose registered factory calls `DispatcherQueue.GetForCurrentThread()` and throws the same `COMException` in this headless host.
- **Fix:** In the test's own `BuildProvider()` helper, re-registered `ILogConsoleService` with `new LogConsoleService(dispatcher: null)` after the three real registration calls — DI resolves the *last* registration for a non-collection service, so this test-only override does not touch `App.xaml.cs`'s production wiring.
- **Files modified:** `src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs`
- **Verification:** `dotnet test --filter "FullyQualifiedName~TweakHandlerOrdering"` — 4/4 pass
- **Committed in:** `256d31d` (Task 1 commit)

**3. [Rule 1 - Bug/false-positive] D-03 regression grep matched a documentation comment, not actual anti-pattern code**
- **Found during:** Task 2 (running the full-catalog D-03 anti-pattern grep)
- **Issue:** `grep -rn "HasState|SaveState|ClearState" src/AkariToolbox.App/Services/TweakHandlers/ src/AkariToolbox.App/Services/TweakCatalog.cs` matched a comment in `DefenderTweakHandler.cs` that referenced the predecessor's old `SaveState("DisableDefender")`/`ClearState("DisableDefender")` method calls for historical context — not an actual instance of the anti-pattern in this codebase.
- **Fix:** Reworded the comment to describe the same behavior (`DefenderStateValue` flag written/removed inside `SetDefenderAsync`) without the literal `SaveState`/`ClearState` substrings, so the grep — meant as a genuine zero-noise CI backstop per the plan's own framing ("this should not happen ... this is a regression backstop, not expected work") — returns clean.
- **Files modified:** `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs`
- **Verification:** `grep -rn "HasState|SaveState|ClearState" src/AkariToolbox.App/Services/TweakHandlers/ src/AkariToolbox.App/Services/TweakCatalog.cs` returns "clean"
- **Committed in:** `d8862fd` (Task 2 commit)

---

**Total deviations:** 3 auto-fixed (2 blocking, 1 bug/false-positive)
**Impact on plan:** All three were required to actually complete the plan's own specified tests/verify commands in this execution environment. No scope creep — no new features, no architectural changes; TryGetStateAsync's observable behavior (catch, log, default false) is exactly what the plan's `<action>` text specified, only its parameter shape changed from instance-implicit to explicit-static.

## Issues Encountered
- `DispatcherQueue.GetForCurrentThread()` throws rather than returning `null` in this worktree's headless test-runner environment (no WinRT activation context available, unlike a real packaged/unpackaged WinUI 3 app's UI thread). This affected both the ViewModel constructor and the `AddAkariSystemPrimitives()` `ILogConsoleService` factory; both were worked around at the test layer (see Deviations above) without changing production behavior.
- Task 2's elevated manual-launch human-check (32 rows rendering, 3 spot-checked `reg query` matches, Home/nav render, non-destructive toggle round-trip) could not be executed from this automated worktree — no live elevated Windows session is available here. Per `.planning/config.json`'s `workflow.human_verify_mode: end-of-phase`, this is deferred to the phase-level UAT consolidation (the verifier harvests `<verify><human-check>` blocks from PLAN.md at end-of-phase) rather than a mid-flight checkpoint halt. Also recorded in `.planning/WINDOWS.md` (`unrun-verify`, phase 01) for ship-gate visibility.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All 32 `ITweakHandler` registrations are now locked by an automated regression test (`TweakHandlerOrderingTests`) — a future phase accidentally dropping or reordering a handler will fail `dotnet test` immediately rather than silently drifting from the predecessor's sequence.
- `AkariOSTweaksViewModel` is resilient to any single handler's `GetState()` throwing; the other 31 toggles will still render correctly.
- Blocker/concern for ship: the elevated-launch human-check (D4 above) is still open in `.planning/WINDOWS.md` and must be run on a real Windows machine before Phase 1 is considered fully verified end-to-end.

---
*Phase: 01-foundation-akari-os-tweaks*
*Completed: 2026-09-01*

## Self-Check: PASSED

- FOUND: src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs
- FOUND: src/AkariToolbox.App/AssemblyInfo.cs
- FOUND: .planning/phases/01-foundation-akari-os-tweaks/01-07-SUMMARY.md
- FOUND commit: 256d31d
- FOUND commit: d8862fd
