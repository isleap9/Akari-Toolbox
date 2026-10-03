# Project Research Summary

**Project:** Akari Toolbox — Windows gaming optimization push
**Domain:** Windows gaming optimization desktop tool (WinUI 3, native registry/service/driver/WMI ops, tweak catalog + rollback)
**Researched:** 2026-10-03
**Confidence:** HIGH

## Executive Summary

Akari Toolbox is a WinUI 3 native-ops gaming optimizer competing in the Winhance / WinUtil / Fr33thy-tweak-set space. Experts in this domain all converge on the same shape: a searchable, categorized tweak catalog where every row is explained (what / why / risk / revert), mutations go through auditable native APIs only (registry, service control, power/display P/Invoke — never scripts or bundled binaries), and bulk-apply is paired with per-value rollback. The enthusiast user does not trust one-click "boost" buttons; trust comes from showing the registry path, current vs. recommended value, and a working revert.

The recommended approach is to **extend, not rebuild**: keep the pinned .NET 10 + Windows App SDK 2.3.1 + 13-page shell exactly as-is, add only two packages (CsWin32 for type-safe P/Invoke, Toolkit SettingsControls for Winhance-style rows), and do the gaming push as domain-layer work — a `GamingTweakCatalog` descriptor list, missing `*Actions` native methods, a snapshot-before-apply `RollbackStore` journal, a small `BulkApplyRunner`, and finally the GamingPage browse rework on top. Catalog schema first (with build-range gating + verify-read + reboot flags), native actions second, journal/runner third, UI last — building UI first forces mock data that drifts from the real catalog.

The key risks are shipping folklore as features (bcdedit timer myths, placebo network keys, service massacres, Store/WebView removal) and treating a System Restore point as rollback. Mitigation is mechanical: an explicit timer stance (TSC defaults, no `useplatformclock`/`useplatformtick` optimizations), a never-disable service allowlist, a debloat keep-list with reinstall paths, per-adapter network snapshots with read-back verification, hardware-gated power/GPU presets, and a two-layer safety model (restore point as backstop + per-tweak JSON value journal as the real revert) with read-back-after-write on every tweak.

## Key Findings

### Recommended Stack

Stay on the pinned stack; add exactly two packages. .NET 10 (`net10.0-windows10.0.26100.0`) + Windows App SDK 2.3.1 is verified working with zero warnings — upgrading to WASDK 2.4/2.5 buys nothing the tweak engine needs and forces a full 13-page regression. Central package management is on; add versions in `Directory.Packages.props` only. Full detail: `STACK.md`.

**Core technologies:**
- .NET 10.0.x SDK / `net10.0-windows10.0.26100.0` — runtime + TFM for whole app — current release, matches pinned BuildTools.
- Windows App SDK / WinUI 3 2.3.1 (pinned, stay) — NavigationView, Mica, InfoBar, AutoSuggestBox — verified shell; 2.5.x features irrelevant to tweaks, defer upgrade to its own phase.
- Microsoft.Windows.SDK.BuildTools 10.0.26100.4654 — Win32/WinRT projections — keep in lockstep with TFM.
- C# 13 + CommunityToolkit.Mvvm 8.4.2 — source-generated VMs for all pages + new browse VM — latest stable, zero-cost, maps directly to search/filter/selection.
- Microsoft.Windows.CsWin32 0.3.335 (**new**) — source-generated P/Invoke for powrprof/advapi32/display/`NtSetTimerResolution` — Microsoft-recommended replacement for hand-written `[DllImport]`; minimal `NativeMethods.txt` allowlist.
- CommunityToolkit.WinUI.Controls.SettingsControls 8.2.251219 (**new**) — `SettingsCard`/`SettingsExpander` rows — exactly the Winhance-style title+description+state+info row; do not hand-roll.
- Microsoft.Extensions.Hosting/DI/Logging/Configuration.Json 10.0.11 — register new `TweakCatalogService`, `TweakExecutionService`, `RollbackJournalService`, `HardwareSurveyService` in existing host.
- System.Management 10.0.12 (read-only WMI) — GPU/driver/CPU inventory — keep disposal discipline; never `Put()`.
- xunit 2.9.3 + runner + Test.Sdk + Moq (stay) — catalog-integrity, filter/search, journal round-trip tests — do NOT migrate to xUnit v3 mid-milestone.

**Hard avoids:** script/`.exe`/`.reg` payloads, NVAPI/vendor SDKs, whole-hive tweak packs, always-on timer resolution, Update/Store/driver disables, page-level `x:Bind` (WMC9999), UWP-era Toolkit packages.

### Expected Features

Converged from Winhance docs, Fr33thy sets, FPSBoostPro/mojo/toolkit inventories. Full landscape + dependency graph: `FEATURES.md`.

**Must have (table stakes):**
- Searchable gaming tweak catalog with categories — users expect search, not page-hunt (169-row base exists; add search + filter).
- Per-tweak explanation + technical details (registry path, current vs recommended, reboot flag, Reversible=Yes) — cheap, huge trust payoff.
- Game Mode ON + Game DVR / background-recording OFF + power-plan control incl. Ultimate unlock — highest-signal toggles.
- HAGS + fullscreen-optimizations + flip-model/VRR + mouse-precision-off + visual-effects trim — expected graphics/input checklist.
- Background/startup control + conservative service allow-list — clean-background path without breaking Windows.
- Debloat with protected-app guard (Store/Terminal/Xbox-if-Game-Pass-user safe) + provisioned-level removal.
- Telemetry / advertising-ID / suggestions lockdown — expected hygiene, safe GPO-layer keys.
- Network latency path (per-adapter Nagle, throttling off, auto-tuning Normal, DNS flush) with Wi-Fi vs Ethernet clarity.
- Bulk apply with progress + per-value snapshot + one-click rollback + restore point — the v1 done bar.
- What-changed report / exportable log — proof of work.
- Driver guidance (staleness check + vendor-panel deep links; guide, don't bundle installers).

**Should have (competitive):**
- Gaming browse model (Winhance-style gaming-only scope) — the project's core bet, headline differentiator.
- Trusted-control bar (live current-value read-back per row) — Winhance Technical Details parity.
- Game profiles / presets (Competitive / Balanced / Streaming) + JSON export/import — one tap per context.
- Before/after performance snapshot (ping, startup weight, FPS advice with honest 5–15% ranges) — counters placebo criticism.
- Read-only diagnostics scan (crash history, BSOD decode, SMART, XMP/EXPO check) — triages "my FPS is low" before tweaking.
- GPU-vendor-aware rows (ReBAR, MSI-mode, Low-Latency/Anti-Lag guidance) + SSD/HDD-aware storage tuning — expert feel, cheap logic.

**Defer (v2+):**
- Auto gaming mode (launch watcher with crash-safe revert) — HIGH complexity, background-process trust cost.
- Game-library detection (Steam/Epic scan → per-title suggestions) — needs launcher parsing + title DB.
- Scheduled maintenance task — opt-in hygiene, no urgency.
- Timer-depth Advanced tier (0.5ms lock, core-parking, EcoQoS) — reboot-sensitive, needs diagnostics maturity.
- Explicitly never build: Defender/VBS/mitigation disables, custom ISOs, Update disable, mass service massacres, bundled installers, resident "RAM booster".

### Architecture Approach

The 4-layer shape (Shell composition root → thin Views + stateful VMs → static UI-free domain → Windows OS) already exists; the gaming push extends the domain catalog and browse/runner layers, not the shell. Direction `Views → ViewModels → Tweaks → OS` is a hard rule. Full patterns + data flow: `ARCHITECTURE.md`.

**Major components:**
1. `Tweaks/GamingTweakCatalog.cs` (NEW) — one record per tweak (id, category, title, description, risk/reboot/build-range, read/apply/revert delegates) — single source of truth enabling search, grouping, bulk-apply.
2. `Tweaks/*Actions.cs` extensions (EXTEND) — all native mutations (registry, service Start, powercfg/sc/netsh, WMI reads) — VM/catalog only call; never duplicate logic.
3. `Tweaks/TweakSnapshot.cs` + `Tweaks/RollbackStore.cs` (NEW) — pre-apply capture of every value/service-start/plan-GUID (incl. "absent"), persisted journal; revert replays in reverse — the real reversibility mechanism.
4. `Tweaks/BulkApplyRunner.cs` (NEW) — ordered off-UI-thread apply, per-item ok/fail capture, continue-on-error, `IProgress<T>`, cancellation — one generic runner, never per-page loops.
5. `Views/GamingPage.xaml` + `ViewModels/GamingViewModel.cs` + `GamingTweakRow.cs` (REWORK) — search box + grouped virtualized list + bulk bar, page-level `{Binding}` (`x:Bind` only in DataTemplates), `IsBusy` guard + `IStatusService`/`IInfoBarService`/`IDialogService` orchestration — reuse existing framework services, no new framework abstractions.

### Critical Pitfalls

Top 7 from `PITFALLS.md` (all HIGH confidence except ordering MEDIUM):

1. **bcdedit timer myths shipped as gains** — `useplatformclock`/`useplatformtick`/HPET-disable tank real perf while looking like gains — default to TSC-deletevalues; any timer row is legacy-game-gated with warning + TSC revert, never in "max performance" presets.
2. **Service-disable presets break Windows** — Update/Store/Xbox/controller/anti-cheat collateral — maintain never-disable allowlist (Update stack, Store/ClipSVC, DCOM/RPC, Defender core, Gaming Services), snapshot prior StartType per service, UAT must include Update check + Store install + controller connect + reboot.
3. **Debloat breaks games/updates** — Store/VCLibs/WebView2/Xbox-Identity removal is HIGH-cost recovery — never remove platform runtimes; per-entry reinstall path recorded at apply time; keep-list vs remove-list review before code.
4. **Placebo network tweaks** — all-interfaces Nagle loops, auto-tuning off harming throughput — resolve active gaming adapter first, snapshot per-GUID, verify via `netsh`/`Get-NetTCPSetting` read-back, honest single-digit-ms copy.
5. **Power/GPU without hardware guards** — one preset fries laptops/iGPU/hybrid — gate on chassis type + GPU vendor + driver floor; export-before-write GPU profiles; driver flows guided-only, no in-app DDU.
6. **"Restore point = rollback" illusion** — stalls, disabled restore, coarse whole-system revert — two-layer safety mandatory: journal-first per-tweak replay + restore point backstop with timeout/disabled detection; bulk must be cancellable with partial-failure undo.
7. **Build-fragile registry paths (Win10 vs 24H2+ drift)** — silent no-ops shown as "Applied" — every catalog row carries MinBuild/MaxBuild + verify-read; write-then-read-back-compare; test matrix Win10 22H2 + Win11 23H2/24H2; paths centralized in catalog, never in VMs.

Also: bulk-apply on UI thread freezes; explanations as afterthought strings rejected by enthusiasts; privileged `NativeOps` must be allowlisted to catalog paths with per-write tweak-ID logging.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Gaming browse model + catalog schema
**Rationale:** ARCHITECTURE build-order step 1 and PITFALLS build-drift gate — the catalog schema (build range, verify-read, reboot flag, explanation fields) is inherited by every later phase; UI rework stays on the existing 13-page nav contract so `verify-pages.ps1` stays green.
**Delivers:** `GamingTweakCatalog` descriptor list (gaming-filtered view over 169 rows + new rows), category/search LINQ filtering, `SettingsCard`/`SettingsExpander` row templates, CsWin32 + SettingsControls package adds, per-tweak explanation content requirement enforced.
**Addresses:** Searchable catalog + categories, per-tweak explanations + technical details (P1).
**Avoids:** Build-fragile paths pitfall; Anti-Pattern per-keystroke live reads (filter in-memory, `Read()` only on navigate/after-apply) and new-framework-abstraction bloat.

### Phase 2: Core gaming toggles (system + graphics + input)
**Rationale:** Lowest-cost, highest-signal table stakes; each native method independently testable; fills the `Apply`/`Revert` the catalog needs before any bulk work.
**Delivers:** Game Mode/DVR, power-plan control + Ultimate unlock (prior GUID capture), HAGS, FSO/flip/VRR, mouse-precision, visual-effects trim — all with read/apply/revert + reboot flags + hardware/build gates (HAGS RTX20+/RX5000+, VRR monitor note).
**Addresses:** Game Mode/DVR/power, HAGS/FSO/flip/VRR/mouse/visuals (P1).
**Avoids:** Timer-myth pitfall (decide timer stance here: TSC defaults, no optimization toggles); power-without-guards pitfall; WMI disposal discipline.
**Uses:** CsWin32 P/Invoke + `Microsoft.Win32.Registry`; `System.Management` reads behind `Task.Run`.

### Phase 3: Trusted-control v1 bar (snapshot journal + bulk apply + rollback + report)
**Rationale:** FEATURES dependency backbone — profiles, proof, and every preset require snapshot store + restore point first; PITFALLS mandates journal-before-first-preset, not after.
**Delivers:** `TweakSnapshot` + `RollbackStore` (JSON journal in `%LOCALAPPDATA%\AkariToolbox`), `BulkApplyRunner` (async, progress, cancel-between-tweaks, per-item results), restore-point-before-bulk with timeout/disabled handling, crash-recovery replay prompt, what-changed report/log export, single-tweak revert + mid-preset-cancel UAT.
**Addresses:** Bulk apply + snapshot + rollback + restore point, what-changed report (P1).
**Avoids:** Restore-point-only illusion; bulk-on-UI-thread freeze; unbounded-log bloat (keep rolling-log caps + journal rotation).

### Phase 4: Clean-system path (services + background/startup + debloat + telemetry)
**Rationale:** Highest support-load risk concentrated in one phase so the allowlist/guard review happens once; depends on Phase 3 journal (every service/disable must snapshot).
**Delivers:** Conservative service allow-list with prior-StartType restore, startup Run-keys/folder + Store-task control, provisioned debloat with protected-app guard + stored reinstall commands, telemetry/ads/suggestions lockdown; UAT exit criteria: Update check + Store install + controller connect + reboot.
**Addresses:** Background/startup + services, debloat + guard, telemetry lockdown (P1).
**Avoids:** Service-breakage + AppX-removal pitfalls; Defender-engine-off and Update-disable anti-features (exclusions/pause instead).

### Phase 5: Gaming network path
**Rationale:** Self-contained per-adapter vertical; needs Phase 3 journal already in place; honest-copy review is a deliverable, not polish.
**Delivers:** Active-adapter picker (GUID shown), per-GUID Nagle snapshot, NetworkThrottlingIndex/SystemResponsiveness as explained profiles, auto-tuning level control + DNS change with old-server record, `netsh`/`Get-NetTCPSetting` read-back in UI.
**Addresses:** Network latency path (P1).
**Avoids:** Placebo-network pitfall (no all-GUID loops, no blind auto-tuning-off).

### Phase 6 (v1.x): Profiles, proof, and smarts
**Rationale:** All require the v1 bar working — profiles are named bulk-sets, snapshots enhance bulk-apply, diagnostics/vendors build on the survey layer.
**Delivers:** Game profiles (Competitive/Balanced/Streaming) + JSON export/import, before/after snapshot card, read-only diagnostics scan (crashes/BSOD/SMART/XMP), GPU-vendor-aware rows + driver-staleness check, SSD/HDD-aware storage tuning.
**Addresses:** All P2 features. P3 (auto mode, library detection, scheduled task, timer-depth tier) stays v2+.

### Phase Ordering Rationale

- **Dependencies first:** catalog schema → native actions → journal/runner → presets/profiles/auto-mode (FEATURES dependency graph); UI last per ARCHITECTURE build order so no mock drift.
- **Risk concentration:** timer stance decided in Phase 2 planning before code; service/debloat guards reviewed once in Phase 4; network adapter-scoping in Phase 5; journal before any phase that mutates the system (Phase 3 precedes 4–5).
- **Architecture grouping:** domain-layer work (catalog/actions/journal/runner) clusters in Phases 1–3; presentation rework rides on top; framework untouched throughout.
- **Pitfall avoidance:** each phase owns its named pitfall's verification (timer review, Update/Store/controller UAT, reinstall-path audit, netsh read-back, hardware-gate matrix, journal round-trip) so "looks done but isn't" items become exit criteria.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 2 (core toggles):** exact registry/policy key per tweak + build-range applicability (Win10 22H2 vs 23H2 vs 24H2 drift) — keys move between builds; verify each with read-back. Use `/gsd-plan-phase --research-phase`.
- **Phase 4 (clean-system):** keep-list vs remove-list adjudication (which AppX/services are safe) + reinstall paths — community consensus varies; needs doc verification. Use `/gsd-plan-phase --research-phase`.
- **Phase 5 (network):** per-adapter enumeration + safe auto-tuning/DNS levels on current builds — Win7-era guides are stale. Use `/gsd-plan-phase --research-phase`.
- **Phase 6 (diagnostics/vendor):** SMART/EventLog/WMI query shapes + vendor-profile store formats — niche APIs, sparse WinUI-specific docs. Use `/gsd-plan-phase --research-phase`.

Phases with standard patterns (skip research-phase):
- **Phase 1 (browse model):** AutoSuggestBox + CollectionViewSource filtering + virtualized list + Toolkit SettingsCards — well-documented WinUI patterns; ARCHITECTURE gives the exact VM shape (`CheckViewModel.RunAsync` precedent).
- **Phase 3 (journal/runner):** snapshot-record + reverse-replay + `IProgress<T>` background runner — standard pattern fully specified in ARCHITECTURE Patterns 2–3; only schema naming is project-specific.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Pinned versions read from repo + NuGet latest-stable checks + Learn docs for CsWin32; only WASDK-2.5 timing is time-sensitive. |
| Features | HIGH | Winhance official docs + widely replicated tweak sets converge on the checklist; MEDIUM only on measured-gain numbers (hardware-dependent, creator-measured). |
| Architecture | HIGH | Grounded in live codebase map + direct `Tweaks/` source reads; precedent patterns (AkariTweakCatalog, TweaksViewModel rows) are first-party. |
| Pitfalls | HIGH | Timer/service findings cross-checked across enthusiast + technical sources; MEDIUM only on exact phase ordering (roadmap not yet written at research time). |

**Overall confidence:** HIGH

### Gaps to Address

- **Measured-gain honesty ranges:** 5–15% CPU-bound FPS / 10–30ms input-lag figures are community-benchmark aggregates, not controlled tests — handle by shipping honest "up to / depends on hardware" copy and the Phase 6 before/after snapshot rather than promises; validate copy in plan review.
- **Build-key drift on 24H2+:** several gaming-relevant defaults changed in 24H2; target SDK is 26100 so 24H2 is the reference, but Win10 22H2 + 23H2 rows need per-key verification during Phase 2/4 planning — handle via MinBuild/MaxBuild + read-back verify fields in the catalog schema.
- **WASDK servicing timeline:** 2.3.1-serviced vs 2.5.x-current status will age — handle by scheduling the SDK upgrade as its own future phase with full regression, never bundled with gaming work.
- **Contributor toolchain:** VS 2022 cannot target .NET 10 — handle by documenting VS 2026 + SDK 10.0.401 requirement in CONTRIBUTING during Phase 1.

## Sources

### Primary (HIGH confidence)
- Repo ground truth — `Directory.Packages.props` pins, `Tweaks/*Actions`, `AkariTweakCatalog`, `RestorePointActions`, `SystemInfo` WMI disposal note, PROJECT.md constraints — stack + architecture + safety rails.
- `.planning/codebase/ARCHITECTURE.md` + `STRUCTURE.md` (2026-10-03 mapping) + `TweakAction.cs` / `TweakToggle.cs` / `TweaksViewModel.cs` source reads — component boundaries.
- Microsoft Learn — "Call Win32 APIs from a C# Windows app (CsWin32)"; Windows App SDK downloads (2.5.1 stable 09/2026); AutoSuggestBox guidelines; .NET 10 SDK 10.0.401.
- NuGet — CommunityToolkit.Mvvm 8.4.2, WinUI.Controls.SettingsControls 8.2.251219, CsWin32 0.3.335, System.Management 10.0.12 (latest-stable checks).
- Winhance official docs (Optimizations index, Gaming & Performance, Power Management, Quick Start, Configuration Files) — browse model + per-tweak-docs bar.
- Melody's timer misconceptions guide (TSC/HPET/PMT/RTC, `useplatformclock` harm, anticheat skew) + WinUtil FAQ revert model.

### Secondary (MEDIUM confidence)
- FR33THY Ultimate / WinSux / debunk repos + walkthrough (tweak inventory HIGH, gain claims MEDIUM); win11-gaming-toolkit tier + manifest-revert model; FPSBoostPro tabs + Restore Center + roadmap; mojo presets + before/after + game-detection roadmap; PitCrew Diagnose + provisioned debloat; SpeedGuide + Nagle threads; Defender-disable incident writeup; DDU Safe-Mode threads; TechPowerUp/Overclock.net/Blur Busters timer benches.

### Tertiary (LOW confidence)
- enesehs-windows-optimizer + AIO-Ultimate-System-Optimizer READMEs — corroborating checklist convergence only; needs per-key validation during planning.

---
*Research completed: 2026-10-03*
*Ready for roadmap: yes*
