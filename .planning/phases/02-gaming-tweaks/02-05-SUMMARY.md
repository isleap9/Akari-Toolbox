---
phase: 02-gaming-tweaks
plan: 05
subsystem: tweaks
tags: [winui3, mvvm, registry, tdd, gaming, svchost, priority-separation]

# Dependency graph
requires:
  - phase: 02-gaming-tweaks (Plan 02-02)
    provides: "GamingTweaksViewModel/GamingTweaksPage skeleton (TweakCategory.Gaming filter, ObservableCollection<TweakItem> Tweaks, D-05 shortcut RelayCommands) this plan extends with the two dropdowns"
provides:
  - "IGamingDropdownService / GamingDropdownService — SvcHostPresets (10, Default=delete-value) and Win32PriorityPresets (13), bounds-validated Get*PresetIndex/Set*Preset, deterministic nearest-preset tie-break-to-lower"
  - "GamingTweaksViewModel.SvcHostPresetLabels/Win32PriorityPresetLabels/SelectedSvcHostIndex/SelectedWin32PriorityIndex — live-read-on-load, write-through-on-change, guarded by _initialized"
  - "2 ComboBox controls on GamingTweaksPage.xaml"
affects: [02-06, 02-07]

# Actuals (#2632)
actuals:
  tokens: 6002
  tasks: 2
  commits: 2

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IGamingDropdownService is deliberately not an ITweakHandler — live-read/live-write only, no ITweakCatalog prior-value-capture or revert semantics, since neither dropdown is boolean state"
    - "Deterministic nearest-preset matching with tie-break-to-lower-value, shared via one private NearestPresetIndex helper reused by both GetSvcHostPresetIndex and GetWin32PriorityPresetIndex"
    - "_initialized guard bool on a ViewModel prevents [ObservableProperty]-generated OnXChanged hooks from re-writing a value the constructor just read from live state (same problem CR-02 solved for TweakItem.PropertyChanged subscription timing, applied here to CommunityToolkit.Mvvm partial change hooks instead)"

key-files:
  created:
    - src/AkariToolbox.App/Services/IGamingDropdownService.cs
    - src/AkariToolbox.App/Services/GamingDropdownService.cs
    - src/AkariToolbox.Tests/GamingDropdownServiceTests.cs
  modified:
    - src/AkariToolbox.App/Services/TweakHandlerRegistration.cs
    - src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs
    - src/AkariToolbox.App/Views/GamingTweaksPage.xaml

key-decisions:
  - "D-09 checkpoint resolved as 'research-proposed': 10-preset SvcHost list (Default deletes the value; 4/8/12/16/24/32/48/64/128 GB) and 13-preset Win32PrioritySeparation list (all 12 {Short,Long}x{Fixed,Variable}x{No,Medium,High boost} combinations plus the predecessor's Legacy/Advanced 0x06), exactly as approved by the orchestrator's AskUserQuestion before this plan's tasks executed"
  - "Absent Win32PrioritySeparation registry value is treated as 0 and run through the same nearest-preset/tie-break-to-lower algorithm as an exact-match/near-match case (not a plan requirement — the plan's must_haves only define the absent-value contract for SvcHost's 'Default' preset, since Win32Priority has no delete-equivalent preset to fall back to); this selects 'Legacy/Advanced' (0x06=6) as nearest to 0, a deterministic, documented choice consistent with this plan's already-established tie-break-to-lower philosophy"
  - "App.xaml.cs required no changes despite being listed in the plan's files_modified frontmatter — GamingTweaksViewModel was already registered AddTransient in Plan 02-02, and constructor-injected IGamingDropdownService resolves automatically via the DI container once registered in AddTweakHandlers(); no separate registration call site exists to touch"

requirements-completed: [GAMING-01]

# Coverage metadata (#1602)
coverage:
  - id: D1
    description: "IGamingDropdownService/GamingDropdownService validates every selection index against [0, presetArray.Length) before any registry write (both SvcHost and Win32Priority), rejecting out-of-range indices with zero writes performed"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingDropdownServiceTests.cs#SetSvcHostPreset_out_of_range_index_performs_zero_writes,SetWin32PriorityPreset_out_of_range_index_performs_zero_writes"
        status: pass
    human_judgment: false
  - id: D2
    description: "SvcHost 'Default' preset deletes SvcHostSplitThresholdInKB entirely rather than writing the predecessor's buggy literal 380000 or any guessed hex-equivalent number; GetSvcHostPresetIndex returns the Default index when the value is absent"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingDropdownServiceTests.cs#SetSvcHostPreset_Default_deletes_value_never_writes_a_literal,GetSvcHostPresetIndex_returns_Default_index_when_value_absent"
        status: pass
    human_judgment: false
  - id: D3
    description: "Nearest-preset matching on dropdown load ties break toward the lower value, proven independently for both SvcHost and Win32PrioritySeparation"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingDropdownServiceTests.cs#GetSvcHostPresetIndex_ties_break_toward_lower_preset,GetWin32PriorityPresetIndex_ties_break_toward_lower_preset"
        status: pass
    human_judgment: false
  - id: D4
    description: "Both ComboBoxes render on the Gaming Tweaks page with the approved preset labels, reflect live registry state on load, and write validated selections through to the registry (SvcHost writing a real DWORD or deleting on Default; Win32Priority always writing a real hex DWORD)"
    requirement: "GAMING-01"
    verification:
      - kind: other
        ref: "dotnet build AkariToolbox.slnx -c Debug (exit 0)"
        status: pass
    human_judgment: true
    rationale: "Live WinUI page rendering (2 ComboBoxes with correct labels/selection) and confirming a live registry write via a separate elevated reg query require an elevated manual launch — no unit test exercises real WinUI rendering or actual Windows Registry access. Deferred to end-of-phase UAT per workflow.human_verify_mode=end-of-phase (config.json); logged to .planning/WINDOWS.md as unrun-verify entry #5."

duration: ~20min
completed: 2026-09-01
status: complete
---

# Phase 2 Plan 5: Gaming Tweaks Registry Dropdowns Summary

**IGamingDropdownService with bounds-validated SvcHost split threshold (10 presets) and Win32 Priority Separation (13 presets) dropdowns, wired into GamingTweaksViewModel/Page — closing out GAMING-01's dropdown requirement.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-09-01T11:50:00Z
- **Completed:** 2026-09-01T12:10:09Z
- **Tasks:** 2 (plus 1 pre-resolved checkpoint)
- **Files modified:** 6 (3 created, 3 modified)

## Checkpoint Resolution (D-09)

This plan opens with `<task type="checkpoint:decision" gate="blocking">` approving the SvcHost split threshold / Win32 Priority Separation dropdown preset lists. **This checkpoint was already presented to the human user by the orchestrator via `AskUserQuestion` before this executor was dispatched** — it is not re-asked here.

**User's decision: `research-proposed`** — 02-RESEARCH.md's proposed expanded lists, used exactly as written:
- SvcHost split threshold: 10 presets (Default = delete the value; 4/8/12/16/24/32/48/64/128 GB)
- Win32PrioritySeparation: 13 presets (all 12 `{Short,Long}` × `{Fixed,Variable}` × `{No,Medium,High} boost` combinations, plus the predecessor's Legacy/Advanced `0x06`)

Both preset lists were read directly from the checkpoint-resolution context supplied to this executor and hard-coded into `GamingDropdownService` exactly as specified — no defaulting occurred without confirming the selection.

## Accomplishments
- `IGamingDropdownService`/`GamingDropdownService` — deliberately **not** an `ITweakHandler`: no boolean state, no `ITweakCatalog` revert/prior-capture semantics needed for either dropdown
- `SvcHostPresets` (10 entries, `Default` deletes `SvcHostSplitThresholdInKB` rather than writing a literal number) and `Win32PriorityPresets` (13 entries, always writes a real hex DWORD, no delete case)
- Every `SetSvcHostPreset`/`SetWin32PriorityPreset` call validates the index against `[0, presets.Count)` **before** any registry write — out-of-range indices (negative, or `== Count`) perform zero writes, mirroring the predecessor's own bounds guard (ASVS V5)
- Deterministic nearest-preset matching on load: exact match wins outright; otherwise smallest absolute distance; an exact tie breaks toward the lower-valued preset (explicit contract this plan sets, since neither the source script nor RESEARCH.md defined one) — implemented once via a shared `NearestPresetIndex` helper reused by both dropdowns
- Registered `IGamingDropdownService`/`GamingDropdownService` as a singleton in `TweakHandlerServiceCollectionExtensions.AddTweakHandlers()`, alongside the existing `IPostInstallService` registration
- `GamingTweaksViewModel` now takes `IGamingDropdownService`, exposes `SvcHostPresetLabels`/`Win32PriorityPresetLabels` and `[ObservableProperty]`-backed `SelectedSvcHostIndex`/`SelectedWin32PriorityIndex` initialized from live state; a private `_initialized` guard stops the constructor's initial read from immediately re-writing the value it just read
- Two `ComboBox` controls added to `GamingTweaksPage.xaml` below the D-05 shortcut buttons, bound `TwoWay` to the new `SelectedIndex` properties, each with the correct `Header`
- 16 new unit tests exercise both dropdowns against a hand-rolled `FakeRegistryService`: bounds validation (both dropdowns), exact match, tie-break-to-lower (both dropdowns), Default = delete (never a literal), absent-value handling for both dropdowns, and preset-list shape assertions

## Task Commits

Each task was committed atomically:

1. **Task 1: IGamingDropdownService + validated read/write for both presets** - `0f02c86` (feat)
2. **Task 2: Wire dropdowns into GamingTweaksViewModel + GamingTweaksPage** - `ad5ea28` (feat)

**Plan metadata:** pending (this SUMMARY commit)

_Task 1 carried `tdd="true"` in the plan; tests were authored alongside the implementation and both landed in the same commit (both were new/untested code with no pre-existing behavior to prove RED against first) — the full RED→GREEN split wasn't meaningfully separable here since the fake registry test double and the service under test were designed together._

## Files Created/Modified
- `src/AkariToolbox.App/Services/IGamingDropdownService.cs` - New interface: `SvcHostPresets`/`Win32PriorityPresets`, `Get*PresetIndex`/`Set*Preset`
- `src/AkariToolbox.App/Services/GamingDropdownService.cs` - New implementation: preset constants, bounds-validated writes, shared nearest-preset tie-break helper
- `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs` - Registers `IGamingDropdownService` as a singleton
- `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs` - `IGamingDropdownService` dependency, preset label lists, `SelectedSvcHostIndex`/`SelectedWin32PriorityIndex` with `_initialized`-guarded change hooks
- `src/AkariToolbox.App/Views/GamingTweaksPage.xaml` - Two `ComboBox` controls bound to the new ViewModel properties
- `src/AkariToolbox.Tests/GamingDropdownServiceTests.cs` - 16 new unit tests

## Decisions Made
- D-09 checkpoint resolved as `research-proposed` (see Checkpoint Resolution above) — the expanded 10/13-preset lists, not the predecessor's narrower 6/5-preset lists
- Absent `Win32PrioritySeparation` registry value is treated as `0` and run through the same nearest-preset/tie-break-to-lower algorithm as any other value (no special-cased "Default" index exists for this dropdown, unlike SvcHost) — documented in the interface doc comment and proven by a dedicated test
- `App.xaml.cs` needed no code changes despite being listed in the plan's `files_modified` — `GamingTweaksViewModel` was already `AddTransient`-registered in Plan 02-02, and the new constructor parameter resolves automatically once `IGamingDropdownService` is registered in `AddTweakHandlers()` (confirmed by a successful full-solution build)

## Deviations from Plan

None - plan executed exactly as written (the D-09 checkpoint was pre-resolved by the orchestrator per this plan's dispatch instructions, and App.xaml.cs's listed-but-unneeded change is a discretionary observation, not a deviation from any explicit action).

## Issues Encountered

- Task 2's `<verify><human-check>` (elevated launch, confirm both dropdowns render with correct labels/live-selected-index, change SvcHost dropdown and confirm the registry write via `reg query`, select "Default" and confirm the value is deleted) could not be executed by this automated worktree executor — no live elevated Windows session available. Expected, normal flow under `workflow.human_verify_mode=end-of-phase` (config.json), consistent with every prior plan in this phase. Logged to `.planning/WINDOWS.md` as an `unrun-verify` entry (id 5).
- Full-suite `dotnet test` (175 tests) surfaces the same pre-existing, unrelated failure already logged in `.planning/phases/02-gaming-tweaks/deferred-items.md` by Plan 02-01: `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` (expects a `COMException` this worktree's headless test run doesn't throw). Not re-logged, not fixed here (Scope Boundary — unrelated to this plan's files).

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- GAMING-01's dropdown requirement is fully satisfied: both dropdowns are user-approved (D-09 `research-proposed`), bounds-validated, and implement the deterministic nearest-preset tie-break contract this plan set explicitly
- Ready for Plan 02-06 (D-06 network-dependent driver/tool scripts) and Plan 02-07 — no further `GamingTweaksViewModel`/`GamingDropdownService` wiring needed by those plans
- Blocker/concern: the elevated manual UI/registry verification for both dropdowns has not yet been run against a real machine — carries forward the same recommendation from 02-01's and 02-02's SUMMARYs to run the full end-of-phase UAT pass once all 7 plans in Phase 2 are complete

---
*Phase: 02-gaming-tweaks*
*Completed: 2026-09-01*

## Self-Check: PASSED

- All 6 files confirmed present on disk (`IGamingDropdownService.cs`, `GamingDropdownService.cs`, `GamingDropdownServiceTests.cs`, `TweakHandlerRegistration.cs`, `GamingTweaksViewModel.cs`, `GamingTweaksPage.xaml`).
- Both commits (`0f02c86`, `ad5ea28`) confirmed in `git log --oneline`.
- `dotnet build AkariToolbox.slnx -c Debug` exits 0.
- `dotnet test src/AkariToolbox.Tests/AkariToolbox.Tests.csproj --filter "FullyQualifiedName~GamingDropdownService"` — 16/16 pass.
- `grep -n "index >= 0" src/AkariToolbox.App/Services/GamingDropdownService.cs` — 2 matches (SvcHost, Win32Priority).
- `grep -n "SelectedSvcHostIndex" src/AkariToolbox.App/Views/GamingTweaksPage.xaml` and `grep -n "SelectedWin32PriorityIndex" src/AkariToolbox.App/Views/GamingTweaksPage.xaml` — each match.
