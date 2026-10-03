# Codebase Concerns

**Analysis Date:** 2026-10-03

## Tech Debt

**Swallowed exceptions everywhere (`catch {}`):**
- Issue: ~60+ bare `catch {}` blocks silently discard failures, mirroring the original PowerShell `try {} catch {}`. Operators get no log, no InfoBar signal, and no way to distinguish "already clean" from "access denied".
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs` (lines 32-51 `Winget`, 96, 99, 125, 199, 242, 266, 300, 421, 619, 649, 665, 674, 716, 727, 743, 753, 762, 764, 776, 833, 906), `src/AppTemplate.App/Tweaks/WindowsActions.cs` (58, 89, 93, 105, 109, 112, 128, 141, 192, 212, 228, 237, 333, 705, 738, 745, 746, 810), `src/AppTemplate.App/Tweaks/WindowsActions.System.cs` (141, 318, 328, 458, 605, 623, 644), `src/AppTemplate.App/Tweaks/SystemInfo.cs` (37, 62, 90, 112, 165, 186 — every probe returns `"Unknown"`), `src/AppTemplate.App/Tweaks/CheckActions.cs` (95), `src/AppTemplate.App/Tweaks/SetupActions.cs` (42), `src/AppTemplate.App/Tweaks/DriverActions.cs` (128, 202), `src/AppTemplate.App/Tweaks/AdvancedActions.Services.cs` (48), `src/AppTemplate.App/Tweaks/ControlPanelActions.cs` (89), `src/AppTemplate.App/Tweaks/Data/Windows/SetTimerResolutionService.cs` (39, 61, 86)
- Impact: Failed debloat/registry/service operations look successful; debugging requires ProcMon instead of logs.
- Fix approach: Route at minimum through `ILogger` (`FileLoggerProvider` already exists at `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`). Keep UI silent but log `LogDebug` with operation name + HRESULT/exception. Add an opt-in verbose log toggle.

**`async void` fire-and-forget ViewModel handlers:**
- Issue: All toggle/option applicators are `async void` (`ApplyToggle`, `ApplyOption`) invoked from `[ObservableProperty]` partial `OnXChanged` callbacks, plus unawaited `_ = ApplySafeBootAsync(...)` and `_ = _apply(this, value)`. Exceptions only surface via the inner try/catch; re-entrancy is guarded by a per-VM `IsBusy` flag that is easy to bypass (rapid toggle flips are dropped silently).
- Files: `src/AppTemplate.App/ViewModels/AdvancedViewModel.cs` (362, 391, 132, 303, `ApplySafeBootAsync` 339-360), `src/AppTemplate.App/ViewModels/GamingViewModel.cs` (202), `src/AppTemplate.App/ViewModels/GraphicsViewModel.cs` (125), `src/AppTemplate.App/ViewModels/HardwareViewModel.cs` (92, 121), `src/AppTemplate.App/ViewModels/SetupViewModel.cs` (229), `src/AppTemplate.App/ViewModels/WindowsViewModel.cs` (385), `src/AppTemplate.App/ViewModels/TweaksViewModel.cs` (`CpRowItem.OnIsOnChanged` 284-289 does `_ = _apply(this, value)`)
- Impact: Overlapping applies can interleave registry writes; dropped toggles leave switch position disagreeing with system state until re-navigation. `OnUnobservedTaskException` handler in `src/AppTemplate.App/App.xaml.cs` (223-227) exists precisely because of this pattern.
- Fix approach: Convert to `[RelayCommand]`-gated async `Task` methods with a shared `SemaphoreSlim(1,1)` per VM (pattern already exists in `src/AppTemplate.Framework/Services/IDialogService.cs:52`), disable the control via `IsBusy`, and re-verify with the `Read*` probe after apply (some VMs already do this — standardize it).

**Blocking sync-over-async on the UI path:**
- Issue: `NativeOps.Download`, `HttpGetString`, `DownloadWithHeaders` call `.GetAwaiter().GetResult()` on `HttpClient`; imaging helpers block on `BitmapDecoder/Encoder` WinRT async ops; `App.xaml.cs:101` blocks on `RedirectActivationToAsync(...).GetAwaiter().GetResult()`. They only avoid deadlock today because every VM wraps them in `await Task.Run(...)`.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs` (432, 435, 447, 459, 830, 844, 853), `src/AppTemplate.App/App.xaml.cs` (101)
- Impact: Any future direct call from the UI thread deadlocks. Thread-pool threads are held for the full 10-minute HTTP timeout.
- Fix approach: Add `DownloadAsync` / `HttpGetStringAsync(CancellationToken)` overloads, make `TweakAction.Apply` async (`ApplyAsync`), thread `CancellationToken` from the VM `RunAsync` helpers. Keep sync wrappers only for the headless safe-boot entry points.

**No cancellation, no timeouts, no progress:**
- Issue: No `CancellationToken` anywhere in the tweak pipeline (`IncrementalLoadingCollection` is the only cancellable code, and it is unused by tweaks). `HttpClient.Timeout = 10 min`, `Process.WaitForExit()` waits forever, downloads have no progress reporting despite `IStatusService.Start` accepting only a name.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs` (426 `new HttpClient { Timeout = 10min }`, 45, 240, 263, 363 `WaitForExit`), `src/AppTemplate.App/Tweaks/TweakAction.cs` (133-149 `RunProcessAction`), all `*ViewModel.cs` `RunAsync` helpers
- Impact: A hung winget/download/schtasks call hangs the page's `IsBusy` gate indefinitely; user must kill the app.
- Fix approach: `WaitForExit(timeout)` + kill-on-timeout, `HttpClient.GetAsync(..., cancellationToken)` with linked CTS per apply, surface bytes/sec in `IStatusService`.

**Duplicated / dead helpers:**
- Issue: `PromoteAllTrayIcons()` and `SetTrayIconsPromoted(int)` in `NativeOps.cs` (679-690, 963-974) are near-identical; the former is a no-op (`if promoted != 0 → set 1`). `CpTabGroup.Header` (`src/AppTemplate.App/ViewModels/TweaksViewModel.cs:212`) concatenates the same suffix regardless of `IsExpanded`. `TreatWarningsAsErrors=false` in `Directory.Build.props:16` while scripts claim "Errors: 0 Warnings: 0".
- Impact: Confusion about which tray-icon path is live; XAML `Header` binding never visibly changes.
- Fix approach: Delete `PromoteAllTrayIcons`, keep parameterized version; fix `Header` to show expand/collapse glyph; either enforce `TreatWarningsAsErrors=true` or drop the zero-warning claim from `README.md` / `OPENCODE.md`.

**Static mutable global state:**
- Issue: `AkariToolState.Store` is a never-disposed static `RegistryKey` (`src/AppTemplate.App/Tweaks/AkariToolState.cs:14-15`); `SystemInfo` caches OS/CPU/GPU in static strings with no invalidation (37-92); `ControlPanelRows.ProbeCache` is an unbounded static dictionary (grows to 169 entries, fine, but never cleared); `App.Services` / `App.MainWindow` are public static mutable singletons (`src/AppTemplate.App/App.xaml.cs:27-30`); `NativeOps.Http` is a static `HttpClient` with no DNS-refresh handling.
- Impact: Registry handle leak for process lifetime (benign but unclean); stale hardware strings after driver change; test-host cross-test pollution.
- Fix approach: Dispose-on-shutdown for `Store`, cache invalidation on driver pages, `IHttpClientFactory` instead of static client (already have DI + `Microsoft.Extensions.Hosting`).

## Known Bugs

**`ApplySafeBootAsync` never resets on success:**
- Symptoms: After arming a safe-boot flow (Defender disable, Services off), `IsBusy` stays `true` and `_status` stays in "operation..." state. Since the machine reboots immediately this is mostly invisible — but if `Restart()` fails or the user cancels, the page is permanently locked until re-navigation.
- Files: `src/AppTemplate.App/ViewModels/AdvancedViewModel.cs:339-360` — `try/catch` with no `finally`; the `IsBusy=false` / `_status.Complete()` only run in the `catch` branch. Compare with `ApplyToggle` (370-389) which has the `finally`.
- Trigger: Any successful safe-boot arm where reboot does not happen (bcdedit failure swallowed, `Restart()` throws, dry-run).
- Workaround: Re-navigate to the page (fresh transient VM).
- Fix: Add the same `finally { IsBusy = false; _status.Complete(); }` block.

**`NativeOps.PickFile` can never return a path:**
- Symptoms: Intel driver picker (`DriverActions.PickIntelDriver` → `NativeOps.PickFile`) always yields null/empty, so `InstallIntelDriver` never runs.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs:500-524` — `lpstrFile = buffer.ToString()` copies the *current empty contents* of the `StringBuilder` into a `string` field; `GetOpenFileNameW` then has no buffer to write into, and `return buffer.ToString()` returns the still-empty builder.
- Trigger: Graphics page → Intel driver → "Select installer".
- Fix approach: Change `OpenFileName.lpstrFile` to `StringBuilder` with capacity (`nMaxFile = buffer.Capacity`), pass the builder by ref, return `buffer.ToString()`.

**Tray-icon "promote all" is a no-op:**
- Symptoms: The action reads `IsPromoted != 0` and writes `1` — icons already shown stay shown, hidden icons (`0`) are skipped, so nothing changes.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs:679-690` (`PromoteAllTrayIcons`), same shape in `SetTrayIconsPromoted` (963-974).
- Trigger: Any tweak calling the promote path.
- Fix approach: Decide intent (show-all → write `1` unconditionally; hide-all → write `0`) and probe with `RegRead` after.

**Control Panel probe only inspects the first value:**
- Symptoms: `ParseProbe` returns after the first `dword:`/`"string"` line in a multi-value `.reg` body, so a row reports "Optimized" when only 1 of N values matches. Toggling such a row off then on can also leave partial state.
- Files: `src/AppTemplate.App/Tweaks/ControlPanelRows.cs:47-74`, applied via `NativeOps.ImportRegContent` in `ApplyRow` (44-45).
- Trigger: Any of the 169 rows in `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs` whose Optimize body sets 2+ values.
- Fix approach: Parse *all* value lines into the probe and require all to match; fall back to `null` (persisted state) when the body contains deletes (`-`) or non-value lines.

**Synchronous registry/disk work on the UI thread:**
- Symptoms: `TweaksViewModel.OnSvcHostIndexChanged` / `OnWin32PriorityIndexChanged` (104-131) call `GamingActions.Apply*` synchronously inside the property-changed callback; `OnNavigatedTo` refreshes all 169 `CpRowItem`s synchronously (68-75); short UI freezes on navigation.
- Files: `src/AppTemplate.App/ViewModels/TweaksViewModel.cs:100-131`, `src/AppTemplate.App/ViewModels/TweaksViewModel.cs:53-81`
- Trigger: Open Individual Tweaks page; flip the two scheduling combos.
- Fix approach: Route through the same `Task.Run` + `IsBusy` pattern every other VM uses; make row refresh async/batched.

## Security Considerations

**Unsigned runtime downloads executed as admin (highest risk):**
- Risk: ~30+ binaries/installers are fetched at runtime from `https://github.com/FR33THYFR33THY/Ultimate/releases/download/Files/` (plus `addons.mozilla.org` for uBO, `gfwsl.geforce.com`/`amd.com` scrapes for drivers) with **no hash, signature, or certificate check**, then executed/launched from `%SystemRoot%\Temp` — all while the process is elevated (`requireAdministrator` manifest) and sometimes as TrustedInstaller. A compromised release asset, DNS/MITM, or stale URL = arbitrary code as SYSTEM.
- Files: `src/AppTemplate.App/Tweaks/InstallerActions.cs` (13-14, 57-117), `src/AppTemplate.App/Tweaks/CheckActions.cs` (13-14, 103-138), `src/AppTemplate.App/Tweaks/DriverActions.cs` (15-16, 27-38, 48-50, 106-133, 152-182), `src/AppTemplate.App/Tweaks/GraphicsActions.cs` (21, 67, 106, 409, 423, 447), `src/AppTemplate.App/Tweaks/NativeOps.cs` (429-460 download core)
- Current mitigation: HTTPS only; `Zone.Identifier` stripping is per-folder (`UnblockFolder`), not a bypass of SmartScreen for the downloads themselves.
- Recommendations: Pin SHA-256 hashes per file (embed manifest, verify before execute); `EnsureSuccessStatusCode` already throws — catch it into a user-visible failure instead of hanging `IsBusy`; consider Authenticode check (`WinVerifyTrust`) for `.exe`/`.msi`; never execute from `%SystemRoot%\Temp` (use per-run ACL'd subdir); log URL + hash + result.

**TrustedInstaller service-hijack primitive:**
- Risk: `RunAsTrustedInstaller` rewrites `TrustedInstaller binPath` to `cmd.exe /c powershell.exe -encodedcommand <b64>`, starts the service, then restores. Any bug (exception between config and restore, reboot mid-flow, concurrent caller) leaves the highest-privilege service pointing at attacker-influencable content. The encoded command is built by string concatenation.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs:915-933`, callers in `src/AppTemplate.App/Tweaks/AdvancedActions.cs` (67, 88), `src/AppTemplate.App/Tweaks/WindowsActions.cs` (662, 669), `src/AppTemplate.App/Tweaks/WindowsActions.System.cs` (434), `src/AppTemplate.App/Tweaks/ControlPanelActions.cs` (109)
- Current mitigation: Runs only elevated; restore attempted synchronously after start.
- Recommendations: Wrap restore in `try/finally`; read back `sc qc TrustedInstaller` to verify restore; prefer targeted ACL/ownership APIs over full TI impersonation where possible; audit every `tiCommand` string for quoting flaws.

**Defender / Firewall / BitLocker / service destruction:**
- Risk: One-toggle actions fully disable Defender, Firewall, DEP, Spectre/Meltdown mitigations, decrypt all BitLocker volumes, and `sc delete` whole service families (`StopAndDeleteServicesMatching("Edge")`, `"brlapi"`; `StopAndDeleteService`). Safe-boot flows reboot the machine twice via `bcdedit` + `RunOnce` under HKLM.
- Files: `src/AppTemplate.App/Tweaks/AdvancedActions.cs` (35-90 Defender), `src/AppTemplate.App/Tweaks/AdvancedActions.Services.cs` (services matrix), `src/AppTemplate.App/Tweaks/NativeOps.cs:857-864` (`DisableBitLockerAllDrives`), `1003-1008` (`StopAndDeleteService`), `215-222`, `src/AppTemplate.App/Tweaks/SetupActions.cs` (19, 98), `src/AppTemplate.App/Tweaks/WindowsActions.cs` (421, 748, 781)
- Current mitigation: Toggles show "Restart to apply"; safe-boot re-entry is explicit.
- Recommendations: Add confirm dialogs with irreversible-action wording (pattern exists: `NativeMessageBox.Confirm` in `CheckActions.RunBiosCheck`); create a restore point *before* destructive applies (only Home does it today — `HomeViewModel.cs:105`); log pre/post service Start values for rollback.

**PowerShell injection via prompt interpolation:**
- Risk: `NativeOps.Prompt` builds `powershell.exe -EncodedCommand` by interpolating user-visible `message`/`title` with only `'`-doubling; `BuildAutounattendUsb` substitutes the account name with raw `string.Replace("@", username)` into XML written to USB root.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs:148-165` (`Prompt`), `1033-1048` (`BuildAutounattendUsb`), `177-188` (`BuildSetupCompleteUsb` — writes `SetupComplete.cmd` to removable media from app content)
- Recommendations: Pass prompt strings as base64 arguments instead of inline literals; XML-escape the username (`SecurityElement.Escape`); validate drive-root target is actually removable before writing.

**Registry mass-operations with no backup/rollback:**
- Risk: `ImportRegContent` shells to `regedit.exe /S` with temp `.reg` files in `%SystemRoot%\Temp` (predictable names, world-readable window); `DeleteAllOtherPowerSchemes` deletes every GUID-shaped scheme; `SetDwordAllDescendants` recurses whole subtrees; `.reg` blobs use the `?`-for-`$` substitution hack (`dollarFromQuestion`) that can corrupt unrelated `?` characters.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs:896-907`, `1025-1030`, `654-676`, `623-651`, `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs` (112 KB generated, "do not edit by hand")
- Recommendations: Pre-export affected keys (`reg export`) before import; use randomized temp subdirs with restrictive ACLs; replace the `?`/`$` hack with a real placeholder token.

**Elevated-context identity confusion:**
- Risk: App runs elevated so HKCU = admin hive; `AkariToolState.OpenRealHkcu` guesses the interactive user via the first `explorer.exe` token (`AkariToolState.cs:38-63`). Multi-session/RDP machines can resolve the wrong SID and write another user's hive. The static `Store` handle also keeps `HKCU\Software\AkariTool` open for the process lifetime.
- Files: `src/AppTemplate.App/Tweaks/AkariToolState.cs`
- Recommendations: Resolve the console-session user (`WTSQuerySessionInformation`) rather than first-explorer; open/close keys per operation instead of a static handle.

## Performance Bottlenecks

**Full WMI + registry sweep on every Home/Tweaks navigation:**
- Problem: `TweaksViewModel.OnNavigatedTo` probes 169 rows synchronously; Home re-reads RAM/drive/memory each visit (only OS/CPU/GPU are cached in `SystemInfo`).
- Files: `src/AppTemplate.App/ViewModels/TweaksViewModel.cs:53-81`, `src/AppTemplate.App/Tweaks/SystemInfo.cs:18-92` (cache) vs `94-188` (uncached), `src/AppTemplate.App/ViewModels/HomeViewModel.cs:105-147`
- Cause: No batched/async probe; `ReadRowOptimized` opens one registry key per row.
- Improvement path: Probe off-UI-thread in batches with progress via `IStatusService`; cache + invalidate on apply; virtualize the 169-row lists (currently all materialized in `BuildRowGroups`).

**Unbounded recursive registry walks:**
- Problem: `DisplayDeviceInstanceIds` / `Walk` / `WalkDisplayDevices` / `DescendantKeyPaths` enumerate entire `HKLM\SYSTEM\ControlSet001\Enum` and display-class subtrees key-by-key, catching per-key exceptions.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs:594-651`, `937-960`
- Cause: No depth limit, no caching, exception-driven flow on locked `Properties` children.
- Improvement path: Cache per boot; skip known-locked subkeys by name before opening; prefer `Get-PnpDevice`-equivalent SetupAPI enumeration once.

**Image blackening decodes every file:**
- Problem: `BlackenImagesInFolder` opens + decodes each `.png`/`.bmp` to read dimensions, then re-encodes a black bitmap — CPU and I/O heavy on wallpaper/theme folders.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs:818-854`
- Improvement path: Batch on thread-pool with cancellation; skip files already uniform; report progress.

**schtasks CSV re-parsed per operation:**
- Problem: `DeleteScheduledTasksMatching` / `ChangeScheduledTasks` shell `schtasks /query /fo csv /nh`, split lines with naive `Split("\",\"")`, and shell one `schtasks` per match with infinite `WaitForExit`.
- Files: `src/AppTemplate.App/Tweaks/NativeOps.cs:270-283`, `787-799`
- Improvement path: Parse once with a real CSV reader, batch deletes, add per-process timeout.

## Fragile Areas

**Safe-boot RunOnce + bcdedit choreography:**
- Files: `src/AppTemplate.App/App.xaml.cs:53-94` (headless re-entry), `src/AppTemplate.App/Tweaks/DriverActions.cs:44-84` (`PrepareDdu`/`FinishDdu`), `src/AppTemplate.App/Tweaks/AdvancedActions.cs:37-90`, `src/AppTemplate.App/Tweaks/AdvancedActions.Services.cs:27-35`, `src/AppTemplate.App/ViewModels/AdvancedViewModel.cs:339-360`
- Why fragile: Multi-reboot state machine with no journal — if power is lost between `bcdedit /set safeboot` and the finish half, the machine boots to safe mode with no app continuation; `*ddu`/`*ddumanual` RunOnce names can collide with other tools; `AppInstance` redirection is deliberately skipped for these args (WinAppSDK 2.3.1 unpackaged quirk noted in comment).
- Safe modification: Never reorder the `Has(--flag)` checks; test headless halves with `--ddu-manual` in a VM snapshot; add a boot-marker file + "resume or clear safeboot" recovery dialog.
- Test coverage: None — untestable in testhost, no harness.

**Generated Control Panel data + `?`-substitution:**
- Files: `src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs` (~112 KB, 169 rows, "produced from Invoke-CpTweaks.ps1, do not edit by hand"), `src/AppTemplate.App/Tweaks/ControlPanelRows.cs`, `src/AppTemplate.App/Tweaks/NativeOps.cs:896-900`
- Why fragile: Hand-editing desyncs from the generator script (which is not in the repo); the `dollarFromQuestion` replace corrupts any legitimate `?`; `regedit /S` failures are silent (`RunTool` swallows exit codes except in `RegLoadImportUnload`).
- Safe modification: Regenerate via the documented script only; check `regedit` exit code and surface via InfoBar.
- Test coverage: Zero — no test touches `ControlPanelRows`.

**Vendor-scrape driver installers:**
- Files: `src/AppTemplate.App/Tweaks/DriverActions.cs:88-101` (NVIDIA `AjaxDriverService.php` JSON shape), `152-182` (AMD `minimalsetup*_web.exe` regex on `amd.com` HTML), `18-20` (Intel search URL)
- Why fragile: Any vendor site/API redesign breaks the flow with a bare `InvalidOperationException`; the NVIDIA `win10-win11` heuristic keys off `Environment.OSVersion.Version >= 9.1` which is historically unreliable without a manifest-supported-OS list.
- Safe modification: Isolate URL/regex per vendor with fallback to opening the vendor page (`OpenIntelDriverSearch` pattern); add offline unit tests with captured payloads.
- Test coverage: None.

**Hardcoded system paths and Pray-it-exists tools:**
- Files: `NativeOps.SevenZipExe` (`%ProgramFiles%\7-Zip\7z.exe`), `CompileWithCsc` (`%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` — absent on .NET-8+-only images), `DeleteDirectory(@"C:\NVIDIA")`-style literals in `DriverActions` (133, 181, 210-211), `C:\Windows\Black.jpg` / `start2.bin` / `settings.dat` deletes in `WindowsActions` (192, 212, 228, 237, 333, 810), `WinGetPackages` (`%LOCALAPPDATA%\Microsoft\WinGet\Packages`), `DduSettings.xml` / `DduDir` under `%SystemRoot%\Temp`
- Why fragile: Localized, redirected, or cleaned paths throw or silently no-op; `%SystemDrive%\Program Files (x86)\Microsoft` delete (`WindowsActions.cs:745`) is one typo away from catastrophe.
- Safe modification: Resolve via `Environment.SpecialFolder` / `PackagePath` helpers; guard deletes with parent-path assertions; never expand delete scope without a second reviewer.

**Single-instance + elevation + picker triangle:**
- Files: `src/AppTemplate.App/App.xaml.cs:96-106` (`FindOrRegisterForKey("AkariToolbox")`), `src/AppTemplate.App/app.manifest` (`requireAdministrator`), README "file pickers use `Microsoft.Win32.OpenFileDialog`" note
- Why fragile: Elevated process cannot use WinUI pickers; single-instance key is a fixed string (session collisions on multi-user hosts); `GetWindowHandle`/`XamlRoot` providers assume `MainWindow` exists (headless flows exit before it is built — correct today, brittle to reorder).
- Safe modification: Keep headless checks above `FindOrRegisterForKey`; keep Win32 dialog path; don't normalize the instance key without considering Terminal-Services sessions.

## Scaling Limits

**Single-machine admin tool — no horizontal scale:**
- Current capacity: One Windows 10/11 x64 admin session; 13 pages, 169 Control Panel rows, ~30 runtime downloads; all operations serialized per-page by `IsBusy`.
- Limit: Concurrent applies across pages are uncoordinated (each VM has its own `IsBusy`; two pages can write the same key simultaneously). `ProbeCache` + static `HttpClient` assume one user.
- Scaling path: Not applicable (desktop tool) — but centralize the apply gate into a single app-wide `ITweakRunner` (semaphore + queue + cancel) instead of per-VM flags.

**Log retention:**
- Current capacity: Daily rolling files, ~1 MB cap, newest 10 kept (`README.md` app-notes; `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs:50,150` locked writes).
- Limit: Verbose failure logging (recommended above) will rotate within a single heavy debloat run.
- Scaling path: Raise cap / add per-operation log files for destructive runs.

## Dependencies at Risk

**Microsoft.WindowsAppSDK 2.3.1:**
- Risk: Pinned well behind current 2.x servicing; unpackaged + `requireAdministrator` + `RedirectActivationToAsync` behavior already carries a version-specific workaround comment (`App.xaml.cs:55`). Newer Windows builds may change single-instance/activation semantics.
- Impact: Launch/activation regressions on next Windows feature update.
- Migration plan: Track WindowsAppSDK 2.5+/3.x release notes in a scheduled chore; re-verify `verify-pages.ps1` UIA pass after any bump.

**System.Management 10.0.12 (WMI):**
- Risk: WMI provider stalls are a known hang source; `OPENCODE.md:44-45` already documents dispose-or-deadlock discipline. Package is new (v10 line) with limited bake time.
- Impact: Home page hangs if a provider blocks; `SystemInfo` swallows into `"Unknown"` so failure is silent.
- Migration plan: Add per-query timeouts; consider CIM/`Microsoft.Management.Infrastructure` for new reads.

**.NET 10 SDK pin (`global.json` 10.0.400, `TargetFramework net10.0-windows10.0.26100.0`):**
- Risk: Bleeding-edge SDK + `LangVersion=latest`; CI/dev machines without the exact SDK fail restore. `TreatWarningsAsErrors=false` hides new-analyzer warnings from the new SDK.
- Impact: Build breaks on stale agents; silent warning growth.
- Migration plan: Keep `rollForward: latestMinor` (already set); enable `TreatWarningsAsErrors` for Release; pin CI image to the same SDK.

**CommunityToolkit.Mvvm 8.4.2 / Microsoft.Xaml.Behaviors 3.0.1:**
- Risk: No known CVE, but source-generator behavior (`[ObservableProperty]`, `[RelayCommand]`, partial `OnXChanged`) is load-bearing for every page — a minor bump can rename generated members.
- Impact: Whole-solution build break on bump.
- Migration plan: Bump behind `build-and-run.ps1` + `check-parity.ps1` + `verify-pages.ps1` green gate.

## Missing Critical Features

**No rollback / restore-point automation on destructive pages:**
- Problem: Only Home creates a restore point (`RestorePointActions.CreateRestorePoint` via `HomeViewModel`); Debloat/Services/Defender/Driver pages mutate services, drivers, and security posture with no snapshot, export, or undo.
- Blocks: Safe "try and revert" workflows; support triage after a bad tweak.

**No integrity verification for downloads:**
- Problem: Covered under Security — no hashes, no signatures, no offline mirror. Blocks enterprise/air-gapped use.

**No apply journal:**
- Problem: No record of which tweaks were applied when (only scattered `AkariToolState` ints + `CpRow:` keys). Blocks "what changed my machine?" diagnosis.
- Fix: Append-only JSONL journal in `%LOCALAPPDATA%\AkariToolbox\` (timestamp, action, pre/post values, result).

## Test Coverage Gaps

**Zero coverage for the entire `App` project (the risky 90%):**
- What's not tested: All 13 ViewModels, all `*Actions` classes, `NativeOps`, `TweakAction`, `ControlPanelRows` (+169 generated rows), `AkariToolState`, `SystemInfo`, safe-boot flows, download/install paths.
- Files: Everything under `src/AppTemplate.App/Tweaks/`, `src/AppTemplate.App/ViewModels/`, `src/AppTemplate.App/Helpers/`, `src/AppTemplate.App/Services/LocalizedStrings.cs`
- Risk: The most destructive code (registry deletes, service deletes, driver wipes, BitLocker off, Defender off) ships without any automated check. A typo in a generated `.reg` body or debloat path is caught only by `check-parity.ps1` (string diff vs WPF original) or manual UIA (`verify-pages.ps1` clicks nav items but asserts only content counts, not tweak outcomes).
- Priority: High

**Framework-only test suite gives false confidence:**
- What's not tested: Tests in `src/AppTemplate.Tests/` (10 files: `ConvertersTests`, `NavigationTests`, `LoggingTests`, `ServicesTests`, `SettingsServiceTests`, `ViewModelTests`, `MessagingTests`, `CollectionsTests`, `FileSettingsStorageTests`, `ObjectExtensionsTests`) cover only `src/AppTemplate.Framework/`. UI types cannot instantiate in testhost (`README.md:96-98` — `new Page()` throws `COMException`), so navigation/dialog/file-picker services are tested with fakes.
- Risk: Green `dotnet test` says nothing about the app users run.
- Priority: High — extract pure logic (probe parsing in `ControlPanelRows.ParseProbe`, NVIDIA/AMD URL builders, `HexToBytes`, `MatchIndex`, service-matrix computation in `AdvancedActions.Services.cs`) into testable units with captured fixtures; add snapshot tests over `ControlPanelRows.All` (id uniqueness, tab/section integrity, Optimize/Default bodies parse, probe coverage %).

**No failure-path tests:**
- What's not tested: Offline download failure, missing 7-Zip/DDU/OCCT binaries, locked registry keys, absent `explorer.exe` (SID resolution), `bcdedit` failure, `regedit` non-zero exit. Every one of these is swallowed by `catch {}` today, so there is no oracle for "correct" behavior.
- Priority: Medium — define and test the contract first (throw typed exception vs return result), then wire InfoBar display.

---

*Concerns audit: 2026-10-03*
