# Requirements: Akari Toolbox — Gaming Optimizer Push

**Defined:** 2026-10-03
**Core Value:** Gamers get measurably smoother, lower-latency play from a one-pass safe tune where every tweak is explained, visible, and reversible.

## v1 Requirements

### Browse & Trust

- [ ] **BRW-01**: User can find any gaming tweak by search within a categorized catalog
- [ ] **BRW-02**: User can read a per-tweak explanation (what it does, why it helps gaming, risk, how it reverts) before toggling

### System Tuning

- [ ] **SYS-01**: User can enable Game Mode and disable Game DVR / background recording from one place

### Clean Background

- [ ] **BKG-01**: User can control startup apps and background UWP apps (Task Manager parity)

### Network Path

- [ ] **NET-01**: User can apply the gaming latency path (Nagle off, network throttling off, auto-tuning Normal, DNS flush)

### GPU & Display

- [ ] **GPU-01**: User can toggle Hardware-Accelerated GPU Scheduling with hardware gating and restart notice

### Safety & Rollback

- [ ] **SFT-01**: User can bulk-apply gaming tweaks with progress and revert all of them with one click

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### System Tuning

- **SYS-02**: User can switch power plans including Ultimate Performance unlock (prior plan saved for rollback)
- **SYS-03**: User can trim visual effects and disable mouse pointer precision for 1:1 aim
- **SYS-04**: User can toggle fullscreen optimizations, flip-model, and VRR with monitor-dependence notes

### Clean Background

- **BKG-02**: User can tune services from a conservative allow-list with start-type restore
- **BKG-03**: User can lock down telemetry, advertising ID, and suggestions via GPO-layer keys
- **BKG-04**: User can debloat consumer/OEM AppX at provisioned level with protected-app guard and Store reinstall path

### Network Path

- **NET-02**: User gets per-adapter awareness (Wi-Fi vs Ethernet) with read-back verification

### GPU & Display

- **GPU-02**: User gets driver-staleness guidance with vendor panel tips (guide only, no bundled installers)

### Safety & Rollback

- **SFT-02**: System restore point is created before bulk changes (coarse backstop to per-value snapshots)
- **SFT-03**: User can export a what-changed report (TXT/HTML) after applying tweaks

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Technical-details row (registry path, current vs recommended) | Differentiator not selected for v1; revisit once BRW-02 explanations prove the trust model |
| Game profiles / presets (Competitive vs Balanced vs Streaming) | Requires bulk apply to exist first; v1.x candidate |
| Config export/import (shareable JSON profiles) | Requires profiles to exist first; later |
| Auto gaming mode (apply on game launch, revert on exit) | HIGH complexity, crash-safe revert required; v2+ |
| Before/after performance snapshot | Needs diagnostics read layer; v1.x candidate, honest ranges only |
| Read-only diagnostics scan (crashes, SMART, XMP/EXPO) | Read-only, can ship early but not v1-selected; v1.x |
| GPU-vendor-aware rows (NVIDIA / AMD / Arc) | Enhances GPU tab after base toggles land; v1.x |
| SSD-vs-HDD-aware storage tuning | Smart-defaults layer on top of base; later |
| Timer / core-parking / power-throttling depth | Opt-in Advanced tier; needs per-key build-range research first |
| Game-library detection (Steam/Epic scan) | HIGH complexity; v2 material |
| Scheduled maintenance task | Set-and-forget hygiene; low priority vs trust bar |
| Wholesale Defender disable | Breaks security posture + updates + anti-cheat flags; violates fully-reversible line |
| VBS/HVCI / mitigation disabling | Breaks Vanguard/BattlEye on Win11 24H2+, real exploit exposure |
| Custom Windows ISOs | Supply-chain risk, update breakage, benchmark-debunked; native-ops-only forbids it |
| Disabling Windows Update entirely | Leaves machines unpatched, breaks Store/drivers; pause/delay instead |
| Aggressive service massacres (30+ at once) | Breaks printing, Bluetooth audio, Xbox/Game Pass; tiny gains, revert hell |
| Bundled driver/exe installers or script execution | Supply-chain risk; architecture explicitly forbids it |
| Resident "RAM cleaner / game booster" process | Fights Windows memory manager, placebo; on-demand apply only |
| Blind timer/HAGS/VRR application without hardware checks | No-ops or regressions; every gated tweak needs detection + N/A state |
| Xbox/Game Bar removal by default | Breaks Game Pass + clips users rely on; explicit opt-in only |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| BRW-01 | TBD | Pending |
| BRW-02 | TBD | Pending |
| SYS-01 | TBD | Pending |
| BKG-01 | TBD | Pending |
| NET-01 | TBD | Pending |
| GPU-01 | TBD | Pending |
| SFT-01 | TBD | Pending |

**Coverage:**
- v1 requirements: 7 total
- Mapped to phases: 0
- Unmapped: 7

---
*Requirements defined: 2026-10-03*
*Last updated: 2026-10-03 after initial definition*
