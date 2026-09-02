# Akari Toolbox

## What This Is

Akari Toolbox is a Windows system tweaking, debloat, and optimization utility for Akari OS and Windows 10/11 in general. It's a ground-up port of the existing WPF app "AkariOS Companion" to native WinUI 3 with clean MVVM architecture, built on the user's own reusable WinUI 3 MVVM framework. It runs elevated and applies registry, service, and PowerShell-backed changes with instant status feedback.

## Core Value

Every tweak, debloat action, and downloaded asset must apply correctly, report accurate state, and (where applicable) be safely revertible — this is a system-modification tool, not just a settings screen, so correctness and predictability come before UI polish.

## Requirements

### Validated

- ✓ Home page ported to WinUI 3 MVVM (landing/dashboard, matching current app's entry point) — Phase 1
- ✓ Akari OS Tweaks page ported: 32 registry-backed OS-level tweaks as toggle switches, instant apply with state feedback, including the two-phase Disable Defender workflow — Phase 1
- ✓ App runs elevated (`requireAdministrator` in app manifest), matching predecessor's privilege model — Phase 1
- ✓ App identity rebranded to "Akari Toolbox" (namespace, assembly name, manifest identity, icon/branding assets) — Phase 1
- ✓ Gaming Tweaks page ported (GAMING-01): 11 registry/service/subprocess-backed toggles (GPU HDCP/P0State/MSI Mode/AMD/Intel settings, device/network power savings, write-cache flush, IPv4-only, gaming power plan, high-precision timer resolution), 2 registry dropdowns (SvcHost split threshold, Win32 priority separation) with bounds-validated writes, 2 display-settings shortcuts, and the D-06 driver-tools action set (12 embedded network-dependent scripts, explicit no-signature-verification accepted risk) — all reflecting real system state with genuine prior-state revert — Phase 2
- ✓ Debloat page ported (DEBLOAT-01/02/03): 28 PowerShell-backed debloat actions across 5 categories, Run+Undo button pairs with live streamed output, logic in `DebloatViewModel`/`DebloatCatalog` not page code-behind — Phase 3. Initial execution shipped with 6 of 25 Undo scripts silently failing to reverse their paired Run script's real change (wrong registry key/hive/env-var/ACL target); caught by independent verification (not the initial `DebloatCatalogTests` suite, which only asserted resource wiring), closed by a gap-closure round (03-08) that fixed all 6 scripts and added `DebloatScriptRegressionTests` — a suite that reads real registry/env/ACL state before and after Run+Undo

### Active

- [ ] Downloads page ported: self-healing PostInstall asset fetcher (mirrors `C:\PostInstall\` from GitHub when missing), playbooks/drivers/utility links
- [ ] Misc page ported: 12 context-menu add/remove entries plus extra tools
- [ ] All ported pages use the WinUI-3-MVVM-Framework's DI, navigation, settings, theming, and dialog services rather than page-level code-behind logic (demonstrated for Home + Akari OS Tweaks in Phase 1, Gaming Tweaks in Phase 2, and Debloat in Phase 3; must hold through Phase 4)
- [ ] Visual style uses native WinUI 3 Fluent 2 controls and the framework's Mica backdrop/theming — the predecessor's WPF-UI custom theme (`Themes/Colors.xaml`, `Themes/Controls.xaml`) is not carried over (demonstrated in Phase 1 shell, held through Phase 2 and Phase 3; must hold through Phase 4)

### Out of Scope

- Deeper "Ultimate" tweak collection (~110 PowerShell scripts across Check/Refresh/Setup/Installers/Graphics/Windows/Hardware/Advanced categories, at `C:\Users\isleap\Desktop\AkariOS Tweaks`) — deferred to a v2 milestone; v1 is parity-first per explicit decision
- Any new tweaks/features beyond AkariOS Companion's current feature set — same reason, v1 = parity first
- GAMING-02 (predecessor's ~29-button NVIDIA/AMD/third-party quick-launch tool grid) — retired mid-Phase-2 per 02-CONTEXT.md D-11/D-12: the PostInstall asset mirror it depended on is deprecated project-wide with no replacement; the D-06 driver-tools buttons are explicitly not a revival of this grid

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
| Port to WinUI 3 on top of the existing WinUI-3-MVVM-Framework (AppTemplate) rather than a fresh solution | Framework already provides DI, navigation, settings, theming, dialogs, and logging — avoids re-solving MVVM plumbing | Shipped in Phase 1 — solution copied/renamed successfully, DI/nav/settings/theming/dialogs all reused without rework |
| v1 = feature parity with AkariOS Companion; deeper "Ultimate" tweak collection deferred to v2 | Ship a clean, correct port first before expanding scope | Phase 1 delivered Home + the full 32-tweak Akari OS Tweaks page at parity, including the Defender two-phase workflow |
| Rebrand from "AkariOS Companion" to "Akari Toolbox" | Intentional product/repo rename | Namespace/assembly/manifest/icon all rebranded in Phase 1 |
| Keep the admin-elevation requirement (`requireAdministrator`) | Same system-level operations (registry, services, Defender) as the predecessor | Confirmed working — UAC prompt + elevated launch verified via Phase 1 UAT |
| Drop the predecessor's WPF-UI custom theme entirely; use the framework's native WinUI 3 Fluent 2 look (Mica, default controls) | User explicitly wants the native WinUI 3 MVVM look, not a WPF-UI skin carried over | Confirmed via Phase 1 UAT — Mica backdrop renders, no WPF-UI theme files present |
| TWEAKS-02 (Defender two-phase workflow): keep the overall two-phase workflow shape (Tamper Protection gate, cab+ps1 install, post-reboot phase 2) as a direct carry-over, not decomposed into the ITweakHandler registry/service-primitive pattern | Decomposing Defender into the generic per-tweak pattern is out of scope for v1 (tracked as SEC-01, v2) | Workflow shape unchanged in Phase 1, but its internal *elevation mechanism* was replaced mid-phase — see next two rows |
| Replace Defender's elevation mechanism (MinSudo.exe/PowerRun.exe) with native SYSTEM impersonation (P/Invoke token duplication from winlogon.exe) | Explicit project-owner direction, closing a code-review finding (CR-01/CR-03): eliminates the unverified-elevated-binary-execution risk entirely rather than adding more SHA256 pins | Implemented in Phase 1, security-audited (01-SECURITY.md) — 1 new threat surfaced by the change (T-01-17, the headless `--defender-phase2` relaunch had no proof of legitimate scheduling) and closed with a single-use token gate; `threats_open: 0` |
| Remove Defender's PostInstall/`C:\PostInstall` dependency entirely; embed `NoDefender.cab`/`DisableDefender.ps1` as assembly resources | Explicit project-owner direction — Defender should have no runtime dependency on the downloaded PostInstall mirror | Implemented in Phase 1; `IPostInstallService`/`PostInstallService` remain in the codebase, unused, ahead of the Phase 4 Downloads-page asset mirror (DOWNLOADS-01) |
| Introduce `TweakCategory` (AkariOS, Gaming) as a discriminator on the existing flat `ITweakHandler`/`ITweakCatalog.Handlers` list, rather than changing the catalog interface | Lets Gaming Tweaks reuse Phase 1's `ITweakHandler`/`TweakCatalog` real-state-read/revert pattern exactly, with zero catalog-interface changes | Shipped in Phase 2 — 11 new Gaming handlers (Order 100-110) coexist with the 32 AkariOS handlers (Order 0-31), both category-filtered ordering invariants enforced by regression tests |
| Retire GAMING-02 (predecessor's ~29-button NVIDIA/AMD/third-party quick-launch tool grid) with no replacement | The PostInstall asset mirror it depended on is being deprecated project-wide; reviving it would add a dependency the project is actively removing | Retired 2026-09-01 per 02-CONTEXT.md D-11/D-12; REQUIREMENTS.md records the retirement, not a gap |
| D-06: port the 6 network-dependent driver-install scripts with their live download-and-execute behavior exactly as authored, with no added SHA256/signature verification for v1 | Explicit, deliberate project-owner decision — v1 is parity-first with the predecessor's own accepted risk, not a new gap introduced by the port | Implemented in Phase 2; the accepted risk is surfaced both in a pre-launch `ILogConsoleService` log line (fires before every one of the 12 buttons runs) and in the page's own "not integrity-verified" UI section header — documented as an accepted risk in `02-SECURITY.md`, not silently hidden |
| Harden `PowerPlanTweakHandler`'s revert beyond what the source script does (session-scoped `powercfg -export`/`-import` instead of the source's destructive `-restoredefaultschemes`) | GAMING-01 requires "the same real-state and revert behavior as Tweaks," which the source script's own revert cannot satisfy as written | Implemented in Phase 2; a post-execution code review (CR-01) found the initial implementation discarded `powercfg` exit codes, letting a failed export still proceed to delete — fixed to verify every export/duplicate/setactive exit code (and on-disk file existence) before any delete runs, independently re-verified by both the phase verifier and the security auditor |
| Propagate a fault (throw) from `DefenderTweakHandler`'s re-enable path when native SYSTEM-level service restoration fails, instead of silently clearing the app's own "Defender disabled" state flag | A post-execution code review (CR-02) found the toggle could report Defender as protecting the system again even when the underlying service restore failed — a false-assurance bug in a security-relevant control, directly contradicting the project's "report accurate state" core value | Implemented in Phase 2; the fault now reaches the existing `OnTweakItemPropertyChanged` real-state-correction path instead of being swallowed by the generic catch-all |
| Debloat's Run+Undo model has no live-state read-back by design (D-01, 03-CONTEXT.md) — action-log parity with the predecessor rather than an `ITweakHandler`-style toggle | The predecessor's Debloat model never had real "is this applied" state (`_applied` in `ToolService` is write-only); several actions (Disk Cleanup, Temp Files) are one-shot side effects with no meaningful on/off state | Implemented in Phase 3; means Debloat's correctness proof has to come from the Undo scripts' own target-accuracy, not a state read-back — exactly the property that broke silently until independently verified (see next row) |
| Independently re-verify Undo scripts by diffing each against its paired Run script's actual writes, rather than trusting that a passing `DebloatCatalogTests` suite (resource-wiring assertions only) proves Undo correctness | Phase 3's first verification pass found 6 of 25 Undo scripts targeted the wrong registry key/hive/env-var/ACL — a class of bug invisible to any test that only checks "the script resolves and runs," never "the script changes the right thing" | Closed via gap-closure plan 03-08 + a second code-review pass that caught 3 further Critical bugs in the *fix's own* new regression-test suite (a false-Skip-as-Pass elevation guard, incomplete `finally`-block state restoration) — the lesson: a regression suite added to prove a fix is itself review-worthy, not exempt because it "adds tests" |

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
*Last updated: 2026-09-02 after Phase 3*
