---
status: testing
phase: 01-trusted-catalog-foundation
source: [01-01-SUMMARY.md, 01-02-SUMMARY.md, 01-03-SUMMARY.md]
started: 2026-10-04T00:00:00Z
updated: 2026-10-04T00:00:00Z
---

## Current Test

number: 3
name: Nav click-through sweep (verify-pages.ps1)
expected: |
  Search filters rows by title from the first character with max 8 suggestions in a scrollable dropdown; tapping a suggestion filters the list without ticking. Apply selected (N) is disabled at zero and tracks the tick count; applying shows Applying {done} of {total} live in the status footer with controls disabled; a failing row stops the run and flags that row while later rows stay untouched; ticks stay sticky after success; restore-point offer appears with a Skip path and never blocks; Revert all changes is one click with no confirmation and restores prior values — including after an apply-apply-revert cycle (the CR-01 fix: originals return, not intermediate values).
awaiting: user response

## Tests

### 1. Catalog page in shell nav and first-run browse
expected: Launch the app elevated (build-and-run.ps1). Gaming Catalog appears in shell nav after Gaming Tweaks. Opening it shows four group cards (System, Network, GPU, Background) with ticked/unticked rows, inline four-line explanations under each title, Requires restart badges on reboot rows (inline, never segregated), and typing a nonsense search shows the No matching tweaks empty state with the name-only hint.
result: issue
reported: "but i wanted something like winhance easy to read and understand"
severity: minor

### 2. Search, bulk apply with progress, and one-click revert (incl. apply-apply-revert)
expected: Search filters rows by title from the first character with max 8 suggestions in a scrollable dropdown; tapping a suggestion filters the list without ticking. Apply selected (N) is disabled at zero and tracks the tick count; applying shows Applying {done} of {total} live in the status footer with controls disabled; a failing row stops the run and flags that row while later rows stay untouched; ticks stay sticky after success; restore-point offer appears with a Skip path and never blocks; Revert all changes is one click with no confirmation and restores prior values — including after an apply-apply-revert cycle (the CR-01 fix: originals return, not intermediate values).
result: blocked
blocked_by: release-build
reason: "i cant test it since it didnt build it so i can transfer it to my virtual machine"

### 3. Nav click-through sweep (verify-pages.ps1)
expected: Run verify-pages.ps1 -Exe <real path to AkariToolbox.App.exe> (the script default $Exe path is stale, so -Exe is required). The sweep clicks through all 14 nav items including Gaming Catalog and passes.
result: [pending]

### 4. Catalog seeds with shared-row cross-references
expected: Gaming catalog seeds preemption, network-opt, game-mode, game-dvr plus 4 shared-row cross-references with zero duplicated .reg text.
result: pass
source: automated
coverage_id: 01-01/D1

### 5. Four-line inline explanations with no implementation locations
expected: Every row renders What/Why/Risk/Reverts-by inline, one sentence each, no implementation locations named.
result: pass
source: automated
coverage_id: 01-01/D2

### 6. Title-only prefix-first search ranking
expected: Title-only prefix-first search, first-character trigger, 8-item cap, tap-to-filter without ticking.
result: pass
source: automated
coverage_id: 01-01/D3

### 7. Journal capture and reverse-replay revert
expected: Journal captures full footprints and reverse-replays them, including absent-to-delete semantics.
result: pass
source: automated
coverage_id: 01-01/D4

### 8. Bulk apply on ticked rows with stop-at-first-failure
expected: Bulk apply acts on ticked rows with footer progress, stops at first failure, ticks stay sticky, restore offered with Skip, one-click revert.
result: pass
source: automated
coverage_id: 01-01/D5

### 9. Search precision, contract copy, failure-marker plumbing
expected: Search/suggest precision, contract copy, and failure-marker plumbing in the VM plus domain.
result: pass
source: automated
coverage_id: 01-02/D1

### 10. Release gates green
expected: Full app build holds Errors 0 Warnings 0 and the full test suite is green (119/119).
result: pass
source: automated
coverage_id: 01-02/D3

### 11. First-capture-wins journal guard with regression test
expected: MarkApplied is first-capture-wins; apply-apply-revert on a scratch Sentinel restores the first original.
result: pass
source: automated
coverage_id: 01-03/D1

### 12. Single-run round-trips unaffected, gates green
expected: Single-run journal round-trips unaffected; build bar holds and full suite is green.
result: pass
source: automated
coverage_id: 01-03/D2

## Summary

total: 12
passed: 9
issues: 1
pending: 1
skipped: 0
blocked: 1

## Gaps

- gap_id: G-01-1
  truth: "Gaming Catalog page reads like Winhance — easy to read and understand (four groups, inline explanations, restart badges, empty state)"
  status: failed
  reason: "User reported: but i wanted something like winhance easy to read and understand"
  severity: minor
  test: 1
  artifacts: []
  missing: []
