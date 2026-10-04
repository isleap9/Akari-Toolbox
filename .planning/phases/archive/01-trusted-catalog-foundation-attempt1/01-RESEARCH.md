# Phase 1: Trusted Catalog Foundation — Research

**Researched:** 2026-10-03
**Domain:** WinUI 3 searchable tweak-catalog page + snapshot-backed bulk apply/revert (.NET 10, Windows App SDK 2.3.1, CommunityToolkit.Mvvm 8.4.2)
**Confidence:** HIGH (codebase-grounded; all structural claims read from source this session)

## Summary

Phase 1 adds one new page (a searchable gaming-tweak catalog in four groups) plus the reversibility infrastructure every later phase rides on (per-value snapshot journal + ordered bulk runner). All required UI controls already exist in the codebase or the WinUI framework: `AutoSuggestBox` (in-box WinUI 3), `CheckBox` per row, the shell status footer (`IStatusService.Start/Report/Complete`), `IDialogService` for the restore-point offer, and `InfoBar` for results. No new NuGet packages are needed — this phase is pure domain + presentation work following the `AkariTweakCatalog` descriptor precedent and the `CheckViewModel.RunAsync` / `TweaksViewModel.ApplyRowAsync` run-site precedents.

The two genuinely new designs are (1) a `GamingCatalogEntry` descriptor that cross-references `CpTweakRow` shared definitions by ID instead of duplicating `.reg` bodies (D-04), and (2) a dedicated JSON snapshot journal (`%LOCALAPPDATA%\AkariToolbox\gaming-journal.json`) that captures prior values per row immediately before that row's apply, so stop-at-first-failure (D-14) leaves a precise completed-prefix journal and one-click revert (D-16) replays it in reverse. The existing `RevertNetworkOptimization` (winsock reset — does not restore the ~57 NIC values it overwrote) is the canonical example of why the journal, not bespoke revert methods, is the revert mechanism.

**Primary recommendation:** New `Tweaks/GamingCatalog.cs` (record descriptor + group enum + seed list with `FromNative`/`FromSharedRow` factories), new `Tweaks/GamingJournal.cs` (snapshot capture + JSON journal + reverse replay), new `GamingCatalogViewModel` + `GamingCatalogPage` following the 13-page registration pattern, bulk loop modeled on `TweaksViewModel.ApplyRowAsync` with `StatusService.Report(done/total)` progress, restore-point offer via `IDialogService.ShowAsync` primary/secondary buttons, and xUnit tests for catalog integrity + search ranking + journal round-trip (all UI-free logic).

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Catalog scope and page**
- **D-01:** Catalog seeds from current gaming rows plus Game Mode and DVR only — Phases 2-4 rows are not pulled forward.
- **D-02:** The catalog lives on a new dedicated page in nav, not as a GamingPage rework.
- **D-03:** Rows group into four groups: System, Network, GPU, Background.
- **D-04:** Gaming rows cross-reference the shared row definitions behind the 169 existing Individual Tweaks rows instead of duplicating them.
- **D-05:** Reboot-required rows show a badge in place; they are not segregated and do not block.

**Search and suggest**
- **D-06:** Search matches against row titles only, not descriptions or technical keys.
- **D-07:** Suggestions appear from the first character typed.
- **D-08:** Prefix matches rank above substring matches.
- **D-09:** Tapping a suggestion filters the row list; it does not jump to or tick the row.

**Explanations**
- **D-10:** Per-tweak explanations render inline under each row, not behind expand or dialog.

**Bulk apply and revert**
- **D-11:** Bulk apply acts on ticked rows only; there is no apply-all button.
- **D-12:** Ticks stay sticky after a successful apply so the set can be re-run or inspected.
- **D-13:** Bulk apply progress shows in the existing shell status footer, not a dialog or inline spinners.
- **D-14:** On a row failure the run stops at the first failure rather than finishing all rows or skipping.
- **D-15:** A restore point is offered before bulk apply but the user may skip it; apply is never blocked on it.
- **D-16:** The revert control is a single one-click button in the top bar of the page.

### the agent's Discretion

No areas were explicitly delegated; the planner has flexibility on descriptor schema shape, journal storage location, and suggestion UI control choice within the decisions above.

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope.

## Phase Requirements

| ID | Description (from REQUIREMENTS.md) | Research Support |
|----|-------------------------------------|------------------|
| BRW-01 | Find any gaming tweak by search within a categorized catalog | §2 descriptor schema (Title + Group fields); §4 AutoSuggestBox title-only prefix-first search, tap-to-filter |
| BRW-02 | Per-tweak explanation (what / why-gaming / risk / reverts-by) before toggling | §2 four-string explanation fields on the descriptor, rendered inline per UI-SPEC; §7 copy-tone pitfalls |
| SFT-01 | Bulk-apply with progress + one-click revert | §3 snapshot journal + reverse replay; §5 bulk runner (StatusService progress, stop-at-first-failure, restore-point offer) |

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Catalog search/filter/suggest | API / Backend (in-process domain: static catalog + LINQ) | — | Filtering is pure in-memory LINQ over an immutable descriptor list; no OS reads on keystroke |
| Live tweak state read | API / Backend (`Tweaks/*Actions` + `RegRead`) | — | Registry/service probes; must run off UI thread |
| Bulk apply + snapshot + revert | API / Backend (`Tweaks/` statics via `Task.Run`) | — | Admin registry writes; VM only orchestrates and reports |
| Progress footer / InfoBar / dialogs | Frontend Server (shell services) | — | Existing `IStatusService` / `IInfoBarService` / `IDialogService` singletons |
| Row list + ticks + explanations | Browser / Client (WinUI XAML view) | — | Thin declarative layout; `{Binding}` page-level, `x:Bind` in DataTemplate |
| Journal persistence | Database / Storage (local JSON file) | — | `%LOCALAPPDATA%\AkariToolbox\gaming-journal.json`, atomic temp+move writes |

## Standard Stack

### Core (no changes — pinned stack stands)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET SDK / TFM | 10.0.400 / `net10.0-windows10.0.26100.0` [VERIFIED: global.json + Directory.Build.props via codebase map] | Runtime + target | Pinned; whole app builds Errors:0 Warnings:0 on it |
| Windows App SDK (WinUI 3) | 2.3.1 [VERIFIED: Directory.Packages.props via codebase map] | `AutoSuggestBox`, `CheckBox`, `NavigationView`, `InfoBar`, footer `ProgressBar` | `AutoSuggestBox` is in-box — no package needed for D-06–D-09 |
| CommunityToolkit.Mvvm | 8.4.2 [VERIFIED: Directory.Packages.props via codebase map] | `[ObservableProperty]` / `[RelayCommand]` in new VM + row item class | Every existing VM uses it; zero-cost source generation |
| Microsoft.Extensions.Hosting/DI | 10.0.11 | Register new VM + page as transients | Two-line addition to existing `BuildHost` |
| System.Text.Json (in-box) | .NET 10 in-box | Journal serialization | Already used by `FileSettingsStorage`; no new dependency |
| xUnit + Moq | 2.9.3 / 4.20.72 | Catalog-integrity, search-ranking, journal round-trip tests | UI-free logic is fully testable under `dotnet test` |

### New packages required: NONE

Phase 1 needs no `SettingsControls`, no `CsWin32`, no new Toolkit packages. (Those were suggested in `.planning/research/SUMMARY.md` for the broader push; the locked UI-SPEC uses plain `CheckBox` rows + `AkariCard` styles, and the seed tweaks need no new P/Invoke.)

## Package Legitimacy Audit

Not applicable — this phase installs zero external packages. No registry verification required. If a later plan introduces a package, the legitimacy gate must run then.

## 1. Descriptor Schema (D-01–D-05, D-10)

### What the codebase already proves

- `TweakToggle` is `(key, title, description, stateKey, Func<bool> read, Action<bool> apply)` [VERIFIED: src/AppTemplate.App/Tweaks/TweakToggle.cs:8-30] — read/apply pairing with a persisted state key.
- `CpTweakRow` is `(Id, Title, Tab, Section, Revertible, Optimize, Default)` where `Optimize`/`Default` are `.reg` bodies applied via `NativeOps.ImportRegContent` → `regedit.exe /S` [VERIFIED: src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs:8 + src/AppTemplate.App/Tweaks/ControlPanelRows.cs:44-45 + src/AppTemplate.App/Tweaks/NativeOps.cs:896-907].
- `ControlPanelRows.ReadRowOptimized` probes only the **first** concrete value of the Optimize body to infer state [VERIFIED: src/AppTemplate.App/Tweaks/ControlPanelRows.cs:18-42]; `ParseProbe` handles `dword:HEX` and `"string"` lines only [VERIFIED: src/AppTemplate.App/Tweaks/ControlPanelRows.cs:47-74].
- Seed candidates in `GamingActions`: preemption has true live read + symmetric set (`ReadPreemption` ORs two DWORDs [VERIFIED: src/AppTemplate.App/Tweaks/GamingActions.cs:35-37]; `SetPreemption(bool)` writes 6 values symmetrically [VERIFIED: GamingActions.cs:39-61]); network optimization read is **persisted-flag only** (`AkariToolState.Has("NetworkOptimization")` [VERIFIED: GamingActions.cs:69]) and its revert is `netsh winsock reset`, which does **not** restore the ~57 NIC values `ApplyNetworkOptimization` wrote [VERIFIED: GamingActions.cs:144-184] — a reversibility gap the journal must cover (§3).
- **Gap:** no standalone Game Mode / Game DVR read/apply pair exists. GameDVR keys are embedded inside the `fso` toggle's on/off lambdas (`GameDVR_Enabled`, `GameDVR_FSEBehaviorMode`, `AllowGameDVR`, `AppCaptureEnabled`, `BcastDVRUserService` Start) [VERIFIED: src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs:297-329]; GameBar `AllowAutoGameMode`/`AutoGameModeEnabled` likewise [VERIFIED: AkariTweakCatalog.cs:301-305]. Phase 1 needs small `GamingActions` additions (`ReadGameMode`/`SetGameMode`, `ReadGameDvr`/`SetGameDvr`) extracted from—but not replacing—the `fso` toggle.

### Approaches compared

| Approach | Shape | Verdict |
|----------|-------|---------|
| A. Subclass `TweakToggle` with explanation fields | `GamingToggle : TweakToggle` + 4 strings + group + reboot | **Rejected** — `TweakToggle` ctor takes a single `Action<bool> apply`; shared-row entries need `.reg`-body apply + multi-value snapshot, which doesn't fit on/off lambdas cleanly |
| B. New record with delegate triple + optional shared-row reference | `GamingCatalogEntry` with `Read/Apply` delegates + nullable `SharedRowId` + `Group` enum + 4 explanation strings + `RequiresReboot` | **Recommended** — fits both native entries and cross-references; single source of truth for search/group/explain/apply |
| C. Wrap `CpTweakRow` directly for all rows | Every gaming entry IS a `CpTweakRow` | **Rejected** — native gaming rows (preemption 6-value set, netsh commands, Game Mode service+reg mix) are not representable as `.reg` bodies; D-01 seeds are mostly native |

### Recommended schema

```csharp
// Tweaks/GamingCatalog.cs — NEW, beside AkariTweakCatalog.cs
namespace AkariToolbox.Tweaks;

/// <summary>One of the four Phase 1 catalog groups (D-03).</summary>
public enum GamingGroup { System, Network, Gpu, Background }

/// <summary>
/// One searchable, explainable gaming tweak (BRW-01/BRW-02).
/// Native entries carry read/apply delegates; shared-row entries carry
/// <see cref="SharedRowId"/> and delegate to <see cref="ControlPanelRows"/> (D-04).
/// Explanation lines render inline; never include registry paths (UI-SPEC copy contract).
/// </summary>
public sealed record GamingCatalogEntry(
    string Id,                // stable id, e.g. "game-mode", "disable-preemption"
    GamingGroup Group,
    string Title,             // search + display (D-06: titles only)
    string WhatItDoes,        // 1 sentence
    string WhyItHelpsGaming,  // 1 sentence
    string Risk,              // 1 sentence, honest
    string RevertsBy,         // 1 sentence, e.g. "Restores the 6 prior values from the journal."
    bool RequiresReboot,      // D-05: inline badge, never blocks
    Func<bool> Read,          // live state; true = optimized/applied
    Action<bool> Apply,       // true = apply optimization, false = direct revert
    string? SharedRowId = null); // non-null => cross-reference, delegates forward to CpTweakRow
```

Factory pattern (mirrors `AkariTweakCatalog.Toggle(key,title,desc,stateKey,read,on,off)` [VERIFIED: AkariTweakCatalog.cs:543-547]):

- `FromNative(id, group, title, what/why/risk/reverts, reboot, read, applyOn, applyOff)` — for preemption, network opt, Game Mode, DVR.
- `FromSharedRow(CpTweakRow row, GamingGroup group, what/why/risk/reverts, reboot)` — looks up `ControlPanelRows.All` by `row.Id` once at build time (fail-fast `KeyNotFoundException` if the shared row is renamed), sets `Read = () => ControlPanelRows.ReadRowOptimized(row) ?? persisted fallback`, `Apply = on => ControlPanelRows.ApplyRow(row, on)`.
- Seed list is a static `IReadOnlyList<GamingCatalogEntry> All { get; } = Build();` with collection-expression syntax `[...]` per codebase convention.
- `RequiresReboot` values [ASSUMED — needs per-row confirmation at plan time]: preemption YES (driver scheduler keys), network opt MAYBE (UI copy already says "A reboot may be required" [VERIFIED: GamingViewModel.cs:199]), Game Mode NO, DVR NO. Planner confirms each flag; the badge is display-only so a wrong flag is cosmetic, not destructive.

### Why cross-reference by ID, not by duplicating `.reg` text

D-04 forbids duplication. Storing only `SharedRowId` + resolved `CpTweakRow` reference means: (a) the 169-row generated file is never touched, (b) probe/apply logic stays in `ControlPanelRows` (single behavior owner), (c) the journal (§3) snapshots actual live values at apply time, so even if a shared row's `.reg` body changes upstream, revert still restores what was there — not what the old body assumed.

## 2. Snapshot Journal Design (SFT-01 revert half; D-14, D-16)

### Requirement shape

- Bulk apply stops at first failure (D-14): journal must record the **completed prefix only** — snapshotted per row immediately before that row's apply, marked applied only after success.
- One-click revert, no confirmation (D-16): revert replays journaled rows in **reverse apply order**, writing back captured prior values (including "absent" → delete value).
- Ticks stay sticky (D-12): journal is independent of tick state; re-running apply re-snapshots (idempotent by overwrite).

### Approaches compared (storage location)

| Approach | Location | Verdict |
|----------|----------|---------|
| A. `HKCU\Software\AkariTool` keys (extend `AkariToolState`) | Registry | **Rejected** — binary values need hex encoding, subkey-per-row enumeration is clumsy, registry has size limits, and journal data (multi-KB blobs) doesn't belong next to single-bit toggle memory |
| B. `settings.json` via `FileSettingsStorage`/`ISettingsService` | `%LOCALAPPDATA%\AkariToolbox\settings.json` string dict | **Rejected** — mixes machine-state snapshots with user prefs in one flat `Dictionary<string,string>`; every journal write rewrites prefs and vice versa; binary encoding still needed inside string values |
| C. Dedicated versioned JSON journal file | `%LOCALAPPDATA%\AkariToolbox\gaming-journal.json` | **Recommended** — clean separation, atomic temp+move writes (same pattern as `FileSettingsStorage.Save` [VERIFIED: src/AppTemplate.Framework/Services/FileSettingsStorage.cs:91-99]), `System.Text.Json` typed schema with binary-as-hex, schema `version` field for forward migration |

### Recommended journal schema

```csharp
// Tweaks/GamingJournal.cs — NEW
public sealed record JournalValue(
    string Path,              // "HKLM\..." full path (RegistryPath.Resolve-compatible)
    string Name,
    string? Kind,             // "DWord" | "String" | "ExpandString" | "Binary" | null when absent
    string? DataHex,          // null when absent; binary/DWORD payload as hex
    string? DataString);      // string payload when Kind is String/ExpandString

public sealed record JournalEntry(
    string CatalogId,
    DateTime TakenUtc,
    IReadOnlyList<JournalValue> Values);   // "absent" = Kind null => revert deletes the value

public sealed record GamingJournal(
    int Version,              // 1; bump on schema change, old versions ignored-with-warning
    IReadOnlyList<JournalEntry> Entries);  // apply order; revert walks in reverse
```

Capture protocol per row, immediately before that row's apply:

1. **Native entries:** each `GamingCatalogEntry` gains an internal `IReadOnlyList<(string Path, string Name)> Footprint` (declared next to the tweak — per ARCHITECTURE.md Pattern 2, footprint lives beside the tweak so it cannot drift). Capture reads each via `RegRead.Dword/String/HasValue` [VERIFIED: AkariToolState.cs:98-123] plus `Registry.GetValue` kind detection for binary/expand-string. Non-registry effects (netsh globals, `winsock reset` side effects) are **not capturable per-value** — record them as an uncaptured-side-effect note in the entry and rely on the registry snapshot + honest `RevertsBy` copy ("restores NIC values; netsh globals re-applied to defaults"). This is a known, accepted limitation, not a blocker: the NIC values (the bulk of network-opt state) ARE registry values under the adapter-class key [VERIFIED: GamingActions.cs:147-166] and snapshot cleanly.
2. **Shared-row entries:** generalize the `ParseProbe` line parser to return **all** `(key, name, kind)` triples in the Optimize body (same `dword:`/`"str"` shapes [VERIFIED: ControlPanelRows.cs:47-74]; `-` deletion lines in the Default body need no capture — capture reads live state, not the body). Reuse, don't modify, `ControlPanelRows`; put the full-body parser in `GamingJournal.cs` as `internal static` so `ControlPanelRows.Generated.cs` ("do not edit by hand" [VERIFIED: ControlPanelRows.Generated.cs:6-7]) is never touched.
3. **Revert** for both kinds = write back captured values (`Registry.SetValue` with recorded kind; `DeleteValue` when `Kind is null`), in reverse entry order, each row wrapped in try/catch with per-row ok/fail collection (revert continues past single-row failures and reports them — unlike apply's stop-at-first-failure, a stuck revert row must not strand the remaining rows; this asymmetry is intentional and must be in the plan).

Multi-value subtlety (preemption touches 6 values [VERIFIED: GamingActions.cs:43-48]): snapshot captures all 6; revert restores all 6 even though `ReadPreemption` only probes 2. Journal completeness is driven by Footprint, never by the read probe.

## 3. Search / Suggest (BRW-01; D-06–D-09)

All control behavior below is standard WinUI 3 `AutoSuggestBox` API knowledge [ASSUMED — verify event/property names against the Windows App SDK 2.3.1 SDK contracts at plan time; the pattern itself is HIGH confidence].

- **Control:** in-box `AutoSuggestBox` in the page top bar (`PlaceholderText="Search gaming tweaks"` per UI-SPEC copy contract). No custom control, no package.
- **Trigger:** `TextChanged` with `AutoSuggestionBoxTextChangeReason.UserInput`, from the first character (D-07 — no minimum-length gate).
- **Ranking (D-06, D-08):** filter `GamingCatalog.All` titles with `OrdinalIgnoreCase`: `StartsWith(query)` first, then `Contains(query)`, each subgroup in catalog order; `.Take(8)` for the suggestion cap (UI-SPEC overflow state). Pure LINQ over the in-memory list — never call `Read()` on keystroke (established anti-pattern: per-keystroke live reads freeze typing; live state refreshes only on `OnNavigatedTo` and after apply/revert, cf. `TweaksViewModel.OnNavigatedTo` row-refresh loop [VERIFIED: TweaksViewModel.cs:53-81]).
- **Tap behavior (D-09):** `SuggestionChosen` sets the box text (with `args.SelectedItem`) and applies it as the list filter; it never ticks a row and never navigates. `QuerySubmitted` (Enter) applies whatever text is present as the filter. Clearing the box clears the filter (all groups visible).
- **List filtering:** VM holds `ObservableCollection<GamingCatalogGroupView>` (4 groups, D-03) or a flat filtered view; simplest shape honoring the UI-SPEC layout (`AkariSectionHeader` + `AkariCard` per group) is per-group `ObservableCollection<RowItem>` rebuilt on filter change, with group headers collapsed when their filtered collection is empty, and the "No matching tweaks" empty state when all four are empty (copy contract in UI-SPEC).
- **State/robustness:** query matching uses `string.StartsWith/Contains` with `StringComparison.OrdinalIgnoreCase` — culture-invariant, no crash surface on empty/whitespace query (treat as no-filter).

## 4. Bulk Runner (SFT-01 apply half; D-11–D-15)

### Canonical shape to copy

`TweaksViewModel.ApplyRowAsync` is the closest precedent: `IsBusy` guard → `_status.Start(title)` → `SetRowsBusy(true)` fan-out → `await Task.Run(work)` → InfoBar → `finally { unlock; IsBusy=false; _status.Complete(); }` [VERIFIED: TweaksViewModel.cs:158-186]. `GamingViewModel.ApplyToggle` adds the rollback-on-failure idiom (`rollback(Safe(read))` in catch [VERIFIED: GamingViewModel.cs:202-229]) — at bulk scale this becomes journal replay, not read-back rollback. `CheckViewModel.RunAsync` adds the dialog-report step (`ShowInfoAsync` after work [VERIFIED: CheckViewModel.cs:86-109]).

### Recommended run procedure (planner: one RelayCommand, `ApplySelectedAsync`)

1. Guard `IsBusy`; collect ticked rows in catalog order (D-11: ticked only, no apply-all).
2. **Restore-point offer (D-15)** via `IDialogService.ShowAsync(title, body, primaryText: "Create restore point and continue", secondaryText: "Skip and apply", cancelText: null)` — signature supports primary+secondary [VERIFIED: IDialogService.cs:13-18]. Primary → `await Task.Run(RestorePointActions.CreateRestorePoint)`; on failure show warning InfoBar and **continue** (offered-not-forced); secondary/cancel → continue immediately. `CreateRestorePoint` throws readable errors on disabled protection [VERIFIED: RestorePointActions.cs:39-49] — catch and downgrade to warning, never block.
3. `IsBusy=true`, fan `IsBusy` to all row items (mirrors `GamingViewModel.OnIsBusyChanged` → tiles [VERIFIED: GamingViewModel.cs:67-71] and `TweaksViewModel.SetRowsBusy` [VERIFIED: TweaksViewModel.cs:190-195]), `_status.Start("Applying…")`.
4. Sequential loop over ticked rows on `Task.Run`: per row → `GamingJournal.Capture(row)` → `row.Apply(true)` → on success mark journaled + `_status.Report(done/total, $"Applying {done} of {total}: {title}")` (`Report(double, string?)` exists with determinate `Value` [VERIFIED: IStatusService.cs:18,53-59]); on exception → record `(FailedRow, ex)`, **break** (D-14).
5. After: if failed → InfoBar error with UI-SPEC copy (`"{Row title}" failed — the run stopped…`), failed row flagged (destructive title state per UI-SPEC), remainder untouched; else InfoBar success (`12 tweaks applied`). Ticks untouched in both cases (D-12). `finally`: unlock rows, `IsBusy=false`, `_status.Complete()`.
6. **Revert (`RevertAllAsync`, D-16):** one click, no confirmation dialog; guard `IsBusy`; `Task.Run` reverse-replay with per-row continue-on-error; InfoBar summary (restored count + any per-row failures); journal file cleared (or tombstoned with timestamp) after fully-successful revert so a second click is a no-op with "nothing to revert" InfoBar — empty-journal revert must not throw.

### Deliberate deviation from project research

`.planning/research/ARCHITECTURE.md` Pattern 3 prescribes **continue-on-error** bulk runs. D-14 (stop at first failure) overrides it — CONTEXT.md locked decisions outrank prior research. The plan must not "fix" this back to continue-on-error.

## 5. New Page Registration (D-02)

Follows the exact 13-page pattern [VERIFIED: App.xaml.cs:147-175, MainWindow.xaml.cs:82-96]:

1. `App.xaml.cs` `BuildHost`: `AddTransient<GamingCatalogViewModel>()` + `AddTransient<GamingCatalogPage>()` (transient, beside the existing registrations).
2. `MainWindow.xaml.cs` `NavItems`: one `new("Gaming Catalog", "<glyph>", typeof(GamingCatalogPage))` entry — position adjacent to "Gaming Tweaks" (exact slot is planner's choice; suggested directly after it). Glyph: Segoe Fluent Icons `FontIcon` string like neighbors.
3. New files: `Views/GamingCatalogPage.xaml` + `.xaml.cs` (3-line code-behind: resolve VM + `LocalizedStrings` from `App.Services`, `DataContext = ViewModel`, per `CheckPage.xaml.cs` precedent), `ViewModels/GamingCatalogViewModel.cs` (+ `GamingCatalogRowItem` in the same file, mirroring `CpRowItem` co-location [VERIFIED: TweaksViewModel.cs:231-290]).
4. **No `.csproj` edit needed:** `Compile Include="ViewModels\*.cs"`, `"Views\*.xaml.cs"`, `"Tweaks\*.cs"` and `Page Include="Views\*.xaml"` are wildcard globs [VERIFIED: AppTemplate.App.csproj:61-70] — new files are picked up automatically.
5. VM conventions: `public partial class GamingCatalogViewModel : ViewModelBase, INavigationAware`; state as `[ObservableProperty]` backing fields; `_loading` guard suppressing change-handlers during hydration; `partial void On<Prop>Changed` for search-text → re-filter; `IsBusy` hand-written over `_busy` only if row fan-out needs it (else `[ObservableProperty]` + `OnIsBusyChanged` partial like `GamingViewModel` [VERIFIED: GamingViewModel.cs:67-71]).
6. Row item: `public sealed partial class GamingCatalogRowItem : ObservableObject` holding the `GamingCatalogEntry`, `[ObservableProperty] bool _isSelected` (TwoWay-bound `CheckBox`; **no item command needed** — CheckBox binds to the property directly, unlike `GamingButton`/`CpRowItem` which need commands for buttons/toggles), `[ObservableProperty] bool _isBusy` + `IsEnabled => !IsBusy` [VERIFIED pattern: TweaksViewModel.cs:257-268]. Live-state display (applied/restart badge) via plain properties refreshed in `RefreshFromSystem()` with `Safe()` wrappers [VERIFIED pattern: GamingViewModel.cs:131-134].

## 6. XAML Binding Notes (load-bearing)

- Page root uses `{Binding}` with `DataContext = ViewModel`; **page-level `x:Bind` dies with WMC9999** (project constraint). `x:Bind` only inside `DataTemplate` with `x:DataType="...GamingCatalogRowItem"`, binding `CheckBox IsChecked="{x:Bind IsSelected, Mode=TwoWay}"` and explanation `Text="{x:Bind Entry.WhatItDoes, Mode=OneWay}"` — same shape as `GamingButton` template (`Command="{x:Bind RunToolCommand}"`, `Content="{x:Bind Label}"` [VERIFIED: GamingPage.xaml:209-214]).
- Row grid: two columns (`*` + `Auto`, `ColumnSpacing="16"`), left text stack (`Spacing="2"`) with title + 4 explanation lines (`AkariCardDescription`, `TextWrapping="Wrap"`) + `Requires restart` caption badge (`AkariCaption`, `Visibility` via `InvertedBooleanToVisibilityConverter`-style converter or `x:Bind` function), right `CheckBox` (+ state label). Follows UI-SPEC layout contract + `GamingPage.xaml` row-grid precedent [VERIFIED: GamingPage.xaml:28-45].
- Top bar: `AutoSuggestBox` + `Apply selected (N)` (`AccentButtonStyle`) + `Revert all changes` (default style — revert is the safe action, NOT destructive-styled, per UI-SPEC color contract). Apply button `IsEnabled` bound to selection-count > 0 && !IsBusy (zero-one-many state).
- No hardcoded colors/fonts; `ToggleSwitch`-style `MinWidth="0"` where applicable; page container `ScrollViewer > StackPanel Padding="32" Spacing="16" MaxWidth="1000"`.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Suggestion dropdown UI | Custom popup/list filtering control | In-box `AutoSuggestBox` + `TextChanged`/`SuggestionChosen` | Tested keyboard/mouse/screen-reader behavior; cap-8 + scrollable dropdown is built in |
| Progress footer | Dialog or inline spinners | `IStatusService.Start/Report/Complete` (shell footer) | D-13 locked; ref-counted so overlapping runs don't flicker [VERIFIED: IStatusService.cs:38-68] |
| Restore-point offer dialog | Custom ContentDialog XAML | `IDialogService.ShowAsync` primary/secondary buttons | Serialized gate, XamlRoot handled, no UI types in VM |
| `.reg` parsing for capture | New parser from scratch | Generalize `ParseProbe` line shapes (`dword:`/`"str"`/`[key]`) to full-body enumeration | Shapes already proven [VERIFIED: ControlPanelRows.cs:47-74] |
| Journal file writes | Ad-hoc `File.WriteAllText` | Temp-file + `Move(overwrite:true)` atomic pattern | Crash mid-write must never corrupt the journal [VERIFIED pattern: FileSettingsStorage.cs:91-99] |
| Registry value restore | "Import the Default body" as revert | Write-back of captured prior values | Default bodies *delete* values (`=-` lines [VERIFIED: ControlPanelRows.Generated.cs:28-36]) — that destroys pre-existing user data; revert ≠ reset-to-script-default |

**Key insight:** the journal makes revert a data problem (capture → write back), not a logic problem (no per-tweak revert methods to write or keep symmetric). The existing asymmetric-revert debt (`RevertNetworkOptimization` = winsock reset) is exactly what this retires.

## Common Pitfalls

### P1. Journaling the probe instead of the footprint
**What goes wrong:** snapshot captures only the first Optimize value (like `ReadRowOptimized`), revert restores 1 of N values, row reads "not applied" forever after. **Why:** copying the probe pattern for the capture pattern. **Avoid:** Footprint = full Optimize-body enumeration (native entries: explicit list beside the tweak). **Detect:** journal-integrity test asserts `entry.Values.Count > 1` for known multi-value rows (preemption = 6).

### P2. Revert-as-Default destroys user data
**What goes wrong:** reverting a shared row imports its Default body, deleting values the user had customized before ever opening the app. **Avoid:** revert only ever writes captured prior values; Default bodies are never used in the revert path. **Detect:** round-trip test seeds a sentinel value, applies, reverts, asserts sentinel restored.

### P3. Bulk apply on the UI thread
**What goes wrong:** `regedit /S` imports + netsh + service probes freeze the page for seconds; `AutoSuggestBox` typing dies mid-run. **Avoid:** all apply/capture/revert work inside `await Task.Run(...)`; `IProgress<T>`/`StatusService.Report` marshal back. Precedent in every existing run site.

### P4. Clearing ticks after apply (violates D-12)
**What goes wrong:** "helpful" reset of `IsSelected` after success breaks re-run/inspect and contradicts the UI-SPEC success state. **Avoid:** never mutate `IsSelected` in apply/revert paths; say so explicitly in the plan.

### P5. Blocking apply on restore-point failure (violates D-15)
**What goes wrong:** `CreateRestorePoint` throws (disabled protection is common on gaming rigs) and the plan treats it as fatal. **Avoid:** catch → warning InfoBar → continue; journal-first means per-tweak undo works regardless (established PITFALLS.md stance).

### P6. Page-level `x:Bind` (WMC9999)
**What goes wrong:** build breaks with `WMC9999` on the new page. **Avoid:** page root `{Binding}` + `DataContext`; `x:Bind` only in `DataTemplate` with `x:DataType`. There is a `SetAkariXamlLocalAssembly` workaround target in the csproj, but the convention stands.

### P7. Live reads on keystroke / on the UI thread
**What goes wrong:** search typing stutters; WMI/service probes block the UI. **Avoid:** keystroke path touches only the in-memory descriptor list; `Read()` calls happen in `OnNavigatedTo` (behind `Task.Run` if slow) and after apply/revert.

### P8. Game Mode / DVR seed rows have no native methods yet
**What goes wrong:** plan assumes `GamingActions.ReadGameMode` exists. **Avoid:** plan includes extracting `ReadGameMode`/`SetGameMode` + `ReadGameDvr`/`SetGameDvr` from the `fso` toggle's key set as a Wave-0 domain task; `fso` itself is untouched (D-01 seeds Game Mode/DVR as catalog rows, not the FSO bundle).

### P9. `RegRead` returns null for missing values — capture must distinguish absent vs zero
**What goes wrong:** `Dword(...) is null` (absent) vs `0` conflated; revert writes `0` where the value never existed, leaving residue. **Avoid:** `JournalValue.Kind is null` = absent = `DeleteValue` on revert; helpers return null shorthands, so capture must check `HasValue` first [VERIFIED: AkariToolState.cs:116-123].

### P10. `verify-pages.ps1` nav-count drift
**What goes wrong:** the UIA click-through script enumerates 13 nav items; a 14th page breaks its count assumptions. **Avoid:** plan includes updating `verify-pages.ps1` expectations (or confirming it enumerates dynamically).

## Code Examples

### Descriptor entries (follow `AkariTweakCatalog.Toggle` factory shape)

```csharp
// Tweaks/GamingCatalog.cs — FromNative + FromSharedRow factories
public static IReadOnlyList<GamingCatalogEntry> All { get; } = Build();
private static IReadOnlyList<GamingCatalogEntry> Build() => [
    GamingCatalogEntry.FromNative(
        "disable-preemption", GamingGroup.System,
        "Disable Preemption (NVIDIA)",
        whatItDoes: "Turns off GPU thread preemption on NVIDIA cards.",
        whyItHelpsGaming: "Removes scheduling interruptions for steadier frame times.",
        risk: "Can reduce stability on some NVIDIA driver versions.",
        revertsBy: "Restores all 6 prior scheduler values from the journal.",
        requiresReboot: true,
        read: GamingActions.ReadPreemption,
        applyOn: () => GamingActions.SetPreemption(true),
        applyOff: () => GamingActions.SetPreemption(false),
        footprint: [(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler", "EnablePreemption"),
                    (@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm", "DisablePreemption"),
                    /* + 4 more nvlddmkm values per GamingActions.SetPreemption */]),
    // Shared-row cross-reference (D-04): no .reg text duplicated here.
    GamingCatalogEntry.FromSharedRow(
        ControlPanelRows.All.First(r => r.Id == "<gaming-relevant-id>"),
        GamingGroup.Background, /* + 4 explanation strings, reboot flag */),
];
```

### Bulk loop (follow `TweaksViewModel.ApplyRowAsync` shape)

```csharp
// ViewModel orchestration (cf. TweaksViewModel.cs:158-186 + CheckViewModel.cs:86-109)
if (IsBusy) return;
IsBusy = true; SetRowsBusy(true);
_status.Start("Applying gaming tweaks…");
try
{
    var ticked = Rows.Where(r => r.IsSelected).ToList(); // catalog order; D-11
    int done = 0;
    foreach (var row in ticked)
    {
        try
        {
            await Task.Run(() => { GamingJournal.Capture(row.Entry); row.Entry.Apply(true); });
            GamingJournal.MarkApplied(row.Entry.Id); // completed-prefix only; D-14
            done++;
            _status.Report(100.0 * done / ticked.Count, $"Applying {done} of {ticked.Count}: {row.Entry.Title}");
        }
        catch (Exception ex) { FailedRow = row; FailMessage = ex.Message; break; } // stop at first failure
    }
    _infoBar.Show(FailedRow is null ? $"{done} tweaks applied" : $"\"{FailedRow.Entry.Title}\" failed — the run stopped before applying the rest. Fix or untick it, then re-run.",
        FailedRow is null ? "Ticks stay selected for re-run." : $"Details: {FailMessage}");
}
finally { SetRowsBusy(false); IsBusy = false; _status.Complete(); }
// Ticks never cleared (D-12). Restore-point offer precedes this block via IDialogService.ShowAsync.
```

### Title-only prefix-first filter (D-06–D-08)

```csharp
// Pure LINQ over GamingCatalog.All; called from TextChanged (UserInput) + QuerySubmitted.
private static IReadOnlyList<GamingCatalogEntry> Suggest(string q, int cap = 8)
{
    if (string.IsNullOrWhiteSpace(q)) return [];
    return [.. GamingCatalog.All.Where(e => e.Title.StartsWith(q, StringComparison.OrdinalIgnoreCase)),
            .. GamingCatalog.All.Where(e => !e.Title.StartsWith(q, StringComparison.OrdinalIgnoreCase)
                                         && e.Title.Contains(q, StringComparison.OrdinalIgnoreCase))].Take(cap).ToList();
}
```

## File Touch List

| File | Action | Why |
|------|--------|-----|
| `src/AppTemplate.App/Tweaks/GamingCatalog.cs` | **NEW** | `GamingGroup` enum + `GamingCatalogEntry` record + `FromNative`/`FromSharedRow` + seed `All` |
| `src/AppTemplate.App/Tweaks/GamingJournal.cs` | **NEW** | `JournalValue`/`JournalEntry`/`GamingJournal` + `Capture`/`MarkApplied`/`RevertAll`/load/save + full-body `.reg` footprint parser |
| `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs` | **NEW** | Search text, per-group filtered collections, ticked selection, `ApplySelectedAsync`, `RevertAllAsync`, `GamingCatalogRowItem` (same file, cf. `CpRowItem`) |
| `src/AppTemplate.App/Views/GamingCatalogPage.xaml` | **NEW** | Top bar (AutoSuggestBox + Apply + Revert), 4 group cards with CheckBox rows + inline explanations + reboot badge, empty state |
| `src/AppTemplate.App/Views/GamingCatalogPage.xaml.cs` | **NEW** | 3-line code-behind (VM + strings resolve, `DataContext`) |
| `src/AppTemplate.Tests/GamingCatalogTests.cs` | **NEW** | Catalog integrity (ids unique, shared IDs resolve, explanations non-empty, no registry paths in copy), suggest ranking (prefix > substring, cap 8, title-only), journal round-trip (capture → mutate → revert → sentinel restored) |
| `src/AppTemplate.App/Tweaks/GamingActions.cs` | EXTEND | Add `ReadGameMode`/`SetGameMode` + `ReadGameDvr`/`SetGameDvr` extracted from `fso` key set; `fso` toggle untouched |
| `src/AppTemplate.App/App.xaml.cs` | EDIT (2 lines) | `AddTransient<GamingCatalogViewModel>()` + `AddTransient<GamingCatalogPage>()` |
| `src/AppTemplate.App/MainWindow.xaml.cs` | EDIT (1 line) | One `NavItems` entry after "Gaming Tweaks" |
| `src/AppTemplate.App/Tweaks/ControlPanelRows.cs` | READ-ONLY | Do not modify; journal parser lives in `GamingJournal.cs` |
| `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs` | READ-ONLY | Generated — never edit by hand |
| `src/AppTemplate.App/AppTemplate.App.csproj` | NO CHANGE | Wildcard globs cover new files [VERIFIED: .csproj:61-70] |
| `verify-pages.ps1` | CHECK | Confirm it enumerates nav dynamically or update its 13-item expectation |

## State of the Art

| Old Approach | Current Approach | Impact for Phase 1 |
|--------------|------------------|--------------------|
| `TweakToggle` on/off lambdas as the only descriptor | Record descriptor + footprint + journal (ARCHITECTURE.md Pattern 1+2) | Multi-value + shared-row tweaks get true revert, not best-effort inverse lambdas |
| `RevertNetworkOptimization` = `netsh winsock reset` | Journal write-back of captured NIC values | Revert actually restores what apply changed |
| `ReadRowOptimized` first-value probe as state | Probe stays for *display*; Footprint drives *capture* | Display can be approximate; revert must be complete |
| Restore point as the only safety net | Journal-first + restore-point offer (PITFALLS.md Pitfall 6) | D-15 skip-path is safe because per-tweak undo never depended on restore |

**Deprecated/outdated:** none introduced by this phase. `GamingPage`, `fso` toggle, and all 169 rows stay untouched.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | `AutoSuggestBox` events are `TextChanged` (+`AutoSuggestionBoxTextChangeReason.UserInput`), `SuggestionChosen`, `QuerySubmitted` with `ItemsSource`-bound suggestions on WASDK 2.3.1 | §3 | LOW — standard WinUI 3 API, but plan should verify names against SDK contracts during implementation; fallback is `TextBox` + `ListView` popup |
| A2 | Seed set = preemption + network opt + Game Mode + DVR (exact "current gaming rows" membership) | §1 | MEDIUM — planner confirms the D-01 row list against `GamingViewModel` before Wave 0; extra/missing rows change catalog size, not architecture |
| A3 | `RequiresReboot` per-row flags (preemption YES, network MAYBE, Game Mode/DVR NO) | §1 | LOW — badge is display-only, never blocks |
| A4 | Network-opt netsh globals treated as uncaptured side effects with honest copy | §2 | MEDIUM — discuss-phase should confirm honest-copy wording is acceptable vs. extending capture to `netsh show` parsing |
| A5 | `verify-pages.ps1` hardcodes 13 nav items | §P10 | LOW — one-line script check at plan time |

## Open Questions

1. **Exact D-01 seed membership**
   - What we know: preemption + network opt are the two live toggles on `GamingPage`; Game Mode + DVR keys live inside the `fso` toggle.
   - What's unclear: whether "current gaming rows" also includes the NVIDIA/AMD graphics toggles and SvcHost/Win32Priority dropdowns as catalog seeds.
   - Recommendation: planner locks the seed ID list (suggested: preemption, network-opt, game-mode, game-dvr + the gaming-relevant subset of the 169 shared rows) before Wave 0; architecture is extra/missing rows change catalog size, not architecture.

2. **Journal retention across app restarts**
   - What we know: one-click revert must work after apply; ticks are sticky but journal file persists regardless.
   - What's unclear: whether revert-after-restart is required (journal survives) or revert is session-scoped (journal cleared on launch).
   - Recommendation: persist across restarts (enables "applied yesterday, revert today"); surface row-count in the revert button tooltip.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| .NET 10 SDK | Build + test | ✓ (assumed — repo pins 10.0.400 in global.json) | check at plan time (`dotnet --version`) | — (blocking if missing) |
| Windows 10/11 x64 + admin | Registry HKLM writes, restore point | ✓ (assumed — project constraint) | — | None — OS-level requirement |
| NuGet (nuget.org) | Restore | Assumed | — | None needed — zero new packages |

No new external dependencies. Test plan runs `dotnet test` (xUnit, UI-free logic only — `new Page()` throws under testhost per architecture constraints).

## Validation Architecture

Nyquist validation config not inspected this session; planner confirms whether `workflow.nyquist_validation` applies. Recommended coverage regardless:

### Test Framework
| Property | Value |
|----------|-------|
| Framework | xUnit 2.9.3 + `dotnet test` |
| Config file | `src/AppTemplate.Tests/AppTemplate.Tests.csproj` (existing) |
| Quick run command | `dotnet test src/AppTemplate.Tests --filter GamingCatalog` |
| Full suite command | `dotnet test src/AppTemplate.Tests` |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| BRW-01 | Title-only search, prefix > substring, cap 8, 1-char trigger | unit | `dotnet test --filter SuggestRanking` | ❌ Wave 0 (`GamingCatalogTests.cs`) |
| BRW-01 | Shared-row IDs resolve to `ControlPanelRows.All` | unit | `dotnet test --filter CatalogIntegrity` | ❌ Wave 0 |
| BRW-02 | Every entry has 4 non-empty explanation lines, no registry paths in copy | unit | `dotnet test --filter ExplanationContract` | ❌ Wave 0 |
| SFT-01 | Capture → mutate → revert restores sentinel (incl. absent→delete) | unit (HKCU scratch keys — safe without admin) | `dotnet test --filter JournalRoundTrip` | ❌ Wave 0 |
| SFT-01 | Stop-at-first-failure leaves completed-prefix journal; revert replays reverse | unit (fake entries) | `dotnet test --filter BulkSemantics` | ❌ Wave 0 |
| SFT-01 | Bulk apply progress + one-click revert end-to-end | manual UAT | `build-and-run.ps1` + `verify-pages.ps1` | N/A (UI) |

### Sampling Rate
- **Per task commit:** `dotnet build` with the Errors:0 Warnings:0 gate (kill `AkariToolbox.App` first — apphost lock).
- **Phase gate:** full suite green + manual UAT (search → tick → offer → apply → footer progress → sticky ticks → one-click revert → values restored).

### Wave 0 Gaps
- [ ] `src/AppTemplate.Tests/GamingCatalogTests.cs` — all rows above
- [ ] `src/AppTemplate.App/Tweaks/GamingActions.cs` Game Mode/DVR methods (P8) — needed before catalog seeds compile
- [ ] Framework install: none (xUnit already referenced)

## Security Domain

`security_enforcement` config not inspected; treating as enabled (native-ops admin app — the domain demands it).

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | Local desktop app, no accounts |
| V3 Session Management | No | No sessions |
| V4 Access Control | Yes | Admin manifest (`requireAdministrator`) + runtime elevation is the gate; catalog rows are the allowlist — no generic "write any key" path reachable from UI (PITFALLS.md security stance) |
| V5 Input Validation | Yes | Search query is display-only (never executed, never a registry path); `SuggestionChosen` casts guarded; journal JSON deserialization uses typed schema with `version` check, corrupt file → empty journal (same degrade-to-default rule as `FileSettingsStorage` [VERIFIED: FileSettingsStorage.cs:83-87]) |
| V6 Cryptography | No | No secrets; journal holds machine config only, local-only, never exported |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Privileged arbitrary registry write via XAML/binding bug | Tampering / Elevation | Apply path only touches Footprint-declared + shared-row-body keys; log every privileged write with calling catalog ID |
| Journal tampering → revert writes attacker keys | Tampering | Journal is local-only, same-trust as the app itself (admin); schema-validated on load; no network surface |
| Concurrent apply from second instance | Tampering (integrity) | Single-instance guard exists (`FindOrRegisterForKey("AkariToolbox")` [VERIFIED: App.xaml.cs:97]); `IsBusy` re-entrancy guard per run |

## Sources

### Primary (HIGH confidence — read this session)
- `src/AppTemplate.App/Tweaks/TweakToggle.cs:8-30` — toggle shape
- `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs:297-329,543-547` — fso key set, Toggle factory
- `src/AppTemplate.App/Tweaks/GamingActions.cs:35-69,144-184` — preemption symmetry, network persisted-read + winsock revert gap
- `src/AppTemplate.App/Tweaks/ControlPanelRows.cs:18-74` — probe-first-value + parser shapes
- `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs:8` — CpTweakRow shape
- `src/AppTemplate.App/Tweaks/NativeOps.cs:896-907` — ImportRegContent via regedit /S
- `src/AppTemplate.App/Tweaks/RestorePointActions.cs:39-49` — readable throw on disabled protection
- `src/AppTemplate.App/Tweaks/AkariToolState.cs:98-123` — RegRead null-on-missing semantics
- `src/AppTemplate.App/ViewModels/GamingViewModel.cs:67-71,131-134,202-229` — IsBusy fan-out, Safe(), rollback-on-failure
- `src/AppTemplate.App/ViewModels/TweaksViewModel.cs:158-195,231-290` — bulk run-site shape, CpRowItem co-location
- `src/AppTemplate.App/ViewModels/CheckViewModel.cs:86-109` — dialog-report run shape
- `src/AppTemplate.App/App.xaml.cs:97,147-175` — single-instance, DI registration pattern
- `src/AppTemplate.App/MainWindow.xaml.cs:82-96` — NavItems pattern
- `src/AppTemplate.App/Views/GamingPage.xaml:28-45,209-214` — row grid + DataTemplate x:Bind
- `src/AppTemplate.App/AppTemplate.App.csproj:61-70` — wildcard globs (no csproj edit)
- `src/AppTemplate.Framework/Services/IStatusService.cs:38-68` — Start/Report/Complete ref-count
- `src/AppTemplate.Framework/Services/IDialogService.cs:13-18` — ShowAsync primary/secondary
- `src/AppTemplate.Framework/Services/FileSettingsStorage.cs:83-99` — corrupt-degrade + atomic save
- `.planning/PROJECT.md`, `REQUIREMENTS.md`, `ROADMAP.md`, `research/{SUMMARY,ARCHITECTURE,PITFALLS}.md`, phase `01-CONTEXT.md`, `01-UI-SPEC.md` — scope + locked decisions

### Secondary (MEDIUM confidence)
- UI-SPEC layout/typography/spacing/copy contracts (generated contract, taken as locked input)

### Tertiary (LOW confidence — flagged [ASSUMED] inline)
- WinUI 3 `AutoSuggestBox` event/property names (A1); exact seed membership (A2); per-row reboot flags (A3)

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — zero new packages; pinned versions from codebase map
- Descriptor + journal architecture: HIGH — every precedent read from source this session
- Search/suggest API shape: MEDIUM — standard WinUI pattern, names need SDK-contract check (A1)
- Pitfalls: HIGH — grounded in verified code gaps (winsock revert, probe-vs-footprint, absent-vs-zero)

**Research date:** 2026-10-03
**Valid until:** 2026-11-02 (stable domain; pinned stack)
