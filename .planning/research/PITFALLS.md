# Pitfalls Research

**Domain:** WPF-to-WinUI3 port of an elevated Windows system-tweak/debloat desktop utility (unpackaged, self-contained, distributed as a portable exe via GitHub releases)
**Researched:** 2026-08-31
**Confidence:** MEDIUM-HIGH (critical items corroborated by official Microsoft repos/docs and multiple independent sources; AV-heuristic specifics are inherently LOW-confidence/anecdotal by nature of the topic)

## Critical Pitfalls

### Pitfall 1: File/folder pickers crash under `requireAdministrator`

**What goes wrong:**
`Windows.Storage.Pickers` (`FileOpenPicker`, `FileSavePicker`, `FolderPicker`) throw a `COMException` (`HRESULT E_FAIL`, "an error occurred while communicating with the picker") the moment they're invoked in a process running elevated. This is a documented WinRT activation limitation, not a bug you can code around at the call site — WinRT COM activation for these specific APIs does not work reliably from an elevated integrity level. It reproduces consistently and has open issues against `microsoft/WindowsAppSDK`.

**Why it happens:**
Akari Toolbox runs elevated by design (`requireAdministrator`) for its entire lifetime — there's no unelevated mode to fall back to. The inherited `WinUI-3-MVVM-Framework` template ships an `IFilePickerService` built on the standard `Windows.Storage.Pickers` API, which was designed and tested for the framework's default (non-elevated) app manifest. The predecessor WPF app used WPF's own `Microsoft.Win32.OpenFileDialog`/`SaveFileDialog` (raw Win32 common dialogs), which have no such restriction — so this bug is invisible in the WPF codebase and only surfaces after the port, likely on whatever page first calls a file/folder picker (Downloads page "choose install location," Misc page, or any future settings import/export).

**How to avoid:**
Do not reuse the framework's default `IFilePickerService` implementation as-is. Either:
1. Replace its picker implementation with `Microsoft.Windows.Storage.Pickers` (the Windows App SDK's newer `WindowId`-based pickers, purpose-built to work in elevated/unpackaged desktop apps), or
2. Fall back to raw Win32 common item dialogs via CsWin32 P/Invoke (`IFileOpenDialog`/`IFileSaveDialog`), the same approach WPF used under the hood.
Decide this during the framework-adaptation/elevation phase, before any page that needs file/folder selection is built — not after a crash report comes in.

**Warning signs:**
- Any `IFilePickerService` call throws `System.Runtime.InteropServices.COMException` with `HRESULT: 0x80004005 (E_FAIL)`.
- Works fine when the app is run without elevation but breaks only when launched elevated (easy to miss if a dev habitually runs from an already-elevated terminal).

**Phase to address:**
Framework adaptation / elevation-enablement phase (early — before any picker-dependent page is ported), with explicit verification in the Downloads page phase.

---

### Pitfall 2: Two-phase Disable Defender workflow is not optional — it's forced by Tamper Protection

**What goes wrong:**
Since Windows 10 1903+, Microsoft Defender's **Tamper Protection** blocks registry-based and Group-Policy-based attempts to disable real-time protection (`DisableAntiSpyware`, `DisableRealtimeMonitoring`, etc.) via a kernel-mode filter driver (`WdFilter.sys`) that intercepts writes to Defender's protected registry keys — even from a process running as SYSTEM/TrustedInstaller. A naive "toggle disables Defender" implementation will appear to succeed (the registry write doesn't error) but silently have no effect, because the driver filters the write, or Defender's tamper-resistant state simply overrides it. Tamper Protection itself cannot be toggled headlessly — it can only be turned off through the Windows Security UI (or centrally via Intune/MDM on managed devices), never by a desktop app's own registry/API call.

**Why it happens:**
This is precisely why the predecessor app already implements a "two-phase Disable Defender workflow" (per PROJECT.md) — phase one almost certainly walks the user to the Windows Security UI to turn off Tamper Protection manually, phase two then applies the registry/policy changes that actually take effect once Tamper Protection is off. Anyone re-implementing this from scratch (or "simplifying" it during the port) without understanding *why* it's two phases will collapse it back into a single registry write that silently no-ops on any machine with Tamper Protection on (the Windows 10/11 default for consumer installs).

**How to avoid:**
- Port the two-phase flow as-is; don't "clean up" it into a single toggle.
- After applying the Defender-disable registry keys, **read back and verify actual effective state** (query the real Defender status via `Get-MpComputerStatus`/WMI, not just "the registry key I wrote"), and surface an explicit "Tamper Protection is still on — open Windows Security to disable it" message rather than reporting success.
- Treat Tamper Protection state as a precondition to check before phase two, not an assumption.

**Warning signs:**
- Registry write to Defender keys "succeeds" (no exception) but Defender's actual protection state (Windows Security app, `Get-MpComputerStatus`) doesn't change.
- User reports the toggle "doesn't do anything" after a Windows feature update (Tamper Protection defaults can be re-enabled by major updates).

**Phase to address:**
AkariOS Tweaks page phase (where the Disable Defender toggle lives) — needs its own explicit state-verification sub-task, not just a UI port of the existing two screens.

---

### Pitfall 3: Registry/service tweaks with no backup, no revert, no restore point

**What goes wrong:**
A tweak toggle that writes a registry value or reconfigures a service with no recorded "previous value" is a one-way door: users who regret a change (or hit an unintended side effect — a disabled service breaking Bluetooth, a debloat script removing an app they wanted) have no way back except reinstalling Windows or hand-editing the registry from memory. This is the single most common category of complaint against tools in this exact space (WinUtil, Optimizer, Win11Debloat, etc.) — "the toggle doesn't actually toggle off" or "I disabled X and now Y is broken and I can't undo it."

**Why it happens:**
Toggle-switch UIs invite an "apply forward" mental model (set value A when on, value B when off) that's easy to implement and looks correct in a demo, but doesn't capture what the value *was* before the tool touched it. If the user's system already had a non-default value (e.g., a service already disabled by another tool, or a registry key that doesn't exist at all pre-tweak, where "off" should mean "delete the key," not "set it to some assumed default"), a hardcoded on/off pair silently corrupts state instead of reverting it.

**How to avoid:**
- Every tweak/service/registry mutation should read-and-record the current value (or "key/subkey absent") immediately before writing, so "revert" restores the actual prior state rather than a guessed default.
- Offer (or automatically trigger) a System Restore point before a batch of destructive changes (the Debloat page's PowerShell-backed removals in particular), matching the precedent set by comparable tools (e.g., WinUtil creates a restore point before applying tweaks).
- For toggle-style tweaks specifically, the "off" state must be defined per-tweak as either "restore recorded prior value" or "restore documented Windows default," not a blanket assumption — this needs to be explicit in the `TweakService` port, not implicit.
- State feedback (PROJECT.md explicitly requires "instant status feedback") must read the *actual current system state* on toggle-switch load/refresh, not just reflect whatever the app last set — otherwise the UI lies about state after an out-of-band change (Windows Update, another tool, manual edit).

**Warning signs:**
- Toggle a tweak on, then off, then inspect the registry/service directly — does it match the pre-tweak state, or a hardcoded default?
- App restart shows a toggle in the wrong position because the "read current state" logic doesn't match the "apply" logic 1:1.
- No restore point is created before the Debloat page runs a batch of PowerShell removal scripts.

**Phase to address:**
`TweakService`/`ITweakService` port phase and Debloat page phase — needs an explicit "read actual state on load + record prior value before mutating" pattern baked into the service layer, not left to each toggle's individual implementation.

---

### Pitfall 4: PowerShell scripts inherit elevation automatically, but silently swallow output/errors if launched wrong

**What goes wrong:**
Two distinct but related mistakes: (1) developers add `-Verb runas` or a fresh UAC elevation request when launching PowerShell from an already-elevated parent process — this is unnecessary (a child process of an elevated process is elevated by default) and either throws or produces a redundant/confusing second UAC prompt if attempted with `UseShellExecute = true`; (2) once launched correctly with `UseShellExecute = false`, `RedirectStandardOutput`/`RedirectStandardError` must both be set and drained (async or via `WaitForExit` + read), or long-running debloat scripts hang once the OS output buffer fills, or errors from a failed script are lost entirely and the app reports "success" for a script that actually failed partway through.

**Why it happens:**
The predecessor's `ToolService` already solved this (scripts are extracted from embedded resources and run via `Process.Start`), so the risk during the port isn't "getting it right from zero" — it's regressing this behavior while restructuring `DebloatPage.xaml.cs` logic into a proper ViewModel/service (per the explicit architecture-debt goal in PROJECT.md). Moving code without preserving the exact `ProcessStartInfo` configuration (`UseShellExecute = false`, `CreateNoWindow = true`, `RedirectStandardOutput/Error = true`, `-ExecutionPolicy Bypass -NoProfile -File <path>`) is an easy regression.

**How to avoid:**
- Preserve (don't rewrite from memory) the predecessor's exact `ProcessStartInfo` flags when porting `ToolService`.
- Always pass `-NoProfile` (avoids user PowerShell profile scripts interfering) and `-ExecutionPolicy Bypass` scoped to the single invocation (not a system-wide policy change).
- Read stdout/stderr asynchronously (`OutputDataReceived`/`ErrorDataReceived` event handlers + `BeginOutputReadLine()`) rather than a blocking synchronous `ReadToEnd()` after `WaitForExit()`, which can deadlock on scripts producing enough output to fill the pipe buffer.
- Check and surface the actual exit code, not just "process exited without throwing."
- Because the whole app is already elevated, never call `Start-Process -Verb RunAs` or set `UseShellExecute = true` with `Verb = "runas"` for these child scripts — it's redundant and can break silent/scripted execution.

**Warning signs:**
- Debloat action reports "done" instantly for a script that should take several seconds — output wasn't actually awaited.
- App hangs indefinitely on a specific debloat script under certain conditions (classic pipe-buffer deadlock symptom).
- A second UAC prompt appears mid-run even though the app itself is already elevated.

**Phase to address:**
Debloat page / `ToolService` port phase — verify this explicitly rather than assuming "it's a straight copy so it's fine."

---

### Pitfall 5: Cross-thread UI updates crash WinUI 3 apps that WPF tolerated

**What goes wrong:**
WPF's dispatcher/binding stack is comparatively forgiving of near-UI-thread updates in some scenarios; WinUI 3's underlying COM/WinRT plumbing is not. Raising `PropertyChanged` or otherwise touching XAML-bound state from a background thread (e.g., inside a `Process` output-received event handler, a `Task.Run` inside a debloat/tweak service call, or a PostInstall asset-download progress callback) throws `System.Runtime.InteropServices.COMException: The application called an interface that was marshalled for a different thread.` This is a very common, well-documented WinUI 3 migration crash.

**Why it happens:**
The exact places this project is most likely to hit it are: `PostInstallService`'s asset download/mirroring progress reporting, `ToolService`'s PowerShell stdout/stderr event handlers feeding "instant status feedback" back to a toggle's bound state, and any async debloat/tweak operation that updates an `ObservableObject` property from a background continuation instead of the UI thread.

**How to avoid:**
- Every background callback (process output events, `HttpClient` download progress, `Task.Run` continuations) that touches a ViewModel property must marshal back via `DispatcherQueue.TryEnqueue()` before mutating bound state — audit this explicitly for every service that reports async progress.
- `CommunityToolkit.Mvvm`'s `IAsyncRelayCommand`/`[RelayCommand]`-generated async commands still run their body on the calling thread's continuation context unless explicitly hopped — don't assume the source generator handles thread marshaling for you.
- Centralize this in the framework layer (a `IDispatcherService` or similar wrapper) rather than sprinkling raw `DispatcherQueue.TryEnqueue` calls through every ViewModel/service — check whether the `WinUI-3-MVVM-Framework` template already provides this abstraction and use it consistently.

**Warning signs:**
- Intermittent crashes only under real usage (a script that takes long enough to produce async output) that never reproduce when single-stepping in the debugger.
- Crash message specifically containing "marshalled for a different thread."

**Phase to address:**
Framework adaptation phase (establish the pattern/service once) plus explicit verification in Debloat and Downloads page phases (the two most async-heavy pages).

---

### Pitfall 6: Distribution as an unsigned, portable exe will get flagged — plan for it, don't fight it late

**What goes wrong:**
An unsigned, self-contained, single-file exe that (a) is freshly published with low reputation, (b) requires admin elevation, (c) contains embedded PowerShell scripts as resources that get extracted and executed at runtime, and (d) modifies registry/services/Defender settings ticks essentially every heuristic-AV and SmartScreen "this looks like malware" signal simultaneously. This is explicitly expected per the project brief, but the failure mode to avoid is treating it as purely a "user education" problem after release rather than an engineering/distribution problem addressed up front.

**Why it happens:**
SmartScreen's reputation system flags rare/unsigned binaries by default regardless of actual behavior. Antivirus heuristic/ML engines (the `!ml` suffix seen in many false-positive reports) specifically target the pattern of "app extracts and runs PowerShell/scripts at runtime" and "app touches Defender/registry/services," because that pattern is genuinely indistinguishable from real malware without a trust signal like code signing or an established publisher reputation. Self-contained single-file publish (`PublishSingleFile`) also self-extracts to a temp directory at first launch, which independently triggers "self-extracting dropper" heuristics on some engines.

**How to avoid:**
- Budget for code signing (even a cheap/OV certificate, or Microsoft Trusted Signing) as a release-readiness item, not a nice-to-have — it's the single highest-leverage mitigation for both SmartScreen and generic AV reputation flags.
- Document expected AV/SmartScreen warnings prominently in the release notes/README (what the predecessor presumably already does) so it's a known quantity, not a support fire per release.
- Where feasible, submit the built exe to Microsoft Defender/major AV vendors' false-positive submission portals as part of the release checklist for major versions — this is a real, if slow, mitigation.
- Consider whether the embedded PowerShell scripts truly need to be extracted-and-run-from-disk (a more suspicious pattern) vs. executed via an in-process PowerShell hosting API (`System.Management.Automation`) where feasible — though this is a bigger architectural change and may not be worth it for a v1 parity port.
- Do not attempt to "hide" the elevation/Defender-touching behavior from AV (obfuscation, packing) — that reliably makes heuristic detection worse, not better.

**Warning signs:**
- GitHub release download triggers a SmartScreen "Windows protected your PC" prompt (expected — track whether it clears with reputation over time or gets worse).
- Issue reports of "my antivirus deleted the exe" after each release (expect this to spike right after any code change, since reputation is partly tied to the exact binary hash).

**Phase to address:**
Release/distribution phase (near the end of the roadmap) — but the "don't obfuscate, don't hide behavior" principle should be a standing constraint applied during every phase that touches Defender/registry/PowerShell execution.

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|-----------------|------------------|
| Copy predecessor's `TweakService` logic verbatim without adding prior-state recording | Faster port, matches v1 parity goal | No real revert capability; "off" toggles can corrupt state that was already non-default | Never — this is core to "safely revertible" per PROJECT.md's Core Value |
| Skip `Microsoft.Windows.Storage.Pickers`/CsWin32 fallback and ship with default `Windows.Storage.Pickers` | Zero extra work, framework default "just works" in dev (if dev runs unelevated) | Hard crash for every real user (app is always elevated) the first time a picker opens | Never |
| Leave PowerShell scripts as loose extracted files instead of validating them post-extraction (hash check) | Simpler `ToolService` port | Tampering/corruption of extracted scripts goes undetected, and it's one more thing AV heuristics flag (arbitrary unsigned script written to disk and executed) | Acceptable for v1 if scripts are still embedded resources under the app's control (low realistic risk) and time doesn't allow it; revisit before scaling to the ~110-script "Ultimate" v2 collection |
| Fire-and-forget `Task.Run` for tweak/debloat actions without marshaling completion back to the UI thread | Looks fine in quick manual testing | Intermittent `COMException` crashes under real timing (see Pitfall 5) | Never |
| Treat "registry write succeeded" as "tweak applied" for Defender-related toggles | Simple, matches naive on/off toggle UX | Silently no-ops under Tamper Protection, misleading the user that the tweak worked (see Pitfall 2) | Never for Defender-related tweaks specifically |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|-------------------|
| Windows Registry (`Microsoft.Win32.Registry`) | Assuming a key/value always exists and using a hardcoded "default" for revert | Read-and-record actual prior state (including "key/value absent") before every mutation |
| Windows Services (`ServiceController`) | Disabling a service without checking dependent services or whether it's already in the desired state, causing exceptions or breaking dependents | Query current `StartType`/`Status` first, check `ServicesDependedOn`, and make the operation idempotent |
| Windows Defender / Tamper Protection | Writing Defender policy registry keys and reporting success based on the write not throwing | Verify actual effective state (`Get-MpComputerStatus` or equivalent WMI/API) after writing, and detect Tamper Protection blocking the change |
| PowerShell (`Process.Start powershell.exe`) | Using `UseShellExecute = true` / `Verb = runas` from an already-elevated parent, or not draining stdout/stderr | `UseShellExecute = false`, redirect + async-read both streams, `-NoProfile -ExecutionPolicy Bypass -File` |
| GitHub (PostInstall asset mirroring) | Assuming the GitHub download always succeeds, or downloading synchronously and blocking the UI thread during a ~30MB fetch | Async download with cancellation + progress reported back to the UI thread via dispatcher marshaling; handle rate-limit/network failure gracefully (self-healing per PROJECT.md, but "self-healing" needs an explicit retry/backoff, not just an unhandled-exception log) |
| Windows.Storage.Pickers (file/folder pickers) | Reusing the framework's default implementation unchanged in an elevated app | Swap for `Microsoft.Windows.Storage.Pickers` or Win32 CsWin32 dialogs (see Pitfall 1) |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|-----------------|
| Synchronous registry/service reads on the UI thread for "instant status feedback" across dozens of toggles | Page (AkariOS Tweaks with 32 toggles, Debloat with 28 actions) feels laggy/frozen on navigation while every toggle's current state is queried | Batch/parallelize state reads on load, or lazy-load state per-toggle as it scrolls into view; keep registry/service queries off the UI thread even though they're normally fast, since 32+ sequential calls compound | Noticeable at 30+ toggles per page — exactly this project's scale on the Tweaks and Debloat pages |
| Blocking synchronous `Process.WaitForExit()` without redirected/drained output on the UI thread for debloat scripts | App appears hung during a debloat action that produces enough output to fill the OS pipe buffer | Always run process execution + output draining off the UI thread, async end-to-end | Any script producing more output than the pipe buffer (varies, but common with verbose uninstall/removal scripts) |
| Re-downloading/re-verifying the full ~30MB PostInstall asset set on every app launch instead of only "when missing" | Slow startup, unnecessary network/GitHub API usage, possible rate-limiting on frequent launches | Check for `C:\PostInstall\` presence/completeness first (matches PROJECT.md's stated self-healing "mirrors from GitHub when missing" behavior) and skip network entirely when already present | Noticeable immediately if the "when missing" check is implemented as "always" by mistake |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| Downloading PostInstall assets over plain HTTP or without verifying they came from the expected GitHub repo/release | Supply-chain risk: a compromised or spoofed source could plant malicious content in `C:\PostInstall\`, which then gets referenced/run as trusted playbooks/drivers | Use HTTPS (default for GitHub), pin to the specific repo, and ideally verify checksums for downloaded assets, same as the "Ultimate" v2 tool list's stated SHA256-verification practice — extend that discipline to PostInstall assets too |
| Embedding PowerShell scripts as resources with no integrity check before execution | If the app's own files are tampered with post-build (or a future update mechanism is compromised), scripts run with full elevated privileges with no detection | At minimum, keep scripts under the app's own signed/verified binary rather than as loose sidecar files; consider a build-time hash manifest checked at runtime if resourcing allows |
| Running arbitrary "quick-access tool grid" third-party utilities (NVIDIA/AMD/third-party tools on Gaming Tweaks page) without provenance verification | Launching an unverified third-party exe from an elevated context is a much larger attack surface than the app's own tweaks | Only bundle/link tools with verified checksums/publishers (as the v2 "Ultimate" collection already plans to do) — apply the same standard even for v1's smaller tool grid |
| Treating "the app is already elevated, so everything inside it is trusted" | An elevated app is a high-value target; any injectable behavior (e.g., a debloat script path built from unsanitized input, or a downloaded asset path used unsafely) becomes a privilege-escalation-adjacent bug | Keep the elevated attack surface as small and well-audited as possible; avoid ever shelling out with unsanitized/user-influenced strings even though the "user" here is trusted (defense in depth against bugs, not just malice) |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-------------------|
| Toggle switch shows "on" from the app's last-known state, not the system's actual current state | User believes a tweak is applied when Windows Update, another tool, or manual reset silently reverted it | Re-query actual state every time a page/toggle is displayed, not just after the user flips it |
| No indication of *why* the Defender toggle needs a two-phase flow (Tamper Protection) | User perceives the second step as buggy/redundant rather than a genuine Windows security requirement | Explain the Tamper Protection dependency in-app at the point of the second phase, not just silently gating on it |
| SmartScreen/AV warning on first launch with no in-app or release-note context | Users assume the app really is malware and abandon it (support burden, reputation damage) | Proactive messaging in release notes/README about expected AV behavior, as discussed in Pitfall 6 |
| Debloat/tweak actions give no progress feedback during multi-second PowerShell execution | User double-clicks/re-triggers the action, or assumes it's frozen | Show explicit in-progress state (spinner/status text) driven by the async process lifecycle, disable the control until completion |

## "Looks Done But Isn't" Checklist

- [ ] **File/folder pickers:** Looks done in the designer/unelevated dev run — verify by launching the actual `requireAdministrator` exe (not from an already-elevated dev terminal, which masks the difference) and exercising every picker-using page.
- [ ] **Defender toggle:** Looks done when the registry write doesn't throw — verify by checking actual Defender status (`Get-MpComputerStatus`) with Tamper Protection both on and off, on a real Windows 10 and Windows 11 machine.
- [ ] **Tweak "off" state:** Looks done when toggling off sets *some* value — verify the exact bytes/type written match Windows' actual documented default (or the pre-tweak recorded value), not an assumed default.
- [ ] **PowerShell debloat actions:** Looks done when the process exits without an unhandled exception — verify the exit code is checked and stderr is actually inspected for script-level failures, not just process-level ones.
- [ ] **Elevation manifest:** Looks done when `requireAdministrator` is added to `app.manifest` — verify the *unpackaged self-contained* build (the actual published artifact, `PublishSingleFile`) still honors it, since some publish/deployment configurations have had reported issues distinct from a normal `dotnet run`.
- [ ] **Async progress/status feedback:** Looks done in a quick manual click-through — verify with a debloat script or download that's slow enough to guarantee the async callback lands mid-interaction, to surface any cross-thread crash.
- [ ] **PostInstall self-healing:** Looks done when it downloads assets successfully once — verify the "no-op when files already exist" path and a network-failure path (partial download, GitHub rate limit) both degrade gracefully instead of throwing unhandled.

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|----------------|------------------|
| Picker crash discovered late (post-port, all pages built on default `IFilePickerService`) | MEDIUM | Swap the framework's picker service implementation for `Microsoft.Windows.Storage.Pickers`/CsWin32 behind the existing `IFilePickerService` interface — contained to one service if the interface boundary was respected |
| Tweak toggles found to have no real revert capability after several tweaks are already ported | MEDIUM-HIGH | Retrofit a "read current state before mutate" wrapper in `TweakService`; requires re-auditing every already-ported tweak method, not just new ones |
| Cross-thread crash shipped and reported post-release | LOW-MEDIUM | Usually a localized fix (wrap the specific callback in `DispatcherQueue.TryEnqueue`); establish the pattern as a checklist item to prevent recurrence elsewhere |
| AV false-positive backlash after release | LOW (per-incident) / ongoing | Submit to vendor false-positive portals, update release notes, evaluate code signing if not already budgeted — cost is mostly relationship/reputation management, not code |
| Defender two-phase flow collapsed to one-phase during port, discovered via bug reports | LOW | Restore the second phase/state-verification step; root cause is usually a misunderstanding of Tamper Protection, not a hard technical blocker, so the fix is straightforward once understood |

## Pitfall-to-Phase Mapping

| Pitfall | Prevention Phase | Verification |
|---------|-------------------|----------------|
| File/folder picker crash under elevation | Framework adaptation / elevation-enablement phase | Launch the actual elevated exe and open every picker-using flow (Downloads page, Misc page) |
| Defender two-phase workflow / Tamper Protection | AkariOS Tweaks page phase | Toggle Defender-disable with Tamper Protection both on and off; confirm real state change via `Get-MpComputerStatus`, not just "no exception" |
| No revert/backup for registry & service tweaks | `TweakService` port phase | For a sample of tweaks, toggle on → off and diff actual registry/service state against pre-tweak baseline |
| PowerShell execution regressions during code-behind-to-service refactor | Debloat page / `ToolService` port phase | Run each debloat action to completion, verify exit code + stderr surfaced, confirm no redundant UAC prompt |
| Cross-thread UI crashes | Framework adaptation phase (establish pattern) + Debloat/Downloads page phases (apply it) | Trigger async status updates from real (not instant/mocked) long-running operations to surface marshaling bugs |
| Unsigned exe / AV & SmartScreen flags | Release/distribution phase | Confirm release notes document expected AV behavior; evaluate/budget code signing before first public release |
| PostInstall self-healing correctness | Downloads page phase | Test with assets already present (no-op expected) and assets missing (download expected), plus a simulated network failure |

## Sources

- [How to run WinUI 3.0 unpackaged application elevated/as Administrator? — microsoft/WindowsAppSDK Discussion #3038](https://github.com/microsoft/WindowsAppSDK/discussions/3038) — MEDIUM confidence (official repo discussion)
- [Trying to use a FileOpenPicker while running the app as Administrator will crash the app — microsoft/WindowsAppSDK Issue #2504](https://github.com/microsoft/WindowsAppSDK/issues/2504) — MEDIUM confidence (official repo issue)
- [WinUI FileOpenPicker Throw Exception When Run Application In Administrator — Microsoft Learn Q&A](https://learn.microsoft.com/en-us/answers/questions/1855661/winui-fileopenpicker-throw-exception-when-run-appl) — MEDIUM confidence (Microsoft Learn)
- [Migrate WPF app patterns to WinUI 3 — Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/migrate-to-windows-app-sdk/wpf-patterns-winui3) — MEDIUM confidence (official docs)
- [PowerToys wpf-to-winui3-migration skill — microsoft/PowerToys](https://github.com/microsoft/powertoys/blob/main/.github/skills/wpf-to-winui3-migration/SKILL.md) — MEDIUM confidence (official repo)
- [Run PowerShell as Administrator from C# (UAC, ProcessStartInfo, ClickOnce Workarounds) — IT trip](https://en.ittrip.xyz/windows/powershell/csharp-powershell-admin) — LOW-MEDIUM confidence (community writeup, corroborated by Microsoft Learn Q&A results)
- [Microsoft confirms why Windows Defender can't be disabled via registry — BleepingComputer](https://www.bleepingcomputer.com/news/microsoft/microsoft-confirms-why-windows-defender-can-t-be-disabled-via-registry/) — MEDIUM confidence (corroborated by multiple independent sources incl. Microsoft Q&A)
- [Enable or Disable Tamper Protection using Intune, REGEDIT, UI — TheWindowsClub](https://www.thewindowsclub.com/how-to-enable-tamper-protection-in-windows-10) — LOW-MEDIUM confidence (community, consistent with official framing)
- [Create System Restore Point before applying any changes — ChrisTitusTech/winutil Issue #983](https://github.com/ChrisTitusTech/winutil/issues/983) — MEDIUM confidence (direct comparable-tool precedent)
- [WinUtil Restore Point — Create — Chris Titus Tech docs](https://winutil.christitus.com/code-reference/tweaks/essential-tweaks/restorepoint/) — MEDIUM confidence (comparable tool's own docs)
- [\[WinUI 3\] COMException: interface marshalled for a different thread — microsoft/microsoft-ui-xaml Discussion #8410](https://github.com/microsoft/microsoft-ui-xaml/discussions/8410) — MEDIUM confidence (official repo)
- [COMException: interface marshalled for a different thread — microsoft/microsoft-ui-xaml Issue #9208](https://github.com/microsoft/microsoft-ui-xaml/issues/9208) — MEDIUM confidence (official repo)
- [Distribute an unpackaged WinUI 3 app — Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app) — MEDIUM confidence (official docs)
- [Self contained, WindowsPackageType none, Single file published winui3 app is not launching — microsoft/microsoft-ui-xaml Issue #9758](https://github.com/microsoft/microsoft-ui-xaml/issues/9758) — MEDIUM confidence (official repo, known-issue signal)
- [Mitigating SmartScreen and Defender False Positives for Line-of-Business Apps — buralog](https://buralog.jp/en/defender-smartscreen-falsepositive-en/) — LOW confidence (community blog, directionally consistent with other sources)
- [\[False Positive?\] Windows Defender detects Trojan:Win32/Wacatac.H!ml — anomalyco/opencode Issue #7592](https://github.com/anomalyco/opencode/issues/7592) — LOW-MEDIUM confidence (real-world comparable case, single incident)
- Predecessor codebase context (`ToolService`, `TweakService`, `PostInstallService`, per-page code-behind sizes) — sourced from `.planning/PROJECT.md`, HIGH confidence (project's own documented ground truth)

---
*Pitfalls research for: WPF-to-WinUI3 elevated system-tweak utility (Akari Toolbox)*
*Researched: 2026-08-31*
