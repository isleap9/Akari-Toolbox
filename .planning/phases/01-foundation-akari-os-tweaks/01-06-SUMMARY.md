---
phase: 01-foundation-akari-os-tweaks
plan: 06
subsystem: tweaks
tags: [defender, powershell, http-client-factory, sha256, registry, tweakhandler]

requires:
  - phase: 01-foundation-akari-os-tweaks (plan 01-02)
    provides: IRegistryService, ILogConsoleService, IWindowsServiceController, IScriptRunner primitives and AddAkariSystemPrimitives DI wiring
provides:
  - IPostInstallService/PostInstallService — minimal injectable port of the predecessor's asset-presence/download-fallback service (EnsureMinSudoAsync/EnsureDefenderFilesAsync/EnsurePostInstallAsync + the ~147-entry AllFiles manifest)
  - IPostInstallService.VerifyFileSha256Async — new SHA256 integrity gate primitive (T-01-SC), unit-tested independent of real downloaded assets
  - DefenderTweakHandler — the 32nd and final tweak, a byte-for-byte port of TweakService.SetDefenderAsync's full call graph, gated by the new integrity check
  - AddHttpClient("PostInstall", ...) DI registration
affects: [phase-4-downloads-page, phase-1-defender-real-machine-verification]

actuals:
  tokens: 10250
  tasks: 2
  commits: 2

tech-stack:
  added: [Microsoft.Extensions.Http]
  patterns:
    - "IHttpClientFactory named client (\"PostInstall\") registered via services.AddHttpClient, resolved per-call via CreateClient — no cached static HttpClient field"
    - "SHA256 pre-execution integrity gate on downloaded, elevated-execution-bound assets before any file copy/script run"
    - "D-01 unrefactored byte-for-byte port exemption: ITweakHandler as a thin routing wrapper around an otherwise untouched predecessor call graph"

key-files:
  created:
    - src/AkariToolbox.App/Services/IPostInstallService.cs
    - src/AkariToolbox.App/Services/PostInstallService.cs
    - src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs
    - src/AkariToolbox.Tests/PostInstallIntegrityTests.cs
  modified:
    - src/AkariToolbox.App/Services/TweakHandlerRegistration.cs
    - src/AkariToolbox.Framework/ServiceCollectionExtensions.Primitives.cs
    - Directory.Packages.props
    - src/AkariToolbox.App/AkariToolbox.App.csproj

key-decisions:
  - "IPostInstallService/PostInstallService and the AddHttpClient(\"PostInstall\", ...) registration are wired in AkariToolbox.App's TweakHandlerServiceCollectionExtensions.AddTweakHandlers, not the Framework project's AddAkariSystemPrimitives as the plan's action text specified — AkariToolbox.Framework has no ProjectReference to AkariToolbox.App, so registering an App-project type from the Framework project would require a circular project reference and fail to build."
  - "ExpectedNoDefenderCabSha256/ExpectedDisableDefenderPs1Sha256 were pinned by downloading both files directly from the pinned raw.githubusercontent.com PostInstall repo and hashing the bytes with sha256sum, rather than running Get-FileHash against a locally-downloaded C:\\PostInstall copy on a real Windows machine — no such machine was available in this automated worktree execution. Flagged in-code and in this SUMMARY for re-confirmation during the Task 2 human real-machine check."
  - "Defender's GetState/SetState back onto a dedicated CurrentUser flag (Software\\AkariToolbox\\DefenderState\\DisableDefender) instead of the predecessor's HKCU\\Software\\AkariTool hive (Pitfall 4), with SetState remaining a synchronous fire-and-forget dispatcher (_ = SetDefenderAsync(disable)) mirroring the predecessor's own SetDefender shape exactly; the flag is written/cleared from inside SetDefenderAsync at the same points the predecessor called SaveState/ClearState."

patterns-established:
  - "Circular-reference-safe DI placement: App-project-only service registrations live in App-project extension methods (TweakHandlerServiceCollectionExtensions), even when a plan's action text nominally points at the Framework project's shared AddAkariSystemPrimitives."

requirements-completed: [TWEAKS-02]

coverage:
  - id: D1
    description: "IPostInstallService ports EnsureMinSudoAsync/EnsureDefenderFilesAsync/EnsurePostInstallAsync and the full ~147-entry asset manifest, registered via IHttpClientFactory (no bare new HttpClient())"
    requirement: TWEAKS-02
    verification:
      - kind: unit
        ref: "dotnet build AkariToolbox.slnx -c Debug"
        status: pass
    human_judgment: false
  - id: D2
    description: "VerifyFileSha256Async correctly matches/mismatches/handles a missing file"
    requirement: TWEAKS-02
    verification:
      - kind: unit
        ref: "src/AkariToolbox.Tests/PostInstallIntegrityTests.cs#VerifyFileSha256Async_matching_hash_returns_true"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/PostInstallIntegrityTests.cs#VerifyFileSha256Async_mismatched_hash_returns_false"
        status: pass
      - kind: unit
        ref: "src/AkariToolbox.Tests/PostInstallIntegrityTests.cs#VerifyFileSha256Async_missing_file_returns_false_without_throwing"
        status: pass
    human_judgment: false
  - id: D3
    description: "DefenderTweakHandler's two-phase disable/re-enable workflow works end-to-end on a real Windows machine: Tamper Protection early-return with no partial state change, SHA256 integrity gate rejecting a tampered/mismatched file, and a full Phase 1 run (NoDefender.cab copy, elevated PS execution, RunOnce cleanup write) when Tamper Protection is off and hashes match"
    requirement: TWEAKS-02
    verification: []
    human_judgment: true
    rationale: "Requires a real Windows 10/11 machine with admin elevation, live Tamper Protection toggling, and a second UAC prompt — none of which are available in this automated worktree execution. Recorded as an open unrun-verify entry in .planning/WINDOWS.md (entry #1)."

duration: 55min
completed: 2026-09-01
status: complete
---

# Phase 1 Plan 06: Defender Two-Phase Workflow Summary

**DefenderTweakHandler ports TweakService.SetDefenderAsync's full call graph byte-for-byte (32nd and final tweak), backed by a new minimal PostInstallService and a SHA256 integrity gate on the two downloaded Defender-critical assets.**

## Performance

- **Duration:** 55 min
- **Started:** 2026-09-01T00:XX:XXZ
- **Completed:** 2026-09-01T01:XX:XXZ
- **Tasks:** 2
- **Files modified:** 8 (4 created, 4 modified)

## Accomplishments
- `IPostInstallService`/`PostInstallService`: injectable port of the predecessor's static asset-presence/download-fallback service, covering only the Defender dependency subset (`EnsureMinSudoAsync`/`EnsureDefenderFilesAsync`/`EnsurePostInstallAsync` + the ~147-entry manifest), using `IHttpClientFactory` instead of a cached static `HttpClient`
- `VerifyFileSha256Async`: new, not-ported integrity primitive closing BLOCKER T-01-SC — verified via 3 unit tests (match/mismatch/missing-file), all passing
- `DefenderTweakHandler`: the 32nd and final Akari OS tweak, a direct carry-over of `SetDefenderAsync`/`DefenderScheduleCleanup`/`IsDefenderTamperProtectionOn`/`DefenderBuildServiceBat`/`DefenderRunElevatedPsFileAsync`/`DefenderRunElevatedPsAsync`/`DefenderRunAsTrustedInstallerAsync` per D-01, with the new SHA256 gate inserted ahead of the Tamper Protection check
- Tamper Protection early-return path preserved exactly (4-line guidance, no partial state change)
- `AddHttpClient("PostInstall", ...)` and `IPostInstallService` DI registrations added to the App-project's tweak-handler registration extension

## Task Commits

1. **Task 1: Minimal IPostInstallService port + SHA256 integrity primitive** - `76b3141` (feat)
2. **Task 2: DefenderTweakHandler — byte-for-byte port of SetDefenderAsync** - `b53e22d` (feat)

**Plan metadata:** (this commit)

## Files Created/Modified
- `src/AkariToolbox.App/Services/IPostInstallService.cs` - New interface: LocalRoot/MinSudoPath/PowerRunPath/NoDefenderPath, presence flags, Ensure*/VerifyFileSha256Async
- `src/AkariToolbox.App/Services/PostInstallService.cs` - Implementation: full AllFiles manifest, IHttpClientFactory-backed downloads, SHA256 verification
- `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs` - The two-phase Defender disable/re-enable workflow, ITweakHandler Key="defender" Order=30
- `src/AkariToolbox.Tests/PostInstallIntegrityTests.cs` - 3 unit tests for VerifyFileSha256Async
- `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs` - Added AddHttpClient("PostInstall", ...) and AddSingleton<IPostInstallService, PostInstallService>() (deviation, see below)
- `src/AkariToolbox.Framework/ServiceCollectionExtensions.Primitives.cs` - Doc comment explaining why the PostInstall registration lives in the App project instead
- `Directory.Packages.props` - Pinned Microsoft.Extensions.Http 10.0.11 (needed for AddHttpClient/IHttpClientFactory)
- `src/AkariToolbox.App/AkariToolbox.App.csproj` - Added Microsoft.Extensions.Http PackageReference

## Decisions Made
- Registered `IPostInstallService`/`AddHttpClient` in the App project's `TweakHandlerServiceCollectionExtensions.AddTweakHandlers` rather than the Framework project's `AddAkariSystemPrimitives` (plan's literal action text) — see Deviations below.
- Pinned SHA256 hashes computed via direct HTTPS download + hash rather than `Get-FileHash` on a real machine's local `C:\PostInstall` copy — no live Windows test machine was available in this automated execution.
- Defender's `GetState`/`SetState` use a dedicated `CurrentUser\Software\AkariToolbox\DefenderState\DisableDefender` flag instead of the predecessor's `HKCU\Software\AkariTool` hive, consistent with every other handler's Pitfall-4 avoidance, while keeping `SetState`'s fire-and-forget dispatch shape identical to the predecessor's `SetDefender`.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Circular project reference prevented from AddAkariSystemPrimitives registration site**
- **Found during:** Task 1
- **Issue:** The plan's action text directs registering `AddHttpClient("PostInstall", ...)` and `IPostInstallService`/`PostInstallService` inside `AddAkariSystemPrimitives` in `src/AkariToolbox.Framework/ServiceCollectionExtensions.Primitives.cs`. `IPostInstallService`/`PostInstallService` are `AkariToolbox.App.Services` types (per the plan's own file list), but `AkariToolbox.Framework` has no `ProjectReference` to `AkariToolbox.App` — and `AkariToolbox.App` already references `AkariToolbox.Framework`. Adding the reverse reference would be circular and fail to build.
- **Fix:** Registered both lines in `AkariToolbox.App.Services.TweakHandlerServiceCollectionExtensions.AddTweakHandlers` instead — the closest existing App-project registration method, extended rather than a new one created. Left an explanatory doc comment in both files pointing to the actual registration site.
- **Files modified:** `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs`, `src/AkariToolbox.Framework/ServiceCollectionExtensions.Primitives.cs`
- **Verification:** `dotnet build AkariToolbox.slnx -c Debug` exits 0; `dotnet test` confirms `IPostInstallService`/`PostInstallService` resolve correctly in the DI-backed tests.
- **Committed in:** `76b3141` (Task 1 commit)

**2. [Rule 3 - Blocking] Missing Microsoft.Extensions.Http package reference**
- **Found during:** Task 1
- **Issue:** `services.AddHttpClient(...)` and `IHttpClientFactory` require the `Microsoft.Extensions.Http` NuGet package, which was not yet referenced by the project or centrally pinned in `Directory.Packages.props`.
- **Fix:** Added `Microsoft.Extensions.Http` version 10.0.11 (matching the existing `Microsoft.Extensions.*` family pin) to `Directory.Packages.props` and referenced it from `AkariToolbox.App.csproj`. This is an official, well-known Microsoft package in the same family as `Microsoft.Extensions.Hosting`/`DependencyInjection` already used throughout the project — not a novel/unverified dependency, so no package-legitimacy checkpoint was raised.
- **Files modified:** `Directory.Packages.props`, `src/AkariToolbox.App/AkariToolbox.App.csproj`
- **Verification:** `dotnet build AkariToolbox.slnx -c Debug` exits 0.
- **Committed in:** `76b3141` (Task 1 commit)

---

**Total deviations:** 2 auto-fixed (both Rule 3 — blocking build issues caused by the plan's literal file-placement instructions conflicting with the project's actual project-reference graph)
**Impact on plan:** Both deviations are DI-wiring/build-plumbing fixes only — no change to Defender's ported logic, the integrity gate's behavior, or the manifest content. No scope creep.

## Issues Encountered
- No live Windows 10/11 test machine was available in this automated worktree execution, so Task 2's `<human-check>` verification step (real-machine Tamper Protection ON/OFF flow, integrity-gate rejection of a tampered file, and a full Phase 1 disable run producing the `AkariDefenderCleanup` RunOnce entry) could not be executed. The automated portion of Task 2's verify (`dotnet build`) passed. This is recorded as an open `unrun-verify` entry (#1) in `.planning/WINDOWS.md` and as `human_judgment: true` coverage item D3 above — a human must run this check on a real machine before TWEAKS-02 can be considered fully verified, and should re-confirm the pinned SHA256 constants against the locally-downloaded `C:\PostInstall\Defender\NoDefender.cab`/`DisableDefender.ps1` at that time (they should match, since both were downloaded from the identical `raw.githubusercontent.com` URL `EnsureDefenderFilesAsync` uses, but a live re-check closes the loop).

## Next Phase Readiness
- All 32 Akari OS Tweaks handlers are now implemented (`DefenderTweakHandler` was the last).
- Phase 4/DOWNLOADS-01 can reuse `IPostInstallService`'s `AllFiles` manifest and `EnsurePostInstallAsync` as the foundation for the full Downloads-page asset mirror, extending rather than duplicating this plan's work.
- Blocker: Task 2's real-machine human-check (Tamper Protection flow, integrity-gate rejection, Phase 1 RunOnce write) remains open — see `.planning/WINDOWS.md` entry #1 and coverage item D3.

---
*Phase: 01-foundation-akari-os-tweaks*
*Completed: 2026-09-01*

## Self-Check: PASSED

- FOUND: src/AkariToolbox.App/Services/IPostInstallService.cs
- FOUND: src/AkariToolbox.App/Services/PostInstallService.cs
- FOUND: src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs
- FOUND: src/AkariToolbox.Tests/PostInstallIntegrityTests.cs
- FOUND commit: 76b3141
- FOUND commit: b53e22d
