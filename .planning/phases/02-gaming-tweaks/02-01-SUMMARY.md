---
phase: 02-gaming-tweaks
plan: 01
subsystem: tweaks
tags: [winui3, mvvm, registry, tdd, powershell]

# Dependency graph
requires:
  - phase: 01-foundation-akari-os-tweaks
    provides: "ITweakHandler/ITweakCatalog live-state pattern, IRegistryService, IScriptRunner, AkariOSTweaksViewModel/Page precedent"
provides:
  - "TweakCategory discriminator (AkariOS, Gaming) on ITweakHandler, retrofitted across all 32 Phase 1 handlers"
  - "IRegistryService.GetSubKeyNames(RegistryHive, string) — GPU/network-adapter-class subkey enumeration"
  - "IScriptRunner.RunEmbeddedScriptAsync — generalized embedded-script primitive for Plan 02-06's network scripts"
  - "GamingTweaksViewModel/GamingTweaksPage — catalog-driven, auto-renders every future Gaming handler with zero further changes"
  - "HdcpTweakHandler, P0StateTweakHandler, MsiModeTweakHandler (Order 100/101/102) — first 3 of 11 Gaming toggle handlers"
  - "GpuAdapterEnumeration.GetGpuAdapterSubKeys shared helper — standardized ^\\d{4}$ filter for GPU-class-GUID subkey enumeration"
affects: [02-02, 02-03, 02-04, 02-05, 02-06, 02-07]

# Actuals (#2632)
actuals:
  tokens: 13688
  tasks: 3
  commits: 5

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "TweakCategory discriminator filters one flat ITweakCatalog.Handlers list per page (AkariOSTweaksViewModel/GamingTweaksViewModel .Where(h => h.Category == ...)) instead of restructuring the catalog interface"
    - "Gaming handler Order values live in a disjoint 100+ range so the flat DI-wide handler list stays collision-free with the AkariOS 0-31 range"
    - "GPU adapter enumeration standardized on IRegistryService.GetSubKeyNames + a shared ^\\d{4}$ regex filter, reused by every GPU-class-GUID-enumerating handler"
    - "RunEmbeddedScriptAsync generalizes DefenderTweakHandler's private ExtractEmbeddedAsync into a shared IScriptRunner primitive (GUID-suffixed temp file, finally-block cleanup)"

key-files:
  created:
    - src/AkariToolbox.App/Services/TweakCategory.cs
    - src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs
    - src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs
    - src/AkariToolbox.App/Views/GamingTweaksPage.xaml
    - src/AkariToolbox.App/Views/GamingTweaksPage.xaml.cs
    - src/AkariToolbox.Framework/Fixtures/exit7.ps1
    - src/AkariToolbox.Tests/ScriptRunnerEmbeddedTests.cs
    - src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs
    - .planning/phases/02-gaming-tweaks/deferred-items.md
  modified:
    - src/AkariToolbox.App/Services/ITweakHandler.cs
    - src/AkariToolbox.App/Services/TweakHandlers/WifiTweakHandler.cs
    - src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchA.cs
    - src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchB.cs
    - src/AkariToolbox.App/Services/TweakHandlers/BcdeditDismTweaks.cs
    - src/AkariToolbox.App/Services/TweakHandlers/ServiceBackedTweaks.cs
    - src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs
    - src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs
    - src/AkariToolbox.App/ViewModels/HomeViewModel.cs
    - src/AkariToolbox.App/MainWindow.xaml.cs
    - src/AkariToolbox.App/App.xaml.cs
    - src/AkariToolbox.Framework/Services/IRegistryService.cs
    - src/AkariToolbox.Framework/Services/RegistryService.cs
    - src/AkariToolbox.Framework/Services/IScriptRunner.cs
    - src/AkariToolbox.Framework/Services/ScriptRunner.cs
    - src/AkariToolbox.Framework/AkariToolbox.Framework.csproj
    - src/AkariToolbox.Tests/TweakCatalogTests.cs
    - src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs

key-decisions:
  - "TweakCategory added as a minimal-additive interface member (not a catalog restructure) — ITweakCatalog.Handlers stays one flat list; only the two ViewModels filter, per RESEARCH.md Pattern 1"
  - "Gaming handler Order values start at 100 (disjoint from AkariOS's 0-31) so the flat DI-registered handler list never collides across categories"
  - "exit7.ps1 test fixture embedded in AkariToolbox.Framework (not AkariToolbox.Tests) so ScriptRunnerEmbeddedTests exercises RunEmbeddedScriptAsync's real typeof(ScriptRunner).Assembly resolution contract instead of a mocked one"
  - "MsiModeTweakHandler enumerates GPU InstanceIds via a non-interactive powershell.exe -Command \"(Get-PnpDevice -Class Display).InstanceId\" spawn through IScriptRunner rather than adding a System.Management/WMI dependency, per RESEARCH.md Pattern 4 and the CLAUDE.md no-in-process-PowerShell constraint"

patterns-established:
  - "Pattern: TweakCategory-scoped ViewModel filtering — every future category (Debloat, Misc) adds a new enum value + one ViewModel .Where() clause, no catalog interface change"
  - "Pattern: GPU/network-adapter-class subkey enumeration always goes through IRegistryService.GetSubKeyNames + the shared ^\\d{4}$ regex, never a bespoke per-handler heuristic"

requirements-completed: [GAMING-01]

coverage:
  - id: D1
    description: "TweakCategory discriminator lands on ITweakHandler and all 32 existing Phase 1 handlers without changing their GetState/SetState behavior; AkariOS-scoped ordering tests still assert exactly 32 handlers at Order [0..31] in the predecessor's exact key sequence"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs#Resolving_ITweakHandler_yields_exactly_32_handlers,Handler_order_values_span_0_to_31_with_no_gaps_or_duplicates,Handlers_sorted_by_order_match_predecessors_exact_key_sequence"
        status: pass
    human_judgment: false
  - id: D2
    description: "Gaming Tweaks page is reachable from the Home card and nav sidebar entry, showing a real, revertible GPU HDCP Override toggle (HdcpTweakHandler) that reads/writes the live RMHdcpKeyglobZero registry value across every detected GPU adapter"
    requirement: "GAMING-01"
    verification: []
    human_judgment: true
    rationale: "HdcpTweakHandler's live-registry read/write correctness and the Gaming Tweaks page's actual WinUI rendering require an elevated manual launch + reg query verification (this plan's Task 1 human-check) — no unit test exercises real registry state or WinUI page rendering. Deferred to end-of-phase UAT per workflow.human_verify_mode=end-of-phase (config.json); logged to .planning/WINDOWS.md as unrun-verify entry #3."
  - id: D3
    description: "IScriptRunner.RunEmbeddedScriptAsync extracts a named embedded resource to a GUID-suffixed temp file, runs it via powershell.exe, propagates the real exit code, and deletes the temp file in a finally block on both success and failure paths"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/ScriptRunnerEmbeddedTests.cs#RunEmbeddedScriptAsync_runs_fixture_and_returns_real_exit_code,RunEmbeddedScriptAsync_deletes_temp_file_after_completion,RunEmbeddedScriptAsync_missing_resource_throws_FileNotFoundException"
        status: pass
    human_judgment: false
  - id: D4
    description: "P0StateTweakHandler and MsiModeTweakHandler (Order 101/102, Category Gaming) compile, pass unit tests against fake IRegistryService/IScriptRunner doubles, and render on the Gaming Tweaks page automatically (catalog-driven, no ViewModel change) alongside Hdcp"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs (9 tests)"
        status: pass
    human_judgment: false
---

# Phase 2 Plan 1: TweakCategory Discriminator + Gaming Tweaks Vertical Slice Summary

**TweakCategory discriminator retrofitted across all 32 Phase 1 handlers, generalized RunEmbeddedScriptAsync primitive, and 3 real Gaming-category toggle handlers (Hdcp, P0State, MsiMode) proving the catalog-partitioned architecture end-to-end on a new, catalog-driven Gaming Tweaks page.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-09-01T10:43:00Z
- **Completed:** 2026-09-01T10:58:17Z
- **Tasks:** 3
- **Files modified:** 26

## Accomplishments
- Added `TweakCategory` enum (`AkariOS`, `Gaming`) and `ITweakHandler.Category`, retrofitted onto all 32 existing Phase 1 handlers (one line each, zero behavior change) — proven by the AkariOS-scoped `TweakHandlerOrderingTests` still asserting exactly 32/`[0..31]`/predecessor key sequence
- Built the first Gaming Tweaks vertical slice: `HdcpTweakHandler` (Order 100) reads/writes the real `RMHdcpKeyglobZero` value across every detected GPU adapter, `GamingTweaksViewModel`/`GamingTweaksPage` mirror the Phase 1 pattern exactly and are catalog-driven (no future ViewModel changes needed as more Gaming handlers land)
- Generalized `DefenderTweakHandler.ExtractEmbeddedAsync`'s private extract-and-run logic into a shared `IScriptRunner.RunEmbeddedScriptAsync` primitive, TDD'd against a real embedded fixture (`exit7.ps1`) exercising the actual `typeof(ScriptRunner).Assembly` resolution path
- Added `P0StateTweakHandler` and `MsiModeTweakHandler` (Order 101/102), the latter proving the "no in-repo PnP enumeration primitive" gap gets solved via a non-interactive `Get-PnpDevice` process spawn through `IScriptRunner`, not a new WMI dependency
- Flipped the Home dashboard's Gaming Tweaks card and the nav sidebar entry from disabled placeholders to `GamingTweaksPage`, matching the enabled `AkariOSTweaksPage` precedent

## Task Commits

Each task was committed atomically (Tasks 2 and 3 followed the RED→GREEN TDD cycle):

1. **Task 1: TweakCategory discriminator + Hdcp toggle end-to-end** - `554bbe3` (feat)
2. **Task 2: IScriptRunner.RunEmbeddedScriptAsync** - `71a8ce2` (test, RED) → `d45dd60` (feat, GREEN)
3. **Task 3: P0State + MsiMode toggle handlers** - `781f1a8` (test, RED) → `e641067` (feat, GREEN)

**Plan metadata:** pending (this SUMMARY commit)

_No REFACTOR commits were needed — both GREEN implementations were minimal and clean on first pass._

## Files Created/Modified
- `src/AkariToolbox.App/Services/TweakCategory.cs` - New `TweakCategory` enum (AkariOS, Gaming)
- `src/AkariToolbox.App/Services/ITweakHandler.cs` - Added `Category` member to the interface
- `src/AkariToolbox.App/Services/TweakHandlers/{WifiTweakHandler,RegistryTweaksBatchA,RegistryTweaksBatchB,BcdeditDismTweaks,ServiceBackedTweaks,DefenderTweakHandler}.cs` - Added `Category => TweakCategory.AkariOS` to all 32 existing handlers
- `src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs` - New: `GpuAdapterEnumeration` shared helper, `HdcpTweakHandler`, `P0StateTweakHandler`, `MsiModeTweakHandler`
- `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs` - New, mirrors `AkariOSTweaksViewModel` filtered to `TweakCategory.Gaming`
- `src/AkariToolbox.App/Views/GamingTweaksPage.xaml(.cs)` - New, mirrors `AkariOSTweaksPage`
- `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs` - Filters `_catalog.Handlers` to `Category == AkariOS`
- `src/AkariToolbox.App/ViewModels/HomeViewModel.cs` - Gaming Tweaks card `IsEnabled: false → true`, `Target: GamingTweaksPage`
- `src/AkariToolbox.App/MainWindow.xaml.cs` - Gaming Tweaks nav entry enabled, `Target: GamingTweaksPage`
- `src/AkariToolbox.App/App.xaml.cs` - Registered `GamingTweaksViewModel` in DI
- `src/AkariToolbox.Framework/Services/IRegistryService.cs`/`RegistryService.cs` - Added `GetSubKeyNames(RegistryHive, string)`
- `src/AkariToolbox.Framework/Services/IScriptRunner.cs`/`ScriptRunner.cs` - Added `RunEmbeddedScriptAsync`
- `src/AkariToolbox.Framework/Fixtures/exit7.ps1` + `.csproj` - Embedded test fixture for `RunEmbeddedScriptAsync`
- `src/AkariToolbox.Tests/{TweakCatalogTests,TweakHandlerOrderingTests}.cs` - `Category` on test doubles; ordering tests scoped to `Category == AkariOS`
- `src/AkariToolbox.Tests/{ScriptRunnerEmbeddedTests,GamingGraphicsTweaksTests}.cs` - New test suites (3 + 9 tests)
- `.planning/phases/02-gaming-tweaks/deferred-items.md` - New: logs a pre-existing, unrelated `ConvertersTests` failure found during full-suite verification

## Decisions Made
- `TweakCategory` is an additive interface member, not a catalog restructure — `ITweakCatalog.Handlers` stays one flat list; only the two ViewModels filter (RESEARCH.md Pattern 1, explicitly avoiding the "no catalog-plumbing changes needed" false assumption in 02-CONTEXT.md)
- Gaming handler `Order` values start at 100, a disjoint range from AkariOS's 0-31, so the flat DI-registered handler list never collides across categories
- `exit7.ps1` fixture embedded in `AkariToolbox.Framework` (not `AkariToolbox.Tests`) so the test exercises `RunEmbeddedScriptAsync`'s real `typeof(ScriptRunner).Assembly` resolution contract, not a mocked one
- `MsiModeTweakHandler` enumerates GPU `InstanceId`s via a non-interactive `powershell.exe -Command "(Get-PnpDevice -Class Display).InstanceId"` spawn through `IScriptRunner`, avoiding a new `System.Management`/WMI dependency per CLAUDE.md's no-in-process-PowerShell-hosting constraint

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Doc comments violated the plan's own `ControlSet001` grep acceptance criterion**
- **Found during:** Task 3 (P0State + MsiMode GREEN implementation, before final verification)
- **Issue:** Explanatory doc comments on `MsiModeTweakHandler` and its `BuildMsiPath` helper quoted the literal string `ControlSet001` (to explain the deliberate deviation from the source script) — this satisfied the intent but violated the plan's own automated acceptance check (`grep -n "ControlSet001" ... GamingGraphicsTweaks.cs` must return no matches)
- **Fix:** Reworded the three comments to say "hardcoded legacy control-set number" instead of quoting the literal string, preserving the explanation without the banned literal
- **Files modified:** `src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs`
- **Verification:** `grep -n "ControlSet001" src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs` returns no matches (exit 1)
- **Committed in:** `e641067` (Task 3 GREEN commit)

---

**Total deviations:** 1 auto-fixed (1 bug, self-caught before commit landed in final form)
**Impact on plan:** Cosmetic-only — no behavior change, purely a doc-comment wording fix to satisfy the plan's own acceptance criterion.

## Issues Encountered
- Full-suite `dotnet test` (130 tests) surfaced 1 pre-existing failure unrelated to this plan: `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` expects a `COMException` from `DispatcherQueue.GetForCurrentThread()` that this worktree's headless test run did not throw. Confirmed via `git log` that `ConvertersTests.cs` was last touched in Phase 1 commit `61efb3f` — not modified by any 02-01 commit. Not fixed here (Scope Boundary: only auto-fix issues directly caused by this task's changes) — logged to `.planning/phases/02-gaming-tweaks/deferred-items.md`.
- Task 1's `<verify><human-check>` (elevated launch, click through Home/nav to Gaming Tweaks, toggle Hdcp, confirm via `reg query`) could not be executed by this automated worktree executor — no live elevated Windows session available. This is expected, normal flow under `workflow.human_verify_mode=end-of-phase` (config.json): the check is deferred to end-of-phase UAT rather than a mid-flight `checkpoint:human-verify`. Logged to `.planning/WINDOWS.md` as an `unrun-verify` entry (id 3) so it stays visible through to `/gsd-ship`.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- The category-partitioned catalog architecture is proven: `TweakCategory` filters cleanly, all 32 AkariOS handlers are unaffected, and 3 Gaming handlers register/render correctly on the new page
- `GetGpuAdapterSubKeys`/`GpuAdapterEnumeration` is ready for reuse by Plan 02-02's `AmdSettingsTweakHandler`/`IntelSettingsTweakHandler` (same GPU-class-GUID enumeration need)
- `IScriptRunner.RunEmbeddedScriptAsync` is proven independently and ready for Plan 02-06's 6 network-dependent D-06 scripts
- Blocker/concern: the elevated manual UI/registry verification for Hdcp (and, by extension, every future live-registry Gaming handler) has not yet been run against a real machine in this phase — recommend running the full end-of-phase UAT pass (human_verify_mode=end-of-phase) once all 7 plans in Phase 2 are complete, rather than deferring it further

---
*Phase: 02-gaming-tweaks*
*Completed: 2026-09-01*
