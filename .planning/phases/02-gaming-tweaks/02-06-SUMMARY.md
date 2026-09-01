---
phase: 02-gaming-tweaks
plan: 06
subsystem: ui
tags: [winui3, powershell, embedded-resources, gaming-tweaks, driver-install, nvidia, amd, intel]

# Dependency graph
requires:
  - phase: 02-01
    provides: IScriptRunner.RunEmbeddedScriptAsync primitive (embedded .ps1 extraction + Process.Start execution)
  - phase: 02-05
    provides: GamingTweaksViewModel/GamingTweaksPage.xaml scaffold with existing dropdown/toggle wiring pattern
provides:
  - All 6 D-06 network-dependent one-shot scripts ported as 12 embedded .ps1 resources (split per source menu branch)
  - 12 RelayCommand methods on GamingTweaksViewModel, one per embedded resource, sharing a pre-launch risk-disclosure log pattern
  - "Driver Tools (downloads third-party binaries — not integrity-verified)" UI section on GamingTweaksPage.xaml with 12 buttons across 3 rows
affects: [02-gaming-tweaks phase completion, future-phase self-heal/download-verification work if D-06's accepted-risk decision is ever revisited]

# Actuals (#2632)
actuals:
  tokens: 24625
  tasks: 3
  commits: 4

tech-stack:
  added: []
  patterns:
    - "D-06 script porting: keep the source script's admin-check/internet-check/silent-mode header verbatim, strip only the Read-Host menu (Write-Host menu prompt, while/switch loop, Read-Host prompt itself), keep each branch's underlying logic byte-identical"
    - "Multi-branch scripts with unconditional setup before AND after the menu (Driver Install Debloat & Settings) get that shared setup/tail duplicated into every per-branch resource file, same as Task 2's Nvidia Settings pattern (shared 7-Zip/winget prefix duplicated into both branches)"
    - "Every D-06 RelayCommand routes through a shared RunD06ScriptAsync(displayName, resourceSuffix) helper that logs the accepted-risk disclosure line before calling IScriptRunner.RunEmbeddedScriptAsync"

key-files:
  created:
    - src/AkariToolbox.App/Resources/GamingScripts/driverclean-auto.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/driverclean-manual.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/directx.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/cpp.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/driverinstalllatest-nvidia.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/driverinstalllatest-amd.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/driverinstalllatest-intel.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/nvidiasettings-recommended.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/nvidiasettings-default.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/driverinstalldebloat-nvidia.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/driverinstalldebloat-amd.ps1
    - src/AkariToolbox.App/Resources/GamingScripts/driverinstalldebloat-intel.ps1
  modified:
    - src/AkariToolbox.App/AkariToolbox.App.csproj
    - src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs
    - src/AkariToolbox.App/Views/GamingTweaksPage.xaml

key-decisions:
  - "Task 3's 771-line source script (Driver Install Debloat & Settings) was not itemized by 02-RESEARCH.md; read in full and found a 3-branch NVIDIA/AMD/Intel Read-Host menu wrapped between a shared unconditional 7-Zip setup prefix and a shared unconditional display/sound/MSI-mode/taskbar-icon/restart tail. Both the prefix and tail were duplicated into all 3 per-branch resource files (same duplication pattern established by Task 2's Nvidia Settings 2-branch split) rather than trying to factor them into a 4th shared resource, since RunEmbeddedScriptAsync runs one self-contained script per invocation."
  - "CommunityToolkit.Mvvm's [RelayCommand] source generator strips the trailing 'Async' from generated command property names (RunFooAsync -> RunFooCommand). All 8 new XAML Command bindings (5 in Task 2, 3 in Task 3) were written using the Async-stripped name from the start, and verified by a `dotnet build` after each task, to avoid repeating the WMC9999 misdiagnosis that occurred during Task 1's original execution."

requirements-completed: [GAMING-01]

coverage:
  - id: D1
    description: "Driver Clean (DDU Auto/Manual), DirectX, C++ Redistributables — 4 D-06 buttons embedded and wired (Task 1, completed in a prior session)"
    requirement: GAMING-01
    verification:
      - kind: other
        ref: "dotnet build AkariToolbox.slnx -c Debug (commit 425e54d)"
        status: pass
    human_judgment: true
    rationale: "D-06 buttons launch live third-party downloads/installs; a human must visually confirm the driver-tools section renders and click-test at least one button per the plan's own human-check instructions. Build success alone does not prove the script content is correct."
  - id: D2
    description: "Driver Install Latest (NVIDIA/AMD/Intel) and Nvidia Settings (Recommended/Default) — 5 D-06 buttons embedded and wired (Task 2)"
    requirement: GAMING-01
    verification:
      - kind: other
        ref: "dotnet build src/AkariToolbox.App/AkariToolbox.App.csproj (commit 37bdb42)"
        status: pass
    human_judgment: true
    rationale: "Same as D1 — network-dependent installer buttons need a human to visually confirm the 5 buttons render correctly labeled; not click-tested per the plan's own scope (build/visual confirmation only was sufficient for this task)."
  - id: D3
    description: "Driver Install Debloat & Settings (NVIDIA/AMD/Intel) — 3 D-06 buttons embedded and wired (Task 3), the largest and most invasive of the 6 D-06 scripts"
    requirement: GAMING-01
    verification:
      - kind: other
        ref: "dotnet build src/AkariToolbox.App/AkariToolbox.App.csproj (commit 5ca5d41)"
        status: pass
    human_judgment: true
    rationale: "Plan explicitly instructs 'Do not click-test this one (771 lines, longest-running, most invasive of the 6 D-06 scripts) — visual/build confirmation only is sufficient for this task.' A human must still visually confirm the 3 buttons appear."

duration: 25min
completed: 2026-09-01
status: complete
---

# Phase 02 Plan 06: D-06 Driver Tools (Driver Clean, Install Latest, Debloat & Settings, Nvidia Settings, DirectX, C++) Summary

**All 6 D-06 network-dependent one-shot scripts ported as 12 embedded PowerShell resources split per source menu branch, wired to 12 RelayCommand-backed buttons in a "not integrity-verified" Gaming Tweaks driver-tools section — completing GAMING-01's full toggle/action surface.**

## Performance

- **Duration:** ~25 min (this session covered Tasks 2-3; Task 1 was completed and committed in a prior interrupted session)
- **Started:** 2026-09-01T15:40:00Z (approx, this session)
- **Completed:** 2026-09-01T16:07:48Z
- **Tasks:** 3 (all complete)
- **Files modified:** 15 total across the plan (12 new .ps1 resources + csproj + ViewModel + XAML)

## Accomplishments
- Driver Clean (DDU Auto/Manual), DirectX, and C++ Redistributables embedded and wired (Task 1, prior session, commits 5114b1c/425e54d)
- Driver Install Latest split into NVIDIA/AMD/Intel branches, and Nvidia Settings split into Recommended/Default branches, each preserving the source script's unconditional shared setup steps verbatim per branch (Task 2, commit 37bdb42)
- Driver Install Debloat & Settings (771 lines, not itemized by RESEARCH.md) read in full, its actual 3-branch NVIDIA/AMD/Intel structure determined, and split into 3 resources each carrying the shared unconditional 7-Zip prefix and display/MSI/taskbar-icon/restart tail alongside its own branch logic (Task 3, commit 5ca5d41)
- All 12 D-06 buttons now visibly disclose the "not SHA256/signature-verified" accepted risk both via a pre-launch `ILogConsoleService` log line and via the persistent "Driver Tools (downloads third-party binaries — not integrity-verified)" section header

## Task Commits

Each task was committed atomically:

1. **Task 1: Driver Clean (2 branches), DirectX, C++** — `5114b1c` (feat), corrected by `425e54d` (fix: Command binding names) — completed and salvaged from a prior interrupted session, per the objective's instructions
2. **Task 2: Driver Install Latest (3 branches) + Nvidia Settings (2 branches)** — `37bdb42` (feat)
3. **Task 3: Driver Install Debloat & Settings (3 branches, determined by reading the 771-line source)** — `5ca5d41` (feat)

**Plan metadata:** this commit (docs: complete plan)

## Files Created/Modified
- `src/AkariToolbox.App/Resources/GamingScripts/driverclean-auto.ps1` / `driverclean-manual.ps1` — DDU Auto/Manual branches of `1 Driver Clean.ps1` (Task 1)
- `src/AkariToolbox.App/Resources/GamingScripts/directx.ps1` / `cpp.ps1` — direct copies of the no-menu source scripts (Task 1)
- `src/AkariToolbox.App/Resources/GamingScripts/driverinstalllatest-nvidia.ps1` / `-amd.ps1` / `-intel.ps1` — 3 branches of `2 Driver Install Latest.ps1` (Task 2)
- `src/AkariToolbox.App/Resources/GamingScripts/nvidiasettings-recommended.ps1` / `-default.ps1` — 2 branches of `4 Nvidia Settings.ps1`, each with the unconditional 7-Zip/NVCP/winget setup preserved (Task 2)
- `src/AkariToolbox.App/Resources/GamingScripts/driverinstalldebloat-nvidia.ps1` / `-amd.ps1` / `-intel.ps1` — 3 branches of `3 Driver Install Debloat & Settings.ps1`, each with the shared unconditional 7-Zip prefix and display/MSI-mode/taskbar-icon/restart tail preserved (Task 3)
- `src/AkariToolbox.App/AkariToolbox.App.csproj` — 12 `<EmbeddedResource>` entries across all 3 tasks
- `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs` — 12 `[RelayCommand]` methods routed through a shared `RunD06ScriptAsync(displayName, resourceSuffix)` helper
- `src/AkariToolbox.App/Views/GamingTweaksPage.xaml` — "Driver Tools" section with 12 buttons across 3 rows

## Decisions Made
- Task 3's branch count (3: NVIDIA/AMD/Intel) was determined by reading the full 771-line source script rather than trusting 02-RESEARCH.md, which explicitly did not itemize this script — confirmed the flagged assumption in the plan's `<output>` section was correct to leave open at planning time.
- The 771-line script's shared unconditional prefix (7-Zip install) and shared unconditional tail (display/sound settings dialogs, MSI-mode registry writes, taskbar icon unhiding, restart) were duplicated into all 3 branch resource files rather than factored into a 4th "common" resource, since `IScriptRunner.RunEmbeddedScriptAsync` executes one self-contained script per button click — this mirrors the duplication already established by Task 2's Nvidia Settings split.
- Verified the CommunityToolkit.Mvvm Async-stripping pitfall (documented in Task 1's post-mortem) did not recur: all 8 new XAML `Command=` bindings across Tasks 2-3 used the Async-stripped generated names from the first write, confirmed by a successful `dotnet build` after each task.

## Deviations from Plan

None - Tasks 2 and 3 executed exactly as written, including Task 3's explicit "read the source, don't guess" instruction for determining branch count.

## Issues Encountered

None for Tasks 2-3 in this session. (Task 1, completed in a prior interrupted session, had a Command-binding-name bug that was already fixed and committed as `425e54d` before this session began — not re-litigated here.)

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All 6 D-06 scripts (`1 Driver Clean.ps1`, `2 Driver Install Latest.ps1`, `3 Driver Install Debloat & Settings.ps1`, `4 Nvidia Settings.ps1`, `10 DirectX.ps1`, `11 C++.ps1`) are now fully ported, completing the `5 Graphics` folder's D-04/D-05/D-06 scope per this plan's objective.
- Human verification still needed: elevated manual launch of the Gaming Tweaks page to confirm the driver-tools section renders with all 12 buttons and the risk-disclosure log line fires before each launch (plan's own `<verify>` human-check step) — not performed in this non-interactive execution session.
- No blockers for subsequent Phase 02 plans.

## Self-Check: PASSED

- FOUND: all 12 embedded `.ps1` resources under `src/AkariToolbox.App/Resources/GamingScripts/`
- FOUND: `.planning/phases/02-gaming-tweaks/02-06-SUMMARY.md`
- FOUND commits: `5114b1c`, `425e54d`, `37bdb42`, `5ca5d41` in `git log --oneline`

---
*Phase: 02-gaming-tweaks*
*Completed: 2026-09-01*
