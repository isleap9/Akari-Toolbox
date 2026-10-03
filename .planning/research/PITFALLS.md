# Pitfalls Research

**Domain:** Windows gaming optimizer desktop app (WinUI 3, admin-required native ops, fully-reversible line)
**Researched:** 2026-10-03
**Confidence:** HIGH (timer/service findings cross-checked across enthusiast + technical sources); MEDIUM on exact phase ordering (roadmap not yet written)

## Critical Pitfalls

### Pitfall 1: Shipping bcdedit timer tweaks as "performance gains"

**What goes wrong:**
Copying the classic YouTube tweak pack — `bcdedit /set useplatformclock true`, `useplatformtick yes`, `disabledynamictick yes`, HPET disable in BIOS — tanks real performance or makes the system stutter, while users believe they gained FPS. `useplatformclock` forces the whole OS off the fast CPU TSC timer onto HPET (up to ~22 MHz interrupt churn); `useplatformtick` forces RTC, a non-dynamic tick with no smart compute optimizations. Latency monitors then report misleading "lower latency" that is actually frame-drop-induced CPU bottlenecking, not improvement.

**Why it happens:**
Every legacy optimizer guide lists these tweaks, forum anecdotes claim "+20 FPS from deleting useplatformclock," and the settings *do* change benchmark numbers — so they look like they work. Developers cargo-cult them without understanding TSC/HPET/PMT/RTC semantics.

**How to avoid:**
- Default stance: **do not ship `useplatformclock` / `useplatformtick` toggles as gaming optimizations.** The correct default on modern systems is TSC+TSC with HPET *enabled* in BIOS for sync (`bcdedit /deletevalue useplatformclock`, `/deletevalue useplatformtick`).
- If any timer toggle is ever exposed, gate it behind: legacy-game-only labeling (e.g. "only for Saints Row 2-era engines that need PMT"), a per-tweak explanation of the harm, and a one-click revert to TSC defaults. Never include in bulk "max performance" presets.
- Document the known edge cases instead of "optimizing": disabling HPET in BIOS stops TSC sync and causes clock skew over hours (anticheat speedhack false-positive risk); disabling HPET to "fix stutters" masks real ISR/DPC latency problems.

**Warning signs:**
- A tweak whose only evidence is forum FPS anecdotes with no LatencyMon/frame-time methodology.
- Any `bcdedit` change applied without a reboot-required + revert-tested flow.
- Timer "improvements" that coincide with new stutter complaints on 8000 Hz mice (classic PMT/RTC symptom).

**Phase to address:**
Timer/power phase (CPU / timer / power / Game Mode). Decide the timer stance in planning, before any GamingActions code is written — this is a content decision, not an implementation detail.

---

### Pitfall 2: Disabling services to "free resources" breaks Windows itself

**What goes wrong:**
Bulk service-disable presets (SysMain, DiagTrack,BITS, WSearch, Xbox services, Gaming Services, even Defender-adjacent services) break Windows Update, Microsoft Store installs/updates, Xbox Game Bar/Game Mode hooks, controller/peripheral detection, search indexing for the Start menu, and anti-cheat services that depend on specific drivers. Defender-disable via policy/registry additionally causes app hangs and multi-minute startup freezes (documented production incident class), not just "no antivirus."

**Why it happens:**
Task Manager shows services consuming RAM/CPU at idle, so "fewer services = more FPS" feels obvious. Most guides never test the *second-order* effects (update next Tuesday, game install next week, controller plugged in tomorrow).

**How to avoid:**
- Maintain an explicit **never-disable allowlist**: Windows Update stack (wuauserv, UsoSvc, BITS), Store/ClipSVC, DCOM/RPC, Defender core (WinDefend, WdNisSvc — offer *exclusions for game folders*, never engine-disable), Gaming Services/Xbox services when any Game Pass/Xbox title path is in scope.
- Every service toggle stores **previous StartType** (Automatic/Manual/Disabled) per service and restores exactly that — never "set everything back to Automatic."
- Separate "telemetry/diagnostic-data" toggles (safe, reversible) from "service disable" toggles (risky) in the UI; never bundle them in one preset.
- Test matrix must include: Windows Update check, Store app install, Xbox controller connect, and a reboot — not just "game launches."

**Warning signs:**
- A preset named "Disable all unnecessary services" or code that loops services setting Disabled without snapshotting prior state.
- Any Defender-off toggle that isn't scoped to real-time-protection-temporary or folder exclusions.
- QA that only launches one game but never runs Update/Store flows.

**Phase to address:**
Clean-system gaming path phase (debloat, services, telemetry, background apps). The allowlist + snapshot-restore design must land in that phase's plan, and the phase's UAT must include Update/Store/controller checks as exit criteria.

---

### Pitfall 3: Debloating AppX / Store / Xbox / WebView runtimes breaks games and updates

**What goes wrong:**
Removing provisioned AppX packages, the Microsoft Store itself, Xbox overlays, or Edge WebView2 runtime breaks: Store-delivered game updates, Game Pass titles, overlays/widgets that games embed via WebView2, and the update path for inbox apps. Reinstalling what was removed is often harder than removing it (Store removal famously requires re-provisioning gymnastics), which violates the fully-reversible promise.

**Why it happens:**
"Debloat" scripts conflate *third-party crapware/preinstalls* (safe to remove) with *platform runtimes* (Store, VCLibs, WebView2, Xbox Identity) that games silently depend on. Bulk-remove feels powerful and demoable.

**How to avoid:**
- Hard rule: **never remove the Store, VCLibs/XAML runtimes, WebView2, or Xbox Identity/Gaming Services** in the safe line. Only offer removal of clearly-third-party provisioned bloat, each toggle individually reversible (reinstall command stored alongside).
- Prefer *disable/deprovision-for-new-users* over *remove-for-all-users* where the platform supports it, and record package full names + versions for reinstall.
- Every debloat entry needs: what breaks if removed, reinstall path, and "requires reboot / Store re-check" notes — this is the per-tweak explanation content the trusted-control bar demands.

**Warning signs:**
- Code calling `Remove-AppxPackage -AllUsers` or package-family wildcards.
- "Remove Edge/WebView" bundled into a gaming preset (breaks launchers and overlays).
- No reinstall path recorded at apply time.

**Phase to address:**
Clean-system gaming path phase. Debloat catalog review (keep-list vs remove-list) is a planning deliverable before implementation.

---

### Pitfall 4: Placebo network "low ping" registry tweaks that do nothing or hurt

**What goes wrong:**
The standard set — per-interface `TcpNoDelay`/`TcpAckFrequency` Nagle tweaks, `NetworkThrottlingIndex`, `SystemResponsiveness`, disabling TCP auto-tuning / ECN / RSS — either (a) targets the wrong NIC GUID because the code enumerates adapters incorrectly, (b) is already the default on modern Windows, or (c) actively harms throughput (disabling auto-tuning on high-bandwidth links; forcing `TcpAckFrequency=1` adds ACK storms on lossy Wi-Fi). Users see ping unchanged, blame the tool, and the "before/after clarity" promise collapses.

**Why it happens:**
SpeedGuide-style guides from the Windows 7 era are copy-pasted forever. The tweaks are registry writes — trivially easy to implement — so they get added without A/B measurement.

**How to avoid:**
- Only ship network tweaks with **measurable, adapter-scoped effects**: adapter selection UI (which NIC the tweak targets, with GUID shown), auto-tuning levels exposed as profiles (not just on/off), and DNS change with old-server record + one-click revert.
- Never write Nagle keys to all interfaces blindly; resolve the active gaming adapter (the route to the game/default gateway), snapshot existing values per GUID, restore per GUID.
- Pair the network path with honest copy: "last-mile tweaks change single-digit ms at most; your ISP/route dominates." The enthusiast user respects honesty; placebo destroys trust.
- Verify with `Get-NetTCPSetting` / `netsh interface tcp show global` reads after write, surfaced in UI as before/after.

**Warning signs:**
- Registry writes to `...Tcpip\Parameters\Interfaces\{...}` in a loop over all GUIDs.
- `NetworkThrottlingIndex = 0xffffffff` presented as pure upside (it trades multimedia scheduling for throughput — say so).
- No adapter picker, no before/after readout.

**Phase to address:**
Gaming network path phase (adapter, DNS, throttling). Require per-adapter snapshot + verification reads in the phase plan.

---

### Pitfall 5: Power / GPU / driver flows applied without per-hardware guards

**What goes wrong:**
Forcing Ultimate Performance power scheme + `HighPerformance` GPU preference + MSI-mode/IRQ tweaks + driver "clean reinstall" guidance on every machine causes: laptop thermal throttling and battery drain, iGPU-only devices stuttering worse, multi-GPU laptops switching to the wrong GPU, and NVIDIA/AMD profile loss with no backup. DDU-style full driver purges from inside the app (without Safe Mode) leave half-removed driver states — the exact "broke my drivers" review.

**Why it happens:**
Desktop-tested presets get shipped to laptops; driver flows are designed as "delete then install" without testing the "install fails halfway" branch.

**How to avoid:**
- Gate power/GPU presets on hardware detection: chassis type (laptop vs desktop via `Win32_SystemEnclosure`/`BatteryStatus`), GPU vendor + hybrid-graphics detection, driver version floor. Laptops get a separate, thermally-safe preset or a warning gate.
- GPU profile changes (NVCP/AMD equivalents, display scaling, HAGS, Game Mode) must export current profile/settings first and restore byte-for-byte; never "reset to defaults" as the rollback.
- Driver flows stay *guided, not destructive*: download-verified installer launch + pre-flow restore point + explicit "we never DDU from inside the app" boundary. If a clean-install path exists, it requires Safe Mode reboot flow (the existing safe-boot headless pattern) with clear state resume after reboot.
- Reuse the existing `SystemInfo` WMI helpers but audit disposal (collections + objects) — the codebase already flags this as a build-bar issue.

**Warning signs:**
- One power preset for all chassis types.
- GPU/driver code with no vendor branch and no profile backup step.
- "Clean drivers" button that deletes without a verified installer already staged.

**Phase to address:**
Max-performance tuning phase (CPU/timer/power/Game Mode) + GPU tuning depth phase. Hardware-gating is an architecture decision shared by both — define once, enforce in both.

---

### Pitfall 6: "Restore point = rollback" illusion

**What goes wrong:**
Relying solely on System Restore for reversibility fails in practice: restore-point creation can stall (documented 45-minute hangs), is disabled on many gaming rigs, doesn't cover all hives/keys the tool touches, and rolling back the *whole system* to undo one tweak nukes everything the user did since (other installs, saves-adjacent config). Users promised "every tweak reversible" discover revert means "lose an afternoon."

**Why it happens:**
WinUtil-style tools normalize "we create a restore point, you're safe." Per-tweak undo is 10x the engineering (snapshot every value, store, restore individually), so teams defer it until after the damage.

**How to avoid:**
- Two-layer safety, both mandatory: (1) restore point before bulk apply (existing `RestorePointActions` — keep, but with timeout + disabled-restore detection + clear failure messaging), (2) **per-tweak value journal**: before each write, record hive/key/value-name/old-data/type (and service StartType, power GUID, DNS servers) into `%LOCALAPPDATA%\AkariToolbox` settings store; rollback replays the journal in reverse.
- Bulk-apply must be resumable and individually revertible: progress UI with per-tweak status, cancel that stops cleanly between tweaks (never mid-write), and partial-failure report with one-click "undo what just applied."
- Registry deletes get special handling: journal the full deleted subtree, not just "it existed."

**Warning signs:**
- Rollback code that only calls System Restore.
- No journal file/schema in the settings store.
- Bulk apply as a single fire-and-forget loop with no cancellation or per-item status.

**Phase to address:**
Trusted-control v1 bar phase (explained + bulk-apply with progress + full rollback). This phase *is* the journal + progress + cancel + per-tweak undo work — it must come before or with the first preset that touches the system, not after.

---

### Pitfall 7: Build-fragile registry paths (Win10 vs Win11 24H2+ drift)

**What goes wrong:**
Hardcoded registry paths and policy keys that moved between builds (Game Mode/Game Bar, telemetry `AllowTelemetry` semantics, power throttling, network profile keys) silently no-op on the user's build — or worse, write a stale key that does nothing while the UI shows "Applied." 24H2+Also changed several gaming-relevant defaults, so "tested on 23H2" ≠ works on current.

**Why it happens:**
Dev machine runs one build; registry guides rarely note build ranges; code writes with `Registry.SetValue` and reports success if no exception — but no-exception ≠ effective.

**How to avoid:**
- Every tweak row in the catalog carries: `MinBuild`/`MaxBuild` (or "all"), expected verification read-back, and "not applicable on this build" UI state (disabled with reason, not silently applied).
- After every write, **read back and compare**; surface mismatch as "needs reboot" vs "not applicable" vs "failed."
- Pin the test matrix: Windows 10 22H2 + Windows 11 23H2/24H2 minimum; target SDK is `10.0.26100` so 24H2 behavior is the reference.
- Centralize paths in the catalog (`AkariTweakCatalog` / per-page Actions modules), never inline string literals scattered across ViewModels.

**Warning signs:**
- Registry path string literals in ViewModels or code-behind.
- Apply methods with no read-back verification.
- No build-gating anywhere in the tweak pipeline.

**Phase to address:**
Winhance-style browse model phase (searchable catalog with per-tweak info) — the catalog schema (build range, verify-read, explanation fields) is designed there and every later phase inherits it.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Service StartType set to Disabled without snapshotting prior value | One-line code, dramatic preset | Unrestorable services; Update/Store breakage with no repair path | Never |
| `bcdedit` timer tweaks copied from guides | Fast "more tweaks" count | Stutter/perf regressions + anticheat skew risk; trust loss | Never as optimizations; legacy-game-only with warning, or never |
| Restore-point-only rollback | No journal schema to build | "Reversible" promise is hollow; partial failures unrecoverable | Never — journal is the v1 bar |
| Registry writes without read-back verify | Less code per tweak | Silent no-ops on drifted builds; "Applied" lies | Never |
| Hardcoded NIC GUID / all-interfaces loop for network tweaks | Works on dev machine | Wrong-adapter writes on user machines; Wi-Fi harm | Never |
| One power preset for all chassis | Simpler UX | Laptop thermals/battery complaints, 1-star reviews | Only if hardware-gated first |
| AppX wildcard removal | Impressive debloat count | Store/Game Pass/WebView breakage; hard reinstall | Never for platform runtimes |
| Bulk apply as single loop, no cancel/progress | Ships faster | Frozen UI, killed process mid-write, corrupt state | Never for admin ops |
| Explanation copy as last-minute strings | Faster phase exit | Enthusiast users (the primary persona) reject unexplained toggles | Never — explanations are the product |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| System Restore API (`RestorePointActions`) | Assume creation always succeeds fast | Timeout + progress + disabled-restore detection; proceed only with explicit user consent if restore unavailable; journal-first so per-tweak undo works regardless |
| Windows Services (SCM) | `SetServiceStatus` without prior StartType; forgetting dependent services | Snapshot StartType per service; stop in dependency order, restore in reverse; handle `SERVICE_DISABLED` dependents gracefully |
| `bcdedit` / BCD store | Writing BCD values from the running OS without reboot semantics | Treat as reboot-required op: stage, verify, prompt reboot, re-verify post-reboot; default to TSC-deletevalues, not set-values |
| Power schemes (`powercfg`) | Activating GUID without recording prior scheme | Record active scheme GUID pre-apply; restore exact GUID; set laptop vs desktop defaults differently |
| WMI `SystemInfo` reads | Leaking `ManagementObjectCollection` / objects (known codebase flag) | `using` + explicit `Dispose()` on collections and each object; never hold WMI objects across await points |
| DNS / adapter settings | Writing per-interface keys for all GUIDs | Resolve active gaming adapter first; snapshot per-GUID values; verify via `Get-NetTCPSetting` / `netsh` read-back |
| GPU vendor APIs / profile stores | Overwriting profiles with no export | Export-before-write for NV/AMD profiles; byte-identical restore; vendor-detect gate before showing toggles |
| UAC / elevation | Assuming elevated for whole session; silent fail when not | Admin manifest (existing) + runtime elevation check at tweak-apply entry with clear "restart as admin" path; safe-boot headless flows reuse the same gate |
| WinUI 3 single-instance + apphost lock | Rebuild/test while app running; second instance applying tweaks concurrently | Keep single-instance guard; kill `AkariToolbox.App` before rebuild (existing build bar); mutex around bulk-apply so two instances can't interleave writes |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Timer-resolution "always 0.5ms" service | Higher idle power, heat, no game gain; HPET-churn confusion | Ship `SetTimerResolutionService`-style residency only as opt-in, game-session-scoped (start on game launch, revert on exit), never always-on | Laptops / handhelds immediately; desktops on power bill + thermals |
| Disabling SysMain/Search/Prefetch for "RAM savings" | Slower game load times (cold cache), Start-menu search dead | Measure load-time before/after; keep caching services on SSD-era systems; offer only with explanation | Large open-world titles with heavy streaming |
| Defender real-time off for FPS | 1–3% gain in synthetic benches, app freezes in real use | Folder exclusions for game dirs instead; never engine-off | Systems with Store/Game Pass titles (worst interaction) |
| Bulk-apply on UI thread | Frozen browse UI, "not responding" during 50-tweak preset | Async apply with `IProgress<T>`, per-tweak status rows, cancel token between tweaks | Any preset over ~10 tweaks |
| Unbounded rolling logs of every registry write | `%LOCALAPPDATA%` bloat, slow settings load | Bounded rolling logs (existing pattern — keep the cap, include journal rotation) | After weeks of enthusiast toggling |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Permanent Defender/WDAC/SmartScreen disable toggles | Malware foothold; user blames tool for infection; update-stack breakage | Offer scoped exclusions + tempered-hardening explanations; engine-off is out of scope (already rejected as aggressive-irreversible line) |
| Always-elevated app with broad file/registry write helpers (`NativeOps`) | Any XAML/binding bug becomes a privileged-arbitrary-write primitive | Constrain `NativeOps` to catalog-declared paths only (allowlist); log every privileged write with calling tweak ID; no generic "write any key" helper reachable from UI |
| Storing DNS/adapter/GPU state or logs with sensitive paths in plaintext | Minor privacy leak (network topology in logs) | Keep journal local-only, document it, exclude machine-identifying details from any shared/exported log bundle |
| Recommending third-party driver updaters / bundled binaries | Supply-chain risk; violates native-ops-only architecture | Architecture forbids scripts/bundled binaries — driver flows launch only verified vendor installers already on disk or Store-sourced; never download-and-run inside the tool |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| One-click "Optimize everything" with no preview | Enthusiast (primary persona) distrusts it; casuals break things blindly | Winhance model: searchable list, per-tweak checkbox + explanation + risk badge; bulk presets are saved selections, previewed before apply |
| Toggles with no "what it does / what breaks / how to undo" | Users won't touch powerful tweaks; support load from surprises | Every row: one-line effect, expandable why + risk + revert note; trusted-control bar = every tweak explained |
| "Applied ✓" with no before/after | Placebo suspicion, especially network/power | Show old → new per tweak (value, StartType, scheme GUID, DNS); bulk summary screen with per-item status |
| No reboot-required signaling | Timer/BCD/driver tweaks silently pending; user thinks tool failed | Per-tweak `RequiresReboot` flag; post-apply reboot banner listing pending items; re-verify state after reboot |
| Destructive actions styled like safe ones | Debloat/driver purge clicked casually | Risk tiers in UI (Safe / Caution / Requires-reboot), with Caution gated behind expand + confirm; aggressive-irreversible never shown |

## "Looks Done But Isn't" Checklist

- [ ] **Timer tweak:** Shows "Applied" but no reboot + post-reboot re-verify — verify `bcdedit /enum` read-back after restart, or (better) the tweak shouldn't exist.
- [ ] **Service tweak:** Shows "Disabled" but prior StartType wasn't journaled — verify journal entry has old StartType; restore round-trips to Manual, not just Automatic.
- [ ] **Network tweak:** Shows "Applied" but targeted the wrong NIC — verify active-adapter GUID match + `netsh`/`Get-NetTCPSetting` read-back.
- [ ] **Debloat entry:** Shows "Removed" but no reinstall path recorded — verify package full name + reinstall command in journal.
- [ ] **Power/GPU preset:** Works on dev desktop but untested on laptop/iGPU/hybrid — verify hardware-gate branches exercised.
- [ ] **Bulk apply:** Completes fast with no progress/cancel — verify async + cancel + partial-failure undo path actually fires (kill-test mid-preset).
- [ ] **Rollback:** Restore point exists but per-tweak undo untested — verify single-tweak revert without touching anything applied after it.
- [ ] **Build coverage:** Passes on dev build only — verify on Win10 22H2 + Win11 23H2/24H2, and "not applicable" states render correctly.
- [ ] **Explanation copy:** Toggle exists but info panel is placeholder — verify every shipped tweak has effect + risk + revert text (v1 done = trusted control).

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Timer tweaks regressed perf | LOW (if revert path kept) | Re-apply TSC defaults (`deletevalue` both), reboot, verify; remove toggles from presets in next release |
| Services broke Update/Store | MEDIUM | Replay journal StartTypes in reverse; run Update troubleshooter check; reboot; verify Update + Store install; add services to never-disable list |
| AppX/Store removal broke games | HIGH | Re-provision via stored package names (Store reset/reinstall flow); verify Game Pass launch + update; publish keep-list correction |
| Network tweak hurt throughput | LOW | Restore per-GUID snapshot; reset auto-tuning to highly-restricted/default profile; verify with speed/latency check |
| GPU profile lost | MEDIUM–HIGH | Restore exported profile; if missing, vendor-clean-install + reconfigure; add export-before-write gate so it can't recur |
| Bulk apply killed mid-write | MEDIUM | Journal replay of completed items in reverse on next launch (crash-recovery prompt); add cancel-between-tweaks + mutex |
| Build-drift silent no-op | LOW | Add MinBuild/MaxBuild + read-back verify; mark rows "not applicable" with reason |
| Restore unavailable mid-flow | LOW (if journal-first) | Continue per-tweak journaled apply only with explicit consent; banner that system-wide restore is unavailable |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| bcdedit timer myths shipped as gains | Max-performance tuning (CPU/timer/power/Game Mode) | Preset contents reviewed: no `useplatformclock`/`useplatformtick` as optimizations; any timer row is legacy-gated with warning + TSC-default revert tested |
| Service-disable breakage | Clean-system path (services/telemetry/background) | Never-disable allowlist in plan; UAT includes Update check + Store install + controller connect + reboot |
| AppX/Store/WebView removal | Clean-system path | Keep-list vs remove-list review signed off; every remove-row has reinstall path; Store/WebView/Xbox-Identity absent from remove set |
| Placebo network tweaks | Gaming network path | Adapter picker + per-GUID snapshot + `netsh`/`Get-NetTCPSetting` read-back in UI; honest copy review |
| Power/GPU without hardware guards | Max-performance tuning + GPU tuning depth | Hardware gates (chassis/GPU/vendor) in plan; laptop + iGPU + hybrid tested; profile export-before-write demonstrated |
| Restore-point-only rollback | Trusted-control v1 bar (journal + progress + rollback) | Journal schema exists; single-tweak revert + mid-preset cancel + crash-recovery replay all UAT-passed |
| Build-fragile registry paths | Browse-model catalog phase | Catalog schema has build range + verify-read + requires-reboot fields; Win10 22H2 + Win11 23H2/24H2 matrix green |

## Sources

- Melody's timer misconceptions guide (TSC/HPET/PMT/RTC semantics, `useplatformclock`/`useplatformtick` harm, PMT legacy-only cases, anticheat skew risk) — https://sites.google.com/view/melodystweaks/misconceptions-about-timers-hpet-tsc-pmt — HIGH (technical, cross-consistent with forum bench threads)
- TechPowerUp / Overclock.net / Blur Busters timer bench threads (platformclock stutter, `useplatformtick` input lag, timer-resolution interrupt behavior) — community bench reports — MEDIUM (anecdotal but directionally consistent)
- WinUtil FAQ + tweak-revert model (restore-point-before-tweaks, standard-vs-aggressive tiers, post-tweak breakage recovery via re-open/System Restore) — https://winutil.christitus.com/faq/ — HIGH for competitor pattern, MEDIUM as safety evidence (restore-point stalls documented in their issue tracker)
- perfgamer.com safe-debloat guidance + r/Windows11 / Tom's Hardware debloat threads (Store removal breaks update path, service-disable side effects) — MEDIUM (community consensus, not controlled tests)
- SpeedGuide gaming tweaks + r/pcmasterrace Nagle threads (per-interface `TcpNoDelay`, `NetworkThrottlingIndex`/`SystemResponsiveness` vintage, modern-defaults-already-set caveat) — MEDIUM
- AzureToTheMax Defender-disable production incident writeup (policy-disable causing app hangs/freezes) + HP service-disable guidance — MEDIUM-HIGH for Defender risk specifically
- DDU / NZXT / Tom's Hardware driver-clean threads (Safe Mode requirement, half-removed states, clean-install vs DDU scope) — MEDIUM
- Codebase context: `src/AppTemplate.App/Tweaks/*Actions` modules, `AkariTweakCatalog`, `RestorePointActions`, `SetTimerResolutionService`, `SystemInfo` WMI disposal note, admin manifest + safe-boot flows (PROJECT.md constraints) — HIGH (first-party)
