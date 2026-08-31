# Walking Skeleton — Akari Toolbox

**Phase:** 1
**Generated:** 2026-08-31

## Capability Proven End-to-End

A user launches the rebranded, elevated "Akari Toolbox" app (copied/renamed from `WinUI-3-MVVM-Framework`), lands on a Home dashboard, opens the Akari OS Tweaks page, and flips the "Disable WiFi" toggle — the toggle reads the real `WlanSvc\Start` registry value on load, writes real registry values through a primitive/handler/catalog stack when flipped, and reverts to the real prior value when flipped back — all inside a native Fluent 2 / Mica shell running under `requireAdministrator`.

## Architectural Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Starting point | Copy `WinUI-3-MVVM-Framework` verbatim into this repo's root, rename `AppTemplate` -> `AkariToolbox` everywhere (folders, `.csproj`, `.slnx`, namespaces, assembly names, `App.AppName`, `SettingsFolder`) | Framework template is already-tested MVVM/DI/navigation/theming plumbing (PROJECT.md decision); rebuilding it from scratch would duplicate proven work |
| Elevation | Add the predecessor's `<trustInfo>`/`<requestedExecutionLevel level="requireAdministrator">` block to `app.manifest` (framework template currently has none) | APP-01; predecessor's exact manifest shape already proven on Windows App SDK 2.3.1 |
| Tweak architecture | `ITweakHandler` (self-describing: `Key`/`Title`/`Description`/`Order`/`GetState()`/`SetState(bool)`) implementations auto-discovered via one-time assembly-scan reflection registration into `ITweakCatalog`, instead of a hand-maintained switch or a shared static definitions array | Decouples "add a new tweak" from any shared file edit (no central registration list to touch), which is what makes the 27 remaining tweak handlers plannable as independent, parallel, conflict-free plans. `ITweakCatalog.SetStateAsync` orchestrates capture-then-write (reads live `GetState()` before mutating, stores it, skips redundant writes) so TWEAKS-03's real-prior-state guarantee is centralized once instead of reimplemented 31 times (RESEARCH.md "Don't Hand-Roll") |
| Live state (D-03) | Every non-Defender handler's `GetState()` reads the exact registry/service value(s) its own `SetState()` last wrote — no app-tracked flag, no `HKCU\Software\AkariTool` hive | Replaces the predecessor's `HasState`/`SaveState`/`ClearState` anti-pattern; this is the phase's core new design contribution |
| Defender exemption (D-01) | `DefenderTweakHandler` is a special-cased entry in the catalog, calling the ported `SetDefenderAsync` chain byte-for-byte; its `GetState()` stays the predecessor's `HasState("DisableDefender")` semantics | Explicit, twice-repeated user instruction — do not refactor/decompose this code path in Phase 1 |
| System primitives location | `IRegistryService`, `IWindowsServiceController`, `IScriptRunner`, `ILogConsoleService` live in `AkariToolbox.Framework/Services/`, registered via a new `ServiceCollectionExtensions.Primitives.cs` extension file (not the original `ServiceCollectionExtensions.cs`) | Matches where `IFilePickerService`/`IDialogService`/`IInfoBarService` already live in the framework; a dedicated extension file lets later plans extend primitive registration without re-touching (and wave-conflicting on) the original `AddMvvmFramework()` |
| File/folder picker | Swap `IFilePickerService`'s implementation to `Microsoft.Windows.Storage.Pickers` (`WindowId`-based, elevation-safe), add folder-pick methods | APP-04; official WinAppSDK API purpose-built for exactly this elevated-picker gap (RESEARCH.md Code Example 1) |
| Deployment target | No deployment — "dev environment" for this desktop app is `dotnet run --project src/AkariToolbox.App -c Debug` (or the built self-contained exe) launched from an elevated shell | Matches the project's "unpackaged, self-contained, just run the exe" distribution model; there is no hosted/cloud target for a Windows system-tweak tool |
| Directory layout | `src/AkariToolbox.App/{Models,Services,Services/TweakHandlers,ViewModels,Views}` (app-specific), `src/AkariToolbox.Framework/{Services,Threading,Navigation,...}` (reusable), `src/AkariToolbox.Tests/` (xUnit) | Mirrors the copied framework's existing layout; app-specific tweak logic stays out of the reusable Framework project |

## Stack Touched in Phase 1

- [x] Project scaffold (copy/rename framework: `.slnx`, both `.csproj`, `global.json`, `Directory.Packages.props`, namespaces, `App.AppName`/`SettingsFolder`)
- [x] Elevation — `requireAdministrator` in `app.manifest`, smoke-tested as a real elevated + self-contained build
- [x] Routing — Home page navigates to the Akari OS Tweaks page via the framework's `INavigationService`
- [x] "Database" (registry, for this app) — one real read (`WlanSvc\Start` via `IRegistryService.GetValue`) AND one real write (`IRegistryService.SetValue` on toggle)
- [x] UI — one interactive `ToggleSwitch` (WiFi tweak) wired end-to-end through `ITweakCatalog` to the registry primitive
- [x] Local full-stack run command — `dotnet run --project src/AkariToolbox.App -c Debug` from an elevated PowerShell/terminal

## Out of Scope (Deferred to Later Plans in This Phase)

- The remaining 31 tweak handlers (registry-only, service-backed, bcdedit/DISM-hybrid, and Defender) — Plans 01-04, 01-05, 01-06
- `ILogConsoleService`, `IWindowsServiceController`, `IScriptRunner` — Plan 01-02
- Elevation-safe file/folder picker swap + debug smoke-test button — Plan 01-03
- Full 32-item async-parallel state-read wiring, D-03 anti-pattern verification pass, and final elevated build/launch check — Plan 01-07
- Gaming Tweaks, Debloat, Downloads/Misc pages — Phases 2-4, not this phase

## Subsequent Slice Plan

- Plan 01-02: system primitives (log console, service controller, script runner) — none of these change the skeleton's architecture, they extend it
- Plan 01-03: elevation-safe picker (APP-04), built on the skeleton's DI shell
- Plans 01-04/01-05/01-06: the remaining 31 `ITweakHandler` implementations, auto-discovered by the skeleton's reflection-based catalog registration with zero shared-file edits
- Plan 01-07: final integration — expands the skeleton's single-tweak async read into the full 32-tweak parallel read, verifies the whole page end-to-end
- Phase 2 (Gaming Tweaks): reuses this skeleton's `ITweakHandler`/`ITweakCatalog`/primitive stack unchanged, per ROADMAP.md
