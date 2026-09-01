# Phase 2: Gaming Tweaks - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-09-01
**Phase:** 2-Gaming Tweaks
**Areas discussed:** PostInstall dependency, Live-state read for toggles/dropdowns, Tool launcher grid scope, Service dropdown scope, SvcHost/Win32 Priority enhancement, GAMING-02 retirement

---

## PostInstall dependency

Initial framing assumed PostInstall would eventually be mirrored (Phase 4), and asked how Phase 2 should cope with the gap. The user interrupted to clarify the real situation: PostInstall is deprecated entirely and no longer maintained — everything must become native, sourced from scripts the user provides.

| Option | Description | Selected |
|--------|-------------|----------|
| Pull fetch forward | Gaming becomes first real consumer of IPostInstallService | |
| Detect & gray out | Wire up UI, check file existence, show install-assets messaging | |
| Registry-only for v1 | Build only non-file-dependent tweaks now | |
| **(actual resolution)** | **PostInstall dropped entirely — no dependency in any form** | ✓ |

**User's choice:** Drop PostInstall completely; native scripts only, user will provide them.
**Notes:** This reframed the entire area — see below for the follow-on scoping discussion.

---

## Native script sourcing (HDCP/Network Optimization rewrite)

| Option | Description | Selected |
|--------|-------------|----------|
| Embed as PS/bat resources | Same pattern as Defender tweak | |
| Rewrite as pure C# registry writes | Port script logic directly into tweak handler, no process-spawn | ✓ |
| Mix of both | Whichever fits each script | |

**User's choice:** Rewrite as pure C# registry writes.
**Notes:** Superseded shortly after by the broader "whole page revamp" decision once the `5 Graphics` script source was identified.

---

## Tool grid sourcing / page revamp scope

User pointed to `C:\Users\isleap\Desktop\AkariOS Tweaks\5 Graphics` as the new source for the tool grid, then clarified: "the entire tab needs a revamp." Claude scouted the folder (13 scripts: 7 local toggles, 2 one-shot shortcuts, 6 network-dependent installer scripts) and asked whether the revamp extended to the SvcHost/Win32Priority/Services-preset dropdowns too.

| Option | Description | Selected |
|--------|-------------|----------|
| Only toggles + tool grid | Dropdowns carry over unchanged | |
| Whole page, start over | Reconsider everything including dropdowns | ✓ |

**User's choice:** Whole page, start over.

---

## Network-dependent installer scripts (Driver Clean/DDU, Driver Install Latest, Driver Install Debloat & Settings, DirectX, C++, Nvidia Settings)

First attempt at this question was answered with a pointer to a different folder (`6 Windows`, thought to be about network tweaks) — clarified after investigation that `6 Windows` was actually the answer to a *different*, not-yet-asked question about latency/network tweaks elsewhere in the collection. The original question was re-asked; user then asked Claude to explain the options in plain language before answering.

| Option | Description | Selected |
|--------|-------------|----------|
| Keep it, verify integrity | Add SHA256 verification before running | |
| Keep it, as-is | No added verification, matches script's current trust model | ✓ |
| Drop from v1 scope | Only port no-network scripts now | |

**User's choice:** Keep it, as-is (no added verification).

---

## Tool launcher grid (predecessor's ~29-button NVIDIA/AMD/Useful-Tools grid)

| Option | Description | Selected |
|--------|-------------|----------|
| Drop it entirely | No separate launcher grid in v1 | ✓ |
| Keep a trimmed grid | Only tools the user can supply natively | |
| You decide | Claude proposes | |

**User's choice:** Drop it entirely.

---

## 6 Windows folder scope ("my new network tweaks")

User's answer to the driver-installer-scripts question instead pointed to `C:\Users\isleap\Desktop\AkariOS Tweaks\6 Windows` (36 scripts, mostly unrelated to Gaming — Bloatware, Start Menu, Theme, etc.). Claude proposed a candidate subset based on titles (#25-30, the latency/power-savings/network scripts) and asked for confirmation rather than guessing silently.

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, that subset (#25-30) | Device Manager Power Savings & Wake, Network Adapter Power Savings & Wake, Network IPv4 Only, Write Cache Buffer Flushing, Power Plan, Timer Resolution | ✓ |
| Different subset | User names specific scripts | |

**User's choice:** Confirmed #25-30.

---

## SvcHost / Win32 Priority Separation dropdowns

| Option | Description | Selected |
|--------|-------------|----------|
| Keep as-is | Same value lists as predecessor | |
| I want to change these | User describes what's different | ✓ |

**User's choice:** "enhance it we need to add more" → follow-up clarified as more preset values, with exact values left to Claude/research to propose.

---

## Services preset dropdown replacement

Claude found `8 Advanced\17 Services.ps1` (services on/off toggle + auto restore-point creation) in the user's Ultimate collection and asked if it should replace the old Services-preset dropdown.

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, use that script | Replace old dropdown with this toggle | |
| No, drop it | No services preset/toggle in Gaming Tweaks v1 | ✓ |
| Different source | User points elsewhere | |

**User's choice:** No, drop it.

---

## GAMING-02 requirement conflict

Dropping the tool launcher grid means REQUIREMENTS.md's GAMING-02 ("launch quick-access tools... from a launcher grid") is no longer satisfied by this phase. Claude flagged the conflict rather than silently leaving it unresolved.

| Option | Description | Selected |
|--------|-------------|----------|
| Retire GAMING-02 | Amend REQUIREMENTS.md — superseded by the native toggle set | ✓ |
| Keep GAMING-02, different shape | User redefines what "quick-access tools" means now | |

**User's choice:** Retire GAMING-02.

---

## Live-state read discipline

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, same D-03/D-04 rule | Live read + real-prior-value revert, same as Phase 1 | ✓ |
| No, exception needed | User names which toggles should be exempt | |

**User's choice:** Yes, same D-03/D-04 rule — applies to all new local toggles.

---

## Claude's Discretion

- Exact expanded preset value lists for SvcHost split threshold / Win32 Priority Separation dropdowns — Claude/research proposes for approval at planning time.
- Technical adaptation of interactive `Read-Host`-driven scripts into non-interactive toggle invocations.
- Per-script mapping of "Recommended"/"Default" branches to On/Off toggle state.

## Deferred Ideas

- The other 30 scripts in `6 Windows` (Bloatware checks, Start Menu, Theme, Widgets, Copilot, Edge/WebView, Notepad, Control Panel, UAC, Core Isolation, Defender Optimize, Autoruns Startup, Cleanup, Restore Point) — likely Debloat (Phase 3) or Misc (Phase 4) material.
- `8 Advanced/17 Services.ps1` — declined for Gaming; its auto-restore-point behavior is relevant to the already-deferred v2 SAFE-01 requirement.
- The remaining ~97 scripts across the other "Ultimate" collection folders (`1 Check`, `2 Refresh`, `3 Setup`, `4 Installers`, `7 Hardware`, `8 Advanced` minus `17 Services.ps1`) — untouched, still v2/ULT-01 territory unless pulled forward by a future phase discussion.
