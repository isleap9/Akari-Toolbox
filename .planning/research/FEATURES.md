# Feature Research

**Domain:** Windows gaming optimization desktop tools (Winhance-style utilities, Fr33thy-type tweak sets, GPU/driver utilities)
**Researched:** 2026-10-03
**Confidence:** HIGH (official docs + widely replicated tweak sets); MEDIUM on measured-gain claims (vendor/creator benchmarks vary, hardware-dependent)

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Searchable tweak catalog with categories | Winhance set the UX bar: users expect to find a tweak by search, not page-hunt | MEDIUM | Akari already has 169-row Individual Tweaks catalog; gaming push needs search + category filter + per-tweak description. Native WinUI 3 AutoSuggestBox + grouped ListView. |
| Per-tweak explanation (what / why / risk / revert) | Enthusiasts will not toggle blind; Winhance shows description + technical details per row | LOW | Each TweakToggle needs Description, RegistryPath/Effect, RequiresRestart flag, Reversible=Yes. Cheap to add, huge trust payoff. |
| Game Mode ON + Xbox Game DVR / Game Bar background recording OFF | Universally agreed highest signal-to-noise gaming toggle; present in every tool surveyed | LOW | Registry (AllowAutoGameMode, GameDVR_Enabled=0). Reversible. Game Bar itself should stay removable-safe, not force-removed. |
| Power plan control (Balanced → High/Ultimate Performance) | Every gamer expects one-click power plan; Winhance, FPSBoostPro, all batch tools do it | LOW | powercfg /GUID activation. Akari must capture prior plan GUID for rollback. Ultimate Performance hidden-plan unlock = table stakes. |
| Hardware-Accelerated GPU Scheduling (HAGS) toggle | Expected on any "GPU" tab; hardware-gated (RTX 20+/RX5000+) | LOW | HwSchMode registry. Requires restart, must surface that. |
| Fullscreen optimizations + DirectX flip-model / VRR toggles | Standard gaming-graphics checklist (FSO disable fixes stutter in older titles; flip model + VRR reduce latency) | LOW | Per-app + global keys. Must note monitor-dependence for VRR. |
| Mouse input correctness (disable Enhance pointer precision) | Competitive players demand raw 1:1 aim; in every guide | LOW | Trivial registry; requires logoff. Include polling-queue note, not driver hacks. |
| Visual-effects trim (transparency, animations off) | "Disable animations for FPS" is the oldest expected toggle; frees DWM/GPU cycles | LOW | SystemParametersInfo/registry. Offer Balanced vs Minimal presets. |
| Background-apps / startup-apps control | Users expect to kill startup bloat and background UWP apps | MEDIUM | Startup Run-keys + Startup-folder + Store-task toggles. Task Manager parity is the bar (FPSBoostPro StartupManager). |
| Debloat (consumer/OEM AppX removal, provisioned level) | "Remove TikTok/Copilot-promo junk" is the #1 reason people open these tools | MEDIUM | Remove-AppxPackage equivalent via native packaging APIs; provisioned-image removal so junk doesn't return on new profiles. Must have protected-app guard (Store, Calculator, Terminal, Xbox-if-Game-Pass-user). |
| Telemetry / advertising-ID / suggestions lockdown | Expected privacy hygiene bundled with every optimizer | LOW | GPO-layer keys (AllowTelemetry, AdvertisingInfo, ContentDeliveryManager). Safe, reversible. |
| SysMain/Superfetch + Windows Search indexing control | Expected service-tuning pair; SSD vs HDD behavior differs | LOW | Allow-list services with start-type restore. SysMain off on SSD/16GB+, Search to Manual not Disabled. |
| Network latency path (Nagle off, network throttling off, auto-tuning Normal, DNS flush) | Every gaming tool has a network tab promising -5–15ms; users look for it | MEDIUM | Per-adapter Nagle keys +TcpipParameters; NetworkThrottlingIndex; netsh-equivalent native calls; flush DNS. Must be per-adapter aware (Wi-Fi vs Ethernet). |
| Bulk apply with progress + full rollback | Fr33thy's revert scripts, FPSBoostPro Restore Center, win11-gaming-toolkit manifest: reversibility is now expected | MEDIUM | Snapshot-before-apply (restore point + per-value backup store), Apply-all with progress bar, one-click Revert. This is Akari's stated v1 bar. |
| System restore point creation before changes | The universal safety ritual; every tool/script offers it | LOW | SRSetRestorePoint via native API. Already exists in Akari safety rails — expose per bulk-apply. |
| Optimization report / what-changed log | Users want proof of what happened (FPSBoostPro HTML report, batch-tool logs) | LOW | Rolling logs already exist; surface as exportable report (TXT/HTML). |
| Driver guidance (GPU driver version check + vendor tips) | Users expect the tool to tell them their driver is stale and link the right vendor panel settings (NV Low-Latency Ultra, AMD Anti-Lag) | MEDIUM | Version-compare against known releases + deep-link vendor settings. Native-ops constraint: guide, don't bundle installers. |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Gaming browse model (Winhance-style search + categories, gaming-only scope) | Directly the project's core bet: Winhance UX applied to gaming-only, no clutter | MEDIUM | Filter existing 169 tweaks + new gaming rows into Gaming/System/Network/GPU/Background categories with instant search. Akari's #1 differentiator vs generic tools. |
| Trusted-control bar: every tweak shows registry path + current vs recommended value | Winhance's Technical Details toggle is beloved; most script tools hide what they do | LOW | Read-back current value per tweak and display beside toggle. High trust, low cost given native engine. |
| Game profiles / presets (Competitive FPS vs Balanced vs Streaming) | mojo-gaming-mode and FPSBoostPro roadmaps both converge here; users want one tap per context | MEDIUM | Named profile = saved set of tweak states + power plan + service set. Save/load JSON in %LOCALAPPDATA%. |
| Auto gaming mode (apply profile on game launch, revert on exit) | The "it just works" moment competitors list as roadmap, few ship well | HIGH | Process watcher for monitored executables; apply/revert profile. Needs careful revert-on-crash handling. Defer to v1.x. |
| Before/after performance snapshot (FPS advice, latency, boot impact) | mojo shows before/after toast; Fr33thy uses FrameView/CapFrameX methodology — users crave measured gains, not vibes | MEDIUM | Snapshot frametime/ping/boot-startup weight before apply, show delta after. Honest ranges (5–15% CPU-bound FPS, 10–30ms input lag) not promises. Counters placebo criticism. |
| Read-only diagnostics scan (crash history, BSOD decode, SMART, XMP/EXPO check) | PitCrew's Diagnose is the freshest differentiator: tells users what actually matters (XMP off? failing drive?) before tweaking | MEDIUM | WMI/EventLog/SMART reads only — no mutations. High value for "my FPS is low" triage. Akari already does WMI SystemInfo; extend it. |
| GPU-vendor-aware tuning (NVIDIA vs AMD vs Intel Arc paths) | Generic tools apply one-size keys; vendor-aware tips (ReBAR, MSI-mode, ULPS, Low-Latency/Anti-Lag) feel expert | MEDIUM | Detect discrete GPU (exclude virtual adapters: Parsec/RDP/VMware), show only relevant rows + vendor panel guidance. |
| SSD-vs-HDD-aware storage tuning (TRIM vs prefetch, pagefile sizing) | Smart defaults beat blanket "disable everything": TRIM for SSD, prefetch for HDD, pagefile by RAM size | LOW | Detect storage type + RAM size; conditional recommendations. Cheap logic, expert feel. |
| Timer-resolution / core-parking / power-throttling depth (0.5ms, Win32PrioritySeparation, EcoQoS off) | Enthusiast depth beyond the basics; AIO tools advertise this as "true" tuning | MEDIUM | Boot-config + power-scheme keys. Must be opt-in Advanced tier with reboot warnings — still reversible, fits deeper-but-safe. |
| Config export/import (.winhance-style JSON profiles) | Winhance config files enable share/repeat setups; streamers share profiles | LOW | Serialize tweak states to JSON; import applies with same safety rails. Enables community profiles later. |
| Game-library detection (Steam/Epic scan → per-title suggestions) | mojo roadmap item; turns generic tool into "my games" tool | HIGH | Parse launcher manifests for installed titles; suggest title-specific settings (Reflex, flip model). v2 material. |
| Scheduled maintenance (weekly cleanup + TRIM task) | Set-and-forget hygiene; FPSBoostPro lists as roadmap | LOW | Optional scheduled task; off by default, explicit opt-in. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but create problems.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Wholesale Windows Defender disable | "Defender costs FPS" folklore; biggest ask in tweak threads | Breaks security posture; blocks updates; some anti-cheats flag tampered security state; Fr33thy himself notes gains only matter on old low-end CPUs | Per-exclusion guidance or process-priority tuning only; never wholesale disable. Keep Defender + cloud-delivery intact (matches Akari reversible/safe line). |
| Disabling VBS/HVCI/memory-integrity, Spectre/Meltdown mitigations, DEP | Real FPS in CPU-bound benchmarks; red-tier tools offer it | Breaks Valorant Vanguard / R6 BattlEye on Win11 24H2+, exposes to real exploits; irreversible-feeling to average users | Document as explicitly out-of-scope; if ever added, quarantined red-tier opt-in with anti-cheat breakage warnings — recommend leaving ON. |
| Custom Windows ISOs / stripped images | "Pre-debloated = faster" marketing | Malware/keylogger risk, update breakage, benchmark-debunked on modern CPUs (Fr33thy debunk repo); violates native-ops-only | Tune stock Windows with reversible tweaks; never ship or bless ISOs. |
| Disabling Windows Update entirely | "Updates cause stutter/reboots" | Leaves machines unpatched; Store/driver updates break; Akari safety line forbids breaking updates | Offer "pause/delay + active-hours + no auto-restart while gaming" instead. |
| Aggressive service massacres (30+ services at once) | "Fewer services = more FPS" | Breaks printing, Bluetooth audio, Xbox/Game Pass, Search; tiny gains on modern CPUs; revert hell | Conservative allow-list only (DiagTrack, SysMain-conditional, MapsBroker, RetailDemo etc.) with per-service reason + restore. |
| Bundled driver/exe installers or script execution | "One click installs everything" convenience | Supply-chain risk; Akari architecture explicitly forbids scripts/bundled binaries | Vendor guidance + deep links; winget/Store reinstall path for removed apps only. |
| Fake "RAM cleaner / game booster" background process | Feels active, "boost" button dopamine | Wastes CPU, fights Windows memory manager, placebo; memory compression off already covers real case | No resident booster; on-demand apply + optional game-launch watcher only. |
| Timer/HAGS/VRR tweaks applied blindly without hardware checks | "Enable all = max FPS" | HAGS needs RTX20+/RX5000+, VRR needs capable monitor, ReBAR needs firmware — no-ops or regressions otherwise | Gate each behind hardware detection with "N/A on your hardware" state. |
| One-click-only UX with no per-tweak control | Casual simplicity | Directly contradicts enthusiast primary user; hides what changed, kills trust | Bulk-apply AND per-tweak toggles with explanations (both, not either). |
| Breaking Xbox/Game Bar/Xbox services by default | "Xbox bloat wastes resources" | Breaks Game Pass, Game Bar clip users rely on, controller stack | Xbox-service disable = explicit opt-in with confirmation + Game Pass warning; default preserves. |

## Feature Dependencies

```
Bulk apply with progress + full rollback
    └──requires──> Per-tweak value snapshot store (current-value backup)
                       └──requires──> Restore-point creation
                       └──requires──> Per-tweak explanation metadata (what/revert/reboot-flag)

Game profiles / presets
    └──requires──> Bulk apply + rollback
                       └──requires──> Power-plan control + service allow-list

Auto gaming mode (launch watcher)
    └──requires──> Game profiles / presets
                       └──requires──> Crash-safe revert (restore on watcher death)

Before/after performance snapshot
    └──requires──> Diagnostics read layer (WMI/EventLog/SMART/ping)
                        └──enhances──> Bulk apply (proof of effect)

GPU-vendor-aware tuning ──enhances──> GPU tab (HAGS/flip/VRR/ReBAR/MSI-mode)
Network latency path ──requires──> Per-adapter enumeration (Wi-Fi vs Ethernet)
Debloat (provisioned) ──requires──> Protected-app guard + Store reinstall path
Diagnostics scan ──requires──> (nothing mutating; read-only, can ship early)

Wholesale Defender disable ──conflicts──> Fully-reversible/safe line
VBS/HVCI/mitigation disable ──conflicts──> Anti-cheat compatibility (Vanguard/BattlEye)
Windows Update disable ──conflicts──> Safety rail (never break updates)
```

### Dependency Notes

- **Bulk apply requires snapshot store + restore point:** rollback is only real if pre-apply values (registry/service/plan GUID) are captured first. Snapshot store is the foundation of the whole v1 bar.
- **Profiles require bulk apply:** a profile is just a named bulk-apply set; no point building profiles before apply/revert works.
- **Auto gaming mode requires crash-safe revert:** if the watcher dies mid-game, the machine must return to the base profile — otherwise users get stuck in Esports mode. This is why it's HIGH complexity and deferred.
- **Snapshot/diagnostics enhances bulk apply:** measured deltas turn "I think it's faster" into "startup −20%, ping −8ms". Builds trust, counters optimizer skepticism.
- **Debloat requires protected-app guard:** provisioned removal without a guard deletes Store/Xbox/Terminal and creates support load. Guard + reinstall path first.
- **Red-tier security trade-offs conflict with anti-cheat + safe line:** VBS/Defender/mitigation disables break Vanguard/BattlEye and violate the project's fully-reversible-safe positioning — excluded by design.

## MVP Definition

### Launch With (v1)

Minimum viable product — trusted-control gaming tune.

- [ ] Searchable gaming tweak catalog with categories + per-tweak explanation — the Winhance-style browse model; the project's headline requirement
- [ ] Technical-details row (registry path / current vs recommended / reboot flag) — trust differentiator, cheap
- [ ] Game Mode ON + Game DVR/background-recording OFF + power-plan control (incl. Ultimate unlock) — highest-signal toggles
- [ ] HAGS + fullscreen-optimizations + flip-model/VRR + mouse-precision-off + visual-effects trim — expected graphics/input checklist
- [ ] Background/startup control + conservative service allow-list — clean-background path without breaking Windows
- [ ] Debloat with protected-app guard — #1 acquisition feature, safe version
- [ ] Telemetry/ads/suggestions lockdown — expected hygiene
- [ ] Network latency path (Nagle, throttling, auto-tuning, DNS flush) with per-adapter clarity
- [ ] Bulk apply with progress + per-value snapshot + one-click rollback + restore point — the v1 done bar
- [ ] What-changed report/log export — proof of work

### Add After Validation (v1.x)

Features to add once core is working.

- [ ] Game profiles / presets (Competitive / Balanced / Streaming) + JSON export/import — trigger: users ask to switch contexts or share setups
- [ ] Before/after snapshot card (ping, startup weight, FPS advice) — trigger: credibility asks ("prove it works")
- [ ] Read-only diagnostics scan (crashes, BSOD decode, SMART, XMP check) — trigger: support load shows users misattribute hardware issues to tuning
- [ ] GPU-vendor-aware rows + driver-staleness check — trigger: GPU tab feedback shows confusion
- [ ] SSD/HDD-aware storage tuning + pagefile sizing — trigger: diagnostics data shows mixed storage base

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] Auto gaming mode (launch watcher with crash-safe revert) — why defer: HIGH complexity, background-process trust cost
- [ ] Game-library detection + per-title suggestions — why defer: needs launcher parsing + title DB, low ROI before core trust is earned
- [ ] Scheduled maintenance task — why defer: opt-in hygiene, no urgency
- [ ] Timer-depth tier (0.5ms lock, core-parking, EcoQoS) as Advanced opt-in — why defer: reboot-sensitive, needs diagnostics maturity first

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Searchable catalog + categories | HIGH | MEDIUM | P1 |
| Per-tweak explanations + technical details | HIGH | LOW | P1 |
| Game Mode / DVR / power plan | HIGH | LOW | P1 |
| HAGS / FSO / flip / VRR / mouse / visuals | HIGH | LOW | P1 |
| Bulk apply + snapshot + rollback + restore point | HIGH | MEDIUM | P1 |
| Background/startup + service allow-list | HIGH | MEDIUM | P1 |
| Debloat + protected guard | HIGH | MEDIUM | P1 |
| Telemetry lockdown | MEDIUM | LOW | P1 |
| Network latency path | HIGH | MEDIUM | P1 |
| What-changed report | MEDIUM | LOW | P1 |
| Game profiles + import/export | HIGH | MEDIUM | P2 |
| Before/after snapshot | HIGH | MEDIUM | P2 |
| Diagnostics scan (read-only) | HIGH | MEDIUM | P2 |
| GPU-vendor-aware rows + driver check | MEDIUM | MEDIUM | P2 |
| SSD/HDD-aware storage tuning | MEDIUM | LOW | P2 |
| Auto gaming mode | MEDIUM | HIGH | P3 |
| Game-library detection | MEDIUM | HIGH | P3 |
| Scheduled maintenance | LOW | LOW | P3 |
| Timer-depth Advanced tier | MEDIUM | MEDIUM | P3 |
| Defender/VBS/mitigation disables | LOW (niche) | HIGH (support cost) | P3 — do not build |

**Priority key:**
- P1: Must have for launch
- P2: Should have, add when possible
- P3: Nice to have, future consideration

## Competitor Feature Analysis

| Feature | Winhance | Fr33thy Ultimate set | FPSBoostPro / mojo / toolkit-style tools | Our Approach |
|---------|----------|----------------------|------------------------------------------|--------------|
| Browse model | Search + categories + per-tweak descriptions + technical-details toggle; config export | Script menus by flow (Check/Refresh/Setup/Graphics…), revert scripts per flow | Tabs (Dashboard/One-click/Advanced/Startup/Services/Debloat/Restore) | Winhance-style searchable gaming-only catalog over Akari's existing 13 pages; keep Individual Tweaks as the power list |
| Gaming core (Game Mode/DVR/power/HAGS) | Yes, per-toggle with docs | Yes, BIOS→Windows→driver full chain incl. stress tests | Yes, one-click + advanced per-tweak | Same toggles, every one explained + reversible; prior power-plan GUID captured |
| Background/services | Per-service toggles with descriptions | Service + scheduled-task + UWP suppression depth | Allow-list services, startup incl. Store apps | Conservative allow-list + startup parity; never mass-disable |
| Network | Throttling + Nagle keys | Adapter/DNS/delivery-optimization guidance | Nagle/RSS/RSC/auto-tuning/DNS flush | Per-adapter Nagle + throttling + auto-tuning + flush; show Wi-Fi vs Ethernet |
| GPU depth | HAGS, flip, VRR, sharpening, GPU priority | Driver DDU flows, inspector/ReBAR, Reflex, FrameView measurement | MSI-mode, ReBAR, P0, driver compare | Native-safe subset: HAGS/flip/VRR/MSI/ReBAR-state + vendor guidance; no bundled drivers, no DDU |
| Rollback | Apply-on-toggle (no background re-apply); config files | Revert scripts per flow | Restore Center / manifest + REVERT-EVERYTHING | Snapshot store + restore point + one-click revert; crash-safe for later auto mode |
| Proof/diagnostics | Recommended-config concept | Benchmark methodology (FrameView/CapFrameX), XMP/EXPO + stability emphasis | Before/after snapshot, FPS advice, SMART/BSOD/XMP scan | What-changed report v1; diagnostics scan + before/after v1.x |
| Red-tier (Defender/VBS/mitigations) | Not pushed | Measured-honest: gains mostly low-end; server-edition curiosity | win11-gaming-toolkit quarantines as red opt-in | Explicitly out: document why, don't build |

## Sources

- Winhance official docs: Optimizations index, Gaming & Performance, Power Management, Quick Start, Configuration Files (HIGH confidence — primary UX reference)
- FR33THYFR33THY Ultimate / WinSux / Debunking-Custom-ISOs repos + Ultimate walkthrough video (HIGH on tweak inventory, MEDIUM on gain claims — creator-measured, hardware-dependent)
- FPSBoostPro README (feature/tab inventory incl. Restore Center + roadmap) (MEDIUM — single-project README, pattern-confirmed elsewhere)
- win11-gaming-toolkit README (tier model incl. red-tier + manifest revert contract) (MEDIUM — single-project, well-sourced headers)
- mojo-gaming-mode README (presets, before/after snapshot, game detection roadmap) (MEDIUM)
- PitCrew README (Diagnose + provisioned-level debloat + protected services) (MEDIUM)
- windows11nontouchgamingoptimizer README (15-step structure, vendor tips, honest 5–15% CPU-bound expectations) (MEDIUM)
- enesehs-windows-optimizer + AIO-Ultimate-System-Optimizer READMEs (checklist convergence) (LOW-MEDIUM — corroborating only)

---
*Feature research for: Windows gaming optimization desktop app (Akari Toolbox gaming push)*
*Researched: 2026-10-03*
