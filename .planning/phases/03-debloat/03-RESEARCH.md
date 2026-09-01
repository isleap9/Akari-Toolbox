# Phase 3: Debloat - Research

**Researched:** 2026-09-01
**Domain:** WinUI 3 MVVM port of a PowerShell-backed action list (Run/Undo button pairs, streamed process output), plus three self-elevating "Ultimate"-collection console scripts adapted to non-interactive single-branch invocation.
**Confidence:** HIGH (all findings sourced from reading this repo's own code, the predecessor repo, and the three canonical replacement scripts — no external library research was required; this phase introduces zero new NuGet packages)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** Debloat actions stay as Run + optional Undo buttons with no live state read-back — action-log parity with the predecessor, not a live-state toggle model. Matches DEBLOAT-01's "run each action" wording and avoids inventing state detection for actions that have none (DiskCleanup, TempFiles, RestorePoint are one-shot side effects). Reversibility: costly.
- **D-02:** The Undo button is always enabled regardless of whether Run was clicked in the current session — exact parity with the predecessor. Most undo scripts are safe to run standalone.
- **D-03:** The predecessor's "Unwanted Apps — Remove" (`Debloat.ps1`/`Debloat-Undo.ps1`) is retired entirely, replaced by `13 Bloatware.ps1`'s "Remove: All Bloatware (Recommended)" branch (option 2), ported as-authored in full — including its broader exclusion-list removal approach, optional-feature/capability disabling, and side-removals of OneDrive, RDC, Snipping Tool, GameInput. Reversibility: one-way.
- **D-04:** The existing separate "OneDrive — Remove" action stays as its own button even though Bloatware removal also removes OneDrive as a side effect — accepted overlap, idempotent.
- **D-05:** Bloatware action's Undo maps to the script's "Install: All UWP Apps" branch (option 4) only — not a full symmetric undo; will not restore disabled optional features/capabilities or separately-removed OneDrive/RDC/SnippingTool. Predecessor's `Debloat-Undo.ps1` is dropped entirely.
- **D-06:** The predecessor's "Microsoft Edge — Remove" (`RemoveEdge.ps1`) is retired, replaced by `20 Edge & WebView.ps1`'s branch 1 ("Uninstall", Recommended) — ported as-authored, including full WebView2 (`msedgewebview2`) runtime removal.
- **D-07:** This explicitly overrides REQUIREMENTS.md's Out-of-Scope entry "Full removal of Microsoft Store, WebView2, or Edge runtime dependencies". User made an explicit, informed decision to accept breakage risk. Reversibility: one-way. **Needs a REQUIREMENTS.md follow-up edit** to record the override (not edited during discussion — flagged for user/next step, same pattern as Phase 2's D-12).
- **D-08:** The predecessor's "Microsoft Edge — Debloat" (`EdgeDebloat.ps1`/`EdgeDebloat-Undo.ps1`) is retired, replaced by `10 Edge Settings.ps1`'s branch 1 ("Optimize", Recommended).
- **D-09:** Both new Edge actions' Undo maps to their script's branch 2 ("Default"): Edge & WebView's Undo reinstalls Edge + WebView2 via GitHub-downloaded installers and reapplies the Edge Settings import; Edge Debloat's Undo clears Edge policies and reinstalls Edge via the same downloaded installer.
- **D-10:** Edge & WebView's branch-2 downloads (`edge.exe`, `edgewebview.exe` from `github.com/FR33THYFR33THY/Ultimate-Files`) run with **no added SHA256/signature verification** — consistent with Phase 2's D-06 accepted-risk precedent. First instance of this pattern on the Debloat page.
- **D-11:** Destructive/risky Debloat actions (e.g. BitLocker disable, the broadened Bloatware removal, Edge/WebView removal, Hibernation disable) get a confirmation dialog before running, using the framework's existing `IDialogService` — new safety behavior beyond strict parity with the predecessor's zero-friction one-click buttons.

### Claude's Discretion

- Exact list of which actions require a confirmation dialog (D-11) — Claude/research proposes a risk classification for all actions during planning, presented for approval before implementation (same pattern as Phase 2 D-09's preset-list approval checkpoint). **See "Proposed Risk Classification" below — needs user approval before/at planning.**
- The predecessor's page displays **29** buttons across 5 categories, but DEBLOAT-01 says "28 PowerShell-backed debloat actions" — not resolved in discussion. Likely "Create Restore Point" isn't counted among the 28. **Resolved by this research — see "28 vs 29 Reconciliation" below.**
- Category grouping (Privacy & Telemetry / System & Performance / Cleanup / Explorer & UI / Tools) — default to keeping the predecessor's grouping unless research surfaces a reason to change. **No reason found to change — keep as-is.**
- Per-row busy/running indicator and whether actions can run concurrently or must serialize — not decided; Claude's technical call. `TweakCatalog`'s per-key `SemaphoreSlim` lock is a reference pattern. **See "Concurrency Guard" below.**
- Non-interactive extraction of the three replacement scripts' console-menu shape — same technique as Phase 2 D-04/D-07 (strip the menu, invoke the chosen branch's underlying logic directly). **See "Non-Interactive Extraction Technique" below — full branch-by-branch extraction plan included.**

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope, including the two mid-discussion script-replacement decisions.

</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DEBLOAT-01 | User can run each of the 28 PowerShell-backed debloat actions from the Debloat page | "28 vs 29 Reconciliation" confirms the exact 28-action set and category breakdown; "Full Action Catalog" table lists every action with its Run/Undo embedded-resource mapping |
| DEBLOAT-02 | User sees streamed status/output feedback while a debloat action runs, without the UI freezing or crashing | `IScriptRunner.RunEmbeddedScriptAsync` already streams every stdout/stderr line to `ILogConsoleService` (verified by reading `ScriptRunner.cs`) — satisfied by reuse, zero new plumbing; async command pattern from `GamingTweaksViewModel.RunD06ScriptAsync` is the direct analog |
| DEBLOAT-03 | Debloat page logic lives in a ViewModel/service, not page code-behind | `GamingTweaksPage.xaml.cs` (verified, 17 lines) is the code-behind shape to replicate — zero logic, only `DataContext = viewModel`; "Recommended Project Structure" proposes `DebloatViewModel` + `DebloatCatalog`/`DebloatAction` model |

</phase_requirements>

## Summary

Phase 3 has no new external dependencies — every plumbing primitive DEBLOAT-02 needs (`IScriptRunner.RunEmbeddedScriptAsync`, `ILogConsoleService`) and DEBLOAT-03 needs (`ViewModelBase`, `CommunityToolkit.Mvvm` `[RelayCommand]`, DI registration in `App.xaml.cs`) already exists and is proven in Phase 1 and Phase 2. The work is entirely a **port + extraction** exercise: (1) enumerate the predecessor's 29 Debloat buttons and confirm which 28 satisfy DEBLOAT-01, (2) port 25 of them as direct embedded-script carries from `AkariOS-Companion/Scripts/*.ps1` (unchanged except for the extraction technique already proven in Phase 2 — strip self-elevation/window-title/menu-loop boilerplate, invoke via `IScriptRunner`), (3) extract 3 replacement actions from the three canonical "Ultimate"-collection scripts by isolating a single numbered branch's body and embedding it as its own `.ps1` resource, exactly like Phase 2 did for its 6 D-06 network scripts, and (4) build a new `DebloatViewModel` + `DebloatAction`/`DebloatCatalog` model — deliberately NOT `ITweakHandler`/`ITweakCatalog`, since Debloat has no live-state read-back (D-01).

The critical extraction risk is specific to `13 Bloatware.ps1`: unlike the two Edge scripts (which end each branch with a plain `exit`), Bloatware's branches 2 and 4 end by calling `show-menu` and falling back into the `while ($true)` `Read-Host` loop — that trailing call must be removed and replaced with `exit`, or the embedded script will hang waiting for console input that a `Process`-spawned, non-redirected-stdin invocation will never receive. A second, easy-to-miss detail: `13 Bloatware.ps1` runs an unconditional `reg add ... DevicePasswordLessBuildVersion ... 0` line *before* the menu is even shown (line 22) — this must be preserved in whichever branch scripts are extracted, since it is script-startup setup, not part of any single numbered branch.

**Primary recommendation:** Build `DebloatAction` (Title, Description, Category, RunResourceSuffix, UndoResourceSuffix?, RequiresConfirmation) + a static/DI-registered `DebloatCatalog` providing the 28 actions grouped into the predecessor's 5 categories; drive a new `DebloatViewModel` off that catalog with `[RelayCommand]` Run/Undo methods that call `IScriptRunner.RunEmbeddedScriptAsync`, gated by a per-action `SemaphoreSlim` (TweakCatalog pattern) and, for the D-11 risk list, `IDialogService.ConfirmAsync` before invocation.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Debloat action catalog (28 actions, 5 categories, Run/Undo resource mapping) | App / Business Logic (`DebloatCatalog`) | — | Static data + resource-name mapping, no UI or process concerns |
| Run/Undo command dispatch, confirmation gating, concurrency guard | App / ViewModel (`DebloatViewModel`) | — | CommunityToolkit.Mvvm `[RelayCommand]`s bound directly to the page's XAML, per DEBLOAT-03 |
| Embedded PowerShell script extraction + process execution + stdout/stderr streaming | Framework / OS Integration (`IScriptRunner`) | — | Already built (Phase 1/2), reused unchanged — this phase adds no new plumbing here |
| Streamed output display | Framework / UI (`ILogConsoleService` + shell log dock) | — | Already built (Phase 1), reused unchanged |
| Confirmation dialogs for risky actions (D-11) | Framework / UI (`IDialogService`) | App / ViewModel | Dialog rendering lives in Framework; the decision of *which* actions need confirmation lives in the App-tier catalog data |
| Actual registry/service/appx mutations (the 28 debloat effects) | OS / PowerShell script body | — | Deliberately stays in PowerShell per the project's "port PowerShell logic, don't rewrite in C#" convention (CLAUDE.md Stack Patterns) |

## Standard Stack

### Core

No new packages this phase. `## Package Legitimacy Audit` below documents this explicitly.

| Component | Version | Purpose | Why Standard |
|-----------|---------|---------|--------------|
| `IScriptRunner`/`ScriptRunner` (`AkariToolbox.Framework.Services`) | already in repo | Extracts an embedded `.ps1` resource, runs `powershell.exe -NoProfile -ExecutionPolicy Bypass -File <temp>`, streams stdout/stderr line-by-line to `ILogConsoleService` | [VERIFIED: src/AkariToolbox.Framework/Services/ScriptRunner.cs:109-136] `RunEmbeddedScriptAsync` — "Extracts an embedded resource whose name ends with resourceSuffix to a GUID-suffixed temp file, runs it via powershell.exe -NoProfile -ExecutionPolicy Bypass -File <temp> {arguments}, and deletes the temp file in a finally block" — exactly satisfies DEBLOAT-02 by reuse |
| `ILogConsoleService` (`AkariToolbox.Framework.Services`) | already in repo | In-memory, dispatcher-safe, `ObservableCollection<string>`-backed log sink bound to the shell's log dock | [VERIFIED: src/AkariToolbox.Framework/Services/ILogConsoleService.cs:11-23] `ObservableCollection<string> Lines { get; }` / `void Log(string message)` |
| `IDialogService`/`DialogService` (`AkariToolbox.Framework.Services`) | already in repo | `ConfirmAsync(title, message, confirmText, cancelText)` returns `Task<bool>`, serialized via internal `SemaphoreSlim` so overlapping requests queue | [VERIFIED: src/AkariToolbox.Framework/Services/IDialogService.cs:20-25,82-90] — for D-11 confirmation dialogs |
| `CommunityToolkit.Mvvm` `[RelayCommand]`/`[ObservableProperty]` | 8.4.2 (CLAUDE.md-confirmed) | ViewModel command binding | Already used identically in `GamingTweaksViewModel` |
| `ITweakCatalog`/`TweakCatalog` per-key `SemaphoreSlim` pattern | already in repo, reference-only | Per-key run-concurrency guard | [VERIFIED: src/AkariToolbox.App/Services/TweakCatalog.cs:10,33] `private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();` / `var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));` — reference pattern only, Debloat does NOT reuse `ITweakCatalog` itself (D-01) |

### Supporting

None — this phase is additive UI/ViewModel/embedded-script work on top of existing Framework primitives.

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Embedding each replacement-script branch as its own separate `.ps1` resource file (Phase 2 D-06 pattern) | Embedding the full multi-branch script once and piping simulated `Read-Host` input via stdin | Simulated stdin is fragile (exact input sequence coupling, breaks silently if the script's menu numbering changes) and the codebase has zero precedent for it; the file-per-branch technique is proven twice already (Gaming's 6 D-06 scripts, its NVIDIA/AMD/Intel branch splits) — use the proven technique |
| `IScriptRunner.RunEmbeddedScriptAsync` reuse | A new `IDebloatScriptRunner` abstraction | No justification — `RunEmbeddedScriptAsync`'s resource-resolution already searches both `AkariToolbox.Framework`'s own assembly and every other loaded assembly (so `AkariToolbox.App`'s embedded resources resolve correctly), per its own doc comment; a new abstraction would duplicate proven code for no capability gain |

**Installation:** None required — no new NuGet packages.

**Version verification:** N/A — no new packages recommended this phase.

## Package Legitimacy Audit

**Not applicable this phase.** No new external packages are introduced. All execution plumbing (`IScriptRunner`, `ILogConsoleService`, `IDialogService`) and MVVM plumbing (`CommunityToolkit.Mvvm`) already exist in the repo from Phase 1/Phase 2 and are reused unchanged. The three replacement scripts (`13 Bloatware.ps1`, `20 Edge & WebView.ps1`, `10 Edge Settings.ps1`) are local files supplied directly by the user from `C:\Users\isleap\Desktop\AkariOS Tweaks\`, not registry packages — the Package Legitimacy Gate protocol (npm/PyPI/crates registry checks) does not apply to them. Their own runtime network calls (downloading `edge.exe`/`edgewebview.exe`/`remotedesktopconnection.exe`/`snippingtool.exe` from `github.com/FR33THYFR33THY/Ultimate-Files`) are an accepted, already-decided risk (D-10, matching Phase 2's D-06 precedent) — not a package-legitimacy concern, a supply-chain-of-downloaded-binaries concern, tracked separately in "Common Pitfalls" below.

**Packages removed due to [SLOP] verdict:** none
**Packages flagged as suspicious [SUS]:** none

## 28 vs 29 Reconciliation

[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Views/DebloatPage.xaml.cs:38-85] Counting every tuple in each `BuildGroup(...)` call:

| Category | Count | Items |
|----------|-------|-------|
| Privacy & Telemetry | 8 | Telemetry, Activity History, Location Tracking, PS7 Telemetry, Windows AI, Consumer Features, Background Apps, Store Search |
| System & Performance | 9 | **Create Restore Point**, Visual Effects, Services, Delivery Optimization, BitLocker, Hibernation, Storage Sense, WPBT, Set Time to UTC |
| Cleanup | 6 | Disk Cleanup, Temporary Files, Unwanted Apps, OneDrive, Microsoft Edge — Debloat, Microsoft Edge — Remove |
| Explorer & UI | 5 | End Task, Folder Discovery, Explorer Home, Right-Click Classic, Widgets |
| Tools | 1 | O&O ShutUp10++ |
| **Total buttons on page** | **29** | |

[VERIFIED: src/AkariToolbox.App/ViewModels/HomeViewModel.cs:37] The Home dashboard's own placeholder card already states the number Claude Toolbox is targeting: `new HomeCard { Title = "Debloat", Description = "Run 28 PowerShell-backed debloat actions", ... }` — confirms **28**, matching DEBLOAT-01's wording exactly.

**Conclusion (confirms the CONTEXT.md hypothesis):** "Create Restore Point" is the one excluded action. [VERIFIED: DebloatPage.xaml.cs:52] its row tuple is `("Create Restore Point", "Creates a Windows system restore point before making changes", "RestorePoint.ps1", "")` — it is a safety/utility action with an empty Undo slot, categorically distinct from the 28 debloat/privacy/cleanup actions. **The planner should build exactly 28 Debloat-page actions and treat "Create Restore Point" as out of DEBLOAT-01's scope for this phase** (it could later become part of the deferred SAFE-01 v2 requirement, not re-litigated here).

## Full Action Catalog

25 of the 28 actions port directly from `AkariOS-Companion/Scripts/*.ps1` (embed the existing `.ps1` files as-is, minus the extraction cleanup described below where the predecessor script needs it — most of these are already non-interactive, no menu to strip). 3 actions are replaced per D-03/D-06/D-08.

| # | Category | Title | Run Script (source) | Undo Script (source) | Status |
|---|----------|-------|----------------------|-----------------------|--------|
| 1 | Privacy & Telemetry | Telemetry — Disable | `Telemetry.ps1` | `Telemetry-Undo.ps1` | Direct carry |
| 2 | Privacy & Telemetry | Activity History — Disable | `ActivityHistory.ps1` | `ActivityHistory-Undo.ps1` | Direct carry |
| 3 | Privacy & Telemetry | Location Tracking — Disable | `LocationTracking.ps1` | `LocationTracking-Undo.ps1` | Direct carry |
| 4 | Privacy & Telemetry | PS7 Telemetry — Disable | `PS7Telemetry.ps1` | `PS7Telemetry-Undo.ps1` | Direct carry |
| 5 | Privacy & Telemetry | Windows AI — Disable | `WindowsAI.ps1` | `WindowsAI-Undo.ps1` | Direct carry |
| 6 | Privacy & Telemetry | Consumer Features — Disable | `ConsumerFeatures.ps1` | `ConsumerFeatures-Undo.ps1` | Direct carry |
| 7 | Privacy & Telemetry | Background Apps — Disable | `DisableBGApps.ps1` | `DisableBGApps-Undo.ps1` | Direct carry |
| 8 | Privacy & Telemetry | Store Search — Disable | `StoreSearch.ps1` | `StoreSearch-Undo.ps1` | Direct carry |
| 9 | System & Performance | Visual Effects — Best Perf | `VisualEffects.ps1` | `VisualEffects-Undo.ps1` | Direct carry |
| 10 | System & Performance | Services — Set to Manual | `Services.ps1` | `Services-Undo.ps1` | Direct carry |
| 11 | System & Performance | Delivery Optimization — Disable | `DeliveryOptimization.ps1` | `DeliveryOptimization-Undo.ps1` | Direct carry |
| 12 | System & Performance | BitLocker — Disable | `DisableBitLocker.ps1` | `DisableBitLocker-Undo.ps1` | Direct carry — **D-11 risk-flagged** |
| 13 | System & Performance | Hibernation — Disable | `Hibernation.ps1` | `Hibernation-Undo.ps1` | Direct carry — **D-11 risk-flagged** |
| 14 | System & Performance | Storage Sense — Disable | `StorageSense.ps1` | `StorageSense-Undo.ps1` | Direct carry |
| 15 | System & Performance | WPBT — Disable | `WPBT.ps1` | `WPBT-Undo.ps1` | Direct carry |
| 16 | System & Performance | Set Time to UTC | `UTC.ps1` | `UTC-Undo.ps1` | Direct carry |
| 17 | Cleanup | Disk Cleanup — Run | `DiskCleanup.ps1` | *(none)* | Direct carry |
| 18 | Cleanup | Temporary Files — Remove | `TempFiles.ps1` | *(none)* | Direct carry |
| 19 | Cleanup | Unwanted Apps — Remove | **`13 Bloatware.ps1` branch 2** ("Remove: All Bloatware") | **`13 Bloatware.ps1` branch 4** ("Install: All UWP Apps") | **REPLACED (D-03/D-05)** — **D-11 risk-flagged** |
| 20 | Cleanup | OneDrive — Remove | `RemoveOneDrive.ps1` | `RemoveOneDrive-Undo.ps1` | Direct carry (accepted overlap with #19, D-04) — proposed **D-11 risk-flag** (see below) |
| 21 | Cleanup | Microsoft Edge — Debloat | **`10 Edge Settings.ps1` branch 1** ("Optimize") | **`10 Edge Settings.ps1` branch 2** ("Default") | **REPLACED (D-08/D-09)** |
| 22 | Cleanup | Microsoft Edge — Remove | **`20 Edge & WebView.ps1` branch 1** ("Uninstall") | **`20 Edge & WebView.ps1` branch 2** ("Default") | **REPLACED (D-06/D-07/D-09)** — **D-11 risk-flagged** |
| 23 | Explorer & UI | End Task — Enable | `EndTask.ps1` | `EndTask-Undo.ps1` | Direct carry |
| 24 | Explorer & UI | Folder Discovery — Disable | `FolderDiscovery.ps1` | `FolderDiscovery-Undo.ps1` | Direct carry |
| 25 | Explorer & UI | Explorer Home — Remove | `RemoveHomeAndGallery.ps1` | `RemoveHomeAndGallery-Undo.ps1` | Direct carry |
| 26 | Explorer & UI | Right-Click — Classic | `RightClickMenu.ps1` | `RightClickMenu-Undo.ps1` | Direct carry |
| 27 | Explorer & UI | Widgets — Remove | `Widgets.ps1` | `Widgets-Undo.ps1` | Direct carry |
| 28 | Tools | O&O ShutUp10++ — Run | `OOSU.ps1` | *(none)* | Direct carry |

**Excluded from the 28 (out of DEBLOAT-01 scope this phase):** "Create Restore Point" (`RestorePoint.ps1`) — see reconciliation above.

[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Scripts/*.ps1 directory listing] All 25 direct-carry `.ps1`/`.ps1-Undo` pairs above were confirmed present on disk by directory listing (`ActivityHistory.ps1`, `ActivityHistory-Undo.ps1`, `ConsumerFeatures.ps1`, `ConsumerFeatures-Undo.ps1`, `DeliveryOptimization.ps1`, `DeliveryOptimization-Undo.ps1`, `DisableBGApps.ps1`, `DisableBGApps-Undo.ps1`, `DisableBitLocker.ps1`, `DisableBitLocker-Undo.ps1`, `DiskCleanup.ps1`, `EndTask.ps1`, `EndTask-Undo.ps1`, `FolderDiscovery.ps1`, `FolderDiscovery-Undo.ps1`, `Hibernation.ps1`, `Hibernation-Undo.ps1`, `LocationTracking.ps1`, `LocationTracking-Undo.ps1`, `OOSU.ps1`, `PS7Telemetry.ps1`, `PS7Telemetry-Undo.ps1`, `RemoveHomeAndGallery.ps1`, `RemoveHomeAndGallery-Undo.ps1`, `RemoveOneDrive.ps1`, `RemoveOneDrive-Undo.ps1`, `RightClickMenu.ps1`, `RightClickMenu-Undo.ps1`, `Services.ps1`, `Services-Undo.ps1`, `StorageSense.ps1`, `StorageSense-Undo.ps1`, `StoreSearch.ps1`, `StoreSearch-Undo.ps1`, `Telemetry.ps1`, `Telemetry-Undo.ps1`, `TempFiles.ps1`, `UTC.ps1`, `UTC-Undo.ps1`, `VisualEffects.ps1`, `VisualEffects-Undo.ps1`, `WPBT.ps1`, `WPBT-Undo.ps1`, `Widgets.ps1`, `Widgets-Undo.ps1`, `WindowsAI.ps1`, `WindowsAI-Undo.ps1`). `Debloat.ps1`/`Debloat-Undo.ps1`, `RemoveEdge.ps1`, and `EdgeDebloat.ps1`/`EdgeDebloat-Undo.ps1` also exist on disk but are dropped per D-03/D-06/D-08 (not embedded in the new app).

## Proposed Risk Classification (D-11 — needs user approval)

D-11 names 4 examples explicitly ("e.g., BitLocker disable, the broadened Bloatware removal, Edge/WebView removal, Hibernation disable"). This research proposes the exact confirmation-required set, following the same "propose for approval" checkpoint pattern as Phase 2's D-09 preset lists:

| Action | Requires Confirmation | Rationale |
|--------|----------------------|-----------|
| BitLocker — Disable | **YES** (D-11 named) | Removes disk encryption; [VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Scripts/DisableBitLocker.ps1:9] `Disable-BitLocker -MountPoint $Env:SystemDrive` — irreversible without re-enabling BitLocker from scratch (Undo script exists but re-encryption is a slow background operation, not instant) |
| Unwanted Apps — Remove (Bloatware, replaced) | **YES** (D-11 named) | D-03: exclusion-list removal (broader than predecessor's allow-list), disables optional Windows features/capabilities, side-removes OneDrive/RDC/Snipping Tool/GameInput — largest blast radius of any action on the page |
| Microsoft Edge — Remove (Edge & WebView, replaced) | **YES** (D-11 named) | D-07 explicitly overrides a written Out-of-Scope entry citing WebView2 removal as "the most-cited cause of breakage in debloat post-mortems" — user accepted this risk but it still warrants a click-through warning |
| Hibernation — Disable | **YES** (D-11 named) | Removes `hiberfil.sys` and disables Fast Startup (which depends on hibernation on many systems) — can surprise users who rely on Fast Startup for boot speed |
| OneDrive — Remove | **PROPOSED YES** (not in D-11's named list — Claude's discretion) | Data-loss risk distinct from the others: if any files exist only in the cloud (not fully synced locally — "Files On-Demand" placeholders), running `OneDriveSetup.exe -uninstall` can leave those files inaccessible until OneDrive is reinstalled and re-signed-in. This is a genuine, non-obvious data-loss vector the other three don't share (they're all system-setting reversals, not user-data-adjacent) |
| Microsoft Edge — Debloat (Edge Settings, replaced) | NO | Policy-only registry changes (extension forcelist, hardware acceleration, background mode) — fully reversible via its own Undo branch, no data loss, no feature removal |
| All 22 remaining actions (Telemetry, Activity History, Location Tracking, PS7 Telemetry, Windows AI, Consumer Features, Background Apps, Store Search, Visual Effects, Services, Delivery Optimization, Storage Sense, WPBT, UTC, Disk Cleanup, Temp Files, End Task, Folder Discovery, Explorer Home, Right-Click Classic, Widgets, O&O ShutUp10++) | NO | Either fully reversible via Undo, cosmetic/UI-only, or (Disk Cleanup / Temp Files) standard, expected, Microsoft-sanctioned cleanup operations with no unusual risk profile |

**Recommendation:** 5 of 28 actions get a confirmation dialog (BitLocker, Bloatware, Edge & WebView Remove, Hibernation, OneDrive Remove). Present this table for explicit approval before/at planning, consistent with how the discussion flagged this as Claude's discretion requiring a proposal step.

## Architecture Patterns

### System Architecture Diagram

```
User clicks "Run" or "Undo" on a DebloatPage row
        │
        ▼
DebloatViewModel.RunActionCommand(DebloatAction)
        │
        ├─► [if action.RequiresConfirmation] IDialogService.ConfirmAsync(...)
        │         │
        │         └─► user cancels ──► abort, no further action
        │
        ▼ (confirmed or no confirmation needed)
   per-action SemaphoreSlim gate (TweakCatalog-style, keyed by action.Key)
        │
        ▼
IScriptRunner.RunEmbeddedScriptAsync(action.RunResourceSuffix)
        │
        ├─► extracts embedded .ps1 resource to %TEMP%\AkariToolbox-{guid}-{name}.ps1
        ├─► Process.Start("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File <temp>")
        ├─► stdout/stderr streamed line-by-line ──► ILogConsoleService.Log(line)
        │                                                   │
        │                                                   ▼
        │                                          shell's collapsible log dock
        │                                          (ObservableCollection<string> Lines,
        │                                           bound via x:Bind, UI-thread-marshaled)
        │
        └─► temp file deleted in finally block, exit code returned
        │
        ▼
DebloatViewModel releases the per-action gate; row's busy indicator clears
```

### Recommended Project Structure

```
src/AkariToolbox.App/
├── Models/
│   └── DebloatAction.cs          # Title, Description, Category, RunResourceSuffix,
│                                  # UndoResourceSuffix?, RequiresConfirmation
├── Services/
│   ├── IDebloatCatalog.cs        # IReadOnlyList<DebloatCategoryGroup> Categories { get; }
│   └── DebloatCatalog.cs         # static data — the 28-row table above, grouped into 5 categories
├── ViewModels/
│   └── DebloatViewModel.cs       # [RelayCommand] RunAction / UndoAction, per-key SemaphoreSlim,
│                                  # confirmation gating, ObservableCollection<DebloatActionItem> per group
├── Views/
│   ├── DebloatPage.xaml          # 5 category groups, Run/Undo button pairs — translate the predecessor's
│   │                              # BuildGroup visual shape (card/row/separator) into XAML + ItemsControl/x:Bind
│   └── DebloatPage.xaml.cs       # zero logic — DataContext = viewModel, mirrors GamingTweaksPage.xaml.cs exactly
└── Resources/
    └── DebloatScripts/           # embedded .ps1 resources — 25 direct carries + 6 branch-extracted replacements
        ├── telemetry.ps1
        ├── telemetry-undo.ps1
        ├── ... (23 more direct-carry pairs/singles)
        ├── bloatware-remove.ps1        # 13 Bloatware.ps1 branch 2 body, menu/loop stripped
        ├── bloatware-installall.ps1    # 13 Bloatware.ps1 branch 4 body, menu/loop stripped
        ├── edgewebview-uninstall.ps1   # 20 Edge & WebView.ps1 branch 1 body
        ├── edgewebview-default.ps1     # 20 Edge & WebView.ps1 branch 2 body
        ├── edgesettings-optimize.ps1   # 10 Edge Settings.ps1 branch 1 body
        └── edgesettings-default.ps1    # 10 Edge Settings.ps1 branch 2 body
```

### Pattern 1: Zero-logic page code-behind (DEBLOAT-03)

**What:** The `.xaml.cs` file does nothing but construct with an injected ViewModel and set `DataContext`.
**When to use:** Every page in this app, and specifically required by DEBLOAT-03 to fix the predecessor's `DebloatPage.xaml.cs` anti-pattern.
**Example (verified, full file):**
```csharp
// Source: src/AkariToolbox.App/Views/GamingTweaksPage.xaml.cs (verified, lines 1-17)
using Microsoft.UI.Xaml.Controls;
using AkariToolbox.App.ViewModels;

namespace AkariToolbox.App.Views;

public sealed partial class GamingTweaksPage : Page
{
    /// <summary>View model used by x:Bind bindings.</summary>
    public GamingTweaksViewModel ViewModel { get; }

    public GamingTweaksPage(GamingTweaksViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }
}
```
`DebloatPage.xaml.cs` should be a `DebloatViewModel`-parameterized copy of this exact shape.

### Pattern 2: One-shot action dispatch through `IScriptRunner` (DEBLOAT-02)

**What:** A `[RelayCommand]` async method that logs an accepted-risk line (if applicable), then awaits `RunEmbeddedScriptAsync`, catching `FileNotFoundException` specifically so a resource-name typo surfaces in the visible log dock instead of only an unobserved-task-exception handler.
**When to use:** Every Debloat Run/Undo button — this is the direct precedent for the entire ViewModel.
**Example:**
```csharp
// Source: src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs (verified, lines 165-184)
private async Task RunD06ScriptAsync(string displayName, string resourceSuffix)
{
    _log.Log($"[GAMING] Launching {displayName} — downloaded binary is NOT SHA256/signature-verified before execution (accepted risk, D-06).");

    try
    {
        await _scriptRunner.RunEmbeddedScriptAsync(resourceSuffix);
    }
    catch (FileNotFoundException ex)
    {
        // WR-04 fix (02-REVIEW.md): RunEmbeddedScriptAsync throws before entering
        // its own try/catch when the requested embedded resource can't be found,
        // bypassing the ILogConsoleService logging every other ScriptRunner failure
        // path uses. Catch it here so a mismatched resourceSuffix surfaces visibly.
        _log.Log($"[GAMING] ERROR: {displayName} failed to launch — {ex.Message}");
    }
}
```
For Debloat's 6 network-dependent replacement actions (Bloatware's UWP install/optional-feature calls don't hit the network directly, but the Edge & WebView / Edge Settings Undo branches download `edge.exe`/`edgewebview.exe`), this same pre-launch accepted-risk log line pattern applies (D-10).

### Pattern 3: Per-key run-concurrency guard (Claude's discretion item)

**What:** A `ConcurrentDictionary<string, SemaphoreSlim>` keyed by action key, so the same action can't be double-invoked from a double-click, while different actions can run concurrently (or not — see recommendation below).
**When to use:** DebloatViewModel's Run/Undo command implementations.
**Example:**
```csharp
// Source: src/AkariToolbox.App/Services/TweakCatalog.cs (verified, lines 1-19, 30-36, 56-62)
private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
// ...
var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
await gate.WaitAsync().ConfigureAwait(false);
try
{
    // ... run action ...
}
finally
{
    gate.Release();
}
```
**Recommendation:** Gate **per-action** (not a single page-wide lock) — this matches `TweakCatalog`'s exact pattern and lets independent actions (e.g., Disk Cleanup and Telemetry) run in parallel without blocking each other, while still preventing a double-click on the same row from spawning two concurrent PowerShell processes against the same registry keys. Expose a `bool IsRunning` per `DebloatActionItem` (bound to a `ProgressRing`/disabled-button state on the row) so the UI visibly reflects the gate without needing to inspect the semaphore directly.

### Anti-Patterns to Avoid

- **Reusing `ITweakHandler`/`ITweakCatalog` for Debloat:** D-01 is explicit that Debloat has no live-state read-back — `GetState()`/`SetState(bool)` doesn't fit a one-shot Disk Cleanup or a "remove OneDrive" action that has no meaningful on/off state. Building a fake `GetState()` that always returns `false` just to satisfy the interface would be worse than a distinct, purpose-built model.
- **Simulating `Read-Host` input via stdin piping to run a replacement script's original multi-branch menu unmodified:** Fragile, untested in this codebase, and unnecessary — the file-per-branch extraction technique is already proven twice in Phase 2.
- **Leaving a script's trailing `show-menu` call in an extracted branch:** Specific to `13 Bloatware.ps1` branches 2 and 4 (see Pitfall 1 below) — causes the embedded script to hang.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Streaming a PowerShell process's stdout/stderr to the UI without blocking | A new `Process`+`OutputDataReceived` wrapper | `IScriptRunner.RunProcessAsync`/`RunEmbeddedScriptAsync` | Already built, tested (`ScriptRunnerTests.cs`, `ScriptRunnerEmbeddedTests.cs` exist in the repo), and exactly satisfies DEBLOAT-02 |
| Extracting an embedded `.ps1` resource to a temp path and cleaning it up | A new extraction helper | `IScriptRunner.RunEmbeddedScriptAsync`'s built-in extract-run-delete lifecycle | Already handles GUID-suffixed temp naming and `finally`-block cleanup |
| Confirmation dialogs for destructive actions | A custom `ContentDialog` per call site | `IDialogService.ConfirmAsync` | Already XamlRoot-safe and serialized against overlapping requests |
| Per-action double-invocation guard | Manual `bool _isRunning` flags scattered per command | `ConcurrentDictionary<string, SemaphoreSlim>` (TweakCatalog pattern) | Proven pattern already in the same codebase, thread-safe by construction |

**Key insight:** This entire phase is additive on top of Phase 1/2 primitives — the risk isn't "missing infrastructure," it's script-extraction correctness (menu-loop stripping, preserving unconditional pre-menu setup lines) and getting the risk-classification/confirmation-dialog set right.

## Non-Interactive Extraction Technique

All three replacement scripts share the exact self-elevating/interactive shape already handled in Phase 2 (D-04/D-07 precedent). Every extracted branch script must:

1. **Drop the self-elevation block** (`If (!([Security.Principal.WindowsPrincipal]...` re-launch-as-admin check) — the app itself already runs elevated (APP-01), so this check would either silently no-op or (worse) attempt a second UAC prompt from inside an already-elevated `powershell.exe` child process.
2. **Drop the console cosmetic setup** (`$Host.UI.RawUI.WindowTitle`, `BackgroundColor`, `$Host.PrivateData.Progress*Color`, `Clear-Host`) — harmless but pointless when output is captured, not displayed in a console window.
3. **Keep the `Test-Connection -ComputerName "8.8.8.8"` internet check**, but **replace its `Pause; exit` failure path** with a plain `Write-Host "..."; exit 1` (or similar) — `Pause` internally calls `Read-Host` and will hang or throw when the process's stdin isn't an interactive console (as is the case for a `Process`-spawned child with `RedirectStandardInput` left at its default `false`, per [VERIFIED: src/AkariToolbox.Framework/Services/ScriptRunner.cs:20-30] — the `ProcessStartInfo` in `RunProcessAsync` sets `RedirectStandardOutput = true` and `RedirectStandardError = true` but never sets `RedirectStandardInput`).
4. **Keep the unconditional pre-menu setup line** that exists outside any branch — specific to `13 Bloatware.ps1` line 22 (`reg add ... DevicePasswordLessBuildVersion ... /d "0" /f` — allows password sign-in). This line executes on every invocation regardless of which menu option is chosen, so it must be included at the top of **both** `bloatware-remove.ps1` and `bloatware-installall.ps1`.
5. **Drop the `show-menu`/`Read-Host` loop wrapper** and every `switch ($choice) { ... }` scaffolding, keeping only the chosen `case` block's body.
6. **Replace a trailing `show-menu` call** (loops back into the interactive menu) **with a plain `exit`** or nothing (falling off the end of the script also exits). This only matters for `13 Bloatware.ps1` — see Pitfall 1.

### Branch-by-branch extraction map

| Source script | Branch | Line range (source) | Ends with | New embedded file | Used as |
|----------------|--------|---------------------|-----------|--------------------|---------|
| `13 Bloatware.ps1` | 2 ("Remove: All Bloatware") | [VERIFIED: 6 Windows/13 Bloatware.ps1:48-246] body starts `Clear-Host` (48), ends `show-menu` (246) — **must replace line 246's `show-menu` with `exit`** | `show-menu` (loop-back — MUST STRIP) | `bloatware-remove.ps1` | Run action #19 |
| `13 Bloatware.ps1` | 4 ("Install: All UWP Apps") | [VERIFIED: 6 Windows/13 Bloatware.ps1:318-329] body starts `Clear-Host` (318), ends `show-menu` (327) — **must replace line 327's `show-menu` with `exit`** | `show-menu` (loop-back — MUST STRIP) | `bloatware-installall.ps1` | Undo action #19 |
| `20 Edge & WebView.ps1` | 1 ("Uninstall") | [VERIFIED: 6 Windows/20 Edge & WebView.ps1:29-117] body starts `Clear-Host` (29), ends `exit` (117) | `exit` (safe, no change needed) | `edgewebview-uninstall.ps1` | Run action #22 |
| `20 Edge & WebView.ps1` | 2 ("Default") | [VERIFIED: 6 Windows/20 Edge & WebView.ps1:122-194] body starts `Clear-Host` (122), ends `exit` (194) | `exit` (safe, no change needed) | `edgewebview-default.ps1` | Undo action #22 |
| `10 Edge Settings.ps1` | 1 ("Optimize") | [VERIFIED: 3 Setup/10 Edge Settings.ps1:29-70] body starts `Clear-Host` (29), ends `exit` (70) | `exit` (safe, no change needed) | `edgesettings-optimize.ps1` | Run action #21 |
| `10 Edge Settings.ps1` | 2 ("Default") | [VERIFIED: 3 Setup/10 Edge Settings.ps1:75-100] body starts `Clear-Host` (75), ends `exit` (100) | `exit` (safe, no change needed) | `edgesettings-default.ps1` | Undo action #21 |

This matches the exact `.csproj` embedding pattern Phase 2 used for its D-06 scripts:
```xml
<!-- Source: src/AkariToolbox.App/AkariToolbox.App.csproj (verified, lines 33-50) -->
<ItemGroup>
  <!-- D-06 network-dependent one-shot scripts (02-CONTEXT.md), split one embedded resource
       per source menu branch and run via IScriptRunner.RunEmbeddedScriptAsync. Ported
       exactly as authored, with NO added SHA256/signature verification for v1 — explicit
       accepted user decision, not an oversight (see 02-06-PLAN.md, D-06). -->
  <EmbeddedResource Include="Resources\GamingScripts\driverclean-auto.ps1" />
  <EmbeddedResource Include="Resources\GamingScripts\driverclean-manual.ps1" />
  <!-- ... -->
</ItemGroup>
```
Phase 3 should add an analogous `<ItemGroup>` for `Resources\DebloatScripts\*.ps1` (25 direct-carry files + 6 branch-extracted files = 31 embedded `.ps1` resources total, since 3 of the 28 actions have no Undo: Disk Cleanup, Temp Files, O&O ShutUp10++ — 28 Run scripts + (28 − 3) = 25 Undo scripts = 53... **recompute precisely during planning**; the exact resource count is a planning-time detail, not a research blocker).

## Common Pitfalls

### Pitfall 1: `13 Bloatware.ps1`'s branches loop back into the menu instead of exiting

**What goes wrong:** If branch 2 or branch 4's body is embedded verbatim (including the trailing `show-menu` call), the extracted script will re-print the menu and enter `Read-Host " "` — which blocks forever (or throws, depending on console/stdin availability) because `IScriptRunner`'s spawned `powershell.exe` process has no redirected stdin.
**Why it happens:** The source script's branches are written to return control to an interactive menu loop for repeat use during a live console session — a design assumption that doesn't hold for a single, non-interactive invocation.
**How to avoid:** When authoring `bloatware-remove.ps1` and `bloatware-installall.ps1`, stop copying at the line immediately before `show-menu` and add a bare `exit` (or nothing) instead.
**Warning signs:** The Run/Undo command's `Task` never completes; `IScriptRunner.RunProcessAsync`'s `timeout` (if one is set) fires and logs `[TIMEOUT]`; if no timeout is set (per DEBLOAT-02's requirement not to block/freeze the UI, a timeout is strongly recommended for this specific action), the UI-visible "running" state for that row never clears.

### Pitfall 2: `Pause` in the shared internet-connectivity check hangs on a non-redirected-stdin child process

**What goes wrong:** All three replacement scripts share an identical internet-check block (`if (!(Test-Connection ...)) { Write-Host ...; Pause; exit }`). `Pause` calls `$null = Read-Host`, which will block or throw when there's no interactive stdin.
**Why it happens:** Same root cause as Pitfall 1 — the source scripts assume an interactive console session.
**How to avoid:** Strip `Pause` from the extracted branch's pre-flight failure path (keep `Test-Connection`'s check itself; it's a reasonable pre-flight guard since branch 2 of Edge & WebView and branch 2 of Edge Settings download installer binaries).
**Warning signs:** Any of the 6 extracted scripts hangs specifically on a machine with no internet connectivity (harder to catch in testing if the dev machine is always online — explicitly test with connectivity disabled, or at minimum code-review the extracted files for a lingering `Pause`).

### Pitfall 3: `13 Bloatware.ps1`'s pre-menu setup line is easy to drop by accident

**What goes wrong:** [VERIFIED: 6 Windows/13 Bloatware.ps1:21-22] `# ALLOW PASSWORD SIGN IN` / `cmd /c "reg add ... DevicePasswordLessBuildVersion ... /d \`"0\`" /f >nul 2>&1"` runs unconditionally, before `show-menu` is even called — it is not inside any numbered `case` block. A naive extraction that copies only the `case 2 { ... }` or `case 4 { ... }` body will silently omit this side effect.
**Why it happens:** The line lives in the script's top-level (pre-menu) scope, easy to overlook when scanning for `case N {` blocks to copy.
**How to avoid:** Explicitly include this line at the top of both `bloatware-remove.ps1` and `bloatware-installall.ps1` (or decide, as an explicit planning-time call, that it's out of scope for the port and document the deviation — but silently dropping it is the failure mode to avoid).
**Warning signs:** Behavioral diff vs. the original script if ever compared side-by-side; otherwise silent (this specific tweak has no obviously visible symptom, which is exactly why it's easy to miss).

### Pitfall 4: `20 Edge & WebView.ps1` branch 1 writes a relative-path file (`./reg1.exe`) in the process's current working directory

**What goes wrong:** [VERIFIED: 6 Windows/20 Edge & WebView.ps1:37,113,115] `Copy-Item (Get-Command reg.exe).Source .\reg1.exe -Force -EA 0` creates a copy of `reg.exe` in whatever directory is the PowerShell process's current working directory at launch — not a fixed, known path. `IScriptRunner.RunEmbeddedScriptAsync` extracts the script to `%TEMP%\AkariToolbox-{guid}-{name}` and invokes `powershell.exe -File <temp>`, but does **not** explicitly set `ProcessStartInfo.WorkingDirectory` [VERIFIED: src/AkariToolbox.Framework/Services/ScriptRunner.cs:20-30 — no `WorkingDirectory` property is set in the `ProcessStartInfo` initializer], so the child process inherits the parent app's working directory (typically the app's own install/publish directory).
**Why it happens:** `.\reg1.exe` is a relative path in the original script, written for a console session run from a user-chosen folder; it was never designed to run from an app's install directory.
**How to avoid:** Port this line as-authored (D-06 requires "ported as-authored in full" for these replacement scripts — do not silently rewrite to an absolute temp path without a fresh decision), but flag this at plan-review time: writing/deleting a file in the app's own install directory under `Program Files`-style paths generally works fine when elevated (the app already runs `requireAdministrator`, per APP-01), but is worth a manual smoke test rather than assuming it's inert.
**Warning signs:** If the app is ever published to a read-only or write-restricted location, this specific branch (Edge & WebView Run/"Uninstall") would fail with an access-denied writing `reg1.exe`, while every other action on the page continues to work fine — an isolated, easy-to-misdiagnose failure.

### Pitfall 5: The three replacement scripts' network dependency is new for the Debloat page

**What goes wrong:** [per CONTEXT.md `<specifics>`] All three replacement scripts hard-require internet connectivity (`Test-Connection 8.8.8.8`) even for their removal/uninstall branches — the predecessor's `Debloat.ps1`/`RemoveEdge.ps1`/`EdgeDebloat.ps1` were local-only. A user with no internet connection who clicks "Unwanted Apps — Remove" will now hit the connectivity pre-flight check and the action will abort (assuming Pitfall 2 is fixed so it aborts cleanly instead of hanging).
**Why it happens:** `13 Bloatware.ps1` branch 2 itself is local-only (no downloads in its actual removal logic) but inherits the script-wide connectivity gate written for the script's other branches (7, 8 which do download installers).
**How to avoid:** Not a code fix — a UX consideration for the planner: surface a clear log/dialog message when the connectivity check fails ("Unwanted Apps — Remove requires an internet connection") rather than a bare non-zero exit code with no context.
**Warning signs:** UAT on an offline VM (a stated project pattern — "important for fresh installs and VM environments" per CLAUDE.md) would surface this immediately; worth an explicit UAT check.

## Code Examples

### DebloatAction model (proposed shape)

```csharp
// Proposed — no existing file to cite; modeled directly on the catalog table above
// and on ITweakHandler's Category discriminator pattern (src/AkariToolbox.App/Services/ITweakHandler.cs:33)
public sealed record DebloatAction(
    string Key,
    string Title,
    string Description,
    string Category,
    string RunResourceSuffix,
    string? UndoResourceSuffix,
    bool RequiresConfirmation);
```

### Confirmation-gated run command (proposed shape, composing verified primitives)

```csharp
// Proposed composition of two verified primitives:
//   - IDialogService.ConfirmAsync (src/AkariToolbox.Framework/Services/IDialogService.cs:20-25)
//   - IScriptRunner.RunEmbeddedScriptAsync (src/AkariToolbox.Framework/Services/ScriptRunner.cs:109-136)
private async Task RunActionAsync(DebloatAction action, string resourceSuffix, bool isUndo)
{
    if (action.RequiresConfirmation)
    {
        var confirmed = await _dialogService.ConfirmAsync(
            action.Title,
            $"This action makes system changes that may be difficult to reverse. Continue with \"{action.Title}\"{(isUndo ? " (Undo)" : "")}?");
        if (!confirmed)
        {
            return;
        }
    }

    var gate = _locks.GetOrAdd(action.Key, _ => new SemaphoreSlim(1, 1));
    await gate.WaitAsync().ConfigureAwait(false);
    try
    {
        _log.Log($"[DEBLOAT] Running: {action.Title}{(isUndo ? " (Undo)" : "")}");
        await _scriptRunner.RunEmbeddedScriptAsync(resourceSuffix);
    }
    catch (FileNotFoundException ex)
    {
        _log.Log($"[DEBLOAT] ERROR: {action.Title} failed to launch — {ex.Message}");
    }
    finally
    {
        gate.Release();
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| Predecessor's `ToolService.RunAction`/`RunWithTracking` — a `switch` over a `RunAction` discriminated-union type (`ScriptAction`/`CommandAction`/`UrlAction`), with a write-only `_applied` tracking list | `IScriptRunner.RunEmbeddedScriptAsync` — direct embedded-resource extraction and process execution, no `_applied` tracking (confirmed dead code, D-01) | Phase 1/2 of this port | Simpler surface area; DEBLOAT-01/02 are satisfied without reimplementing the predecessor's action-type polymorphism, since every Debloat action in scope is a script, never a `CommandAction` (winget) or `UrlAction` (predecessor's O&O ShutUp10++ used `RunProcess` directly per its own script, not a `UrlAction`) |
| Predecessor's `DebloatPage.xaml.cs` `BuildGroup` — imperative C# UI construction with closures capturing `capturedScript`/`capturedUndo` strings, calling `Service.RunWithTracking(...)` directly from the button's `Click` handler | Declarative XAML + ViewModel command binding (`[RelayCommand]`), catalog-driven `ObservableCollection` | This phase (DEBLOAT-03) | Fixes the architecture-debt callout in PROJECT.md; testable ViewModel logic instead of UI-event-handler logic |

**Deprecated/outdated:**
- Predecessor's `Debloat.ps1`/`Debloat-Undo.ps1` (hardcoded ~29-app allow-list removal/reinstall) — replaced by D-03/D-05's broader exclusion-list approach from `13 Bloatware.ps1`.
- Predecessor's `RemoveEdge.ps1` and `EdgeDebloat.ps1`/`EdgeDebloat-Undo.ps1` — replaced by D-06/D-08's `20 Edge & WebView.ps1`/`10 Edge Settings.ps1`.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|----------------|
| A1 | Proposed confirmation-required set (BitLocker, Bloatware, Edge & WebView Remove, Hibernation, OneDrive Remove) beyond D-11's 4 named examples — specifically adding OneDrive Remove is this research's own judgment call, not a locked decision | Proposed Risk Classification | If wrong, OneDrive Remove either gets an unwanted extra click-through (low cost) or, if the reasoning is rejected, needs to be removed from the confirmation set during planning — either way, a cheap, easily-reversible UI-only change, not an architectural risk |
| A2 | The exact embedded-resource count ("31 total .ps1 files") in the Non-Interactive Extraction Technique section is a rough estimate based on 28 Run + 25 Undo actions, not independently re-verified against the final catalog | Branch-by-branch extraction map | Low risk — the planner will recompute this exactly when authoring the `.csproj` `<EmbeddedResource>` list; flagged explicitly in-line as "recompute precisely during planning" |
| A3 | Writing `.\reg1.exe` to the app's working directory (Pitfall 4) will succeed under `requireAdministrator` elevation without further mitigation, based on general Windows ACL behavior for admin-elevated processes rather than a test run against this specific app's actual publish directory | Common Pitfalls, Pitfall 4 | If wrong (e.g., if the publish directory somehow ends up read-only despite elevation, or antivirus/EDR flags a `reg.exe` copy operation), only the Edge & WebView "Uninstall" Run action fails — isolated blast radius, easily caught by manual UAT of that specific action |

## Open Questions

1. **Does the Debloat page need a `ProgressRing`/spinner or can the existing `ILogConsoleService`-fed log dock alone satisfy DEBLOAT-02's "streamed status/output feedback" requirement?**
   - What we know: `ILogConsoleService` is the proven, already-wired feedback surface (Phase 1/2 use it exclusively for exactly this purpose).
   - What's unclear: Whether the UI/UX for this specific page wants a per-row busy indicator in addition to the shared log dock (the CONTEXT.md leaves "busy/running indicator" as Claude's discretion).
   - Recommendation: Add a lightweight per-row `IsRunning`-bound visual (disable the Run/Undo buttons for that row, optionally show a small spinner) — cheap to build given the per-action `SemaphoreSlim` gate already tracks this state, and it directly satisfies "without the UI freezing or crashing" by making it visually obvious the action is in flight rather than looking unresponsive.

2. **Should `13 Bloatware.ps1`'s pre-menu `DevicePasswordLessBuildVersion` registry write (Pitfall 3) be preserved, or is it out of scope for a "debloat" action to also silently touch sign-in/password policy?**
   - What we know: The line exists unconditionally in the source script and D-03 says "ported as-authored in full."
   - What's unclear: Whether "as-authored in full" was intended to cover this pre-menu setup line specifically, or only the numbered branch's own body — the discussion's canonical_refs point at "branch 2" and "branch 4" specifically, which technically excludes line 22.
   - Recommendation: Preserve it (matches the strictest reading of "as-authored in full" and D-05's note that this is a deliberately broader, not-fully-symmetric port) — but flag this as a one-line decision point for the planner/user to confirm rather than assuming silently either way.

## Environment Availability

Skipped — this phase has no new external tool/service/runtime dependencies beyond what Phase 1/2 already established (`powershell.exe` availability, which every prior phase already depends on and which is a standard Windows component).

## Validation Architecture

Skipped — `workflow.nyquist_validation` is explicitly `false` in `.planning/config.json`.

## Security Domain

`security_enforcement` is `true` (ASVS Level 1, block on `high`) in `.planning/config.json` — included per policy.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|-------------------|
| V2 Authentication | No | No authentication surface in this phase — app-level elevation (APP-01) is the only privilege boundary, already established |
| V3 Session Management | No | N/A |
| V4 Access Control | No | Single-user, elevated desktop app — no multi-user access control model |
| V5 Input Validation | Marginal | No user-supplied input drives script selection (catalog is static, compiled-in data) — the only "input" is the user's click on a known, fixed set of 28 buttons; no injection surface into the PowerShell invocation (`RunEmbeddedScriptAsync` takes a fixed `resourceSuffix` string literal per call site, never user-typed text) |
| V6 Cryptography | Marginal | No new crypto in this phase; the *absence* of SHA256 verification on downloaded Edge/WebView installer binaries (D-10) is an accepted, already-decided risk carried forward from Phase 2's identical precedent — not introduced fresh here |

### Known Threat Patterns for this phase's stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|----------------------|
| Elevated process running admin-supplied but unverified downloaded binaries (`edge.exe`, `edgewebview.exe` from a third-party GitHub mirror, `github.com/FR33THYFR33THY/Ultimate-Files`) | Tampering | Not mitigated in v1 — explicit accepted risk (D-10), consistent with Phase 2's D-06 precedent. Standard mitigation (SHA256/signature verification before execution) is documented as the correct control in CLAUDE.md's Stack Patterns section ("Compute SHA256 over the downloaded stream ... before moving it into place") but deliberately not applied here per explicit user decision — this is a **known, accepted gap**, not an oversight, and should be recorded as such (not silently normalized) |
| Destructive, hard-to-reverse system mutation triggered by a single accidental click (BitLocker disable, broad Bloatware removal, Edge/WebView removal, Hibernation disable, OneDrive removal) | Repudiation / (user-facing) irreversible-action risk, not a classic STRIDE-security threat but the app's own stated core value ("safely revertible") | D-11's confirmation-dialog requirement, implemented via `IDialogService.ConfirmAsync` — see Proposed Risk Classification above for the exact 5-action set |
| Embedded script extracted to a world-readable temp path (`%TEMP%\AkariToolbox-{guid}-{name}.ps1`) during execution | Information Disclosure (low severity — scripts contain no secrets, only tweak logic already visible in the shipped binary's embedded resources) | Already mitigated by the existing GUID-suffixed naming and `finally`-block deletion in `ScriptRunner.RunEmbeddedScriptAsync` — no new work needed |

## Sources

### Primary (HIGH confidence — direct Read of this repo / predecessor repo / canonical script files this session)

- `C:/Users/isleap/Documents/GitHub/Akari-Toolbox/.planning/phases/03-debloat/03-CONTEXT.md` — phase decisions, canonical refs
- `C:/Users/isleap/Documents/GitHub/Akari-Toolbox/.planning/REQUIREMENTS.md` — DEBLOAT-01/02/03 exact wording
- `C:/Users/isleap/Documents/GitHub/Akari-Toolbox/.planning/STATE.md` — project history/decisions
- `C:/Users/isleap/Documents/GitHub/Akari-Toolbox/.planning/phases/02-gaming-tweaks/02-CONTEXT.md` — D-04/D-07 extraction precedent, D-06 accepted-risk precedent
- `C:/Users/isleap/Documents/GitHub/Akari-Toolbox/.planning/config.json` — `nyquist_validation: false`, `security_enforcement: true`
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Views/DebloatPage.xaml.cs` — full 29-button catalog, code-behind anti-pattern
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Views/DebloatPage.xaml` — button/card styling reference
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ToolService.cs` — action-routing shape, confirms `_applied` is write-only
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Scripts/*.ps1` directory listing — confirmed all 25 direct-carry script pairs exist on disk
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/13 Bloatware.ps1` — full read, all 9 branches
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/20 Edge & WebView.ps1` — full read, both branches
- `C:/Users/isleap/Desktop/AkariOS Tweaks/3 Setup/10 Edge Settings.ps1` — full read, both branches
- `src/AkariToolbox.Framework/Services/IScriptRunner.cs`, `ScriptRunner.cs` — full read
- `src/AkariToolbox.Framework/Services/ILogConsoleService.cs` — full read
- `src/AkariToolbox.Framework/Services/IDialogService.cs` — full read
- `src/AkariToolbox.App/Services/TweakCatalog.cs`, `ITweakHandler.cs` — full read
- `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs` — full read (closest existing analog)
- `src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs` — full read (handler-authoring conventions reference)
- `src/AkariToolbox.App/Views/GamingTweaksPage.xaml.cs` — full read (zero-logic code-behind pattern)
- `src/AkariToolbox.App/AkariToolbox.App.csproj` — full read (embedded-resource convention)
- `src/AkariToolbox.App/ViewModels/HomeViewModel.cs` (grep, line 37) — confirmed "Run 28 PowerShell-backed debloat actions" wording
- `src/AkariToolbox.App/MainWindow.xaml.cs` (grep) — nav-entry pattern confirmed for Gaming Tweaks, same wiring point applies to Debloat
- `src/AkariToolbox.App/App.xaml.cs` (grep) — DI registration pattern (`AddTransient<GamingTweaksViewModel>()`) — same pattern applies to `DebloatViewModel`

### Secondary (MEDIUM confidence)

None — no web/external documentation lookups were needed for this phase (zero new packages, entirely in-repo/predecessor-repo research).

### Tertiary (LOW confidence)

None.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages, 100% reuse of already-verified Phase 1/2 primitives, confirmed by direct source reads
- Architecture: HIGH — `GamingTweaksViewModel`/`GamingTweaksPage.xaml.cs` is a near-exact structural analog, confirmed by direct source reads
- Pitfalls: HIGH for extraction mechanics (all 6 branch scripts fully read, line-cited); MEDIUM for the working-directory (`reg1.exe`) pitfall's real-world impact, since it wasn't tested against the actual publish output — documented as Assumption A3

**Research date:** 2026-09-01
**Valid until:** No expiry driver — this research is grounded in this repo's own code and two local script files supplied directly by the user, not external library/API state that could drift. Re-research only if the canonical script files at `C:\Users\isleap\Desktop\AkariOS Tweaks\` are edited, or if `IScriptRunner`/`ILogConsoleService`/`IDialogService` signatures change before planning.
