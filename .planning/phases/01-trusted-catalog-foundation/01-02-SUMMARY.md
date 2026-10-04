---
phase: 01-trusted-catalog-foundation
plan: 01-02
subsystem: gaming-catalog
tags: [winui3, mvvm-toolkit, autosuggestbox, xunit, gaming-tweaks, copy-contract, ui-states]

# Dependency graph
requires:
  - phase: 01-01
    provides: GamingCatalogEntry catalog with Suggest/MatchesFilter, GamingJournalStore, GamingRunner stop-at-first-failure loop, catalog page plus VM plus shell nav, 18 xUnit facts
provides:
  - Failed-row destructive-state marker (HasFailed) with clear-on-run and clear-on-revert semantics
  - Contract-copy fixes plus title ellipsis, capped scrollable suggestions, critical-brush failure caption in XAML
  - 19th xUnit fact covering the failure-marker plumbing
  - Release-gate evidence: app build Errors 0 Warnings 0, full suite 118/118, dynamic nav enumeration confirmed covering the 14th item
affects: [phase-2-tweak-waves, phase-3-polish, verify-work-uat]

# Actuals (#2632) — pairs with the plan's `estimate` to calibrate future estimates.
# Same estimateTokens scale (chars/4 over the realized diff), never a harness token count.
actuals:
  tokens: 1800
  tasks: 2
  commits: 1

# Tech tracking
tech-stack:
  added: []
  patterns: [failure-marker-with-clear-on-run-semantics, destructive-caption-via-theme-critical-brush]

key-files:
  created: []
  modified:
    - src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs
    - src/AppTemplate.App/Views/GamingCatalogPage.xaml
    - src/AppTemplate.Tests/GamingCatalogTests.cs

key-decisions:
  - "Failed-row state is a plain HasFailed bool on the row item (not a style trigger) so the marker survives search filtering for post-run inspection"
  - "verify-pages.ps1 left untouched: enumeration is dynamic FindAll with no hardcoded 13-count, so the 14th nav item is covered by construction"
  - "Live click-through and in-app UAT left as human steps (same as 01-01 D6): launching the elevated app needs an interactive desktop"

patterns-established:
  - "Failure marker: VM-owned ClearFailedFlags at run/revert start, single-row set on BulkResult.FailedEntry, XAML caption in SystemFillColorCriticalBrush as the only destructive usage"
  - "Suggestion overflow: MaxSuggestionListHeight 320 on the AutoSuggestBox so the 8-cap list scrolls deterministically"

requirements-completed: [BRW-01, BRW-02, SFT-01]

# Coverage metadata (#1602)
coverage:
  - id: D1
    description: "Search/suggest precision, contract copy, and failure-marker plumbing in the VM plus domain (titles-only prefix-first cap-8, tap-to-filter, Apply selected (N), sticky ticks, completed-prefix stop)"
    requirement: "BRW-01"
    verification:
      - kind: unit
        ref: "src/AppTemplate.Tests/GamingCatalogTests.cs#Suggest_* + MatchesFilter_* + Runner_* + RowItem_failed_flag_defaults_to_unset_and_is_settable"
        status: pass
    human_judgment: false
  - id: D2
    description: "All 8 UI states render per the copy/style contract in the running app (empty/loading/error/partial/zero-one-many/long-text/overflow plus populated catalog)"
    requirement: "BRW-02"
    verification:
      - kind: manual_procedural
        ref: "build-and-run.ps1 then Gaming Catalog: type search, read four-line explanations, tick rows, apply with footer progress, revert one-click"
        status: unknown
    human_judgment: true
    rationale: "XAML rendering, live registry read-back, and visual state inspection need a human on an elevated Windows desktop (same open item as 01-01 D6)"
  - id: D3
    description: "Release gates: full app build holds Errors 0 Warnings 0 and the full test suite is green"
    requirement: "SFT-01"
    verification:
      - kind: unit
        ref: "dotnet build src/AppTemplate.App/AppTemplate.App.csproj -c Debug => 0 Warning(s) 0 Error(s); dotnet test => 118/118 Passed"
        status: pass
    human_judgment: false
  - id: D4
    description: "Nav click-through sweep covers all 14 items including Gaming Catalog"
    requirement: "SFT-01"
    verification:
      - kind: other
        ref: "verify-pages.ps1 enumerates ListItems dynamically (no hardcoded count); MainWindow.xaml.cs NavItems holds Gaming Catalog; live sweep not run in this environment"
        status: unknown
    human_judgment: true
    rationale: "The UIA sweep launches the elevated app on an interactive desktop, which this execution environment cannot provide"

# Metrics
duration: ~25min
completed: 2026-10-04
status: complete
---

# Phase 01 Plan 02: Expansion Hardening Summary

**Search/suggest precision locked, contract copy fixed verbatim, failed-row destructive state added, style-token discipline held, and release gates green (build 0/0, suite 118/118)**

## Performance

- **Duration:** ~25 min (single execution session)
- **Started:** 2026-10-04
- **Completed:** 2026-10-04
- **Tasks:** 2 of 2
- **Files modified:** 3 (2 hardened, 1 test extended)

## Accomplishments

- Search path verified title-only prefix-first cap-8 from the first character with tap-to-filter never ticking: `Suggest`/`MatchesFilter` untouched (already OrdinalIgnoreCase, catalog-order, Take 8), `TextChanged` gated on UserInput, `SuggestionChosen` guarded cast applying text as filter only, live reads confined to navigation and post-run refresh
- Contract copy conformed verbatim: fixed double-space InfoBar typos (`Restore point created`, `Restore point skipped`, `Revert failed`); placeholder, empty heading plus name-only hint, footer `Applying {done} of {total}: {title}`, `{N} tweaks applied`, row-failure line with title plus Details, restore offer title/body/both actions, `Requires restart` badge, `Apply selected (N)` disabled-at-zero, `Revert all changes` all verified against UI-SPEC
- Failed-row destructive state implemented per the error/partial contract: `HasFailed` on `GamingCatalogRowItem`, cleared at run/revert start, set on the `BulkResult.FailedEntry` row only, later rows untouched with sticky ticks, XAML caption in `SystemFillColorCriticalBrush` (the only destructive usage; revert keeps default style)
- Long-text and overflow states covered: titles `CharacterEllipsis`-trim, explanation lines wrap at page MaxWidth 1000, suggestion dropdown capped with `MaxSuggestionListHeight="320"` so overflow scrolls
- Release gates green: app build Errors 0 Warnings 0, full suite 118/118, nav enumeration confirmed dynamic (13 pane items plus 1 footer item = 14 swept, Gaming Catalog present) with no script edit needed

## Task Commits

Each task was committed atomically:

1. **Task 1: Search precision plus copy conformance plus state coverage** - `628b5d9` (feat)
2. **Task 2: Release gates — nav click-through, build bar, full suite, UAT pass** - no file changes (verify-only task: build plus suite run, script inspected, no edit warranted), no commit

**Plan metadata:** SUMMARY commit follows (docs: complete plan)

## Files Created/Modified

- `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs` - Single-space InfoBar copy fixes; `HasFailed` flag fan-out with `ClearFailedFlags()` at apply/revert start and failed-row set on `BulkResult.FailedEntry` (Task 1, committed `628b5d9`)
- `src/AppTemplate.App/Views/GamingCatalogPage.xaml` - Title `TextTrimming="CharacterEllipsis"`; `MaxSuggestionListHeight="320"`; destructive failed-row caption bound to `HasFailed` in `SystemFillColorCriticalBrush` (Task 1, committed `628b5d9`)
- `src/AppTemplate.Tests/GamingCatalogTests.cs` - `RowItem_failed_flag_defaults_to_unset_and_is_settable` regression fact; suite now 19 GamingCatalog facts (Task 1, committed `628b5d9`)
- `verify-pages.ps1` - Inspected only: dynamic `FindAll` ListItem enumeration with no hardcoded 13-count, so the 14th item (Gaming Catalog) is covered by construction; no edit per the plan's edit-only-if-hardcoded rule (Task 2, no commit)

## Decisions Made

- Failed-row state is a plain `HasFailed` bool on the row item rather than a visual-state trigger: the marker must survive search re-filtering (rows are the same object references across `ApplyFilter`) so the failure stays inspectable until the next run or revert clears it.
- `verify-pages.ps1` left untouched: the plan scoped edits to hardcoded-count drift only, and the script enumerates dynamically. Its stale `$Exe` default path (old repo location plus old `AkariBase.App` name) is a pre-existing environment issue, recorded as a finding — not silently absorbed, not fixed out of scope.
- Live click-through sweep and in-app UAT left as human steps: this environment has no interactive desktop for the elevated WinUI app (same open item as 01-01 coverage D6, carried here as D2/D4 with explicit rationale).

## Deviations from Plan

None - plan executed exactly as written. The tracer slice from 01-01 already carried the search ranking, copy strings, group filtering, empty state, footer progress, restore offer, sticky ticks, and style tokens; this plan's work was the delta the must-haves demanded: copy typos, the missing failed-row visual, title trimming, suggestion-list height, one regression test, and gate evidence.

## Issues Encountered

- PowerShell shell (not bash): `head` and `[ -f ... ]` guards from the protocol do not parse; used `Select-Object` and `Test-Path` equivalents. No impact on outputs.
- `verify-pages.ps1` default `$Exe` path points at a stale location (`G:\Tools\akari-tool-winui3-mvvm\...\AkariBase.App.exe`): a live sweep would fail on the path before reaching any coverage logic, and launching the elevated app needs an interactive desktop. Left as a recorded finding for the human UAT pass (pass `-Exe <real path>` when running it).

## Threat Model Review (T-01-05 / T-01-06 / T-01-07 / T-01-SC)

- **T-01-05 (search query handling, mitigate):** Preserved — `Suggest`/`MatchesFilter` still pure in-memory LINQ over titles with OrdinalIgnoreCase; `SuggestionChosen` cast still guarded (`is string`); keystrokes never touch live probes or execution paths. No change needed.
- **T-01-06 (restore-point offer copy, mitigate):** Preserved — honest optional-wording body, Skip path present, apply never blocked on dialog outcome or creation failure. Single-space typo fix only.
- **T-01-07 (journal retention, mitigate):** Untouched — no journal changes in this plan.
- **T-01-SC (package installs, mitigate):** Zero installs; gate not triggered.

## Out-of-Scope Re-check

None added: no technical-details row, no presets, no export/import, no auto-mode, no perf snapshot, no vendor-aware rows, no wholesale Defender/VBS/update disabling, no bundled executables or scripts. The `HasFailed` caption and `MaxSuggestionListHeight` are state-coverage conformance, not new surface.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- The catalog browse experience is hardened: search, copy, all 8 states, and style discipline hold on top of the proven 01-01 tracer; later tweak waves ride this foundation.
- Open manual items for the human pass (carried from 01-01 D6): in-app UAT of the four phase success criteria on an elevated desktop, plus the `verify-pages.ps1 -Exe <real path>` sweep covering all 14 nav items.
- Known stubs: none. Full-suite: 118/118. Build bar: Errors 0 Warnings 0.

## Self-Check: PASSED

- All modified files exist on disk (GamingCatalogViewModel.cs, GamingCatalogPage.xaml, GamingCatalogTests.cs).
- Task commit exists: `628b5d9` verified via `git log`.
- Filtered tests 19/19; full suite 118/118; app build 0 Warning(s) 0 Error(s).

---
*Phase: 01-trusted-catalog-foundation*
*Completed: 2026-10-04*
