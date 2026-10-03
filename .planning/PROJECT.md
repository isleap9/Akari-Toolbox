# Akari Toolbox — Gaming Optimizer Push

## What This Is

Akari Toolbox is a WinUI 3 desktop app that applies the FR33THY Ultimate Windows optimization set through native registry / service / driver / WMI operations (no scripts or bundled binaries). This project pushes it further: a Winhance-style, gaming-focused optimizer — searchable categories with per-tweak explanations — built to be the most powerful safe Windows gaming optimization tool.

## Core Value

Gamers get measurably smoother, lower-latency play from a one-pass safe tune where every tweak is explained, visible, and reversible.

## Requirements

### Validated

- ✓ 13-page WinUI 3 shell (Home + 11 tweak destinations + Settings) with NavigationView, Mica, InfoBar, status footer — existing
- ✓ Native tweak engine (TweakAction / TweakToggle, per-page *Actions modules, 169-row Individual Tweaks catalog) — existing
- ✓ Fr33thy Ultimate baseline coverage (Akari OS, Gaming, Check/Refresh/Setup/Installers/Graphics/Windows/Hardware/Advanced flows) — existing
- ✓ Safety rails (restore point, desktop shortcut, %LOCALAPPDATA%\AkariToolbox settings + rolling logs, single-instance, admin manifest, safe-boot headless flows) — existing
- ✓ Theme + language settings with immediate apply — existing

### Active

- [ ] Winhance-style gaming browse model (searchable list, categories, per-tweak info)
- [ ] Max-performance system tuning (CPU / timer / power / Game Mode) with explained, reversible toggles
- [ ] Clean-system gaming path (debloat, background services, telemetry, background apps) without breaking Windows
- [ ] Gaming network path (adapter, DNS, throttling fixes) with before/after clarity
- [ ] GPU tuning depth (driver flows, display / scaling / profile handling) kept safe and undoable
- [ ] Trusted-control v1 bar: every gaming tweak explained, bulk-apply with progress, full rollback

### Out of Scope

- Non-Windows platforms — Windows 10/11 x64 only, by native-ops design
- Script / bundled-binary execution — architecture forbids it, stays forbidden
- Aggressive irreversible tweaks — rejected: deeper-but-safe only, fully reversible line
- Casual one-click-only UX — rejected: primary user is the enthusiast who wants control

## Context

- Brownfield: `src/AppTemplate.App/` (Views + ViewModels × 13, Tweaks/*Actions, Helpers, Services), `src/AppTemplate.Framework/` (MVVM shell), `src/AppTemplate.Tests/`. See `.planning/codebase/` map.
- Current nav order: Home, Akari OS Tweaks, Gaming Tweaks, 1 Check, 2 Refresh, 3 Setup, 4 Installers, 5 Graphics, 6 Windows, 7 Hardware, 8 Advanced, Individual Tweaks, Settings (footer).
- Gaming page + Graphics page + Advanced services/Defender flows are the natural expansion seams for the gaming push.
- Reference model: Winhance browse UX (search, categories, per-tweak descriptions) applied to gaming optimization only.

## Constraints

- **Platform**: Windows 10/11 x64, administrator required — every tweak writes HKLM / services / drivers
- **Packaging**: Unpackaged (`WindowsPackageType=None`), self-contained (`WindowsAppSDKSelfContained=true`)
- **Stack**: .NET 10 (`net10.0-windows10.0.26100.0`), Windows App SDK 2.3.1, CommunityToolkit.Mvvm 8.4.2 — versions pinned in `Directory.Packages.props`
- **Safety**: Fully reversible line — restore point + per-tweak rollback; never break updates, Store, or core drivers
- **Native ops only**: No scripts or bundled binaries ever executed
- **Build bar**: `Errors: 0 Warnings: 0`; kill `AkariToolbox.App` before rebuild (apphost lock)
- **XAML**: Page-level `{Binding}` (page-level x:Bind dies with WMC9999); x:Bind only in DataTemplate; `SystemInfo` WMI reads must dispose collections + objects

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Deeper-but-safe over more-aggressive | User wants power without breaking trust | — Pending |
| Fully reversible as the hard line | Enthusiasts experiment; rollback keeps them safe | — Pending |
| Enthusiasts as primary user | Per-tweak control + explanations over one-click | — Pending |
| Winhance browse model for gaming | Searchable categories + per-tweak info, gaming-only scope | — Pending |
| Trusted control = v1 done | Every tweak explained and undoable beats raw count | — Pending |

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
*Last updated: 2026-10-03 after initialization*
