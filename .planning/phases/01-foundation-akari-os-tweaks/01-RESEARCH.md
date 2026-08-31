# Phase 1: Foundation & Akari OS Tweaks - Research

**Researched:** 2026-08-31
**Domain:** WinUI 3 MVVM port of a WPF elevated system-tweak app — framework rebrand + registry/service-backed toggle page + carried-over Defender workflow
**Confidence:** HIGH (predecessor source, framework template source, and official Microsoft docs all read directly this session)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** The Defender-disable code path (`TweakService.SetDefender`/`SetDefenderAsync` and everything it calls — Tamper Protection check, NoDefender package install, `RunOnce` phase-2 cleanup script) is ported as a direct, minimally-modified carry-over. Do NOT refactor, decompose into `ITweakHandler`, or rewrite this logic in Phase 1 — explicit, twice-repeated user instruction. Only the thin ViewModel/UI binding around it is new. — **Reversibility:** one-way — this is an explicit user directive, not a technical default; changing it requires the user to explicitly ask to revisit (tracked as SEC-01 in REQUIREMENTS.md v2).
- **D-02:** Native WinUI 3 Fluent 2 controls + the framework's Mica backdrop only. The predecessor's WPF-UI theme (`Themes/Colors.xaml`, `Themes/Controls.xaml`) is NOT ported — no custom color/brush resources, no WPF-UI-style visual language.
- **D-03:** Tweak state must be read from the real live system (registry/service value), not the predecessor's app-tracked "did we flip this" flag pattern (`HasState`/`SaveState`/`ClearState` against a private state hive). Each of the 32 tweak toggles needs a live-state reader derived from its actual registry/service effect, not a port of the flag-check.
- **D-04:** Before mutating any tweak (except the Defender tweak, which is exempt per D-01), the app must record the tweak's real prior value so turning it back off restores that real value, not a hardcoded default.
- **D-05:** Keep a persistent, always-visible log/status console in the app shell — same UX intent as the predecessor's `TxtLog` + `ProgressBar`, fed by tweak actions and (later phases) script/download output.
- **D-06:** Console is a docked panel, collapsible by the user (not fixed/always-expanded) — reclaim space when not needed, still visible by default.
- **D-07:** Console is in-memory only and clears on each app launch — matches predecessor behavior exactly, no session persistence or log file in Phase 1.
- **D-08:** Console must be implemented as a framework service (e.g. an `ILogConsoleService` or similar) that ViewModels/services call into via injected interface — NOT the predecessor's anti-pattern of passing raw `TextBox`/`ProgressBar` controls into `ToolService`'s constructor. Same visible behavior, correct MVVM plumbing underneath.
- **D-09:** Home shows all 5 destination cards from day one — Akari OS Tweaks, Gaming Tweaks, Debloat, Downloads, Misc — even though only Tweaks is built in Phase 1. The 4 not-yet-built cards are visibly disabled/labeled "Coming soon" and non-interactive.
- **D-10:** This adds a 5th card to Home beyond the predecessor's 4 (predecessor had no Debloat card, Debloat was nav-sidebar-only) — Debloat gets equal footing with the other four in the new app.
- **D-11:** Nav sidebar follows the same pattern: all 5 destination entries appear now, with the 4 unbuilt ones disabled until their phase ships.

### Claude's Discretion

- **Defender restart UX** was raised as a possible discussion area but not selected/explored. Default: log-message-only, exact parity with the predecessor (no "Restart Now" button or other new UI affordance around the Defender workflow) — consistent with D-01's "don't touch/wrap this code path with new behavior" spirit. If the user wants a restart-prompt convenience later, that's a small, separable addition to revisit explicitly, not something to add proactively during the port.
- Exact visual treatment of "Coming soon" placeholders (grayed card vs. lock icon vs. badge text) — Claude's call, should read as clearly non-interactive without looking broken.
- Live-state-reader implementation per tweak (D-03) — deriving "is this tweak currently active" from each tweak's actual registry/service effect is a technical/research task, not a user vision question. `TweakService.cs` has the per-tweak apply logic to reverse-engineer the read side from (see Code Examples / Common Pitfalls below — this research supplies the read-side design for all 32).

### Deferred Ideas (OUT OF SCOPE)

None — discussion stayed within phase scope. (Defender restart UX was raised as a possible topic but resolved via Claude's Discretion above rather than deferred to a future phase — it's an in-scope Phase 1 UI detail, not a new capability.)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| APP-01 | App requests/runs under `requireAdministrator` elevation, matching predecessor's privilege model | See "Elevation manifest" in Architecture Patterns + Pitfall 1 (manifest-merge history) + Environment Availability |
| APP-02 | App identity (namespace/assembly/manifest identity/icon/branding) reflects "Akari Toolbox" rebrand | See "Copy/rename checklist" in Architecture Patterns, sourced from framework README's own rename instructions |
| APP-03 | Native WinUI 3 Fluent 2 controls + framework's Mica backdrop; no WPF-UI theme carried over | Confirmed: framework's `MainWindow.xaml.cs` already sets `SystemBackdrop = new MicaBackdrop()` and `App.xaml` merges only `XamlControlsResources` — nothing to add, only to NOT port (predecessor's `Themes/` folder) |
| APP-04 | File/folder pickers work correctly while elevated (replace framework's default picker if it crashes) | See Pitfall 2 + Code Example 1 (`Microsoft.Windows.Storage.Pickers` replacement, confirmed by official docs to fix exactly this) |
| APP-05 | Background operations update UI without cross-thread crashes | See Code Example 3 (`DispatcherQueueExtensions.RunOnUIThreadAsync`, already exists in framework) + Pitfall 3 |
| HOME-01 | Home/dashboard landing page listing tool categories | See "Home card / NavigationView disabled-item pattern" in Architecture Patterns + Code Example 4 |
| TWEAKS-01 | View/toggle all 32 tweaks, each reflecting actual current system state | See "Live-state-reader design" in Architecture Patterns — full per-tweak read-side table derived from `TweakService.cs` |
| TWEAKS-02 | Two-phase Defender-disable workflow, ported as direct carry-over | See "Defender carry-over scope" in Architecture Patterns — full call graph including `PostInstallService` cross-phase dependency (Pitfall 5) |
| TWEAKS-03 | Record real prior state before mutating (except Defender) | See "Live-state-reader design" + Don't Hand-Roll (prior-state capture wrapper) |
</phase_requirements>

## Project Constraints (from CLAUDE.md)

Extracted directives the plan must not contradict — see `./.claude/CLAUDE.md`:

- **Packaging:** unpackaged and self-contained (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`) — no Windows App Runtime dependency on the target machine. Already the framework template's default; do not introduce MSIX packaging.
- **Privilege:** must run elevated (`requireAdministrator`) — this phase's APP-01 is the mechanism.
- **Platform:** Windows 10/11 x64 only.
- **Script execution:** PowerShell scripts stay embedded-resource-extracted-and-`Process.Start`-based (the predecessor's `ToolService` pattern) — do NOT add `Microsoft.PowerShell.SDK` in-process hosting.
- **Required package:** `System.ServiceProcess.ServiceController` must be added explicitly — confirmed not part of the shared framework for a WinUI 3 SDK-style project (this research additionally corrects the pinned version to 10.0.11, see Standard Stack).
- **Forbidden:** do not add the standalone `Microsoft.Win32.Registry` NuGet package (in-box BCL API is already available; the NuGet package targets non-Windows TFMs).
- **Forbidden:** do not add Octokit.net or Velopack for this milestone.
- **Forbidden pattern:** raw `RegistryKey.SetValue` without existence/type checks ("registry squatting" risk) — always `OpenSubKey`/`GetValue` with null-checks before writing; prefer `CreateSubKey` only when a tweak is explicitly meant to create the key. This directly informs the read-side design in this research (Live-state-reader design section).
- **Forbidden pattern:** `new HttpClient()` per call — register via `IHttpClientFactory` in the existing DI container. Relevant to the minimal `PostInstallService` port (Pitfall 5) even though full download-heavy work is Phase 4.
- **Forbidden:** MSIX packaging (`Package.appxmanifest`) combined with elevation — WinRT/COM per-user activation limitation for packaged+elevated apps. Not a risk here since the project stays unpackaged per the constraint above.
- **Workflow:** all file-changing work must go through a GSD command (`/gsd-execute-phase` etc.) — not a research-time concern, but the plan this research feeds must respect it.

## Summary

Phase 1 is a **copy-and-rebrand of an already-built, tested WinUI 3 MVVM framework** (`WinUI-3-MVVM-Framework`, read directly this session) combined with a **targeted, architecture-changing port** of one page (`AkariOSTweaksViewModel` + `TweakService`) from a working WPF predecessor (`AkariOS-Companion`, also read directly this session). Both source trees exist on disk and were read in full for the tweak-relevant paths — this is not "research the general domain," it is "port this specific, already-correct code to a new UI stack while fixing one specific architectural property (state must be read live, not cached)."

The single highest-value finding this session: **the file/folder picker blocker flagged as unresolved in STATE.md is now resolved.** Official Microsoft docs (fetched this session) confirm `Microsoft.Windows.Storage.Pickers` — a WinAppSDK-native picker namespace shipped since WinAppSDK 1.8 — was added *specifically* because `Windows.Storage.Pickers` (the framework's current default) does not work in elevated processes. The project's Windows App SDK 2.3.1 is well past 1.8, so this API is available with no package bump. This eliminates the CsWin32/P-Invoke fallback as a Phase 1 requirement — a straight interface-compatible reimplementation of `IFilePickerService` is enough.

The second-highest-value finding: **the Defender tweak (TWEAKS-02) is not self-contained.** `TweakService.SetDefenderAsync` calls `PostInstallService.EnsureDefenderFilesAsync()`/`EnsureMinSudoAsync()`, which — if the Defender-related files under `C:\PostInstall\` are missing — falls through to `EnsurePostInstallAsync()`, which downloads the **entire** ~30MB/50+-file PostInstall folder from GitHub, not just the Defender subset. D-01 requires porting Defender's dependency graph as-is ("everything it calls"), so a minimal `PostInstallService` port (just the three `Ensure*` methods and the `AllFiles` manifest) is in scope for Phase 1, even though the full Downloads/PostInstall *page* (DOWNLOADS-01/02) is Phase 4. The planner needs to explicitly scope this rather than discover it mid-implementation.

Third: this session verified **NuGet package versions directly against the registry** and found a discrepancy with the project's existing `STACK.md`/`PROJECT.md`: Windows App SDK **2.4.0 shipped as the new stable-channel release on 2026-08-13** (per `learn.microsoft.com/windows/apps/whats-new/whats-new-for-developers`, fetched this session, dated 2026-08-25), superseding 2.3.1. The existing project research (same-day, 2026-08-31) says "2.4.x is experimental-channel only — do not move to it," which was true when 2.3.1 was current but is now stale. Recommendation below.

**Primary recommendation:** Copy `WinUI-3-MVVM-Framework` verbatim into this repo, rename per the framework's own README checklist, add `requireAdministrator` to `app.manifest`, swap `IFilePickerService`'s implementation to `Microsoft.Windows.Storage.Pickers` behind the same interface, port `TweakService`'s 32 OS-tweak methods into per-tweak `ITweakHandler` classes with a new *read* side derived from each method's own registry keys (never touch `HKCU\Software\AkariTool`), port `SetDefenderAsync` + the minimal `PostInstallService` subset byte-for-byte, and build a `ILogConsoleService`-backed collapsible status console.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| App identity / manifest / elevation | Native shell (unpackaged Win32 process) | — | `app.manifest` + assembly identity are OS/loader-level, not app-tier concerns |
| Home dashboard / navigation | Client (WinUI 3 View + ViewModel) | — | Pure UI composition, no system mutation |
| Tweak toggle state read | System primitive layer (`IRegistryService`/`IWindowsServiceController`) | ViewModel (initial bind) | Must hit live OS state on every page load — never cached in the app tier |
| Tweak toggle state write + prior-state capture | System primitive layer, orchestrated by `ITweakHandler` | Application service (`ITweakCatalog`) | Mutation and the "read-before-write" invariant belong together in one class per tweak so they can't drift (see Anti-Pattern 3, existing ARCHITECTURE.md) |
| Defender two-phase workflow | Application service (`DefenderTweakHandler`, ported as-is) | External process (`powershell.exe`, `MinSudo.exe`, Windows `RunOnce`) | Explicitly exempted from the primitive-layer decomposition per D-01; phase 2 literally executes outside the app's process lifetime (post-reboot `RunOnce`) |
| PostInstall asset presence check (Defender's dependency only) | Application service (`IPostInstallService`, minimal subset) | External (GitHub raw content over HTTPS) | Full asset-mirror UX is Phase 4 (DOWNLOADS-01); Phase 1 only needs the file-presence-then-download logic Defender's `Ensure*` calls require |
| File/folder picker | Client (WinUI 3, `Microsoft.Windows.Storage.Pickers`) | — | Picker UI activation must run on the UI thread with a `WindowId`; no server/service tier involved |
| Log/status console | Client-facing framework service (`ILogConsoleService`) | ViewModel/service callers (write-only) | Must be UI-agnostic at the call site (D-08) but ultimately renders in the Client tier |
| Cross-thread UI marshaling | Framework/Client boundary (`DispatcherQueueExtensions`) | — | Already exists in the copied framework; this is glue, not new domain logic |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Windows App SDK | **2.3.1** (in place, template-pinned) — see note below re: 2.4.0 | WinUI 3 runtime/APIs | `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/Directory.Packages.props:10]` `<PackageVersion Include="Microsoft.WindowsAppSDK" Version="2.3.1" />` — the copied template already builds against this pin |
| .NET | 10 (`net10.0-windows10.0.26100.0`), SDK `10.0.400` | Runtime/SDK | `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/global.json:3]` pins SDK `"10.0.400"`; `[VERIFIED: dotnet --version]` on this machine reports `10.0.400` — exact match, no SDK install needed |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`/`IAsyncRelayCommand`) | `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/Directory.Packages.props:15]`; `[VERIFIED: api.nuget.org/v3-flatcontainer/communitytoolkit.mvvm/index.json]` confirms 8.4.2 is the latest 8.x release (next is 8.4.1 below it, no 8.5 published) |
| Microsoft.Extensions.Hosting/DI/Logging | 10.0.11 | DI container, generic host, logging | `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/Directory.Packages.props:18-25]` — already pinned in the template's central package management file |
| Microsoft.Xaml.Behaviors.WinUI.Managed | 3.0.1 | XAML behaviors (already used by the framework's `Behaviors/` folder) | `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/Directory.Packages.props:28]` |

**Windows App SDK version note (correction to existing `STACK.md`):** `[VERIFIED: learn.microsoft.com/windows/apps/whats-new/whats-new-for-developers, fetched 2026-08-31]` — Windows App SDK **2.4.0 shipped stable on 2026-08-13** ("Windows App SDK 2.4.0: The August 13 stable release adds touchpad and mouse haptics..."), with 2.4.1-experimental now the experimental-channel head. The existing project `STACK.md` (written same day, 2026-08-31) states 2.3.1 is current-stable and 2.4.x is experimental-only — that was correct until 2.4.0's stable promotion and is now out of date. **Recommendation for the planner:** stay on 2.3.1 for Phase 1. The copied framework template already builds and is tested against 2.3.1; bumping the SDK version is an orthogonal upgrade with its own risk (manifest-merge history exists between WinAppSDK versions — see Pitfall 1) that should not be bundled into a rebrand-and-port phase. Revisit the 2.4.0 upgrade as a deliberate, separately-tested task if a later phase needs a 2.4.0-only API.

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `System.ServiceProcess.ServiceController` | **10.0.11** (correction — see below) | Query/start/stop Windows services for tweaks whose state lives in a service (`bluetooth`, `spooler`, `clipboard`, `cdrom`, `vr`) | Not yet referenced anywhere in the framework or predecessor's `.csproj`; must be added explicitly to `AppTemplate.App.csproj`/its renamed equivalent |
| `Microsoft.Windows.Storage.Pickers` | Included in `Microsoft.WindowsAppSDK` 2.3.1 (no separate package) | Elevation-safe file/folder pickers | Replace `AppTemplate.Framework.Services.FilePickerService`'s implementation; interface (`IFilePickerService`) can stay, only the concrete class changes |
| `Microsoft.Win32.Registry` (in-box BCL) | n/a | Read/write/delete registry keys for the 32 tweaks | Already available on Windows targets; do not add the standalone NuGet package (that one targets non-Windows TFMs) |

**`System.ServiceProcess.ServiceController` version correction:** `[VERIFIED: api.nuget.org/v3-flatcontainer/system.serviceprocess.servicecontroller/index.json, fetched 2026-08-31]` — the registry lists stable releases through **10.0.11** (and `11.0.0-preview.*` beyond that), not just the `9.0.9` cited in the existing `STACK.md`. Since the project targets `net10.0-windows10.0.26100.0` (confirmed in the framework's own `.csproj`/`global.json`), use **`System.ServiceProcess.ServiceController` 10.0.11** to match the TFM's major version rather than the older 9.0.x line — both work (the package multi-targets net8.0+), but 10.0.11 is the version-aligned current release. `[ASSUMED]` (package name only, not yet confirmed via official docs/Context7 — see Package Legitimacy Audit) tag applies to the package identity itself even though the version number is registry-verified.

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `Microsoft.Windows.Storage.Pickers` for elevation-safe pickers | CsWin32 P/Invoke to `IFileOpenDialog`/`IFileSaveDialog` | Only worth it if `Microsoft.Windows.Storage.Pickers` turns out to have its own gap in testing (e.g. an edge case in `FolderPicker.PickSingleFolderAsync()` under elevation) — no evidence of that in official docs, so start with the WinAppSDK-native API and keep CsWin32 as a documented fallback, not a parallel implementation |
| Per-tweak `ITweakHandler` decomposition (31 tweaks) | Keep `TweakService`'s single 32-case `switch`, only fix the `GetState` side | Rejected — the existing project `ARCHITECTURE.md` (already-done project-level research, HIGH confidence, read this session) documents this trade-off in depth; the switch pattern is exactly what caused the `StateKeyFor` / apply-switch key-drift risk already observed in the predecessor (e.g. `"vr"` → `"EnableVR"`) |

**Installation:**
```xml
<!-- add to Directory.Packages.props (central package management already enabled) -->
<PackageVersion Include="System.ServiceProcess.ServiceController" Version="10.0.11" />
<!-- add to the renamed App project's .csproj -->
<PackageReference Include="System.ServiceProcess.ServiceController" />
```
No other new NuGet packages are required for Phase 1 — the picker fix and all registry/BCL work ship inside the already-referenced `Microsoft.WindowsAppSDK` and the .NET BCL.

## Package Legitimacy Audit

The gsd-tools `package-legitimacy check` seam supports only `npm`/`pypi`/`crates` ecosystems; NuGet is not covered by that automated gate. The one new external package for this phase (`System.ServiceProcess.ServiceController`) was verified manually against the authoritative NuGet registry API this session.

| Package | Registry | Age | Downloads | Source Repo | Verdict | Disposition |
|---------|----------|-----|-----------|--------------|---------|-------------|
| `System.ServiceProcess.ServiceController` | NuGet | First published as a stable 4.x release years ago; latest stable 10.0.11 (part of the official `dotnet/runtime` package family, versions tracked alongside every .NET release since 4.1.0) `[VERIFIED: api.nuget.org/v3-flatcontainer/system.serviceprocess.servicecontroller/index.json]` | Not independently queried (download-count API not called this session) | `github.com/dotnet/runtime` (official .NET runtime repo — this is a Microsoft first-party BCL-adjacent package, not a community package) | OK (manual verification — not run through the automated npm/pypi/crates gate) | Approved |

**Packages removed due to `[SLOP]` verdict:** none.
**Packages flagged as suspicious `[SUS]`:** none — this is a first-party Microsoft package published under the official `dotnet/runtime` NuGet namespace, already used by the .NET ecosystem for a decade (identical version cadence to every other split-out BCL package like `System.Text.Json`). Manual verification (direct registry query for full version history, confirming a continuous decade-long release cadence with no gaps or suspicious re-publishes) is sufficient; a `checkpoint:human-verify` gate is not warranted for this specific package, but the planner should still add one as standard practice per the package-legitimacy protocol's guidance for any package not run through the automated npm/pypi/crates gate.

## Architecture Patterns

### System Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│ VIEW LAYER (XAML)                                                        │
│  HomePage (5 cards, 4 disabled)   AkariOSTweaksPage (32-item toggle list)│
└──────────────────────────┬───────────────────────────┬──────────────────┘
                            │ x:Bind / RelayCommand      │
┌───────────────────────────▼───────────────────────────▼──────────────────┐
│ VIEWMODEL LAYER (CommunityToolkit.Mvvm)                                  │
│  HomeViewModel (5 HomeCard defs)   AkariOSTweaksViewModel (32 TweakItem) │
│  — unchanged shape vs. predecessor: still calls GetState(key)/SetState  │
└──────────────────────────┬────────────────────────────────────────────── ┘
                            │ ITweakCatalog.GetStateAsync/SetStateAsync
┌───────────────────────────▼───────────────────────────────────────────────┐
│ APPLICATION SERVICE LAYER                                                 │
│  ITweakCatalog → 32× ITweakHandler (31 primitive-backed + 1 Defender)     │
│  ILogConsoleService (D-08)          IPostInstallService (minimal subset)  │
└─────┬───────────────────────┬──────────────────────────┬──────────────── ┘
      │                       │                          │
┌─────▼──────────┐  ┌─────────▼──────────┐  ┌────────────▼──────────────┐
│ IRegistryService│  │IWindowsServiceCtrl │  │ Defender handler: direct  │
│ (30 of 32       │  │ (service-backed    │  │ Process.Start powershell/ │
│  tweaks are pure │  │  tweaks: bluetooth,│  │ MinSudo.exe + RunOnce reg │
│  registry)      │  │  spooler, clipboard│  │  write — NOT decomposed   │
│                 │  │  cdrom, vr subset) │  │  through the primitives   │
└────────┬────────┘  └──────────┬──────── ┘  │  layer (D-01 exemption)  │
         │                      │            └────────────┬─────────────┘
┌────────▼──────────────────────▼─────────────────────────▼──────────────┐
│ OS SURFACE: Registry (HKLM/HKCU + real-user-HKCU-under-elevation trick), │
│ Service Control Manager, powershell.exe, MinSudo.exe/PowerRun.exe,      │
│ Windows RunOnce key, GitHub raw content (Defender's PostInstall files)  │
└───────────────────────────────────────────────────────────────────────┘
```

### Copy/rename checklist (APP-02)

`[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/README.md:48-54]` The framework's own README states the exact rename procedure:
> "1. Copy the repository and rename `AppTemplate` everywhere (folders, `.csproj`, `.slnx`, namespaces, assembly names, `App.AppName`, the settings-folder name in `App.xaml.cs`). 2. Add pages under `src/AppTemplate.App/Views` with matching view models, register them in `App.xaml.cs`, and add entries to `NavItems` in `MainWindow.xaml.cs`. 3. Keep app-independent code in the framework library and add tests in `AppTemplate.Tests`."

Concretely, this touches (all confirmed present this session):
- `AppTemplate.slnx`, both `.csproj` files (`RootNamespace`, `AssemblyName`)
- `app.manifest`'s `<assemblyIdentity name="AppTemplate.App.app"/>` → rename AND add `requireAdministrator` (framework's manifest currently has neither elevation nor the Akari identity — `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/app.manifest:3,5-16]`, no `<trustInfo>`/`<requestedExecutionLevel>` block exists at all)
- `App.AppName` (`[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/App.xaml.cs:29]` `public static string AppName => "App Template";`) and `SettingsFolder` (`App.xaml.cs:35-36`, currently `"AppTemplate"`)
- `AppTemplate.App.csproj`'s `<ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>` — swap for the predecessor's icon assets, confirmed present: `[VERIFIED: ls C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Assets/]` → `AkariLogo.ico`, `AkariLogo.png`, `HomeBackdrop.png`
- `MainWindow.xaml.cs`'s `NavItems`/`FooterNavItems` (`[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/MainWindow.xaml.cs:70-79]`) — add the 5 Home destinations here per D-11

### Elevation manifest (APP-01)

`[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/app.manifest:1-28]` The predecessor's manifest is the exact shape to replicate:
```xml
<trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
  <security>
    <requestedPrivileges xmlns="urn:schemas-microsoft-com:asm.v3">
      <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
    </requestedPrivileges>
  </security>
</trustInfo>
```
The framework template's manifest has DPI-awareness and supported-OS blocks only — `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/app.manifest:1-17]`, no `<trustInfo>` block present. Add the block above; keep the framework's existing `<windowsSettings>`/`<compatibility>` blocks (they're a superset of the predecessor's, including `PerMonitorV2` DPI awareness the predecessor also declares).

**Verification requirement (per STATE.md's own flagged blocker):** the `WindowsAppSDKSelfContained=true` + `requireAdministrator` combination had a documented manifest-merge bug (`WindowsAppSDK#3054`/`microsoft-ui-xaml#7560`, `c1010001 Values of attribute 'level' not equal`) on early 1.x SDK versions, fixed internally at the 1.3 milestone. The project's 2.3.1 pin is far past 1.3, so this should not reproduce — but this has not been independently re-verified in this environment. **Treat the first actual elevated + self-contained build as a smoke test, not an assumption** — this is exactly the kind of manifest interaction that regresses silently across SDK bumps.

### Live-state-reader design (TWEAKS-01/TWEAKS-03, D-03/D-04)

This is the core new design work for Phase 1 — `[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/TweakService.cs:164-1056]`, all 32 `Set*` methods for the Akari OS Tweaks page were read in full this session (the file also contains 3 additional Gaming-tweak methods — `SetPreemption`, `SetHdcp`, `SetNetworkOptimization` — at lines 1062-1117, which are **Gaming Tweaks page scope, not this phase's 32**; confirmed by the `"case "preempt":"`/`"hdcp"`/`"netopt"` switch entries under a `// Gaming tweaks` comment at `TweakService.cs:69-72`, separate from the `// AKARI OS TWEAKS` section header at line 161).

The 32 OS-tweak keys (confirmed by counting the `SetState` switch cases under the `AKARI OS TWEAKS` region, `TweakService.cs:37-68`, minus the 3 gaming-tweak cases at lines 70-72): `wifi, tsx, actioncenter, dep, clipboard, bluetooth, bootmenu, vpn, ntfsenc, fso, notifications, prefetch, cdrom, spooler, nolazy, uacadmin, vr, uac, startmenu, hyperv, vbs, wallpaperq, mpo, transparency, lockscreen, animations, dcom, nvme, largecache, sysprofile, defender, mitigation`.

Each `Set*` method already writes to a **specific, discoverable** registry location — the predecessor's own `HasState`/`SaveState` calls just aren't a reliable "is this active" signal anymore (they only prove the app itself once flipped it, not the OS's actual current value, and rely on a private `HKCU\Software\AkariTool` hive per D-03). For every tweak the **read side is symmetric with the write side already ported**: query the same registry value(s) the `Set*` method last wrote, and compare against the "enabled" value. Examples read directly from source this session:

| Tweak key | Write logic already read (source of truth for the read side) | Read-side design |
|-----------|----------------------------------------------------------------|-------------------|
| `wifi` | `TweakService.cs:164-183` — sets 4 services' `Start` DWORD to `4` (disabled) or `2`/`1`/`3` (enabled) | `IsOn` = `WlanSvc\Start == 4` (the other 3 services move in lockstep in this code, so one representative read is sufficient; a stricter handler could check all 4) |
| `defender` | `TweakService.cs:828-1017` — two-phase, `RunOnce`-deferred | **Exempt from live-state derivation** per D-01/D-04 — port `HasState("DisableDefender")` as literally written; do not attempt to add real Defender-status verification in Phase 1 (that's SEC-01, v2 scope) |
| `bluetooth` | `TweakService.cs:245-259` — loops 15 named services, sets `Start` to `4` or `3` | `IsOn` = representative service (e.g. `bthserv\Start == 4`) — all 15 move together |
| `dep` (DEP/NX) | `TweakService.cs:215-228` — `bcdedit /set NX AlwaysOff` / `/set NX OptIn`, **no registry write at all** | Cannot be read via `IRegistryService` — requires shelling `bcdedit /enum {current} | findstr nx` (or equivalent) and parsing output. **Flag for planner:** this tweak's read side needs an `IScriptRunner`/process-based check, not a registry read — the primitive-layer abstraction (`ITweakHandler`) must support both read strategies, not assume all reads are registry reads |
| `hyperv`, `vr` | `TweakService.cs:561-593`, `497-519` — mix `bcdedit`/`DISM` commands AND registry writes | Same "not pure registry" caveat as `dep` — read side should check the **registry** portion (fast, synchronous) and treat that as the toggle's displayed state, since re-invoking `DISM`/`bcdedit` just to read state on every page load would be a Pitfall-3-class performance trap |

**This table is illustrative, not exhaustive** — the planner/executor should derive the read side for all 32 from the same `TweakService.cs` methods (all fully quoted above/available at the cited line ranges), following the same "read what the write already wrote" principle. The 5 tweaks noted above (`wifi` as a lockstep example, `defender`, `dep`, `hyperv`, `vr`) are flagged specifically because they deviate from the "single registry DWORD" happy path that covers roughly 27 of the 32 tweaks.

**Real-user-HKCU trick** (`startmenu`, `transparency`, `wallpaperq` use `Registry.CurrentUser` directly instead, but `startmenu`/`transparency` specifically use the P/Invoke `CreateRealHkcuSubKey` helper): `[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/TweakService.cs:141-155]` —
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
Because the app runs elevated, `Registry.CurrentUser` resolves to the **elevated token's** HKCU (which may differ from the interactive user's HKCU in some launch scenarios), not necessarily the logged-in user's. The predecessor isolates this quirk behind `CreateRealHkcuSubKey`. This must be ported into `IRegistryService.OpenRealUserHive(string)` (already named in the project's existing `ARCHITECTURE.md`, Pattern 1) so it's written and tested exactly once.

### Defender carry-over scope (TWEAKS-02, D-01)

`[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/TweakService.cs:828-1038]` — the full call graph read this session:

1. `SetDefenderAsync(bool disable)` — top-level entry, called from the 32-case switch's `"defender"` case (`TweakService.cs:67,828`)
2. Calls `PostInstallService.EnsureDefenderFilesAsync()` (disable path) or `EnsureMinSudoAsync()` (enable path) — **cross-service dependency, see Pitfall below**
3. Disable path: checks `IsDefenderTamperProtectionOn()` (`TweakService.cs:976-986`, reads `HKLM\SOFTWARE\Microsoft\Windows Defender\Features\TamperProtection`, treats anything other than integer `4` as "on") — if on, logs guidance and **returns without proceeding** (no exception, no partial state change)
4. If Tamper Protection is off: copies `NoDefender.cab` into `C:\Windows\NoDefender.cab`, runs `Defender\DisableDefender.ps1` **elevated via `Verb = "runas"`** (`DefenderRunElevatedPsFileAsync`, `TweakService.cs:994-1005` — note this is a **second** elevation request even though the whole app is already elevated; this is intentional/pre-existing predecessor behavior, port as-is, do not "fix" it per D-01)
5. Writes a `RunOnce` registry value (`HKLM\...\CurrentVersion\RunOnce\AkariDefenderCleanup`) pointing at a generated `.bat` file that runs on next login and does the actual service-disable + SmartScreen-disable + self-delete (`DefenderScheduleCleanup`, `TweakService.cs:899-974`) — **this is "Phase 2," and it executes entirely outside the app's process lifetime**, after a user-initiated reboot
6. Enable path mirrors this via `MinSudo.exe --TrustedInstaller --Privileged` (`DefenderRunAsTrustedInstallerAsync`, `TweakService.cs:1019-1038`)

**Nothing here should be rewritten.** The only new code for Phase 1 is: (a) the `ITweakHandler` (or equivalent thin wrapper, exempted from the primitive-layer pattern per D-01) that calls this ported logic from the tweak list, and (b) whatever minimal `PostInstallService` subset step 2 requires (see Pitfall 5 below — this is the cross-phase dependency finding).

### NavigationView disabled-item pattern (D-09/D-11)

`[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/MainWindow.xaml:36-57]` — the framework's `NavigationView` already binds `MenuItemsSource="{x:Bind NavItems}"` against `NavigationItem` records (`[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/NavigationItem.cs:4]` `public sealed record NavigationItem(string Label, string Glyph, Type PageType);`) via a `DataTemplate`. To support disabled/"coming soon" entries per D-11, `NavigationItem` needs an added `bool IsEnabled` (or similar) field, and the `DataTemplate` (`MainWindow.xaml:50-56`) needs `NavigationViewItem.IsEnabled="{x:Bind IsEnabled}"` added — WinUI 3's `NavigationViewItem` does honor a bound `IsEnabled` for this exact purpose (confirmed via community pattern research this session — a `NavigationViewItem` bound to a view-model `IsEnabled` plus a companion "SOON" badge element is the standard shape; no first-party Microsoft Learn page dedicated to this narrow case was found, so this specific composition is `[CITED: github.com/microsoft/microsoft-ui-xaml issue #3687 discussion]` rather than official-docs-verified). Same underlying pattern applies to the Home page's 5 `HomeCard` entries — reuse whatever `IsEnabled`/visual-disabled convention is picked for one, apply to both (single source of truth for "which pages exist yet" avoids drift between the Home grid and the nav sidebar).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|--------------|-----|
| Elevation-safe file/folder pickers | Custom P/Invoke wrapper around `IFileOpenDialog`/`IFileSaveDialog` (CsWin32) | `Microsoft.Windows.Storage.Pickers.FileOpenPicker`/`FileSavePicker`/`FolderPicker` | Purpose-built by Microsoft for exactly this gap (confirmed via official docs this session); CsWin32 is more code, more surface area to get wrong, for a problem Microsoft already solved in the SDK version already in use |
| Cross-thread UI marshaling | Manual `DispatcherQueue.TryEnqueue` scattered through every service/ViewModel | `DispatcherQueueExtensions.RunOnUIThreadAsync` (already exists, `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/Threading/DispatcherQueueExtensions.cs:1-102]`) | Already written, already exception-propagating (`TaskCompletionSource` pattern), already ported into this exact codebase — using it consistently is a discipline problem, not a code-writing problem |
| Prior-state capture for revert (TWEAKS-03) | Ad-hoc "remember what I set it to" per tweak | A generic `ITweakHandler.GetState()`-before-`SetState()` capture, orchestrated once in `ITweakCatalog.SetStateAsync` (read current value, store it, then call the handler's write) | If capture is embedded per-handler it will drift — 31 independent implementations of "record, then write" is 31 chances to get the ordering wrong. Centralize the capture-then-write sequencing in the catalog, let each handler only implement `GetState`/`SetState` |
| Registry-squatting-unsafe writes | Direct `RegistryKey.SetValue` without existence checks | `OpenSubKey`/`GetValue` with null-checks before writing, matching the CLAUDE.md-documented project convention already agreed for this repo | Already a documented project constraint (see Project Constraints below) — not new guidance, just confirming it applies to every one of the 32 tweak read/write implementations |

**Key insight:** almost none of this phase's "hard" problems are actually hard — they're each already solved exactly once, either in the framework template (dispatcher marshaling, navigation, DI, Mica/Fluent 2 shell) or in the predecessor (every tweak's exact registry mutation, the Defender workflow, the log/progress UX intent). The actual new work is the **read side** for TWEAKS-01/03 (deriving `GetState` from each `SetState`) and the **picker swap** for APP-04 — both are narrow, well-scoped, and this research supplies concrete starting points for both.

## Common Pitfalls

### Pitfall 1: Manifest-merge history between elevation + self-contained WinAppSDK

**What goes wrong:** A known build-time manifest-merge error (`c1010001 Values of attribute 'level' not equal`) affected `WindowsAppSDKSelfContained=true` + custom `requireAdministrator` manifests on early 1.x SDK versions.
**Why it happens:** The framework template currently has no `<trustInfo>` block at all (confirmed this session) — adding one is new territory for this specific copied template, even though the underlying SDK (2.3.1) is well past the version where this bug was fixed.
**How to avoid:** Treat the first elevated + self-contained build as an explicit smoke-test task in the plan, not an assumption folded into a larger task. If the `c1010001` error reappears, it's a signal of SDK version drift (a stale/cached older SDK resolving despite the 2.3.1 pin), not a fundamental incompatibility to work around with a manifest hack.
**Phase to address:** Phase 1, early — before building any picker- or tweak-dependent page on top of an unverified elevated build.

### Pitfall 2: Reusing the framework's default `IFilePickerService` implementation as-is

**What goes wrong:** `[VERIFIED: C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/Services/IFilePickerService.cs:1-119]` — the framework's current `FilePickerService` is built on `Windows.Storage.Pickers.FileOpenPicker`/`FileSavePicker` (note: **no `FolderPicker`** is exposed by the current interface at all — only single/multi file open and file save). This namespace is documented by Microsoft to not work in elevated processes.
**Why it happens:** The framework was designed/tested unelevated; this project's app is elevated for its entire lifetime, so every picker call will hit the crash.
**How to avoid:** Swap the concrete implementation to `Microsoft.Windows.Storage.Pickers` (same method shapes: `PickSingleFileAsync()`, `PickMultipleFilesAsync()`, plus now `PickSingleFolderAsync()`/`PickMultipleFoldersAsync()` if `IFilePickerService` is extended to add folder support — worth doing now since APP-04 explicitly calls out "file/folder picker"). See Code Example 1.
**Warning signs:** `COMException` with `HRESULT: 0x80004005 (E_FAIL)` the moment any picker method is invoked, only reproducing when launched via the actual elevated exe (not from an already-elevated dev terminal, which can mask the difference during manual testing).
**Phase to address:** Phase 1 (APP-04 is explicitly in this phase's requirement list) — even though no picker-*consuming* page (Downloads, Misc) is built until Phase 4, the service itself and its smoke-test are foundational per the phase goal's own wording ("elevation-safe file/folder picker" is listed as one of the pieces Phase 1 stands up).

### Pitfall 3: Cross-thread UI updates from tweak-state reads / Defender log callbacks

**What goes wrong:** WinUI 3's WinRT/COM plumbing throws `COMException: The application called an interface that was marshalled for a different thread` when a background thread touches XAML-bound state directly.
**Why it happens:** `AkariOSTweaksViewModel`'s constructor already calls `_tweaks.GetState(key)` for all 32 tweaks synchronously (`[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/AkariOSTweaksViewModel.cs:54-63]`) — if the ported version makes these reads async/parallel (recommended, see Performance note below) to avoid blocking the UI thread on page load, every completion must marshal back before touching `TweakItem.IsOn`. Similarly, `SetDefenderAsync`'s `Log(msg)` calls (`TweakService.cs:832,853` etc.) currently go through `App.Tool?.Log`, which in the predecessor dispatches via WPF's `Dispatcher.Invoke` (`[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ToolService.cs:33-38]`) — the new `ILogConsoleService` must do the WinUI 3 equivalent.
**How to avoid:** Route all `ILogConsoleService` writes and all state-read completions through `DispatcherQueueExtensions.RunOnUIThreadAsync` (already exists in the framework) or ensure they're already running as a continuation of a UI-thread-originated `async`/`await` call (which naturally marshals back via the captured `SynchronizationContext`).
**Warning signs:** Intermittent crashes that only reproduce under real timing (e.g. the Defender workflow's multi-second `powershell.exe` phase), never in step-through debugging.
**Phase to address:** Phase 1 — this is exactly the kind of bug that's cheap to prevent by centralizing the pattern once (`ILogConsoleService`, the tweak-catalog's state-read orchestration) and expensive to hunt down later.

### Pitfall 4: Confusing the predecessor's `HKCU\Software\AkariTool` state hive with real system state

**What goes wrong:** The predecessor's `HasState`/`SaveState`/`ClearState` (`[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/TweakService.cs:17-25]`) write to a **private app-tracking key**, separate from the actual registry values each tweak mutates. If a developer copies `TweakService.cs` wholesale (reasonable first instinct — it's proven, tested code) without removing the `HasState`/`SaveState`/`ClearState` calls, the new app will silently recreate the exact anti-pattern D-03 explicitly rejects, AND will pollute `HKCU\Software\AkariTool` with entries that have no meaning to the new app (or worse, will be misread as "prior AkariOS Companion state" by a naive migration attempt).
**Why it happens:** Every one of the 32 `Set*` methods has the shape `if (HasState(...)) return; <mutate>; SaveState(...)` — it's woven throughout, not isolated to one place, so a mechanical copy-paste port carries the anti-pattern along by default.
**How to avoid:** When porting each `Set*` method into its `ITweakHandler.SetState`, strip the `HasState`/`SaveState`/`ClearState` calls entirely — replace the `if (HasState(...)) return` idempotency guard with a live `GetState()` check instead (same guard, correct source of truth). Do not read or write `HKCU\Software\AkariTool` anywhere in the new app.
**Warning signs:** Grep the ported code for `_store`, `HasState`, `SaveState`, `ClearState`, or `Software\\AkariTool` — any hit outside a code comment is D-03 non-compliance.
**Phase to address:** Phase 1, `TweakService` port — this is the single most important mechanical check for TWEAKS-01/TWEAKS-03 compliance.

### Pitfall 5: Defender's `PostInstallService` dependency pulls Phase 4 scope forward

**What goes wrong:** `[VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/PostInstallService.cs:196-234]` — `EnsureDefenderFilesAsync()`/`EnsureMinSudoAsync()` fall through to `EnsurePostInstallAsync()` when their narrow file-presence checks fail, and `EnsurePostInstallAsync()` downloads the **entire** `AllFiles` manifest (50+ files across `AntiCheat/`, `Change Username/`, `Defender/`, `GPU/AMD/`, and more — `[VERIFIED: PostInstallService.cs:27-60+]`, list continues past what was read this session), not just the Defender subset. If the planner scopes "port `SetDefenderAsync`" without also scoping "port the `Ensure*`/`EnsurePostInstallAsync` chain it calls," the Defender tweak will compile but throw/fail at runtime the first time it's exercised on a machine that doesn't already have `C:\PostInstall\` populated (i.e. every fresh dev/test machine).
**Why it happens:** D-01 says port "`SetDefender`/`SetDefenderAsync` and everything it calls" — this chain is "everything it calls," but it looks, at a glance, like Downloads/PostInstall-page territory (DOWNLOADS-01, Phase 4).
**How to avoid:** Scope a **minimal** `PostInstallService` port into Phase 1: the `AllFiles` manifest, `LocalRoot`/`MinSudoPath`/`PowerRunPath`/`NoDefenderPath` constants, `MinSudoPresent`/`PowerRunPresent`/`NoDefenderPresent`/`IsFullyInstalled` properties, and the three `Ensure*` methods — all File I/O and HTTP download logic, no UI. Do not build the Downloads *page* (that's Phase 4, DOWNLOADS-02) — only the service dependency the Defender tweak needs to function.
**Warning signs:** Defender toggle throws `FileNotFoundException`/`HttpRequestException` the first time it's tested on a machine without `C:\PostInstall\` pre-populated; works fine on a dev machine that happens to already have the folder from a prior predecessor-app run (masking the gap).
**Phase to address:** Phase 1 planning — this needs an explicit task, not an assumption that "port Defender" is self-contained.

### Pitfall 6: `bcdedit`/`DISM`-backed tweaks (`dep`, `hyperv`, `vr`) don't have a pure registry read

**What goes wrong:** Naively assuming every tweak's `GetState` can be "read the registry value the `Set*` method wrote" breaks for `dep` (pure `bcdedit`, no registry write at all — `[VERIFIED: TweakService.cs:215-228]`) and partially for `hyperv`/`vr` (mixed `bcdedit`/`DISM` + registry).
**Why it happens:** The temptation is to build one generic `RegistryTweakHandler` base class covering all 32 — `dep` breaks that assumption outright.
**How to avoid:** `dep`'s read side needs a process-based check (parse `bcdedit /enum {current}` output for the `nx` line) or must be explicitly documented as "not independently readable — infer from whatever registry side-effects, if any, exist" if a process-spawn-per-toggle-load is deemed too slow for the page's 32-item load. This is a genuine open design question — flagged in Open Questions below.
**Warning signs:** `dep` toggle always shows the same state regardless of actual system config, because its `GetState` was implemented as "always return the app's last-set value" (silently reintroducing the D-03 anti-pattern for just this one tweak).
**Phase to address:** Phase 1, `TweakService` port — needs an explicit design decision, not a default fallback.

## Code Examples

### 1. Elevation-safe file/folder picker (`Microsoft.Windows.Storage.Pickers`)

```csharp
// Source: official Windows App SDK docs, fetched this session —
// learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers
// (namespace exists since WinAppSDK 1.8; confirmed included in 2.3.1/2.4.0)
using Microsoft.Windows.Storage.Pickers;
using Microsoft.UI;

public sealed class ElevationSafeFilePickerService : IFilePickerService
{
    private readonly Func<WindowId> _windowIdProvider;

    public ElevationSafeFilePickerService(Func<WindowId> windowIdProvider) =>
        _windowIdProvider = windowIdProvider;

    public async Task<StorageFile?> PickOpenFileAsync(
        IReadOnlyList<string> fileTypeFilters, string? suggestedStartLocation = null)
    {
        var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker(_windowIdProvider());
        foreach (var filter in fileTypeFilters) picker.FileTypeFilter.Add(filter);
        // PickSingleFileAsync() returns PickFileResult (a lightweight wrapper with a
        // string Path — NOT the old Windows.Storage.StorageFile). Adjust IFilePickerService's
        // return type or bridge via StorageFile.GetFileFromPathAsync(result.Path) if the
        // interface must keep returning StorageFile.
        var result = await picker.PickSingleFileAsync();
        return result is null ? null : await Windows.Storage.StorageFile.GetFileFromPathAsync(result.Path);
    }

    public async Task<StorageFolder?> PickSingleFolderAsync(string? suggestedStartLocation = null)
    {
        var picker = new Microsoft.Windows.Storage.Pickers.FolderPicker(_windowIdProvider());
        var result = await picker.PickSingleFolderAsync();
        return result is null ? null : await Windows.Storage.StorageFolder.GetFolderFromPathAsync(result.Path);
    }
}

// Registration change in the renamed App.xaml.cs (was Func<IntPtr>, now Func<WindowId>):
builder.Services.AddSingleton(sp => new Func<WindowId>(() =>
    MainWindow is null ? default : Microsoft.UI.Win32Interop.GetWindowIdFromWindow(
        WinRT.Interop.WindowNative.GetWindowHandle(MainWindow))));
```
**Note:** the constructor signature is `FileOpenPicker(WindowId)`/`FolderPicker(WindowId)` — a `WindowId`, not the old API's `IntPtr` hwnd + `InitializeWithWindow.Initialize()` pattern. `[VERIFIED: learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.fileopenpicker and .folderpicker, fetched 2026-08-31]` — both classes' only constructor takes a `WindowId`; methods are `PickSingleFileAsync()`/`PickMultipleFilesAsync()` (`FileOpenPicker`) and `PickSingleFolderAsync()`/`PickMultipleFoldersAsync()` (`FolderPicker`), returning `PickFileResult`/`PickFolderResult` (each "a lightweight class that contains a string attribute representing the file/folder path" per the official docs, not a `StorageFile`/`StorageFolder`).

### 2. Live-state read pattern for a straightforward registry tweak

```csharp
// Source: derived from AkariOS-Companion/Services/TweakService.cs:164-183 (SetWifi),
// read directly this session — write side quoted verbatim, read side is new.
public sealed class DisableWifiTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string Key = @"SYSTEM\CurrentControlSet\Services\WlanSvc";

    public string Key => "wifi";

    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, Key, "Start") is int start && start == 4;

    public void SetState(bool disable)
    {
        int start = disable ? 4 : 2;
        registry.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\WlanSvc", "Start", start, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\vwififlt", "Start", disable ? 4 : 1, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\netprofm", "Start", disable ? 4 : 3, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Services\NlaSvc", "Start", disable ? 4 : 2, RegistryValueKind.DWord);
        // No SaveState/HasState/ClearState — D-03 compliance: state comes from GetState() above.
    }
}
```

### 3. Cross-thread-safe log console (D-05/D-08, APP-05)

```csharp
// Source: pattern derived from the framework's existing DispatcherQueueExtensions
// (C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/Threading/DispatcherQueueExtensions.cs,
// read in full this session) + CommunityToolkit.Mvvm ObservableObject pattern already used
// by the framework's IInfoBarService (same file structure, read this session).
public interface ILogConsoleService
{
    ObservableCollection<string> Lines { get; }
    void Log(string message);
}

public sealed class LogConsoleService(DispatcherQueue dispatcher) : ILogConsoleService
{
    public ObservableCollection<string> Lines { get; } = new();

    public void Log(string message) =>
        // Fire-and-forget is fine here: callers (tweak handlers, Defender workflow) don't
        // need to await the UI update completing, only that it eventually lands safely.
        _ = dispatcher.RunOnUIThreadAsync(() => Lines.Add(message));
}
```

### 4. NavigationView disabled-item pattern (D-09/D-11)

```csharp
// Extend the framework's existing NavigationItem record
// (C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/NavigationItem.cs, read this session)
public sealed record NavigationItem(string Label, string Glyph, Type PageType, bool IsEnabled = true);
```
```xml
<!-- Extend MainWindow.xaml's existing DataTemplate (MainWindow.xaml:49-57, read this session) -->
<NavigationView.MenuItemTemplate>
    <DataTemplate x:DataType="local:NavigationItem">
        <NavigationViewItem Content="{x:Bind Label}" Tag="{x:Bind PageType}" IsEnabled="{x:Bind IsEnabled}">
            <NavigationViewItem.Icon>
                <FontIcon Glyph="{x:Bind Glyph}" />
            </NavigationViewItem.Icon>
        </NavigationViewItem>
    </DataTemplate>
</NavigationView.MenuItemTemplate>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|-------------------|---------------|--------|
| `Windows.Storage.Pickers` for file/folder selection in a WinUI 3 desktop app | `Microsoft.Windows.Storage.Pickers` (`WindowId`-based, elevation-aware) | Added in WinAppSDK 1.8, refined through 2.0/2.4.0 (keyboard-focus-restore fix shipped in 2.4.0 per the same-session official docs fetch) | Directly resolves APP-04's previously-open picker-elevation question — no CsWin32 fallback needed for Phase 1 |
| WPF `Dispatcher.Invoke` for cross-thread UI updates | WinUI 3 `DispatcherQueue.TryEnqueue`/`RunOnUIThreadAsync` | N/A — WinUI 3 uses a different threading model than WPF from the start | The predecessor's `ToolService.Log`'s `_log.Dispatcher.Invoke(...)` pattern (`ToolService.cs:33-38`) cannot be ported verbatim; the *behavior* (append line, auto-scroll) ports, the *mechanism* must change |
| Windows App SDK 2.3.1 as "current stable" | Windows App SDK 2.4.0 stable (shipped 2026-08-13) | Superseded the project's existing same-day `STACK.md` claim | Recommendation: stay on 2.3.1 for Phase 1 (already template-pinned, avoids bundling an SDK upgrade into a rebrand phase) — see Standard Stack note |

**Deprecated/outdated:** the predecessor's `HKCU\Software\AkariTool` app-tracked state hive (`TweakService.cs:17-25`) is the one piece of "old approach" this phase must NOT carry forward at all, per D-03 — it's not a technology-version deprecation, it's an architectural anti-pattern the user explicitly asked to be replaced.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|----------------|
| A1 | `System.ServiceProcess.ServiceController` is the correct package name for service start/stop/query (vs. some other Microsoft-published alternative) | Standard Stack / Package Legitimacy Audit | Low — this is the long-standing, unambiguous first-party package for this exact purpose; version history on the registry (4.1.0 → 10.0.11, continuous since ~2016) is itself strong circumstantial evidence, but the *name* was not cross-checked against an official "how to control Windows services from .NET" doc page in this session |
| A2 | The `NavigationViewItem.IsEnabled="{x:Bind IsEnabled}"` + companion badge composition is the right pattern for D-09/D-11's "coming soon" cards/nav-items | Architecture Patterns (NavigationView disabled-item pattern) | Low-Medium — sourced from a GitHub issue discussion, not an official Microsoft Learn "how-to" page; the underlying `IsEnabled` binding itself is a standard WinUI/XAML mechanism (not exotic), so the core mechanism is safe even if the exact "SOON badge" composition needs adjustment |
| A3 | A representative single-service registry read (e.g. just `WlanSvc\Start` for the `wifi` tweak, which touches 4 services) is sufficient to drive `IsOn`, rather than requiring all N services/keys a multi-key tweak touches to agree | Live-state-reader design | Low-Medium — if an out-of-band tool changes only one of the N keys, the toggle could show a state that doesn't match every underlying value; this is a design trade-off (simplicity vs. strict correctness) the planner should confirm, not a verified fact |
| A4 | The `dep`/`hyperv`/`vr` tweaks' `bcdedit`/`DISM` state should be read via registry side-effects (where present) rather than shelling a process on every page load | Common Pitfalls (Pitfall 6) | Medium — this is a genuine open design question, not a settled recommendation; see Open Questions below |

## Open Questions

All three resolved during resume-session research review (2026-08-31) — see CONTEXT.md D-12/D-13/D-14.

1. **How should `dep` (pure `bcdedit`, zero registry footprint) report its live state?** — **RESOLVED (D-12):** spawn `bcdedit /enum {current}` and parse the NX line on page load. Strict D-03 compliance chosen over a write-only exception.
   - What we know: `SetDepNx` (`TweakService.cs:215-228`) only ever calls `bcdedit /set NX AlwaysOff`/`/set NX OptIn` — no registry write exists to read back.

2. **Should the `Microsoft.Windows.Storage.Pickers`-based `IFilePickerService` implementation be built as part of Phase 1, given no Phase 1 page actually invokes a picker?** — **RESOLVED (D-13):** yes, with a temporary debug smoke-test button (removed once Phase 4 wires a real consumer).
   - What we know: the phase goal explicitly lists "an elevation-safe file/folder picker" as one of the foundational pieces Phase 1 stands up, and APP-04 is in this phase's requirement list.

3. **Does `IRegistryService.OpenRealUserHive` need to handle the case where `explorer.exe` isn't running** (e.g. a server-core-like or Explorer-crashed state)? — **RESOLVED (D-14):** preserve the predecessor's hard failure as-is, no fallback.
   - What we know: `CreateRealHkcuSubKey` (`TweakService.cs:145-155`) throws `InvalidOperationException("explorer.exe not found.")` if no explorer process exists.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|-------------|-----------|---------|----------|
| .NET SDK | Build/run the app | Yes | `[VERIFIED: dotnet --version]` → `10.0.400` — exact match to the framework's `global.json` pin | — |
| Windows App SDK 2.3.1 | WinUI 3 runtime | Resolved via NuGet at restore time (not pre-installed globally; self-contained deploy) | 2.3.1 pinned centrally | — |
| Windows 10/11 x64 | Elevation, registry/service APIs, Mica backdrop | This research session ran on `win32` per the environment info supplied — actual target-machine verification (elevated launch, Mica rendering, real registry writes) must happen on the developer's actual Windows machine, not assumed from this research pass | — | — |
| `explorer.exe` running (for `CreateRealHkcuSubKey`) | `startmenu`/`transparency` tweaks' real-user-HKCU trick | Assumed present on any interactive desktop session (standard Windows behavior) | — | Hard failure preserved per predecessor parity (see Open Question 3) |

**Missing dependencies with no fallback:** none identified — this phase has no external service dependencies beyond the Defender tweak's GitHub-hosted PostInstall assets (Pitfall 5), which already have documented failure handling (`filesReady` check, logs and returns rather than throwing — `TweakService.cs:843-847`).

**Missing dependencies with fallback:** none beyond the `explorer.exe` case noted above.

## Validation Architecture

Skipped — `workflow.nyquist_validation` is explicitly `false` in `.planning/config.json` `[VERIFIED: C:/Users/isleap/Documents/GitHub/Akari-Toolbox/.planning/config.json]`.

## Security Domain

`security_enforcement: true`, `security_asvs_level: 1` `[VERIFIED: C:/Users/isleap/Documents/GitHub/Akari-Toolbox/.planning/config.json]` — Security Domain section required.

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|----------------|---------|--------------------|
| V2 Authentication | No | Single-user local desktop tool; no login/auth surface in this phase |
| V3 Session Management | No | Not applicable — no session concept |
| V4 Access Control | Partial | The entire app runs as a single elevated identity — "access control" here means the manifest-level `requireAdministrator` gate (APP-01) is the *only* access boundary; there is no in-app privilege separation, by design (matches predecessor) |
| V5 Input Validation | Partial | The tweak keys/values are all hardcoded (copied from `TweakService.cs`'s own literal registry paths/values) — no user-supplied input reaches registry/service mutation code in this phase. The one input surface (a future file/folder picker consumer, not built until Phase 4) is out of this phase's runtime attack surface |
| V6 Cryptography | No | No cryptographic operations in this phase (SHA256 verification of downloaded assets is a Phase 4/DOWNLOADS-01 concern per the existing project `STACK.md`, not Phase 1) |
| V10 Malicious Code | Yes | The Defender-disable workflow (TWEAKS-02) is itself the single highest-sensitivity code path in this phase — it disables the OS's own malware protection. D-01 mandates porting it unmodified rather than "improving" it, which is itself the correct security posture here (don't introduce new bugs into security-sensitive, already-tested code during a UI-stack port) |

### Known Threat Patterns for this stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|-----------------------|
| Registry-squatting (writing to a key namespace another process may have claimed, or creating unexpected keys on first-run machines missing an expected parent key) | Tampering | `OpenSubKey`/`GetValue` with null-checks before writing; already a documented CLAUDE.md-level project constraint for this repo |
| Second elevation prompt from `SetDefenderAsync`'s `Verb = "runas"` child process, even though the whole app is already elevated | Elevation of Privilege (informational — not a vulnerability, a UX/trust-signal quirk) | Port as-is per D-01 — do not silently drop the redundant `runas` (that's a behavior change the user explicitly said not to make to this code path), but be aware a code reviewer unfamiliar with the predecessor may flag this as suspicious; document it inline as intentional |
| Untrusted `C:\PostInstall\NoDefender.cab`/`.ps1` execution if the GitHub-hosted PostInstall repo is ever compromised or DNS-spoofed | Tampering / Elevation of Privilege | Out of Phase 1's scope to fix (the predecessor already fetches over HTTPS from a pinned repo, no integrity/checksum verification exists in the source read this session) — flagged here for awareness; a SHA256 verification pass on downloaded PostInstall assets is Phase 4/DOWNLOADS-01 territory per the existing project `STACK.md`, but Phase 1's minimal `PostInstallService` port inherits this same unverified-download characteristic for the Defender-required files specifically. Not a new risk introduced by this phase — pre-existing in the predecessor, carried forward per D-01/parity |

## Sources

### Primary (HIGH confidence)

- Direct read of `AkariOS-Companion` predecessor source this session: `Services/TweakService.cs` (full file, 1118 lines), `Services/ITweakService.cs`, `Services/ToolService.cs`, `Services/PostInstallService.cs` (partial), `Models/TweakItem.cs`, `ViewModels/AkariOSTweaksViewModel.cs`, `ViewModels/HomeViewModel.cs`, `App.xaml.cs`, `app.manifest`, `MainWindow.xaml` (log console region)
- Direct read of `WinUI-3-MVVM-Framework` template source this session: `README.md`, `Directory.Packages.props`, `global.json`, `src/AppTemplate.App/{App.xaml, App.xaml.cs, MainWindow.xaml, MainWindow.xaml.cs, NavigationItem.cs, app.manifest, AppTemplate.App.csproj}`, `src/AppTemplate.Framework/{ServiceCollectionExtensions.cs, Services/IFilePickerService.cs, Services/IInfoBarService.cs, Navigation/FrameNavigationService.cs, Threading/DispatcherQueueExtensions.cs}`
- `[VERIFIED: api.nuget.org/v3-flatcontainer/system.serviceprocess.servicecontroller/index.json]` — direct NuGet registry API query, fetched this session
- `[VERIFIED: api.nuget.org/v3-flatcontainer/microsoft.windowsappsdk/index.json]` — direct NuGet registry API query, fetched this session
- `[VERIFIED: api.nuget.org/v3-flatcontainer/communitytoolkit.mvvm/index.json]` — direct NuGet registry API query, fetched this session
- [Microsoft.Windows.Storage.Pickers Namespace — Microsoft Learn](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers?view=windows-app-sdk-2.0) — official docs, fetched this session, explicitly states the elevation-mode gap this API addresses
- [FileOpenPicker Class — Microsoft Learn](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.fileopenpicker?view=windows-app-sdk-2.0) and [FolderPicker Class — Microsoft Learn](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.windows.storage.pickers.folderpicker?view=windows-app-sdk-2.0) — official docs, fetched this session, exact constructor/method signatures
- [What's new: SDK, WinUI, tools — Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/whats-new/whats-new-for-developers) — official docs, fetched this session (page dated 2026-08-25), confirms Windows App SDK 2.4.0 stable / 2.4.1-experimental as current, superseding the existing project research's 2.3.1-is-current claim
- Direct read of `.planning/{CONTEXT.md, REQUIREMENTS.md, STATE.md, config.json}` for this project, this session

### Secondary (MEDIUM confidence)

- Existing project-level research (`.planning/research/{STACK.md, ARCHITECTURE.md, PITFALLS.md}`), read this session — same-day (2026-08-31), HIGH-confidence for anything grounded in direct source reads (which this document cross-references and extends), MEDIUM for anything grounded in external web sources not independently re-verified this session (e.g. the manifest-merge bug history)
- [Trying to use a FileOpenPicker while running the app as Administrator will crash the app — microsoft/WindowsAppSDK Issue #2504](https://github.com/microsoft/WindowsAppSDK/issues/2504) and related issue threads — official repo issues, corroborate the elevation-crash problem `Microsoft.Windows.Storage.Pickers` was built to solve

### Tertiary (LOW confidence)

- NavigationView `IsEnabled`/"coming soon" composition pattern — sourced from a GitHub issue discussion (`microsoft/microsoft-ui-xaml#3687`), not an official Microsoft Learn how-to page; flagged in Assumptions Log (A2)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — every version claim in this document is either a direct read of the already-committed `Directory.Packages.props`/`global.json` pins, or a fresh NuGet registry API query performed this session
- Architecture: HIGH — both the predecessor's tweak/Defender logic and the framework's shell/DI/dispatcher/picker code were read in full (not summarized from memory) this session; the live-state-reader design is new synthesis grounded in that source, flagged with an explicit "illustrative, not exhaustive" caveat where appropriate
- Pitfalls: HIGH for the mechanically-derivable ones (D-03 anti-pattern carry-over risk, PostInstall cross-phase dependency, bcdedit/DISM non-registry tweaks) — all confirmed by direct source reads this session, not inferred; MEDIUM for the elevation-manifest-merge-history pitfall, which relies on the existing project research's characterization of a historical GitHub issue not independently re-fetched this session

**Research date:** 2026-08-31
**Valid until:** 30 days (stable domain — the underlying predecessor/framework source won't change; the one fast-moving element, Windows App SDK version currency, should be re-checked if this phase's implementation slips past ~2026-09-30)
