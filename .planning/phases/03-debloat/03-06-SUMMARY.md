---
phase: 03-debloat
plan: 06
subsystem: debloat
tags: [powershell, embedded-resource, winui3, xunit, script-extraction]

# Dependency graph
requires:
  - phase: 03-debloat
    provides: DebloatCatalog/DebloatAction/DebloatViewModel scaffolding, IScriptRunner reuse, and the branch-extraction technique proven across 03-01/03-02/03-03 (03-05 provides the Cleanup category's 3 direct-carry actions this plan completes)
provides:
  - 6 branch-extracted embedded PowerShell scripts closing out DEBLOAT-01's 28-action set (bloatware-remove, bloatware-installall, edgewebview-uninstall, edgewebview-default, edgesettings-optimize, edgesettings-default)
  - Comprehensive 28-action resource-resolution regression test (All_28_catalog_actions_have_resolvable_run_resources)
  - REQUIREMENTS.md D-07 override annotation on the WebView2 Out-of-Scope row
affects: [03-debloat phase closure, future ULT-01/v2 planning that touches these same "Ultimate" collection source scripts]

# Actuals (#2632)
actuals:
  tokens: 7600
  tasks: 3
  commits: 3

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Branch-extraction technique (proven in Phase 2 D-06 and earlier in this phase): strip self-elevation/console-cosmetic scaffolding from a self-elevating multi-branch console script, replace Pause-guarded pre-flight checks with a bare exit 1 (no Read-Host against non-redirected stdin), and copy only the chosen numbered branch's body as its own standalone embedded .ps1 resource"

key-files:
  created:
    - src/AkariToolbox.App/Resources/DebloatScripts/bloatware-remove.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/bloatware-installall.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/edgewebview-uninstall.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/edgewebview-default.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/edgesettings-optimize.ps1
    - src/AkariToolbox.App/Resources/DebloatScripts/edgesettings-default.ps1
  modified:
    - src/AkariToolbox.App/AkariToolbox.App.csproj
    - src/AkariToolbox.Tests/DebloatCatalogTests.cs
    - .planning/REQUIREMENTS.md

key-decisions:
  - "Split the csproj's EmbeddedResource additions across Task 1 (2 bloatware lines) and Task 2 (4 edge lines) rather than one combined edit, so each task's `dotnet build` verification only depends on files that task itself created — keeps atomic commits buildable in isolation"

patterns-established:
  - "Comprehensive per-catalog regression theory: [Theory]/[MemberData(nameof(AllCatalogActions))] iterating every DebloatCatalog action and asserting Run/Undo resource resolution — the direct DEBLOAT-01 analog to TweakHandlerOrderingTests' 'resolve exactly N, no gaps' pattern, permanently guarding future catalog edits against a missing embedded resource or typo'd suffix"

requirements-completed: [DEBLOAT-01, DEBLOAT-02]

coverage:
  - id: D1
    description: "'Unwanted Apps — Remove' (bloatware) Run/Undo scripts extracted from 13 Bloatware.ps1 branches 2/4, with show-menu loop-back replaced by exit and the pre-menu DevicePasswordLessBuildVersion registry write preserved"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Cleanup_replacement_actions_have_resolvable_resources"
        status: pass
      - kind: other
        ref: "grep -c show-menu bloatware-remove.ps1 bloatware-installall.ps1 (both 0)"
        status: pass
    human_judgment: true
    rationale: "Automated checks confirm the scripts are embedded, resolvable, and free of the hang-inducing show-menu/Pause tokens, but actual runtime behavior (app uninstall correctness, optional-feature disabling, no hang when run through IScriptRunner) requires a live elevated smoke test per the plan's own verification section item 4 — not exercisable in this non-Windows-elevated execution environment"
  - id: D2
    description: "'Microsoft Edge — Remove' (edgewebview) and 'Microsoft Edge — Debloat' (edgesettings) Run/Undo scripts extracted from 20 Edge & WebView.ps1 and 10 Edge Settings.ps1, with Pause stripped from the shared internet pre-flight check"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#Cleanup_replacement_actions_have_resolvable_resources"
        status: pass
      - kind: other
        ref: "grep -c Pause on all 4 edge*.ps1 files (all 0)"
        status: pass
    human_judgment: true
    rationale: "Resource resolution and Pause-absence are automated-checked, but the plan's own verification section flags Pitfall 4 (edgewebview-uninstall's .\\reg1.exe relative-path write) as needing manual smoke-test attention against the actual publish directory — not verifiable without a live elevated run"
  - id: D3
    description: "Comprehensive 28-action resource-resolution regression lock (All_28_catalog_actions_have_resolvable_run_resources) closing out DEBLOAT-01's full scope"
    requirement: "DEBLOAT-01"
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/DebloatCatalogTests.cs#All_28_catalog_actions_have_resolvable_run_resources (28 theory cases)"
        status: pass
    human_judgment: false
  - id: D4
    description: "REQUIREMENTS.md Out-of-Scope row annotated with the D-07 override record (WebView2 removal accepted-risk exception)"
    requirement: "DEBLOAT-01"
    verification:
      - kind: other
        ref: ".planning/REQUIREMENTS.md Out-of-Scope table row, manually diffed"
        status: pass
    human_judgment: false

duration: 8min
completed: 2026-09-02
status: complete
---

# Phase 3 Plan 6: Cleanup Category Replacement Actions Summary

**Extracted 6 branch-isolated PowerShell scripts (Bloatware Run/Undo, Edge & WebView Run/Undo, Edge Settings Run/Undo) from three self-elevating "Ultimate" collection console scripts, fixing 2 hang-risk pitfalls, and closed DEBLOAT-01's 28-action set with a comprehensive regression test.**

## Performance

- **Duration:** ~8 min (commit span)
- **Started:** 2026-09-02T01:46:41+02:00 (first task commit)
- **Completed:** 2026-09-02T01:49:31+02:00 (last task commit)
- **Tasks:** 3 completed
- **Files modified:** 9 (6 created, 3 modified)

## Accomplishments

- Extracted `bloatware-remove.ps1`/`bloatware-installall.ps1` from `13 Bloatware.ps1` branches 2/4, fixing the show-menu loop-back hang (Pitfall 1) and preserving the pre-menu `DevicePasswordLessBuildVersion` registry write (Pitfall 3)
- Extracted `edgewebview-uninstall.ps1`/`edgewebview-default.ps1` from `20 Edge & WebView.ps1` branches 1/2 (full WebView2 runtime removal + reinstall), preserving the `.\reg1.exe` relative-path write as-authored (Pitfall 4, D-06)
- Extracted `edgesettings-optimize.ps1`/`edgesettings-default.ps1` from `10 Edge Settings.ps1` branches 1/2
- All 6 scripts strip the shared internet-connectivity check's `Pause` call (Pitfall 2), replacing it with a bare `exit 1` so a non-redirected-stdin child process never blocks on `Read-Host`
- Added `Cleanup_replacement_actions_have_resolvable_resources` (5th and final per-category resource-resolution fact) and `All_28_catalog_actions_have_resolvable_run_resources` (comprehensive `[Theory]`/`[MemberData]` regression lock over all 28 catalog actions)
- Annotated REQUIREMENTS.md's WebView2 Out-of-Scope row with the D-07 override record, without deleting the original guidance
- 100% of DEBLOAT-01's 28-action scope is now embedded and resource-resolvable

## Task Commits

1. **Task 1: Extract Bloatware's Run/Undo branches (Pitfalls 1 & 3)** - `b11fd90` (feat)
2. **Task 2: Extract Edge & WebView and Edge Settings branches (Pitfalls 2 & 4)** - `946c63c` (feat)
3. **Task 3: Lock replacement actions' resource resolution + record D-07 override** - `51555aa` (test)

_Note: `commit_docs: true` for this repo, but this worktree-mode agent excludes STATE.md/ROADMAP.md per the parallel-execution contract — only SUMMARY.md and REQUIREMENTS.md are committed here; the orchestrator applies the final metadata commit after merge._

## Files Created/Modified

- `src/AkariToolbox.App/Resources/DebloatScripts/bloatware-remove.ps1` - D-03 Run action (13 Bloatware.ps1 branch 2, exclusion-list removal)
- `src/AkariToolbox.App/Resources/DebloatScripts/bloatware-installall.ps1` - D-05 Undo action (branch 4, Install All UWP Apps)
- `src/AkariToolbox.App/Resources/DebloatScripts/edgewebview-uninstall.ps1` - D-06 Run action (20 Edge & WebView.ps1 branch 1, full WebView2 removal)
- `src/AkariToolbox.App/Resources/DebloatScripts/edgewebview-default.ps1` - D-09 Undo action (branch 2, downloads+reinstalls edge.exe/edgewebview.exe)
- `src/AkariToolbox.App/Resources/DebloatScripts/edgesettings-optimize.ps1` - D-08 Run action (10 Edge Settings.ps1 branch 1)
- `src/AkariToolbox.App/Resources/DebloatScripts/edgesettings-default.ps1` - D-09 Undo action (branch 2, clears policies + reinstalls edge.exe)
- `src/AkariToolbox.App/AkariToolbox.App.csproj` - added 6 `<EmbeddedResource>` entries across 2 comment-annotated additions (Task 1: bloatware, Task 2: edge)
- `src/AkariToolbox.Tests/DebloatCatalogTests.cs` - added `Cleanup_replacement_actions_have_resolvable_resources` and the `All_28_catalog_actions_have_resolvable_run_resources` theory + `AllCatalogActions` MemberData source
- `.planning/REQUIREMENTS.md` - annotated the WebView2 Out-of-Scope row with the D-07 override

## Decisions Made

- Split the csproj's `<EmbeddedResource>` additions into two separate edits (Task 1: 2 bloatware lines; Task 2: 4 edge lines) instead of one combined edit up front, so each task's own `dotnet build` verification step only depends on the `.ps1` files that specific task created — keeps every atomic commit independently buildable, which the plan's per-task `<verify>` blocks require.

## Deviations from Plan

None - plan executed exactly as written. All 6 files, the DevicePasswordLessBuildVersion preservation, the show-menu→exit fix, and the Pause-stripping were built exactly per the plan's line-by-line extraction instructions, verified against the source scripts read in full before extraction.

## Issues Encountered

- Initial `dotnet build AkariToolbox.sln` (as literally written in the plan's `<verify>` blocks) failed with `MSB1009: Project file does not exist` — the repo's solution file is `AkariToolbox.slnx` at the repo root, not `src/AkariToolbox.sln`. Used the correct path (`dotnet build AkariToolbox.slnx` from repo root) for all build/test verification; this is a plan `<verify>` command typo carried over from earlier phase plans, not a code issue. Build and all 40 `DebloatCatalogTests` pass.
- Running the full test suite (not just the `DebloatCatalogTests` filter) surfaced 1 pre-existing failure: `ConvertersTests.EnumToBoolean_matches_parameter`, unrelated to this plan (last touched in Phase 1, expects a `COMException` that doesn't throw in this headless test environment). Already logged in `.planning/phases/03-debloat/deferred-items.md` under 03-01 — out of scope per the deviation rules' scope boundary, not re-logged here.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- DEBLOAT-01's full 28-action scope is now 100% embedded and resource-resolvable, locked by a comprehensive regression theory that will fail loudly (naming the specific broken action) if any future catalog edit adds an action whose script isn't embedded or has a typo'd suffix
- Manual smoke testing (plan's verification item 4) remains recommended before considering this phase's Cleanup category fully UAT-verified: clicking "Unwanted Apps — Remove"/"Microsoft Edge — Remove" Run and Undo on a live elevated Windows machine, both with and without internet connectivity, to confirm the Pause/show-menu fixes actually prevent hangs in the real `IScriptRunner` child-process environment (not exercisable from this non-Windows-elevated execution context)
- No blockers for phase closure — this was the final plan (wave 6) in Phase 3's 03-debloat sequence per the roadmap's plan count

## Self-Check: PASSED

All 6 created `.ps1` files and this SUMMARY.md verified present via `git ls-files`; all 3 task commits (`b11fd90`, `946c63c`, `51555aa`) verified present in `git log`.

---
*Phase: 03-debloat*
*Completed: 2026-09-02*
