# Phase 2: Gaming Tweaks - Pattern Map

**Mapped:** 2026-09-01
**Files analyzed:** 20 (1 plumbing fix + 6 modified files + 13 new files, collapsed to logical groups below)
**Analogs found:** 20 / 20

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `src/AkariToolbox.App/Services/ITweakHandler.cs` (MODIFY — add `TweakCategory Category`) | interface/model | CRUD | itself (extend in place) | exact |
| `src/AkariToolbox.App/Services/TweakCategory.cs` (NEW enum) | model | — | none (new small enum) | no analog |
| `src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchA.cs` etc. (MODIFY — add `Category => TweakCategory.AkariOS` to all 32 existing handlers, 6 files) | controller/model | CRUD | itself | exact |
| `src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs` (MODIFY — scope assertions to `Category == AkariOS`, add Gaming-scoped variant) | test | request-response | itself | exact |
| `src/AkariToolbox.Framework/Services/IRegistryService.cs` (MODIFY — add `GetSubKeyNames`) | service (interface) | CRUD | itself | exact |
| `src/AkariToolbox.Framework/Services/RegistryService.cs` (MODIFY — implement `GetSubKeyNames`) | service | CRUD | itself | exact |
| `src/AkariToolbox.Framework/Services/IScriptRunner.cs` / `ScriptRunner.cs` (MODIFY — add `RunEmbeddedScriptAsync`) | service | file-I/O + event-driven (process spawn) | `DefenderTweakHandler.ExtractEmbeddedAsync` (`src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs:317-333`) | exact |
| `src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs` (NEW — Hdcp, P0State, MsiMode, AmdSettings, IntelSettings handlers) | controller (tweak handler) | CRUD | `src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchB.cs` (multi-value registry-only handlers) | role-match |
| `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs` (NEW — DevicePowerSavings, NetAdapterPowerSavings, WriteCacheFlush, PowerPlan, TimerResolution handlers) | controller (tweak handler) | CRUD + event-driven (process spawn for powercfg/csc/sc) | `src/AkariToolbox.App/Services/TweakHandlers/ServiceBackedTweaks.cs` (service-controller-backed) + `WifiTweakHandler.cs` (multi-value capture-prior pattern) | role-match |
| `NetworkIpv4OnlyTweakHandler` (part of `GamingWindowsTweaks.cs`) | controller (tweak handler) | event-driven (PowerShell cmdlet invocation) | `IScriptRunner.RunProcessCaptureOutputAsync` call sites — none yet in-repo; closest analog is `DefenderTweakHandler`'s `RunProcess` pattern | role-match |
| `src/AkariToolbox.App/Services/IGamingDropdownService.cs` / `GamingDropdownService.cs` (NEW) | service | CRUD (live-read + write, non-boolean) | `IRegistryService`/`RegistryService.cs` (thin registry wrapper) + `ITweakCatalog`/`TweakCatalog.cs` (orchestration shape, but simpler — no revert semantics needed) | role-match |
| `src/AkariToolbox.App/Resources/GamingScripts/*.ps1` (NEW embedded resources, D-06 scripts) | config/asset | file-I/O | `NoDefender.cab` / `DisableDefender.ps1` embedded resources (see `AkariToolbox.App.csproj` `EmbeddedResource` entries + `DefenderTweakHandler.cs`) | exact |
| `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs` (NEW) | provider/viewmodel | request-response (UI bind + write-through) | `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs` | exact |
| `src/AkariToolbox.App/Views/GamingTweaksPage.xaml(.cs)` (NEW) | component | request-response | `src/AkariToolbox.App/Views/AkariOSTweaksPage.xaml(.cs)` | exact |
| `src/AkariToolbox.App/ViewModels/HomeViewModel.cs` (MODIFY — flip `Gaming Tweaks` card `IsEnabled: false → true`) | provider/viewmodel | CRUD (config flag) | itself, line 35 | exact |
| `src/AkariToolbox.App/MainWindow.xaml.cs` (MODIFY — flip nav entry `IsEnabled` false → true) | component | CRUD (config flag) | itself, line 79 | exact |
| Resolution/Refresh Rate + HAGS one-shot `RelayCommand`s (part of `GamingTweaksViewModel.cs`, D-05) | provider/viewmodel | event-driven (`Process.Start` URI launch) | no in-repo analog (new `Process.Start(UseShellExecute:true)` shape) — closest reference is `DefenderTweakHandler.DefenderRunElevatedPsFileAsync`'s `UseShellExecute = true` usage | partial |

## Pattern Assignments

### `src/AkariToolbox.App/Services/ITweakHandler.cs` + new `TweakCategory` enum (Pattern 1 — catalog filtering gap)

**Analog:** itself (additive change) — this is the single most important cross-cutting fix RESEARCH.md flags. Do this BEFORE any Gaming handler files are added.

**Current interface** (`src/AkariToolbox.App/Services/ITweakHandler.cs:11-32`):
```csharp
public interface ITweakHandler
{
    string Key { get; }
    string Title { get; }
    string Description { get; }
    int Order { get; }
    bool GetState();
    void SetState(bool enabled);
}
```

**Add:**
```csharp
public interface ITweakHandler
{
    string Key { get; }
    string Title { get; }
    string Description { get; }
    int Order { get; }
    TweakCategory Category { get; }   // NEW
    bool GetState();
    void SetState(bool enabled);
}

public enum TweakCategory { AkariOS, Gaming }
```

**Every one of the 32 existing handlers** (across `WifiTweakHandler.cs`, `RegistryTweaksBatchA.cs`, `RegistryTweaksBatchB.cs`, `ServiceBackedTweaks.cs`, `BcdeditDismTweaks.cs`, `DefenderTweakHandler.cs`) needs one line added, e.g. matching the existing property style at `WifiTweakHandler.cs:30-36`:
```csharp
public string Key => "wifi";
public string Title => "Disable WiFi";
public string Description => "Toggle WiFi On or Off";
public int Order => 0;
public TweakCategory Category => TweakCategory.AkariOS;   // NEW
```

New Gaming handlers use `TweakCategory.Gaming` and a disjoint `Order` range (recommend `100..110`) so the flat DI-wide list stays collision-free (RESEARCH.md Pattern 1, item 4).

**ViewModel filtering** — `AkariOSTweaksViewModel.cs:30` changes from:
```csharp
foreach (var handler in _catalog.Handlers)
```
to:
```csharp
foreach (var handler in _catalog.Handlers.Where(h => h.Category == TweakCategory.AkariOS))
```
`GamingTweaksViewModel.cs` (new) mirrors this exact constructor shape (see below) filtering on `TweakCategory.Gaming`.

**Test updates** — `src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs:48-56` and `:58-66` must scope to `Category == AkariOS` (still asserting 32/0..31 for that subset) plus a new parallel test asserting 11 Gaming handlers with disjoint orders. `TweakCatalog` itself (`TweakCatalog.cs`) needs NO interface change — `Handlers` stays one flat list (RESEARCH.md Pattern 1, item 5).

---

### `src/AkariToolbox.App/Services/TweakHandlers/GamingGraphicsTweaks.cs` (Hdcp, P0State, MsiMode, AmdSettings, IntelSettings)

**Analog:** `src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchB.cs` (multi-value registry handlers, e.g. `VbsTweakHandler`, `MpoTweakHandler`) + new `IRegistryService.GetSubKeyNames`

**Imports pattern** (`RegistryTweaksBatchB.cs:1-5`):
```csharp
using System.Runtime.InteropServices;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;
```

**Core handler shape** (`RegistryTweaksBatchB.cs:12-31`, `VbsTweakHandler` — multi-value write, single-value read):
```csharp
public sealed class VbsTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string DeviceGuard = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
    private const string HvciScenario = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";

    public string Key => "vbs";
    public string Title => "Enable VBS";
    public string Description => "Toggle Virtualization Based Security";
    public int Order => 20;

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, DeviceGuard, "EnableVirtualizationBasedSecurity") is int v && v == 1;

    public void SetState(bool enabled)
    {
        registry.SetValue(RegistryHive.LocalMachine, DeviceGuard, "EnableVirtualizationBasedSecurity", enabled ? 1 : 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, HvciScenario, "Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
    }
}
```

**Delete-on-disable pattern** (`MpoTweakHandler`, `RegistryTweaksBatchB.cs:60-85`) — use for Hdcp/P0State/MsiMode's "Off" branches per the D-06/scripts' own delete-based revert:
```csharp
public void SetState(bool enabled)
{
    if (enabled)
    {
        registry.SetValue(RegistryHive.LocalMachine, GraphicsDrivers, "DisableOverlays", 1, RegistryValueKind.DWord);
    }
    else
    {
        registry.DeleteValue(RegistryHive.LocalMachine, GraphicsDrivers, "DisableOverlays");
    }
}
```

**New `IRegistryService.GetSubKeyNames` member** (add to `src/AkariToolbox.Framework/Services/IRegistryService.cs`, implement in `RegistryService.cs`, matching the existing null-safety convention at `IRegistryService.cs:13-14`):
```csharp
/// <summary>Returns direct child subkey names under subKeyPath, or an empty list if absent.</summary>
IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath);
```
Implementation (RESEARCH.md Code Examples section):
```csharp
public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath)
{
    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
    using var subKey = baseKey.OpenSubKey(subKeyPath);
    return subKey?.GetSubKeyNames() ?? [];
}
```
Shared GPU-adapter filter helper (regex `^\d{4}$`, standardize per RESEARCH Pattern 3):
```csharp
private const string GpuDisplayClassGuid = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

private IEnumerable<string> GetGpuAdapterSubKeys(IRegistryService registry) =>
    registry.GetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid)
        .Where(name => System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d{4}$"));
```

**Prior-value capture pattern for MsiMode/AmdSettings** (per-instance iteration) — mirror `WifiTweakHandler.cs:19-28,38-69` (capture real live value on GetState, restore on SetState(false) rather than a hardcoded default):
```csharp
private int _priorWlanSvcStart = 2; // WifiTweakHandler's captured-prior-value field pattern
...
public bool GetState()
{
    var wlanStart = registry.GetValue(RegistryHive.LocalMachine, WlanSvc, "Start");
    if (wlanStart is int started && started == 4) return true;
    if (wlanStart is int wlan) { _priorWlanSvcStart = wlan; }
    return false;
}
```

**Standardize on `CurrentControlSet`** — never `ControlSet001`, per RESEARCH.md Pitfall 1, consistent with every existing handler's convention (`WifiTweakHandler.cs:14` uses `SYSTEM\CurrentControlSet\Services\WlanSvc`).

**PnP enumeration for MsiMode** — no in-repo analog exists; use `IScriptRunner.RunProcessCaptureOutputAsync` (see `IScriptRunner.cs:17-27` contract) exactly as documented in RESEARCH.md's Code Examples:
```csharp
var output = await scriptRunner.RunProcessCaptureOutputAsync(
    "powershell.exe",
    "-NoProfile -Command \"(Get-PnpDevice -Class Display).InstanceId\"");
var instanceIds = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
```

---

### `src/AkariToolbox.App/Services/TweakHandlers/GamingWindowsTweaks.cs` (DevicePowerSavings, NetAdapterPowerSavings, NetworkIpv4Only, WriteCacheFlush, PowerPlan, TimerResolution)

**Analog:** `ServiceBackedTweaks.cs` (for the service-lifecycle shape of TimerResolution) + `RegistryTweaksBatchB.cs`/`WifiTweakHandler.cs` (for the pure-registry handlers) + `IScriptRunner` (for PowerPlan/TimerResolution process-spawn calls)

**Service-lifecycle handler shape** (`ServiceBackedTweaks.cs:16-27`, `ClipboardTweakHandler` — closest existing analog for TimerResolution's compile-install-service flow, though TimerResolution is materially more complex per RESEARCH.md's dedicated subsection):
```csharp
public sealed class ClipboardTweakHandler(IWindowsServiceController serviceController) : ITweakHandler
{
    private const string Cbdhsvc = "cbdhsvc";
    public string Key => "clipboard";
    public string Title => "Enable Clipboard";
    public string Description => "Toggle Clipboard service On or Off";
    public int Order => 4;
    public bool GetState() => serviceController.GetStartType(Cbdhsvc) == 2;
    public void SetState(bool enable) => serviceController.SetStartType(Cbdhsvc, enable ? 2 : 4);
}
```

**Multi-service iteration shape** (`ServiceBackedTweaks.cs:30-57`, `BluetoothTweakHandler` — closest analog for DevicePowerSavings/NetAdapterPowerSavings iterating multiple subkeys):
```csharp
public void SetState(bool disable)
{
    var startVal = disable ? 4 : 3;
    foreach (var svc in Services)
    {
        serviceController.SetStartType(svc, startVal);
    }
}
```

**Process-spawn for PowerPlan** (`powercfg.exe`) — use `IScriptRunner.RunProcessAsync`/`RunProcessCaptureOutputAsync` (`IScriptRunner.cs:10-27`); implementation reference is `ScriptRunner.cs:16-59` (redirect stdout/stderr, timeout via `Task.WhenAny`, never throw — return `-1`/empty string). `SetState(true)` calls `powercfg -duplicatescheme`, `powercfg -setactive`, then a `powercfg /L` parse + delete loop; `SetState(false)` calls `powercfg -restoredefaultschemes` (destructive — see Common Pitfalls note below; RESEARCH.md flags an optional `-export`/`-import` hardening as A4, needs planner/user decision).

**Process-spawn for TimerResolution** (`csc.exe` compile + `sc.exe`/`ServiceController` install) — same `IScriptRunner.RunProcessAsync` primitive; the `System.ServiceProcess.ServiceController` package is already referenced (`AkariToolbox.App.csproj:44`) but has no existing call site in the App project yet — this is the first consumer. Pre-flight probe `File.Exists(cscPath)` before compiling (Pitfall 3), log failure via `ILogConsoleService.Log` exactly like `DefenderTweakHandler`'s error paths (`DefenderTweakHandler.cs:174-177`).

**NetworkIpv4Only** — no raw registry; shells to PowerShell cmdlets (`Disable-NetAdapterBinding`/`Enable-NetAdapterBinding`) via `IScriptRunner.RunProcessAsync("powershell.exe", "-NoProfile -Command \"Disable-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6\"")` — same primitive, no new capability needed.

---

### `src/AkariToolbox.App/Services/IGamingDropdownService.cs` / `GamingDropdownService.cs` (SvcHost + Win32PrioritySeparation dropdowns)

**Analog:** `src/AkariToolbox.Framework/Services/IRegistryService.cs` (thin wrapper shape) — this is NOT an `ITweakHandler` (non-boolean state), so it needs its own small interface, not a catalog entry.

**Registry read/write pattern to reuse directly** (`IRegistryService.cs:13-20`):
```csharp
object? GetValue(RegistryHive hive, string subKeyPath, string valueName);
void SetValue(RegistryHive hive, string subKeyPath, string valueName, object value, RegistryValueKind kind);
```
Live-read-on-load, match-to-nearest-preset-index, write-on-selection-change — no revert/prior-capture semantics needed (unlike `ITweakCatalog`/`TweakCatalog.cs`, which exists specifically for the boolean-toggle revert contract this dropdown doesn't need). Validate the selected index against the fixed preset array bounds before writing (predecessor precedent: `GamingTweaksViewModel.cs:29-30,56-57` `if (value < 0 || value >= Values.Length) return;` — security-relevant per RESEARCH.md's V5 Input Validation note).

---

### `src/AkariToolbox.App/Resources/GamingScripts/*.ps1` + `IScriptRunner.RunEmbeddedScriptAsync` (D-06 network scripts)

**Analog:** `DefenderTweakHandler.ExtractEmbeddedAsync` (`DefenderTweakHandler.cs:317-333`) — promote this exact logic into `IScriptRunner`, don't reinvent.

**Existing private implementation to generalize:**
```csharp
private static async Task<string> ExtractEmbeddedAsync(string endsWith, string destFileName)
{
    var asm = typeof(DefenderTweakHandler).Assembly;
    var name = asm.GetManifestResourceNames()
        .FirstOrDefault(n => n.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase))
        ?? throw new FileNotFoundException($"Embedded resource not found: {endsWith}");
    var dest = Path.Combine(Path.GetTempPath(), destFileName);
    await using var rs = asm.GetManifestResourceStream(name)!;
    await using var fs = File.Create(dest);
    await rs.CopyToAsync(fs);
    return dest;
}
```

**New shared `IScriptRunner.RunEmbeddedScriptAsync`** (add to `IScriptRunner.cs`, implement in `ScriptRunner.cs` alongside `RunProcessAsync`/`RunProcessCaptureOutputAsync`):
```csharp
public async Task<int> RunEmbeddedScriptAsync(string resourceSuffix, string? arguments = null, TimeSpan? timeout = null)
{
    var asm = typeof(ScriptRunner).Assembly;
    var name = asm.GetManifestResourceNames()
        .FirstOrDefault(n => n.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase))
        ?? throw new FileNotFoundException($"Embedded resource not found: {resourceSuffix}");
    var temp = Path.Combine(Path.GetTempPath(), $"AkariToolbox-{Guid.NewGuid():N}-{resourceSuffix}");
    try
    {
        await using (var rs = asm.GetManifestResourceStream(name)!)
        await using (var fs = File.Create(temp))
        {
            await rs.CopyToAsync(fs);
        }
        var args = $"-NoProfile -ExecutionPolicy Bypass -File \"{temp}\" {arguments}".TrimEnd();
        return await RunProcessAsync("powershell.exe", args, timeout);
    }
    finally
    {
        try { if (File.Exists(temp)) File.Delete(temp); } catch { /* best-effort cleanup */ }
    }
}
```

**Embedding convention** — mirror however `NoDefender.cab`/`DisableDefender.ps1` are declared as `<EmbeddedResource>` items in `AkariToolbox.App.csproj` (check the csproj for the exact glob/Include pattern used for those two files and replicate for the 6 D-06 branch-split `.ps1` files under `Resources/GamingScripts/`).

**Splitting each menu script into one resource per branch** — per RESEARCH.md Pattern 5, each D-06 script's `Read-Host` wrapper is stripped and each branch becomes its own embedded `.ps1` (e.g. `driverinstalllatest-nvidia.ps1`, `-amd.ps1`, `-intel.ps1`) with one UI button per branch — no stdin-piping added to `IScriptRunner`.

---

### `src/AkariToolbox.App/ViewModels/GamingTweaksViewModel.cs`

**Analog:** `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs` (full file, 128 lines) — mirror 1:1, changing only the `Handlers` filter and `Title`.

**Constructor/filtering pattern** (`AkariOSTweaksViewModel.cs:23-61`):
```csharp
public GamingTweaksViewModel(ITweakCatalog catalog, ILogConsoleService log)
{
    _catalog = catalog;
    _log = log;
    _dispatcher = DispatcherQueue.GetForCurrentThread();
    Title = "Gaming Tweaks";

    foreach (var handler in _catalog.Handlers.Where(h => h.Category == TweakCategory.Gaming))
    {
        var item = new TweakItem { Key = handler.Key, Title = handler.Title, Description = handler.Description, IsOn = false };
        Tweaks.Add(item);
        _ = TryGetStateAsync(_catalog, _log, handler).ContinueWith(
            task => _dispatcher.RunOnUIThreadAsync(() =>
            {
                item.IsOn = task.Result;
                item.PropertyChanged += OnTweakItemPropertyChanged;
            }),
            TaskScheduler.Default);
    }
}
```

**Write-through + error-correction pattern** (`AkariOSTweaksViewModel.cs:88-126`) — copy verbatim, including the CR-04 fix (re-read real live state and reflect it in the UI on fault, unsubscribe/resubscribe around the correction to avoid re-triggering a write).

**One-shot shortcut commands** (Resolution/Refresh Rate, HAGS Windowed, D-05) — no existing in-repo analog for a bare `Process.Start` URI launch; add as plain `[RelayCommand]` methods on `GamingTweaksViewModel`:
```csharp
[RelayCommand]
private void OpenDisplaySettings() =>
    Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true });

[RelayCommand]
private void OpenAdvancedGraphicsSettings() =>
    Process.Start(new ProcessStartInfo("ms-settings:display-advancedgraphics") { UseShellExecute = true });
```
(`UseShellExecute = true` convention borrowed from `DefenderTweakHandler.DefenderRunElevatedPsFileAsync`, `DefenderTweakHandler.cs:352-363`, which is the only existing `UseShellExecute = true` call site in the codebase.)

---

### `src/AkariToolbox.App/Views/GamingTweaksPage.xaml(.cs)`

**Analog:** `src/AkariToolbox.App/Views/AkariOSTweaksPage.xaml.cs` (full file, 17 lines) — copy verbatim structure:
```csharp
public sealed partial class AkariOSTweaksPage : Page
{
    public AkariOSTweaksViewModel ViewModel { get; }

    public AkariOSTweaksPage(AkariOSTweaksViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }
}
```
The `.xaml` markup itself should be read directly (not excerpted here) and adapted for the two additional dropdown controls (SvcHost, Win32PrioritySeparation) and the two one-shot shortcut buttons that `AkariOSTweaksPage.xaml` doesn't have.

---

## Shared Patterns

### `TweakCategory` discriminator (must land first)
**Source:** `src/AkariToolbox.App/Services/ITweakHandler.cs` (additive change)
**Apply to:** every existing 32 handler files + all new Gaming handler files + `AkariOSTweaksViewModel.cs` + new `GamingTweaksViewModel.cs` + `TweakHandlerOrderingTests.cs`
This is the mandatory architectural prerequisite RESEARCH.md flags — without it, new Gaming handlers break the existing 32-handler regression test and leak onto the Akari OS Tweaks page.

### Registry-squatting-safe read/write
**Source:** `src/AkariToolbox.Framework/Services/IRegistryService.cs` / `RegistryService.cs`
**Apply to:** every new Gaming registry-backed handler (Hdcp, P0State, MsiMode, AmdSettings, IntelSettings, DevicePowerSavings, NetAdapterPowerSavings, WriteCacheFlush, GamingDropdownService)
Never blind-create; open-then-create only when legitimately absent (existing convention, no change needed beyond the new `GetSubKeyNames` member).

### Live-read/live-write, no private state hive
**Source:** D-03/D-04 (Phase 1), every existing `ITweakHandler` implementation
**Apply to:** all 11 new stateful Gaming handlers — `GetState()` always reads the real live registry/service value; `SetState()` writes it directly; `ITweakCatalog` (unmodified) handles prior-value capture and no-op detection centrally, same as Phase 1.

### Process-spawn via `IScriptRunner`, never in-process PowerShell hosting
**Source:** `src/AkariToolbox.Framework/Services/ScriptRunner.cs`
**Apply to:** MsiMode (PnP enumeration), NetworkIpv4Only (NetAdapterBinding cmdlets), PowerPlan (`powercfg`), TimerResolution (`csc.exe`/`sc.exe`), all 6 D-06 embedded scripts
Per CLAUDE.md constraint: no `Microsoft.PowerShell.SDK`; every external call is a `Process.Start`-based spawn through the existing `IScriptRunner` interface.

### Embedded-resource extraction
**Source:** `DefenderTweakHandler.ExtractEmbeddedAsync` (`DefenderTweakHandler.cs:317-333`), promoted into `IScriptRunner.RunEmbeddedScriptAsync`
**Apply to:** all 6 D-06 network-dependent one-shot scripts (split per menu branch)

### `CurrentControlSet`, never `ControlSet001`
**Source:** existing convention across every Phase 1 handler (e.g. `WifiTweakHandler.cs:14`)
**Apply to:** every new Gaming registry handler, even where the source `.ps1` script hardcodes `ControlSet001` (Pitfall 1 — deliberate deviation from source-script literalism for correctness)

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `src/AkariToolbox.App/Services/TweakCategory.cs` | model/enum | — | Brand-new small enum type, nothing to model it on beyond the interface change itself |
| One-shot `ms-settings:` shortcut commands (D-05) | provider (RelayCommand) | event-driven (URI launch) | No existing `Process.Start(UseShellExecute:true, "ms-settings:...")` call site in the codebase; closest partial precedent is `DefenderTweakHandler`'s `UseShellExecute = true` runas launch, structurally different (elevation prompt vs. URI scheme) |
| `29 Power Plan.ps1` hardened revert (optional `-export`/`-import`, RESEARCH.md Assumption A4) | controller (tweak handler, special-case) | CRUD + file-I/O | No existing handler in this codebase performs a scheme-export/backup-before-mutate pattern; needs explicit user/planner approval before locking as the "real prior state" implementation per D-09-adjacent Open Question |

## Metadata

**Analog search scope:** `src/AkariToolbox.App/Services/`, `src/AkariToolbox.App/Services/TweakHandlers/`, `src/AkariToolbox.App/ViewModels/`, `src/AkariToolbox.App/Views/`, `src/AkariToolbox.Framework/Services/`, `src/AkariToolbox.Tests/`
**Files scanned:** `ITweakHandler.cs`, `ITweakCatalog.cs`, `TweakCatalog.cs`, `TweakHandlerRegistration.cs`, `WifiTweakHandler.cs`, `RegistryTweaksBatchB.cs`, `ServiceBackedTweaks.cs`, `DefenderTweakHandler.cs`, `IRegistryService.cs`, `IScriptRunner.cs`, `ScriptRunner.cs`, `AkariOSTweaksViewModel.cs`, `AkariOSTweaksPage.xaml.cs`, `HomeViewModel.cs`, `MainWindow.xaml.cs`, `TweakHandlerOrderingTests.cs`
**Pattern extraction date:** 2026-09-01
