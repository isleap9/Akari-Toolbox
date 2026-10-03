# Stack Research

**Domain:** Windows gaming optimization desktop tool (WinUI 3, native registry/service/driver/WMI ops, tweak catalog + rollback)
**Researched:** 2026-10-03
**Confidence:** HIGH

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| .NET | 10.0.x SDK (10.0.401 current, Sep 2026) / `net10.0-windows10.0.26100.0` | Runtime + TFM for the whole app | Already pinned in the project; .NET 10 is the current LTS-era release and the only TFM the Windows 26100 SDK projection targets cleanly. Stay — downgrading to .NET 8/9 buys nothing and breaks the pinned SDK BuildTools. |
| Windows App SDK / WinUI 3 | **Keep 2.3.1 (pinned)** — note 2.5.1 stable exists (09/2026) | UI framework (NavigationView, Mica, InfoBar, AutoSuggestBox, ItemsRepeater) | 2.3.1 is verified working with the 13-page shell and `Errors: 0 Warnings: 0` bar. 2.4/2.5 add Phi Silica / XAML-optional-changes / Video SR — none of which the tweak engine needs. Upgrading mid-milestone forces a full 13-page regression for zero gaming-feature gain. Defer upgrade to its own phase. |
| Microsoft.Windows.SDK.BuildTools | 10.0.26100.4654 (pinned) | Win32/WinRT projection headers at build time | Matches the `windows10.0.26100.0` TFM; required for CsWin32-adjacent P/Invoke surface (power schemes, services, display). Keep in lockstep with TFM. |
| C# language | 13 (.NET 10 default) | App + tweak engine language | Source generators (MVVM Toolkit, CsWin32) depend on current Roslyn; no reason to pin `LangVersion` down. |
| CommunityToolkit.Mvvm | 8.4.2 (pinned = latest stable) | ObservableObject / RelayCommand / source-generated VMs for all 13 ViewModels + new browse VM | Verified latest stable on NuGet (6 months, ~3.8M downloads of this version). UI-framework-agnostic, zero runtime cost beyond generated code, used by first-party Store apps. The gaming browse model (search text + category filter + selection → 169+ item list) maps directly onto `[ObservableProperty]` + `ICommand` + `ObservableCollection` with `CollectionViewSource` filtering. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Microsoft.Extensions.Hosting + DependencyInjection + Logging + Configuration.Json | 10.0.11 (pinned) | App host, DI for tweak services, file logging, settings JSON | Already in the tree. Gaming work adds new services (TweakCatalogService, TweakExecutionService with progress, RollbackJournalService, HardwareSurveyService) — register them in the existing host instead of hand-rolling singletons. `Configuration.Json` backs `%LOCALAPPDATA%\AkariToolbox` settings. |
| System.Management | 10.0.12 (pinned = latest) | WMI reads (GPU/driver versions, CPU, adapter inventory for GPU/network paths) | Correct choice for read-only hardware survey. Keep the existing rule: dispose `ManagementObjectSearcher` + `ManagementObjectCollection` + objects (the known leak source). Read-only — never use WMI `Put()` for tweaks; registry/service APIs are deterministic and rollback-friendly. |
| Microsoft.Windows.CsWin32 | 0.3.335 (latest stable, **new addition**) | Source-generated type-safe P/Invoke for powrprof (`PowerSetActiveScheme`, `PowerWriteACValueIndex`), advapi32 service/registry, display (`ChangeDisplaySettingsEx`), `NtSetTimerResolution` | This is the Microsoft-recommended way to call Win32 from C# (per official Learn docs) — replaces hand-written `[DllImport]` strings that rot. Needed the moment the gaming milestone touches power plans, timer resolution, service control, and display scaling natively. Private `NativeMethods.txt` allowlist keeps the generated surface minimal (only what the tweak engine calls). |
| CommunityToolkit.WinUI.Controls.SettingsControls | 8.2.251219 (latest stable) | `SettingsCard` / `SettingsExpander` for per-tweak rows with description + state + info affordance | **New addition for the Winhance-style browse model.** This is exactly the control Winhance-style UIs are built from: title + description + icon + trailing toggle/button per row, with expanders for per-tweak explanation text. Hand-rolling this in raw XAML across 100+ gaming rows duplicates a tested, accessible control. Use it for the gaming browse list rows; keep existing pages untouched. |
| Microsoft.Xaml.Behaviors.WinUI.Managed | 3.0.1 (pinned) | XAML behaviors (e.g., invoke command on AutoSuggestBox query submit) | Already in the tree. Use for wiring search-box events to VM commands without code-behind, preserving the page-level-`{Binding}` rule. |
| xunit + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk + Moq | 2.9.3 / 3.1.4 / 17.14.1 / 4.20.72 (all pinned, current) | Unit tests for tweak catalog integrity, rollback journal, search/filter logic | Keep. The gaming milestone's highest-value tests are pure-logic: catalog schema validation (every tweak has ID/description/rollback), filter/search correctness over the 169+ row set, rollback-journal round-trip. xUnit 2.9.x is stable and matches the existing `AppTemplate.Tests` project; do NOT migrate to xUnit v3/MTP mid-milestone (new runner model, churn for no coverage gain). |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| Visual Studio 2026 (17.14+) + .NET 10 SDK | Build WinUI 3 unpackaged self-contained app | VS 2022 does NOT support .NET 10 targeting — contributors must be on VS 2026 / current SDK. Document in CONTRIBUTING. |
| WinUI 3 Controls Gallery app | Reference implementations for AutoSuggestBox, CollectionViewSource grouping, InfoBar, progress patterns | Use as the pattern source for the searchable grouped list; do not copy-vendor its code. |
| `powercfg`, `sc.exe`, regedit (OS-built-in, manual verification only) | Verify what the engine wrote (power scheme GUIDs, service states, registry values) | Verification tools, never invoked as child processes from the app (native-ops-only constraint cuts both ways: no shelling out to scripts/binaries either). |

## Installation

Central package management is ON (`Directory.Packages.props`) — add versions there, reference without versions in `.csproj`:

```xml
<!-- Directory.Packages.props: ADD these two lines -->
<PackageVersion Include="Microsoft.Windows.CsWin32" Version="0.3.335" />
<PackageVersion Include="CommunityToolkit.WinUI.Controls.SettingsControls" Version="8.2.251219" />
```

```xml
<!-- App .csproj: ADD -->
<PackageReference Include="Microsoft.Windows.CsWin32" />
<PackageReference Include="CommunityToolkit.WinUI.Controls.SettingsControls" />
<!-- NativeMethods.txt (project root or Properties/): list one API per line, e.g. -->
<!-- PowerSetActiveScheme, PowerWriteACValueIndex, ChangeDisplaySettingsEx, ... -->
```

No other new packages needed. Everything else is already pinned.

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| WinUI 3 (stay) | WPF | Only if WinUI 3 hit a hard blocker (e.g., unpackaged admin-elevation scenario). It hasn't — the 13-page shell ships. WPF would forfeit Mica/NavigationView modern UX and require a full rewrite. |
| Windows App SDK 2.3.1 (stay) | Upgrade to 2.5.1 now | When a dedicated "SDK upgrade + full regression" phase is scheduled. 2.5.x features (Phi Silica JSON, Video SR, ARM64EC ML) are irrelevant to registry/service tweaks. |
| CsWin32 source generator | Hand-written `[DllImport]` / pinvoke.net copy-paste | Never for new code — hand-rolled signatures are the #1 source of subtle marshaling bugs (struct layout, SetLastError, CharSet). CsWin32 generates from real metadata. |
| System.Management (WMI, read-only) | Microsoft.Management.Infrastructure (MMI/CIM cmdlets-style API) | MMI is the "modern" API and PowerShell moved to CIM, but for a desktop app doing simple synchronous hardware inventory, System.Management 10.x is maintained, simpler, and already integrated with the disposal discipline in this codebase. Revisit only if async/remote WMI is ever needed (it isn't — local machine only). |
| xUnit 2.9.3 (stay) | xUnit v3 + Microsoft.Testing.Platform / TUnit / MSTest 4.x | For a greenfield test project chasing MTP speed. Not worth migrating `AppTemplate.Tests` mid-milestone; the value is in catalog/journal tests, not runner throughput. |
| CommunityToolkit SettingsControls | Syncfusion/DevExpress/Telerik WinUI suites | Only if a future need (charts for benchmarking history, grids) justifies a commercial dependency. The browse model needs cards/expanders only — the free Toolkit covers it. Never pull a whole suite for one control. |
| JSON rollback journal (file in %LOCALAPPDATA%) | SQLite / EF Core | If the journal ever needs querying across hundreds of entries with relations. Current need is append-only per-tweak before/after snapshots + replay — a versioned JSON journal is simpler, human-inspectable (trust!), and dependency-free. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| PowerShell/`cmd` script execution or bundled `.exe`/`.bat`/`.reg` payloads | Architecture forbids it (PROJECT.md hard constraint); also breaks reversibility auditing, triggers Defender/SmartScreen, and makes per-tweak rollback unimplementable | Native C# ops: `Microsoft.Win32.Registry`, `ServiceController`/advapi32 via CsWin32, WMI reads via System.Management |
| NVAPI / vendor GPU SDKs (NVIDIA/AMD proprietary libs) | Native binaries + per-vendor SDK versioning + redistribution licensing; violates native-ops-only and explodes the test matrix | Registry-backed driver settings + documented display APIs (`ChangeDisplaySettingsEx`, DXGI refresh enumeration) + driver-version survey via WMI; mark deeper vendor-profile editing out of scope |
| Registry "tweak packs" applied blindly (whole-hive imports) | Un-auditable, un-rollbackable per tweak, the exact trust violation the "deeper-but-safe" decision rejects | Per-tweak `TweakAction` with captured before-value → journal → replay-undo |
| Timer-resolution tools' approach (global `NtSetTimerResolution` forced always-on) | Measurably increases idle power draw; enthusiast-trust poison if applied silently | Scoped/explained toggle with measured-effect note + rollback, default off |
| Disabling Windows Update / Store / core drivers for "FPS" | Breaks the safety invariant (PROJECT.md: never break updates, Store, core drivers); causes support load and update-revert churn | "Clean-system path" limited to telemetry, background apps, non-essential services with documented safe list |
| x:Bind at page level | Known WMC9999 compiler death in this project (PROJECT.md XAML rule) | Page-level `{Binding}`; x:Bind only inside DataTemplates (browse-list item templates are the textbook case) |
| UWP-era `Microsoft.Toolkit.*` packages | Deprecated; namespace/type collisions with `CommunityToolkit.WinUI.*` | `CommunityToolkit.WinUI.Controls.*` 8.x only |

## Stack Patterns by Variant

**If building the gaming browse page (searchable categories + per-tweak info):**
- Use `AutoSuggestBox` (query) + `NavigationView`-adjacent category filter + `ListView`/`ItemsRepeater` bound to a `CollectionViewSource`-filtered `ObservableCollection<TweakRowViewModel>`, rows rendered as Toolkit `SettingsCard`/`SettingsExpander` via DataTemplate (x:Bind allowed there).
- Because virtualization keeps 169+ rows smooth, and the Toolkit cards give title/description/state/info affordance for free.

**If applying tweaks in bulk with progress + rollback:**
- Use existing `TweakAction`/`TweakToggle` engine + new `RollbackJournalService` (JSON, before-values) + `IProgress<T>`-driven batch executor surfaced through `InfoBar` + determinate `ProgressBar` in the trusted-control bar.
- Because every tweak stays individually reversible and the UI never blocks: the "trusted control" requirement is a UX-over-engine pattern, not a new engine.

**If surveying hardware/drivers (GPU path, network adapters):**
- Use `System.Management` WMI reads (Win32_VideoController, Win32_NetworkAdapter) + registry driver-version keys, cached per session.
- Because it needs no new dependency and matches the existing `SystemInfo` disposal discipline.

**If touching power plans / timer / Game Mode / services:**
- Use CsWin32-generated P/Invoke (`powrprof`, `advapi32`) + `Microsoft.Win32.Registry` for documented policy keys.
- Because these are the only native, auditable, rollback-capable surfaces; no scripts, no binaries.

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| Microsoft.WindowsAppSDK 2.3.1 | .NET 10 + windows10.0.26100.0 TFM + BuildTools 10.0.26100.4654 | Verified pinned combo in this repo; do not bump one without the others. |
| CommunityToolkit.Mvvm 8.4.2 | .NET 10, Roslyn source generators in VS 2026 | Latest stable; no conflicts with Toolkit WinUI controls packages (separate namespaces). |
| CommunityToolkit.WinUI.Controls.SettingsControls 8.2.251219 | Windows App SDK 2.x + WinUI 3 | 8.2 stable targets the 2.x line; safe alongside WASDK 2.3.1. |
| Microsoft.Windows.CsWin32 0.3.335 | .NET 10, C# 13, VS 2026 | Build-time source generator; emits no runtime dependency. Requires `NativeMethods.txt` allowlist file; start minimal. |
| System.Management 10.0.12 | .NET 10 Windows-only | Windows-only package — fine, app is Windows 10/11 x64 only by design. |
| Microsoft.Extensions.* 10.0.11 | .NET 10 | Keep all five at the same 10.0.x band; mixed bands cause DI/logging abstract mismatch warnings (breaks the zero-warning bar). |
| xunit 2.9.3 + runner 3.1.4 + Test.Sdk 17.14.1 | .NET 10 test projects | Stable trio; do not mix with xUnit v3 packages in the same project. |

## Sources

- Microsoft Learn — Windows App SDK downloads (2.5.1 latest stable 09/2026; 2.3.1 still serviced; verified 2026-10-03) — HIGH
- NuGet — CommunityToolkit.Mvvm 8.4.2 (latest stable) — HIGH
- NuGet — CommunityToolkit.WinUI.Controls.SettingsControls 8.2.251219 (latest stable) — HIGH
- NuGet — Microsoft.Windows.CsWin32 0.3.335 (latest stable) — HIGH
- NuGet — System.Management 10.0.12 (latest) — HIGH
- Microsoft Learn — "Call Win32 APIs from a C# Windows app (CsWin32)" (recommended way) — HIGH
- Microsoft Learn — Testing in .NET (MSTest/NUnit/TUnit/xUnit landscape) + xUnit v3 MTP docs — MEDIUM (informs stay-on-v2 call)
- Microsoft Learn — AutoSuggestBox guidelines + WinUI virtualization (One Dev Minute) — MEDIUM
- dotnet.microsoft.com — .NET 10 SDK 10.0.401 (Sep 2026) — HIGH
- Repo ground truth — `Directory.Packages.props` pins read directly — HIGH

---
*Stack research for: Akari Toolbox gaming optimizer push (WinUI 3 native-ops desktop app)*
*Researched: 2026-10-03*
