# Phase 1: Foundation & Akari OS Tweaks - Pattern Map

**Mapped:** 2026-08-31
**Files analyzed:** 24
**Analogs found:** 22 / 24

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `src/AkariToolbox.App/*` (renamed from `AppTemplate.App`) | config/project | file-I/O | `WinUI-3-MVVM-Framework/src/AppTemplate.App/` (whole tree) | exact (copy+rename) |
| `app.manifest` (elevation block) | config | request-response | `AkariOS-Companion/app.manifest` (source of truth for trustInfo block) + `AppTemplate.App/app.manifest` (target to modify) | exact |
| `App.xaml.cs` (AppName/SettingsFolder/DI registration) | provider | event-driven (host bootstrap) | `AppTemplate.App/App.xaml.cs` | exact |
| `MainWindow.xaml` / `MainWindow.xaml.cs` (NavItems + log console dock) | component | request-response | `AppTemplate.App/MainWindow.xaml(.cs)` | exact |
| `NavigationItem.cs` (+ `IsEnabled`) | model | transform | `AppTemplate.App/NavigationItem.cs` | exact |
| `Views/HomePage.xaml` + `HomeViewModel.cs` (5 cards, 1 disabled-aware) | component/hook | request-response | `AkariOS-Companion/ViewModels/HomeViewModel.cs` + `Views/HomePage.xaml` | exact (content), needs disabled-state addition |
| `Models/TweakItem.cs` | model | CRUD | `AkariOS-Companion/Models/TweakItem.cs` | exact (copy verbatim) |
| `ViewModels/AkariOSTweaksViewModel.cs` | hook/viewmodel | CRUD | `AkariOS-Companion/ViewModels/AkariOSTweaksViewModel.cs` | exact (defs copied verbatim; GetState call becomes async) |
| `Views/AkariOSTweaksPage.xaml` | component | request-response | (no direct WinUI analog — predecessor is WPF `ItemsControl`) | role-match via `HomePage.xaml`/framework `ItemsRepeater` conventions |
| `Services/ITweakService.cs` / `ITweakCatalog.cs` | service | CRUD | `AkariOS-Companion/Services/ITweakService.cs` | exact (keep shape, D-03 semantics change) |
| `Services/ITweakHandler.cs` (31 registry/service handlers + 1 Defender) | service | CRUD | `AkariOS-Companion/Services/TweakService.cs` (per-tweak `Set*` methods, one handler per method) | exact (1 analog per handler; each handler is a small port of one `Set*` method) |
| `Services/DefenderTweakHandler.cs` | service | event-driven | `AkariOS-Companion/Services/TweakService.cs:828-1038` (`SetDefenderAsync` + helpers) | exact (byte-for-byte port per D-01) |
| `Services/IRegistryService.cs` (+ `OpenRealUserHive`) | service | CRUD | `AkariOS-Companion/Services/TweakService.cs:141-155` (`CreateRealHkcuSubKey`, direct `RegistryKey` calls throughout) | role-match (no interface existed in predecessor — raw static calls); primitive to extract new |
| `Services/IWindowsServiceController.cs` | service | CRUD | `AkariOS-Companion/Services/TweakService.cs` (service-loop tweaks: `bluetooth` L245-259, `spooler`, `clipboard`, `cdrom`, `vr`) | role-match (extract new interface from repeated inline `ServiceController`-equivalent code) |
| `Services/IScriptRunner.cs` | service | event-driven | `AkariOS-Companion/Services/ToolService.cs:88-173` (`RunScript`/`RunProcess`) | exact (process-spawn pattern to preserve, D-05 log wiring) |
| `Services/IPostInstallService.cs` (minimal subset) | service | file-I/O | `AkariOS-Companion/Services/PostInstallService.cs:196-234` (`Ensure*` methods) | exact (port 3 `Ensure*` methods + manifest only, no UI) |
| `Services/ILogConsoleService.cs` | service | pub-sub | `AppTemplate.Framework/Services/IInfoBarService.cs` (ObservableObject-backed service pattern) + `AkariOS-Companion/Services/ToolService.cs:33-38` (`Log` behavior to preserve) | role-match (structural analog is InfoBarService; behavioral analog is ToolService.Log) |
| `Services/IFilePickerService.cs` (elevation-safe swap) | service | file-I/O | `AppTemplate.Framework/Services/IFilePickerService.cs` | exact (same interface shape, swap implementation to `Microsoft.Windows.Storage.Pickers`) |
| `ServiceCollectionExtensions.cs` (DI registrations for new services) | config | CRUD | `AppTemplate.Framework/ServiceCollectionExtensions.cs` | exact |
| `Threading/DispatcherQueueExtensions.cs` | utility | transform | `AppTemplate.Framework/Threading/DispatcherQueueExtensions.cs` | exact (reuse unmodified) |

## Pattern Assignments

### `Services/ITweakService.cs` / `ITweakCatalog.cs` (service, CRUD)

**Analog:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ITweakService.cs`

**Full interface to keep the shape of** (lines 1-17):
```csharp
using AkariOSCompanion.Models;

namespace AkariOSCompanion.Services;

public interface ITweakService
{
    /// <summary>Read the live system state for a tweak key (drives the initial toggle position).</summary>
    bool GetState(string key);

    /// <summary>Apply a tweak. Should be idempotent and safe to call repeatedly.</summary>
    void SetState(string key, bool enabled);
}
```
Per D-03/RESEARCH's "Don't Hand-Roll" table, the new `ITweakCatalog` orchestrates capture-then-write (read old value via `ITweakHandler.GetState()`, store per D-04, then call `SetState`) so no individual handler re-implements the ordering. Keep `GetState(string key)`/`SetState(string key, bool enabled)` signatures (async-ify per handler if a handler needs it, e.g. Defender).

---

### `Models/TweakItem.cs` (model, CRUD)

**Analog:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Models/TweakItem.cs` — copy verbatim (CommunityToolkit.Mvvm `ObservableObject`, `[ObservableProperty] IsOn`, `Key`/`Title`/`Description` init-only):
```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace AkariOSCompanion.Models;

public partial class TweakItem : ObservableObject
{
    public string Key { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";

    [ObservableProperty]
    private bool _isOn;
}
```
Only the namespace changes (`AkariToolbox.Models` or equivalent).

---

### `ViewModels/AkariOSTweaksViewModel.cs` (hook/viewmodel, CRUD)

**Analog:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/AkariOSTweaksViewModel.cs`

**Definitions array — copy the exact 32-tuple list verbatim** (lines 18-52) — do not re-derive these; every `Key`/`Title`/`Description` triple must match byte-for-byte per CONTEXT.md's "carry over verbatim" instruction.

**Core wiring pattern** (lines 54-64):
```csharp
foreach (var (key, title, desc) in defs)
{
    var item = new TweakItem { Key = key, Title = title, Description = desc, IsOn = _tweaks.GetState(key) };
    item.PropertyChanged += (_, e) =>
    {
        if (e.PropertyName == nameof(TweakItem.IsOn))
            _tweaks.SetState(item.Key, item.IsOn);
    };
    Tweaks.Add(item);
}
```
**Change required:** `_tweaks.GetState(key)` at construction time reads live registry/service state on the UI thread synchronously in the predecessor — per RESEARCH Pitfall 3, the ported version should make these reads async/parallel to avoid blocking page load, then marshal each `IsOn` write back via `DispatcherQueueExtensions.RunOnUIThreadAsync`. Keep the `PropertyChanged`-driven write-back pattern unchanged (it already only fires on `IsOn` changes, matching a `[RelayCommand]`-free toggle-binding idiom already proven in this codebase).

---

### `Services/ITweakHandler.cs` per-tweak handlers (service, CRUD) — 31 registry/service handlers

**Analog:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/TweakService.cs` — one handler class per `Set*` method (full file read this session, 1118 lines; the 32 OS-tweak `Set*` methods live in the region flagged `// AKARI OS TWEAKS`, keys enumerated in RESEARCH.md's Live-state-reader table).

**Representative simple-registry pattern** (`SetWifi`, lines 164-183, write side to port unmodified except stripping `HasState`/`SaveState`):
```csharp
// Write side (verbatim mutation logic to port)
registry.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\WlanSvc", "Start", start, RegistryValueKind.DWord);
registry.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\vwififlt", "Start", disable ? 4 : 1, RegistryValueKind.DWord);
registry.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\netprofm", "Start", disable ? 4 : 3, RegistryValueKind.DWord);
registry.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NlaSvc", "Start", disable ? 4 : 2, RegistryValueKind.DWord);
```
**New read side to add** (no analog — this is the D-03 new-design work; RESEARCH's Code Example 2 gives the target shape):
```csharp
public bool GetState() =>
    registry.GetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\WlanSvc", "Start") is int start && start == 4;
```

**Anti-pattern to strip on every port** (RESEARCH Pitfall 4) — grep the source for and remove all of:
- `HasState(...)` / `SaveState(...)` / `ClearState(...)` calls
- Any read/write of `HKCU\Software\AkariTool`
Replace the `if (HasState(...)) return;` idempotency guard with `if (GetState() == enabled) return;`.

**Real-user-HKCU trick** (needed by `startmenu`/`transparency`; port into `IRegistryService.OpenRealUserHive(string)`) — `TweakService.cs:141-155`:
```csharp
[System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

private static RegistryKey CreateRealHkcuSubKey(string subKey)
{
    var explorer = Process.GetProcessesByName("explorer").FirstOrDefault()
        ?? throw new InvalidOperationException("explorer.exe not found.");
    if (!OpenProcessToken(explorer.Handle, 8, out var token))
        throw new InvalidOperationException("Could not open explorer process token.");
    using var identity = new System.Security.Principal.WindowsIdentity(token);
    var sid = identity.User!.Value;
    var hku = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
    return hku.CreateSubKey($@"{sid}\{subKey}", writable: true)!;
}
```
Preserve the hard-failure behavior (throw on missing `explorer.exe`) per RESEARCH Open Question 3 — parity, not a graceful fallback, unless the user says otherwise.

**Non-registry exceptions (no pure-registry read side):**
- `dep` (`TweakService.cs:215-228`) — pure `bcdedit`, zero registry footprint; needs `IScriptRunner`-based read (parse `bcdedit /enum {current}`) or documented write-only exception — flag for planner per RESEARCH Open Question 1.
- `hyperv`, `vr` (`TweakService.cs:561-593`, `497-519`) — mixed `bcdedit`/`DISM` + registry; read only the registry portion.

---

### `Services/DefenderTweakHandler.cs` (service, event-driven) — D-01 exempt, port as-is

**Analog:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/TweakService.cs:828-1038` (`SetDefenderAsync` and its full call graph — `IsDefenderTamperProtectionOn` L976-986, `DefenderRunElevatedPsFileAsync` L994-1005, `DefenderScheduleCleanup` L899-974, `DefenderRunAsTrustedInstallerAsync` L1019-1038).

**Do not refactor into `ITweakHandler`.** Port the entire chain byte-for-byte; only the thin ViewModel/handler-wrapper that calls it is new. GetState for this tweak stays exempt from D-03 — port the predecessor's `HasState("DisableDefender")`-style check as literally written (per RESEARCH's Live-state-reader table row for `defender`).

**Cross-service dependency (Pitfall 5):** also requires a minimal port of:

**Analog:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/PostInstallService.cs:196-234` — port only `AllFiles` manifest, `LocalRoot`/`MinSudoPath`/`PowerRunPath`/`NoDefenderPath` constants, `MinSudoPresent`/`PowerRunPresent`/`NoDefenderPresent`/`IsFullyInstalled` properties, and the three `Ensure*` methods (`EnsureDefenderFilesAsync`, `EnsureMinSudoAsync`, `EnsurePostInstallAsync`). No UI, no Downloads-page scope (that's Phase 4).

---

### `Services/IScriptRunner.cs` (service, event-driven)

**Analog:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ToolService.cs:88-173` (`RunScript` embedded-resource extraction + `RunProcess` process-spawn-with-output-capture).

**Core pattern to port** (script extraction + execution, lines 88-109):
```csharp
public async Task RunScript(string scriptName)
{
    var resourceName = FindEmbeddedScriptResource(scriptName);
    if (resourceName is null) { Log($"[ERROR] Embedded script not found: {scriptName}"); return; }

    var temp = Path.Combine(Path.GetTempPath(), $"AkariOS-{Guid.NewGuid():N}-{scriptName}");
    try
    {
        await ExtractEmbeddedScript(resourceName, temp);
        Log($"[RUN] {scriptName}");
        await RunProcess("powershell.exe", $"-ExecutionPolicy Bypass -File \"{temp}\"", timeout: null);
        Log("[DONE]");
    }
    finally { TryDeleteTempScript(temp); }
}
```
**Process-runner with output capture and error handling** (lines 135-173):
```csharp
public async Task<int> RunProcess(string fileName, string arguments, int? timeout)
{
    StartProgress(fileName);
    try
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName, Arguments = arguments, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log(e.Data); };
        process.ErrorDataReceived  += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log($"[ERROR] {e.Data}"); };
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        var waitTask = process.WaitForExitAsync();
        if (timeout is null) await waitTask;
        else if (await Task.WhenAny(waitTask, Task.Delay(timeout.Value)) != waitTask)
        {
            process.Kill(entireProcessTree: true);
            Log("[TIMEOUT]");
            return -1;
        }
        return process.ExitCode;
    }
    catch (Exception ex) { Log($"[EXCEPTION] {ex.Message}"); return -1; }
    finally { StopProgress(); }
}
```
Keep the `Process.Start(powershell.exe, -ExecutionPolicy Bypass -File ...)` pattern exactly (per CLAUDE.md's explicit "do not add Microsoft.PowerShell.SDK" constraint). Replace `Log(...)` calls with `ILogConsoleService.Log(...)` injected via constructor (D-08) instead of the anti-pattern constructor-injected `TextBox`/`ProgressBar` (see next section).

---

### `Services/ILogConsoleService.cs` (service, pub-sub)

**Structural analog:** `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/Services/IInfoBarService.cs` — `ObservableObject`-backed service pattern, registered as a DI singleton, bound directly from XAML via `x:Bind`, exactly the shape D-08 asks for instead of raw-control injection:
```csharp
public interface IInfoBarService
{
    bool IsOpen { get; set; }
    string Title { get; set; }
    string Message { get; set; }
    InfoBarSeverity Severity { get; set; }
    bool IsClosable { get; set; }
    void Show(string title, string message, InfoBarSeverity severity = InfoBarSeverity.Informational);
    void Hide();
}

public partial class InfoBarService : ObservableObject, IInfoBarService
{
    [ObservableProperty] public partial bool IsOpen { get; set; }
    [ObservableProperty] public partial string Title { get; set; } = string.Empty;
    // ... same [ObservableProperty] shape for Message/Severity/IsClosable
}
```

**Behavioral analog (what NOT to structurally copy) — the anti-pattern to fix:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ToolService.cs:15-38`:
```csharp
// ANTI-PATTERN — do not replicate this constructor shape:
public class ToolService
{
    private readonly TextBox _log;
    private readonly ProgressBar _progress;
    private readonly TextBlock _progressStatus;

    public ToolService(TextBox log, ProgressBar progress, TextBlock progressStatus) { ... }

    public void Log(string message) =>
        _log.Dispatcher.Invoke(() =>
        {
            _log.AppendText(message + Environment.NewLine);
            _log.ScrollToEnd();
        });
}
```
Preserve the *behavior* (append line, auto-scroll, in-memory only per D-07) but route through `ObservableCollection<string> Lines` + `DispatcherQueueExtensions.RunOnUIThreadAsync` instead of a WPF `Dispatcher.Invoke` on an injected control (RESEARCH Code Example 3 gives the exact target shape):
```csharp
public interface ILogConsoleService
{
    ObservableCollection<string> Lines { get; }
    void Log(string message);
}

public sealed class LogConsoleService(DispatcherQueue dispatcher) : ILogConsoleService
{
    public ObservableCollection<string> Lines { get; } = new();

    public void Log(string message) =>
        _ = dispatcher.RunOnUIThreadAsync(() => Lines.Add(message));
}
```

---

### `Services/IFilePickerService.cs` (service, file-I/O) — elevation-safe swap

**Analog (interface shape to keep):** `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/Services/IFilePickerService.cs:11-28`:
```csharp
public interface IFilePickerService
{
    Task<IReadOnlyList<StorageFile>?> PickOpenFilesAsync(IReadOnlyList<string> fileTypeFilters, string? suggestedStartLocation = null);
    Task<StorageFile?> PickOpenFileAsync(IReadOnlyList<string> fileTypeFilters, string? suggestedStartLocation = null);
    Task<StorageFile?> PickSaveFileAsync(string suggestedFileName, IReadOnlyList<string> fileTypeFilters, string? suggestedStartLocation = null);
}
```
**Current (broken-under-elevation) implementation** uses `Windows.Storage.Pickers` + `InitializeWithWindow.Initialize(picker, _hwndProvider())` (`IFilePickerService.cs:1-119`, e.g. lines 80-99) — swap to `Microsoft.Windows.Storage.Pickers` per RESEARCH Code Example 1, which takes a `WindowId` constructor arg instead of `IntPtr` + `InitializeWithWindow`. Also add `PickSingleFolderAsync`/`PickMultipleFoldersAsync` to satisfy APP-04's "file/folder picker" wording (current interface has no folder methods at all).

---

### `Threading/DispatcherQueueExtensions.cs` (utility, transform)

**Analog:** `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/Threading/DispatcherQueueExtensions.cs` — reuse **unmodified**, no port needed. Full file (102 lines) already implements `RunOnUIThreadAsync` for `Action`, `Func<T>`, and `Func<Task>` overloads with `TaskCompletionSource`-based exception propagation. This is the single marshaling primitive `ILogConsoleService`, `AkariOSTweaksViewModel`'s async state-read completions, and the Defender workflow's log callbacks all route through (APP-05, Pitfall 3).

---

### `App.xaml.cs` / `MainWindow.xaml.cs` / `NavigationItem.cs` (provider/component/model)

**Analog:** `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/{App.xaml.cs, MainWindow.xaml.cs, MainWindow.xaml, NavigationItem.cs}` — copy/rename per the framework's own README checklist (quoted in RESEARCH.md).

**Rename touchpoints** (`App.xaml.cs:29-38`):
```csharp
public static string AppName => "App Template";
public static string SettingsFolder => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppTemplate");
```
→ `"Akari Toolbox"` / `"AkariToolbox"`.

**NavItems extension point** (`MainWindow.xaml.cs:69-79`):
```csharp
public IReadOnlyList<NavigationItem> NavItems { get; } =
[
    new("Home", "\uE80F", typeof(HomePage)),
];
```
→ extend to 5 entries (Home, Akari OS Tweaks, Gaming Tweaks [disabled], Debloat [disabled], Downloads [disabled], Misc [disabled]) per D-11 — see `NavigationItem.cs` pattern below.

**`NavigationItem` record to extend** (`NavigationItem.cs:4`):
```csharp
public sealed record NavigationItem(string Label, string Glyph, Type PageType);
```
→ add `bool IsEnabled = true` (RESEARCH Code Example 4):
```csharp
public sealed record NavigationItem(string Label, string Glyph, Type PageType, bool IsEnabled = true);
```
**`MainWindow.xaml` DataTemplate** (`MainWindow.xaml:49-57`) needs `IsEnabled="{x:Bind IsEnabled}"` added to the `NavigationViewItem`.

**Log console dock (D-05/D-06):** `MainWindow.xaml`'s existing `Grid`+`InfoBar` docked-at-bottom pattern (lines 59-75, `InfoBar` bound to `InfoBar.IsOpen`/`Message`/`Severity`) is the structural analog for adding a collapsible log console panel — same "docked `Grid.Row`, bound `IsOpen`-style visibility" idiom, extended with a user-collapsible toggle (e.g. `Expander` or a `ToggleButton` driving a `Visibility`/`Height` binding) rather than the InfoBar's auto-show/hide.

---

### `HomeViewModel.cs` / `HomePage.xaml` (hook/component, request-response)

**Analog:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/HomeViewModel.cs` — full file:
```csharp
public sealed class HomeCard
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Glyph { get; init; } = "";      // Segoe Fluent Icons glyph
    public Type Target { get; init; } = typeof(HomePage);
}

public partial class HomeViewModel : ObservableObject
{
    public IReadOnlyList<HomeCard> Cards { get; } = new[]
    {
        new HomeCard { Title = "Gaming Tweaks",   Description = "GPU, latency & service tuning for peak FPS",   Glyph = "\uE7FC", Target = typeof(GamingTweaksPage) },
        new HomeCard { Title = "Akari OS Tweaks", Description = "Toggle deep system modifications & services",  Glyph = "\uE713", Target = typeof(AkariOSTweaksPage) },
        new HomeCard { Title = "Downloads",       Description = "Playbooks, drivers & recommended utilities",   Glyph = "\uE896", Target = typeof(DownloadsPage) },
        new HomeCard { Title = "Misc",            Description = "Context-menu entries & extra tools",          Glyph = "\uE712", Target = typeof(MiscPage) },
    };

    [RelayCommand]
    private void Open(Type pageType) => AppNavigation.Navigate(pageType);
}
```
**Changes required:** add a 5th `HomeCard` for Debloat (D-10, pick a new glyph — e.g. `\uE74D` delete/broom-adjacent glyph, Claude's discretion per CONTEXT.md), add `bool IsEnabled` to `HomeCard` (mirrors the `NavigationItem.IsEnabled` addition — single source-of-truth convention per RESEARCH's NavigationView pattern note), and reroute `[RelayCommand] Open` through the framework's `INavigationService`/`FrameNavigationService` instead of the predecessor's static `AppNavigation.Navigate` helper (framework already DI-registers navigation — see `ServiceCollectionExtensions.cs`/`App.xaml.cs` `AddSingleton<INavigationService>` registration).

---

### `ServiceCollectionExtensions.cs` (config, CRUD) — DI registration pattern

**Analog:** `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/ServiceCollectionExtensions.cs:26-42`:
```csharp
public static IServiceCollection AddMvvmFramework(this IServiceCollection services)
{
    ArgumentNullException.ThrowIfNull(services);
    services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
    services.AddSingleton<ISettingsStorage, FileSettingsStorage>();
    services.AddSingleton<ISettingsService, SettingsService>();
    services.AddSingleton<ICultureService, CultureService>();
    services.AddSingleton<IThemeService, ThemeService>();
    services.AddSingleton<IInfoBarService, InfoBarService>();
    services.AddSingleton<IWindowService, WindowService>();
    services.AddSingleton<IDialogService, DialogService>();
    services.AddSingleton<IFilePickerService, FilePickerService>();
    return services;
}
```
Add a parallel block (either inside this method or a new `AddAkariToolboxServices` extension called from `App.xaml.cs`'s `BuildHost()`) registering: `ILogConsoleService`, `IRegistryService`, `IWindowsServiceController`, `IScriptRunner`, `IPostInstallService`, `ITweakCatalog`, and all `ITweakHandler` implementations (likely `AddSingleton<ITweakHandler, XyzTweakHandler>()` per handler, resolved as `IEnumerable<ITweakHandler>` by the catalog — standard `Microsoft.Extensions.DependencyInjection` multi-registration pattern, no analog needed beyond this file's existing `AddSingleton<T, TImpl>` idiom).

**`App.xaml.cs` `BuildHost()` registration site** (lines 95-129) — new view models and the `Func<WindowId>`/`Func<IntPtr>` provider swap (for the picker fix) go here, following the existing `AddTransient<HomeViewModel>()` / `AddSingleton(sp => new Func<IntPtr>(...))` idioms already present.

## Shared Patterns

### Cross-thread UI marshaling (APP-05)
**Source:** `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/Threading/DispatcherQueueExtensions.cs` (full file, reuse unmodified)
**Apply to:** `ILogConsoleService.Log`, `AkariOSTweaksViewModel`'s async state-read completions, Defender workflow log callbacks, any other background→UI state write.
```csharp
public static Task RunOnUIThreadAsync(this DispatcherQueue dispatcher, Action action)
{
    if (dispatcher.HasThreadAccess) { action(); return Task.CompletedTask; }
    var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    if (!dispatcher.TryEnqueue(() => { try { action(); tcs.SetResult(); } catch (Exception ex) { tcs.SetException(ex); } }))
        tcs.SetException(new InvalidOperationException("Failed to enqueue work on the dispatcher queue."));
    return tcs.Task;
}
```

### D-03 anti-pattern strip (registry-squatting-safe writes + no private state hive)
**Source:** RESEARCH.md Pitfall 4 + CLAUDE.md "Forbidden pattern" section
**Apply to:** every one of the 31 non-Defender `ITweakHandler` ports.
- Grep the ported code for `_store`, `HasState`, `SaveState`, `ClearState`, `Software\\AkariTool` — any hit outside a comment is non-compliant.
- Always `OpenSubKey`/`GetValue` with null-checks before writing; use `CreateSubKey` only when a tweak is explicitly meant to create the key (never raw `RegistryKey.SetValue` without existence checks).

### DI registration idiom
**Source:** `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/ServiceCollectionExtensions.cs`
**Apply to:** all new services (`ILogConsoleService`, `IRegistryService`, `IWindowsServiceController`, `IScriptRunner`, `IPostInstallService`, `ITweakCatalog`, `ITweakHandler` implementations) — `services.AddSingleton<IInterface, Implementation>()`, called from `App.xaml.cs`'s `BuildHost()`.

### Elevated-process picker construction
**Source:** RESEARCH.md Code Example 1 (`Microsoft.Windows.Storage.Pickers`, `WindowId`-based constructor) vs. current framework `Windows.Storage.Pickers` + `InitializeWithWindow.Initialize(picker, hwnd)` (`IFilePickerService.cs:76,97`)
**Apply to:** `IFilePickerService`'s concrete implementation only — interface consumers unaffected.

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `Views/AkariOSTweaksPage.xaml` | component | request-response | Predecessor's tweak list is WPF `ItemsControl`/`UniformGrid` (no WinUI 3 XAML analog exists yet in either source tree); build as `ItemsRepeater`/`GridView` following `HomePage.xaml`'s card-grid conventions and D-02's Fluent-2-only constraint. |
| `Services/IWindowsServiceController.cs` | service | CRUD | Predecessor has no service-abstraction interface — service starts/stops are inlined per-tweak in `TweakService.cs` (e.g. the 15-service loop at L245-259 for `bluetooth`). New interface wraps `System.ServiceProcess.ServiceController` (STACK.md-mandated package); no existing analog to extract from, only inline call sites to generalize. |

## Metadata

**Analog search scope:** `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/{Services,Models,ViewModels,Views}`, `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/{AppTemplate.App,AppTemplate.Framework}`
**Files scanned:** 16 source files read directly this session (predecessor: `TweakService.cs`, `ITweakService.cs`, `ToolService.cs`, `PostInstallService.cs` [partial, per RESEARCH], `TweakItem.cs`, `AkariOSTweaksViewModel.cs`, `HomeViewModel.cs`; framework: `IFilePickerService.cs`, `IInfoBarService.cs`, `DispatcherQueueExtensions.cs`, `ServiceCollectionExtensions.cs`, `App.xaml.cs`, `MainWindow.xaml.cs`, `MainWindow.xaml`, `NavigationItem.cs`) plus CONTEXT.md/RESEARCH.md's own extensive verified excerpts of `app.manifest`, `HomePage.xaml`/`AkariOSTweaksPage`-equivalent WPF views (not independently re-read where RESEARCH.md already quoted the relevant lines).
**Pattern extraction date:** 2026-08-31
