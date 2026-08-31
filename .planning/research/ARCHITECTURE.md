# Architecture Research

**Domain:** WinUI 3 MVVM desktop system-tweak/debloat utility (elevated, unpackaged, self-contained)
**Researched:** 2026-08-31
**Confidence:** HIGH (component boundaries, service decomposition, build order — grounded directly in the predecessor codebase and the target framework template) / MEDIUM (WinAppSDK elevation manifest behavior, PowerShell hosting trade-off — cross-checked against Microsoft's own GitHub repos but not independently reproduced in this environment)

## Standard Architecture

### System Overview

```
┌───────────────────────────────────────────────────────────────────────────┐
│  VIEW LAYER (XAML + minimal code-behind)                                   │
│  ┌──────────┐ ┌────────────────┐ ┌───────────┐ ┌───────────┐ ┌─────────┐ │
│  │ HomePage │ │ AkariOSTweaks   │ │ Gaming    │ │ Debloat   │ │ Downloads│ │
│  │          │ │ Page            │ │ Tweaks    │ │ Page      │ │ / Misc   │ │
│  └────┬─────┘ └────────┬────────┘ └─────┬─────┘ └─────┬─────┘ └────┬────┘ │
│       │  DataContext = ViewModel, bound via x:Bind / RelayCommand   │      │
├───────┴────────────────┴─────────────────┴─────────────┴────────────┴──────┤
│  VIEWMODEL LAYER (CommunityToolkit.Mvvm, source-generated)                 │
│  ObservableObject + [ObservableProperty] + [RelayCommand]/IAsyncRelayCommand│
│  Depends only on SERVICE INTERFACES + framework services (Nav/Dialog/InfoBar)│
├───────────────────────────────────────────────────────────────────────────┤
│  APPLICATION SERVICE LAYER (feature-facing interfaces, DI singletons)      │
│  ┌────────────────┐ ┌─────────────────┐ ┌──────────────────┐             │
│  │ ITweakCatalog  │ │ IDebloatService  │ │ IPostInstallService│           │
│  │ (was ITweak-   │ │ (runs 28 embed-  │ │ (mirrors asset    │           │
│  │  Service God-  │ │  ded .ps1 via    │ │  folder from      │           │
│  │  switch)       │ │  IScriptRunner)  │ │  GitHub)          │           │
│  └───────┬────────┘ └────────┬─────────┘ └─────────┬─────────┘           │
│          │  each ITweakHandler is its own class     │                     │
├──────────┴────────────────────┴──────────────────────┴────────────────────┤
│  SYSTEM PRIMITIVE / MUTATION LAYER (thin, UI-agnostic, mockable)           │
│  ┌────────────────┐ ┌─────────────────────┐ ┌───────────────────────┐    │
│  │ IRegistryService│ │ IWindowsServiceCtrl  │ │ IScriptRunner /        │   │
│  │ (HKLM/HKCU,     │ │ (start/stop/config   │ │ IProcessRunner         │   │
│  │  real-HKCU-     │ │  Win32 services)      │ │ (extract embedded ps1, │  │
│  │  under-elevation│ │                       │ │  run, capture output,  │  │
│  │  quirk isolated)│ │                       │ │  timeout/cancel)        │  │
│  └────────────────┘ └─────────────────────┘ └───────────────────────┘    │
├───────────────────────────────────────────────────────────────────────────┤
│  OS / EXTERNAL SURFACE                                                     │
│  Windows Registry · Service Control Manager · powershell.exe · Defender    │
│  winget · explorer.exe token (real-user HKCU) · GitHub (PostInstall assets)│
└───────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|-------------------------|
| Views (`*Page.xaml` + thin `.xaml.cs`) | Layout, data-binding, control instantiation. **No system calls, no business logic, no UI-control-building in C#.** | XAML `ItemsControl`/`ItemsRepeater` bound to `ObservableCollection<TweakItem>`; code-behind limited to `InitializeComponent()` and framework wiring (`SetFrame`, etc.) |
| ViewModels (one per page, `ObservableObject`) | Expose bindable state, translate user intent into service calls, marshal results back to the UI thread, surface errors via `IDialogService`/`IInfoBarService`. | CommunityToolkit.Mvvm `[ObservableProperty]` + `[RelayCommand]` generating `IAsyncRelayCommand` with `IsRunning` bound to a `ProgressRing`/`ProgressBar` |
| `ITweakCatalog` (replaces `ITweakService`) | Resolves a tweak `Key` to its handler and exposes `GetStateAsync`/`SetStateAsync` — same call shape ViewModels already expect, but backed by composition instead of a 32-case switch. | `Dictionary<string, ITweakHandler>` built from `IEnumerable<ITweakHandler>` injected by DI; iterates, never a switch |
| `ITweakHandler` (one per tweak, or a small family of reusable base classes) | Owns exactly one tweak's `GetState`/`SetState` logic, calling only the system-primitive interfaces below. | `sealed class DisableWifiTweakHandler(IRegistryService registry) : ITweakHandler` — independently unit-testable with a fake registry |
| `IDebloatService` | Runs the 28 PowerShell debloat actions; reports progress/output as it goes; is UI-agnostic (no `TextBox`/`ProgressBar` references). | Wraps `IScriptRunner`; exposes `IAsyncEnumerable<string>` or `IProgress<string>` for streamed log lines |
| `IRegistryService` | All registry reads/writes/deletes, including the "real logged-on-user HKCU while running elevated" P/Invoke trick from the predecessor. | Thin wrapper over `Microsoft.Win32.Registry*`; fully mockable in tests |
| `IWindowsServiceController` | Start/stop/configure Win32 services (e.g. Print Spooler, Bluetooth). | Wraps `System.ServiceProcess.ServiceController` |
| `IScriptRunner` / `IProcessRunner` | Extracts an embedded `.ps1` resource to a temp file, launches `powershell.exe -ExecutionPolicy Bypass -File`, captures stdout/stderr line-by-line, enforces timeout/cancellation, deletes the temp file. | `Process` + `ProcessStartInfo` (redirected streams) — **not** the `System.Management.Automation` SDK (see Pattern 2) |
| `IPostInstallService` | Mirrors the ~30MB PostInstall asset folder from GitHub to `C:\PostInstall\` on first use; no-op if already present. | HTTP download + file-existence check, independent of the tweak framework |
| Framework services (already exist) | Navigation (`INavigationService`/`FrameNavigationService`), dialogs (`IDialogService`), transient status (`IInfoBarService`), settings, theming, logging (`ILogger<T>` via `FileLoggerProvider`). | Reused as-is from `WinUI-3-MVVM-Framework`; do not reinvent |

## Recommended Project Structure

```
src/AkariToolbox.App/
├── Services/
│   ├── System/                      # UI-agnostic wrappers around Win32/OS primitives — the mockable seam
│   │   ├── IRegistryService.cs
│   │   ├── RegistryService.cs
│   │   ├── IWindowsServiceController.cs
│   │   ├── WindowsServiceController.cs
│   │   ├── IScriptRunner.cs
│   │   └── PowerShellScriptRunner.cs
│   ├── Tweaks/                      # one ITweakHandler class (or small base-class family) per tweak
│   │   ├── ITweakHandler.cs
│   │   ├── ITweakCatalog.cs
│   │   ├── TweakCatalog.cs          # replaces the old TweakService God-switch
│   │   └── Handlers/
│   │       ├── DisableWifiTweakHandler.cs
│   │       ├── DisableDefenderTweakHandler.cs   # two-phase workflow lives here
│   │       └── ... (32 total, grouped by page: OS / Gaming)
│   ├── Debloat/
│   │   ├── IDebloatService.cs
│   │   └── DebloatService.cs        # drives the 28 PowerShell actions via IScriptRunner
│   ├── PostInstall/
│   │   ├── IPostInstallService.cs
│   │   └── PostInstallService.cs
│   └── Misc/
│       ├── IContextMenuService.cs   # 12 add/remove entries — thin, reuses IRegistryService
│       └── ContextMenuService.cs
├── Models/
│   ├── TweakItem.cs                 # bindable row: Key, Title, Description, IsOn
│   ├── TweakDefinition.cs           # static catalog metadata (title/description/category per key)
│   ├── DebloatActionItem.cs
│   └── MiscItem.cs
├── ViewModels/                      # unchanged shape from predecessor, one per page
│   ├── HomeViewModel.cs
│   ├── AkariOSTweaksViewModel.cs
│   ├── GamingTweaksViewModel.cs
│   ├── DebloatViewModel.cs
│   ├── DownloadsViewModel.cs
│   └── MiscViewModel.cs
├── Views/                           # XAML-first; ItemsRepeater/ItemsControl over collections, no
│   │                                 # programmatic UI construction in code-behind
│   └── ...
├── Scripts/                         # embedded .ps1 resources, unchanged from predecessor
└── app.manifest                     # + requireAdministrator (see Pitfall: elevation + self-contained)

src/AkariToolbox.Tests/
└── Services/
    ├── Tweaks/                      # unit tests per handler, using fake IRegistryService etc.
    └── System/                      # optional thin integration tests against a scratch HKCU subkey
```

### Structure Rationale

- **`Services/System/` is the seam that makes everything else testable.** Every tweak handler, the debloat service, and the misc-page service depend only on these three interfaces — never on `Microsoft.Win32.Registry` or `Process.Start` directly. This is the single most important structural change from the predecessor.
- **`Services/Tweaks/Handlers/` replaces the 1117-line `TweakService.cs` switch statement** with one small class per tweak. Adding tweak #33 means adding one file and one DI registration line, not editing a shared file (avoids merge conflicts and makes each tweak's logic reviewable/testable in isolation).
- **ViewModels stay unchanged in shape** — `AkariOSTweaksViewModel` still just needs `GetStateAsync(key)`/`SetStateAsync(key, enabled)`, now served by `ITweakCatalog` instead of `ITweakService`. This means the port can keep the predecessor's proven ViewModel-facing contract while fixing the implementation underneath — low risk, high payoff.
- **`Views/` must not contain the programmatic UI-building code seen in `GamingTweaksPage.xaml.cs` (579 lines) and the delegate-based toggle wiring in `DebloatPage.xaml.cs` (180 lines).** Both are architecture debt explicitly called out in `PROJECT.md`; the WinUI 3 port should express these as XAML `ItemsControl`/`ItemsRepeater` bound to `ObservableCollection<TweakItem>` / `ObservableCollection<DebloatActionItem>`, matching how `AkariOSTweaksViewModel` already works.

## Architectural Patterns

### Pattern 1: Two-tier service abstraction (feature service → system primitive)

**What:** Don't let ViewModels or per-tweak handlers touch `Microsoft.Win32.Registry`, `ServiceController`, or `Process` directly. Introduce a thin primitive layer (`IRegistryService`, `IWindowsServiceController`, `IScriptRunner`) that every feature-facing service (`ITweakCatalog`, `IDebloatService`) composes.
**When to use:** Always, for any operation that mutates real system state. This is what makes `ITweakHandler` implementations unit-testable without a real registry or a real elevated process.
**Trade-offs:** One extra layer of indirection and a handful of small interfaces to maintain — worth it given this app's correctness bar ("every tweak must apply correctly, report accurate state, and be safely revertible").

```csharp
public interface IRegistryService
{
    object? GetValue(RegistryHive hive, string subKey, string valueName);
    void SetValue(RegistryHive hive, string subKey, string valueName, object value, RegistryValueKind kind);
    void DeleteValue(RegistryHive hive, string subKey, string valueName);
    // Isolates the "write to the real logged-on user's HKCU while running elevated"
    // P/Invoke trick the predecessor used — callers never see the Win32 details.
    RegistryKey OpenRealUserHive(string subKey);
}

public interface ITweakHandler
{
    string Key { get; }
    bool GetState();
    void SetState(bool enabled);
}

public sealed class DisableWifiTweakHandler(IRegistryService registry) : ITweakHandler
{
    public string Key => "wifi";
    public bool GetState() => registry.GetValue(RegistryHive.CurrentUser, @"Software\AkariTool", "DisableWiFi") != null;
    public void SetState(bool enabled) { /* registry.SetValue(...) */ }
}
```

### Pattern 2: Out-of-process PowerShell execution behind `IScriptRunner`, not `System.Management.Automation`

**What:** Keep the predecessor's proven approach — extract the embedded `.ps1` resource to a temp file and launch it via `Process.Start("powershell.exe", "-ExecutionPolicy Bypass -File ...")` with redirected stdout/stderr — but wrap it behind `IScriptRunner` so the mechanism is swappable and the callers never see `Process`/`ProcessStartInfo`.
**When to use:** For all 28 debloat scripts and any future PowerShell-backed tweak. These are standalone, infrequent, user-triggered scripts (not called in a hot loop), so process-start overhead (~100-200ms) is irrelevant.
**Trade-offs:** `System.Management.Automation` (the in-process PowerShell SDK) avoids process-spawn overhead and can reuse a runspace across calls, which matters for high-frequency automation scenarios — it does not matter here. Hosting it in-process would add a heavy dependency, complicate the self-contained/unpackaged deployment (assembly probing for the PowerShell SDK), and buys nothing for one-shot scripts. **Stay with the process-based approach**, matching the predecessor and keeping the deployment simple.

```csharp
public interface IScriptRunner
{
    Task<ScriptResult> RunEmbeddedScriptAsync(string scriptName, IProgress<string>? output = null,
        TimeSpan? timeout = null, CancellationToken ct = default);
}
// Implementation extracts the embedded resource, runs powershell.exe with redirected
// streams, and reports each output line through IProgress<string> — never a direct
// reference to a TextBox/ProgressBar (see Anti-Pattern 1).
```

### Pattern 3: Async commands with `IsRunning`/cancellation, UI-thread marshaling only at the edges

**What:** Every tweak toggle and every debloat action button is a CommunityToolkit.Mvvm `[RelayCommand]`-generated `IAsyncRelayCommand`. Long-running work (PowerShell, registry writes that shell out to `bcdedit`/`powercfg`) runs on a background thread implicitly (the command body is `async Task`, and `IScriptRunner`/`IRegistryService` calls are themselves async or wrapped in `Task.Run`); the command's `IsRunning` property drives a progress indicator without any manual dispatcher bookkeeping.
**When to use:** Any command that can take more than ~50ms — i.e. every tweak apply, every debloat script, the PostInstall mirror.
**Trade-offs:** Requires accepting a `CancellationToken` parameter on the command method to get cancellation "for free" from the toolkit's source generator; skipping this means `Cancel()` on the generated command has nothing to signal.

```csharp
public partial class DebloatViewModel : ViewModelBase
{
    private readonly IDebloatService _debloat;
    public ObservableCollection<DebloatActionItem> Actions { get; } = new();

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task RunActionAsync(DebloatActionItem item, CancellationToken ct)
    {
        var progress = new Progress<string>(line => LogLines.Add(line)); // ObservableCollection, UI-thread safe via source-gen marshaling
        var result = await _debloat.RunAsync(item.Key, progress, ct);
        item.LastResult = result; // triggers [ObservableProperty] change notification
    }
}
```

### Pattern 4: Manifest-driven tweak metadata (prepares for the v2 "Ultimate" collection)

**What:** Keep tweak *titles/descriptions/grouping* as declarative metadata (a static list or embedded JSON) separate from the *handler logic* (`ITweakHandler` classes). `AkariOSTweaksViewModel` already does this with its `defs` tuple array — formalize it into a `TweakDefinition` model consumed by the ViewModel, decoupled from how `ITweakCatalog` resolves handlers.
**When to use:** From v1, even though the current 32+28+12 items don't strictly need it — this is cheap now and expensive to retrofit once the v2 "Ultimate" collection (~110 more scripts across 8 categories) lands.
**Trade-offs:** Slightly more indirection for v1's modest item count; pays for itself the moment the tweak count roughly triples in v2, and enables category-based filtering/search in the UI without touching handler code.

## Data Flow

### Request Flow (tweak toggle)

```
[User flips ToggleSwitch, bound to TweakItem.IsOn]
    ↓ (x:Bind two-way, or PropertyChanged on TweakItem)
[ViewModel: IAsyncRelayCommand / property-changed handler]
    ↓ await, off UI thread implicitly (async Task body)
[ITweakCatalog.SetStateAsync(key, enabled)]
    ↓ dictionary lookup, no switch
[ITweakHandler.SetState(enabled)]
    ↓
[IRegistryService / IWindowsServiceController / IScriptRunner]
    ↓
[Windows Registry / SCM / powershell.exe]
    ↓ result / exception
[ITweakHandler → ITweakCatalog → ViewModel]
    ↓ on failure: IDialogService.ShowErrorAsync or IInfoBarService.ShowError
    ↓ on success: TweakItem.IsOn reconciled with actual GetState(), IInfoBarService.ShowSuccess
[UI updates via data binding — no manual DispatcherQueue calls needed if the
 ViewModel property setter uses SetProperty/OnPropertyChanged from the async
 continuation, since CommunityToolkit.Mvvm + WinUI marshal via the captured
 SynchronizationContext by default]
```

### Request Flow (debloat script)

```
[User taps "Run" on a DebloatActionItem]
    ↓
[DebloatViewModel.RunActionCommand (IAsyncRelayCommand, IsRunning=true → shows ProgressRing)]
    ↓
[IDebloatService.RunAsync(key, IProgress<string>, CancellationToken)]
    ↓
[IScriptRunner.RunEmbeddedScriptAsync: extract .ps1 from embedded resource → temp file
 → Process.Start powershell.exe -ExecutionPolicy Bypass -File → redirected stdout/stderr]
    ↓ each output line
[IProgress<string>.Report(line) — framework/CommunityToolkit.Mvvm marshals back to
 the UI thread automatically because IProgress<T> captures the calling SynchronizationContext]
    ↓
[ViewModel's ObservableCollection<string> LogLines updated → UI list auto-updates]
    ↓ on completion
[temp script deleted; DebloatActionItem.LastResult set; IsRunning=false → ProgressRing hides]
```

### Key Data Flows

1. **State reconciliation on page load:** every `*ViewModel` constructor (or `OnNavigatedTo`) calls `ITweakCatalog.GetStateAsync(key)` for each item to set the initial `TweakItem.IsOn` — this must read live system state, not a cached value, so the UI never lies about what's actually applied (matches predecessor's `HasState`/registry-persisted approach).
2. **Two-phase Disable Defender workflow:** modeled as a single `ITweakHandler` whose `SetState(true)` internally sequences multiple registry/service steps (Tamper Protection check → registry policy → service disable) and throws/returns a structured result if a precondition (e.g. Tamper Protection still on) blocks the second phase — the ViewModel surfaces that as a specific `IDialogService` prompt, not a generic error.
3. **Streamed script output:** `IScriptRunner` never buffers a script's full output before returning — it reports line-by-line via `IProgress<string>` so the UI shows progress in real time, matching the predecessor's `ToolService.Log` behavior but without the direct `TextBox` coupling.
4. **PostInstall self-heal:** independent of the tweak framework entirely — `IPostInstallService` is invoked once (e.g. from `DownloadsViewModel.OnNavigatedTo` or app startup) and only touches the filesystem + GitHub, never registry/services.

## Scaling Considerations

Reframed for this domain: the relevant "scale" axis is **tweak/action count**, not concurrent users (this is a single-user local desktop tool).

| Scale | Architecture Adjustments |
|-------|---------------------------|
| v1: 32 registry tweaks + 28 PowerShell actions + 12 misc entries (~72 items) | One `ITweakHandler` class per item, hand-registered in DI (`AddSingleton<ITweakHandler, DisableWifiTweakHandler>()` × N) is perfectly manageable. Static `TweakDefinition` metadata list per page. |
| v2: + ~110 "Ultimate" scripts across 8 categories (~180+ items) | Hand-registering every handler in DI becomes noisy. Move to **manifest-driven discovery**: a JSON/embedded manifest listing key, title, category, script name, and (where applicable) a revert script, loaded at startup and used to construct generic `PowerShellTweakHandler`/`RegistryTweakHandler` instances reflectively — only tweaks with genuinely unique logic (e.g. Disable Defender's two-phase flow) need a bespoke `ITweakHandler` class. This is why Pattern 4 (manifest-driven metadata) is worth adopting in v1 even though it's not strictly required yet. |
| Beyond: category-heavy UI (search/filter across 180+ tweaks) | `ITweakCatalog` should expose `GetByCategory`/`Search` rather than the ViewModel iterating a flat collection; consider lazy-loading tweak state (only call `GetState()` for the visible category) if reading all states on startup becomes a perceptible delay. |

### Scaling Priorities

1. **First bottleneck (v1→v2 transition):** the God-switch pattern the predecessor used (`TweakService.SetState`, one `switch` with 32+ cases) — already addressed by decomposing into `ITweakHandler` classes in v1, which is a prerequisite for the manifest-driven approach v2 will want.
2. **Second bottleneck (v2, if reached):** DI registration boilerplate for ~180 handlers — addressed by generic manifest-driven handlers plus bespoke classes only where behavior genuinely diverges (two-phase workflows, hardware-detection-gated tweaks).

## Anti-Patterns

### Anti-Pattern 1: UI controls injected into a service constructor

**What people do:** The predecessor's `ToolService` takes a `TextBox`, `ProgressBar`, and `TextBlock` directly in its constructor (`ToolService(TextBox log, ProgressBar progress, TextBlock progressStatus)`) and calls `_log.Dispatcher.Invoke(...)` to write to it.
**Why it's wrong:** Makes the service impossible to unit test, impossible to reuse across pages without re-wiring three UI references, and couples system-mutation logic to a specific XAML tree. It's also WPF-`Dispatcher`-specific and won't compile against WinUI 3's `DispatcherQueue` without rework anyway — a forcing function to fix it properly during the port.
**Instead:** Services report progress/log lines through `IProgress<string>` or events; the ViewModel (which does have a legitimate UI-thread affinity) subscribes and updates an `ObservableCollection<string>` or the framework's `IInfoBarService`. The service itself never references a WinUI control type.

### Anti-Pattern 2: Business logic and UI construction in code-behind

**What people do:** `GamingTweaksPage.xaml.cs` (579 lines) and `DebloatPage.xaml.cs` (180 lines) build UI elements programmatically in C# (`RootPanel.Children.Add(new TextBlock {...})`) and wire registry/tweak calls directly from the page, bypassing the ViewModel that already exists for that page (`GamingTweaksViewModel`).
**Why it's wrong:** Defeats MVVM entirely — untestable, unthemeable with native WinUI 3 Fluent 2 controls (which this project explicitly wants), and duplicates logic that should live once in `ITweakHandler`/`ITweakCatalog`. `PROJECT.md` calls this out explicitly as debt the port must fix, not transliterate.
**Instead:** Declare the UI in XAML using `ItemsRepeater`/`ItemsControl` with `DataTemplate`s bound to `ObservableCollection<TweakItem>` (exactly the pattern `AkariOSTweaksViewModel` + its page already use correctly) or `ObservableCollection<DebloatActionItem>`. Code-behind is limited to `InitializeComponent()` and framework navigation hooks.

### Anti-Pattern 3: One God-service with a giant switch keyed by string

**What people do:** `TweakService.SetState(string key, bool enabled)` is a single 32-case `switch` dispatching to 32 private methods in one 1117-line file; `StateKeyFor` is a second parallel `switch` mapping ViewModel keys to registry value names.
**Why it's wrong:** Every new tweak requires editing a shared file (merge-conflict magnet), no tweak can be unit-tested without instantiating the whole class and hitting the real registry, and nothing enforces that a key added to one switch is also added to the other (the predecessor already shows key/name drift risk — e.g. `"vr"` → `"EnableVR"` naming is easy to typo silently).
**Instead:** One `ITweakHandler` class (or generic parameterized handler, per Pattern 4) per tweak, each owning both its state-key mapping and its apply/revert logic together, resolved through `ITweakCatalog`'s dictionary lookup — impossible for the two switches to drift because there's only one place per tweak.

### Anti-Pattern 4: Scattering raw `Microsoft.Win32.Registry` / P/Invoke calls across services and pages

**What people do:** Both `TweakService.cs` and `GamingTweaksPage.xaml.cs` independently open `Registry.CurrentUser.CreateSubKey(@"Software\AkariTool", ...)`, and the "read the real logged-on user's HKCU while running elevated" `OpenProcessToken`/P/Invoke trick lives inline in `TweakService`.
**Why it's wrong:** Duplicated low-level code across layers, no single seam to mock for tests, and a genuinely tricky piece of platform code (impersonating the interactive user's token from an elevated process) that deserves to be written and tested exactly once.
**Instead:** All registry access — including the real-user-HKCU trick — goes through `IRegistryService`. Nothing outside `Services/System/RegistryService.cs` calls `Microsoft.Win32.Registry*` or the token P/Invoke directly.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|----------------------|-------|
| GitHub (`github.com/isleap9/PostInstall`) | HTTP download of a ~30MB asset folder, mirrored to `C:\PostInstall\` | `IPostInstallService`; must be resilient to no-network (no-op gracefully) and idempotent (skip if already present, matching predecessor) |
| `winget` | Shelled out via `powershell.exe -NoProfile -Command "..."`, same mechanism as script execution | Route through the same `IScriptRunner`/`IProcessRunner` abstraction rather than a separate code path |
| Windows Defender | Registry policy + service control, sequenced as a two-phase workflow | Needs its own `ITweakHandler` with explicit precondition checks (Tamper Protection), not a generic registry toggle |
| Third-party tool grid (NVIDIA/AMD utilities, Gaming Tweaks page) | `Process.Start` with `UseShellExecute = true` to launch external installers/URLs, or a download-then-launch flow | Keep as `UrlAction`/launch actions distinct from tweak state — these don't have "state" to report the way a registry toggle does |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|----------------|-------|
| ViewModel ↔ `ITweakCatalog`/`IDebloatService`/`IPostInstallService` | Direct async method calls (constructor-injected interfaces) | Keep ViewModels thin: no registry/process knowledge, only calls into feature services and translation of results into bindable state |
| `ITweakCatalog`/`IDebloatService` ↔ `IRegistryService`/`IWindowsServiceController`/`IScriptRunner` | Direct calls | This is the boundary unit tests target — fake the primitives, exercise real handler/catalog logic |
| ViewModel ↔ Framework services (`IDialogService`, `IInfoBarService`, `INavigationService`) | Direct calls, reused as-is | No changes needed; these are already UI-agnostic-enough abstractions provided by `WinUI-3-MVVM-Framework` |
| Long-running service work ↔ UI thread | `IProgress<string>` (captures `SynchronizationContext` automatically) for streaming output; `IAsyncRelayCommand.IsRunning` for busy state; framework's `DispatcherQueueExtensions.RunOnUIThreadAsync` only needed for code that isn't already running from a UI-thread-originated `async` continuation (e.g. callbacks from a raw background `Thread` or `Process` event handler) | Prefer `IProgress<T>`/`async`/`await` over manual `DispatcherQueue.TryEnqueue` wherever possible — less error-prone |

## Sources

- Direct reading of `AkariOS-Companion` predecessor source (`ITweakService.cs`, `TweakService.cs`, `ToolService.cs`, `RunActions.cs`, `TweakItem.cs`, `AkariOSTweaksViewModel.cs`, `GamingTweaksPage.xaml.cs`, `app.manifest`) — HIGH confidence, primary source, this is the actual code being ported.
- Direct reading of `WinUI-3-MVVM-Framework` template (`App.xaml.cs`, `ServiceCollectionExtensions.cs`, `ViewModelBase.cs`, `FrameNavigationService.cs`, `IInfoBarService.cs`, `DispatcherQueueExtensions.cs`, `app.manifest`) — HIGH confidence, primary source, this is the actual foundation being built on.
- [AsyncRelayCommand — Community Toolkits for .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand) and [RelayCommand attribute docs](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand) — official Microsoft docs confirming `IsRunning`/`ExecutionTask`/cancellation-via-`CancellationToken`-parameter pattern used in Pattern 3.
- [microsoft/WindowsAppSDK Discussion #3038](https://github.com/microsoft/WindowsAppSDK/discussions/3038) and [Issue #3376](https://github.com/microsoft/WindowsAppSDK/issues/3376) — confirms self-contained WinUI 3 apps historically had a manifest-merge bug (`c1010001`) preventing `requireAdministrator` from working correctly with `WindowsAppSDKSelfContained=true`; reported fixed targeting the 1.3 milestone. The framework here targets Windows App SDK 2.3.1 (well past 1.3), so this should not reproduce — but MEDIUM confidence only (not independently verified in this environment); **flag as an early build-order item to smoke-test** (see Roadmap Implications below / PITFALLS.md).
- General web search on `System.Management.Automation` (in-process PowerShell SDK) vs `Process.Start powershell.exe` — informs Pattern 2's recommendation to stay process-based; LOW/MEDIUM confidence (blog/reference-level sources, not a single authoritative Microsoft comparison), but the reasoning (no hot-loop, avoid SDK weight in a self-contained deploy) is sound independent of the sources.

---
*Architecture research for: WinUI 3 MVVM system-tweak/debloat desktop utility (Akari Toolbox)*
*Researched: 2026-08-31*
