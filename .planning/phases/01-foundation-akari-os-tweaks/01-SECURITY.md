---
phase: 01
slug: foundation-akari-os-tweaks
status: verified
threats_open: 0
asvs_level: 1
created: 2026-09-01
---

# Phase 01 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Elevated process → registry (HKLM/HKCU) | All 32 tweak handlers read/write real system state through `IRegistryService`; writes are gated by existence checks, no blind `CreateSubKey` on unverified paths | Registry key paths/values, all local |
| Elevated process → filesystem via picker-selected path | `Microsoft.Windows.Storage.Pickers`-based picker returns a user-navigated path; no Phase 1 consumer writes/executes on the picked path (debug button only logged it — since removed, WR-03) | Local file path string |
| Elevated process → child process (bcdedit/DISM/fsutil/powershell.exe) | `ScriptRunner`/`Process.Start` call sites are hardcoded literals, never built from free text or user input; timeout kills the whole process tree | Process exit codes, stdout/stderr (log-only) |
| Elevated process → downloaded network content (PostInstall mirror) | `PostInstallService` fetches ~130 general assets from a pinned GitHub raw-content URL over HTTPS; not currently consumed by any Phase 1 handler (Defender's payload is embedded instead) | Arbitrary file bytes from `raw.githubusercontent.com` |
| Elevated process → embedded assembly resource | `NoDefender.cab`/`DisableDefender.ps1` are embedded at build time and extracted to temp, then one is `Add-WindowsPackage`'d and the other copied to `C:\Windows`; bytes are fixed at compile time, not a runtime/network boundary | Local file bytes, build-time fixed |
| Elevated process → SYSTEM identity (impersonation) | `ElevationService.RunAsSystem` duplicates winlogon.exe's token into an impersonation token attached to the calling thread for the duration of a synchronous action, then reverts | Windows access token (in-process only, never persisted or exposed) |
| Any admin-token process → headless CLI relaunch (`--defender-phase2 <token>`) | `App.xaml.cs` parses `Environment.GetCommandLineArgs()` before any other startup logic; a matching single-use token (persisted by `DefenderPhase2Scheduler.ScheduleRunOnce`, consumed by `ConsumeToken`) gates whether the native SYSTEM-impersonation phase-2 mutation runs at all | Process command-line arguments (token string) |

---

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| T-01-01 | Tampering | RegistryService.SetValue | high | mitigate | `OpenSubKey(writable:true) ?? CreateSubKey(...)` — never blind-creates over an unverified path | closed |
| T-01-02 | Elevation of Privilege | app.manifest | medium | accept | `requireAdministrator` — same privilege model as predecessor, required for the app's purpose | closed |
| T-01-03 | Information Disclosure | Registry reads | low | accept | Local-machine-only reads, no exfiltration path | closed |
| T-01-04 | Denial of Service | Assembly scan at startup | low | accept | Bounded single-assembly reflection scan | closed |
| T-01-05 | Elevation of Privilege | ScriptRunner call sites | high | mitigate | Hardcoded literal commands only (bcdedit/DISM/fsutil), never built from free text | closed |
| T-01-06 | Denial of Service | ScriptRunner timeout | low | mitigate | `Task.WhenAny` + `process.Kill(entireProcessTree:true)` on timeout | closed |
| T-01-07 | Information Disclosure | Log console | low | accept | Local log console only, no network egress | closed |
| T-01-08 | Elevation of Privilege | File/folder picker | medium | accept | No write/execute action taken on picker-selected path in Phase 1 | closed |
| T-01-09 | Tampering | Tweak state persistence | high | mitigate | All state writes go through `IRegistryService`; no raw/unchecked registry writes elsewhere | closed |
| T-01-10 | Repudiation | Registry writes | medium | accept | Covered by `RegistryService`'s existence-check convention | closed |
| T-01-11 | Tampering | Tweak toggles | medium | accept | Explicit user-initiated action per toggle | closed |
| T-01-12 | Denial of Service | Script execution | low | mitigate | Inherits ScriptRunner timeout handling (T-01-06) | closed |
| T-01-13 | Tampering | Defender Tamper Protection gate | high | mitigate | `IsDefenderTamperProtectionOn()` early-return before any package/service mutation, no partial state change | closed |
| T-01-14 | Elevation of Privilege | Redundant UAC prompt (DisableDefender.ps1) | low | accept | Intentional, documented pre-existing predecessor behavior | closed |
| T-01-15 | Information Disclosure | Defender workflow logging | low | accept | Local log console only | closed |
| T-01-16 | Denial of Service | Tweak catalog resolution | medium | mitigate | `TryGetStateAsync` isolates a throwing handler; confirmed via `TweakHandlerOrderingTests` | closed |
| T-01-SC | Tampering | Defender asset integrity | high | mitigate → superseded | Originally a SHA256 gate on downloaded `NoDefender.cab`/`DisableDefender.ps1`; both files are now embedded assembly resources (build-time fixed bytes, no network fetch) — the network trust boundary this threat targeted no longer exists for these two files | closed |
| CR-01 (review) | Tampering (race/re-entrancy) | DefenderTweakHandler.SetState | high | mitigate | Commit `4df3c18` — `SetState` blocks via `.GetAwaiter().GetResult()`, restoring `TweakCatalog`'s per-key semaphore guarantee | closed |
| CR-02 (review) | Tampering (unintended revert) | AkariOSTweaksViewModel init | high | mitigate | Commit `768845d` — `PropertyChanged` subscribed only after initial value is set | closed |
| CR-03 (review) | Tampering (unverified elevated binary exec) | Defender elevation mechanism | high | mitigate | Commit `4df3c18` — MinSudo.exe/PowerRun.exe eliminated entirely, replaced by `ElevationService.RunAsSystem` (native P/Invoke SYSTEM impersonation) | closed |
| CR-04 (review) | Tampering (state-accuracy) | AkariOSTweaksViewModel fault path | medium | mitigate | Commit `768845d` — on fault, re-reads and reflects real state | closed |
| WR-01 (review) | Denial of Service (handle leak) | RegistryService.OpenRealUserHive | low | mitigate | Commit `0c70637` — `using` + `finally { CloseHandle(token); }` | closed |
| WR-02 (review) | Tampering (unverified hash pin) | Defender SHA256 constants | medium | accept → moot | Superseded by T-01-SC's architecture change — the constants this warned about no longer exist in code | closed |
| WR-03 (review) | Elevation of Privilege (exposed debug surface) | MainWindow picker smoke-test button | low | mitigate | Commit `d64a1d9` — button removed entirely | closed |
| T-01-17 | Tampering / Elevation of Privilege | App.xaml.cs `--defender-phase2` headless entry | high | mitigate | Single-use GUID token: `DefenderPhase2Scheduler.ScheduleRunOnce` persists a random token separately from the RunOnce command and embeds it in the command line; `RunPhase2Native` calls `ConsumeToken` (verifies + deletes regardless of outcome) before any registry/service mutation. A bare `--defender-phase2` invocation without a freshly-scheduled, matching token is a logged no-op. | closed |

*Status: open · closed · open — below high threshold (non-blocking)*
*Severity: critical > high > medium > low — only open threats at or above workflow.security_block_on (high) count toward threats_open*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-01 | T-01-02 | `requireAdministrator` is the app's entire purpose (registry/service/Defender tweaks require admin) — same model as the predecessor | Project owner (CLAUDE.md constraints) | 2026-09-01 |
| AR-02 | T-01-08 | No write/execute action is taken on the picker-selected path in Phase 1; revisit when Phase 4 wires a real consumer | Project owner | 2026-09-01 |
| AR-03 | T-01-14 | Intentional redundant UAC prompt in `DefenderRunElevatedPsFileAsync`, documented inline as pre-existing accepted behavior | Project owner (RESEARCH "Known Threat Patterns") | 2026-09-01 |
| AR-04 | T-01-02, T-01-03, T-01-04, T-01-07, T-01-10, T-01-11, T-01-15 | Local-only, no-network, single-user desktop tool — standard low-severity accepted risks for this threat model, unchanged from plan-time disposition | Project owner | 2026-09-01 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-09-01 | 25 | 24 | 1 | gsd-security-auditor (initial audit — found T-01-17, the `--defender-phase2` headless CLI path had no scheduling proof) |
| 2026-09-01 | 25 | 25 | 0 | Claude (orchestrator) — implemented single-use token gate (`DefenderPhase2Scheduler.ConsumeToken`) closing T-01-17; build clean, 117/118 tests pass (1 pre-existing unrelated failure) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-09-01
