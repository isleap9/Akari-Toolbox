# Phase 1: Trusted Catalog Foundation - Context

**Gathered:** 2026-10-04
**Status:** Ready for planning

## Phase Boundary

Phase 1 rebuilds the gaming-tweak catalog on Winhance rows: each FR33THY gaming tweak renders as a scannable card (title + one-line description + badge pills + live toggle), every row offers one-click Recommended/Default quick-set, and full technical detail (registry footprint, current/recommended/default values) sits behind a collapsed expander. Scope is the gaming set first (Game Mode/DVR, network latency, GPU scheduling, background — 8–12 rows); full-Windows categories (privacy, updates, power, telemetry) follow in later phases once the row pattern is proven. This is a clean rebuild — attempt 1 (bulk tick-then-apply with always-visible paragraphs) was fully reverted and archived.

## Implementation Decisions

### Product vision (locked)

- **D-00:** Build something like Winhance where the user optimizes Windows and enhances it with FR33THY Ultimate, which is the best for gaming. Winhance-style browse and per-tweak detail; FR33THY Ultimate as the optimization content backbone. — **Reversibility:** one-way — redefining the product away from Winhance parity would invalidate every row-level decision below

### Apply model

- **D-01:** Instant per-row toggles with live-apply. No tick-then-bulk-apply staging model — the staging state caused the attempt-1 revert-baseline bug class and contradicts the Winhance reference. — **Reversibility:** costly — reverting to bulk would reshape the VM, journal, and every row template
- **D-02:** SFT-01 must be rewritten: "bulk-apply with progress and revert all with one click" no longer describes the product. New shape: per-row instant apply with per-row revert, plus a restore-point offer on first apply (not a gate). Planner flags the REQUIREMENTS.md edit; researcher confirms no other requirement depends on bulk wording.

### Phase scope

- **D-03:** Gaming set first: Game Mode/DVR, network latency path, GPU scheduling, background control (8–12 rows). Full-Windows categories (privacy, updates, power, telemetry) are later phases, reusing the proven row pattern. — **Reversibility:** reversible — later phases add categories without touching the row contract

### Row anatomy (binding, from Winhance source)

- **D-04:** Every row follows `.planning/research/winhance-patterns.md` §6: title + ONE description line + badge pills + live control on the right. No always-visible paragraph stacks — that was attempt 1's exact failure (user UAT: "wanted something like Winhance, easy to read and understand").
- **D-05:** The one-liner carries what-it-does in plain gamer language (Winhance copy style: "Optimize your PC for play by turning things off in the background"). What/why/risk/reverts detail lives behind the collapsed Technical-details expander, not on the card face.
- **D-06:** Every tweak definition carries Recommended + Default values and exposes the per-row green "Set to Recommended" / grey "Set to Default" quick-set pair with tooltips naming the target value.
- **D-07:** Badge pills show live state: Recommended / Default / Custom (diverged) + restart-required where applicable. Pills reflect reality on every navigation (live read-back), never cached optimism.

### Revert and safety

- **D-08:** Revert is per-row (back to that row's prior/Default values), instant like apply. The attempt-1 snapshot journal does NOT return in bulk-prefix form; if a journal is needed for toggle semantics, the planner designs it row-scoped.
- **D-09:** Restore point is offered on first apply of the session with a Skip path and never blocks. Fully-reversible line and native-ops-only constraint stand unchanged.

### Carried forward from attempt 1 (still valid)

- **D-10:** Catalog seeds from current gaming rows plus Game Mode and DVR only — later-phase rows are not pulled forward.
- **D-11:** The catalog lives on a new dedicated page in nav, grouped System / Network / GPU / Background.
- **D-12:** Rows cross-reference the shared definitions behind the 169 existing Individual Tweaks rows instead of duplicating registry text.
- **D-13:** Reboot-required rows show a badge in place; never segregated, never blocking.
- **D-14:** Search matches row titles only (not descriptions or keys), suggests from the first character, prefix-first ranking, tap-to-filter without applying.

### Superseded from attempt 1 (do NOT follow)

- Attempt-1 D-10 (inline four-line explanations), D-11–D-16 (bulk tick model, sticky ticks, footer progress, stop-at-first-failure, one-click bulk revert) are dead. The archived `01-CONTEXT.md` in `.planning/phases/archive/01-trusted-catalog-foundation-attempt1/` is history, not guidance.

### the agent's Discretion

- Descriptor schema shape (extend TweakToggle vs new SettingDefinition-style record) — planner's call within D-04/D-06.
- Journal-vs-direct-revert mechanics for toggle semantics (D-08) — researcher + planner's call, must preserve the fully-reversible line.
- Suggestion dropdown control choice and empty-state copy — within D-14.
- Whether the quick-set pair also needs a page-level "apply recommended to all" — explicitly undecided, planner may propose but must not silently add bulk scope.

## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Product and requirements

- `.planning/PROJECT.md` — Product scope, core value, fully-reversible line, native-ops-only constraint
- `.planning/REQUIREMENTS.md` — BRW-01, BRW-02 still current; SFT-01 bulk wording is STALE (see D-02 — rewrite it during planning); v2 and out-of-scope lists (note: the "Technical-details row" out-of-scope entry is overridden by D-05 for the expander form)
- `.planning/ROADMAP.md` — Phase 1 goal, success criteria, Winhance design-bar note, attempt-1 archive pointer

### Design bar (binding)

- `.planning/research/winhance-patterns.md` — Row anatomy, quick-set pair, technical-details expander, page composition, per-row status feedback, §6 adoption constraints. Extracted from the real Winhance source with file/line refs. **Highest-priority input after this file.**

### Research

- `.planning/research/SUMMARY.md` — Synthesized stack, feature, architecture, and pitfalls findings with phase ordering
- `.planning/research/ARCHITECTURE.md` — Descriptor-catalog pattern, snapshot-before-apply findings (re-evaluate for toggle semantics per D-08)
- `.planning/research/PITFALLS.md` — Restore-point-only rollback is hollow; timer folklore stance

### Codebase map

- `.planning/codebase/ARCHITECTURE.md` — Shell and VM run pattern, TweakAction and TweakToggle abstractions
- `.planning/codebase/STRUCTURE.md` — Where views, view models, and tweak modules live and where new code goes
- `.planning/codebase/CONVENTIONS.md` — MVVM Toolkit idioms, page-level Binding rule, run-site InfoBar and status pairing

## Existing Code Insights

### Reusable Assets

- `src/AppTemplate.App/ViewModels/GamingViewModel.cs` — Existing gaming toggles and item-command pattern; seed rows come from here.
- `src/AppTemplate.App/ViewModels/CheckViewModel.cs` — Canonical run shape: IsBusy guard, StatusService Start, Task.Run work, InfoBar and dialog report, Complete in finally (adapt to per-row apply, not bulk).
- `src/AppTemplate.App/Tweaks/TweakToggle.cs` — Live-state read/apply pairing; extend with Recommended/Default + explanation metadata per D-06/D-05.
- `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs` — 32-item OS catalog showing the descriptor pattern to mirror.
- `src/AppTemplate.App/Tweaks/ControlPanelRows.cs` plus `ControlPanelRows.Generated.cs` — The 169 shared row definitions to cross-reference per D-12.
- `src/AppTemplate.App/Tweaks/RestorePointActions.cs` — Existing restore-point creation behind the D-09 offer gate.
- `src/AppTemplate.App/Tweaks/AkariToolState.cs` — HKCU write-through toggle memory.

### Established Patterns

- Item commands live on item classes because DataTemplate has no page-ancestor binding; page-level Binding with xBind only inside DataTemplate.
- Every run site pairs StatusService Start and Complete with InfoBar success or failure reporting.
- ViewModels never touch UI types; dialogs go through IDialogService and progress through IStatusService.
- SystemInfo WMI reads must dispose collections and objects and never run on the UI thread without Task.Run.

### Integration Points

- New page registers in `src/AppTemplate.App/App.xaml.cs` BuildHost alongside the 13 existing pages and view models, with a nav entry in `src/AppTemplate.App/MainWindow.xaml.cs` NavItems.
- New descriptor catalog sits beside `AkariTweakCatalog.cs` in `src/AppTemplate.App/Tweaks/`.

## Specific Ideas

- "I want to create something like Winhance where the user can optimize Windows and enhance it with FR33THY Ultimate which is the best for gaming" — the product thesis, locked as D-00.
- Winhance source provided by the user at `../Winhance` (sibling of the repo, reference only) — patterns already extracted; downstream agents read the extraction, not the raw source.
- Attempt-1 UAT verdict driving the redesign: page existed and functioned but "wanted something like Winhance, easy to read and understand" (minor severity, test 1 of archived `01-UAT.md`).

## Deferred Ideas

- Full-Windows categories (privacy, updates, power, telemetry) — later phases reusing this phase's row pattern (D-03).
- Page-level "apply recommended to all" — undecided, explicitly not silent scope (the agent's Discretion).
- Game profiles/presets, config export/import, auto gaming mode — already in REQUIREMENTS.md v2/out-of-scope; unchanged.

---

*Phase: 1-Trusted Catalog Foundation*
*Context gathered: 2026-10-04*
