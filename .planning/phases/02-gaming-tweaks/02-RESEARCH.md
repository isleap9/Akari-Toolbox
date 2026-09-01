# Phase 2: Gaming Tweaks - Research

**Researched:** 2026-09-01
**Domain:** WinUI 3 native port of PowerShell-authored gaming/latency registry & service tweaks (`ITweakHandler` extension), plus expanded registry-dropdown presets
**Confidence:** MEDIUM — the toggle-mapping logic is HIGH (read directly from the 19 canonical scripts and the existing codebase); the SvcHost/Win32Priority expanded preset *values* are LOW/ASSUMED (community-sourced, not Microsoft-authoritative) and explicitly flagged for user approval per D-09.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** `C:\PostInstall\` is fully deprecated. Gaming Tweaks must not depend on it in any form — no reading, writing, or launching files under that path.
- **D-02:** The Gaming Tweaks page is rebuilt from scratch, not ported 1:1 from `GamingTweaksViewModel.cs`/`GamingTweaksPage.xaml.cs`. The predecessor's private `HKCU\Software\AkariTool` index dropdown mechanism and `HasState`/`SaveState` toggle pattern are reference-only, superseded by the Phase 1 `ITweakHandler` live-state pattern.
- **D-03:** Predecessor's `TweakService.SetHdcp`/`SetNetworkOptimization` (bat-script calls under `C:\PostInstall\...`) are NOT ported — replaced entirely by the native scripts below.
- **D-04:** 5 local, registry-only scripts become live-state toggle tweaks via `ITweakHandler` (same live-read/real-prior-value-revert rule as Phase 1's 32 tweaks): `7 Hdcp.ps1`, `8 P0 State.ps1`, `9 Msi Mode.ps1`, `5 Amd Settings.ps1`, `6 Intel Settings.ps1`.
- **D-05:** `12 Resolution Refresh Rate.ps1` and `13 Hags Windowed.ps1` are one-shot launch-shortcut actions (`Start-Process ms-settings:display` / `ms-settings:display-advancedgraphics`), not stateful toggles — no live-state read/revert applies.
- **D-06:** 6 network-dependent scripts (`1 Driver Clean.ps1`, `2 Driver Install Latest.ps1`, `3 Driver Install Debloat & Settings.ps1`, `4 Nvidia Settings.ps1`, `10 DirectX.ps1`, `11 C++.ps1`) keep their live download-and-install behavior exactly as authored. **No added SHA256/signature verification for v1** — explicit user choice.
- **D-07:** Exactly 6 of `6 Windows`'s 36 scripts are in scope — `25 Device Manager Power Savings & Wake.ps1`, `26 Network Adapter Power Savings & Wake.ps1`, `27 Network IPv4 Only.ps1`, `28 Write Cache Buffer Flushing.ps1`, `29 Power Plan.ps1`, `30 Timer Resolution.ps1` — all local-only, become live-state toggle tweaks via `ITweakHandler`, same rule as D-04.
- **D-08:** The remaining 30 scripts in `6 Windows` are explicitly OUT of Gaming Tweaks scope.
- **D-09:** SvcHost split threshold and Win32 Priority Separation dropdowns are kept (direct registry writes, never PostInstall-dependent), enhanced with more preset values than the predecessor's fixed lists. Exact expanded value list is research's call to propose for user approval — not specified now.
- **D-10:** The "Services preset" dropdown (AkariOS Default vs Windows Default, PostInstall-`.reg`-dependent) is dropped entirely — not replaced by anything. `8 Advanced\17 Services.ps1` was explicitly considered and declined as a replacement.
- **D-11:** The predecessor's ~29-button NVIDIA/AMD/Useful-Tools quick-launch grid is dropped entirely — no third-party utility launcher in Gaming Tweaks v1.
- **D-12:** GAMING-02 is retired by D-11 — no longer satisfied by this phase (already reflected in REQUIREMENTS.md).

### Claude's Discretion

- Exact expanded preset value lists for SvcHost/Win32 Priority Separation dropdowns (D-09) — proposed below for approval before/at planning.
- How each interactive, `Read-Host`-driven console script (D-04/D-07) gets adapted into a non-interactive toggle invocation (strip the menu loop and call the underlying registry/command logic directly, vs. pipe simulated input) — resolved below: **strip the menu, call the underlying logic directly** (see Architecture Patterns).
- Mapping each script's "Recommended" (option 1) vs "Default" (option 2) branch to the toggle's On/Off state — verified per-script below; confirms 1:1 (Recommended = On) for all 11 stateful toggles.

### Deferred Ideas (OUT OF SCOPE)

- The other 30 `6 Windows` scripts (Bloatware checks, Start Menu, Theme, Widgets, Copilot, Edge/WebView, Notepad, Control Panel, UAC, Core Isolation, Defender Optimize, Autoruns Startup, Cleanup, Restore Point) — likely Debloat (Phase 3) or Misc (Phase 4) material.
- `8 Advanced/17 Services.ps1` — declined as the Gaming Services-dropdown replacement (D-10).
- The remaining ~97 scripts across the other "Ultimate" collection folders — untouched by this discussion, v2/ULT-01 territory.
- Third-party tool launcher grid, PostInstall asset mirror usage, GAMING-02 — retired, out of scope for this phase.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| GAMING-01 | User can toggle gaming/latency/service tweaks (SvcHost split threshold, Win32 priority separation, service configuration dropdowns), with the same real-state and revert behavior as Tweaks (TWEAKS-01/TWEAKS-03) | Section "Toggle Mapping Tables" below documents live-read/write logic for all 11 stateful `ITweakHandler` candidates plus a live-read approach for the 2 dropdowns. **Note:** the requirement's parenthetical "service configuration dropdowns" phrase predates 02-CONTEXT.md D-10, which drops the Services-preset dropdown entirely with no replacement — GAMING-01 is satisfied by the SvcHost + Win32PrioritySeparation dropdowns and the 11 toggles, not by any services dropdown. Flagged in Open Questions. |
</phase_requirements>

## Summary

Gaming Tweaks is a from-scratch page reusing Phase 1's `ITweakHandler`/`ITweakCatalog` live-state pattern, sourced from 19 canonical PowerShell scripts the user supplied (13 in `5 Graphics`, 6 in `6 Windows`). Of these: **11 become stateful `ITweakHandler` implementations** (5 in Graphics: Hdcp, P0 State, Msi Mode, Amd Settings, Intel Settings; 6 in Windows: Device Manager Power Savings, Network Adapter Power Savings, Network IPv4 Only, Write Cache Buffer Flushing, Power Plan, Timer Resolution), **2 are one-shot `ms-settings:` shortcut launches** (no state), and **6 are one-shot download-and-install actions** (D-06, network-dependent, all pulling supporting tools from `github.com/FR33THYFR33THY/Ultimate-Files` except official vendor driver sources). Two registry dropdowns (SvcHost split threshold, Win32 Priority Separation) are kept with expanded preset lists.

Every script in scope was read end-to-end this session. The single most important finding for planning is **not** about any individual script — it's that **`ITweakCatalog.Handlers` is currently one flat, unfiltered list consumed directly by `AkariOSTweaksViewModel`, and a hard regression test (`TweakHandlerOrderingTests.Resolving_ITweakHandler_yields_exactly_32_handlers`) asserts exactly 32 handlers with `Order` spanning `[0..31]` with no gaps**. Registering new Gaming `ITweakHandler`s the same way Phase 1 did (bare reflection scan → `services.AddSingleton(typeof(ITweakHandler), type)`) will (a) immediately break that test and (b) leak all 11 new Gaming toggles onto the existing Akari OS Tweaks page, because nothing currently filters `Handlers` by page/category. This directly contradicts 02-CONTEXT.md's code-context claim of "no catalog-plumbing changes needed" — a small, additive plumbing change (a category discriminator) is required. See Architecture Patterns.

Several source scripts also contain non-trivial gotchas worth flagging up front: the Graphics-folder scripts inconsistently hardcode `ControlSet001` vs. the safer `CurrentControlSet` alias; `29 Power Plan.ps1`'s "revert" (`powercfg -restoredefaultschemes`) is destructive by design (deletes every existing power scheme, including ones the app never touched) and cannot satisfy a literal "real prior state" guarantee without extra work; `30 Timer Resolution.ps1` compiles a C# Windows Service at runtime via a hardcoded `csc.exe` path; and `9 Msi Mode.ps1` needs PnP device enumeration (`Get-PnpDevice -Class Display`) that neither `IRegistryService` nor `IWindowsServiceController` currently expose.

**Primary recommendation:** Extend `IRegistryService` with a `GetSubKeyNames` method (needed by 4 of the 11 toggle handlers for GPU/network-adapter-class enumeration), add a `TweakCategory` discriminator to `ITweakHandler` so `ITweakCatalog.Handlers` can be filtered per page, generalize `DefenderTweakHandler`'s existing private `ExtractEmbeddedAsync` helper into a shared `IScriptRunner` capability for the 6 D-06 network scripts (embed as resources, strip each script's own `Read-Host` branch into one UI action per branch), and treat `29 Power Plan.ps1` and `30 Timer Resolution.ps1` as special-case handlers whose `SetState`/`GetState` semantics are approximations of "real prior state," not literal captures — document this the same way STATE.md already documents the same caveat for Phase 1's `VpnTweakHandler`/`BluetoothTweakHandler`.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| GPU/network-adapter registry toggle read/write | Backend (App/Framework services) | — | Registry is a local machine resource; no UI logic involved beyond binding a bool |
| PnP device (GPU instance) enumeration for MSI Mode | Backend (Framework service, new) | — | Requires `Get-PnpDevice`-equivalent; belongs beside `IRegistryService`/`IWindowsServiceController`, not in the ViewModel |
| Power Plan scheme creation/deletion via `powercfg` | Backend (Framework `IScriptRunner`) | — | `powercfg.exe` process invocation, same tier as existing bcdedit/DISM tweaks |
| Timer Resolution service compile + install | Backend (Framework, new capability) | — | Runtime C# compilation (`csc.exe`) + `System.ServiceProcess.ServiceController` (already referenced) is a backend-only concern |
| Driver/tool download-and-install (D-06) | Backend (`IScriptRunner` + embedded resource extraction) | — | Matches Phase 1's Defender cab+ps1 pattern; no UI logic beyond a button click |
| SvcHost/Win32Priority dropdown live-state read | Backend (App service, new small abstraction) | UI (ViewModel binds `SelectedIndex`) | Not boolean — doesn't fit `ITweakHandler`; needs its own live-read-on-load helper |
| Gaming Tweaks page toggle list rendering | UI (ViewModel + Page, mirrors `AkariOSTweaksViewModel`) | — | Same `TryGetStateAsync`/write-through-with-revert pattern as Phase 1, filtered to Gaming-category handlers only |
| Home dashboard card / nav sidebar entry flip | UI (`HomeViewModel.Cards`, `MainWindow.xaml.cs`) | — | `IsEnabled: false → true`, same mechanic already used for Phase 1's Akari OS Tweaks card |

## Standard Stack

No new external NuGet packages are required for this phase — everything needed is already referenced in the solution:

| Library | Version | Purpose | Already Present |
|---------|---------|---------|-----------------|
| `System.ServiceProcess.ServiceController` | 9.0.9 (per CLAUDE.md, already pinned) | Query/install the "Set Timer Resolution Service" the Timer Resolution toggle self-compiles | Yes — `AkariToolbox.App.csproj:44` `[VERIFIED: src/AkariToolbox.App/AkariToolbox.App.csproj:44]` (`<PackageReference Include="System.ServiceProcess.ServiceController" />`) |
| `Microsoft.Win32.Registry` (in-box) | n/a | All 11 toggle handlers' registry reads/writes via `IRegistryService` | Yes, already the whole Phase 1 pattern |
| `System.Diagnostics.Process` (in-box) | n/a | `powercfg.exe`, `sc.exe`, `csc.exe`, `powershell.exe`, `reg.exe`-equivalent invocations via `IScriptRunner` | Yes — `IScriptRunner`/`ScriptRunner` already built in Phase 1 |

**No `Package Legitimacy Audit` is required for this phase** — no new external packages are installed. The 6 network-dependent scripts (D-06) download third-party binaries at *runtime* (not build-time NuGet packages); their trust boundary is a script-execution/download-source concern (see Common Pitfalls), not a package-registry concern, and D-06 explicitly declines adding verification for v1.

## Architecture Patterns

### System Architecture Diagram

```
User toggles a Gaming switch (UI)
        │
        ▼
GamingTweaksViewModel.OnTweakItemPropertyChanged
        │  (mirrors AkariOSTweaksViewModel exactly)
        ▼
ITweakCatalog.SetStateAsync(key, enabled)
        │  (existing Phase 1 catalog: capture real prior value on first
        │   mutation this session, no-op if already at requested state)
        ▼
ITweakHandler.SetState(bool)  ── new Gaming handler, Category = Gaming
        │
        ├─► IRegistryService.SetValue / GetSubKeyNames (new)   ← Hdcp, P0State, AmdSettings,
        │                                                          IntelSettings, DevicePower,
        │                                                          NetAdapterPower, WriteCacheFlush
        ├─► IScriptRunner.RunProcessAsync("powershell.exe", ...) ← MsiMode (Get-PnpDevice),
        │      or new IPnpDeviceEnumerator (Framework)             NetworkIPv4Only (Disable/Enable-NetAdapterBinding)
        ├─► IScriptRunner.RunProcessAsync("powercfg", ...)      ← PowerPlan
        └─► IScriptRunner.RunProcessAsync("csc.exe"/"sc.exe")   ← TimerResolution (compile+install service)
                + IWindowsServiceController / ServiceController

One-shot actions (not ITweakHandler, plain RelayCommand):
  ResolutionRefreshRate / HagsWindowed → Process.Start("ms-settings:display[-advancedgraphics]")
  6 × D-06 driver/tool install buttons → IScriptRunner.RunEmbeddedScriptAsync (new)
        → extract embedded .ps1-derived logic to temp → powershell.exe -File <temp>
        (mirrors DefenderTweakHandler.ExtractEmbeddedAsync, generalized)

Dropdowns (not ITweakHandler — no boolean state):
  SvcHost split threshold / Win32PrioritySeparation
        → live IRegistryService.GetValue() on page load → match to nearest preset index
        → IRegistryService.SetValue() on selection change
```

### Recommended Project Structure

```
src/AkariToolbox.App/Services/TweakHandlers/
├── GamingGraphicsTweaks.cs      # HdcpTweakHandler, P0StateTweakHandler, MsiModeTweakHandler,
│                                 #   AmdSettingsTweakHandler, IntelSettingsTweakHandler
├── GamingWindowsTweaks.cs       # DevicePowerSavingsTweakHandler, NetAdapterPowerSavingsTweakHandler,
│                                 #   NetworkIpv4OnlyTweakHandler, WriteCacheFlushTweakHandler,
│                                 #   PowerPlanTweakHandler, TimerResolutionTweakHandler
src/AkariToolbox.App/Services/
├── IGamingDropdownService.cs    # new — live-read/write for SvcHost + Win32Priority (not ITweakHandler)
├── GamingDropdownService.cs
src/AkariToolbox.Framework/Services/
├── IRegistryService.cs          # extend: + GetSubKeyNames(hive, subKeyPath)
├── IScriptRunner.cs             # extend: + RunEmbeddedScriptAsync(resourceSuffix, args?) — generalizes
│                                 #   DefenderTweakHandler.ExtractEmbeddedAsync into a reusable primitive
src/AkariToolbox.App/Resources/GamingScripts/
├── driverclean.ps1 / driverinstalllatest.ps1 / driverinstalldebloat.ps1 /
│   nvidiasettings.ps1 / directx.ps1 / cpp.ps1     # embedded resources, D-06 scripts, largely as-authored
src/AkariToolbox.App/ViewModels/
├── GamingTweaksViewModel.cs     # mirrors AkariOSTweaksViewModel.cs 1:1, filtered to Category.Gaming
src/AkariToolbox.App/Views/
├── GamingTweaksPage.xaml(.cs)   # mirrors AkariOSTweaksPage.xaml(.cs)
```

### Pattern 1: The `ITweakCatalog.Handlers` filtering gap (must-fix before wiring new handlers)

**What:** `TweakHandlerServiceCollectionExtensions.AddTweakHandlers()` reflection-scans the whole assembly for non-abstract `ITweakHandler` implementations and registers every one as a multi-bound `ITweakHandler`. `TweakCatalog` exposes them all as one `Handlers` list sorted by `Order`. `AkariOSTweaksViewModel` constructs one `TweakItem` per entry in `_catalog.Handlers` — **unfiltered**.

`[VERIFIED: src/AkariToolbox.App/Services/TweakHandlerRegistration.cs:19-25]`
```csharp
var handlerTypes = typeof(WifiTweakHandler).Assembly.GetTypes()
    .Where(type => !type.IsAbstract && typeof(ITweakHandler).IsAssignableFrom(type));

foreach (var type in handlerTypes)
{
    services.AddSingleton(typeof(ITweakHandler), type);
}
```
`[VERIFIED: src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs:30]`
```csharp
foreach (var handler in _catalog.Handlers)
```
`[VERIFIED: src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs:48-56]`
```csharp
[Fact]
public void Resolving_ITweakHandler_yields_exactly_32_handlers()
{
    using var provider = BuildProvider();
    var handlers = provider.GetServices<ITweakHandler>().ToList();
    Assert.Equal(32, handlers.Count);
}
```
and `[VERIFIED: src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs:58-66]`
```csharp
Assert.Equal(Enumerable.Range(0, 32).ToList(), orders);
```
Every `Order` value 0–31 is already claimed — confirmed by grepping every handler file's `Order =>` (Wifi=0, Tsx=1, ActionCenter=2, Bootmenu/Dep=3, Clipboard=4, Bluetooth=5, VpnRelated=6/7, NtfsEnc=8, Fso=9, Notifications=10, Prefetch=11, Cdrom=12, Spooler=13, NoLazy=14, AdminUac=15, Mitigation=16, Uac=17, StartMenu=18, Hyperv=19, RegistryTweaksBatchB 20-29/31, Defender=30).

**When it matters:** As soon as new Gaming `ITweakHandler` classes are added and registered the same way, `Resolving_ITweakHandler_yields_exactly_32_handlers` fails (count becomes 43), the ordering test fails (Orders exceed 31 or collide), and — more importantly — the new handlers render on the *existing* Akari OS Tweaks page too, since nothing filters by page.

**Recommended fix (additive, minimal):**
1. Add `TweakCategory Category { get; }` to `ITweakHandler` (enum: `AkariOS`, `Gaming`). Every one of Phase 1's 32 existing handlers gets `Category => TweakCategory.AkariOS` added (one-line addition per file, 6 files) — no behavior change.
2. `AkariOSTweaksViewModel` filters `_catalog.Handlers.Where(h => h.Category == TweakCategory.AkariOS)`.
3. New `GamingTweaksViewModel` filters `_catalog.Handlers.Where(h => h.Category == TweakCategory.Gaming)`.
4. `TweakHandlerOrderingTests` needs updating: `Resolving_ITweakHandler_yields_exactly_32_handlers` should scope to `Category == AkariOS` (or be renamed/duplicated with a new Gaming-scoped variant asserting 11), and the Order-range assertion likewise scopes per category (or Gaming handlers use a disjoint range, e.g. `100..110`, so the flat DI-wide list stays collision-free without requiring Order to be category-relative).
5. `ITweakCatalog` itself needs no interface change — `Handlers` stays one flat list; only the two ViewModels add a `.Where(...)`.

This directly corrects 02-CONTEXT.md's "no catalog-plumbing changes needed" assumption — flag this to the user/planner explicitly as a scope correction, not a silent deviation.

### Pattern 2: Strip the `Read-Host` menu, expose the two branches as `GetState`/`SetState`

All 19 canonical scripts share the same shape: self-elevation check → `Write-Host "1. X (Recommended)" / "2. X: Default"` → `while ($true) { $choice = Read-Host ...; switch ($choice) { 1 { ... } 2 { ... } } }`. For the 11 stateful toggles (D-04/D-07), branch 1 ("Recommended") maps 1:1 to `SetState(true)`; branch 2 ("Default") maps to `SetState(false)`. This was verified per-script this session (see Toggle Mapping Tables) — no exceptions found among the 11.

`GetState()` cannot come from the source scripts (they have no read-only branch) — it must be synthesized per-handler by reading the same registry value(s) the "Recommended" branch writes and comparing against the Recommended value, following the exact precedent already established by every Phase 1 handler (e.g. `WifiTweakHandler.GetState()` reads `Start` and compares to `4`).

### Pattern 3: GPU/network-adapter subkey enumeration needs a new `IRegistryService` capability

4 of the 11 toggle handlers (Hdcp, P0 State, Amd Settings, Intel Settings) enumerate **subkey names** under a Device Setup Class GUID before reading/writing per-adapter values — `IRegistryService` has no such method today (`GetValue`/`SetValue`/`DeleteValue`/`OpenRealUserHive` only).

`[VERIFIED: src/AkariToolbox.Framework/Services/IRegistryService.cs:11-33]` — confirmed no subkey-enumeration member exists.

Recommend adding:
```csharp
/// <summary>Returns direct child subkey names under subKeyPath, or an empty list if absent.</summary>
IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath);
```
implemented the same registry-squatting-safe way as the rest of `RegistryService` (open, null-check, never throw on missing key). Each handler then applies its own filter (see Toggle Mapping Tables — the 19 scripts use two different filter heuristics; standardize on the safer one).

### Pattern 4: PnP device enumeration (Msi Mode) — no in-repo primitive exists

`9 Msi Mode.ps1` uses `Get-PnpDevice -Class Display` to get each GPU's `InstanceId`, then writes a registry value under `HKLM\SYSTEM\ControlSet001\Enum\$instanceID\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties`. Neither `IRegistryService` nor `IWindowsServiceController` enumerate PnP devices.

`[VERIFIED: C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/9 Msi Mode.ps1:22-28]`
```
$gpuDevices = Get-PnpDevice -Class Display
foreach ($gpu in $gpuDevices) {
$instanceID = $gpu.InstanceId
reg add "HKLM\SYSTEM\ControlSet001\Enum\$instanceID\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties" /v "MSISupported" /t REG_DWORD /d "1" /f | Out-Null
}
```

**Recommendation:** do not add a new NuGet dependency (no `System.Management`/WMI package is referenced anywhere in the solution today). Reuse the already-proven "shell out to `powershell.exe`" pattern via `IScriptRunner.RunProcessCaptureOutputAsync("powershell.exe", "-NoProfile -Command \"(Get-PnpDevice -Class Display).InstanceId\"")`, parse the newline-separated InstanceId strings, then perform the registry write via `IRegistryService.SetValue` (with `LocalMachine`/dynamic subkey path built from each InstanceId) rather than `reg.exe`. This keeps the "no in-process PowerShell hosting" CLAUDE.md constraint intact (a single non-interactive `-Command` invocation is a process spawn, not an SDK runspace) while avoiding a new package.

### Pattern 5: Embedded-resource script extraction — generalize existing precedent, don't invent one

`ScriptRunner`'s own doc comment explicitly defers this: *"no `.ps1` resources are embedded in Phase 1 ... A later phase that needs to run an embedded script should add that capability then, when there is a real call site to prove it against."* `[VERIFIED: src/AkariToolbox.Framework/Services/ScriptRunner.cs:9-12]` — **this is that phase.**

However, a private version of exactly this capability already exists and should be generalized rather than reinvented:

`[VERIFIED: src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs:317-333]`
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
This is also nearly identical to the predecessor's `ToolService.RunScript` pattern `[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ToolService.cs:88-109]`:
```csharp
public async Task RunScript(string scriptName)
{
    var resourceName = FindEmbeddedScriptResource(scriptName);
    ...
    var temp = Path.Combine(Path.GetTempPath(), $"AkariOS-{Guid.NewGuid():N}-{scriptName}");
    await ExtractEmbeddedScript(resourceName, temp);
    await RunProcess("powershell.exe", $"-ExecutionPolicy Bypass -File \"{temp}\"", timeout: null);
}
```
**Recommendation:** promote `ExtractEmbeddedAsync`'s logic into `IScriptRunner` as `Task<int> RunEmbeddedScriptAsync(string resourceSuffix, string? arguments = null, TimeSpan? timeout = null)` (extract to temp → `RunProcessAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{temp}\" {arguments}")` → delete temp in `finally`). `DefenderTweakHandler` can optionally be refactored to call the shared method (not required — TWEAKS-02 explicitly says Defender's code path stays untouched in v1) but the 6 D-06 scripts and any embedded-script call sites should use the new shared primitive going forward.

For the 6 D-06 scripts specifically: since each has its own internal `Read-Host` menu (DDU: 2 branches; Driver Install Latest: 3 branches; Nvidia Settings: 2 branches after an unconditional winget install step; Driver Clean/DirectX/C++ have none — no menu at all), and `IScriptRunner` has no stdin-redirect capability today, the cleanest option consistent with Pattern 2's "strip the menu, expose the branches directly" philosophy is: **split each menu-driven D-06 script into one embedded resource per branch** (e.g. `driverinstalllatest-nvidia.ps1`, `-amd.ps1`, `-intel.ps1`), each with the `Read-Host` wrapper removed, one UI button per branch. This avoids adding stdin-piping to `IScriptRunner` and keeps every embedded script's content otherwise byte-identical to its source branch (satisfying D-06's "exactly as authored" instruction at the level of the underlying install logic).

### Anti-Patterns to Avoid

- **Hardcoding `ControlSet001` instead of `CurrentControlSet`:** `5 Amd Settings.ps1`, `6 Intel Settings.ps1`, `9 Msi Mode.ps1`, `25 Device Manager Power Savings & Wake.ps1`, and `26 Network Adapter Power Savings & Wake.ps1` all hardcode `ControlSet001`, while `7 Hdcp.ps1` and `8 P0 State.ps1` use the self-resolving `CurrentControlSet` alias. `ControlSet001` is not guaranteed to be the *active* control set (Windows may boot from `ControlSet002` after a "last known good" recovery). Standardize every new C# handler on `CurrentControlSet` (which `RegistryHive`/`IRegistryService`'s existing convention already uses everywhere in Phase 1) regardless of which alias the source script happened to use.
- **Trusting a script's own "revert" branch as ground truth for `GetState`/`SetState(false)` semantics:** several D-07 scripts (`25`, `26`) `reg delete` the values instead of restoring a captured prior value — this is fine and matches Phase 1's own established precedent (`ITweakCatalog` captures the *live* prior value before the first mutation regardless of what the underlying script's "default" branch does), but don't assume the script's off-branch is itself state-accurate.
- **Treating `29 Power Plan.ps1`'s off-branch as a real revert:** `powercfg -restoredefaultschemes` deletes every power scheme on the system (including ones the user created independently of this app) and replaces them with only the 3 Windows-shipped defaults. This is destructive beyond the scope of "the tweak's own prior state" — see Common Pitfalls.
- **Adding `Microsoft.PowerShell.SDK` for in-process PowerShell hosting to solve the PnP-enumeration or stdin-menu problems above** — explicitly out per CLAUDE.md; use process-spawn (`powershell.exe -Command`/`-File`) via `IScriptRunner` instead, exactly as Phase 1 already does for Defender/bcdedit/DISM.

## Toggle Mapping Tables

Every value below was read directly from the named script this session; branch numbering matches the script's own `Write-Host "1. ... (Recommended)"` / `"2. ... Default"` labels. "On" = `SetState(true)` (Recommended); "Off" = `SetState(false)` (Default).

### `5 Graphics` — 5 local toggles (D-04)

| Script | Proposed Key | Registry path pattern | On value(s) | Off value(s) | Enumeration needed |
|---|---|---|---|---|---|
| `7 Hdcp.ps1` `[VERIFIED: 5 Graphics/7 Hdcp.ps1:24-30,49-55]` | `gpuhdcp` | `Registry::HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\<subkey>` for every direct child subkey **not** matching `*Configuration` | `RMHdcpKeyglobZero`=1 (DWORD) | `RMHdcpKeyglobZero`=0 (DWORD) | `GetSubKeyNames` on class GUID, filter out names ending `Configuration` |
| `8 P0 State.ps1` `[VERIFIED: 5 Graphics/8 P0 State.ps1:26-33,56-63]` | `gpup0state` | same class GUID/subkey pattern as Hdcp | `DisableDynamicPstate`=1 (DWORD) | `DisableDynamicPstate`=0 (DWORD) | same as Hdcp |
| `9 Msi Mode.ps1` `[VERIFIED: 5 Graphics/9 Msi Mode.ps1:22-27,56-61]` | `gpumsimode` | `HKLM\SYSTEM\ControlSet001\Enum\$InstanceId\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties` per GPU `InstanceId` (⚠ hardcodes `ControlSet001` — normalize to `CurrentControlSet`) | `MSISupported`=1 (DWORD) | `MSISupported`=0 (DWORD) | `Get-PnpDevice -Class Display` (Pattern 4) |
| `5 Amd Settings.ps1` `[VERIFIED: 5 Graphics/5 Amd Settings.ps1:11-139,141-256]` | `gpuamdsettings` | Mixed: `HKCU\Software\AMD\{CN,AIM,DVR}` fixed keys **plus** per-adapter class-GUID subkeys (`UMD`, `power_v1` nested subkeys, and direct 4-digit adapter keys) under `ControlSet001` (⚠ normalize to `CurrentControlSet`) | See full value list below | See full value list below | `GetSubKeyNames` + regex `^\d{4}$` filter, then nested lookup for `UMD`/`power_v1` child names |
| `6 Intel Settings.ps1` `[VERIFIED: 5 Graphics/6 Intel Settings.ps1:21-49,55-65]` | `gpuintelsettings` | Creates/deletes a `3DKeys` subkey under each 4-digit adapter key, `ControlSet001` (⚠ normalize) | Create `3DKeys`; `Global_AsyncFlipMode`=2, `Global_LowLatency`=0 (DWORD) | Delete the entire `3DKeys` subkey | `GetSubKeyNames` + regex `^\d{4}$` filter |

**AMD Settings full value list** (On → Off, all under `HKCU` unless noted):
`[VERIFIED: 5 Graphics/5 Amd Settings.ps1:29-137 (on), 148-255 (off)]`

| Value | On (Recommended) | Off (Default) |
|---|---|---|
| `HKCU\Software\AMD\CN\AutoUpdate` (DWORD) | `0` | value deleted |
| `HKCU\Software\AMD\AIM\LaunchBugTool` (DWORD) | `0` | `1` |
| `HKCU\Software\AMD\DVR\HotkeysDisabled` (DWORD) | `1` | value deleted |
| `HKCU\Software\AMD\CN\SystemTray` (REG_SZ) | `"false"` | value deleted |
| `HKCU\Software\AMD\DVR\ShowRSOverlay` (REG_SZ) | `"false"` | value deleted |
| `HKCU\Software\AMD\CN\RSXBrowserUnavailable` (REG_SZ) | `"true"` | value deleted |
| `HKCU\Software\AMD\CN\AllowWebContent` (REG_SZ) | `"false"` | value deleted |
| `HKCU\Software\AMD\CN\CN_Hide_Toast_Notification` (REG_SZ) | `"true"` | value deleted |
| `HKCU\Software\AMD\CN\AnimationEffect` (REG_SZ) | `"false"` | value deleted |
| `HKCU\Software\AMD\CN\WizardProfile` (REG_SZ) | `"PROFILE_CUSTOM"` | value deleted |
| `<classGuid>\<adapter>\UMD\VSyncControl` (BINARY, per adapter) | `"3000"` | `"31000000"` |
| `<classGuid>\<adapter>\UMD\TFQ` (BINARY, per adapter) | `"3200"` | value deleted |
| `<classGuid>\<adapter>\UMD\Tessellation` (BINARY, per adapter) | `"3100"` | `"360034000000"` |
| `<classGuid>\<adapter>\UMD\Tessellation_OPTION` (BINARY, per adapter) | `"3200"` | `"30000000"` |
| `HKCU\Software\AMD\CN\CustomResolutions\EulaAccepted` (REG_SZ) | `"true"` | subkey deleted |
| `HKCU\Software\AMD\CN\DisplayOverride\EulaAccepted` (REG_SZ) | `"true"` | subkey deleted |
| `<classGuid>\<adapter>\power_v1\abmlevel` (BINARY, per adapter) | `"00000000"` | value deleted |
| `<classGuid>\<4-digit adapter>\IsAutoDefault` | `BINARY "00000000"` | `DWORD 1` (type changes — as authored) |
| `<classGuid>\<4-digit adapter>\IsComponentControl` (BINARY) | `"0f000000"` | `"00000000"` |
| `HKCU\Software\AMD\CN\Notification` | deleted then recreated empty | subkey deleted |
| `HKCU\Software\AMD\CN\{FreeSync,OverlayNotification,VirtualSuperResolution}\AlreadyNotified` (DWORD) | `1` each | subkeys deleted |

Note: the script also opens/closes `RadeonSoftware.exe` once on the On branch as a side effect ("so settings stick") `[VERIFIED: 5 Graphics/5 Amd Settings.ps1:22-24]` — a best-effort UX step, not state; safe to keep or drop for the native port (recommend keeping via `IScriptRunner.RunProcessAsync`, best-effort/non-blocking on failure, since AMD's own software isn't guaranteed installed).

### `6 Windows` — 6 local toggles (D-07)

| Script | Proposed Key | Mechanism | On (Recommended) | Off (Default) |
|---|---|---|---|---|
| `25 Device Manager Power Savings & Wake.ps1` `[VERIFIED: 6 Windows/25 ...:23-118,128-224]` | `devpowersavings` | Recurse `HKLM:\SYSTEM\ControlSet001\Enum\{ACPI,HID,PCI,USB}` (⚠ normalize to `CurrentControlSet`), match child subkeys named `Device Parameters` and `WDF` | On each `Device Parameters`: `EnhancedPowerManagementEnabled`=0, `SelectiveSuspendEnabled`=`00`(BINARY, note ACPI branch has a source typo `SeleactiveSuspendEnabled` — preserve as authored, see Pitfalls), `SelectiveSuspendOn`=0, `WaitWakeEnabled`=0 (DWORD); on each `WDF`: `IdleInWorkingState`=0 | All listed values deleted |
| `26 Network Adapter Power Savings & Wake.ps1` `[VERIFIED: 6 Windows/26 ...:24-62,74-113]` | `netpowersavings` | Enumerate 4-digit subkeys under `HKLM:\System\ControlSet001\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}` (⚠ normalize) | `PnPCapabilities`=24(DWORD); `AdvancedEEE`/`*EEE`/`EEELinkAdvertisement`/`SipsEnabled`/`ULPMode`/`GigaLite`/`EnableGreenEthernet`/`PowerSavingMode`/`S5WakeOnLan`/`*WakeOnMagicPacket`/`*ModernStandbyWoLMagicPacket`/`*WakeOnPattern`/`WakeOnLink` = `"0"` (REG_SZ, all) | All listed values deleted |
| `27 Network IPv4 Only.ps1` `[VERIFIED: 6 Windows/27 ...:27-30,53-56]` | `netipv4only` | PowerShell `NetAdapterBinding` cmdlets, not raw registry | `Disable-NetAdapterBinding -Name "*" -ComponentID <id>` for `ms_lldp,ms_lltdio,ms_implat,ms_rspndr,ms_tcpip6,ms_server,ms_msclient,ms_pacer` | `Enable-NetAdapterBinding` for the same list **plus** `ms_tcpip` |
| `28 Write Cache Buffer Flushing.ps1` `[VERIFIED: 6 Windows/28 ...:26-37,49-60]` | `writecacheflush` | On: create/write `...\Device Parameters\Disk` subkey under every `SCSI`/`NVME` `Device Parameters` match (⚠ `ControlSet001`); Off: **delete** every subkey directly named `Disk` (asymmetric match target — see Pitfalls) | `CacheIsPowerProtected`=1 (DWORD) under new `\Disk` subkey | Entire `Disk` subkey deleted |
| `29 Power Plan.ps1` `[VERIFIED: 6 Windows/29 ...:21-230,239-269]` | `powerplan` | `powercfg` process calls — see dedicated subsection below | Duplicate Ultimate Performance scheme to fixed GUID `99999999-9999-9999-9999-999999999999`, delete all other schemes, set active, ~14 registry values, ~25 `powercfg /set{a,d}cvalueindex` pairs | `powercfg -restoredefaultschemes` (destructive — see Pitfalls), re-enable hibernate/lock/sleep/fast-boot/power-throttling |
| `30 Timer Resolution.ps1` `[VERIFIED: 6 Windows/30 ...:21-244,249-266]` | `timerresolution` | Compile+install a Windows Service — see dedicated subsection below | Write C# source → compile via `csc.exe` → `New-Service -Name "Set Timer Resolution Service"` (Auto, Running) → `GlobalTimerResolutionRequests`=1 (DWORD) | Stop/delete service, delete binary, delete `GlobalTimerResolutionRequests` |

#### `29 Power Plan.ps1` — full detail

On branch creates a custom scheme (fixed GUID `99999999-9999-9999-9999-999999999999`, duplicated from Ultimate Performance base `e9a42b02-d5df-448d-aa00-03f14749eb61`), sets it active, then **enumerates and deletes every other existing power scheme** (`powercfg /L` parsed for GUIDs, each `powercfg /delete <guid>`). It disables hibernate (`powercfg /hibernate off` + `HibernateEnabled`/`HibernateEnabledDefault`=0), lock/sleep menu items (`ShowLockOption`/`ShowSleepOption`=0), fast boot (`HiberbootEnabled`=0), and power throttling (`PowerThrottlingOff`=1), all under `HKLM\SYSTEM\CurrentControlSet\Control\...`. It then sets ~25 `powercfg /setacvalueindex`/`/setdcvalueindex` pairs against the new scheme GUID for hard-disk timeout, sleep/hibernate timers, USB selective suspend, PCIe link-state power management, min/max processor state (100%/100%), display timeout/brightness, and battery-notification thresholds (full GUID pairs at `[VERIFIED: 6 Windows/29 Power Plan.ps1:65-227]`).

Off branch calls `powercfg -restoredefaultschemes` (restores only the 3 Windows-shipped defaults: Balanced/Power saver/High performance — any custom scheme that existed *before* the On branch ran, including ones unrelated to this app, is unrecoverably gone), then reverses the hibernate/lock/sleep/fast-boot/power-throttling registry writes and hides (rather than deletes) the two `Attributes`=1 USB/core-parking visibility keys.

#### `30 Timer Resolution.ps1` — full detail

On branch writes a ~200-line C# source file (`SetTimerResolutionService.cs`, a Windows Service named internally `STR` that P/Invokes `NtSetTimerResolution`/`NtQueryTimerResolution` from `ntdll.dll` to force the OS's minimum timer resolution) to `C:\Windows\SetTimerResolutionService.cs`, compiles it via `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` into `C:\Windows\SetTimerResolutionService.exe`, deletes the source, then registers it as a service via `New-Service -Name "Set Timer Resolution Service" -BinaryPathName "...\SetTimerResolutionService.exe"`, sets `StartupType Auto`, starts it, and sets `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel\GlobalTimerResolutionRequests`=1 (DWORD). `[VERIFIED: 6 Windows/30 Timer Resolution.ps1:219-239]`. Off branch stops/disables/deletes the service (`sc.exe delete`), deletes the `.exe`, and deletes the `GlobalTimerResolutionRequests` value.

### `5 Graphics` — 2 one-shot shortcuts (D-05, no state)

`[VERIFIED: 5 Graphics/12 Resolution Refresh Rate.ps1:1]` — `Start-Process "ms-settings:display"` (single line, no elevation check, no menu).
`[VERIFIED: 5 Graphics/13 Hags Windowed.ps1:1]` — `Start-Process "ms-settings:display-advancedgraphics"` (single line, no elevation check, no menu).

Confirmed: both are literally one-line files. Implement as two plain `RelayCommand`s calling `Process.Start(new ProcessStartInfo("ms-settings:display[-advancedgraphics]") { UseShellExecute = true })` directly from the ViewModel — no `ITweakHandler`, no `IScriptRunner` needed (these aren't external processes, they're URI-scheme launches).

### `5 Graphics` — 6 network-dependent one-shot actions (D-06)

| Script | Download source(s) | Notes |
|---|---|---|
| `1 Driver Clean.ps1` `[VERIFIED: 5 Graphics/1 ...:35,52,105-127,166,213-241]` | `github.com/FR33THYFR33THY/Ultimate-Files` (`7zip.exe`, `ddu.exe`) | 2-branch menu (DDU Auto / DDU Manual); both branches install 7-Zip, extract DDU, write a static `Settings.xml`, then reboot into Safe Mode via `bcdedit`+`RunOnce` to run DDU headless (Auto) or interactively (Manual) |
| `2 Driver Install Latest.ps1` `[VERIFIED: 5 Graphics/2 ...:41,47,51,55,64-78,87-88]` | **NVIDIA**: official `gfwsl.geforce.com`/`international.download.nvidia.com`; **AMD**: official `amd.com` driver page (scraped for a matching link); **Intel**: opens official `intel.com` search page (no direct download) | 3-branch menu (NVIDIA/AMD/Intel) — the only D-06 script that does **not** touch Ultimate-Files |
| `3 Driver Install Debloat & Settings.ps1` `[VERIFIED: 5 Graphics/3 ...:22,59,174,392,603]` (771 lines, grep-verified for all network calls) | `github.com/FR33THYFR33THY/Ultimate-Files` (`7zip.exe`, `inspector.exe`) + opens official nvidia.com/amd.com/intel.com pages | Largest script in scope; full line-by-line branch logic not itemized here (out of scope per D-06 — "keep exactly as authored", not decomposed into toggle state) |
| `4 Nvidia Settings.ps1` `[VERIFIED: 5 Graphics/4 ...:22,35,38-39,85,328]` | `github.com/FR33THYFR33THY/Ultimate-Files` (`7zip.exe`, `inspector.exe`) **plus `winget install "9NF8H0H7WMLT"`** (NVIDIA Control Panel, Store app ID) | New environment dependency: `winget.exe` — flag in Environment Availability |
| `10 DirectX.ps1` `[VERIFIED: 5 Graphics/10 ...:24,38]` | `github.com/FR33THYFR33THY/Ultimate-Files` (`7zip.exe`, `directx.exe`) | No menu — single linear script |
| `11 C++.ps1` `[VERIFIED: 5 Graphics/11 ...:24-35]` | `github.com/FR33THYFR33THY/Ultimate-Files` (12 vcredist installers, 2005–2022, x86+x64) | No menu — single linear script |

**Confirmed answer to investigation question 3:** 5 of 6 scripts pull supporting tools (7-Zip, DDU, NVIDIA Profile Inspector, DirectX redist, vcredist) from `github.com/FR33THYFR33THY/Ultimate-Files`; only `2 Driver Install Latest.ps1` differs, sourcing GPU drivers themselves from each vendor's own official domain. All 6 should be invoked via the generalized `IScriptRunner.RunEmbeddedScriptAsync` (Pattern 5) — embedded-resource-extract-and-run, matching Phase 1's Defender cab+ps1 precedent, **not** `Microsoft.PowerShell.SDK` in-process hosting.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| GPU class-GUID subkey enumeration (Hdcp, P0 State, Amd, Intel) | 4 separate ad-hoc `Get-ChildItem`-equivalent loops, each with its own filter heuristic | One `IRegistryService.GetSubKeyNames(hive, path)` + a shared `^\d{4}$` regex helper reused by all 4 handlers | The 19 source scripts already show 2 different, inconsistent filter heuristics (`-notlike '*Configuration'` vs `^\d{4}$` regex) for the *same* underlying problem — standardizing prevents a 3rd inconsistent variant from creeping into the C# port |
| GPU PnP-device enumeration (Msi Mode) | P/Invoke to SetupAPI (`CM_Get_Device_ID_List`) or a new WMI/`System.Management` dependency | `IScriptRunner.RunProcessCaptureOutputAsync("powershell.exe", "-Command \"(Get-PnpDevice -Class Display).InstanceId\"")`, parsed | No `System.Management` package exists anywhere in this solution today; adding one for a single call site is disproportionate. A single non-interactive `-Command` process spawn reuses the already-proven `IScriptRunner` primitive and stays inside CLAUDE.md's "no in-process PowerShell hosting" boundary |
| REG_BINARY hex-string values (AMD Settings: `VSyncControl`, `TFQ`, `Tessellation*`, `IsComponentControl`, `abmlevel`) | Manual byte-array literals scattered across the handler | One small `HexStringToBytes(string hex)` helper (pairs of hex chars → `byte[]`) shared by `AmdSettingsTweakHandler`, mirroring how `reg.exe /t REG_BINARY /d "3000"` itself parses the string | `RegistryKey.SetValue(..., RegistryValueKind.Binary)` requires a `byte[]`, not a string — every REG_BINARY value quoted in the Toggle Mapping Tables needs this conversion once, not five times |
| Embedded PowerShell script extraction (D-06) | A second private `ExtractEmbeddedAsync`-style method inside a new Gaming handler class | Promote `DefenderTweakHandler.ExtractEmbeddedAsync`'s logic into `IScriptRunner.RunEmbeddedScriptAsync` (Pattern 5) | Already implemented once in this exact codebase; a second private copy is the "don't hand-roll" smell this project's own architecture note (`ScriptRunner.cs:9-12`) explicitly anticipated a future phase would resolve |

**Key insight:** almost every "new capability" this phase needs (subkey enumeration, PnP device listing, embedded-script extraction) already has either a documented gap comment pointing at it (`ScriptRunner.cs`) or a private, unshared implementation elsewhere in the same codebase (`DefenderTweakHandler`). The work is generalization, not invention.

## Common Pitfalls

### Pitfall 1: `ControlSet001` vs `CurrentControlSet` inconsistency across the source scripts
**What goes wrong:** A tweak silently no-ops or writes to the wrong control set on a machine that most recently booted from "Last Known Good Configuration" (which activates `ControlSet002`, not `ControlSet001`).
**Why it happens:** 5 of the 11 source scripts hardcode `ControlSet001`; 2 use the self-resolving `CurrentControlSet` registry alias.
**How to avoid:** Every new C# handler in this phase should target `CurrentControlSet` regardless of what its source script used — this is also consistent with every Phase 1 handler's existing convention (`WlanSvc`, `VWifiFlt`, etc. all use `SYSTEM\CurrentControlSet\Services\...`).
**Warning signs:** A toggle reports correct-looking `GetState()` on a fresh install but silently fails to apply after a system-recovery event; hard to reproduce without deliberately forcing a control-set switch.

### Pitfall 2: `29 Power Plan.ps1`'s "revert" is destructive, not a real prior-state restore
**What goes wrong:** If GAMING-01's "same real-state and revert behavior as Tweaks (TWEAKS-01/TWEAKS-03)" is read literally for Power Plan, it's unsatisfiable as authored: the On branch enumerates and **deletes every existing power scheme** before creating its own, and the Off branch (`powercfg -restoredefaultschemes`) only restores the 3 Windows-shipped defaults — any custom scheme the user created independently of this app, before ever touching this toggle, is permanently gone the moment On is applied.
**Why it happens:** The source script was authored as a one-way "optimize for gaming" action with a best-effort "undo," not as a byte-for-byte state machine.
**How to avoid:** Two options for the planner to choose between (flagged for user/planning decision, not resolved here): (a) accept the lossy behavior as-authored (matches D-06's "no added verification" precedent of accepting the scripts' own risk tradeoffs) and document `GetState()` as "is the app's custom scheme (`99999999-...`) currently active" rather than a literal snapshot; or (b) harden it — before the On branch's `powercfg /delete` loop, export every existing scheme via `powercfg -export <guid> <file>` to a per-session temp folder, and on Off, `powercfg -import` them back before/instead of `-restoredefaultschemes`. Option (b) is a real improvement over the source script and low-risk to add (no new dependency, same `powercfg.exe` already being shelled out to).
**Warning signs:** A user with a pre-existing custom power plan (e.g., from OEM software) reports it "disappeared" after toggling Gaming Tweaks' Power Plan off, not on.

### Pitfall 3: Timer Resolution's runtime C# compilation is fragile
**What goes wrong:** `csc.exe` is invoked at the hardcoded path `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe` — a specific .NET Framework 4.x SxS install location. If that exact Framework version/architecture isn't present (unlikely but not guaranteed on all Windows 10/11 SKUs, especially Server or N/KN editions), the On branch fails silently mid-sequence (the script has no error handling around `Start-Process -Wait`).
**Why it happens:** The script reimplements the well-known open-source `SetTimerResolution` service (valleyofdoom) by generating and compiling its C# source at runtime rather than shipping a prebuilt binary — likely done specifically to keep this tweak local-only/network-free (consistent with D-07's "local-only" scoping).
**How to avoid:** Before compiling, probe `csc.exe`'s existence (`File.Exists`) via the handler's `GetState()`/pre-flight and surface a clear failure through `ILogConsoleService` rather than a silent no-op; document this as an Environment Availability item (below) so the planner can decide on a fallback (e.g., skip gracefully with a log message vs. block the toggle).
**Warning signs:** `Set-Service -Name "Set Timer Resolution Service"` calls silently fail (`-ErrorAction SilentlyContinue` throughout the source script) with no compiled binary present.

### Pitfall 4: `28 Write Cache Buffer Flushing.ps1` uses asymmetric subkey match targets between On and Off
**What goes wrong:** The On branch matches subkeys named `"Device Parameters"` and appends `\Disk` to create a new child subkey there; the Off branch matches subkeys named `"Disk"` directly and deletes them entirely. A naive C# port that reuses one enumeration helper for both directions will silently do nothing on one side.
**Why it happens:** As authored in the source script — verified this session, not a typo in the phase's own reading of it.
**How to avoid:** Implement `GetState()`/`SetState()` with two explicit, differently-targeted enumeration calls, matching the script's own asymmetry exactly; add a unit test asserting both directions against a fake registry tree.
**Warning signs:** Toggling Write Cache Buffer Flushing off leaves the `Disk` subkey (and `CacheIsPowerProtected`) in place — looks like a no-op revert.

### Pitfall 5: `25 Device Manager Power Savings & Wake.ps1` has a source typo (`SeleactiveSuspendEnabled`) — preserve it or fix it, decide at planning
**What goes wrong:** The ACPI branch of the On path writes a misspelled value name `SeleactiveSuspendEnabled` (extra "a") instead of the correct `SelectiveSuspendEnabled` used consistently in the HID/PCI/USB branches of the same script. Windows never reads a value by that misspelled name, so this specific write is a dead no-op for ACPI devices only.
**Why it happens:** Author typo in the source script.
**How to avoid:** Flag as an Open Question for planning — porting "exactly as authored" (matching D-04's philosophy of trusting the source scripts) means reproducing the typo; silently "fixing" it changes real-world behavior (ACPI devices would newly get the correct suspend-disable they never got before) beyond what was asked. Recommend: preserve the typo as-authored for v1 parity, note it in a code comment, and let a future phase decide whether to fix it as a deliberate behavior change.

## Code Examples

### Registry-squatting-safe subkey-name enumeration (new `IRegistryService` member, matches existing null-check philosophy)
```csharp
// Source: pattern matches existing IRegistryService.GetValue's null-safety convention
// (src/AkariToolbox.Framework/Services/IRegistryService.cs:13-14)
public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath)
{
    using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
    using var subKey = baseKey.OpenSubKey(subKeyPath);
    return subKey?.GetSubKeyNames() ?? [];
}
```

### GPU adapter subkey filtering — standardize on the safer `^\d{4}$` regex (used by Amd/Intel Settings in source), not the `-notlike '*Configuration'` heuristic (used by Hdcp/P0 State in source)
```csharp
// Source: derived from 5 Graphics/5 Amd Settings.ps1:113 ($key.PSChildName -match '^\d{4}$')
private const string GpuDisplayClassGuid = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

private IEnumerable<string> GetGpuAdapterSubKeys(IRegistryService registry) =>
    registry.GetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid)
        .Where(name => System.Text.RegularExpressions.Regex.IsMatch(name, @"^\d{4}$"));
```

### Embedded-script runner (generalizes `DefenderTweakHandler.ExtractEmbeddedAsync`, per Pattern 5)
```csharp
// Source: src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs:317-333 (generalized)
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

### PnP GPU device enumeration via existing `IScriptRunner` (Pattern 4 — no new package)
```csharp
// Source: pattern reuses IScriptRunner.RunProcessCaptureOutputAsync
// (src/AkariToolbox.Framework/Services/IScriptRunner.cs:17-27)
var output = await scriptRunner.RunProcessCaptureOutputAsync(
    "powershell.exe",
    "-NoProfile -Command \"(Get-PnpDevice -Class Display).InstanceId\"");
var instanceIds = output
    .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
```

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Expanded SvcHost split threshold preset list (below) — sourced from community/forum content via WebSearch, not a Microsoft-authoritative page; the search itself noted "I did not find an official Microsoft documentation page specifically detailing these recommended values." | D-09 proposal | User picks a preset that doesn't match their actual RAM tier as well as intended; low real-world risk (the DWORD is just a threshold, not destructive), but the exact preset *labels/values* need explicit user sign-off before being locked in a plan |
| A2 | Expanded Win32PrioritySeparation preset list (below) and its bit-decode table — sourced from community forum/blog content via WebSearch, cross-referenced across 2 searches for consistency but not from a Microsoft Learn page | D-09 proposal | If the bit-decode is subtly wrong, a preset could apply an unintended scheduling behavior (background-process starvation or excessive foreground boost) — low severity (reversible via the same dropdown), but should be verified against the predecessor's known-working 5 values before shipping the other 7 as new |
| A3 | The predecessor's `SvcHostValues[0] = 380000` (labeled "Default") is a decimal/hex confusion bug — the actual Windows default is `0x380000` = decimal `3670016` KB, per this session's WebSearch cross-check | Predecessor discrepancy note (see below) | If this analysis is wrong, "removing the override" (this research's recommended fix) could differ from the predecessor's intended behavior in some edge case; low risk since deleting the value reverts to Windows' own computed default either way |
| A4 | `29 Power Plan.ps1`'s hardening recommendation (`powercfg -export`/`-import` around the destructive delete) is proposed by this research, not present in the source script — needs explicit user approval before a plan commits to it, since it changes behavior beyond "port exactly as authored" | Common Pitfalls, Pitfall 2 | If declined, ship the destructive revert as-authored (matches D-06's precedent of accepting source-script risk tradeoffs for v1) |
| A5 | Recommendation to preserve the `SeleactiveSuspendEnabled` typo as-authored (Pitfall 5) rather than silently fix it | Common Pitfalls, Pitfall 5 | If the user actually wants ACPI devices to get the correct suspend-disable, the typo needs a deliberate fix flagged as a behavior change, not a silent "bug fix" |

## Predecessor discrepancy: SvcHost "Default" value looks like a bug

`[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/GamingTweaksViewModel.cs:21-24]`
```csharp
public IReadOnlyList<string> SvcHostOptions { get; } = new[]
    { "Default (380000 KB)", "4GB", "8GB", "16GB", "32GB", "64GB" };
private static readonly long[] SvcHostValues = { 380000, 4194304, 8388608, 16777216, 33554432, 67108864 };
```
The predecessor writes literal decimal `380000` (≈ 371 MB) for its "Default" preset. Windows' actual default `SvcHostSplitThresholdInKB` is `0x380000` hex = decimal `3,670,016` KB (≈ 3.5 GB) — this looks like a decimal/hex transcription bug in the predecessor (A3 above), not an intentional "aggressive splitting" preset (the label says "Default", not "Aggressive"). **Recommendation:** for the new "Default" preset, delete the `SvcHostSplitThresholdInKB` value entirely (letting Windows compute its own default) rather than writing either `380000` or `3670016` literally — this also matches the delete-on-revert convention used by nearly every other toggle in this phase and in Phase 1.

## Proposed expanded preset lists (D-09 — for user approval, not locked)

### SvcHost split threshold (`HKLM\SYSTEM\CurrentControlSet\Control`, value `SvcHostSplitThresholdInKB`, DWORD, KB)

| Label | Value | Confidence |
|---|---|---|
| Default | *(delete value — let Windows compute its own default)* | `[CITED: community sources]` — see A3 |
| 4 GB | 4,194,304 | `[CITED: community sources]` — matches predecessor |
| 8 GB | 8,388,608 | `[CITED: community sources]` |
| 12 GB | 12,582,912 | `[CITED: community sources]` |
| 16 GB | 16,777,216 | `[CITED: community sources]` — matches predecessor |
| 24 GB | 25,165,824 | `[CITED: community sources]` |
| 32 GB | 33,554,432 | `[CITED: community sources]` — matches predecessor |
| 48 GB | 50,331,648 | `[ASSUMED]` — extrapolated (GB × 1,048,576), not independently found in search results |
| 64 GB | 67,108,864 | `[CITED: community sources]` — matches predecessor |
| 128 GB | 134,217,728 | `[ASSUMED]` — extrapolated for high-end workstation builds, not independently found in search results |

### Win32 Priority Separation (`HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl`, value `Win32PrioritySeparation`, DWORD)

| Label | Hex | Decimal | Meaning (per cross-checked community sources) | In predecessor? |
|---|---|---|---|---|
| Short, Fixed, High boost | `0x2A` | 42 | Short quantum, fixed length, high foreground boost | Yes (predecessor's default-recommended) |
| Short, Fixed, Medium boost | `0x29` | 41 | Short quantum, fixed length, medium foreground boost | No — new |
| Short, Fixed, No boost | `0x28` | 40 | Short quantum, fixed length, no foreground boost | Yes |
| Short, Variable, High boost | `0x26` | 38 | Short quantum, variable length, high foreground boost | Yes |
| Short, Variable, Medium boost | `0x25` | 37 | Short quantum, variable length, medium foreground boost | No — new |
| Short, Variable, No boost | `0x24` | 36 | Short quantum, variable length, no foreground boost | No — new |
| Long, Fixed, High boost | `0x1A` | 26 | Long quantum, fixed length, high foreground boost | No — new |
| Long, Fixed, Medium boost | `0x19` | 25 | Long quantum, fixed length, medium foreground boost | No — new |
| Long, Fixed, No boost | `0x18` | 24 | Long quantum, fixed length, no foreground boost | No — new |
| Long, Variable, High boost | `0x16` | 22 | Long quantum, variable length, high foreground boost | Yes |
| Long, Variable, Medium boost | `0x15` | 21 | Long quantum, variable length, medium foreground boost | No — new |
| Long, Variable, No boost | `0x14` | 20 | Long quantum, variable length, no foreground boost | No — new |
| Legacy/Advanced | `0x06` | 6 | Predecessor's 5th preset — bit meaning not independently confirmed this session | Yes (unresolved semantics — see Open Questions) |

All 13 rows tagged `[ASSUMED]`/LOW confidence (community-sourced, cross-checked across 2 independent searches for internal consistency but not verified against a Microsoft Learn page) — **explicitly flagged per D-09 for user approval before the planner locks this list.**

## Open Questions

1. **GAMING-01's "service configuration dropdowns" wording vs. D-10's retirement of the Services-preset dropdown**
   - What we know: 02-CONTEXT.md D-10 explicitly drops the AkariOS-Default/Windows-Default Services preset dropdown "not replaced by anything," and REQUIREMENTS.md's GAMING-01 text (written before this discussion) still lists "service configuration dropdowns" as part of the requirement.
   - What's unclear: whether GAMING-01's requirement text itself needs a REQUIREMENTS.md wording update (similar to the GAMING-02 retirement edit already made), or whether it's understood to be satisfied by the SvcHost/Win32Priority dropdowns alone.
   - Recommendation: flag for the user/planner at plan-check time; this research treats GAMING-01 as satisfied by the 11 toggles + 2 dropdowns per CONTEXT.md's more current, more specific decisions (D-09/D-10 supersede the general requirement wording).

2. **`0x06` Win32PrioritySeparation preset semantics**
   - What we know: the predecessor ships this as its 5th preset alongside 4 values this research's bit-decode table confirms (`0x2A`, `0x28`, `0x26`, `0x16`).
   - What's unclear: `0x06`'s exact scheduling behavior wasn't confirmed by this session's WebSearch results (the low nibble below `0x14` wasn't covered by either source).
   - Recommendation: carry `0x06` forward as a "Legacy/Advanced" compatibility entry (predecessor parity) without asserting its semantic label, or drop it if the planner/user prefers not to carry forward an unverified value.

3. **Should `TweakCategory` be an enum on `ITweakHandler`, or should Gaming Tweaks get its own parallel `IGamingTweakCatalog`/`ITweakHandler`-alike interface?**
   - What we know: adding a `Category` property is the smallest additive change (Pattern 1) and keeps one DI registration call site.
   - What's unclear: whether a future phase (Debloat, Misc) will want yet another category, in which case an enum with 2 values today might need a 3rd/4th later — not a blocker, just worth the planner noting the enum will grow.
   - Recommendation: proceed with the enum approach; it's the standard low-cost extensibility pattern and matches this codebase's existing preference for small additive interface changes (e.g., `IWindowsServiceController`'s own doc comment anticipates exactly this kind of future extension).

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `csc.exe` (.NET Framework 4.x SxS, `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\`) | Timer Resolution toggle (On branch compiles a service at runtime) | Not verified this session (requires target-machine probe; Windows 10/11 ship .NET Framework 4.8+ by default, this SxS path is historically stable but not guaranteed on all SKUs) | — | `GetState()`/pre-flight should probe `File.Exists` and surface a clear log message (Pitfall 3) rather than fail silently like the source script does |
| `winget.exe` | `4 Nvidia Settings.ps1`'s NVIDIA Control Panel install step (`winget install "9NF8H0H7WMLT"`) | Ships with Windows 10 1809+/Windows 11 by default via App Installer; not independently verified on this dev machine this session | — | Script already wraps the call in `try {} catch {}` — safe to keep as best-effort; if missing, NVIDIA Control Panel install is silently skipped (matches source script's own tolerance) |
| PowerShell `NetAdapterBinding`/`PnpDevice` cmdlet modules (`Get-PnpDevice`, `Disable-NetAdapterBinding`) | Msi Mode toggle (Pattern 4), Network IPv4 Only toggle | In-box on Windows 10/11 (built-in modules, no separate install) | — | None needed — always present on the supported OS versions per PROJECT.md's constraints |
| `github.com/FR33THYFR33THY/Ultimate-Files` reachability | 5 of 6 D-06 network scripts | Internet-dependent, third-party GitHub-hosted mirror (not Microsoft/vendor-owned) — availability not verified this session | — | Scripts already self-check connectivity (`Test-Connection 8.8.8.8`) and exit cleanly if offline; no additional fallback recommended for v1 per D-06 |

**Missing dependencies with no fallback:** none — every dependency above already has an existing tolerance mechanism in the source scripts (try/catch, connectivity pre-check) that the C# port should preserve.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | This page has no user-authentication surface (elevation, not auth) |
| V3 Session Management | No | N/A |
| V4 Access Control | Yes | All writes require the process's existing `requireAdministrator` elevation (APP-01) — no new access-control surface introduced by this phase |
| V5 Input Validation | Yes | Dropdown selections must be validated against the fixed preset index list (never write an arbitrary user-typed value to `SvcHostSplitThresholdInKB`/`Win32PrioritySeparation`) — mirrors the predecessor's own `if (value < 0 || value >= Values.Length) return;` guard `[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/GamingTweaksViewModel.cs:29-30,56-57]` |
| V6 Cryptography | No | No cryptographic operations in this phase (D-06 explicitly declines SHA256 download verification for v1 — an accepted, documented risk, not silently dropped) |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Tampered/malicious binary from `github.com/FR33THYFR33THY/Ultimate-Files` executed with admin rights (D-06) | Tampering | **Explicitly accepted risk for v1 per D-06** — no mitigation added this phase; PROJECT.md's threat model already flags this class as the app's top-severity risk, tracked for a future hardening pass, not blocking this phase |
| Registry-squatting on gaming-tweak keys (a malicious process pre-creating a key this app expects to own) | Spoofing/Tampering | Reuse `IRegistryService`'s existing open-then-create (never blind-create) convention for every new write — already the established Phase 1 pattern, no new work needed beyond following it |
| Runtime C# compilation (Timer Resolution) executing attacker-modified source if the embedded string were ever externally influenced | Tampering | The C# source is a fixed string literal in the ported script/handler, never built from user input or a downloaded file — no injection surface as long as the port keeps it a compile-time constant (do not parameterize any part of the compiled source from user input) |
| Command injection via dynamically-built `reg add`/`powercfg` arguments (e.g., GPU `InstanceId` strings interpolated into a registry path) | Tampering | `InstanceId` values come from `Get-PnpDevice`, not free user text, but should still be validated as a well-formed device-instance-path pattern before interpolation into any `Process.Start` argument string, consistent with not trusting external command input even when the immediate source is "the OS" |

## Sources

### Primary (HIGH confidence)
- 19 canonical PowerShell scripts under `C:\Users\isleap\Desktop\AkariOS Tweaks\5 Graphics\` and `\6 Windows\` — read in full this session (line ranges cited throughout)
- `src/AkariToolbox.App/Services/{ITweakHandler,ITweakCatalog,TweakCatalog,TweakHandlerRegistration}.cs` — read in full this session
- `src/AkariToolbox.App/Services/TweakHandlers/{WifiTweakHandler,RegistryTweaksBatchA,RegistryTweaksBatchB,ServiceBackedTweaks,DefenderTweakHandler}.cs` — read/grepped this session
- `src/AkariToolbox.Framework/Services/{IRegistryService,IScriptRunner,ScriptRunner,IWindowsServiceController,WindowsServiceController}.cs` — read in full this session
- `src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs` — read in full this session (source of the "exactly 32 handlers" regression test)
- `src/AkariToolbox.App/ViewModels/{AkariOSTweaksViewModel,HomeViewModel}.cs`, `src/AkariToolbox.App/Views/AkariOSTweaksPage.xaml.cs`, `src/AkariToolbox.App/MainWindow.xaml.cs` — read/grepped this session
- `C:\Users\isleap\Documents\GitHub\AkariOS-Companion\ViewModels\GamingTweaksViewModel.cs`, `Services\ToolService.cs` — read this session (predecessor reference, superseded per D-02)
- `.planning/{REQUIREMENTS.md,STATE.md,config.json}`, `.planning/phases/02-gaming-tweaks/02-CONTEXT.md` — read this session

### Secondary (MEDIUM confidence)
- None cited independently this session beyond the cross-checked WebSearch results below (both fall to LOW/tertiary since no Microsoft Learn or vendor page confirmed them)

### Tertiary (LOW confidence)
- WebSearch: SvcHostSplitThresholdInKB recommended values (community forum/blog sources, no confirmed Microsoft Learn page) — underlies the SvcHost preset proposal, flagged A1/A3
- WebSearch: Win32PrioritySeparation hex-value bit-decode table (community forum/blog sources, cross-checked across 2 searches) — underlies the Win32Priority preset proposal, flagged A2

## Metadata

**Confidence breakdown:**
- Toggle mapping (11 stateful handlers + 2 shortcuts + 6 network actions): HIGH — every value read directly from the source scripts this session, cross-referenced against the existing `ITweakHandler` codebase pattern
- Architecture (catalog-filtering gap, new `IRegistryService`/`IScriptRunner` members): HIGH — verified by reading the actual interfaces, DI registration, and the failing-test-in-waiting directly
- SvcHost/Win32PrioritySeparation expanded preset values: LOW — community-sourced, explicitly flagged for user approval per D-09, not locked by this research

**Research date:** 2026-09-01
**Valid until:** stable — this phase's source material (local PowerShell scripts, in-repo code) does not go stale on a calendar basis; re-verify only if the 19 canonical scripts or the `ITweakHandler`/`ITweakCatalog` interfaces change before planning executes
