# Phase 1: Trusted Catalog Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-10-03
**Phase:** 1-Trusted Catalog Foundation
**Areas discussed:** Catalog scope, Suggest detail

---

## Catalog scope — seed set

| Option | Description | Selected |
|--------|-------------|----------|
| Gaming seed | Current gaming rows plus Game Mode DVR, curated seed set | ✓ |
| Full sweep | Gaming plus system network GPU rows up front | |
| Absorb all | Unify all 169 individual rows into one catalog | |

**User's choice:** Gaming seed
**Notes:** Phases 2-4 rows stay in their own phases.

## Catalog scope — page location

| Option | Description | Selected |
|--------|-------------|----------|
| Rework | Turn GamingPage into the catalog | |
| New page | Add a new page for the catalog | ✓ |
| Shared | Catalog as shared rows, pages stay | |

**User's choice:** New page
**Notes:** None.

## Catalog scope — grouping

| Option | Description | Selected |
|--------|-------------|----------|
| Four groups | System Network GPU Background groups | ✓ |
| Flat list | One flat gaming list with search only | |
| Two levels | Gaming plus Advanced subsections | |

**User's choice:** Four groups
**Notes:** None.

## Catalog scope — relation to 169 existing rows

| Option | Description | Selected |
|--------|-------------|----------|
| Cross ref | Gaming catalog links to shared row definitions | ✓ |
| Duplicate | Gaming rows are separate copies | |
| Leave alone | Do not touch the 169 rows in v1 | |

**User's choice:** Cross ref
**Notes:** None.

## Catalog scope — search behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Instant | Filter rows as you type | |
| Suggest | Pick from a suggestion dropdown | ✓ |
| Both | Both type filter and category picker | |

**User's choice:** Suggest
**Notes:** Led to the Suggest detail follow-up area.

## Catalog scope — explanation placement

| Option | Description | Selected |
|--------|-------------|----------|
| Inline | Description text under each row | ✓ |
| Expand | Tap row to expand full details | |
| Dialog | Small info button opens a dialog | |

**User's choice:** Inline
**Notes:** None.

## Catalog scope — bulk selection

| Option | Description | Selected |
|--------|-------------|----------|
| Ticked | Tick rows then apply ticked rows | ✓ |
| Apply all | One button applies every row | |
| Per group | Per group apply buttons | |

**User's choice:** Ticked
**Notes:** None.

## Catalog scope — reboot rows

| Option | Description | Selected |
|--------|-------------|----------|
| Badge | Badge on rows needing a reboot | ✓ |
| Grouped | Group them in a separate section | |
| On apply | Warn only when applying them | |

**User's choice:** Badge
**Notes:** None.

## Catalog scope — ticks after apply

| Option | Description | Selected |
|--------|-------------|----------|
| Sticky | Ticked rows stay ticked after apply | ✓ |
| Clear | Clear ticks after a clean apply | |
| Per row | Show per row done or failed state | |

**User's choice:** Sticky
**Notes:** None.

## Catalog scope — progress surface

| Option | Description | Selected |
|--------|-------------|----------|
| Footer | Shell footer bar shows progress | ✓ |
| Dialog | Dialog with per row progress | |
| Inline | Inline spinner on each row | |

**User's choice:** Footer
**Notes:** Reuses the existing shell status footer.

## Catalog scope — row failure

| Option | Description | Selected |
|--------|-------------|----------|
| Stop | Stop at first failure and roll back | ✓ |
| Finish all | Finish all rows then report failures | |
| Skip | Skip failed rows but keep going | |

**User's choice:** Stop
**Notes:** None.

## Catalog scope — restore point gate

| Option | Description | Selected |
|--------|-------------|----------|
| Required | Block apply until restore point exists | |
| Offer | Offer it but allow skip | ✓ |
| Auto | Silent auto create then apply | |

**User's choice:** Offer
**Notes:** Apply is never blocked on the restore point.

## Catalog scope — revert control

| Option | Description | Selected |
|--------|-------------|----------|
| Top bar | One revert button at top of page | ✓ |
| Near apply | Revert button next to apply button | |
| Both | Per row revert plus global revert | |

**User's choice:** Top bar
**Notes:** Single one-click revert.

## Suggest detail — match text

| Option | Description | Selected |
|--------|-------------|----------|
| Titles | Match titles only | ✓ |
| Plus desc | Match titles and descriptions | |
| All text | Match everything incl technical keys | |

**User's choice:** Titles
**Notes:** None.

## Suggest detail — ranking

| Option | Description | Selected |
|--------|-------------|----------|
| Prefix first | Starts with beats contains | ✓ |
| Group boost | Gaming group rows rank higher | |
| Alpha | Keep it simple, alpha order | |

**User's choice:** Prefix first
**Notes:** None.

## Suggest detail — tap behavior

| Option | Description | Selected |
|--------|-------------|----------|
| Filter | Tapping a suggestion filters the list | ✓ |
| Jump | Tapping jumps straight to the row | |
| Tick | Tapping ticks the row | |

**User's choice:** Filter
**Notes:** None.

## Suggest detail — appearance

| Option | Description | Selected |
|--------|-------------|----------|
| One char | Start typing to see matches | ✓ |
| Two plus | Wait for two or three chars | |
| Show all | Show all rows when empty | |

**User's choice:** One char
**Notes:** None.

---

## the agent's Discretion

No areas delegated. Planner has flexibility on descriptor schema shape, journal storage location, and suggestion control choice.

## Deferred Ideas

None — discussion stayed within phase scope. Rollback detail, nav slot position, and proof bar were offered as follow-up areas and declined.
