# Requirements: Akari Toolbox

**Defined:** 2026-08-31
**Core Value:** Every tweak, debloat action, and downloaded asset must apply correctly, report accurate state, and (where applicable) be safely revertible.

## v1 Requirements

Requirements for initial release: a feature-parity WinUI 3 MVVM port of AkariOS Companion on the WinUI-3-MVVM-Framework, native Fluent 2/Mica look, admin-elevated, with real revertibility for tweaks.

### App

- [ ] **APP-01**: App requests and runs under administrator elevation on launch (`requireAdministrator`), matching the predecessor's privilege model
- [ ] **APP-02**: App identity (namespace, assembly name, manifest identity, icon, branding) reflects the "Akari Toolbox" rebrand
- [ ] **APP-03**: App uses native WinUI 3 Fluent 2 controls and the framework's Mica backdrop/theming — no WPF-UI custom theme (`Themes/Colors.xaml`, `Themes/Controls.xaml`) is carried over
- [ ] **APP-04**: File/folder picker operations work correctly while the app runs elevated (replacing the framework's default picker if it crashes under `requireAdministrator`)
- [ ] **APP-05**: Background operations (PowerShell output streaming, download progress) update the UI without cross-thread crashes

### Home

- [ ] **HOME-01**: User sees a home/dashboard landing page on launch listing the available tool categories

### Tweaks

- [ ] **TWEAKS-01**: User can view and toggle each of the 32 Akari OS registry/service-backed tweaks, with each toggle reflecting the actual current system state (not cached UI state)
- [ ] **TWEAKS-02**: User can disable Windows Defender via the two-phase guided workflow (tamper protection phase, then real-time protection phase), with explicit warnings at each phase — ported as a direct carry-over of the predecessor's existing Defender-disable logic; that specific code path is NOT refactored/decomposed into the new tweak-handler architecture for v1, left untouched on explicit user instruction
- [ ] **TWEAKS-03**: When a user toggles a tweak, the app records the tweak's real prior state before mutating it, so turning a tweak back off restores the actual previous state rather than a hardcoded default — does not apply to the Defender tweak (TWEAKS-02), whose logic is ported as-is

### Gaming Tweaks

- [ ] **GAMING-01**: User can toggle gaming/latency/service tweaks (SvcHost split threshold, Win32 priority separation, service configuration dropdowns), with the same real-state and revert behavior as Tweaks (TWEAKS-01/TWEAKS-03)
- [ ] **GAMING-02**: User can launch quick-access tools for NVIDIA, AMD, and other third-party utilities from a launcher grid

### Debloat

- [ ] **DEBLOAT-01**: User can run each of the 28 PowerShell-backed debloat actions from the Debloat page
- [ ] **DEBLOAT-02**: User sees streamed status/output feedback while a debloat action runs, without the UI freezing or crashing
- [ ] **DEBLOAT-03**: Debloat page logic lives in a ViewModel/service, not page code-behind (architecture-debt fix from the predecessor)

### Downloads

- [ ] **DOWNLOADS-01**: On first use, the app automatically downloads and mirrors the PostInstall asset folder from GitHub to `C:\PostInstall\` if it's not already present, and is a no-op if it is
- [ ] **DOWNLOADS-02**: User can access playbooks, drivers, and recommended utility links from the Downloads page

### Misc

- [ ] **MISC-01**: User can add or remove each of the 12 context-menu entries (classic/legacy Windows context menu)
- [ ] **MISC-02**: User can access the extra misc tools from the Misc page

## v2 Requirements

Deferred to a future milestone. Tracked but not in the current roadmap.

### Safety Net

- **SAFE-01**: App automatically creates a Windows system restore point before applying tweaks/debloat actions
- **SAFE-02**: Each tweak toggle displays an explicit risk-level indicator (safe/standard vs advanced/risky)

### Security

- **SEC-01**: Decompose the Defender-disable logic into the new `ITweakHandler`/`ITweakCatalog` architecture (currently ported as-is in v1 per TWEAKS-02) and add real Tamper Protection state verification (`Get-MpComputerStatus`) — deferred until the user explicitly asks to revisit this code path

### Ultimate Tweaks

- **ULT-01**: Deep "Ultimate" tweak collection integrated (~110 scripts across Check, Refresh, Setup, Installers, Graphics, Windows, Hardware, Advanced categories)
- **ULT-02**: Curated, SHA256-verified third-party tool bundle (7-Zip, Autoruns, CPU-Z, CRU, DDU, GPU-Z, HWiNFO, MSI Afterburner, NVIDIA Profile Inspector, Prime95, vcredist, etc.)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Continuous background tweak-enforcement/drift-protection daemon | Adds a persistent background service and attack surface disproportionate to value; explicitly an anti-feature for this tool's scope, not just deferred |
| Native GPU overclocking / fan-curve / voltage control | Vendor tools (NVIDIA/AMD control panels, MSI Afterburner) already do this safely with hardware-specific knowledge; reimplementing risks hardware damage for low differentiation — keep the quick-launch grid pattern instead |
| Full removal of Microsoft Store, WebView2, or Edge runtime dependencies | Most-cited cause of breakage in debloat post-mortems (breaks app installs, in-app browsers, unrelated apps that depend on WebView2) — debloat scope stays limited to user-facing bloat apps |
| Disabling Windows Update entirely | Leaves machines unpatchable and can break future repairs in hard-to-reverse ways — not part of this tool's tweak set |
| Automatic restore point / per-toggle risk labeling in v1 | Explicitly deferred to v2 (see SAFE-01/SAFE-02) — v1 ships prior-state revertibility (TWEAKS-03/GAMING-01) as the v1 safety property instead |
| Refactoring the Defender-disable code path into the new tweak-handler architecture | Explicit user decision: port the Defender tweak (TWEAKS-02) as a direct carry-over of the predecessor's existing logic; don't touch/refactor that specific code path in v1 — decomposition tracked as SEC-01 for a future milestone |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| APP-01 | Phase 1 | Pending |
| APP-02 | Phase 1 | Pending |
| APP-03 | Phase 1 | Pending |
| APP-04 | Phase 1 | Pending |
| APP-05 | Phase 1 | Pending |
| HOME-01 | Phase 1 | Pending |
| TWEAKS-01 | Phase 1 | Pending |
| TWEAKS-02 | Phase 1 | Pending |
| TWEAKS-03 | Phase 1 | Pending |
| GAMING-01 | Phase 2 | Pending |
| GAMING-02 | Phase 2 | Pending |
| DEBLOAT-01 | Phase 3 | Pending |
| DEBLOAT-02 | Phase 3 | Pending |
| DEBLOAT-03 | Phase 3 | Pending |
| DOWNLOADS-01 | Phase 4 | Pending |
| DOWNLOADS-02 | Phase 4 | Pending |
| MISC-01 | Phase 4 | Pending |
| MISC-02 | Phase 4 | Pending |

**Coverage:**
- v1 requirements: 18 total
- Mapped to phases: 18 (100%)
- Unmapped: 0

---
*Requirements defined: 2026-08-31*
*Last updated: 2026-08-31 after roadmap creation*
