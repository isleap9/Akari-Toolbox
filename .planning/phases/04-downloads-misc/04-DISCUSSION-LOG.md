# Phase 4: Downloads & Misc - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-09-02
**Phase:** 4-Downloads & Misc
**Areas discussed:** Downloads page identity, "Extra misc tools" (MISC-02), Context-menu safety friction, Context Menu.ps1 replace-vs-add, Installer install method, PostInstall mirror trigger + verification

---

## Initial area selection

User selected all 4 presented gray areas, and appended freeform guidance in the same response rather than answering one at a time:

> "lets keep the downloads the same but lets add more stuffs from my Installers.ps1 in C:\Users\isleap\Desktop\AkariOS Tweaks\4 Installers and the context menu lets change it with my new one Context Menu.ps1 in C:\Users\isleap\Desktop\AkariOS Tweaks\6 Windows"

This immediately resolved the direction of "Downloads page identity" (keep the predecessor's app-installer catalog, expand it) and pointed at a specific replacement script for the context-menu area — both scripts were read in full before continuing.

---

## Downloads page identity

| Option | Description | Selected |
|--------|-------------|----------|
| Port the app-installer catalog (matches actual predecessor code) | | (implied by user's freeform answer — "keep the downloads the same") |
| Build new UI browsing the PostInstall folder contents (matches REQUIREMENTS.md wording) | | |
| Both | | |

**User's choice:** Keep the app-installer catalog as-is, but expand it with more apps from `1 Installers.ps1`.
**Notes:** Discovered mid-discussion that REQUIREMENTS.md's DOWNLOADS-02 wording ("playbooks, drivers, and recommended utility links") does not match the predecessor's actual DownloadsPage implementation at all — that text traces to the Home card's description copy, not the real feature. Flagged as a REQUIREMENTS.md follow-up in CONTEXT.md.

---

## Install method for new apps (Installers.ps1)

| Option | Description | Selected |
|--------|-------------|----------|
| winget + port the hardening steps | Install via winget package ID, then apply the script's post-install hardening (disable telemetry/hardware-accel, remove autostart, delete bloat scheduled tasks) as a separate step | ✓ |
| Script's direct-download method, as-authored | Port the 15 new entries exactly as the script does it — direct CDN download + silent install, bypassing winget | |
| winget only, no hardening | Add apps via winget, skip the hardening steps | |

**User's choice:** winget + port the hardening steps.
**Notes:** Keeps one consistent install mechanism across the whole catalog while preserving the real privacy/performance value the source script adds beyond a bare install.

---

## "Extra misc tools" (MISC-02)

| Option | Description | Selected |
|--------|-------------|----------|
| Defer — not in scope for v1 | No existing implementation or script reference for this; flag a REQUIREMENTS.md follow-up | ✓ |
| I have specific tools in mind | | |

**User's choice:** Defer — not in scope for v1.
**Notes:** Confirmed nothing in the predecessor or the "Ultimate" collection implements this; inventing content wasn't wanted.

---

## Context Menu.ps1 — replace vs. add

| Option | Description | Selected |
|--------|-------------|----------|
| Add alongside | Keep all 12 predecessor entries, add the classic-menu-restore toggle as a 13th item | ✓ |
| Replace entirely | Drop the predecessor's 12 entries; ship only the new toggle | |
| Replace some, keep others | User specifies which to drop | |

**User's choice:** Add alongside.
**Notes:** Initial freeform phrasing ("change it with my new one") sounded like a full replacement, but the two features are structurally unrelated (12 independent custom-command entries vs. one classic-menu-restore/declutter toggle) — clarified via direct follow-up question before locking in.

---

## Misc safety friction

| Option | Description | Selected |
|--------|-------------|----------|
| Confirm Take Ownership only | It recursively grants broad ACL permissions; the other 12 (now 13) are low-blast-radius additive shell entries | ✓ |
| Zero-friction for all 13 | Exact predecessor parity | |
| Confirm on Add only, not Remove | | |

**User's choice:** Confirm Take Ownership only.

---

## PostInstall mirror trigger + verification

| Option | Description | Selected |
|--------|-------------|----------|
| Auto-trigger silently + verify | Auto-run on first Downloads-page visit, use the already-built SHA256 integrity check | ✓ |
| Auto-trigger silently, no verification | Match predecessor behavior exactly, skip the SHA256 check | |
| Explicit button + progress, with verification | Visible action + progress feedback instead of silent background trigger | |

**User's choice:** Auto-trigger silently + verify.
**Notes:** Diverges from Phase 2's D-06 "no added verification" precedent, but the verification primitive already exists here (unlike Phase 2's driver scripts) so using it isn't extra work — user confirmed they want it used.

---

## Claude's Discretion

- Exact category assignment for the 15 new Downloads apps (D-05)
- Exact per-file expected-SHA256 source — checked-in manifest vs. runtime-fetched (D-08)
- How the new 13th Misc entry's 2-branch script maps onto the existing Add/Remove dispatch shape (Clean→Add/On, Default→Remove/Off)

## Deferred Ideas

- MISC-02 ("extra misc tools") — no existing implementation to draw from; needs a REQUIREMENTS.md follow-up edit
- The remaining 4 scripts in `4 Installers/` (MSI Afterburner, Nvidia Profile Inspector, More Clock Tool, CRU SRE) — not examined, not pulled into this phase's scope
