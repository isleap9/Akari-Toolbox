---
phase: 02-gaming-tweaks
plan: 02
subsystem: tweaks
tags: [winui3, mvvm, registry, tdd, gaming, amd, intel]

# Dependency graph
requires:
  - phase: 02-gaming-tweaks (Plan 02-01)
    provides: "TweakCategory discriminator, GamingTweaksViewModel/Page skeleton, GpuAdapterEnumeration.GetGpuAdapterSubKeys shared helper (Order 100-102: Hdcp, P0State, MsiMode)"
provides:
  - "AmdSettingsTweakHandler (Order 103) — 20-value fixed HKCU\\Software\\AMD\\{CN,AIM,DVR} + per-adapter registry handler"
  - "IntelSettingsTweakHandler (Order 104) — 3DKeys subkey create/delete Intel graphics handler"
  - "RegistryBinaryHelpers.HexStringToBytes — shared REG_BINARY hex-string conversion helper"
  - "IRegistryService.DeleteSubKeyTree / CreateSubKey — new registry-squatting-safe whole-subtree delete/recreate primitives"
  - "GamingTweaksViewModel.OpenDisplaySettingsCommand / OpenAdvancedGraphicsSettingsCommand — D-05 one-shot ms-settings: shortcuts"
affects: [02-03, 02-04, 02-05, 02-06, 02-07]

# Actuals (#2632)
actuals:
  tokens: 8169
  tasks: 3
  commits: 5

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IRegistryService.DeleteSubKeyTree/CreateSubKey extend the registry-squatting-safe convention (never blind-create, open-then-create/delete-only-if-present) to whole-subtree delete/recreate operations, reused by both AmdSettingsTweakHandler (Notification subkey) and IntelSettingsTweakHandler (3DKeys subkey)"
    - "RegistryBinaryHelpers.HexStringToBytes is the single shared REG_BINARY hex-string-to-byte[] conversion (pairs of hex chars -> one byte each, matching reg.exe's own parsing), avoiding scattered manual byte-array literals across every REG_BINARY value in AMD Settings' table"

key-files:
  created: []
  modified:
    - src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs
    - src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs
    - src/AkariToolbox.App/Views/GamingTweaksPage.xaml
    - src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs
    - src/AkariToolbox.Framework/Services/IRegistryService.cs
    - src/AkariToolbox.Framework/Services/RegistryService.cs

key-decisions:
  - "Added IRegistryService.DeleteSubKeyTree/CreateSubKey to the Framework layer even though not listed in the plan's files_modified frontmatter — required by both AMD (Notification delete-then-recreate-empty, 5 other subkey deletes) and Intel (3DKeys whole-subkey delete) handlers; matches the plan's own Task 2 contingency language ('if IRegistryService has no DeleteSubKeyTree-equivalent, add one') and the plan's Artifacts section, which already anticipated this addition"
  - "AmdSettingsTweakHandler and IntelSettingsTweakHandler both target CurrentControlSet uniformly, never the source scripts' hardcoded ControlSet001, per RESEARCH.md Pitfall 1 and this plan's own grep acceptance criterion"
  - "RadeonSoftware.exe restart (AMD Settings' best-effort UX side effect) is implemented as a private static helper wrapped in try/catch with no injected logger — the plan's constructor signature (single IRegistryService param) precluded adding a logging dependency; a code comment documents the best-effort intent instead"

patterns-established:
  - "Pattern: whole-subtree registry delete/recreate (DeleteSubKeyTree/CreateSubKey) for handlers whose On/Off branches operate at the subkey level rather than the individual-value level — first proven by AmdSettingsTweakHandler's Notification subkey and IntelSettingsTweakHandler's 3DKeys subkey"

requirements-completed: [GAMING-01]

# Coverage metadata (#1602)
coverage:
  - id: D1
    description: "AmdSettingsTweakHandler.SetState(true)/(false) writes/reverses all 10 fixed HKCU\\Software\\AMD\\{CN,AIM,DVR} values and 7 per-adapter registry paths exactly per RESEARCH.md's AMD Settings full value list, targeting CurrentControlSet throughout"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs#AmdSettings_SetState_true_writes_all_10_fixed_HKCU_values,AmdSettings_SetState_false_reverses_fixed_values_per_documented_off_behavior,AmdSettings_SetState_true_writes_per_adapter_UMD_values_via_HexStringToBytes,AmdSettings_SetState_false_writes_per_adapter_UMD_off_values_and_deletes_TFQ,AmdSettings_SetState_true_deletes_then_recreates_Notification_subkey_empty,AmdSettings_SetState_false_deletes_CustomResolutions_DisplayOverride_Notification_and_AlreadyNotified_subkeys,AmdSettings_GetState_returns_true_only_when_AutoUpdate_equals_0,AmdSettings_GetState_returns_false_when_AutoUpdate_is_absent_or_nonzero,AmdSettings_metadata_is_Order_103_Category_Gaming,HexStringToBytes_converts_hex_pairs_to_bytes"
        status: pass
    human_judgment: false
  - id: D2
    description: "IntelSettingsTweakHandler.SetState(true) creates a 3DKeys subkey (Global_AsyncFlipMode=2, Global_LowLatency=0) per adapter; SetState(false) deletes the entire 3DKeys subkey per adapter — matching 6 Intel Settings.ps1's own asymmetric create-vs-delete-subkey shape"
    requirement: "GAMING-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs#IntelSettings_SetState_true_writes_AsyncFlipMode_2_and_LowLatency_0_under_3DKeys_per_adapter,IntelSettings_SetState_false_deletes_entire_3DKeys_subkey_per_adapter,IntelSettings_GetState_returns_true_only_when_every_adapter_has_AsyncFlipMode_2,IntelSettings_GetState_returns_false_when_3DKeys_subkey_absent_for_one_adapter,IntelSettings_metadata_is_Order_104_Category_Gaming"
        status: pass
    human_judgment: false
  - id: D3
    description: "The 5 Graphics folder's 5 stateful toggles (Hdcp, P0State, MsiMode, AmdSettings, IntelSettings) all render on the Gaming Tweaks page automatically via the TweakCategory filter, plus 2 working D-05 shortcut buttons (Display Settings, Advanced Graphics Settings) below the toggle list, launching the correct ms-settings: sub-page"
    requirement: "GAMING-01"
    verification:
      - kind: other
        ref: "dotnet build AkariToolbox.slnx -c Debug (exit 0)"
        status: pass
    human_judgment: true
    rationale: "Live WinUI page rendering (5 toggles + 2 buttons visible) and clicking each shortcut button to confirm it opens the correct ms-settings sub-page require an elevated manual launch — no unit test exercises real WinUI rendering or a live Process.Start against the Windows Settings app. Deferred to end-of-phase UAT per workflow.human_verify_mode=end-of-phase (config.json); logged to .planning/WINDOWS.md as unrun-verify entry #4."

duration: ~20min
completed: 2026-09-01
status: complete
---

# Phase 2 Plan 2: AMD/Intel Graphics Handlers + D-05 Shortcuts Summary

**Completes the `5 Graphics` folder's 5-toggle set with AmdSettingsTweakHandler (20-value fixed+per-adapter registry write) and IntelSettingsTweakHandler (3DKeys subkey create/delete), plus two D-05 one-shot `ms-settings:` shortcut buttons.**

## Performance

- **Duration:** ~20 min
- **Started:** 2026-09-01T10:58:36Z
- **Completed:** 2026-09-01T11:18:00Z
- **Tasks:** 3
- **Files modified:** 6

## Accomplishments
- `AmdSettingsTweakHandler` (Order 103): writes/reverses all 10 fixed `HKCU\Software\AMD\{CN,AIM,DVR}` values plus 7 per-adapter registry paths (`UMD`, `power_v1`, direct adapter values) under the shared GPU Display class GUID, exactly per RESEARCH.md's full value table — including the `IsAutoDefault` type-change quirk (BINARY on, DWORD off) and the delete-then-recreate-empty `Notification` subkey behavior, all preserved as authored
- `IntelSettingsTweakHandler` (Order 104): proves the source script's asymmetric create-subkey-on / delete-subkey-off shape — `3DKeys` is created fresh per adapter on enable, deleted wholesale (not value-by-value) on disable
- `RegistryBinaryHelpers.HexStringToBytes` shared helper converts every REG_BINARY value in the AMD table from its hex-string form, matching how `reg.exe /t REG_BINARY /d "..."` itself parses the argument — proven via a dedicated unit test and reused across 7 per-adapter values
- Extended `IRegistryService` with `DeleteSubKeyTree`/`CreateSubKey`, both registry-squatting-safe (never throws on missing, never blind-overwrites) — the shared prerequisite both new handlers needed for whole-subkey delete/recreate operations that `GetValue`/`SetValue`/`DeleteValue` alone couldn't express
- Added the two D-05 one-shot shortcuts (`OpenDisplaySettingsCommand`/`OpenAdvancedGraphicsSettingsCommand`) as plain `[RelayCommand]`s on `GamingTweaksViewModel`, launching `ms-settings:display`/`ms-settings:display-advancedgraphics` via `Process.Start(UseShellExecute: true)` — not registered as `ITweakHandler`s, matching the source scripts' menu-less, state-less, single-line shape
- `5 Graphics`'s full D-04/D-05 scope is now live: 5 stateful toggles (Hdcp, P0State, MsiMode from Plan 02-01; AmdSettings, IntelSettings from this plan) + 2 shortcut buttons, all catalog-driven with zero further `GamingTweaksViewModel` wiring needed

## Task Commits

Each task was committed atomically (Tasks 1 and 2 followed the RED→GREEN TDD cycle):

1. **Task 1: AmdSettingsTweakHandler — 20-value fixed + per-adapter registry handler** - `d07f440` (test, RED) → `6925674` (feat, GREEN)
2. **Task 2: IntelSettingsTweakHandler — 3DKeys subkey create/delete** - `002a498` (test, RED) → `39d5b5f` (feat, GREEN)
3. **Task 3: D-05 one-shot display-settings shortcuts** - `93be3f1` (feat)

**Plan metadata:** pending (this SUMMARY commit)

_No REFACTOR commits were needed — both GREEN implementations were minimal and clean on first pass, aside from one self-caught test-double bug fixed within the same GREEN commit (see Deviations)._

## Files Created/Modified
- `src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs` - Added `RegistryBinaryHelpers.HexStringToBytes`, `AmdSettingsTweakHandler`, `IntelSettingsTweakHandler`
- `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs` - Added `OpenDisplaySettingsCommand`/`OpenAdvancedGraphicsSettingsCommand` `[RelayCommand]`s
- `src/AkariToolbox.App/Views/GamingTweaksPage.xaml` - Added the two shortcut buttons below the toggle list
- `src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs` - Extended `FakeRegistryService` with kind-tracking, `DeleteSubKeyTree`/`CreateSubKey` support; added 15 new tests (10 AMD, 5 Intel)
- `src/AkariToolbox.Framework/Services/IRegistryService.cs` - Added `DeleteSubKeyTree`/`CreateSubKey` members
- `src/AkariToolbox.Framework/Services/RegistryService.cs` - Implemented `DeleteSubKeyTree`/`CreateSubKey`

## Decisions Made
- `IRegistryService.DeleteSubKeyTree`/`CreateSubKey` were added even though the plan's `files_modified` frontmatter didn't list the Framework files — both new handlers structurally require whole-subtree delete/recreate operations that the existing `GetValue`/`SetValue`/`DeleteValue` trio can't express; the plan's own Task 2 action text and Artifacts section both anticipated this addition explicitly
- Both handlers use `CurrentControlSet` uniformly, never the source scripts' hardcoded `ControlSet001`, consistent with every existing Phase 1/Plan 02-01 handler's convention
- `RadeonSoftware.exe` restart (AMD's best-effort UX side effect) has no injected logger — the plan's constructor signature is a single `IRegistryService` param — so it's documented via code comment rather than a formal log line; the try/catch still guarantees it never fails `SetState`

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Comment wording collided with the plan's own UseShellExecute grep acceptance criterion**
- **Found during:** Task 3 (D-05 shortcuts), before final verification
- **Issue:** The explanatory code comment above the two `[RelayCommand]` methods quoted the literal string `UseShellExecute = true` twice, which made `grep -c "UseShellExecute = true" GamingTweaksViewModel.cs` return `4` instead of the plan's required `2` (the acceptance criterion counts actual code occurrences, not comment mentions)
- **Fix:** Reworded the comment to describe the `ProcessStartInfo` shape without repeating the literal string, preserving the DefenderTweakHandler-precedent explanation
- **Files modified:** `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs`
- **Verification:** `grep -c "UseShellExecute = true" src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs` returns `2`
- **Committed in:** `93be3f1` (Task 3 commit)

**2. [Rule 1 - Bug] Test double's DeleteSubKeyTree/CreateSubKey cleared each other's historical flag**
- **Found during:** Task 1 GREEN verification (`AmdSettings_SetState_true_deletes_then_recreates_Notification_subkey_empty` failed on first run)
- **Issue:** `FakeRegistryService.CreateSubKey` removed the path from `_deletedSubKeyTrees` and `DeleteSubKeyTree` removed it from `_createdSubKeys`, so a delete-then-recreate sequence (AMD's `Notification` On branch) left `WasSubKeyTreeDeleted` reporting `false` even though `DeleteSubKeyTree` had genuinely been called first
- **Fix:** Made both flags independent historical facts (append-only sets) rather than mutually-clearing state, so tests can assert "deleted, then recreated" as two separate occurrences
- **Files modified:** `src/AkariToolbox.Tests/GamingGraphicsTweaksTests.cs`
- **Verification:** All 9 AmdSettings tests pass; full `GamingGraphicsTweaksTests` suite (23 tests) passes
- **Committed in:** `6925674` (Task 1 GREEN commit)

---

**Total deviations:** 2 auto-fixed (1 blocking acceptance-criterion collision, 1 test-double bug)
**Impact on plan:** Both fixes are test/comment-only — no production registry-write behavior changed. No scope creep.

## Issues Encountered
- Full-suite `dotnet test` (145 tests) surfaces the same pre-existing, unrelated failure already logged by Plan 02-01: `AkariToolbox.Tests.ConvertersTests.EnumToBoolean_matches_parameter` (expects a `COMException` this worktree's headless test run doesn't throw). Already documented in `.planning/phases/02-gaming-tweaks/deferred-items.md` — not re-logged, not fixed here (Scope Boundary).
- Task 3's `<verify><human-check>` (elevated launch, confirm 5 toggles + 2 buttons render, click both shortcut buttons to confirm the correct Settings sub-page opens) could not be executed by this automated worktree executor — no live elevated Windows session available. Expected, normal flow under `workflow.human_verify_mode=end-of-phase` (config.json): deferred to end-of-phase UAT rather than a mid-flight `checkpoint:human-verify`. Logged to `.planning/WINDOWS.md` as an `unrun-verify` entry (id 4).

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- `5 Graphics`'s full D-04/D-05 scope (5 stateful toggles + 2 shortcuts) is now live on the Gaming Tweaks page — ready for the `6 Windows` toggle subset (Plan 02-03) and the D-06 network-dependent scripts (Plan 02-06)
- `IRegistryService.DeleteSubKeyTree`/`CreateSubKey` are proven independently (9+5 unit tests) and available for any future handler needing whole-subtree registry operations
- Blocker/concern: the elevated manual UI/registry verification for AmdSettings/IntelSettings and the two D-05 shortcut buttons has not yet been run against a real machine — carries forward the same recommendation from 02-01's SUMMARY to run the full end-of-phase UAT pass once all 7 plans in Phase 2 are complete

---
*Phase: 02-gaming-tweaks*
*Completed: 2026-09-01*

## Self-Check: PASSED

- All 6 modified files confirmed present on disk.
- All 5 commits (`d07f440`, `6925674`, `002a498`, `39d5b5f`, `93be3f1`) confirmed in `git log`.
- `dotnet build AkariToolbox.slnx -c Debug` exits 0.
- `dotnet test src/AkariToolbox.Tests/AkariToolbox.Tests.csproj --filter "FullyQualifiedName~AmdSettings|FullyQualifiedName~IntelSettings|FullyQualifiedName~P0State|FullyQualifiedName~MsiMode"` — 23/23 pass.
- `grep -n "ControlSet001" src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs` — no matches.
- `grep -c "UseShellExecute = true" src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs` — returns `2`.
