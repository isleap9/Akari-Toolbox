# Architecture Research

**Domain:** Windows gaming optimizer / tweak-catalog desktop app (WinUI 3, native ops only)
**Researched:** 2026-10-03
**Confidence:** HIGH (grounded in live codebase map `.planning/codebase/ARCHITECTURE.md` + `Tweaks/` source reads)

## Standard Architecture

### System Overview

Tweak-catalog optimizers (Winhance, Chris Titus WinUtil, MajorGeeks-style tuners, FR33THY script sets) all converge on the same 4-layer shape. Akari Toolbox already implements it — the gaming push extends the **domain catalog** and **browse/runner** layers, not the shell:

```
┌─────────────────────────────────────────────────────────────┐
│                    SHELL / COMPOSITION ROOT                  │
│  App.xaml.cs (Host DI, single-instance, headless safe-boot) │
│  MainWindow (NavigationView 13 pages, InfoBar, status footer)│
├─────────────────────────────────────────────────────────────┤
│  PRESENTATION (thin)      │  ORCHESTRATION (stateful VMs)    │
│  ┌──────────────────┐     │  ┌──────────────────────────┐    │
│  │ Views/*.xaml     │────▶│  │ GamingBrowseViewModel    │    │
│  │ search box,      │bind │  │ filter/selection/progress│    │
│  │ grouped list,    │     │  │ BulkRunnerViewModel-ish  │    │
│  │ per-tweak expander│    │  │ (or commands on same VM) │    │
│  └──────────────────┘     │  └────────────┬─────────────┘    │
├───────────────────────────┴───────────────┴─────────────────┤
│                    DOMAIN (static, UI-free)                  │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐   │
│  │ TweakCatalog │  │ *Actions     │  │ RollbackStore +   │   │
│  │ descriptors  │  │ native apply │  │ Snapshot          │   │
│  │ (read/apply/ │  │ (registry/   │  │ (pre-apply values │   │
│  │  revert meta)│  │  svc/bcdedit)│  │  + restore)       │   │
│  └──────────────┘  └──────────────┘  └───────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│              FRAMEWORK SHELL (reusable MVVM infra)           │
│  INavigationService, IStatusService (ref-counted),           │
│  IInfoBarService, IDialogService (serialized),               │
│  ISettingsService, Messenger bus, Converters/Behaviors       │
├─────────────────────────────────────────────────────────────┤
│              WINDOWS OS (registry/services/WMI)              │
│  NativeOps, SystemInfo (cached WMI), AkariToolState          │
│  (HKCU\Software\AkariTool), settings.json, rolling logs      │
└─────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| Shell / composition root | DI graph, nav wiring, elevation, single-instance, headless reboot flows, global error handling | Already done: `App.xaml.cs`, `MainWindow.xaml(.cs)` — do not touch except to register new VMs/pages |
| Browse View (thin) | Search TextBox, category group headers, per-tweak row (title + toggle/checkbox + info expander), bulk-apply bar, progress + result list | New/ reworked `GamingPage.xaml` reusing `TweaksPage.xaml` row patterns; page-level `{Binding}`, `x:Bind` only inside `DataTemplate` |
| Browse ViewModel | Search text → filtered view; category grouping; selection set; `IsBusy` guard; bulk-apply + rollback commands; progress reporting; result reporting via InfoBar/dialogs | Extend `GamingViewModel.cs` (or new `GamingBrowseViewModel` if page splits); `ObservableCollection<GamingTweakRow>` + `ICollectionView`-style filtering; `IStatusService.Start/Complete` + `IProgress<T>` |
| Tweak descriptor catalog | One record per tweak: stable id, category, title, description, risk/reboot flags, `read`/`apply`/`revert` delegates | New `Tweaks/GamingTweakCatalog.cs` following `AkariTweakCatalog.cs` (32 toggles) and `ControlPanelRows(.Generated).cs` (169 rows) patterns; descriptors delegate to `GamingActions`/`GraphicsActions`/`AdvancedActions` — never duplicate native logic |
| Native action modules | The actual mutations: registry writes, service `Start` values, `bcdedit`/`powercfg`/`sc`, `netsh`, driver flows | Existing `Tweaks/*Actions.cs` statics + `TweakAction` hierarchy (`RegDword/Qword/String/Binary`, `RegDeleteValue/Key`, `Run`, `Launch`, `Open`, `Custom`) — gaming push adds methods here, not in VMs |
| Bulk runner | Ordered apply of N selected tweaks off the UI thread, per-item success/fail capture, continue-on-error, progress %, cancellation | Small static `Tweaks/BulkApplyRunner.cs` (or private VM helper if kept simple): `await Task.Run(() => { foreach tweak: try apply → record ok; catch → record fail })`; reports via `IProgress<BulkProgress>` back to VM |
| Rollback / snapshot store | Pre-apply snapshot of every value about to change (registry value or "absent", service start), plus full-catalog revert pass; restore-point creation before bulk runs | New `Tweaks/TweakSnapshot.cs` + `Tweaks/RollbackStore.cs` backed by `AkariToolState` (persisted choice memory) + in-memory/file snapshot of pre-apply values; reuses `RestorePointActions` for the system restore point |
| Safety gate | Restore-point creation, reboot-required flagging, never-break-updates/Store/drivers invariant | Existing `RestorePointActions` + guidance dialogs via `IDialogService`; bulk-apply always offers/creates restore point first |
| Framework services | Progress footer, toasts, dialogs, navigation, messaging — VMs never touch UI types directly | Existing `IStatusService` (ref-counted), `IInfoBarService`, `IDialogService` (serialized, `Func<XamlRoot?>`), `WeakReferenceMessenger` bus — reuse as-is |

## Recommended Project Structure

```
src/AppTemplate.App/
├── Views/                    # 13 pages; Gaming browse rework lives here
│   ├── GamingPage.xaml       # search + grouped list + bulk bar (REWORK)
│   └── Shared/PageStyles.xaml# shared row/expander styles (EXTEND)
├── ViewModels/
│   ├── GamingViewModel.cs    # search filter + selection + bulk/rollback cmds (EXTEND)
│   └── GamingTweakRow.cs     # row item: descriptor + IsSelected + live state (NEW, cf. CpRowItem)
├── Tweaks/                   # domain — all gaming-push logic lands here
│   ├── GamingTweakCatalog.cs # descriptor list: id/category/title/desc/risk/read/apply/revert (NEW)
│   ├── BulkApplyRunner.cs    # ordered apply + per-item results + progress (NEW)
│   ├── TweakSnapshot.cs      # pre-apply value capture record (NEW)
│   ├── RollbackStore.cs      # snapshot persist + revert-all/selected (NEW)
│   ├── GamingActions.cs      # new native methods as needed (EXTEND)
│   ├── TweakAction.cs        # extend factories only if a new op shape appears (RARE)
│   ├── TweakToggle.cs        # unchanged single-toggle primitive (REUSE)
│   └── RestorePointActions.cs# pre-bulk restore point (REUSE)
└── Helpers/
    └── NativeMessageBox.cs   # headless fallback (REUSE)
```

### Structure Rationale

- **Views/:** Gaming browse is a presentation rework, not a new page — reworking `GamingPage.xaml` keeps the 13-page nav contract and `verify-pages.ps1` green. New pages would require nav registration + 4-file ceremony for no benefit.
- **ViewModels/:** `GamingTweakRow` item type co-located with its VM mirrors the proven `CpTabGroup`/`CpSectionGroup`/`CpRowItem` pattern in `TweaksViewModel` — reviewers already know this shape.
- **Tweaks/:** The codebase's hard rule is *domain is static and UI-free* (`Views → ViewModels → Tweaks → OS`). `GamingTweakCatalog`, `BulkApplyRunner`, `TweakSnapshot`, `RollbackStore` all belong here as UI-free statics/records so headless flows and unit tests can use them; the VM only orchestrates and reports.
- **No new Framework code:** Filtering, progress, dialogs, status, and messaging already exist in `AppTemplate.Framework`. Adding framework abstractions for a single page would be premature generalization.

## Architectural Patterns

### Pattern 1: Descriptor Catalog over Action Modules

**What:** Each tweak is a data record (`id`, `category`, `title`, `description`, `risk`, `requiresReboot`, `read`, `apply`, `revert`) in one static catalog; native code lives in `*Actions` modules; the catalog only wires delegates to them.
**When to use:** Always for browse/search/bulk scenarios — it is the pattern `AkariTweakCatalog` (32 toggles) and `ControlPanelRows` (169 rows) already use, and it is what makes search, grouping, and bulk-apply trivial (they operate on records, not code paths).
**Trade-offs:** Pro: single source of truth for UI text + behavior; adding a tweak = one catalog entry + one `*Actions` method. Con: indirection — the `read`/`apply`/`revert` triple must be kept consistent per entry; mitigate with a per-entry test or a `Validate()` that round-trips `read` after `apply` in a dry-run review.

**Example:**
```csharp
// Tweaks/GamingTweakCatalog.cs — follows AkariTweakCatalog.Toggle(...) shape
public sealed record GamingTweak(
    string Id, string Category, string Title, string Description,
    bool RequiresReboot, Func<bool> Read, Action Apply, Action Revert);

public static class GamingTweakCatalog
{
    public static IReadOnlyList<GamingTweak> All { get; } = Build();
    private static IReadOnlyList<GamingTweak> Build() => [
        new("game-mode", "System", "Game Mode", "Prioritises game threads…",
            RequiresReboot: false,
            Read: GamingActions.ReadGameMode,
            Apply: () => GamingActions.SetGameMode(true),
            Revert: () => GamingActions.SetGameMode(false)),
        // … one entry per gaming tweak, grouped by Category
    ];
}
```

### Pattern 2: Snapshot-Before-Apply Rollback

**What:** Before any bulk apply, capture the current value of every registry value / service `Start` the run will touch (including "absent" for values to be created); persist the snapshot; revert = write back captured values. System restore point is the coarse backstop, snapshot revert is the surgical path.
**When to use:** Mandatory for every bulk-apply and for any toggle whose `off` path is not the exact inverse of `on` (multi-value tweaks like preemption disable touch 6 values — revert must restore all 6, not just flip one).
**Trade-offs:** Pro: fully reversible line becomes mechanically enforceable; per-tweak rollback falls out for free. Con: snapshot code must know each tweak's footprint — keep footprint declaration next to the tweak (descriptor carries an `IReadOnlyList<RegTarget>` or a `Capture()` delegate) so it cannot drift.

**Example:**
```csharp
// Tweaks/TweakSnapshot.cs
public sealed record RegTarget(string Path, string Name); // value or "absent"
public sealed record TweakSnapshot(string TweakId, DateTime TakenUtc,
    IReadOnlyList<(RegTarget Target, object? Value, RegistryValueKind? Kind)> Values,
    IReadOnlyList<(string Service, int Start)> Services);
```

### Pattern 3: VM-Orchestrated Background Runner with Progress

**What:** Copy the canonical `CheckViewModel.RunAsync` shape at bulk scale: `IsBusy` guard → `IStatusService.Start` → `await Task.Run(runner)` with `IProgress<T>` marshalled to the UI thread → per-item results collection → summary dialog + InfoBar → `finally Complete()`. Failures are captured per item, never thrown out of the run.
**When to use:** Every bulk-apply and full-rollback operation.
**Trade-offs:** Pro: UI stays responsive through long runs (some flows shell `powercfg`/`sc`/WMI); overlapping runs share the ref-counted footer without flicker. Con: progress plumbing is boilerplate — keep `BulkApplyRunner` generic over `GamingTweak` so only one runner ever exists.

**Example:**
```csharp
// ViewModel orchestration (cf. CheckViewModel.cs:86-109)
IsBusy = true; _status.Start("Applying gaming tweaks");
try {
    var progress = new Progress<BulkProgress>(p => AppliedCount = p.Done);
    BulkResult result = await Task.Run(() =>
        BulkApplyRunner.Apply(selected, Snapshot.Take, progress, ct));
    await _dialogs.ShowInfoAsync("Bulk apply", result.Summary);
    _infoBar.Show(result.Failed == 0 ? "All tweaks applied" : $"{result.Failed} failed", result.Detail);
} catch (Exception ex) { _infoBar.Show("Bulk apply failed", ex.Message); }
finally { IsBusy = false; _status.Complete(); }
```

## Data Flow

### Request Flow

```
User types search / picks category / checks tweaks
    ↓ (binding)
GamingViewModel filters GamingTweakCatalog.All → FilteredRows (ObservableCollection<GamingTweakRow>)
    ↓ (Apply command)
VM: snapshot = RollbackStore.Capture(selected) → RestorePointActions.Create() (opt/confirmed)
    ↓
await Task.Run(() => BulkApplyRunner.Apply(selected, snapshot))   // off UI thread
    ↓ per item: descriptor.Apply() → *Actions native op → registry/service/OS
    ↓ per item: ok/fail recorded; IProgress<T> → VM progress props (UI thread)
VM ← BulkResult (per-item outcomes)
    ↓
IDialogService summary + IInfoBarService toast + IStatusService.Complete()
    ↓ (Rollback command, symmetric)
await Task.Run(() => RollbackStore.Revert(snapshot)) → per-item Revert() → verify via Read()
```

### State Management

```
Live OS state (registry/services)           Ephemeral UI state (VM)
    ↑ read on OnNavigatedTo                     [ObservableProperty] SearchText,
    │ (row.RefreshFromSystem per row,            SelectedCategory, IsBusy,
    │  cf. TweaksViewModel.cs:53-81)            AppliedCount, FilteredRows
    │                                           ↓
Persisted memory                         Shell-global singletons
HKCU\Software\AkariTool (per-tweak       IStatusService (ref-counted footer),
 choice via AkariToolState)               IInfoBarService (toasts),
Snapshot files / settings.json           Messenger (theme/culture/nav)
(rollback payloads)                       Navigation back/forward stacks
```

### Key Data Flows

1. **Browse/filter:** `SearchText`/`SelectedCategory` change → VM re-filters static `GamingTweakCatalog.All` into `FilteredRows`. Catalog is immutable; filtering is pure LINQ — no OS reads on keystroke. Live `Read()` happens once on `OnNavigatedTo` (and after apply/revert), never per keystroke.
2. **Bulk apply:** Selection set → snapshot capture → optional restore point → background ordered apply → per-item results → summary. Snapshot precedes *every* mutation; restore point precedes the run.
3. **Rollback:** Snapshot (pre-apply values) → background revert in reverse order → `Read()` verification per tweak → InfoBar/dialog report. Full-catalog revert and selected-only revert share the same path.
4. **Theme/culture/nav:** Unchanged — `ThemeChangedMessage`/`CultureChangedMessage` via messenger; navigation via `INavigationService`; new page content introduces no new cross-cutting flows.

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Current (single-user desktop, ~200 tweaks) | This design is the end state — static catalog + LINQ filter + sequential background apply is more than fast enough. No infra needed. |
| 500+ tweak rows | Virtualize the list (`ItemsRepeater` / incremental loading via framework `IncrementalLoadingCollection`); keep `Read()` lazy per visible row instead of all-rows on navigate. |
| Long bulk runs (driver/service flows) | Add cancellation (`CancellationToken` through `BulkApplyRunner`) and reboot-required batching (collect `RequiresReboot` flags, single reboot prompt at end). |

### Scaling Priorities

1. **First bottleneck:** `OnNavigatedTo` live-read of every row (registry + service + WMI probes) blocks page entry as row count grows — fix by reading lazily/async per row with a placeholder state, exactly as `TweaksViewModel` will need once the gaming catalog merges the 169-row set.
2. **Second bottleneck:** Bulk runs that touch services/drivers need a reboot to take effect — batch reboot-required tweaks and prompt once, rather than prompting per tweak or silently deferring.

## Anti-Patterns

### Anti-Pattern 1: Putting native logic in the ViewModel or code-behind

**What people do:** `GamingViewModel` writes registry keys or starts processes directly so "it's all in one place for the new page."
**Why it's wrong:** Breaks the `Views → ViewModels → Tweaks → OS` direction the whole codebase enforces; logic becomes untestable, unusable from headless safe-boot flows, and bypasses snapshot/rollback.
**Do this instead:** Every mutation goes in `Tweaks/*Actions.cs` as a static method; the catalog descriptor and the VM only call it.

### Anti-Pattern 2: Bulk-apply without snapshot (restore point as the only backstop)

**What people do:** Create a restore point, apply N tweaks, call it reversible.
**Why it's wrong:** Restore points are coarse, slow, sometimes disabled by debloat tweaks themselves, and cannot revert a *single* tweak — the product's "every tweak explained, visible, and reversible" promise requires per-tweak revert.
**Do this instead:** Snapshot-before-apply per tweak (Pattern 2) as the primary mechanism; restore point as the secondary backstop.

### Anti-Pattern 3: Per-keystroke live system reads during search

**What people do:** Re-run `Read()` for every row on each search keystroke to "keep state fresh."
**Why it's wrong:** Registry/service/WMI probes on the UI thread freeze typing; most reads are synchronous and some (WMI) are expensive.
**Do this instead:** Filter the immutable in-memory catalog on keystroke; refresh live state only on navigate-to, after apply/revert, and via explicit Refresh command.

### Anti-Pattern 4: New framework abstractions for one page

**What people do:** Add `ISearchService`, `IBulkApplyService`, `ITweakRepository` to `AppTemplate.Framework` for the gaming browse.
**Why it's wrong:** Framework must stay app-agnostic; single-page needs do not justify shared abstractions, and DI-registered services for static catalog data add lifetime complexity for zero benefit.
**Do this instead:** Static catalog + static runner in `Tweaks/`; reuse existing `IStatusService`/`IInfoBarService`/`IDialogService` for all UI effects.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| Windows Registry (`Microsoft.Win32`) | Direct via `TweakAction` hierarchy | Only native-op path; `RegistryPath.Resolve` handles HKLM/HKCU roots; elevated manifest required for HKLM |
| Service Control (`sc` / registry `Start`) | `TweakAction.Run("sc", …)` or `Start`-value writes | Capture pre-apply `Start` in snapshot; service changes may need reboot — flag via descriptor |
| `bcdedit` / `powercfg` / `netsh` / `nvidia-smi` | Hidden `RunProcessAction` (wait) | Never on UI thread; `GamingActions` already wraps these — extend, don't duplicate |
| WMI (`SystemInfo`) | Cached per-process reads | Synchronous — always behind `Task.Run`; dispose `ManagementObjectCollection` + objects (existing constraint) |
| System restore | `RestorePointActions` before bulk runs | May be disabled on user machines — treat failure as warning + require snapshot success before proceeding |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| GamingPage.xaml ↔ GamingViewModel | `{Binding}` (page-level; `x:Bind` only in `DataTemplate`) | Hard WinUI constraint (WMC9999) — follow `TweaksPage.xaml` row template precedent |
| GamingViewModel ↔ GamingTweakCatalog | Direct static read (`All`), LINQ filter | No DI, no events — catalog is immutable data |
| GamingViewModel ↔ BulkApplyRunner / RollbackStore | `await Task.Run(...)` + `IProgress<T>` + `CancellationToken` | Runner/store are UI-free; all dialogs/InfoBars stay in the VM |
| GamingTweakCatalog ↔ *Actions | Delegate references (`Read`/`Apply`/`Revert` → static methods) | Catalog never contains native code inline beyond one-liners; footprint for snapshot declared alongside |
| Headless flows ↔ Tweaks | `App.OnLaunched` → `Finish*` methods (existing) | New gaming actions that need safe-boot completion must add a `Finish*` + CLI flag pair, mirroring `DriverActions.FinishDdu` |

## Suggested Build Order (dependencies between components)

1. **`GamingTweakCatalog` descriptors first** — no UI, no runner; it forces the inventory of which gaming tweaks exist, their categories, descriptions, and `read/apply/revert` coverage. Gaps found here (tweaks with no revert) drive `*Actions` extension work.
2. **Missing `*Actions` native methods** — fill every `Apply`/`Revert` the catalog needs (CPU/timer/power, debloat, network, GPU). Each method is independently testable; this is the bulk of the work.
3. **`TweakSnapshot` + `RollbackStore`** — needs the action footprint from (2); build revert alongside apply, not after.
4. **`BulkApplyRunner`** — trivial once (1)–(3) exist (ordered loop + results + progress); needs snapshot API from (3).
5. **Browse View + ViewModel rework last** — search/filter/selection/progress UI binds to the finished catalog + runner; building UI first forces mock data that drifts from the real catalog.

## Sources

- Live codebase: `.planning/codebase/ARCHITECTURE.md`, `.planning/codebase/STRUCTURE.md` (2026-10-03 mapping)
- Source reads: `Tweaks/TweakAction.cs`, `Tweaks/TweakToggle.cs`, `Tweaks/AkariTweakCatalog.cs`, `Tweaks/GamingActions.cs` (head), `ViewModels/TweaksViewModel.cs` (head)
- Domain precedent: Winhance browse model (search + categories + per-tweak info, per PROJECT.md reference); Chris Titus WinUtil tweak-list + apply/undo shape; FR33THY script-set coverage already ported in `*Actions` modules

---
*Architecture research for: Windows gaming optimization desktop app (WinUI 3 MVVM, TweakAction/TweakToggle catalogs)*
*Researched: 2026-10-03*
