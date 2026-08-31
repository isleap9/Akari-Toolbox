# Akari Toolbox

## What This Is

Akari Toolbox is a Windows system tweaking, debloat, and optimization utility for Akari OS and Windows 10/11 in general. It's a ground-up port of the existing WPF app "AkariOS Companion" to native WinUI 3 with clean MVVM architecture, built on the user's own reusable WinUI 3 MVVM framework. It runs elevated and applies registry, service, and PowerShell-backed changes with instant status feedback.

## Core Value

Every tweak, debloat action, and downloaded asset must apply correctly, report accurate state, and (where applicable) be safely revertible — this is a system-modification tool, not just a settings screen, so correctness and predictability come before UI polish.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Home page ported to WinUI 3 MVVM (landing/dashboard, matching current app's entry point)
- [ ] Akari OS Tweaks page ported: 32 registry-backed OS-level tweaks as toggle switches, instant apply with state feedback, including the two-phase Disable Defender workflow
- [ ] Gaming Tweaks page ported: GPU/latency/service tuning toggles, SvcHost split threshold / Win32 priority separation / service config dropdowns, quick-access tool grid (NVIDIA/AMD/third-party utilities)
- [ ] Debloat page ported: 28 PowerShell-backed debloat actions, moved out of code-behind into proper ViewModel + service
- [ ] Downloads page ported: self-healing PostInstall asset fetcher (mirrors `C:\PostInstall\` from GitHub when missing), playbooks/drivers/utility links
- [ ] Misc page ported: 12 context-menu add/remove entries plus extra tools
- [ ] App runs elevated (`requireAdministrator` in app manifest), matching predecessor's privilege model
- [ ] App identity rebranded to "Akari Toolbox" (namespace, assembly name, manifest identity, icon/branding assets)
- [ ] All ported pages use the WinUI-3-MVVM-Framework's DI, navigation, settings, theming, and dialog services rather than page-level code-behind logic
- [ ] Visual style uses native WinUI 3 Fluent 2 controls and the framework's Mica backdrop/theming — the predecessor's WPF-UI custom theme (`Themes/Colors.xaml`, `Themes/Controls.xaml`) is not carried over

### Out of Scope

- Deeper "Ultimate" tweak collection (~110 PowerShell scripts across Check/Refresh/Setup/Installers/Graphics/Windows/Hardware/Advanced categories, at `C:\Users\isleap\Desktop\AkariOS Tweaks`) — deferred to a v2 milestone; v1 is parity-first per explicit decision
- Any new tweaks/features beyond AkariOS Companion's current feature set — same reason, v1 = parity first

## Context

- **Predecessor app**: `AkariOS-Companion` at `C:\Users\isleap\Documents\GitHub\AkariOS-Companion` — WPF + WPF-UI (Fluent) + CommunityToolkit.Mvvm, .NET 8, `requireAdministrator`. Five pages (Home, AkariOSTweaks, GamingTweaks, Debloat, Downloads, Misc). `TweakService` (1117 lines) holds the real registry/service tweak logic behind `ITweakService`. 53 PowerShell scripts embedded as resources, extracted and run via `ToolService` at runtime. `PostInstallService` mirrors a ~30MB asset folder from `github.com/isleap9/PostInstall` to `C:\PostInstall\` on first use (no-op on real Akari OS where the files already exist).
- **Architecture debt being fixed by the port**: `DebloatPage.xaml.cs` (180 lines) and `GamingTweaksPage.xaml.cs` (579 lines) hold significant logic in code-behind despite `GamingTweaksViewModel` existing — the WinUI 3 port should move this into ViewModels/Services properly, not transliterate the code-behind.
- **Visual style is not ported**: the predecessor's WPF-UI Fluent skin (`Themes/Colors.xaml`, `Themes/Controls.xaml`, custom brushes) is deliberately left behind — the WinUI 3 app uses the framework's native Fluent 2 controls, Mica backdrop, and theme/culture services as-is. Only functional assets (logo, background image) are candidates for reuse, not the WPF-UI styling.
- **Framework to build on**: `WinUI-3-MVVM-Framework` (AppTemplate) at `C:\Users\isleap\Documents\GitHub\WinUI-3-MVVM-Framework` — .NET 10, Windows App SDK 2.3.1, WinUI 3, unpackaged + self-contained (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`). Ships DI via `Microsoft.Extensions.Hosting`, `FrameNavigationService`/`INavigationService`, `SettingsService` (JSON file storage), `ThemeService`, `CultureService`, `IDialogService`, `IFilePickerService`, `IInfoBarService`, `IWindowService`, `WeakReferenceMessenger` wrappers, reusable converters/behaviors/collections, and a rolling-file `ILogger` provider. Plan: copy/rename this template's solution (`App` + `Framework` + `Tests` projects) as the starting point rather than building MVVM plumbing from scratch. Its default `app.manifest` does **not** request elevation — needs `requireAdministrator` added.
- **Future tweak source (v2)**: `C:\Users\isleap\Desktop\AkariOS Tweaks` ("Ultimate" collection) — ~110 PowerShell scripts organized into `1 Check`, `2 Refresh`, `3 Setup`, `4 Installers`, `5 Graphics`, `6 Windows`, `7 Hardware`, `8 Advanced`, plus a curated, SHA256-verified list of third-party tools (7-Zip, Autoruns, CPU-Z, CRU, DDU, GPU-Z, HWiNFO, MSI Afterburner, NVIDIA Profile Inspector, Prime95, vcredist, etc.). This goes considerably deeper than the current app (BIOS updates, driver management, hardware overclocking, security/advanced tweaks) and is the source material for enhancements after v1 parity ships.

## Constraints

- **Tech stack**: WinUI 3 (Windows App SDK), CommunityToolkit.Mvvm, built directly on the WinUI-3-MVVM-Framework template — reuses proven DI/navigation/settings/theming plumbing instead of rebuilding it
- **Packaging**: Unpackaged and self-contained (no Windows App Runtime dependency on the target machine) — matches the framework's default and the predecessor's "just run the exe" distribution model, important for fresh installs and VM environments
- **Privilege**: Must run elevated (`requireAdministrator`) — registry, service, and Windows Defender modifications require admin, same as the predecessor
- **Platform**: Windows 10/11 x64 — matches predecessor's supported OS
- **Script execution**: PowerShell scripts embedded as resources and extracted/run at runtime, following the predecessor's `ToolService` pattern — proven approach for the 28+ debloat scripts

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Port to WinUI 3 on top of the existing WinUI-3-MVVM-Framework (AppTemplate) rather than a fresh solution | Framework already provides DI, navigation, settings, theming, dialogs, and logging — avoids re-solving MVVM plumbing | — Pending |
| v1 = feature parity with AkariOS Companion; deeper "Ultimate" tweak collection deferred to v2 | Ship a clean, correct port first before expanding scope | — Pending |
| Rebrand from "AkariOS Companion" to "Akari Toolbox" | Intentional product/repo rename | — Pending |
| Keep the admin-elevation requirement (`requireAdministrator`) | Same system-level operations (registry, services, Defender) as the predecessor | — Pending |
| Drop the predecessor's WPF-UI custom theme entirely; use the framework's native WinUI 3 Fluent 2 look (Mica, default controls) | User explicitly wants the native WinUI 3 MVVM look, not a WPF-UI skin carried over | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-08-31 after initialization*
