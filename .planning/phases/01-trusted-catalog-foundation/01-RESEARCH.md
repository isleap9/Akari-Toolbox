# Phase 01: Trusted Catalog Foundation — Research

**Researched:** 2026-10-04
**Domain:** WinUI 3 catalog UI (Winhance-style rows) + native Windows tweak descriptors with per-row instant apply/revert
**Confidence:** HIGH

## Summary

Phase 1 rebuilds the gaming-tweak catalog as Winhance-style rows: each FR33THY gaming tweak (8–12 rows, gaming set first) renders as a scannable card — title + one-line gamer-language description + live badge pills + live control — with a per-row Recommended/Default quick-set pair and full technical detail (registry footprint, current/recommended/default) behind a collapsed expander. The apply model is instant per-row toggles with live-apply; the attempt-1 bulk tick-then-apply model is dead and its code fully reverted.

The codebase already contains every building block: the `TweakToggle` read/apply pairing, the `CpRowItem` item-command-in-DataTemplate pattern (the exact pattern the new rows need), the canonical per-command run shape (`IsBusy` guard → `StatusService.Start` → `Task.Run` → InfoBar → `Complete` in `finally`), live read-back on every navigation, `RestorePointActions.CreateRestorePoint`, `AkariToolState` persistence, and shared registry bodies in `ControlPanelRows` to cross-reference instead of duplicating. No new NuGet packages are needed. The one genuinely new UI element is the WinUI 3 `Expander` for Technical-details — a first-party control in Windows App SDK, confirmed available via Microsoft Learn.

**Primary recommendation:** Define a new `GamingTweakDescriptor` record (Recommended + Default values, one-liner, technical footprint, reboot flag, shared-row cross-reference) beside `TweakToggle`; render rows with the `CpRowItem` item-command pattern plus a WinUI 3 `Expander` for details; apply per-row instantly off the UI thread with a row-scoped first-capture-wins journal for revert; offer a restore point on first apply with a Skip path.

## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-00:** Build something like Winhance where the user optimizes Windows and enhances it with FR33THY Ultimate, which is the best for gaming. Winhance-style browse and per-tweak detail; FR33THY Ultimate as the optimization content backbone.
- **D-01:** Instant per-row toggles with live-apply. No tick-then-bulk-apply staging model — the staging state caused the attempt-1 revert-baseline bug class and contradicts the Winhance reference.
- **D-02:** SFT-01 must be rewritten: "bulk-apply with progress and revert all with one click" no longer describes the product. New shape: per-row instant apply with per-row revert, plus a restore-point offer on first apply (not a gate). Planner flags the REQUIREMENTS.md edit; researcher confirms no other requirement depends on bulk wording.
- **D-03:** Gaming set first: Game Mode/DVR, network latency path, GPU scheduling, background control (8–12 rows). Full-Windows categories (privacy, updates, power, telemetry) are later phases, reusing the proven row pattern.
- **D-04:** Every row follows `.planning/research/winhance-patterns.md` §6: title + ONE description line + badge pills + live control on the right. No always-visible paragraph stacks — that was attempt 1's exact failure (user UAT: "wanted something like Winhance, easy to read and understand").
- **D-05:** The one-liner carries what-it-does in plain gamer language (Winhance copy style: "Optimize your PC for play by turning things off in the background"). What/why/risk/reverts detail lives behind the collapsed Technical-details expander, not on the card face.
- **D-06:** Every tweak definition carries Recommended + Default values and exposes the per-row green "Set to Recommended" / grey "Set to Default" quick-set pair with tooltips naming the target value.
- **D-07:** Badge pills show live state: Recommended / Default / Custom (diverged) + restart-required where applicable. Pills reflect reality on every navigation (live read-back), never cached optimism.
- **D-08:** Revert is per-row (back to that row's prior/Default values), instant like apply. The attempt-1 snapshot journal does NOT return in bulk-prefix form; if a journal is needed for toggle semantics, the planner designs it row-scoped.
- **D-09:** Restore point is offered on first apply of the session with a Skip path and never blocks. Fully-reversible line and native-ops-only constraint stand unchanged.
- **D-10:** Catalog seeds from current gaming rows plus Game Mode and DVR only — later-phase rows are not pulled forward.
- **D-11:** The catalog lives on a new dedicated page in nav, grouped System / Network / GPU / Background.
- **D-12:** Rows cross-reference the shared definitions behind the 169 existing Individual Tweaks rows instead of duplicating registry text.
- **D-13:** Reboot-required rows show a badge in place; never segregated, never blocking.
- **D-14:** Search matches row titles only (not descriptions or keys), suggests from the first character, prefix-first ranking, tap-to-filter without applying.

### The Agent's Discretion

- Descriptor schema shape (extend TweakToggle vs new SettingDefinition-style record) — planner's call within D-04/D-06.
- Journal-vs-direct-revert mechanics for toggle semantics (D-08) — researcher + planner's call, must preserve the fully-reversible line.
- Suggestion dropdown control choice and empty-state copy — within D-14.
- Whether the quick-set pair also needs a page-level "apply recommended to all" — explicitly undecided, planner may propose but must not silently add bulk scope.

### Deferred Ideas (OUT OF SCOPE)

- Full-Windows categories (privacy, updates, power, telemetry) — later phases reusing this phase's row pattern (D-03).
- Page-level "apply recommended to all" — undecided, explicitly not silent scope.
- Game profiles/presets, config export/import, auto gaming mode — already in REQUIREMENTS.md v2/out-of-scope; unchanged.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| BRW-01 | User can find any gaming tweak by search within a categorized catalog | Title-only prefix-first search algorithm (carried forward from attempt 1, tests passed); grouped page composition; `AutoSuggestBox`/TextBox + suggestion dropdown guidance |
| BRW-02 | User can read a per-tweak explanation (what it does, why it helps gaming, risk, how it reverts) before toggling | One-liner + Technical-details `Expander` pattern from Winhance extraction; descriptor fields for explanation metadata |
| SFT-01 | STALE — "User can bulk-apply gaming tweaks with progress and revert all of them with one click" | Must be rewritten per D-02 (see SFT-01 Bulk-Wording Dependency Check below); revert mechanics research (row-scoped journal vs direct revert) |

### SFT-01 Bulk-Wording Dependency Check (per D-02)

Searched `.planning` for `bulk` this session. Findings:

- `REQUIREMENTS.md` SFT-01 itself (line 31) — the stale text to rewrite. [VERIFIED: .planning/REQUIREMENTS.md:31]
- `REQUIREMENTS.md` SFT-02 (line 59): "System restore point is created before bulk changes" — references bulk changes; needs conforming edit when SFT-01 is rewritten (e.g. "before applying tweaks"). [VERIFIED: .planning/REQUIREMENTS.md:59]
- `REQUIREMENTS.md` out-of-scope presets row (line 69): "Requires bulk apply to exist first" — a v1.x candidate rationale, not a v1 dependency; no v1 requirement depends on bulk wording. Recommend leaving it (it correctly describes a future dependency) or rewording to "requires multi-tweak apply to exist first" at the planner's discretion. [VERIFIED: .planning/REQUIREMENTS.md:69]
- `ROADMAP.md` lines 5, 16 and `PROJECT.md` ("Trusted-control v1 bar… bulk-apply with progress") use bulk language — grep-observed, planner must update these alongside the SFT-01 rewrite so docs stay consistent. No requirement *depends* on them; they are descriptions to conform, not blockers.
- Archived attempt-1 artifacts under `.planning/phases/archive/01-trusted-catalog-foundation-attempt1/` — history only, not normative.

**Conclusion:** No v1 requirement besides SFT-01 itself depends on bulk wording. SFT-02's "bulk changes" phrase and the ROADMAP/PROJECT descriptions need conforming edits. Recommended SFT-01 rewrite: "User can apply each gaming tweak instantly from its row and revert that row to its prior values with one click; a restore point is offered on first apply."

## Project Constraints (from AGENTS.md)

- **Platform:** Windows 10/11 x64, administrator required — every tweak writes HKLM / services / drivers.
- **Packaging:** Unpackaged (`WindowsPackageType=None`), self-contained (`WindowsAppSDKSelfContained=true`).
- **Stack:** .NET 10 (`net10.0-windows10.0.26100.0`), Windows App SDK 2.3.1, CommunityToolkit.Mvvm 8.4.2 — pinned in `Directory.Packages.props`. No new packages needed for this phase.
- **Safety:** Fully reversible line — restore point + per-tweak rollback; never break updates, Store, or core drivers.
- **Native ops only:** No scripts or bundled binaries ever executed — `TweakAction` hierarchy only.
- **Build bar:** `Errors: 0 Warnings: 0`; kill `AkariToolbox.App` before rebuild (apphost lock).
- **XAML:** Page-level `{Binding}` (page-level x:Bind dies with WMC9999); x:Bind only in DataTemplate with `x:DataType`; `SystemInfo` WMI reads must dispose collections + objects and never run on the UI thread without `Task.Run`.
- **Conventions:** Item commands live on item classes (no `RelativeSource AncestorType`); `[ObservableProperty]` requires `partial` class; `_loading` guard during hydration; `On<Prop>Changed` partials for derived state; `Safe`/`SafeNullable` wrappers for live-state probes; per-command `try/catch` reporting via `IInfoBarService` + `IStatusService.Start/Complete`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Tweak row rendering (card, pills, expander) | Desktop client (WinUI 3 view) | — | Single-tier desktop app; all UI is WinUI XAML in `Views/` |
| Live-state read-back (registry/service probes) | Desktop client (Tweaks layer) | — | `RegRead`/`*Actions.Read*` probe HKLM/HKCU/services directly; must run off UI thread |
| Instant apply + per-row revert | Desktop client (ViewModel + Tweaks layer) | OS (registry/services) | VM orchestrates; `TweakAction.Apply()` mutates OS state natively |
| Restore-point offer | Desktop client (VM) + OS (WMI SystemRestore) | — | `RestorePointActions` via WMI; slow — background thread, Skip path mandatory |
| Search / filter / grouping | Desktop client (ViewModel) | — | In-memory filter over 8–12 descriptors; no data layer involved |
| Toggle memory (`HKCU\Software\AkariTool`) | OS registry via `AkariToolState` | — | Persisted choice memory, not source of truth — live read-back always wins |

## Standard Stack

### Core (all already in repo — no installs)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Microsoft.WindowsAppSDK (WinUI 3) | 2.3.1 | `Expander`, `ToggleSwitch`, `AutoSuggestBox`, `InfoBar`, grouped `ListView`/`ItemsView` | Pinned app platform; `Expander` is a first-party WinUI 3 control [CITED: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.expander] |
| CommunityToolkit.Mvvm | 8.4.2 | `[ObservableProperty]` / `[RelayCommand]` for VM + row item classes | Established codebase pattern; item classes must be `partial ObservableObject` |
| Microsoft.Extensions.Hosting / DI | 10.0.11 | Page + VM registration in `App.BuildHost` | New page/VM registers alongside the 13 existing ones [VERIFIED: src/AppTemplate.App/App.xaml.cs:147-175] |
| System.Management | 10.0.12 | Restore-point creation via `\\.\ROOT\DEFAULT:SystemRestore` | Already used by `RestorePointActions`; dispose `ManagementClass`/params/result objects [VERIFIED: src/AppTemplate.App/Tweaks/RestorePointActions.cs:28-37] |

### Supporting (in-box .NET, no packages)

| Library | Purpose | When to Use |
|---------|---------|-------------|
| `Microsoft.Win32.Registry` | All tweak reads/writes via `TweakAction` + `RegRead` | Every row apply, revert, and live probe |
| `System.Diagnostics.Process` | `RunProcessAction` for bcdedit/netsh/DISM-backed rows | Multi-value rows that shell system tools (hidden, waited) |
| `IDialogService` / `IInfoBarService` / `IStatusService` | First-apply restore-point offer, per-row success/failure, progress footer | Every run site; dialogs serialize via `SemaphoreSlim`, status ref-counts via `Interlocked` |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| WinUI 3 `Expander` for Technical-details | Hand-rolled show/hide `StackPanel` + chevron button | Custom collapsible breaks keyboard/Narrator behavior Winhance gets free; `Expander` is built-in — use it |
| New `GamingTweakDescriptor` record | Extend `TweakToggle` with Recommended/Default fields | `TweakToggle` is a positional 6-arg constructor used by 32 catalog entries; extending it churns unrelated code. A new record beside it keeps the gaming catalog independent — recommended |
| `AutoSuggestBox` for search | `TextBox` + `ListView` popup (attempt-1 approach) | Attempt-1's search half worked and its ranking tests passed; either satisfies D-14. `AutoSuggestBox` gives suggestion plumbing free but needs styling to match row cards — planner's call within discretion |

**Installation:**
```bash
# No new packages. Nothing to install.
```

## Package Legitimacy Audit

No external packages are installed by this phase. All controls (`Expander`, `ToggleSwitch`, `AutoSuggestBox`) ship in the pinned Windows App SDK 2.3.1; all logic uses in-box .NET + already-pinned dependencies. Audit: N/A — nothing to verify.

**Packages removed due to SLOP verdict:** none
**Packages flagged as suspicious (SUS):** none

## Architecture Patterns

### System Architecture Diagram

```text
User types/clicks
      │
      ▼
┌─────────────┐   OnNavigatedTo: live read-back per row    ┌──────────────────┐
│ Catalog Page │◄──────────────────────────────────────────│ Registry/Services │
│ (Views/)     │                                            │ (OS state)        │
│  search box  │   toggle flip / quick-set click            └──────────────────┘
│  group cards │──────────┐                                              ▲
│  row cards   │          ▼                                              │ TweakAction.Apply()
│  └ Expander  │   ┌─────────────┐  Task.Run (off UI thread)  ┌──────────┴──────┐
└─────────────┘   │ Row Item VM  │───────────────────────────►│ GamingTweak-    │
       ▲          │ (commands on │  read-back → badge pills   │ Descriptor      │
       │          │  item class) │◄───────────────────────────│ + *Actions      │
  {Binding}       └──────┬──────┘   failure → rollback +      └─────────────────┘
  page-level      InfoBar/Status   InfoBar + control rollback         │
  x:Bind only     pairing                 │                           ▼
  in DataTemplate                         │                  ┌──────────────────┐
                                          └─────────────────►│ Row-scoped       │
                                          first-apply offer  │ journal (prior   │
                                          (Skip path)        │ values) + HKCU   │
                                                              │ AkariTool memory │
                                                              └──────────────────┘
```

A reader traces the primary use case: user flips a row toggle → item command fires → `Task.Run` applies native ops → live read-back refreshes badge pills → InfoBar confirms; first apply of the session offers a restore point with Skip.

### Recommended Project Structure

```text
src/AppTemplate.App/
├── Tweaks/
│   ├── GamingTweakDescriptor.cs   # NEW: record (key, title, one-liner, why/risk/reverts,
│   │                              #   recommended/default values, registry footprint,
│   │                              #   requiresReboot, sharedRowIds, read/apply delegates)
│   ├── GamingCatalog.cs           # NEW: 8–12 seed descriptors (gaming rows + Game Mode + DVR),
│   │                              #   cross-referencing ControlPanelRows via SharedRowId (D-12)
│   ├── RowRevertJournal.cs        # NEW (if journal chosen): row-scoped first-capture-wins
│   │                              #   prior-value store; D-08 forbids bulk-prefix form
│   ├── GamingActions.cs           # EXTEND only if a seed row needs a new native op
│   └── (TweakToggle.cs, AkariTweakCatalog.cs, ControlPanelRows*.cs, RestorePointActions.cs — reuse, don't touch)
├── ViewModels/
│   └── GamingCatalogViewModel.cs  # NEW: search filter + groups + per-row apply/revert +
│                                  #   first-apply restore offer; row items beside it (CpRowItem pattern)
├── Views/
│   └── GamingCatalogPage.xaml(.cs)# NEW: search + grouped rows + Expander details; page-level {Binding}
```

Register the new page + VM in `App.BuildHost` alongside the 13 existing pairs [VERIFIED: src/AppTemplate.App/App.xaml.cs:147-175] and add a nav entry to `MainWindow.NavItems` after Gaming Tweaks [VERIFIED: src/AppTemplate.App/MainWindow.xaml.cs:82-96].

### Pattern 1: Descriptor with Recommended/Default + footprint

**What:** A UI-free record per tweak carrying everything the row renders: identity, copy, target values, technical footprint, reboot flag, shared-row cross-reference, and live `read`/`apply` delegates.
**When to use:** Every catalog row; keeps copy, values, and ops in one testable place (dotnet-testable, no UI types).
**Example:**
```csharp
// Shape recommendation — field set derived from winhance-patterns.md §§1-3 + D-05/D-06.
// TweakToggle's positional 6-arg form (key, title, description, stateKey, read, apply)
// has no slots for Recommended/Default/footprint — hence a new record, not an extension.
public sealed record GamingTweakDescriptor(
    string Key,                 // stable id, e.g. "game-mode"
    string Title,               // e.g. "Game Mode"
    string OneLiner,            // gamer language, ONE sentence (D-05)
    string WhyItHelps,          // expander: why it helps gaming (BRW-02)
    string Risk,                // expander: risk (BRW-02)
    string HowItReverts,        // expander: how it reverts (BRW-02)
    string RecommendedLabel,    // tooltip text naming the recommended target (D-06)
    string DefaultLabel,        // tooltip text naming the default target (D-06)
    IReadOnlyList<RegFootprint> Footprint,  // expander: path + value/type + current/rec/def
    bool RequiresReboot,        // in-place badge, never segregated (D-13)
    string[] SharedRowIds,      // cross-refs into ControlPanelRows.All (D-12)
    Func<bool> Read,            // live probe — source of truth for pills (D-07)
    Action<bool> Apply);        // instant apply; revert = row-scoped journal or Default
```

### Pattern 2: Item commands on the row class (DataTemplate binding)

**What:** Commands live on the row item class because WinUI `DataTemplate` has no page-ancestor binding; the page uses `{Binding}`, `x:Bind` only inside the template with `x:DataType`.
**When to use:** Every interactive element in the row — toggle, Recommended/Default buttons, expander content.
**Example:**
```csharp
// Source: in-repo pattern, TweaksViewModel.cs CpRowItem (lines 232-290) — the exact
// shape to mirror: command delegates to a VM-owned runner, _isLoading guard, refresh.
public sealed partial class CatalogRowItem : ObservableObject
{
    [ObservableProperty] private bool _isOn;
    [ObservableProperty] private bool _isBusy;
    public bool IsEnabled => !IsBusy;

    [RelayCommand]
    private Task SetRecommendedAsync() => _apply(this, Target.Recommended);
    [RelayCommand]
    private Task SetDefaultAsync() => _apply(this, Target.Default);

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));

    public void RefreshFromSystem()
    {
        try { IsOn = Descriptor.Read(); }
        catch { IsOn = AkariToolState.GetInt("Catalog:" + Descriptor.Key, 0) == 1; }
    }

    partial void OnIsOnChanged(bool value)
    {
        if (_isLoading()) return;
        _ = _apply(this, value ? Target.Recommended : Target.Default);
    }
}
```
[VERIFIED: src/AppTemplate.App/ViewModels/TweaksViewModel.cs:232-290]

### Pattern 3: Per-row run site (adapted canonical shape)

**What:** Guard → status → background work → live read-back → InfoBar; on failure roll the control back so it never lies.
**When to use:** Every toggle flip and quick-set click. Note the adaptation: the canonical `CheckViewModel` shape disables the whole page (`IsBusy`); per-row work must lock only the row (`SetRowsBusy`-style fan-out exists as precedent) so other rows stay interactive.
**Example:**
```csharp
// Source: in-repo pattern, GamingViewModel.ApplyToggle (lines 202-229) + TweaksViewModel.SetRowsBusy.
if (row.IsBusy) return;
row.IsBusy = true;
_status.Start($"{title}...");
try
{
    await Task.Run(() => apply(target));       // native ops off UI thread — never block nav
    row.RefreshFromSystem();                    // pills reflect reality (D-07), not optimism
    _infoBar.Show($"{title} applied", note);
}
catch (Exception ex)
{
    suppressChangeHandler = true;
    try { row.RefreshFromSystem(); }            // roll the control back to real state
    finally { suppressChangeHandler = false; }
    _infoBar.Show($"{title} failed", ex.Message);
}
finally { row.IsBusy = false; _status.Complete(); }
```
[VERIFIED: src/AppTemplate.App/ViewModels/GamingViewModel.cs:202-229]

### Pattern 4: Restore-point offer on first apply (Skip path, never blocking)

**What:** Before the session's first apply, offer a restore point via dialog with an explicit Skip; create via `RestorePointActions.CreateRestorePoint` on a background thread; failure to create warns but never blocks the apply (D-09).
**When to use:** First apply per session only — a per-row prompt would train users to dismiss it.
```csharp
// Source: in-repo, RestorePointActions.CreateRestorePoint(description) throws
// InvalidOperationException with a readable message when System Protection is off.
try { await Task.Run(() => RestorePointActions.CreateRestorePoint("AkariToolbox")); }
catch (Exception ex) { _infoBar.Show("Restore point skipped", ex.Message); }
// → then proceed with the apply regardless (offer, not gate).
```
[VERIFIED: src/AppTemplate.App/Tweaks/RestorePointActions.cs:17-51]

### Anti-Patterns to Avoid

- **Bulk tick-then-apply staging:** Dead per D-01. Caused the attempt-1 revert-baseline bug (apply-apply-revert restored intermediate values without the CR-01 guard). Never reintroduce CheckedItems/selection sets.
- **Always-visible paragraph stacks:** Attempt-1's exact UAT failure ("wanted something like Winhance, easy to read and understand" — archived 01-UAT.md test 1). What/why/risk/reverts goes behind the collapsed expander, never on the card face.
- **Cached-optimism pills:** Setting badge state from the requested value instead of a post-apply live read. Every navigation and every apply must re-probe (D-07); `OnNavigatedTo` hydration under `_loading` guard is the established shape [VERIFIED: src/AppTemplate.App/ViewModels/GamingViewModel.cs:84-105].
- **Duplicating registry text in seed rows:** D-12 — cross-reference `ControlPanelRows.All` by `SharedRowId` (e.g. HAGS `HwSchMode` bodies already exist in `ControlPanelOptimize.reg`/`ControlPanelDefault.reg`); the probe helper `ReadRowOptimized` compares the first concrete Optimize value against the live registry [VERIFIED: src/AppTemplate.App/Tweaks/ControlPanelRows.cs:19-46].
- **Page-wide `IsBusy` for per-row work:** Locking the whole catalog for one row's apply contradicts instant-toggle UX; fan busy state out to the affected row only (precedent: `TweaksViewModel.SetRowsBusy`).
- **Page-level `x:Bind`:** Dies with WMC9999; pages use `{Binding}` + `DataContext = ViewModel`, `x:Bind` only inside `DataTemplate` with `x:DataType`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Collapsible technical details | Custom show/hide panel + chevron | WinUI 3 `Expander` (`Microsoft.UI.Xaml.Controls`) | Free keyboard/Tab + Enter/Space + Narrator naming, matching the Winhance reference; custom panels regress accessibility [CITED: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.expander] |
| Registry/service mutation | Scripts, .bat, bundled .exe | `TweakAction` hierarchy (`RegDword…RegDeleteKey`, `Run`, `Custom`) | Native-ops-only constraint; every op is already expressed declaratively [VERIFIED: src/AppTemplate.App/Tweaks/TweakAction.cs:12-80] |
| Toggle state memory | JSON/settings-file choice cache as source of truth | `AkariToolState` (HKCU) as fallback + live `Read` as truth | Attempt-1 proved cached state lies; live read-back on every navigation is the rule |
| Search ranking | Full-text engine / fuzzy lib | Title-only `StartsWith`-first, `Contains`-second, 8-item cap, first-char trigger | D-14 contract; attempt-1 implemented exactly this and its ranking tests passed (archived UAT tests 6, 9) |
| Restore points | Custom snapshot-restore of system state | `RestorePointActions` (WMI SystemRestore, frequency-0 + enable + create) | OS-owned mechanism; custom rollback of system state is unreliable [VERIFIED: src/AppTemplate.App/Tweaks/RestorePointActions.cs:12-51] |
| Progress footer | Per-row progress dialogs | `IStatusService.Start/Complete` + shell `InfoBar` | Established run-site pairing; per-row spinners (`IsApplying`-style overlay) only if the row's apply is slow |

**Key insight:** Every hard primitive (native ops, live probes, revert-capable shared rows, restore points, status/InfoBar plumbing) already exists and is proven. This phase is composition + one new descriptor schema + one new page — not new machinery.

## Common Pitfalls

### Pitfall 1: Revert restores intermediate values (the CR-01 bug class)
**What goes wrong:** Toggle on → toggle off → revert lands on the toggled-on state instead of the original.
**Why it happens:** Capturing "prior" values at every apply instead of only the first; bulk-prefix journals captured per-run, not per-row-origin.
**How to avoid:** Row-scoped journal with first-capture-wins: capture a row's footprint once (before its first apply this session), never overwrite until reverted. Attempt-1's `MarkApplied` first-capture-wins guard + regression test is the proven logic — carry the logic forward, re-scoped per row (D-08).
**Warning signs:** Any journal write outside a "first apply" branch; revert tests that only cover single apply-revert.

### Pitfall 2: Toggle handler re-fires during hydration/rollback
**What goes wrong:** Setting `IsOn` programmatically (navigation hydration, failure rollback) triggers the apply path → infinite or spurious applies.
**Why it happens:** `OnIsOnChanged` fires for programmatic sets too.
**How to avoid:** The `_loading`/`_isLoading()` guard on every change handler — set during `OnNavigatedTo` hydration and rollback (precedent in both VMs) [VERIFIED: src/AppTemplate.App/ViewModels/GamingViewModel.cs:140-146; src/AppTemplate.App/ViewModels/TweaksViewModel.cs:284-289].
**Warning signs:** Change handlers without a guard; rollback code that sets the bound property directly.

### Pitfall 3: Registry writes land in the wrong hive (elevated HKCU split)
**What goes wrong:** A per-user tweak (Game DVR `HKCU\System\GameConfigStore`, Game Bar keys) is written to the elevation account's hive, silently doing nothing for the interactive user.
**Why it happens:** The app runs elevated; `Registry.CurrentUser` is the admin token's hive, not the interactive user's.
**How to avoid:** For interactive-user HKCU targets use `AkariToolState.OpenRealHkcu` / `RealHkcuDword` / `SetRealHkcuDword` (explorer-token SID → HKU) — the established precedent for Start-menu/transparency keys [VERIFIED: src/AppTemplate.App/Tweaks/AkariToolState.cs:27-76]. Game Mode/DVR seed rows are exactly this category (existing `fso` toggle writes `HKCU\System\GameConfigStore` + `HKCU\Software\Microsoft\GameBar` — verify hive routing for each seed row during planning) [VERIFIED: src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs:297-329].
**Warning signs:** Any new `HKCU\...` write path that doesn't go through `RealHkcu*` helpers; Game DVR rows that "apply" but read back unchanged.

### Pitfall 4: WMI/long reads on the UI thread
**What goes wrong:** Navigation stutters or hangs; restore-point creation (minutes) freezes the app.
**Why it happens:** `SystemInfo` WMI reads are synchronous and cached per-process; `CreateRestorePoint` is slow.
**How to avoid:** All probes and applies inside `Task.Run`; dispose every `ManagementObjectCollection` + `ManagementObject` (`using`). Never call reads on the UI thread without `Task.Run`.
**Warning signs:** `await` directly on a `*Actions` call without `Task.Run`; missing `using` on WMI objects.

### Pitfall 5: Multi-value rows partially applied then reported success
**What goes wrong:** Network-optimization-style rows touch NIC keys + netsh + bindings; one leg fails, pills claim Recommended.
**Why it happens:** No post-apply verification; success reported from absence of exception.
**How to avoid:** `RefreshFromSystem` after every apply and derive pills/badges from the read-back (D-07), never from the requested target. For rows whose `read` is persisted-state-based (e.g. `ReadNetworkOptimization` returns `AkariToolState.Has(...)`) [VERIFIED: src/AppTemplate.App/Tweaks/GamingActions.cs:69], pills genuinely reflect intent-memory — planner must decide per seed row whether a true live probe exists and mark Custom-pill rows honestly.
**Warning signs:** `Show(... applied ...)` without a preceding `RefreshFromSystem`.

### Pitfall 6: Reboot-required rows applied without notice
**What goes wrong:** User applies HAGS/preemption tweaks, sees no change, concludes the app is broken.
**Why it happens:** Scheduler/GPU tweaks need a restart; nothing says so.
**How to avoid:** `RequiresReboot` on the descriptor → in-place badge (D-13) + "Restart to apply" InfoBar note (established copy in both VMs). Never segregate or block.
**Warning signs:** A descriptor with scheduler/GPU/driver footprint and no reboot flag.

## Code Examples

### Descriptor seed with shared-row cross-reference (HAGS)

```csharp
// Pattern: cross-reference (D-12) — HwSchMode bodies already exist in the shared
// ControlPanel data; the descriptor points at them instead of duplicating .reg text.
// Grep-observed this session: "HwSchMode"=dword:00000002 in Tweaks/Data/Windows/
// ControlPanelOptimize.reg:1019 and "HwSchMode"=- in ControlPanelDefault.reg:962.
new GamingTweakDescriptor(
    Key: "hags",
    Title: "Hardware-Accelerated GPU Scheduling",
    OneLiner: "Lets your graphics card manage its own memory for lower latency.",
    WhyItHelps: "…", Risk: "…", HowItReverts: "…",
    RecommendedLabel: "Recommended: On (HwSchMode=2)",
    DefaultLabel: "Windows default: Off (value removed)",
    Footprint: [new RegFootprint(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
        "HwSchMode", RegistryValueKind.DWord)],
    RequiresReboot: true,
    SharedRowIds: ["<control-panel-row-id-covering-HwSchMode>"],  // resolve at plan time
    Read: () => RegRead.Dword(@"HKLM\…\GraphicsDrivers", "HwSchMode") == 2,
    Apply: on => ControlPanelRows.ApplyRow(SharedRow.Lookup("…"), on));
```

### Search filter (title-only, prefix-first — D-14)

```csharp
// Contract carried forward from attempt-1 (ranking tests passed, archived UAT 6, 9):
// match Titles only; rank StartsWith above Contains; trigger from first character;
// cap suggestions at 8; empty query = no filter; tap-to-filter never applies.
IEnumerable<GamingTweakDescriptor> Search(string query)
{
    if (string.IsNullOrWhiteSpace(query)) return GamingCatalog.All;
    string q = query.Trim();
    return GamingCatalog.All
        .Where(d => d.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(d => d.Title.StartsWith(q, StringComparison.OrdinalIgnoreCase))
        .ThenBy(d => d.Title);
}
IEnumerable<string> Suggest(string query) => Search(query).Take(8).Select(d => d.Title);
```

### Grouped sections (System / Network / GPU / Background — D-11)

```csharp
// Page composition per winhance-patterns.md §4: grouped settings via CollectionViewSource
// (IsSourceGrouped=True) with section headers; search filters across groups; an explicit
// empty state ("No matching tweaks", title-only hint per D-14) when the filter is empty.
public ObservableCollection<CatalogSectionGroup> Sections { get; } = [];
// where CatalogSectionGroup mirrors CpSectionGroup: Title + ObservableCollection<CatalogRowItem>.
```
[VERIFIED shape precedent: src/AppTemplate.App/ViewModels/TweaksViewModel.cs:135-152, 224-229]

## State of the Art

| Old Approach (attempt 1, superseded) | Current Approach (this phase) | When Changed | Impact |
|--------------------------------------|-------------------------------|--------------|--------|
| Bulk tick-then-apply + footer progress + stop-at-first-failure | Instant per-row toggles, live-apply | 2026-10-04 redesign (D-01) | Deletes staging-state bug class; journal must be row-scoped, not bulk-prefix |
| Inline four-line What/Why/Risk/Reverts paragraphs | One-liner + collapsed Technical-details `Expander` | 2026-10-04 (D-04/D-05, UAT test 1) | Scannable rows; Winhance parity |
| Ticks stay sticky after success; one-click bulk revert | Per-row revert to prior/Default; restore-point offer on first apply | 2026-10-04 (D-08/D-09) | Simpler trust model; SFT-01 rewrite required |
| Bulk-prefix snapshot journal (`GamingRunner`, since reverted) | Row-scoped first-capture-wins journal OR direct revert-to-Default | Decision: planner's call (discretion) | Reuse the proven first-capture-wins logic, not the bulk shape |
| No Recommended/Default distinction | Per-row Recommended + Default quick-set pair with value-naming tooltips | 2026-10-04 (D-06) | New descriptor fields; tooltips must name target values |

**Deprecated/outdated:**
- Archived `01-01-PLAN.md` / `01-02-PLAN.md` / `01-03-PLAN.md` bulk loop, `GamingRunner.cs` (deleted in revert), attempt-1 `01-CONTEXT.md` D-10–D-16 — history, not guidance.
- REQUIREMENTS.md "Technical-details row" out-of-scope entry — explicitly overridden by D-05 for the expander form (noted in CONTEXT.md Canonical References).

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | WinUI 3 `Expander` needs no extra package/Toolkit reference under WindowsAppSDK 2.3.1 (in `Microsoft.UI.Xaml.Controls`) | Standard Stack, Don't Hand-Roll | Low — first-party control since WinAppSDK 1.x; build will confirm immediately |
| A2 | `AutoSuggestBox` vs `TextBox`+popup for the D-14 suggestion dropdown is planner's choice; both can meet the contract | Standard Stack, Code Examples | Low — attempt-1 proved TextBox+popup works; AutoSuggestBox is standard WinUI |
| A3 | Grouped `CollectionViewSource` (`IsSourceGrouped`) works as in the Winhance reference for section headers | Code Examples | Low — standard WinUI pattern, mirrored from the extraction (§4) |
| A4 | Attempt-1's search-ranking implementation is recoverable from git history or archive if the planner wants it verbatim (revert commit `7432980` range) | Code Examples | Low — ranking tests passed; reimplementation from the D-14 contract is trivial anyway |
| A5 | Journal-vs-direct-revert: direct revert-to-Default suffices for rows whose Default body fully restores stock; journal needed only where Default is lossy (multi-value rows with machine-specific stock) | Pitfalls, Architecture | Medium — planner must audit each seed row's Default body; wrong call reintroduces CR-01 class |
| A6 | Game Mode + DVR seed registry paths mirror the existing `fso` toggle keys (`HKCU\System\GameConfigStore`, `HKCU\Software\Microsoft\GameBar`, `HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR`) | Pitfalls | Low — verify against FR33THY source during planning |
| A7 | .NET SDK 10.0.401 satisfies the 10.0.400 pin via `rollForward: latestMinor` | Environment | Very low — build confirms |

## Open Questions (RESOLVED — dispositions recorded 2026-10-04; plan content answers all three)

1. **Journal vs direct revert per seed row? (RESOLVED)**
   - **Disposition: RESOLVED — row-scoped first-capture-wins journal chosen.** Plan 01 creates `RowRevertJournal` keyed by descriptor Key with first-capture-wins semantics; plan 02 audits each seed row so Default-lossless rows revert via Default bodies and the rest revert via the journal, defaulting to journal unless proven lossless.
   - What we know: D-08 allows a row-scoped journal; first-capture-wins logic is proven. Rows backed by shared `ControlPanelRows` Optimize/Default bodies can revert via `ApplyRow(row, optimize: false)` — but only if the Default body truly restores stock for that machine.
   - What's unclear: Whether every seed row's Default body is lossless (multi-value NIC/GPU rows may have machine-specific stock values).
   - Recommendation: Planner audits each of the 8–12 seed rows: Default-lossless → direct revert; otherwise → row-scoped journal entry. Default to journal unless proven lossless.

2. **Page-level "apply recommended to all"? (RESOLVED)**
   - **Disposition: RESOLVED — explicitly excluded from this phase.** Per the agent's Discretion, no page-level apply-to-all action is added; plan 02 ships per-row only and states the exclusion in its objective. A page-level action may be proposed as a follow-up, never as silent scope.
   - What we know: Explicitly undecided in discretion; D-01 kills silent bulk scope.
   - What's unclear: Whether users need it for the "one-pass safe tune" core value.
   - Recommendation: Ship per-row only; propose the page-level action as a follow-up plan item, never silent scope.

3. **Badge-polling cost for NIC-heavy rows? (RESOLVED)**
   - **Disposition: RESOLVED — off-thread hydration specified.** Plans hydrate under `Task.Run` during `OnNavigatedTo` with the `_loading` guard (established pattern); live read-back is cached within a single navigation only, never across navigations, and re-probed on every navigation per D-07.
   - What we know: Live read-back on every navigation is mandatory (D-07); NIC enumeration touches many subkeys.
   - What's unclear: Whether re-probing all rows on each navigation causes visible lag.
   - Recommendation: Hydrate under `Task.Run` during `OnNavigatedTo` (established: "reads are slow — refresh off-thread"); measure; cache within a single navigation only, never across navigations.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET SDK (pin 10.0.400, rollForward latestMinor) | Build | ✓ | 10.0.401 (probed this session) | — |
| Windows 10/11 x64 + admin | All tweak reads/writes | ✓ (dev machine is Windows) | — | Read-only paths still testable unelevated [ASSUMED] |
| nuget.org | Restore | ✓ [ASSUMED — pinned deps already restored in repo] | — | — |
| PowerShell (`build-and-run.ps1`) | Zero-error/zero-warning build gate | ✓ [ASSUMED — repo scripts present] | — | `dotnet build` directly, kill apphost manually |
| WinUI 3 `Expander` | Technical-details rows | ✓ (in-box, WindowsAppSDK 2.3.1) | — | — |

**Missing dependencies with no fallback:** none.
**Missing dependencies with fallback:** none.

Repo-wide grep for `Expander|AutoSuggestBox|CollectionViewSource` in `src/` returned no matches this session — the Technical-details expander and grouped/suggest controls are genuinely new UI surface; no existing XAML to copy, but the `CpRowItem` DataTemplate + item-command pattern is the proven base to build on.

## Security Domain

`security_enforcement` is enabled; ASVS L1. This is an elevated desktop app (administrator by manifest) with no network input surface and no authentication — the threat model centers on safe mutation, not access control.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|------------------|
| V2 Authentication | No | N/A — single-user local admin tool; no identity, no login |
| V3 Session Management | No | N/A — no sessions; per-launch process, single-instance key `AkariToolbox` |
| V4 Access Control | Partial | Elevation via manifest (`requireAdministrator`); no privilege boundaries *within* the app — every row runs admin. Mitigation: restore point + per-row revert as the safety control, never silently widen scope |
| V5 Input Validation | Yes | Search text is filter-only (never executed, never a registry path) — treat as opaque string. `AutoDscpFse` profile-name regex (`^[0-9a-zA-Z]+$`) is the precedent for validating anything that becomes a registry key [VERIFIED: src/AppTemplate.App/Tweaks/GamingActions.cs:218-219]. Quick-set tooltips/descriptor strings are developer-authored constants, not user input |
| V6 Cryptography | No | N/A — no secrets, tokens, or encrypted payloads |
| V7 Error Handling & Logging | Yes | Per-command `try/catch` → InfoBar (user) + `ILogger` file log (diagnostics); logging paths never throw (`FileLoggerProvider` swallows). Never surface stack traces or hive paths beyond what's in Technical-details by design |
| V8 Data Protection | Partial | `HKCU\Software\AkariTool` stores only tweak-choice flags (no PII). Restore-point descriptions are static strings |
| V10 Malicious Code / Native-ops integrity | Yes (project-specific) | Native-ops-only: `TweakAction` allow-list (`reg`, service start values, vetted system tools via `Run` hidden+waited). No script execution, no downloads, no bundled binaries. New seed rows must map to existing `TweakAction` factories or vetted `*Actions` methods — planner must reject any row requiring a new network fetch or binary launch |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Wrong-hive write silently no-ops (elevated HKCU split) | Tampering (integrity of user intent) | `RealHkcu*` helpers for interactive-user keys; read-back verification after every apply (Pitfall 3) |
| Partial multi-value apply reported as success | Tampering | Post-apply `RefreshFromSystem`; pills from read-back, never request-echo (Pitfall 5) |
| Reboot-gated tweak appears broken | Denial of service (perceived) | `RequiresReboot` in-place badge + "Restart to apply" note; never block (D-13) |
| Restore-point creation failure blocks tuning | Denial of service | Offer with Skip; failure warns via InfoBar, apply proceeds (D-09) |
| Registry-footprint display enables user hand-edits that diverge state | Information disclosure (minor) | Footprint display is read-only text + optional "Open in Registry Editor"; pills always re-probe live state so divergence self-corrects on next navigation |

## Sources

### Primary (HIGH confidence)
- In-repo reads this session: `TweakToggle.cs:8-30` (descriptor gap), `GamingViewModel.cs:84-105,140-229` (hydration + run shape), `TweaksViewModel.cs:135-152,232-290` (grouping + CpRowItem pattern), `ControlPanelRows.cs:19-46` (probe/apply), `ControlPanelRows.Generated.cs:8` (`CpTweakRow` shape), `RestorePointActions.cs:17-51`, `AkariToolState.cs:27-128`, `GamingActions.cs:69,218-219`, `TweakAction.cs:12-80`, `App.xaml.cs:147-175` (DI), `MainWindow.xaml.cs:82-96` (nav)
- `.planning/research/winhance-patterns.md` §§1–6 — binding design bar extracted from real Winhance source with file/line refs
- `01-CONTEXT.md` (locked decisions D-00–D-14, discretion, superseded list) + `01-DISCUSSION-LOG.md`
- Archived `01-UAT.md` test 1 (failure evidence) + tests 4–12 (carried-forward passing behaviors)
- Microsoft Learn: `Expander` class, `Microsoft.UI.Xaml.Controls`, Windows App SDK — first-party availability [CITED: https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.expander]

### Secondary (MEDIUM confidence)
- `.planning/REQUIREMENTS.md` (BRW-01/BRW-02 current; SFT-01 stale; SFT-02 + presets-row conforming edits needed)
- AGENTS.md project/stack/conventions/architecture blocks (pinned versions, XAML rules, run-site pairing)

### Tertiary (LOW confidence)
- None — no web-only claims; all findings are repo- or docs-grounded except Assumptions A1–A7, which are logged above.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — zero new dependencies; every primitive verified in-repo this session.
- Architecture: HIGH — four patterns mirror proven in-repo shapes with file:line citations; only the descriptor schema and journal-vs-direct call are planner discretion (flagged, not assumed).
- Pitfalls: HIGH — Pitfalls 1–2 grounded in archived attempt-1 failure evidence; 3–6 grounded in verified code paths.

**Research date:** 2026-10-04
**Valid until:** 2026-11-03 (stable domain — WinUI 3 + pinned SDK; re-check only if WindowsAppSDK version changes)
