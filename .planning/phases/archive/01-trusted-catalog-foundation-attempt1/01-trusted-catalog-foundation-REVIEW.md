---
phase: 01-trusted-catalog-foundation
reviewed: 2026-10-04T00:00:00Z
depth: deep
files_reviewed: 10
files_reviewed_list:
  - src/AppTemplate.App/Tweaks/GamingCatalog.cs
  - src/AppTemplate.App/Tweaks/GamingJournal.cs
  - src/AppTemplate.App/Tweaks/GamingRunner.cs
  - src/AppTemplate.App/Tweaks/GamingActions.cs
  - src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs
  - src/AppTemplate.App/Views/GamingCatalogPage.xaml
  - src/AppTemplate.App/Views/GamingCatalogPage.xaml.cs
  - src/AppTemplate.App/App.xaml.cs
  - src/AppTemplate.App/MainWindow.xaml.cs
  - src/AppTemplate.Tests/GamingCatalogTests.cs
findings:
  critical: 1
  warning: 9
  info: 9
  total: 19
status: issues_found
---

# Phase 01-trusted-catalog-foundation: Code Review Report

**Reviewed:** 2026-10-04T00:00:00Z
**Depth:** deep
**Files Reviewed:** 10
**Status:** issues_found

## Summary

Reviewed the Gaming Catalog slice end to end: catalog definition, journal
(snapshot/revert), bulk runner, native actions, view model + page, DI/nav
wiring, and the 19-test tracer suite. Cross-file tracing covered the
probe/apply/capture hive paths (`RegRead` vs `regedit.exe` import vs
`OpenRealHkcu`), the journal persistence round-trip, and the runner/journal/VM
interaction across re-runs. All 19 `GamingCatalogTests` pass (verified by
executing the suite, including the `0xDEADBEEF` high-bit DWORD sentinel
round-trip). No hardcoded secrets, injection vectors, or dangerous-function
misuse found; user-influenced strings (QoS profile name, exe path) are
validated or never executed.

One Critical finding stands: the sticky-tick re-run workflow the UI
encourages silently destroys the journal's original baseline, voiding the
one-click revert safety net. Nine Warnings cover hive-path inconsistency,
incomplete network revert, silent `netsh` failures, and test-hygiene issues.
Nine Info items are minor quality notes. Advisory only — none of this gates
the phase.

## Critical Issues

### CR-01: Sticky-tick re-run clobbers the original revert baseline

**File:** `src/AppTemplate.App/Tweaks/GamingJournal.cs:113-127` (with `src/AppTemplate.App/Tweaks/GamingRunner.cs:38-43`, `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs:224`)
**Issue:** `MarkApplied` unconditionally replaces any existing journal entry
for the same catalog ID ("Re-capturing an ID replaces its snapshot"). The
runner captures before every apply, and the VM deliberately keeps ticks
selected after a run ("Ticks stay selected for re-run", shown in the success
InfoBar). So apply → apply (the encouraged re-run) snapshots the
already-applied values and overwrites the original priors. A subsequent
one-click revert then writes back the applied values — a silent no-op — and
the true originals are unrecoverable through the tool. The headline safety
feature (SFT-01 revert half) is voided by the headline UX (D-12 sticky
ticks). No test covers apply-twice-then-revert, so the suite stays green
while the safety net is gone.
**Fix:**
```csharp
// First capture wins: preserve the original baseline across re-runs.
public void MarkApplied(string catalogId)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(catalogId);
    lock (_lock)
    {
        if (!_pending.Remove(catalogId, out JournalEntry? captured))
            return;
        GamingJournalDoc current = LoadLocked();
        if (current.Entries.Any(e => e.CatalogId == catalogId))
            return; // baseline already journaled; keep the original priors
        List<JournalEntry> entries = current.Entries.ToList();
        entries.Add(captured);
        SaveLocked(new GamingJournalDoc(CurrentVersion, entries));
    }
}
```
Add a test: capture → mutate → mark → capture → mutate → mark → revert, and
assert the restored value is the first original, not the intermediate.

## Warnings

### WR-01: HKCU hive handling disagrees across probe, apply, and capture paths

**File:** `src/AppTemplate.App/Tweaks/GamingJournal.cs:319-333` (with `src/AppTemplate.App/Tweaks/ControlPanelRows.cs:32-42`, `src/AppTemplate.App/Tweaks/NativeOps.cs:896-907`, `src/AppTemplate.App/Tweaks/GamingActions.cs:338-343`)
**Issue:** Three different HKCU resolutions are in play for the same tweaks.
Shared-row apply goes through `ControlPanelRows.ApplyRow` →
`NativeOps.ImportRegContent` → `regedit.exe /S`, which writes the
*elevated* account's HKCU. Shared-row probes go through
`ReadRowOptimized` → `RegRead`, i.e. `Registry.CurrentUser`, also the
elevated hive. But journal capture goes through `GamingJournalStore.OpenKey`
→ `AkariToolState.OpenRealHkcu`, the *interactive* user's hive. Under
same-user UAC elevation all three coincide, so this is latent — but under
separate-account (over-the-shoulder) elevation they diverge: apply lands in
the admin's hive (no effect for the interactive user), capture snapshots the
interactive hive (unchanged), and revert is a no-op. Same family:
`AutoDscpFse` writes its HKCU compat/GPU-preference values via `TweakAction`
(`Registry.CurrentUser`) instead of `OpenRealHkcu`, unlike Game Mode/Game DVR
in the same file.
**Fix:** Route all three paths through one helper. Either make
`ImportRegContent` rewrite `HKEY_CURRENT_USER`/`HKCU` headers to
`HKEY_USERS\<interactive-SID>` before invoking `regedit.exe`, or replace the
`regedit.exe` import for shared rows with direct `OpenRealHkcu` writes; make
`ReadRowOptimized` probe HKCU via `AkariToolState.OpenRealHkcu` as well, and
switch `AutoDscpFse`'s HKCU writes to the interactive-hive helpers.

### WR-02: Network revert restores only NIC values; netsh/binding changes are never reverted and the copy does not say so

**File:** `src/AppTemplate.App/Tweaks/GamingActions.cs:179-184` (with `src/AppTemplate.App/Tweaks/GamingCatalog.cs:188-203`)
**Issue:** `ApplyNetworkOptimization` performs three classes of mutation:
NIC property writes (journaled via dynamic footprint), `netsh` global-stack
changes (10 commands: DCA, RSS, timestamps, RTO, taskoffload, …), and
`disable-netadapterbinding` removals. `RevertNetworkOptimization` runs only
`netsh winsock reset`, which does not undo the other nine `netsh` settings
or re-enable removed bindings. The in-code comment claims these are
"disclosed in the catalog's RevertsBy copy", but the actual `RevertsBy`
string is "Puts back your saved adapter settings from the journal." — no
mention of the irreversible netsh/binding side effects. One-click revert
over-promises for this row.
**Fix:** Either extend revert coverage (re-apply recorded prior `netsh`
state — capture `netsh int tcp show global` output into the journal entry —
and re-enable the removed bindings), or fix the `RevertsBy` copy to state
plainly that adapter settings revert from the journal while TCP-stack and
binding changes require manual remediation.

### WR-03: netsh/PowerShell failures are swallowed and reported as applied

**File:** `src/AppTemplate.App/Tweaks/GamingActions.cs:168-174` (with `src/AppTemplate.App/Tweaks/NativeOps.cs:225-243`)
**Issue:** `NativeOps.RunTool` catches all exceptions and never checks the
process exit code, and `PowerShellCommand` delegates to it. The network
apply's binding-removal cmdlet and all 10 `netsh` invocations therefore fail
silently; `ApplyNetworkOptimization` still calls `AkariToolState.Save`, the
runner journals success, and the UI reports the tweak applied. A machine
where `netsh` is restricted or the binding cmdlet fails ends up with NIC
values changed but stack changes missing, recorded as fully applied.
**Fix:** Check `p?.ExitCode` in `RunTool` (or add a throwing
`RunToolChecked` used by apply paths) and let failures propagate so the
runner's stop-at-first-failure marks the row failed instead of applied.

### WR-04: Preemption probe covers 2 of 6 footprint values

**File:** `src/AppTemplate.App/Tweaks/GamingActions.cs:35-37` (with `src/AppTemplate.App/Tweaks/GamingCatalog.cs:166-173`)
**Issue:** `ReadPreemption` returns true when *either* of two values matches
(`EnablePreemption is 0 || DisablePreemption is 1`), while apply writes six
values and the footprint journals six. A partially applied or externally
modified scheduler state (e.g. only one of the two probed values set)
reports "Applied". The journal comment ("completeness is driven by
footprint, never by the read probe") acknowledges the asymmetry but the row
label still derives from the partial probe.
**Fix:** Probe all six footprint values and require unanimity (missing key
counts as not-applied), or document why the two-value probe is authoritative
and shrink the footprint claim accordingly.

### WR-05: Game Mode reads "Applied" on machines that never opted in

**File:** `src/AppTemplate.App/Tweaks/GamingActions.cs:197-200`
**Issue:** Both `RealHkcuDword` lookups default missing values to `1`
(enabled), so a fresh machine with no GameBar values shows "Applied" before
the user ever ticks the row. The default-on assumption may match Windows
behavior, but conflating "Windows default" with "tweak applied by this tool"
mislabels state and makes bulk-apply of an already-"applied" row a no-op the
journal still records.
**Fix:** Treat missing values as unknown/not-applied for display (fall back
to persisted `AkariToolState` choice like `ReadSharedRow` does), or record
first-apply explicitly so the label distinguishes default from tool-applied.

### WR-06: Restore-point dialog result compared by magic string inside an over-broad catch

**File:** `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs:181-206`
**Issue:** `choice.ToString() == "Primary"` hard-codes the WinUI enum member
name as a string to avoid referencing a UI type — brittle across dialog
contract changes and unreadable at the call site. The surrounding
`catch {}` swallows everything including `NullReferenceException` (e.g. a
null result), hiding real bugs as "no dialog host" and continuing silently.
**Fix:** Compare against the enum properly (a UI-agnostic wrapper or a
small `IDialogService` result type the VM can reference), and narrow the
catch to the no-host path (e.g. `InvalidOperationException`/`COMException`)
so unexpected failures surface.

### WR-07: Bulk-success test has an async `Progress<T>` race; scratch journal files leak

**File:** `src/AppTemplate.Tests/GamingCatalogTests.cs:333-349`, `src/AppTemplate.Tests/GamingCatalogTests.cs:378-379`
**Issue:** `Runner_success_applies_all_and_reports_progress` collects
`Progress<T>` callbacks into `seen` and asserts the count immediately after
`await RunAsync`. `Progress<T>` posts through the captured sync context, so
under a context that marshals asynchronously the assertions can run before
delivery — currently passing but flaky by construction. Separately,
`NewScratchStore` files are never deleted (only the corrupt-file test cleans
up), leaking temp JSON files on every run while registry scratch keys are
diligently removed.
**Fix:** Either await quiescence (e.g. a test-only synchronous `IProgress<T>`
implementation instead of `Progress<T>`) and delete scratch store files in
`finally`/`IDisposable` alongside `DropScratch`.

### WR-08: Journal capture creates HKCU keys as a side effect of snapshotting

**File:** `src/AppTemplate.App/Tweaks/GamingJournal.cs:226-233` (via `src/AppTemplate.App/Tweaks/AkariToolState.cs:55-63`)
**Issue:** `OpenRealHkcu` uses `CreateSubKey`, so `TryCaptureValue` on a
nonexistent HKCU path *creates* the key during what should be a read-only
snapshot. Capture then records the value as absent (correct), but leaves an
empty key behind, and revert deletes the value while keeping the created key.
Snapshot purity is violated for every not-yet-present HKCU value.
**Fix:** Add an `OpenRealHkcuReadOnly` (open without create, return null when
missing) and use it in `TryCaptureValue`; keep create-on-write for
`SetRealHkcuDword`.

### WR-09: Revert resurrects deleted keys and leaves empty parents behind

**File:** `src/AppTemplate.App/Tweaks/GamingJournal.cs:271-283`
**Issue:** The revert path opens keys with `CreateSubKey`, so reverting a
value whose parent key was deleted since capture recreates the key (arguably
intended) — but reverting an *absent-at-capture* value whose parent was also
deleted creates the parent just to delete a non-existent value from it,
leaving a stray empty key. The `if (key is null) throw` guard is dead code on
the writable path since creation never returns null.
**Fix:** For `Kind is null` (delete-on-revert), open read-only first and skip
when the parent key no longer exists; only create parents when restoring a
captured value.

## Info

### IN-01: `ApplySvcHost` truncates `long` to `int` without validation

**File:** `src/AppTemplate.App/Tweaks/GamingActions.cs:290-291`
**Issue:** Public API takes `long kb` and casts with `(int)kb`; out-of-range
input silently wraps into a wrong DWORD. All current callers pass in-range
constants, so this is latent.
**Fix:** Accept `int`, or range-check and throw `ArgumentOutOfRangeException`.

### IN-02: Catalog factories do not validate explanation copy

**File:** `src/AppTemplate.App/Tweaks/GamingCatalog.cs:84-123`
**Issue:** `FromNative` guards `id`/`title`/`read`/`apply` but not the four
explanation strings the catalog-integrity tests (and D-10) depend on; a
future entry with empty copy compiles and only fails tests, if covered.
`FromSharedRow` does not guard its explanation args at all.
**Fix:** Add `ThrowIfNullOrWhiteSpace` for the four copy parameters in both
factories.

### IN-03: Runner cancellation token is dead; caller can never cancel

**File:** `src/AppTemplate.App/Tweaks/GamingRunner.cs:23-28` (with `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs:218`)
**Issue:** `RunAsync` accepts a `CancellationToken` but the VM never passes
one, and an `OperationCanceledException` from `Apply` escapes via the
exception filter instead of becoming a `BulkResult`. The parameter promises
cancellability the call chain cannot deliver.
**Fix:** Either wire a `CancellationTokenSource` to the VM lifecycle (cancel
on navigate-away) and convert cancellation into a `BulkResult`, or remove the
parameter until cancellation is real.

### IN-04: Live probes run synchronously on the UI thread

**File:** `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs:68-83`, `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs:220-221`
**Issue:** `OnNavigatedTo` and the post-run refresh call `RefreshState()`
inline for every row. The comment frames this as keeping reads off the
keystroke path, but navigation itself still blocks on the full probe set
(registry + shared-row first-value reads). Fine for 8 rows today; the pattern
will stutter as the catalog grows.
**Fix:** Move the refresh loop into `await Task.Run(...)` with the existing
`_loading` guard extended over the async window.

### IN-05: Checkbox tick state and state label can contradict each other

**File:** `src/AppTemplate.App/Views/GamingCatalogPage.xaml:92-98`
**Issue:** `IsChecked` binds the tick (`IsSelected`) while `Content` shows
live state (`AppliedLabel`), so checked+"Not applied" and unchecked+"Applied"
combinations render. That is the design (tick ≠ applied), but the single
control visually merges the two concepts.
**Fix:** Consider a distinct tick column header ("Apply?") or styling the
label (e.g. grayed when unticked) so the two states read as independent.

### IN-06: Gaming Catalog reuses the Gaming Tweaks nav glyph

**File:** `src/AppTemplate.App/MainWindow.xaml.cs:86-87`
**Issue:** Both "Gaming Tweaks" and "Gaming Catalog" use `\uE7FC`, making the
two adjacent nav items visually identical.
**Fix:** Pick a distinct glyph for the catalog (e.g. `\uE8FD`-family or a
list/catalog icon).

### IN-07: Stale header comment on `GamingActions`

**File:** `src/AppTemplate.App/Tweaks/GamingActions.cs:7-14`
**Issue:** The class doc references "AMD Dwords, AUTO DSCP, )" with a
truncated list, mismatched parenthesis, and double-space typos — leftover
from an earlier draft that no longer describes the file contents.
**Fix:** Rewrite the summary to list the actual contents (preemption,
network, Game Mode/DVR, SvcHost/Win32Priority, AUTO DSCP & FSE).

### IN-08: Corrupt journal data is masked with empty values on revert

**File:** `src/AppTemplate.App/Tweaks/GamingJournal.cs:295-306`
**Issue:** `value.DataString ?? string.Empty` and `value.DataMulti ?? []`
write empty strings/arrays when a journal entry's payload is malformed,
instead of failing that row. Combined with clear-on-full-success, a corrupt
journal could zero out values and then delete its own evidence.
**Fix:** Throw `InvalidDataException` on null payload for a non-null kind so
the row records a failure and the journal is retained for inspection.

### IN-09: `.reg` footprint parser duplicates the probe parser; dup check is case-sensitive

**File:** `src/AppTemplate.App/Tweaks/GamingJournal.cs:200-224`
**Issue:** `ParseRegFootprint` re-implements section/assignment scanning
already present in `ControlPanelRows.ParseProbe`, with a comment pledging
never to touch the original — classic drift setup. The
`footprint.Contains((key, name))` dedup is ordinal case-sensitive while
registry paths are case-insensitive, so mixed-case duplicates double-capture.
**Fix:** Share one parser (footnote the no-touch constraint with a test
instead of a copy), and dedupe with `StringComparer.OrdinalIgnoreCase`.

---

_Reviewed: 2026-10-04T00:00:00Z_
_Reviewer: the agent (gsd-code-reviewer)_
_Depth: deep_
