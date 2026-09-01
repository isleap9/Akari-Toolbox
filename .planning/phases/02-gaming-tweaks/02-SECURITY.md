---
phase: 02
slug: gaming-tweaks
status: verified
# threats_open = count of OPEN threats at or above workflow.security_block_on severity (the blocking gate)
threats_open: 0
asvs_level: 1
created: 2026-09-01
---

# Phase 02 — Security

> Per-phase security contract: threat register, accepted risks, and audit trail.

---

## Trust Boundaries

| Boundary | Description | Data Crossing |
|----------|-------------|---------------|
| Elevated process -> Windows registry (HKLM Class GUID subkeys, PnP Enum subkeys, device-class/adapter-class subkeys) | Same elevated-process-to-registry boundary as every Phase 1 handler — no new boundary shape, only new registry paths under the existing `requireAdministrator` gate | Registry values (DWORD/binary/string), PnP `InstanceId` strings interpolated only into fixed path templates |
| GamingTweaksViewModel/Page -> ITweakCatalog | Mirrors the exact Phase 1 AkariOSTweaksViewModel trust boundary — no new UI-to-backend boundary shape | Boolean toggle state |
| GamingTweaksViewModel -> shell (`ms-settings:` URI launch) | First-party OS Settings URI launch, not a registry write | No data passed |
| Elevated process -> `powercfg.exe` / `csc.exe` / `sc.exe` subprocess spawns | Shelling to system utilities that themselves mutate broad system state (all existing power schemes; a compiled, installed Windows Service) — higher blast radius per call than a single registry `SetValue` | Process arguments (fixed strings/GUIDs, no user-controlled shell interpolation) |
| GamingTweaksPage ComboBox selection -> registry write | Input-validation boundary: a dropdown's `SelectedIndex` is a small integer that must be bounds-checked before use as an array index and before being written to a system-wide registry value | Integer index, resolved preset value |
| Elevated process -> internet (github.com/FR33THYFR33THY/Ultimate-Files, official vendor domains) -> downloaded third-party binary execution | The highest-severity trust boundary in this phase and in the whole app: an elevated process downloads and runs third-party binaries with no integrity verification (D-06, explicit accepted risk) | Downloaded installer binaries, no verification |
| Embedded `.ps1` resource extraction/execution pipeline (`RunEmbeddedScriptAsync`) | Build-time embedded asset extracted to a GUID-suffixed temp path and executed, not runtime-downloaded | Script content (build-time embedded, not user input) |

---

## Threat Register

| Threat ID | Category | Component | Severity | Disposition | Mitigation | Status |
|-----------|----------|-----------|----------|-------------|------------|--------|
| T-02-01 | Tampering | `TweakCategory` filter in `AkariOSTweaksViewModel`/`GamingTweaksViewModel` | medium | mitigate | AkariOS-scoped ordering regression test still asserts exactly 32 AkariOS-category handlers with orders `[0..31]` — a mis-tagged handler fails this test immediately | closed |
| T-02-02 | Tampering | `GetSubKeyNames` / Hdcp,P0State,MsiMode registry+PnP writes | high | mitigate | `requireAdministrator` elevation; every write targets `CurrentControlSet`, never `ControlSet001`; PnP `InstanceId` values only interpolated into a fixed registry-path template | closed |
| T-02-03 | Elevation of Privilege | `GamingTweaksPage`/nav newly exposed | low | accept | App already runs `requireAdministrator` before any page renders — adding a page introduces no new elevation surface | closed |
| T-02-04 | Denial of Service | `RunEmbeddedScriptAsync` temp-file extraction | low | mitigate | Extraction path is `Path.GetTempPath()` + a GUID-suffixed filename, deleted in a `finally` block regardless of outcome | closed |
| T-02-05 | Tampering | `AmdSettingsTweakHandler`'s 20-value multi-key/nested-subkey writes | high | mitigate | Writes go through `IRegistryService`'s null-checked open-then-create convention; `HexStringToBytes` only parses fixed compile-time hex literals, never user input; `RadeonSoftware.exe` restart is best-effort/non-blocking, wrapped in try/catch | closed |
| T-02-06 | Information Disclosure | D-05 `ms-settings:` URI launches | low | accept | First-party OS settings page, no data passed, no elevation-prompt bypass — same trust level as a user manually opening Settings | closed |
| T-02-07 | Tampering | Multi-class-recursion registry enumeration (Device/WDF/network-adapter subkeys) | high | mitigate | `CurrentControlSet` only, never `ControlSet001`; ACPI-branch typo preserved as-authored per D-04, documented in-code | closed |
| T-02-08 | Repudiation | `WriteCacheFlushTweakHandler`'s asymmetric On/Off match targets | medium | mitigate | Two explicit, independently-unit-tested enumeration paths (including a round-trip test) | closed |
| T-02-09 | Denial of Service | `PowerPlanTweakHandler`'s `powercfg -restoredefaultschemes` deleting all existing power schemes | high | mitigate | Every pre-existing scheme exported via `powercfg -export` before any delete; `powercfg -import` restores on revert. Post-execution code review (02-REVIEW.md CR-01) found the initial implementation discarded export exit codes; fixed in 02-REVIEW-FIX.md (commit `7341e96`) to check exit code + `File.Exists` before any delete/duplicate/setactive proceeds — re-verified present in current code by this audit | closed |
| T-02-10 | Tampering | `TimerResolutionTweakHandler`'s runtime C# compilation + service install | high | mitigate | Compiled C# source is a fixed compile-time string literal, never built from user/download input; pre-flight `File.Exists(CscPath)` probe with logged failure before any compilation attempt | closed |
| T-02-11 | Denial of Service | `NetworkIpv4OnlyTweakHandler` disabling IPv6/other adapter bindings system-wide | medium | mitigate | Direct 1:1 port of the source script's own binding list; revert path re-enables the exact same component IDs | closed |
| T-02-12 | Tampering | Unvalidated dropdown index write to `SvcHostSplitThresholdInKB`/`Win32PrioritySeparation` | high | mitigate | Every index validated against `[0, presetArray.Length)` before any registry write | closed |
| T-02-13 | Tampering | SvcHost "Default" preset historically miswritten as decimal-for-hex (predecessor bug) | low | mitigate | New "Default" preset deletes the value entirely instead of writing a buggy/guessed literal | closed |
| T-02-14 | Tampering | All 6 D-06 embedded scripts download and execute third-party binaries with admin rights, no SHA256/signature verification | critical | accept | Explicit user decision D-06 (02-CONTEXT.md) — no added verification for v1; each script's download source unchanged from the source script; risk surfaced in-code (visible pre-launch `ILogConsoleService` line, all 12 buttons) and in the page's own UI section header | closed |
| T-02-SC | Tampering | Embedded `.ps1` resource extraction/execution pipeline (`RunEmbeddedScriptAsync`) | high | mitigate | Extraction path is `Path.GetTempPath()` + GUID-suffixed filename; file deleted in a `finally` block; resource itself is a build-time embedded asset, not runtime-downloaded | closed |
| T-02-15 | Denial of Service | 771-line Driver Install Debloat & Settings.ps1 runs unattended after Read-Host stripping | medium | mitigate | Each stripped branch extracted from exact source script content; `IScriptRunner.RunProcessAsync`'s existing timeout/never-throw contract prevents a hung install from blocking the UI thread | closed |
| T-02-16 | Repudiation | Gaming-scoped ordering regression test not asserting exact handler count/disjoint Order range until the final plan | medium | mitigate | `TweakHandlerOrderingTests` now has a Gaming-scoped fact asserting exactly 11 handlers with `Order` values `{100..110}`, alongside the still-passing AkariOS-scoped facts | closed |

*Status: open · closed · open — below high threshold (non-blocking)*
*Severity: critical > high > medium > low — only open threats at or above workflow.security_block_on (high) count toward threats_open*
*Disposition: mitigate (implementation required) · accept (documented risk) · transfer (third-party)*

---

## Accepted Risks Log

| Risk ID | Threat Ref | Rationale | Accepted By | Date |
|---------|------------|-----------|-------------|------|
| AR-02-01 | T-02-03 | Elevation of Privilege for the newly exposed Gaming Tweaks page/nav entry is not a new risk — the entire app already runs `requireAdministrator` before any page renders (APP-01), so adding a page introduces no new elevation surface | Project owner (via 02-CONTEXT.md, verified by gsd-security-auditor against `app.manifest`) | 2026-09-01 |
| AR-02-02 | T-02-06 | D-05 `ms-settings:` URI launches (Display Settings, Advanced Graphics Settings) open first-party OS Settings pages with no data passed and no elevation-prompt bypass — same trust level as a user manually opening Settings | Project owner (via 02-CONTEXT.md D-05, verified by gsd-security-auditor against `GamingTweaksViewModel.cs`) | 2026-09-01 |
| AR-02-03 | T-02-14 | All 6 D-06 network-dependent scripts (12 embedded resources) download and execute third-party binaries with admin rights and no SHA256/signature verification, for v1. Explicit, deliberate project-owner decision (D-06, 02-CONTEXT.md) — download sources unchanged from the source PowerShell scripts; the accepted risk is surfaced at runtime via a visible pre-launch log line (before every one of the 12 buttons runs) and via the page's own "not integrity-verified" UI section header, not silently hidden | Project owner (via 02-CONTEXT.md D-06, verified by gsd-security-auditor against `GamingTweaksViewModel.cs`/`GamingTweaksPage.xaml`) | 2026-09-01 |

*Accepted risks do not resurface in future audit runs.*

---

## Security Audit Trail

| Audit Date | Threats Total | Closed | Open | Run By |
|------------|---------------|--------|------|--------|
| 2026-09-01 | 17 | 17 | 0 | gsd-security-auditor (initial audit, register authored at plan-time across 02-01 through 02-07 PLAN.md) |

---

## Sign-Off

- [x] All threats have a disposition (mitigate / accept / transfer)
- [x] Accepted risks documented in Accepted Risks Log
- [x] `threats_open: 0` confirmed
- [x] `status: verified` set in frontmatter

**Approval:** verified 2026-09-01
