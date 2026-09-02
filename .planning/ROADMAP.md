# Roadmap: Akari Toolbox

## Overview

Akari Toolbox is a ground-up port of AkariOS Companion from WPF to native WinUI 3 MVVM, built on the user's own WinUI-3-MVVM-Framework template. The roadmap ships as four vertical slices, each a working, launchable end-to-end capability rather than a horizontal technical layer. Phase 1 stands up the new solution (copied/renamed from the framework template), enables elevation, and delivers Home + the full Akari OS Tweaks page (32 toggles + the two-phase Defender workflow) — this is also where every foundational primitive the rest of the app depends on (registry/service/script-runner primitives, the elevated picker replacement, and the cross-thread dispatcher/marshaling pattern) gets built and proven, because Tweaks is the smallest page that exercises the full primitive stack. Phases 2-4 each reuse that foundation to ship one more complete page — Gaming Tweaks, Debloat, then Downloads + Misc together — closing out v1 feature parity with the predecessor.

## Phases

**Phase Numbering:**

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [x] **Phase 1: Foundation & Akari OS Tweaks** - New elevated WinUI 3 solution (copied from the framework template, rebranded), Home dashboard, and the full 32-toggle Akari OS Tweaks page with real-state revert and the two-phase Defender workflow (completed 2026-09-01)
- [x] **Phase 2: Gaming Tweaks** - Gaming/latency toggles and service-config dropdowns, reusing the Phase 1 tweak pattern (completed 2026-09-01)
- [x] **Phase 3: Debloat** - 28 PowerShell-backed debloat actions with streamed live output, driven by proper ViewModel/service architecture (completed 2026-09-02)
- [ ] **Phase 4: Downloads & Misc** - Self-healing PostInstall asset mirror, playbooks/drivers/links, and classic context-menu management

## Phase Details

### Phase 1: Foundation & Akari OS Tweaks

**Goal**: Users can launch the rebranded "Akari Toolbox" app — running elevated, built on the copied/renamed WinUI-3-MVVM-Framework solution — see a Home dashboard, and fully operate the Akari OS Tweaks page (32 registry/service-backed toggles plus the two-phase Defender-disable workflow) with toggles that reflect real system state and revert to the real prior state, not a cached or default value.
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: APP-01, APP-02, APP-03, APP-04, APP-05, HOME-01, TWEAKS-01, TWEAKS-02, TWEAKS-03
**Success Criteria** (what must be TRUE):

  1. App launches under administrator elevation with the "Akari Toolbox" identity (namespace/assembly/manifest/icon/branding), built on the copied WinUI-3-MVVM-Framework solution
  2. User sees a Home dashboard on launch listing the available tool categories
  3. User can view all 32 Akari OS Tweaks as toggles reflecting actual current system state, and turning a tweak off restores the real prior state the app recorded before mutating it (not a hardcoded default)
  4. User can complete the two-phase guided Defender-disable workflow (tamper protection phase, then real-time protection phase) with explicit warnings at each phase, ported as a direct carry-over of the predecessor's existing logic
  5. The app shell uses native WinUI 3 Fluent 2 controls and Mica backdrop/theming (no WPF-UI theme carried over), background operations (tweak state reads, async callbacks) update the UI without cross-thread crashes, and file/folder picker operations work correctly while the app runs elevated

**Plans:** 7/7 plans complete
Plans:
**Wave 1**

- [x] 01-01-PLAN.md — Walking Skeleton tracer: copy/rename framework solution, elevate, wire WiFi tweak end-to-end; full Home dashboard + nav sidebar

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 01-02-PLAN.md — System primitives: ILogConsoleService (collapsible dock), IWindowsServiceController, IScriptRunner

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 01-03-PLAN.md — Elevation-safe file/folder picker (Microsoft.Windows.Storage.Pickers) + debug smoke test
- [x] 01-04-PLAN.md — 22 registry-only tweak handlers (batches A/B)
- [x] 01-05-PLAN.md — 8 service-backed + bcdedit/DISM-hybrid tweak handlers
- [x] 01-06-PLAN.md — Defender two-phase disable workflow (byte-for-byte port) + minimal PostInstallService

**Wave 4** *(blocked on Wave 3 completion)*

- [x] 01-07-PLAN.md — Final integration: ordering regression test, error resilience, full 32-tweak verification pass

**UI hint**: yes

### Phase 2: Gaming Tweaks

**Goal**: Users can tune gaming/latency system settings from one page, reusing the tweak pattern (real-state read, prior-state revert) established in Phase 1.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: GAMING-01
**Success Criteria** (what must be TRUE):

  1. User can view and toggle gaming/latency/service tweaks (SvcHost split threshold, Win32 priority separation, service configuration dropdowns) with toggles reflecting actual current system state
  2. Turning a gaming tweak off restores the real prior state the app recorded before mutating it, matching the Tweaks page guarantee

**Note:** GAMING-02 (third-party tool launcher grid) is retired per 02-CONTEXT.md D-11/D-12 — the PostInstall asset mirror it depended on is deprecated project-wide with no replacement. See REQUIREMENTS.md for the retirement record.

**Plans:** 7/7 plans complete
Plans:
**Wave 1**

- [x] 02-01-PLAN.md — Tracer: TweakCategory discriminator + Hdcp toggle end-to-end + RunEmbeddedScriptAsync primitive + P0State/MsiMode handlers

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 02-02-PLAN.md — AmdSettings/IntelSettings toggle handlers + D-05 display-settings shortcuts
- [x] 02-03-PLAN.md — DevicePowerSavings/NetAdapterPowerSavings/WriteCacheFlush toggle handlers

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 02-04-PLAN.md — NetworkIpv4Only/PowerPlan (hardened revert)/TimerResolution toggle handlers
- [x] 02-05-PLAN.md — SvcHost + Win32PrioritySeparation dropdowns (preset-list approval checkpoint)

**Wave 4** *(blocked on Wave 3 completion)*

- [x] 02-06-PLAN.md — 6 D-06 network-dependent driver/tool install actions (embedded scripts)

**Wave 5** *(blocked on Wave 4 completion)*

- [x] 02-07-PLAN.md — Final integration: Gaming-scoped ordering regression test, full-catalog verification pass

**UI hint**: yes

### Phase 3: Debloat

**Goal**: Users can run the predecessor's 28 PowerShell-backed debloat actions with live streamed feedback, with the page's logic living in a ViewModel/service rather than code-behind.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: DEBLOAT-01, DEBLOAT-02, DEBLOAT-03
**Success Criteria** (what must be TRUE):

  1. User can run each of the 28 PowerShell-backed debloat actions from the Debloat page
  2. User sees streamed status/output feedback while a debloat action runs, without the UI freezing or crashing
  3. Debloat page logic lives in a ViewModel/service, not in page code-behind (the predecessor's `DebloatPage.xaml.cs` pattern is not carried over)

**Plans:** 8/8 plans complete
Plans:
**Wave 1**

- [x] 03-01-PLAN.md — Tracer: Debloat architecture (catalog/ViewModel/page/DI/nav) + "Telemetry — Disable" wired end-to-end

**Wave 2** *(blocked on Wave 1 completion)*

- [x] 03-02-PLAN.md — Complete Privacy & Telemetry category (7 remaining actions)

**Wave 3** *(blocked on Wave 2 completion)*

- [x] 03-03-PLAN.md — Complete System & Performance category (8 actions, incl. BitLocker/Hibernation confirmation)

**Wave 4** *(blocked on Wave 3 completion)*

- [x] 03-04-PLAN.md — Complete Explorer & UI (5 actions) + Tools (1 action) categories

**Wave 5** *(blocked on Wave 4 completion)*

- [x] 03-05-PLAN.md — Cleanup direct carries: Disk Cleanup, Temp Files, OneDrive Remove

**Wave 6** *(blocked on Wave 5 completion)*

- [x] 03-06-PLAN.md — Cleanup replacements: Bloatware/Edge & WebView/Edge Settings branch extraction (D-03/D-06/D-07/D-08)

**Wave 7** *(blocked on Wave 6 completion)*

- [x] 03-07-PLAN.md — Final integration: per-row risk captions, full-catalog regression lock

**Wave 8** *(gap closure — 03-VERIFICATION.md, blocked on Wave 7 completion)*

- [x] 03-08-PLAN.md — Fix 6 broken Undo scripts (CR-01..CR-06: locationtracking/consumerfeatures/storesearch/ps7telemetry/wpbt/folderdiscovery) + storesearch confirmation gate + live registry/env/ACL regression tests

**UI hint**: yes

### Phase 4: Downloads & Misc

**Goal**: Users can rely on a self-healing PostInstall asset mirror, browse playbooks/drivers/utility links, and manage classic Windows context-menu entries plus extra misc tools.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: DOWNLOADS-01, DOWNLOADS-02, MISC-01, MISC-02
**Success Criteria** (what must be TRUE):

  1. On first use, the app automatically downloads and mirrors the PostInstall asset folder from GitHub to `C:\PostInstall\` if it's not already present, and is a no-op if it is
  2. User can access playbooks, drivers, and recommended utility links from the Downloads page
  3. User can add or remove each of the 12 context-menu entries (classic/legacy Windows context menu)
  4. User can access the extra misc tools from the Misc page

**Plans**: TBD
**UI hint**: yes

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Foundation & Akari OS Tweaks | 7/7 | Complete    | 2026-09-01 |
| 2. Gaming Tweaks | 7/7 | Complete    | 2026-09-01 |
| 3. Debloat | 8/8 | Complete    | 2026-09-02 |
| 4. Downloads & Misc | 0/TBD | Not started | - |
