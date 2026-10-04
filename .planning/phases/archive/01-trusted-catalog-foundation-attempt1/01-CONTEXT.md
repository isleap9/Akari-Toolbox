# Phase 1: Trusted Catalog Foundation - Context

**Gathered:** 2026-10-03
**Status:** Ready for planning

## Phase Boundary

Phase 1 builds the trusted catalog foundation: a new searchable gaming-tweak catalog page (System, Network, GPU, Background groups) where every row carries an inline explanation, rows are ticked for a footer-progress bulk apply that stops at the first failure, ticks stay sticky after apply, reboot rows carry a badge, a restore point is offered (not forced) before apply, and a top-bar revert button rolls everything back via the Phase 1 snapshot journal. Catalog rows cross-reference the shared definitions behind the existing 169 Individual Tweaks rows rather than duplicating them.

## Implementation Decisions

### Catalog scope and page

- **D-01:** Catalog seeds from current gaming rows plus Game Mode and DVR only — Phases 2-4 rows are not pulled forward.
- **D-02:** The catalog lives on a new dedicated page in nav, not as a GamingPage rework.
- **D-03:** Rows group into four groups: System, Network, GPU, Background.
- **D-04:** Gaming rows cross-reference the shared row definitions behind the 169 existing Individual Tweaks rows instead of duplicating them.
- **D-05:** Reboot-required rows show a badge in place; they are not segregated and do not block.

### Search and suggest

- **D-06:** Search matches against row titles only, not descriptions or technical keys.
- **D-07:** Suggestions appear from the first character typed.
- **D-08:** Prefix matches rank above substring matches.
- **D-09:** Tapping a suggestion filters the row list; it does not jump to or tick the row.

### Explanations

- **D-10:** Per-tweak explanations render inline under each row, not behind expand or dialog.

### Bulk apply and revert

- **D-11:** Bulk apply acts on ticked rows only; there is no apply-all button.
- **D-12:** Ticks stay sticky after a successful apply so the set can be re-run or inspected.
- **D-13:** Bulk apply progress shows in the existing shell status footer, not a dialog or inline spinners.
- **D-14:** On a row failure the run stops at the first failure rather than finishing all rows or skipping.
- **D-15:** A restore point is offered before bulk apply but the user may skip it; apply is never blocked on it.
- **D-16:** The revert control is a single one-click button in the top bar of the page.

### the agent's Discretion

No areas were explicitly delegated; the planner has flexibility on descriptor schema shape, journal storage location, and suggestion UI control choice within the decisions above.

## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project and requirements

- `.planning/PROJECT.md` — Product scope, core value, fully-reversible line, native-ops-only constraint
- `.planning/REQUIREMENTS.md` — BRW-01, BRW-02, SFT-01 definitions plus v2 and out-of-scope lists
- `.planning/ROADMAP.md` — Phase 1 goal, success criteria, UI hint, dependency note (Phases 2-4 ride on this foundation)

### Research

- `.planning/research/SUMMARY.md` — Synthesized stack, feature, architecture, and pitfalls findings with phase ordering
- `.planning/research/ARCHITECTURE.md` — Descriptor-catalog pattern, snapshot-before-apply as primary rollback, bulk runner shape
- `.planning/research/PITFALLS.md` — Restore-point-only rollback is hollow; per-tweak journal is the real mechanism; timer folklore stance

### Codebase map

- `.planning/codebase/ARCHITECTURE.md` — Shell and VM run pattern, TweakAction and TweakToggle abstractions
- `.planning/codebase/STRUCTURE.md` — Where views, view models, and tweak modules live and where new code goes
- `.planning/codebase/CONVENTIONS.md` — MVVM Toolkit idioms, page-level Binding rule, run-site InfoBar and status pairing

## Existing Code Insights

### Reusable Assets

- `src/AppTemplate.App/ViewModels/GamingViewModel.cs` — Existing gaming toggles and item-command pattern; seed rows come from here.
- `src/AppTemplate.App/ViewModels/CheckViewModel.cs` — Canonical bulk-run shape: IsBusy guard, StatusService Start, Task.Run work, InfoBar and dialog report, Complete in finally.
- `src/AppTemplate.App/Tweaks/TweakToggle.cs` — Live-state read and apply pairing to extend with explanation metadata.
- `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs` — 32-item OS catalog showing the descriptor pattern the gaming catalog mirrors.
- `src/AppTemplate.App/Tweaks/ControlPanelRows.cs` plus `ControlPanelRows.Generated.cs` — The 169 shared row definitions to cross-reference.
- `src/AppTemplate.App/Tweaks/RestorePointActions.cs` — Existing restore-point creation behind the offer gate.
- `src/AppTemplate.App/Tweaks/AkariToolState.cs` — HKCU write-through toggle memory.

### Established Patterns

- Item commands live on item classes because DataTemplate has no page-ancestor binding; page-level Binding with xBind only inside DataTemplate.
- Every run site pairs StatusService Start and Complete with InfoBar success or failure reporting.
- ViewModels never touch UI types; dialogs go through IDialogService and progress through IStatusService.
- SystemInfo WMI reads must dispose collections and objects and never run on the UI thread without Task.Run.

### Integration Points

- New page registers in `src/AppTemplate.App/App.xaml.cs` BuildHost alongside the 13 existing pages and view models, with a nav entry in `src/AppTemplate.App/MainWindow.xaml.cs` NavItems.
- Shell status footer in `src/AppTemplate.App/MainWindow.xaml` already binds the progress surface D-13 needs.
- New descriptor catalog sits beside `AkariTweakCatalog.cs` in `src/AppTemplate.App/Tweaks/`.

## Specific Ideas

- Winhance-style browse (search plus categories plus per-tweak info) scoped to gaming only — the project's headline UX bet from PROJECT.md.

## Deferred Ideas

None — discussion stayed within phase scope. Rollback detail, nav slot position, and proof bar were offered as follow-up areas and declined.

---

*Phase: 1-Trusted Catalog Foundation*
*Context gathered: 2026-10-03*
