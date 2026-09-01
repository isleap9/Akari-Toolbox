# Phase 3: Debloat - Context

**Gathered:** 2026-09-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 3 delivers a fully rebuilt Debloat page — the predecessor's ~28-29 PowerShell-backed debloat actions (Run + optional Undo button pairs, grouped into 5 categories: Privacy & Telemetry, System & Performance, Cleanup, Explorer & UI, Tools), moved out of 180-line code-behind (`DebloatPage.xaml.cs`) into a proper ViewModel/service, with live streamed output while an action runs. Unlike Tweaks/Gaming, this is NOT a live-state toggle page — the predecessor's Debloat model has no "is this currently applied" read-back at all (confirmed dead/write-only `_applied` tracking list in `ToolService.cs`), so it keeps its own distinct architecture rather than reusing `ITweakHandler`.

Mid-discussion, the user pulled 3 scripts from their "Ultimate" tweak collection (`C:\Users\isleap\Desktop\AkariOS Tweaks\`) to replace 3 of the predecessor's existing actions outright — same pattern as Phase 2's Gaming Tweaks toggle-source swap.

Requirements covered: DEBLOAT-01, DEBLOAT-02, DEBLOAT-03.

</domain>

<decisions>
## Implementation Decisions

### Action & State Model
- **D-01:** Debloat actions stay as Run + optional Undo buttons with no live state read-back — action-log parity with the predecessor, not a live-state toggle model. Matches DEBLOAT-01's "run each action" wording and avoids inventing state detection for actions that have none (DiskCleanup, TempFiles, RestorePoint are one-shot side effects). — **Reversibility:** costly — switching later to a state-detection/toggle model (`ITweakHandler`-style) would mean re-architecting the ViewModel's command-binding shape away from Run/Undo button pairs.
- **D-02:** The Undo button is always enabled regardless of whether Run was clicked in the current session — exact parity with the predecessor. Covers undoing an action applied in a prior app session; most undo scripts are safe to run standalone.

### Script Replacement — Unwanted Apps Removal
- **D-03:** The predecessor's "Unwanted Apps — Remove" (`Debloat.ps1`/`Debloat-Undo.ps1`, a hardcoded ~29-app removal list) is retired entirely, replaced by `13 Bloatware.ps1`'s "Remove: All Bloatware (Recommended)" branch (option 2), ported **as-authored in full** — including its broader UWP-exclusion-list removal approach (deny-list of protected apps, not an allow-list of known bloat apps), disabling of several Windows optional features/capabilities, and side-removals of OneDrive, Remote Desktop Connection, Snipping Tool, and GameInput. — **Reversibility:** one-way — explicit override of the predecessor's narrower, hardcoded removal list; broadens this action's blast radius considerably and can't be quietly reverted to the old list without a fresh decision.
- **D-04:** The existing separate "OneDrive — Remove" action stays as its own button even though the new Bloatware removal also removes OneDrive as a side effect — accepted overlap (`Remove-AppxPackage`/uninstall calls are idempotent, running twice is harmless).
- **D-05:** Bloatware action's Undo maps to the script's "Install: All UWP Apps" branch (option 4) only — not a full symmetric undo; it will not restore the disabled optional features/capabilities or the separately-removed OneDrive/RDC/SnippingTool. The predecessor's own `Debloat-Undo.ps1` (an 8-app best-effort reinstall) is dropped entirely.

### Script Replacement — Microsoft Edge Actions
- **D-06:** The predecessor's "Microsoft Edge — Remove" (`RemoveEdge.ps1`) is retired, replaced by `20 Edge & WebView.ps1`'s branch 1 ("Uninstall", Recommended) — ported as-authored, including full WebView2 (`msedgewebview2`) runtime removal.
- **D-07:** This explicitly overrides REQUIREMENTS.md's Out-of-Scope entry "Full removal of Microsoft Store, WebView2, or Edge runtime dependencies" (written because WebView2 removal is the most-cited cause of breakage in debloat post-mortems). The user made an explicit, informed decision to accept that breakage risk for this action. — **Reversibility:** one-way — reverses a written Out-of-Scope decision; reinstating the exclusion later needs a fresh scope decision. **Needs a REQUIREMENTS.md follow-up edit** to record the override (not edited during this discussion — flagged for the user/next step, same pattern as Phase 2's D-12 GAMING-02 retirement).
- **D-08:** The predecessor's "Microsoft Edge — Debloat" (`EdgeDebloat.ps1`/`EdgeDebloat-Undo.ps1`) is retired, replaced by `10 Edge Settings.ps1`'s branch 1 ("Optimize", Recommended).
- **D-09:** Both new Edge actions' Undo maps to their script's branch 2 ("Default"): Edge & WebView's Undo reinstalls Edge + WebView2 via GitHub-downloaded installers and reapplies the Edge Settings import; Edge Debloat's Undo clears the Edge policies and reinstalls Edge via the same downloaded installer.
- **D-10:** Edge & WebView's branch-2 downloads (`edge.exe`, `edgewebview.exe` from `github.com/FR33THYFR33THY/Ultimate-Files`) run with **no added SHA256/signature verification** — consistent with Phase 2's D-06 accepted-risk precedent for network-dependent scripts. First instance of this pattern on the Debloat page.

### Script Replacement — BitLocker
- **D-12:** The predecessor's "BitLocker — Disable" (`DisableBitLocker.ps1`/`DisableBitLocker-Undo.ps1`) is retired, replaced by `3 Setup/1 BitLocker.ps1`'s branch 1 ("BitLocker: Off", Recommended) as Run — ported as-authored. Added mid-planning (2026-09-01), same "Ultimate" collection as D-03/D-06/D-08.
- **D-13:** BitLocker's Undo maps to the script's branch 2 ("BitLocker: On") **as-authored** — this only opens the BitLocker Control Panel for manual re-enable, it does NOT call `Enable-BitLocker` the way the predecessor's active `DisableBitLocker-Undo.ps1` did. Explicit, accepted scope reduction on Undo's capability — the user confirmed shipping the weaker (settings-panel-only) Undo rather than preserving the predecessor's active re-encryption call. — **Reversibility:** costly — a user who clicked Undo expecting active re-encryption (predecessor behavior) now only gets a Settings panel; reverting to active re-enable later is a straightforward script swap, not a migration, but the behavior gap is real and user-facing.

### Script Replacement — Windows AI / Copilot
- **D-14:** The predecessor's "Windows AI — Disable" (`WindowsAI.ps1`/`WindowsAI-Undo.ps1` — disables Copilot, Recall, `WSAIFabricSvc`, Notepad AI, and hides the AI settings page) is retired entirely, replaced by `6 Windows/9 Copilot.ps1`'s branch 1 ("Copilot: Off", Recommended) as Run and branch 2 ("Copilot: Default") as Undo — ported as-authored. The new script is Copilot-only: it removes the Copilot AppX package and one registry policy, but does NOT touch Recall, `WSAIFabricSvc`, or Notepad AI. Explicit, informed scope reduction — same pattern as D-07's Edge/WebView override — the user chose the narrower, newer script over the predecessor's broader one. No REQUIREMENTS.md Out-of-Scope entry conflicts (unlike D-07/D-06 Edge/WebView, no requirement or out-of-scope line mentions Recall/Copilot/Windows AI specifically). — **Reversibility:** one-way — explicit override of the predecessor's broader AI-disable action; reverting to also cover Recall/WSAIFabricSvc/Notepad AI later needs a fresh decision, not a config flip. Added mid-planning (2026-09-01).
- **D-15:** OneDrive — no standalone replacement script exists in the "Ultimate" collection (confirmed by directory search: `onedrive` only appears as a side-reference inside `13 Bloatware.ps1`, `20 Edge & WebView.ps1`, `9 Copilot.ps1`, and 2 unrelated Windows-6 scripts, never as its own numbered script). D-04's decision stands unchanged: the predecessor's separate "OneDrive — Remove" action is untouched by this phase; Bloatware's side-removal of OneDrive remains accepted harmless overlap. Recorded to close the loop on a mid-planning question, not a new decision.

### Safety Friction
- **D-11:** Destructive/risky Debloat actions (e.g. BitLocker disable, the broadened Bloatware removal, Edge/WebView removal, Hibernation disable) get a confirmation dialog before running, using the framework's existing `IDialogService` — new safety behavior beyond strict parity with the predecessor's zero-friction one-click buttons.

### Claude's Discretion
- Exact list of which actions require a confirmation dialog (D-11) — Claude/research proposes a risk classification for all actions during planning, presented for approval before implementation (same pattern as Phase 2 D-09's preset-list approval checkpoint).
- The predecessor's page displays **29** buttons across 5 categories, but DEBLOAT-01 says "28 PowerShell-backed debloat actions" — not resolved in this discussion. Likely "Create Restore Point" isn't counted among the 28 (it's a safety action, not a debloat action) but Claude/research should reconcile and confirm during planning if still ambiguous.
- Category grouping (Privacy & Telemetry / System & Performance / Cleanup / Explorer & UI / Tools) — not explicitly revisited; default to keeping the predecessor's grouping unless research surfaces a reason to change.
- Per-row busy/running indicator and whether actions can run concurrently or must serialize — not decided in this discussion (topic wasn't selected). Claude's technical call during planning; `TweakCatalog`'s existing per-key `SemaphoreSlim` lock is a reference pattern if serialization per-action is needed.
- Non-interactive extraction of the three replacement scripts' console-menu shape (numbered `Write-Host`/`Read-Host` loop, self-elevating) — same technique as Phase 2 D-04/D-07 (strip the menu, invoke the chosen branch's underlying logic directly), not a user vision question.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### New replacement scripts (from the "Ultimate" collection)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/13 Bloatware.ps1` — replaces "Unwanted Apps — Remove" (D-03, D-05); branch 2 ("Remove: All Bloatware") is the Run action, branch 4 ("Install: All UWP Apps") is the Undo action
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/20 Edge & WebView.ps1` — replaces "Microsoft Edge — Remove" (D-06, D-07, D-09, D-10); branch 1 (Uninstall) is Run, branch 2 (Default) is Undo
- `C:/Users/isleap/Desktop/AkariOS Tweaks/3 Setup/10 Edge Settings.ps1` — replaces "Microsoft Edge — Debloat" (D-08, D-09); branch 1 (Optimize) is Run, branch 2 (Default) is Undo
- `C:/Users/isleap/Desktop/AkariOS Tweaks/3 Setup/1 BitLocker.ps1` — replaces "BitLocker — Disable" (D-12, D-13); branch 1 ("BitLocker: Off") is Run, branch 2 ("BitLocker: On" — opens Settings only, no active re-enable) is Undo
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/9 Copilot.ps1` — replaces "Windows AI — Disable" (D-14); branch 1 ("Copilot: Off") is Run, branch 2 ("Copilot: Default") is Undo

### Predecessor source (port from, read-only)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Views/DebloatPage.xaml.cs` — the code-behind anti-pattern to fix (DEBLOAT-03); `BuildGroup`/row-button shape is the UI reference to translate, not the architecture
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Views/DebloatPage.xaml` — 5 category groups, Run/Undo button styling reference
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ToolService.cs` (`RunAction`/`RunWithTracking`/`RunScript`, lines ~69-90) — the action-routing shape to reference; confirms `_applied` is write-only (no real state tracking), supporting D-01
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Scripts/*.ps1` — all ~29 predecessor script pairs (Run + optional Undo), superseded only where D-03/D-06/D-08 apply; the rest port as direct carries per DEBLOAT-01

### Current codebase — reusable primitives
- `src/AkariToolbox.Framework/Services/IScriptRunner.cs` / `ScriptRunner.cs` — `RunEmbeddedScriptAsync` already streams every stdout/stderr line to `ILogConsoleService`; satisfies DEBLOAT-02 by reuse, no new streaming plumbing needed
- `src/AkariToolbox.Framework/Services/ILogConsoleService.cs` — persistent, collapsible, in-memory log dock from Phase 1 (D-05/D-06/D-08 in 01-CONTEXT.md) — the sink for all Debloat script output
- `src/AkariToolbox.App/Services/TweakCatalog.cs` — per-key `SemaphoreSlim` locking pattern (lines ~10-60), reference only if Debloat needs to prevent double-invocation of a single running action
- Framework's `IDialogService` — for the new confirmation-dialog behavior (D-11)

### Project-level docs
- `.planning/PROJECT.md` — Key Decisions table; architecture-debt callout on `DebloatPage.xaml.cs` code-behind
- `.planning/REQUIREMENTS.md` — DEBLOAT-01/02/03 exact wording; Out-of-Scope table's WebView2-removal entry **needs a follow-up edit per D-07**
- `.planning/ROADMAP.md` — Phase 3 success criteria
- `.planning/phases/02-gaming-tweaks/02-CONTEXT.md` — D-04/D-07 non-interactive-menu-extraction precedent; D-06 network-download accepted-risk precedent; D-08 flagged the remaining `6 Windows` scripts (including `13 Bloatware.ps1` and `20 Edge & WebView.ps1`) as Debloat candidates — now confirmed pulled in here
- `.planning/phases/01-foundation-akari-os-tweaks/01-CONTEXT.md` — origin of the `ILogConsoleService`/`IScriptRunner` primitives this phase reuses

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IScriptRunner.RunEmbeddedScriptAsync` — already streams stdout/stderr line-by-line to `ILogConsoleService`; DEBLOAT-02's "streamed status/output feedback" requirement is satisfied by reuse, not new work
- `ILogConsoleService` — Phase 1's collapsible log dock, already the display surface for script output
- Framework's `IDialogService` — available for confirmation dialogs (D-11)
- `TweakCatalog`'s per-key `SemaphoreSlim` gate — reference pattern for any needed run-concurrency guard

### Established Patterns
- Predecessor's Run+Undo button-pair row (`DebloatPage.xaml.cs` `BuildGroup`) — the visual/interaction shape to carry into WinUI 3, with the logic moved out of code-behind and into a ViewModel (DEBLOAT-03)
- Phase 2's technique (D-04/D-07) for stripping a self-elevating, `Read-Host`-menu console script down to a single non-interactive branch invocation — needed for all 3 replacement scripts, which share that exact shape

### Integration Points
- DEBLOAT-03 requires a new `DebloatViewModel` + service — no `ITweakHandler`/`ITweakCatalog` reuse, since this isn't a toggle model (D-01)
- Home dashboard's "Debloat" card (already present, disabled, per Phase 1 D-09/D-10) flips to enabled once this phase ships

</code_context>

<specifics>
## Specific Ideas

- All three replacement scripts (`13 Bloatware.ps1`, `20 Edge & WebView.ps1`, `10 Edge Settings.ps1`) share the same self-elevating console-menu shape as the Gaming Tweaks Ultimate-collection scripts: numbered `Write-Host` options + `Read-Host` loop, and all three hard-require an internet connection (`Test-Connection 8.8.8.8` check) even for their "Remove/Uninstall" branches — this connectivity dependency is new for the Debloat page (the predecessor's Debloat.ps1/RemoveEdge.ps1/EdgeDebloat.ps1 were local-only).
- `13 Bloatware.ps1`'s UWP-removal approach is an exclusion list (removes everything except an explicit allowlist of protected packages) — fundamentally broader than the predecessor's allow-list of ~29 named bloat apps.
- The predecessor's `_applied` list in `ToolService.RunWithTracking` is write-only (appended to, logged, never read back) — confirms there was never real "is this applied" state in the original Debloat page, grounding D-01.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope, including the two mid-discussion script-replacement decisions (both are direct substitutions for existing in-scope Debloat actions, not new capabilities).

</deferred>

---

*Phase: 3-Debloat*
*Context gathered: 2026-09-01*
