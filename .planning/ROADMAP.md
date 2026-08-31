# Roadmap: Akari Toolbox

## Overview

Akari Toolbox is a ground-up port of AkariOS Companion from WPF to native WinUI 3 MVVM, built on the user's own WinUI-3-MVVM-Framework template. The roadmap ships as four vertical slices, each a working, launchable end-to-end capability rather than a horizontal technical layer. Phase 1 stands up the new solution (copied/renamed from the framework template), enables elevation, and delivers Home + the full Akari OS Tweaks page (32 toggles + the two-phase Defender workflow) — this is also where every foundational primitive the rest of the app depends on (registry/service/script-runner primitives, the elevated picker replacement, and the cross-thread dispatcher/marshaling pattern) gets built and proven, because Tweaks is the smallest page that exercises the full primitive stack. Phases 2-4 each reuse that foundation to ship one more complete page — Gaming Tweaks, Debloat, then Downloads + Misc together — closing out v1 feature parity with the predecessor.

## Phases

**Phase Numbering:**

- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Foundation & Akari OS Tweaks** - New elevated WinUI 3 solution (copied from the framework template, rebranded), Home dashboard, and the full 32-toggle Akari OS Tweaks page with real-state revert and the two-phase Defender workflow
- [ ] **Phase 2: Gaming Tweaks** - Gaming/latency toggles, service-config dropdowns, and a third-party tool launcher grid, reusing the Phase 1 tweak pattern
- [ ] **Phase 3: Debloat** - 28 PowerShell-backed debloat actions with streamed live output, driven by proper ViewModel/service architecture
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

**Plans:** 7 plans
Plans:
**Wave 1**

- [ ] 01-01-PLAN.md — Walking Skeleton tracer: copy/rename framework solution, elevate, wire WiFi tweak end-to-end; full Home dashboard + nav sidebar

**Wave 2** *(blocked on Wave 1 completion)*

- [ ] 01-02-PLAN.md — System primitives: ILogConsoleService (collapsible dock), IWindowsServiceController, IScriptRunner

**Wave 3** *(blocked on Wave 2 completion)*

- [ ] 01-03-PLAN.md — Elevation-safe file/folder picker (Microsoft.Windows.Storage.Pickers) + debug smoke test
- [ ] 01-04-PLAN.md — 22 registry-only tweak handlers (batches A/B)
- [ ] 01-05-PLAN.md — 8 service-backed + bcdedit/DISM-hybrid tweak handlers
- [ ] 01-06-PLAN.md — Defender two-phase disable workflow (byte-for-byte port) + minimal PostInstallService

**Wave 4** *(blocked on Wave 3 completion)*

- [ ] 01-07-PLAN.md — Final integration: ordering regression test, error resilience, full 32-tweak verification pass

**UI hint**: yes

### Phase 2: Gaming Tweaks

**Goal**: Users can tune gaming/latency system settings and launch third-party GPU/utility tools from one page, reusing the tweak pattern (real-state read, prior-state revert) established in Phase 1.
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: GAMING-01, GAMING-02
**Success Criteria** (what must be TRUE):

  1. User can view and toggle gaming/latency/service tweaks (SvcHost split threshold, Win32 priority separation, service configuration dropdowns) with toggles reflecting actual current system state
  2. Turning a gaming tweak off restores the real prior state the app recorded before mutating it, matching the Tweaks page guarantee
  3. User can launch quick-access tools for NVIDIA, AMD, and other third-party utilities from a launcher grid

**Plans**: TBD
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

**Plans**: TBD
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
| 1. Foundation & Akari OS Tweaks | 0/7 | Planned | - |
| 2. Gaming Tweaks | 0/TBD | Not started | - |
| 3. Debloat | 0/TBD | Not started | - |
| 4. Downloads & Misc | 0/TBD | Not started | - |
