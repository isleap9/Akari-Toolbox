# Phase 1: Foundation & Akari OS Tweaks - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-08-31
**Phase:** 1-Foundation & Akari OS Tweaks
**Areas discussed:** Log/status console, Placeholder scaffolding

---

## Log/status console

| Option | Description | Selected |
|--------|-------------|----------|
| Persistent console (matches predecessor) | Keep a docked log panel + progress bar visible across all pages | ✓ |
| Per-page/toast status | Use the framework's IInfoBarService for transient status messages per page instead | |
| Other | Something in between, or a different idea | |

**User's choice:** Persistent console (matches predecessor)
**Notes:** Follow-up questions resolved placement (docked, collapsible — not fixed/always-expanded) and history behavior (clears on launch, in-memory only, no session log file for Phase 1).

| Option | Description | Selected |
|--------|-------------|----------|
| Docked panel, collapsible (Recommended) | Bottom or side panel, always present but collapsible | ✓ |
| Always-expanded, fixed | Exact predecessor match, no collapse option | |
| Other | Different placement | |

| Option | Description | Selected |
|--------|-------------|----------|
| Clear on launch (matches predecessor) | In-memory only, resets each session | ✓ |
| Also write to a session log file | Also appends to the framework's FileLoggerProvider rolling log | |

---

## Placeholder scaffolding

| Option | Description | Selected |
|--------|-------------|----------|
| Show disabled placeholders | All destination cards/nav entries appear now, labeled "Coming soon" | ✓ |
| Only show what's built | Home shows just the built Akari OS Tweaks card; each phase adds its own | |
| Other | Different approach | |

**User's choice:** Show disabled placeholders
**Notes:** Follow-up resolved whether to add a 5th Debloat card to Home (predecessor only had 4, with Debloat nav-only) — user chose to add it for consistency.

| Option | Description | Selected |
|--------|-------------|----------|
| Add a 5th Debloat card | Home shows all 5 destination pages as cards | ✓ |
| Keep 4 cards, nav-only for Debloat | Match predecessor exactly | |

---

## Claude's Discretion

- **Defender restart UX** — raised as a possible discussion area but not selected by the user. Default resolved: log-message-only, exact parity with the predecessor (no "Restart Now" button or other new UI affordance).
- **"Coming soon" placeholder visual treatment** (grayed card vs. lock icon vs. badge text) — left to Claude's judgment during planning/implementation.

## Deferred Ideas

None — discussion stayed within Phase 1 scope.
