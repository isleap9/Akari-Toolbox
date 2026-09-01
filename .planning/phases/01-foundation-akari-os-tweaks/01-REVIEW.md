---
phase: 01-foundation-akari-os-tweaks
reviewed: 2026-09-01T00:00:00Z
depth: standard
files_reviewed: 41
files_reviewed_list:
  - .gitignore
  - AkariToolbox.slnx
  - Directory.Build.props
  - Directory.Packages.props
  - global.json
  - nuget.config
  - src/AkariToolbox.App/AkariToolbox.App.csproj
  - src/AkariToolbox.App/App.xaml.cs
  - src/AkariToolbox.App/AssemblyInfo.cs
  - src/AkariToolbox.App/MainWindow.xaml
  - src/AkariToolbox.App/MainWindow.xaml.cs
  - src/AkariToolbox.App/Models/TweakItem.cs
  - src/AkariToolbox.App/NavigationItem.cs
  - src/AkariToolbox.App/Services/IPostInstallService.cs
  - src/AkariToolbox.App/Services/ITweakCatalog.cs
  - src/AkariToolbox.App/Services/ITweakHandler.cs
  - src/AkariToolbox.App/Services/PostInstallService.cs
  - src/AkariToolbox.App/Services/TweakCatalog.cs
  - src/AkariToolbox.App/Services/TweakHandlerRegistration.cs
  - src/AkariToolbox.App/Services/TweakHandlers/BcdeditDismTweaks.cs
  - src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs
  - src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchA.cs
  - src/AkariToolbox.App/Services/TweakHandlers/RegistryTweaksBatchB.cs
  - src/AkariToolbox.App/Services/TweakHandlers/ServiceBackedTweaks.cs
  - src/AkariToolbox.App/Services/TweakHandlers/WifiTweakHandler.cs
  - src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs
  - src/AkariToolbox.App/ViewModels/HomeViewModel.cs
  - src/AkariToolbox.App/app.manifest
  - src/AkariToolbox.Framework/ServiceCollectionExtensions.Primitives.cs
  - src/AkariToolbox.Framework/ServiceCollectionExtensions.cs
  - src/AkariToolbox.Framework/Services/IFilePickerService.cs
  - src/AkariToolbox.Framework/Services/ILogConsoleService.cs
  - src/AkariToolbox.Framework/Services/IRegistryService.cs
  - src/AkariToolbox.Framework/Services/IScriptRunner.cs
  - src/AkariToolbox.Framework/Services/IWindowsServiceController.cs
  - src/AkariToolbox.Framework/Services/LogConsoleService.cs
  - src/AkariToolbox.Framework/Services/RegistryService.cs
  - src/AkariToolbox.Framework/Services/ScriptRunner.cs
  - src/AkariToolbox.Framework/Services/WindowsServiceController.cs
  - src/AkariToolbox.Tests/LogConsoleServiceTests.cs
  - src/AkariToolbox.Tests/PostInstallIntegrityTests.cs
  - src/AkariToolbox.Tests/ScriptRunnerTests.cs
  - src/AkariToolbox.Tests/TweakCatalogTests.cs
  - src/AkariToolbox.Tests/TweakHandlerOrderingTests.cs
findings:
  critical: 4
  warning: 3
  info: 3
  total: 10
status: issues_found
---

# Phase 01: Code Review Report

**Reviewed:** 2026-09-01T00:00:00Z
**Depth:** standard
**Files Reviewed:** 41
**Status:** issues_found

## Summary

Reviewed the full Phase 1 vertical slice: solution/build plumbing, the 32-tweak
handler layer (`ITweakHandler`/`ITweakCatalog`/`TweakCatalog`), the PostInstall
asset-mirror + SHA256 integrity gate, the Defender two-phase workflow, the
Framework system primitives (`IRegistryService`, `IScriptRunner`,
`IWindowsServiceController`, `ILogConsoleService`), the shell (`MainWindow`),
and the test suite.

The individual registry/service tweak handlers are largely faithful,
well-documented ports with sensible live-read/live-write semantics and good
test coverage of ordering/catalog invariants. However, four issues undermine
this phase's own stated core value ("every tweak... must apply correctly,
report accurate state") and its explicitly called-out highest-severity threat
(executing a corrupted/tampered downloaded binary under admin rights):

1. The per-key mutual exclusion `TweakCatalog` relies on to prevent concurrent
   writes to the same tweak is silently defeated for the Defender tweak,
   because `DefenderTweakHandler.SetState` is fire-and-forget.
2. A race between the ViewModel's async initial-state load and an early user
   toggle can silently revert a tweak the user just applied.
3. The SHA256 integrity gate added to close the phase-plan's T-01-SC BLOCKER
   only covers 2 of the files that are actually executed with elevated/
   TrustedInstaller privileges — `MinSudo.exe` and `PowerRun.exe` themselves
   are never hash-verified before being run.
4. A failed tweak write leaves the toggle UI showing the requested (not
   actual) state, with no revert — directly contradicting "report accurate
   state."

## Critical Issues

### CR-01: Defender's fire-and-forget `SetState` defeats `TweakCatalog`'s per-key serialization

**File:** `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs:80-82`
**Issue:**
`TweakCatalog.SetStateAsync` (src/AkariToolbox.App/Services/TweakCatalog.cs:30-62)
guarantees only one in-flight mutation per key by holding a per-key
`SemaphoreSlim` for the duration of `await Task.Run(() => handler.SetState(enabled))`
(line 56). That guarantee assumes `SetState` blocks until the real work is
done — true for every other handler (they call `.GetAwaiter().GetResult()`
internally), but **not** for Defender:

```csharp
public void SetState(bool disable) => _ = SetDefenderAsync(disable);
```

`SetState` returns immediately after firing `SetDefenderAsync` and discarding
the task, so `Task.Run(() => handler.SetState(enabled))` completes almost
instantly and the semaphore is released while the real disable/enable
workflow (file download, Tamper Protection check, `NoDefender.cab`
install/uninstall via elevated PowerShell, TrustedInstaller service-registry
rewrite) is still running in the background.

If the user toggles "Disable Defender" twice in quick succession (or the
toggle fires twice for any reason), the second `SetStateAsync("defender", …)`
call reads `GetState()` — which only flips to the new value near the very end
of `SetDefenderAsync` (`registry.SetValue(..., DefenderStateKey, ...)` at
line 141 / `DeleteValue` at line 157) — and, seeing the flag not yet updated,
proceeds to invoke `SetDefenderAsync` a **second time concurrently**. This can
launch two concurrent elevated PowerShell installs, two
`DefenderRunAsTrustedInstallerAsync` runs writing the same temp `.bat` file
from two threads, and two `DefenderScheduleCleanup` writes to the same
`AkariDefenderCleanup.bat` — the most system-sensitive tweak in the app has no
real protection against concurrent re-entry despite the catalog's contract.

**Fix:** Make `DefenderTweakHandler.SetState` actually block for the duration
of the operation (matching every other handler's contract), or expose an
async `ITweakHandler` member so `TweakCatalog` can `await` real completion
instead of a fire-and-forget wrapper:

```csharp
public void SetState(bool disable) => SetDefenderAsync(disable).GetAwaiter().GetResult();
```

If the fire-and-forget UX (immediate return, background completion) is
intentional, `DefenderTweakHandler` needs its own internal
re-entrancy guard (e.g. an `Interlocked`/`SemaphoreSlim` field) so the catalog
lock being ineffective doesn't allow overlapping runs.

---

### CR-02: Async initial state load can silently revert a user's just-applied tweak

**File:** `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs:40-51`
**Issue:** In the constructor, for every handler:

```csharp
var item = new TweakItem { Key = handler.Key, ..., IsOn = false };
item.PropertyChanged += OnTweakItemPropertyChanged;   // wired up immediately
Tweaks.Add(item);

_ = TryGetStateAsync(_catalog, _log, handler).ContinueWith(
    task => _dispatcher.RunOnUIThreadAsync(() => item.IsOn = task.Result),
    TaskScheduler.Default);
```

`OnTweakItemPropertyChanged` is subscribed **before** the live-state read
completes, and any change to `item.IsOn` — whether from the user or from this
initial load's continuation — calls `_catalog.SetStateAsync(item.Key, item.IsOn)`
(line 89), which performs a real system write.

Several handlers' `GetState()` spawn external processes synchronously inside
`Task.Run` (e.g. `DepTweakHandler`/`BootMenuTweakHandler` call
`bcdedit /enum {current}` via `RunProcessCaptureOutputAsync(...).GetAwaiter().GetResult()`),
and all 32 handlers' initial reads are kicked off concurrently, competing for
thread-pool threads. This creates a real window where a user can toggle a
switch (e.g. Wifi, the first item, Order 0) before its corresponding
`TryGetStateAsync` continuation has run.

Sequence:
1. User toggles "wifi" on → `OnTweakItemPropertyChanged` fires →
   `catalog.SetStateAsync("wifi", true)` applies the real tweak.
2. The initial-load continuation for "wifi" (still in flight, started before
   the toggle) now resolves with the **stale pre-toggle** value (`false`) and
   sets `item.IsOn = false` on the UI thread.
3. That assignment is a real change (`true` → `false`), so
   `OnTweakItemPropertyChanged` fires again and calls
   `catalog.SetStateAsync("wifi", false)` — silently reverting the tweak the
   user just applied, with the toggle visually flipping back to Off and no
   error shown.

**Fix:** Don't let the initial-load path go through the same
`PropertyChanged` → write-through pipeline as user interaction. Set the
initial value without triggering a write (e.g. subscribe to
`PropertyChanged` only *after* the initial load completes, or set the
backing field directly / use a "loading" guard flag that `OnTweakItemPropertyChanged`
checks before calling `SetStateAsync`):

```csharp
var item = new TweakItem { Key = handler.Key, Title = handler.Title, Description = handler.Description };
Tweaks.Add(item);

_ = TryGetStateAsync(_catalog, _log, handler).ContinueWith(task =>
    _dispatcher.RunOnUIThreadAsync(() =>
    {
        item.IsOn = task.Result;              // set before subscribing
        item.PropertyChanged += OnTweakItemPropertyChanged;
    }),
    TaskScheduler.Default);
```

---

### CR-03: Elevated/TrustedInstaller executables (`MinSudo.exe`, `PowerRun.exe`) are never integrity-verified before execution

**File:** `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs:109-117, 291-313`
**Issue:** The T-01-SC mitigation added in this phase only verifies two
files before use:

```csharp
var noDefenderOk = await postInstall.VerifyFileSha256Async(postInstall.NoDefenderPath, ExpectedNoDefenderCabSha256);
var disableScriptOk = await postInstall.VerifyFileSha256Async(
    Path.Combine(postInstall.LocalRoot, "Defender", "DisableDefender.ps1"), ExpectedDisableDefenderPs1Sha256);
```

But the same downloaded-from-GitHub PostInstall mirror also supplies two
executables that are actually **run with elevated privileges** and are never
hashed:

- `postInstall.MinSudoPath` (`Tweaks/MinSudo.exe`) is launched directly as the
  process in `DefenderRunAsTrustedInstallerAsync` (line 302:
  `FileName = postInstall.MinSudoPath`) with `--TrustedInstaller --Privileged`
  — i.e. it runs arbitrary code as TrustedInstaller.
- `postInstall.PowerRunPath` (`Tweaks/PowerRun.exe`) is embedded dozens of
  times into the generated `AkariDefenderCleanup.bat`
  (`DefenderScheduleCleanup`, lines 184-219) that Windows executes
  automatically on next login via a `RunOnce` registry key, again running as
  whatever privilege that `.bat` acquires.

Both binaries are downloaded over plain `GetByteArrayAsync` in
`PostInstallService.DownloadFileAsync` with no integrity check at all
(`IsFullyInstalled`/`EnsurePostInstallAsync` only check file *presence*, not
hash). This leaves the exact threat CLAUDE.md calls the app's single
highest-severity failure mode — "executing a corrupted or tampered
downloaded binary... under admin rights" — unmitigated for the two files that
are actually executed with elevated/TrustedInstaller rights.

**Fix:** Add SHA256 pins for `MinSudo.exe` and `PowerRun.exe` and verify them
(same pattern as the two already-covered files) before
`DefenderRunAsTrustedInstallerAsync`/`DefenderScheduleCleanup` reference them,
e.g.:

```csharp
var minSudoOk = await postInstall.VerifyFileSha256Async(postInstall.MinSudoPath, ExpectedMinSudoSha256);
var powerRunOk = await postInstall.VerifyFileSha256Async(postInstall.PowerRunPath, ExpectedPowerRunSha256);
if (!minSudoOk || !powerRunOk) { log.Log("[DEFENDER] ERROR: Integrity check failed..."); return; }
```

---

### CR-04: Failed tweak writes leave the toggle showing the requested state instead of the real state

**File:** `src/AkariToolbox.App/ViewModels/AkariOSTweaksViewModel.cs:89-97`
**Issue:**

```csharp
_ = _catalog.SetStateAsync(item.Key, item.IsOn).ContinueWith(
    task =>
    {
        if (task.IsFaulted)
        {
            _log.Log($"[TWEAK ERROR] {item.Key}: {task.Exception?.GetBaseException().Message}");
        }
    },
    TaskScheduler.Default);
```

`item.IsOn` is set optimistically by the two-way-bound `ToggleSwitch` before
the write is attempted. When `SetStateAsync` throws — e.g.
`StartMenuTweakHandler`/`TransparencyTweakHandler` call
`registry.OpenRealUserHive(...)`, which by design (D-14, documented in
`IRegistryService.OpenRealUserHive`) hard-throws `InvalidOperationException`
when `explorer.exe` isn't running — the failure is only logged; `item.IsOn`
is never set back to the actual (unchanged) live value. The toggle keeps
showing the state the user requested even though nothing was applied (or a
registry write partially completed before the throw), directly contradicting
this project's stated Core Value: "must apply correctly, report accurate
state."

**Fix:** On fault, re-read the real state and reflect it in the UI (and avoid
re-triggering another write-through via `OnTweakItemPropertyChanged`):

```csharp
_ = _catalog.SetStateAsync(item.Key, item.IsOn).ContinueWith(async task =>
{
    if (task.IsFaulted)
    {
        _log.Log($"[TWEAK ERROR] {item.Key}: {task.Exception?.GetBaseException().Message}");
        var real = await AkariOSTweaksViewModel.TryGetStateAsync(_catalog, _log, handlerFor(item.Key));
        await _dispatcher.RunOnUIThreadAsync(() =>
        {
            item.PropertyChanged -= OnTweakItemPropertyChanged;
            item.IsOn = real;
            item.PropertyChanged += OnTweakItemPropertyChanged;
        });
    }
},
TaskScheduler.Default);
```

## Warnings

### WR-01: `OpenRealUserHive` leaks a process token handle (and the `Process` object) on every call

**File:** `src/AkariToolbox.Framework/Services/RegistryService.cs:36-50`
**Issue:**

```csharp
public RegistryKey OpenRealUserHive(string subKeyPath)
{
    var explorer = Process.GetProcessesByName("explorer").FirstOrDefault()
        ?? throw new InvalidOperationException("explorer.exe not found.");

    if (!OpenProcessToken(explorer.Handle, 8, out var token))
    {
        throw new InvalidOperationException("Could not open explorer process token.");
    }

    using var identity = new WindowsIdentity(token);
    ...
}
```

`OpenProcessToken` returns a native handle (`token`) that is never closed
(`CloseHandle`) — `WindowsIdentity`'s `IntPtr` constructor duplicates the
token internally and disposing `identity` does not close the caller's
original handle. The `Process` object from `GetProcessesByName` is also never
disposed. This is called on every `StartMenuTweakHandler`/
`TransparencyTweakHandler` read and write, so repeated toggling accumulates
OS handle leaks for the lifetime of the (long-running, elevated) process.

**Fix:**

```csharp
using var explorer = Process.GetProcessesByName("explorer").FirstOrDefault()
    ?? throw new InvalidOperationException("explorer.exe not found.");

if (!OpenProcessToken(explorer.Handle, 8, out var token))
{
    throw new InvalidOperationException("Could not open explorer process token.");
}

try
{
    using var identity = new WindowsIdentity(token);
    ...
}
finally
{
    CloseHandle(token); // P/Invoke kernel32!CloseHandle
}
```

### WR-02: Defender's pinned SHA256 hashes are explicitly noted as unverified against a real machine

**File:** `src/AkariToolbox.App/Services/TweakHandlers/DefenderTweakHandler.cs:24-37`
**Issue:** The comment above the two `Expected*Sha256` constants states "no
live Windows test machine was available during this automated implementation
pass, so this pin should be re-confirmed against the actual local files
during the Task 2 human real-machine check." If either constant is wrong, the
new integrity gate (CR-03's sibling, already implemented for these two files)
will fail closed for every user, every time — the Disable Defender feature
would be permanently broken until someone re-pins the hash, with only a
generic "Integrity check failed" log line to go on.

**Fix:** Confirm both hashes against the actual downloaded bytes before
shipping (e.g. `Get-FileHash -Algorithm SHA256` against a fresh
`C:\PostInstall` mirror), and add a regression test/CI step that re-fetches
and re-hashes the pinned URLs periodically so a silent upstream file change
doesn't strand this gate.

### WR-03: Leftover "TEMPORARY" debug smoke-test button shipped in the production shell

**File:** `src/AkariToolbox.App/MainWindow.xaml:93-98`, `src/AkariToolbox.App/MainWindow.xaml.cs:107-126`
**Issue:** The shell's log-dock header includes a real, user-visible button:

```xml
<!-- TEMPORARY: D-13 debug smoke test — remove once Phase 4 wires a real picker consumer -->
<Button Grid.Column="1" x:Name="PickerSmokeTestButton"
        Content="Picker smoke test (temporary, remove in Phase 4)"
        Click="OnPickerSmokeTestClick" />
```

with a full handler method in the code-behind. This is debug scaffolding
left in a file included in this phase's completed deliverable, visible to
every end user of a system-tweaking tool that runs elevated.

**Fix:** Remove `PickerSmokeTestButton`/`OnPickerSmokeTestClick` now, or gate
it behind a `#if DEBUG` compilation symbol so it cannot ship in a Release
build even if forgotten again.

## Info

### IN-01: "Akari OS Tweaks" and "Settings" nav items share the same glyph

**File:** `src/AkariToolbox.App/MainWindow.xaml.cs:81, 91`
**Issue:** `NavItems` uses `"\uE713"` for "Akari OS Tweaks" and
`FooterNavItems` uses the same `"\uE713"` for "Settings". `\uE713` is the
Segoe Fluent Icons "Setting" (gear) glyph — using it for both items looks
like a copy/paste oversight and will render the same icon for two different
destinations.
**Fix:** Give "Akari OS Tweaks" a distinct glyph (e.g. a wrench/tool icon
such as `\uE90F` or `\uEC7A`).

### IN-02: Every Home dashboard card has an empty icon glyph

**File:** `src/AkariToolbox.App/ViewModels/HomeViewModel.cs:35-39`
**Issue:** All five `HomeCard` entries set `Glyph = ""`, so none of the Home
page's destination cards render an icon, despite `HomeCard.Glyph` being
documented as "Segoe Fluent Icons glyph" (line 13).
**Fix:** Assign a real glyph per card (Gaming Tweaks, Akari OS Tweaks,
Debloat, Downloads, Misc).

### IN-03: `LoadAppIcon()` returns `null!` for a non-nullable `ImageSource` property

**File:** `src/AkariToolbox.App/MainWindow.xaml.cs:71-75`
**Issue:**

```csharp
private static ImageSource LoadAppIcon()
{
    var path = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png");
    return File.Exists(path) ? new BitmapImage(new Uri(path)) : null!;
}
```

Using `null!` to satisfy a non-nullable return type silences the compiler's
null-safety analysis instead of modeling the missing-icon case honestly.
**Fix:** Make `AppIconSource` (and this method's return type) `ImageSource?`,
matching the actual possible value.

---

_Reviewed: 2026-09-01T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
