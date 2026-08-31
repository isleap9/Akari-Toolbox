# Project Research Summary

**Project:** Akari Toolbox
**Domain:** WinUI 3 MVVM port of an elevated Windows system-tweak/debloat/gaming-optimization desktop utility (unpackaged, self-contained)
**Researched:** 2026-08-31
**Confidence:** MEDIUM-HIGH

## Executive Summary

Akari Toolbox is a WPF-to-WinUI3 MVVM port of an existing, working Windows tweak/debloat tool (AkariOS Companion), built on the user's own `WinUI-3-MVVM-Framework` template. This is not greenfield feature design — it's a parity port with two goals layered on top of "make it work the same": (1) fix known architecture debt (a 1117-line God-switch `TweakService`, 579 lines of programmatic UI-building in code-behind, UI controls injected into services) and (2) survive a set of WinUI3-specific platform traps that are invisible in WPF but will crash or silently no-op in WinUI 3 if not addressed deliberately. Experts in this space (WinUtil, ShutUp10, Win11Debloat) converge on the same shape: toggle-based tweaks grouped by category, accurate live state feedback (not cached UI state), PowerShell-backed bulk removal, and a pre-change safety net — Akari already has most of this except the safety net, which research flags as the single biggest gap versus category norms.

The recommended approach: keep the stack almost entirely as-is (Windows App SDK 2.3.1, .NET 10, CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.Hosting), add only `System.ServiceProcess.ServiceController` as a new package, and keep PowerShell execution process-based (`Process.Start powershell.exe`) rather than adopting the in-process `Microsoft.PowerShell.SDK` — this preserves the predecessor's proven, tested logic and avoids deployment bloat. Architecturally, decompose the God-switch into one `ITweakHandler` class per tweak resolved through an `ITweakCatalog`, introduce a thin `IRegistryService`/`IWindowsServiceController`/`IScriptRunner` primitive layer that is the sole seam touching real system state, and express all list-based UI (tweaks, debloat actions) via XAML `ItemsRepeater`/`ItemsControl` bound to `ObservableCollection<T>` instead of code-behind UI construction.

The key risks are platform-specific and must be addressed early, not discovered late: (1) `Windows.Storage.Pickers` crashes under `requireAdministrator` — the app runs elevated for its entire lifetime, so any file/folder picker page needs a non-default picker implementation from day one; (2) Windows Defender's Tamper Protection silently no-ops naive registry-based disable attempts, which is exactly why the predecessor's two-phase Defender workflow exists and must be ported faithfully with real state verification, not collapsed into a simpler single toggle; (3) cross-thread UI updates that WPF tolerated will throw `COMException` in WinUI 3, requiring disciplined `DispatcherQueue`/`IProgress<T>` marshaling on every async callback (PowerShell output, download progress); (4) tweaks currently have no revert/backup mechanism, which is the most common category-wide complaint against comparable tools and should be added even within "parity" scope since it's a safety property, not a new feature; (5) the distributed unsigned elevated exe will trigger SmartScreen/AV heuristics — expected, but should be budgeted for (code signing, release-note messaging) rather than treated as a late surprise.

## Key Findings

### Recommended Stack

The base stack (Windows App SDK 2.3.1, .NET 10, CommunityToolkit.Mvvm 8.4.2, Microsoft.Extensions.Hosting) is already decided via the framework template and confirmed current. Only one new package is needed for the port's system-mutation needs; everything else (registry access, HTTP, SHA256) is in-box BCL. PowerShell execution deliberately stays process-based rather than adopting the in-process SDK, and GitHub interaction stays a plain `HttpClient` GET against known asset URLs rather than pulling in Octokit — both to avoid weight and complexity the app doesn't need.

**Core technologies:**
- Windows App SDK 2.3.1 (stable) — WinUI 3 runtime; do not move to 2.4.x (experimental channel only)
- .NET 10 (LTS) + CommunityToolkit.Mvvm 8.4.2 — matches framework template, no version bump needed
- `System.ServiceProcess.ServiceController` 9.0.9 (new) — must be added explicitly; not part of the WinUI3 SDK-style project's shared framework
- `Microsoft.Win32.Registry`/`RegistryKey`, `System.Security.Cryptography.SHA256`, `System.Net.Http` — all in-box, no new packages
- Explicitly excluded: `Microsoft.PowerShell.SDK` (weight/behavior mismatch), Octokit.net (unneeded API surface), Velopack (would create a second, overlapping update mechanism alongside the existing self-heal pattern)

### Expected Features

Akari's existing feature set (32 OS tweaks, gaming tweaks + launcher grid, 28 debloat actions, self-healing PostInstall downloader, 12 context-menu entries) already matches or exceeds category table stakes across the board except one: a pre-change safety net (restore point / revert capability), which every credible competitor (WinUtil, ShutUp10) has and Akari currently lacks. The two-phase Defender-disable workflow and the self-healing PostInstall asset mirror are genuine differentiators no mainstream competitor offers.

**Must have (table stakes, v1 parity):**
- Toggle-based tweaks grouped by category, with state read live from the system (not cached UI state)
- Bulk bloatware/app removal via PowerShell-backed actions
- Elevation clearly required (`requireAdministrator`), carried forward as-is
- Context-menu add/remove entries (classic/legacy menu only — Windows 11's modern context menu is not registry-editable)
- Gaming quick-launch grid linking to vendor tools rather than reimplementing GPU tuning

**Should have (competitive differentiators, keep and highlight):**
- Two-phase guided Defender-disable workflow with explicit Tamper Protection gating
- Self-healing PostInstall asset mirror from GitHub (idempotent, no-op if present)
- Product identity tied to Akari OS's specific deployment model

**Add after v1 validation (v1.x, safety upgrades — not new tweak categories):**
- Automatic system-restore-point creation before applying tweaks/debloat
- Explicit risk-level labeling per toggle (safe/standard vs advanced/risky)

**Defer (v2+, per PROJECT.md):**
- Deep "Ultimate" tweak tier (~110 scripts across 8 categories)
- Curated SHA256-verified third-party tool bundle
- Continuous background tweak-enforcement/drift-protection daemon — explicitly an anti-feature for this tool's scope, not just deferred

### Architecture Approach

A two-tier service architecture: ViewModels call feature-facing services (`ITweakCatalog`, `IDebloatService`, `IPostInstallService`), which in turn compose a thin, UI-agnostic system-primitive layer (`IRegistryService`, `IWindowsServiceController`, `IScriptRunner`) that is the sole seam touching real registry/service/process state. This replaces the predecessor's 1117-line `TweakService` God-switch with one `ITweakHandler` class per tweak (independently testable, no merge-conflict-prone shared file), and replaces programmatic UI construction in code-behind (`GamingTweaksPage.xaml.cs` at 579 lines) with XAML `ItemsRepeater`/`ItemsControl` bound to `ObservableCollection<T>`.

**Major components:**
1. `Services/System/` (`IRegistryService`, `IWindowsServiceController`, `IScriptRunner`) — the mockable seam; nothing outside this layer touches `Microsoft.Win32.Registry*`, `ServiceController`, or `Process` directly
2. `Services/Tweaks/` (`ITweakHandler` per tweak + `ITweakCatalog` dictionary lookup) — replaces the God-switch; two-phase Defender logic lives in its own handler
3. `Services/Debloat/` (`IDebloatService` wrapping `IScriptRunner`) — drives the 28 PowerShell actions, streams output via `IProgress<string>`
4. `Services/PostInstall/` (`IPostInstallService`) — GitHub asset mirror, independent of the tweak framework, filesystem + network only
5. ViewModels (unchanged shape from predecessor) — thin, translate user intent into service calls, marshal results via framework's `IDialogService`/`IInfoBarService`

Manifest-driven tweak metadata (`TweakDefinition`, separate from handler logic) is recommended starting in v1 even though not strictly required at 72 items — it's cheap now and prevents an expensive retrofit when v2's "Ultimate" tier roughly triples the tweak count.

### Critical Pitfalls

1. **File/folder pickers crash under `requireAdministrator`** — `Windows.Storage.Pickers` throws `COMException (E_FAIL)` when invoked from an elevated process; the framework's default `IFilePickerService` must be replaced with `Microsoft.Windows.Storage.Pickers` or Win32 CsWin32 dialogs before any picker-using page ships. Address in the framework-adaptation/elevation-enablement phase, verify explicitly on the Downloads page.
2. **Defender's Tamper Protection blocks naive registry disables** — a registry write to Defender keys can "succeed" (no exception) while silently having no effect. The predecessor's two-phase workflow exists precisely to handle this; it must be ported with real state verification (`Get-MpComputerStatus`), not collapsed into a single toggle.
3. **No revert/backup for registry & service tweaks** — the most common category-wide complaint against comparable tools. Every mutation should read-and-record prior state (including "key absent") before writing, so "off" restores actual prior state rather than a guessed default.
4. **Cross-thread UI updates crash WinUI 3 where WPF tolerated them** — background callbacks (process output, download progress) touching bound ViewModel state throw `COMException` ("marshalled for a different thread") unless explicitly marshaled via `DispatcherQueue`/`IProgress<T>`. Establish the pattern once in the framework layer; verify in Debloat and Downloads phases specifically.
5. **Unsigned elevated exe will trigger SmartScreen/AV heuristics** — expected given the app's behavior profile (elevated, extracts/runs PowerShell, touches Defender/registry). Budget code signing and proactive release-note messaging as a distribution-phase deliverable, not a post-release fire.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Framework Adaptation & Elevation Enablement
**Rationale:** Every other phase depends on the app running correctly elevated and unpackaged; platform traps here (picker crashes, manifest merge behavior, cross-thread marshaling pattern) are invisible until you actually run the elevated exe, so they must be resolved before feature pages are built on top of a broken foundation.
**Delivers:** `app.manifest` with `requireAdministrator`, verified against the actual `PublishSingleFile` unpackaged build; replacement file/folder picker service (`Microsoft.Windows.Storage.Pickers` or CsWin32) behind the existing `IFilePickerService` interface; a centralized dispatcher/marshaling pattern (`IDispatcherService` or equivalent) established once for reuse across all async services.
**Avoids:** Pitfall 1 (picker crash), Pitfall 5 (cross-thread crashes), and the manifest-merge bug class (confirmed non-reproducing on SDK 2.3.1, but flagged as an early smoke-test item).

### Phase 2: System Primitive Layer
**Rationale:** `ITweakHandler`, `IDebloatService`, and `IPostInstallService` all depend on this layer; building it first establishes the mockable seam and prevents the predecessor's anti-pattern of scattering raw `Microsoft.Win32.Registry`/P/Invoke calls across services and pages.
**Delivers:** `IRegistryService` (including the real-logged-on-user-HKCU-under-elevation P/Invoke trick, isolated once), `IWindowsServiceController` (wrapping `System.ServiceProcess.ServiceController`), `IScriptRunner` (embedded `.ps1` extraction + `Process.Start powershell.exe` with correct `ProcessStartInfo` flags preserved from the predecessor).
**Uses:** `System.ServiceProcess.ServiceController` 9.0.9 from STACK.md.
**Implements:** Architecture's "system primitive / mutation layer," Anti-Pattern 4 fix.

### Phase 3: Akari OS Tweaks Page (32 registry-backed toggles + two-phase Defender)
**Rationale:** The core category-defining feature and the first real exercise of `ITweakCatalog`/`ITweakHandler` decomposition; also where the Defender Tamper Protection pitfall must be handled correctly since it's the highest-risk single tweak.
**Delivers:** `ITweakCatalog` + one `ITweakHandler` per tweak (replacing the God-switch), read-and-record-prior-state pattern for revertibility, two-phase Defender handler with explicit `Get-MpComputerStatus` verification and Tamper Protection precondition messaging.
**Addresses:** Table-stakes toggle UI with live state feedback; the two-phase Defender differentiator (FEATURES.md).
**Avoids:** Pitfall 2 (Tamper Protection no-op), Pitfall 3 (no revert capability).

### Phase 4: Gaming Tweaks Page (toggles, dropdowns, service config, third-party launcher grid)
**Rationale:** Reuses the same `ITweakCatalog` pattern from Phase 3 plus adds `IWindowsServiceController` usage and non-tweak "launch" actions; natural follow-on once the tweak pattern is proven.
**Delivers:** Gaming-specific tweak handlers, service start/stop/config via `IWindowsServiceController`, quick-launch grid using `Process.Start UseShellExecute=true` for external tools (kept architecturally distinct from stateful tweaks).
**Implements:** Architecture's `IWindowsServiceController` integration point; Anti-Pattern 2 fix (replacing `GamingTweaksPage.xaml.cs`'s 579 lines of programmatic UI with XAML `ItemsRepeater`).

### Phase 5: Debloat Page (28 PowerShell-backed actions)
**Rationale:** The most async-heavy, cross-thread-crash-prone page (streamed script output over several seconds); best done after the dispatcher/marshaling pattern (Phase 1) and `IScriptRunner` (Phase 2) both exist and are proven on the simpler Tweaks pages first.
**Delivers:** `IDebloatService` wrapping `IScriptRunner`, `IAsyncRelayCommand`-driven actions with `IsRunning`/cancellation, streamed log output via `IProgress<string>` to an `ObservableCollection<string>`.
**Addresses:** Table-stakes bulk-removal feature, now architecturally correct (moved out of `DebloatPage.xaml.cs` code-behind per PROJECT.md's explicit debt callout).
**Avoids:** Pitfall 4 (PowerShell output/exit-code regressions), Pitfall 5 (cross-thread crashes) — this page is the primary verification point for both.

### Phase 6: Downloads Page (self-healing PostInstall mirror + playbooks/drivers/links) & Misc Page (context-menu entries)
**Rationale:** Both are lower-risk, more self-contained features that don't block or get blocked by the tweak/debloat pages; grouping them lets the roadmap close out v1 parity with the remaining table-stakes/differentiator features.
**Delivers:** `IPostInstallService` (async download, no-op-if-present, graceful degrade on network failure/rate limit), `IContextMenuService` (registry-based add/remove, classic menu only).
**Addresses:** Self-healing differentiator (FEATURES.md), context-menu table stakes.
**Avoids:** Pitfall 6 integration gotcha (synchronous/always-re-download antipattern), file-picker pitfall if Downloads page needs a location picker (verify Phase 1's fix covers this flow).

### Phase 7: Release & Distribution
**Rationale:** Comes last by nature (packaging/signing/release-notes work), but the "don't obfuscate, don't hide elevated/Defender-touching behavior" constraint from this phase should be treated as a standing rule applied during every earlier phase, not a bolt-on at the end.
**Delivers:** Signed (or explicitly-documented-as-unsigned) `PublishSingleFile` build, release notes documenting expected SmartScreen/AV behavior, AV false-positive submission as part of the release checklist.
**Avoids:** Pitfall 6 (AV/SmartScreen flags treated as a late surprise rather than budgeted).

### Phase Ordering Rationale

- Foundation-first: elevation/picker/dispatcher fixes (Phase 1) and the system-primitive layer (Phase 2) are dependencies of every feature page — building a page before these exist means rework, not just risk.
- Tweak-pattern proven on the simplest surface first: OS Tweaks (Phase 3) establishes `ITweakCatalog`/`ITweakHandler` and the revert-safety pattern before Gaming Tweaks (Phase 4) reuses it with added service-control complexity.
- Highest cross-thread-crash risk deliberately sequenced after the marshaling pattern is proven: Debloat (Phase 5) is the most async-heavy page and benefits from Tweaks pages having already exercised the simpler async paths.
- Lower-risk, more independent features grouped last among feature phases (Phase 6) since they don't share dependencies with the tweak/debloat core and can slot in flexibly.
- Distribution concerns (Phase 7) close the roadmap but their governing constraint (no obfuscation of elevated/Defender-touching behavior) applies throughout — call this out explicitly during phase planning, not just at the end.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 1 (Framework Adaptation):** `Microsoft.Windows.Storage.Pickers` vs CsWin32 fallback is only MEDIUM-confidence researched — needs a concrete implementation spike before committing to one approach.
- **Phase 3 (Defender two-phase workflow):** Tamper Protection state-verification specifics (`Get-MpComputerStatus`/WMI exact fields, precondition UX copy) need validation against a real Windows 10 and Windows 11 machine, not just documentation.
- **Phase 7 (Release/Distribution):** Code-signing options (cheap OV cert vs Microsoft Trusted Signing) and AV false-positive submission workflows weren't deeply compared — needs a cost/process spike closer to release.

Phases with standard patterns (skip research-phase):
- **Phase 2 (System Primitive Layer):** Directly grounded in the predecessor's actual working code (HIGH confidence) — registry/service/process wrapping is well-understood.
- **Phase 4 & 5 (Gaming Tweaks, Debloat):** Reuse Phase 2/3 patterns; primarily a porting and MVVM-restructuring exercise with known anti-patterns to avoid, not new unknowns.
- **Phase 6 (PostInstall/Misc):** Predecessor's `PostInstallService` pattern is already sound (idempotent mirror-if-missing) — port faithfully.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | MEDIUM | Base stack confirmed via official docs/NuGet; PowerShell SDK vs process-based tradeoff is LOW/MEDIUM sourced (blog-level) but reasoning is sound and independent of source quality |
| Features | MEDIUM | Web-sourced, cross-checked across multiple independent tools (WinUtil, ShutUp10); no official vendor API docs exist for this category by nature — competitor analysis is community/aggregator sourced |
| Architecture | HIGH (component boundaries, service decomposition) / MEDIUM (elevation manifest behavior) | Component/service design grounded directly in reading the actual predecessor codebase and framework template (primary sources); WinAppSDK elevation-manifest bug history is cross-checked against official GitHub repos but not independently reproduced |
| Pitfalls | MEDIUM-HIGH | Critical items (picker crash, Tamper Protection, cross-thread marshaling) corroborated by official Microsoft repos/docs and multiple independent sources; AV-heuristic specifics are inherently anecdotal/LOW-confidence by nature of the topic |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **Picker replacement approach (Microsoft.Windows.Storage.Pickers vs CsWin32):** not resolved to a single recommendation — validate with a small spike in Phase 1 before committing across all picker-using pages.
- **Manifest-merge bug reproduction on SDK 2.3.1:** research indicates this should not reproduce (fixed well before 2.3.1) but was not independently verified in this environment — treat as an early smoke-test item in Phase 1, not an assumption.
- **Restore-point / revert-capability scope:** FEATURES.md flags this as a recommended v1.x addition rather than a hard v1 requirement, but PITFALLS.md treats "no revert capability" as unacceptable ("Never" acceptable per the technical debt table) — this tension should be resolved explicitly during requirements/roadmap discussion, since it affects whether Phase 3's scope includes prior-state recording as P1 or defers the restore-point UI to P2.
- **Code-signing cost/process:** not researched in depth — needs a dedicated spike before Phase 7 planning.

## Sources

### Primary (HIGH confidence)
- Direct reading of `AkariOS-Companion` predecessor source (`ITweakService.cs`, `TweakService.cs`, `ToolService.cs`, `RunActions.cs`, `TweakItem.cs`, `AkariOSTweaksViewModel.cs`, `GamingTweaksPage.xaml.cs`, `app.manifest`)
- Direct reading of `WinUI-3-MVVM-Framework` template (`App.xaml.cs`, `ServiceCollectionExtensions.cs`, `ViewModelBase.cs`, `FrameNavigationService.cs`, `IInfoBarService.cs`, `DispatcherQueueExtensions.cs`, `app.manifest`)
- `.planning/PROJECT.md` — project's own documented ground truth on scope, architecture debt, and v1/v2 sequencing
- [Protect security settings with tamper protection — Microsoft Learn](https://learn.microsoft.com/en-us/defender-endpoint/prevent-changes-to-security-settings-with-tamper-protection)
- [Microsoft Store Not Found/Missing After Debloating Windows 10 — Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/3790047/microsoft-store-not-found-missing-after-debloating)

### Secondary (MEDIUM confidence)
- [Distribute an unpackaged WinUI 3 app — Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app)
- [microsoft/WindowsAppSDK Discussion #3038, #671, Issue #3376](https://github.com/microsoft/WindowsAppSDK/discussions/3038) — elevated-app WinRT activation limitations
- [microsoft/WindowsAppSDK Issue #2504 — FileOpenPicker crash under Administrator](https://github.com/microsoft/WindowsAppSDK/issues/2504)
- [microsoft/microsoft-ui-xaml Discussion #8410, Issue #9208 — cross-thread COMException](https://github.com/microsoft/microsoft-ui-xaml/discussions/8410)
- [Microsoft confirms why Windows Defender can't be disabled via registry — BleepingComputer](https://www.bleepingcomputer.com/news/microsoft/microsoft-confirms-why-windows-defender-can-t-be-disabled-via-registry/)
- [ChrisTitusTech/winutil (GitHub)](https://github.com/christitustech/winutil) and [WinUtil restore-point precedent](https://github.com/ChrisTitusTech/winutil/issues/983)
- [O&O ShutUp10 official features page](https://www.oo-software.com/en/shutup10/features)
- [AsyncRelayCommand / RelayCommand attribute docs — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand)

### Tertiary (LOW confidence)
- [Running PowerShell from C# in 2025 — CodeCube Ventures](https://codecube.net/2025/7/powershell-from-csharp-updated/) — needs validation if in-process PowerShell hosting is reconsidered later
- [Mitigating SmartScreen and Defender False Positives — buralog](https://buralog.jp/en/defender-smartscreen-falsepositive-en/) — directionally consistent but anecdotal; validate signing/submission specifics before Phase 7
- [Win32PrioritySeparation for Gaming — FPSHeaven](https://fpsheaven.com/blogs/news/win32priorityseparation) — enthusiast-sourced; real-world FPS impact is debated, frame UI copy accordingly

---
*Research completed: 2026-08-31*
*Ready for roadmap: yes*
