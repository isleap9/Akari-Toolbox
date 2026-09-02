---
phase: 03-debloat
verified: 2026-09-02T00:00:00Z
status: gaps_found
score: 5/8 must-haves verified
behavior_unverified: 0
overrides_applied: 0
gaps:
  - truth: "'Fully functional Run+Undo pairs' for the Privacy & Telemetry category (03-02-PLAN.md must_have) — clicking Undo actually reverses the paired Run script's system change"
    status: failed
    reason: "Independently verified by direct script inspection (not just trusting 03-REVIEW.md): locationtracking-undo.ps1, consumerfeatures-undo.ps1, storesearch-undo.ps1, and ps7telemetry-undo.ps1 each write to registry keys/env vars that are DIFFERENT from the ones their paired Run script wrote — the paired Run script's actual system change is never reverted. 4 of the category's 8 actions are affected."
    artifacts:
      - path: "src/AkariToolbox.App/Resources/DebloatScripts/locationtracking-undo.ps1"
        issue: "Run writes 4 HKLM values (ConsentStore\\location, Sensor\\Overrides, lfsvc\\Service\\Configuration, SYSTEM\\Maps). Undo writes an unrelated HKLM Policies key the Run script never touched, plus HKCU (not HKLM) ConsentStore\\location — none of the 4 original values are reset."
      - path: "src/AkariToolbox.App/Resources/DebloatScripts/consumerfeatures-undo.ps1"
        issue: "Run sets HKLM:\\...\\Policies\\Microsoft\\Windows\\CloudContent!DisableWindowsConsumerFeatures=1 (a Group-Policy-precedence key). Undo only touches 8 unrelated HKCU ContentDeliveryManager values and never clears DisableWindowsConsumerFeatures — the policy still wins, consumer features stay disabled after Undo."
      - path: "src/AkariToolbox.App/Resources/DebloatScripts/storesearch-undo.ps1"
        issue: "Run applies an icacls /deny Everyone:F ACE on store.db (a destructive, effectively irreversible-by-average-user ACL change). Undo never runs icacls /remove:d — it only sets an unrelated HKCU BingSearchEnabled value; the file stays permanently ACL-locked."
      - path: "src/AkariToolbox.App/Resources/DebloatScripts/ps7telemetry-undo.ps1"
        issue: "Run sets the machine env var POWERSHELL_TELEMETRY_OPTOUT=1. Undo never calls SetEnvironmentVariable at all — it deletes a file and edits a $PROFILE the Run script never created/wrote (guaranteed no-ops); the opt-out remains set forever."
    missing:
      - "Fix each Undo script to reverse the actual artifact its paired Run script modified (see 03-REVIEW.md CR-01/CR-02/CR-03/CR-04 for exact fix diffs)."
  - truth: "'Fully functional Run+Undo pairs' for the System & Performance category (03-03-PLAN.md must_have)"
    status: failed
    reason: "wpbt-undo.ps1 independently verified to not reverse wpbt.ps1's change."
    artifacts:
      - path: "src/AkariToolbox.App/Resources/DebloatScripts/wpbt-undo.ps1"
        issue: "Run sets HKLM:\\SYSTEM\\...\\Session Manager!DisableWpbtExecution=1. Undo instead removes a Start value under a nonexistent HKLM:\\SYSTEM\\...\\Services\\WPBT key and calls Set-Service/Start-Service \"WPBT\" (no such service exists, fails silently) — DisableWpbtExecution is never cleared, WPBT execution stays disabled after Undo."
    missing:
      - "Fix wpbt-undo.ps1 to Remove-ItemProperty the actual DisableWpbtExecution value (see 03-REVIEW.md CR-05)."
  - truth: "'Fully functional Run+Undo pairs' for the Explorer & UI category (03-04-PLAN.md must_have)"
    status: failed
    reason: "folderdiscovery-undo.ps1 independently verified to not reverse folderdiscovery.ps1's change."
    artifacts:
      - path: "src/AkariToolbox.App/Resources/DebloatScripts/folderdiscovery-undo.ps1"
        issue: "Run sets FolderType=NotSpecified under HKCU:\\...\\Bags\\AllFolders\\Shell. Undo creates-then-removes an unrelated FolderContentsMode value under HKCU:\\...\\Explorer\\Advanced (a value the Run script never set — guaranteed no-op); FolderType is never touched, automatic folder discovery stays disabled after Undo."
    missing:
      - "Fix folderdiscovery-undo.ps1 to Remove-ItemProperty the actual FolderType value (see 03-REVIEW.md CR-06)."
  - truth: "03-07-PLAN.md closing claim: 'Every one of the 28 Debloat actions ... is fully functional Run (and Undo where applicable) ... DEBLOAT-01 is now completely satisfied, not partially'"
    status: failed
    reason: "This closing must-have is the phase's own final acceptance claim and is directly falsified by the 4 gaps above (6 of 25 Run+Undo pairs across the whole catalog do not actually revert, per independently-confirmed 03-REVIEW.md CR-01/02/03/04/05/06). The DebloatCatalogTests suite this plan added only proves resource-manifest resolution and ScriptRunner call wiring (Assert.Contains(resourceNames, ...), Assert.Equal(scriptRunner.Calls, ...)) — it never asserts on registry/env/ACL state before and after Run+Undo, so it structurally cannot catch this class of bug and did not."
    artifacts:
      - path: "src/AkariToolbox.Tests/DebloatCatalogTests.cs"
        issue: "All 16 facts assert catalog shape and resource-name/ScriptRunner-call wiring only; none assert actual system-state reversal, so 'fully functional Run+Undo pairs' is asserted in prose (SUMMARY.md, plan must_haves) but never verified in code."
    missing:
      - "Fix the 6 broken Undo scripts (CR-01 through CR-06), then add a smoke/manual verification step (or an integration test in a disposable VM/container, if the project ever adds one) that actually reads the registry value before Run, after Run, and after Undo for at least the previously-broken keys."
deferred: []
---

# Phase 3: Debloat Verification Report

**Phase Goal:** Users can run the predecessor's 28 PowerShell-backed debloat actions with live streamed feedback, with the page's logic living in a ViewModel/service rather than code-behind.
**Verified:** 2026-09-02T00:00:00Z
**Status:** gaps_found
**Re-verification:** No — initial verification

**Note on phase mode:** ROADMAP.md marks this phase `Mode: mvp`, but the phase goal is not written in the `As a ..., I want to ..., so that ....` User Story format required for MVP-mode verification (`user-story.validate` returns `valid=false`). This mismatch appears across all phases in this project (Phase 1 has the same pattern), so it looks like a stale/default field rather than a deliberate MVP-mode plan. Standard goal-backward verification was applied instead, using the ROADMAP Success Criteria plus PLAN frontmatter must_haves as the must-have set. Flagging for awareness — does not block this verification.

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | User can run each of the 28 PowerShell-backed debloat actions from the Debloat page (ROADMAP SC1) | ✓ VERIFIED | `DebloatCatalog.cs` has exactly 28 actions in 5 categories (8/8/6/5/1); all 53 corresponding `.ps1` files are embedded (`AkariToolbox.App.csproj` lines 58-133) and resolve via `IScriptRunner.RunEmbeddedScriptAsync`; `DebloatCatalogTests` locks the 28/5/8-8-6-5-1/5-confirmation shape (16 passing facts, all resource-resolution/wiring assertions). |
| 2 | User sees streamed status/output feedback while a debloat action runs, without UI freezing or crashing (ROADMAP SC2) | ✓ VERIFIED | `DebloatViewModel.ExecuteAsync` awaits `IScriptRunner.RunEmbeddedScriptAsync` fully async (no `.Result`/`.Wait()`), logs via `ILogConsoleService` before/after, per-row `ProgressRing`/`IsRunning` binding in `DebloatPage.xaml` — reuses the proven Phase 1/2 `ScriptRunner` streaming mechanism. The 6 branch-extracted Cleanup-category scripts (bloatware/edgewebview/edgesettings pairs) were independently checked for `show-menu`/`Pause`/`Read-Host` hang risks (03-06's stated Pitfall 1/2 fixes) — none found, confirming the hang-fix claims. |
| 3 | Debloat page logic lives in a ViewModel/service, not in page code-behind (ROADMAP SC3 / DEBLOAT-03) | ✓ VERIFIED | `DebloatPage.xaml.cs` is 17 lines: constructor only assigns `ViewModel`, calls `InitializeComponent()`, sets `DataContext` — zero business logic, matching `GamingTweaksPage.xaml.cs`'s precedent. All confirmation-gating, semaphore-locking, and script dispatch lives in `DebloatViewModel.ExecuteAsync`. |
| 4 | "All 8 Privacy & Telemetry actions ... are fully functional Run+Undo pairs" (03-02-PLAN.md must_have) | ✗ FAILED | 4 of 8 Undo scripts do not reverse their Run script's change — see gap 1. Independently confirmed by reading Run/Undo script pairs side-by-side (not just trusting 03-REVIEW.md). |
| 5 | "All 8 System & Performance actions ... are fully functional Run+Undo pairs" (03-03-PLAN.md must_have) | ✗ FAILED | wpbt-undo.ps1 does not reverse wpbt.ps1 — see gap 2. (BitLocker's Undo intentionally only opens Control Panel per accepted D-13 scope reduction — not counted as a failure, matches its own must_have wording.) |
| 6 | "All 5 Explorer & UI actions ... are fully functional Run+Undo pairs" (03-04-PLAN.md must_have) | ✗ FAILED | folderdiscovery-undo.ps1 does not reverse folderdiscovery.ps1 — see gap 3. |
| 7 | "'OneDrive — Remove', Disk Cleanup, Temp Files fully functional" (03-05-PLAN.md must_have) | ✓ VERIFIED | `removeonedrive.ps1`/`removeonedrive-undo.ps1` present and embedded; `diskcleanup.ps1`/`tempfiles.ps1` present with `UndoResourceSuffix: null` matching catalog (Run-only, no Undo claim to fail). Not independently script-diffed beyond resource presence — lower risk since these are Run-only or predecessor byte-for-byte carries not flagged by 03-REVIEW.md. |
| 8 | "Every one of the 28 Debloat actions ... is fully functional Run (and Undo where applicable) ... DEBLOAT-01 is now completely satisfied, not partially" (03-07-PLAN.md closing must_have) | ✗ FAILED | Directly falsified by truths 4-6 above. `DebloatCatalogTests`'s "regression lock" only proves resource-manifest resolution and `ScriptRunner` call wiring, never actual before/after system state — so it cannot and did not catch these bugs. |

**Score:** 5/8 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `src/AkariToolbox.App/Services/DebloatCatalog.cs` | 28-action, 5-category static catalog | ✓ VERIFIED | Exact match: 8/8/6/5/1 counts, category order, 5 `RequiresConfirmation=true` keys (disablebitlocker, hibernation, bloatware, removeonedrive, edgewebview). |
| `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs` | Catalog-driven Run/Undo dispatch, no per-key state read-back | ✓ VERIFIED | Generic `ExecuteAsync(item, isUndo)`, confirmation gate only on Run direction, per-action `SemaphoreSlim` concurrency guard, `FileNotFoundException` catch (WR-01 in 03-REVIEW.md notes this catch is narrower than ideal — a Warning, not a blocker). |
| `src/AkariToolbox.App/Views/DebloatPage.xaml.cs` | Zero business logic | ✓ VERIFIED | 17 lines, constructor-only. |
| `src/AkariToolbox.App/Resources/DebloatScripts/*.ps1` (53 files) | All embedded and resolvable | ✓ VERIFIED (existence/wiring) — ✗ NOT VERIFIED (correctness) | All present in `AkariToolbox.App.csproj` `<EmbeddedResource>` entries and confirmed resolvable via `DebloatCatalogTests`. However, existence and manifest-resolution are not the same as correctness: 6 Undo scripts exist, are embedded, and execute without error, but do not perform the state reversal their name/UI presence promises. |
| `src/AkariToolbox.Tests/DebloatCatalogTests.cs` | Regression lock for catalog shape + wiring | ✓ VERIFIED (as scoped) | 16 facts pass (per SUMMARY claims and static read of the file); scope is explicitly resource-resolution/call-wiring only, not registry-state assertions — this is a real coverage gap, not a bug in the tests themselves. |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `DebloatViewModel.ExecuteAsync` | `IScriptRunner.RunEmbeddedScriptAsync` | `await _scriptRunner.RunEmbeddedScriptAsync(resourceSuffix)` | ✓ WIRED | Confirmed in source; also proven live-fired for "telemetry" via `FakeScriptRunner`-backed unit test. |
| `DebloatPage.xaml` (nested `ItemsRepeater`) | `DebloatViewModel.RunActionCommand` / `UndoActionCommand` | `x:Bind RootPage.ViewModel.RunActionCommand` | ✓ WIRED | `RootPage` named page reference pattern present in XAML, matches plan spec. |
| `AkariToolbox.App.csproj` | `Resources/DebloatScripts/*.ps1` | `<EmbeddedResource Include=...>` | ✓ WIRED | 53 entries counted, matches the 28-action catalog's Run (28) + Undo (25, since 3 actions are Run-only) resource count. |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|--------------|--------|----------|
| DEBLOAT-01 | 03-01 through 03-07 | User can run each of the 28 debloat actions | ⚠️ PARTIALLY SATISFIED | Literal wording ("can run") is satisfied — all 28 Run paths are wired and execute. But the catalog presents 25 of those 28 as having a working Undo (`HasUndo=true`, Undo button rendered), and 6 of those 25 Undo scripts do not actually reverse the change — a correctness gap directly contradicting the project's stated Core Value ("...must apply correctly...and (where applicable) be safely revertible") and the phase's own plan-level must_haves. |
| DEBLOAT-02 | 03-01 through 03-06 | Streamed status/output feedback, no UI freeze/crash | ✓ SATISFIED | Verified above — async, non-blocking, log-dock streaming; hang-risk scripts specifically checked and clean. |
| DEBLOAT-03 | 03-01, 03-07 | Debloat page logic in ViewModel/service, not code-behind | ✓ SATISFIED | Verified above — `DebloatPage.xaml.cs` has zero business logic. |

No orphaned requirements — DEBLOAT-01/02/03 are all declared across the phase's plan frontmatter and match REQUIREMENTS.md's Phase 3 traceability row.

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| — | — | No `TBD`/`FIXME`/`XXX`/`TODO`/`HACK`/`PLACEHOLDER` markers found in any Debloat file | — | Clean — debt-marker gate does not trigger. |
| 6 `*-undo.ps1` files (locationtracking, consumerfeatures, storesearch, ps7telemetry, wpbt, folderdiscovery) | whole file | Undo script targets different registry key/hive/artifact than its paired Run script — a logic bug, not a debt marker, but functionally equivalent to a silent no-op stub for the "reverses the change" behavior | 🛑 Blocker | Directly breaks the phase's own "fully functional Run+Undo pairs" must-haves and the project's stated Core Value on revertibility. |
| `windowsai.ps1` | 8-10 | Force-kills unrelated running processes (OneDrive, Edge, Search, Widgets, RuntimeBroker, GameBar) with `RequiresConfirmation=false` and no disclosure in the catalog Description | ⚠️ Warning | Undisclosed destructive side effect on a "safe" (unconfirmed) action — carried over from 03-REVIEW.md WR-04, not independently re-verified beyond confirming the catalog entry is `RequiresConfirmation: false`. |
| `storesearch.ps1` | 2 | `icacls /deny Everyone:F` — an effectively irreversible ACL change — is `RequiresConfirmation=false` | ⚠️ Warning | Combined with the broken Undo (gap 1), this action can permanently lock a file with no confirmation gate and no way back through the UI. Carried over from 03-REVIEW.md CR-03. |

### Human Verification Required

None. All findings above were established by direct, static comparison of each Run script's registry/env/file mutations against its paired Undo script's mutations — a deterministic code-level fact, not a runtime behavior requiring live-system observation. No item needed to be deferred to human testing.

### Gaps Summary

The Debloat page's architecture (catalog, ViewModel, code-behind cleanliness, streaming, DI/nav wiring) is solid and matches its own design intent — DEBLOAT-02 and DEBLOAT-03 are genuinely satisfied, and the literal "user can run" clause of DEBLOAT-01 is satisfied for all 28 actions.

However, the phase's own plans (03-02, 03-03, 03-04, and the phase-closing 03-07) each explicitly committed to "fully functional Run+Undo pairs," and this claim is false for at least 6 of the 25 actions that expose an Undo button: `locationtracking`, `consumerfeatures`, `storesearch`, `ps7telemetry` (Privacy & Telemetry), `wpbt` (System & Performance), and `folderdiscovery` (Explorer & UI). In every case the Undo script writes to a registry key/environment-variable/ACL that the paired Run script never touched — it is not a partial revert, it is a complete no-op against the actual change, verified by directly reading both halves of each pair. This was independently re-confirmed here (not merely inherited from 03-REVIEW.md) by reading all 8 flagged file pairs.

This matters beyond "one broken button": the project's own stated Core Value is "Every tweak, debloat action ... must apply correctly, report accurate state, and (where applicable) be safely revertible." A user who runs "Location Tracking — Disable" then clicks "Undo" is left with location tracking still disabled and no visible indication anything went wrong (`Write-Host "Location tracking enabled."` prints a success message despite doing nothing to the actual location settings) — this is the exact class of silent-failure the project's threat model is designed to prevent. All 6 of these must-haves are structurally invisible to `DebloatCatalogTests`, which only asserts resource-manifest resolution and `IScriptRunner` call wiring, never before/after system state — so the existing automated suite cannot and did not catch this.

**Not counted as gaps (explicitly accepted deviations, verified against their own plan wording):**
- BitLocker's Undo only opens Control Panel (no active re-encryption) — D-13, explicitly documented as accepted scope reduction in 03-01/03-03-PLAN.md, matches its own must_have wording exactly.
- Bloatware's Undo is explicitly "best-effort, non-symmetric" per D-05 (03-06-PLAN.md) — does not claim full symmetry, so its known incompleteness (WR-06/IN-01 in 03-REVIEW.md) is accepted-as-designed, not a broken promise.
- `windowsai`/`storesearch` confirmation-gating gaps (WR-03/WR-04/CR-03's confirmation-flag half) are UX/disclosure issues layered on top of, but distinct from, the core Undo-correctness bug — listed under Anti-Patterns as Warnings, not counted as additional truth failures since they don't have a dedicated must_have claiming otherwise.

---

_Verified: 2026-09-02T00:00:00Z_
_Verifier: Claude (gsd-verifier)_
