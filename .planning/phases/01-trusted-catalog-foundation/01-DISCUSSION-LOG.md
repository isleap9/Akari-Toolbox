# Phase 1: Trusted Catalog Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-10-04
**Phase:** 1-trusted-catalog-foundation
**Areas discussed:** Apply model (toggles vs bulk), Phase scope (gaming-first vs full-Windows), Row anatomy, Revert and safety

---

## Reset trigger (prior session)

User halted UAT on attempt 1 ("restart the gsd plan from scratch, i dont like the direction") and clarified: the app must still carry FR33THY Ultimate but presented and detailed like Winhance. Full code reset approved (scope question: keep-engine vs full-reset → full reset). Winhance source provided at `../Winhance`; agent extracted row patterns into `.planning/research/winhance-patterns.md`.

## Apply model: toggles vs bulk

| Option | Description | Selected |
|--------|-------------|----------|
| Instant per-row toggles | Winhance model: flip applies immediately, per-row revert | ✓ |
| Tick-then-bulk-apply | Attempt-1 model: stage ticks, Apply button runs all | |

**User's choice:** "go with both" (both agent recommendations: toggles + gaming-first)
**Notes:** Agent recommended toggles — matches the benchmark, deletes the staging-state bug class that killed attempt 1 (CR-01), per-row revert is simpler to trust. SFT-01 bulk wording goes stale; flagged for rewrite during planning (CONTEXT.md D-02).

## Phase scope: gaming-first vs full-Windows

| Option | Description | Selected |
|--------|-------------|----------|
| Gaming set first (8–12 rows) | Proves the row pattern small, then scales to Windows categories | ✓ |
| Full-Windows from the start | Privacy/updates/power/telemetry in Phase 1 | |

**User's choice:** "go with both"
**Notes:** Agent recommended gaming-first — smaller surface, gaming core is the differentiator and sets the template. Full-Windows categories explicitly deferred to later phases.

## Row anatomy (agent-presented, user-confirmed via vision)

Winhance extraction presented: title + one-liner + pills + live toggle, details behind expander. User's earlier UAT feedback ("easy to read and understand like Winhance") is the acceptance anchor. No alternatives offered — the reference source is decisive.

## Revert and safety (agent-presented)

Per-row revert + restore-point offer on first apply; bulk-prefix journal does not return. Presented as part of the toggle recommendation; accepted via "go with both".

---

## the agent's Discretion

- Descriptor schema shape (extend TweakToggle vs new record)
- Journal-vs-direct-revert mechanics for toggle semantics
- Suggestion control choice, empty-state copy
- Page-level "apply recommended to all" left explicitly undecided (not silent scope)

## Deferred Ideas

- Full-Windows categories → later phases
- Game profiles/presets, export/import, auto mode → already in REQUIREMENTS.md v2/out-of-scope

## Dismissed interaction

Gray-area multi-select question was dismissed by the user mid-discussion; conversation continued in plain text ("which is better?" → recommendations → "go with both"). No context lost.
