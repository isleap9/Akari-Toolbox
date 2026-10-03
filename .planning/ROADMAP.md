# Roadmap: Akari Toolbox — Gaming Optimizer Push

## Overview

From a working 13-page WinUI 3 shell with a native tweak engine to a Winhance-style gaming optimizer: first the trusted catalog foundation (searchable browse + explanations + snapshot-backed bulk apply/revert), then the gaming tweak verticals riding on it — system/background, network latency, GPU scheduling — each explained, hardware-gated, and reversible.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Trusted Catalog Foundation** - Searchable explained catalog with snapshot-backed bulk apply and one-click revert
- [ ] **Phase 2: System & Background Tune** - Game Mode/DVR plus startup and background-app control
- [ ] **Phase 3: Gaming Network Path** - Per-adapter latency tuning with read-back verification
- [ ] **Phase 4: GPU Scheduling Toggle** - Hardware-gated HAGS toggle with restart notice

## Phase Details

### Phase 1: Trusted Catalog Foundation
**Goal**: Users can find any gaming tweak by search, understand it before toggling, and bulk-apply/revert safely
**Mode:** mvp
**Depends on**: Nothing (first phase)
**Requirements**: BRW-01, BRW-02, SFT-01
**Success Criteria** (what must be TRUE):
  1. User can find any gaming tweak by typing in search within a categorized catalog
  2. User can read a per-tweak explanation (what it does, why it helps gaming, risk, how it reverts) before toggling
  3. User can bulk-apply selected gaming tweaks with visible progress
  4. User can revert all applied tweaks with one click and see prior values restored
**Plans**: TBD
**UI hint**: yes

### Phase 2: System & Background Tune
**Goal**: Users get a clean, game-ready system from one place without breaking Windows
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: SYS-01, BKG-01
**Success Criteria** (what must be TRUE):
  1. User can enable Game Mode and disable Game DVR / background recording from one place
  2. User can control startup apps and background UWP apps without opening Task Manager
  3. Every toggle in this phase reads back its current state and reverts cleanly via the Phase 1 journal
**Plans**: TBD

### Phase 3: Gaming Network Path
**Goal**: Users can apply the full latency-reduction path with clarity about what changed per adapter
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: NET-01
**Success Criteria** (what must be TRUE):
  1. User can apply the gaming latency path (Nagle off, network throttling off, auto-tuning Normal, DNS flush) from one place
  2. User sees which adapter (Wi-Fi vs Ethernet) each setting targets before applying
  3. Applied network settings are verified by read-back and revert cleanly via the Phase 1 journal
**Plans**: TBD

### Phase 4: GPU Scheduling Toggle
**Goal**: Users can toggle Hardware-Accelerated GPU Scheduling safely with full hardware honesty
**Mode:** mvp
**Depends on**: Phase 1
**Requirements**: GPU-01
**Success Criteria** (what must be TRUE):
  1. User sees whether HAGS is supported on their hardware, with a clear N/A state when it is not
  2. User can toggle HAGS on/off and gets a restart notice when the change needs a reboot
  3. HAGS toggle reverts cleanly via the Phase 1 journal
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Trusted Catalog Foundation | 0/TBD | Not started | - |
| 2. System & Background Tune | 0/TBD | Not started | - |
| 3. Gaming Network Path | 0/TBD | Not started | - |
| 4. GPU Scheduling Toggle | 0/TBD | Not started | - |
