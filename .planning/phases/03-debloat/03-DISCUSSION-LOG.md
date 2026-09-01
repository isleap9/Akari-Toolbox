# Phase 3: Debloat - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-09-01
**Phase:** 3-Debloat
**Areas discussed:** Action & state model, Safety friction for risky actions, Script replacement (Bloatware, Edge Remove, Edge Debloat) — raised mid-discussion by the user

---

## Action & State Model

| Option | Description | Selected |
|--------|-------------|----------|
| Action-log parity | Run + optional Undo buttons, no live state read-back — matches predecessor and DEBLOAT-01's "run each action" wording | ✓ |
| Full state detection | Convert every action to a live-state toggle like Tweaks/Gaming (`ITweakHandler` pattern) | |
| Hybrid | State-detect registry-backed toggles, keep Run/Undo for one-shot actions | |

**User's choice:** Action-log parity (Recommended)
**Notes:** Several actions (DiskCleanup, TempFiles, RestorePoint, OOSU) have no meaningful on/off state to read, which was the deciding factor.

---

## Undo Gating

| Option | Description | Selected |
|--------|-------------|----------|
| Always enabled | Undo clickable regardless of session history — parity with predecessor | ✓ |
| Gated on Run-this-session | Undo disabled until matching Run clicked in current session | |

**User's choice:** Always enabled (Recommended)

---

## Script Swap: Unwanted Apps Removal

User interjected mid-discussion: "we need totally remove the old Unwated Apps -- Remove with bloatware.ps1 in C:\Users\isleap\Desktop\AkariOS Tweaks\6 Windows"

Investigation found `13 Bloatware.ps1` — a much broader script than the predecessor's `Debloat.ps1` (hardcoded ~29-app list). Its "Remove All Bloatware" branch also disables optional Windows features/capabilities and removes OneDrive/RDC/SnippingTool/GameInput as side effects.

| Option | Description | Selected |
|--------|-------------|----------|
| Port as-authored | Keep the full branch exactly as written, broader blast radius accepted | ✓ |
| Trim to app removal only | Strip optional-features/capabilities disabling and side-removals | |

**User's choice:** Port as-authored (Recommended)

| Option | Description | Selected |
|--------|-------------|----------|
| No Undo button | Old undo was unreliable (8-app best-effort); new script has no single equivalent | |
| Undo = reinstall UWP apps only | Wire Undo to script's "Install: All UWP Apps" branch (option 4) | ✓ |

**User's choice:** Undo = reinstall UWP apps only

---

## Safety Friction for Risky Actions

| Option | Description | Selected |
|--------|-------------|----------|
| Confirmation dialogs | New safety behavior via framework's `IDialogService`, beyond parity | ✓ |
| Zero-friction parity | Every action runs immediately on click, matching predecessor exactly | |

**User's choice:** Confirmation dialogs (Recommended)

| Option | Description | Selected |
|--------|-------------|----------|
| Claude proposes at planning | Risk classification for all actions drafted during planning, approved before implementation | ✓ |
| Decide the list now | User specifies exact list of gated actions | |

**User's choice:** Claude proposes at planning (Recommended)

---

## Script Swap: Microsoft Edge Actions

User interjected again: "also the old edge remove and debloat need to be replace with Edge & WebView.ps1 in C:\Users\isleap\Desktop\AkariOS Tweaks\6 Windows and also the edge debloat need to be replaced with Edge Settings.ps1 in C:\Users\isleap\Desktop\AkariOS Tweaks\3 Setup"

Confirmed mapping: "Microsoft Edge — Remove" → `20 Edge & WebView.ps1` branch 1 (Uninstall); "Microsoft Edge — Debloat" → `10 Edge Settings.ps1` branch 1 (Optimize).

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, that's the mapping | Edge Remove → Edge & WebView.ps1; Edge Debloat → Edge Settings.ps1 | ✓ |
| Different mapping | User would clarify | |

**User's choice:** Confirmed the mapping as stated.

`20 Edge & WebView.ps1`'s Uninstall branch also removes the WebView2 runtime — flagged against REQUIREMENTS.md's Out-of-Scope entry on full WebView2 removal (most-cited cause of debloat breakage).

| Option | Description | Selected |
|--------|-------------|----------|
| Port as-authored, override the exclusion | Keep full script including WebView2 removal; update REQUIREMENTS.md to record the override | ✓ |
| Trim WebView2 removal out | Port only Edge-uninstall portion, skip WebView2-specific steps | |

**User's choice:** Port as-authored, override the exclusion

| Option | Description | Selected |
|--------|-------------|----------|
| Yes, Undo = branch 2 for both | Edge & WebView's Undo reinstalls via downloaded installers; Edge Settings' Undo clears policies and reinstalls | ✓ |
| No Undo for one or both | User would specify | |

**User's choice:** Yes, Undo = branch 2 for both (Recommended)

---

## Claude's Discretion

- Exact list of actions requiring a confirmation dialog (proposed at planning for approval)
- Reconciling the 28 vs 29 action-count discrepancy between DEBLOAT-01 and the predecessor's actual button count
- Category grouping fidelity (default: keep predecessor's 5 groups)
- Per-row busy indicator and run concurrency/serialization
- Non-interactive extraction technique for the three replacement scripts' console-menu shape

## Deferred Ideas

None — discussion stayed within phase scope, including the two mid-discussion script-replacement decisions (direct substitutions for existing in-scope actions, not new capabilities).
