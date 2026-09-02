# Phase 4: Downloads & Misc - Pattern Map

**Mapped:** 2026-09-02
**Files analyzed:** 13 (new/modified)
**Analogs found:** 13 / 13

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `src/AkariToolbox.App/Models/AppDefinition.cs` | model | CRUD (static catalog row) | `src/AkariToolbox.App/Models/DebloatAction.cs` | exact |
| `src/AkariToolbox.App/Models/AppItem.cs` | model | CRUD (bindable row) | `src/AkariToolbox.App/Models/DebloatActionItem.cs` | exact |
| `src/AkariToolbox.App/Models/MiscItem.cs` | model | CRUD (bindable row) | `src/AkariToolbox.App/Models/DebloatActionItem.cs` | role-match |
| `src/AkariToolbox.App/Services/IAppCatalog.cs` / `AppCatalog.cs` | service (static catalog) | CRUD | `src/AkariToolbox.App/Services/IDebloatCatalog.cs` / `DebloatCatalog.cs` | exact |
| `src/AkariToolbox.App/Services/IAppInstallerService.cs` / `AppInstallerService.cs` | service (process orchestration) | request-response / event-driven (process exit) | `src/AkariToolbox.Framework/Services/IScriptRunner.cs` (+ predecessor `AppInstallerService.cs`) | role-match |
| `src/AkariToolbox.App/Services/IContextMenuService.cs` / `ContextMenuService.cs` | service (thin registry orchestrator) | CRUD | `src/AkariToolbox.Framework/Services/IRegistryService.cs` (consumer-side, no new analog needed — direct-carry from predecessor `MiscViewModel.cs`) | role-match |
| `src/AkariToolbox.App/Resources/DownloadsScripts/*.ps1` (per-app hardening, D-04) | utility (embedded script) | file-I/O / event-driven | `src/AkariToolbox.App/Resources/DebloatScripts/*.ps1` (pattern only, not read — same `IScriptRunner.RunEmbeddedScriptAsync` consumer) | role-match |
| `src/AkariToolbox.App/ViewModels/DownloadsViewModel.cs` | controller (ViewModel) | request-response + streaming (fire-and-forget async on nav) | `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs` | exact (dispatch shape) + `INavigationAware` from `FrameNavigationService.cs` |
| `src/AkariToolbox.App/ViewModels/MiscViewModel.cs` | controller (ViewModel) | request-response (CRUD-like add/remove) | `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs` | exact (dispatch shape) |
| `src/AkariToolbox.App/Views/DownloadsPage.xaml(.cs)` | component (View) | request-response | predecessor `DownloadsPage.xaml` (UI layout reference only — not read this session; use existing `DebloatPage.xaml`-equivalent ItemsControl/x:Bind wiring in this repo as the WinUI 3 idiom) | role-match |
| `src/AkariToolbox.App/Views/MiscPage.xaml(.cs)` | component (View) | request-response | same as above | role-match |
| `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs` (MODIFIED) | config (DI registration) | — | itself — extend existing method | exact |
| `.../Resources/PostInstallManifest.json` (D-08 SHA256 manifest, embedded) + edit to `PostInstallService.cs` (D-07 gate) | config / service (data) | file-I/O | `src/AkariToolbox.App/Services/PostInstallService.cs` (self) | exact |

## Pattern Assignments

### `src/AkariToolbox.App/Models/AppDefinition.cs` (model, CRUD)

**Analog:** `src/AkariToolbox.App/Models/DebloatAction.cs` (full file read)

**Record shape to copy** (lines 28-36 of analog):
```csharp
public sealed record DebloatAction(
    string Key,
    string Title,
    string Description,
    string Category,
    string RunResourceSuffix,
    string? UndoResourceSuffix,
    bool RequiresConfirmation,
    bool UndoDownloadsUnverifiedBinary = false);
```
**Adapt to:**
```csharp
public sealed record AppDefinition(
    string Name,
    string Description,
    string Category,
    string WingetId,
    string? HardeningResourceSuffix = null); // D-04: null when winget install alone suffices
```
Doc-comment convention: use a `<remarks>` block on the containing catalog class (see `DebloatCatalog.cs` lines 6-21) to record scope/provenance notes (which 15→13 apps were added, why Epic Games/GOG Galaxy were dropped as duplicates per RESEARCH.md Pitfall 4, region defaults for League/Valorant, Escape From Tarkov's D-03 exception).

---

### `src/AkariToolbox.App/Models/AppItem.cs` (model, CRUD)

**Analog:** `src/AkariToolbox.App/Models/DebloatActionItem.cs` (full file read)

Use `ObservableObject` + `[ObservableProperty]` for the mutable field, `init`-only for the rest — but AppItem needs `IsSelected` (multi-select checkbox) instead of `IsRunning`:
```csharp
public sealed partial class AppItem : ObservableObject
{
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Category { get; init; } = "";
    public string WingetId { get; init; } = "";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isInstalling;
}
```
(Mirrors `DebloatActionItem`'s init-only display fields + single `[ObservableProperty]` busy flag pattern, lines 10-37.)

---

### `src/AkariToolbox.App/Models/MiscItem.cs` (model, CRUD)

**Analog:** `src/AkariToolbox.App/Models/DebloatActionItem.cs`

13 rows, no categories needed (predecessor's Misc page is a flat list, not grouped) — simpler than `DebloatActionItem`:
```csharp
public sealed partial class MiscItem : ObservableObject
{
    public string Key { get; init; } = "";       // e.g. "cmd_admin", "take_own", "context_menu_classic"
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public bool RequiresConfirmation { get; init; } // true only for "take_own" per D-11

    [ObservableProperty]
    private bool _isAdded; // drives Add/Remove toggle button text, since these are 2-state not run/undo-with-history
}
```

---

### `src/AkariToolbox.App/Services/IAppCatalog.cs` / `AppCatalog.cs` (service, CRUD static catalog)

**Analog:** `src/AkariToolbox.App/Services/IDebloatCatalog.cs` + `DebloatCatalog.cs` (both full files read)

**Interface pattern** (`IDebloatCatalog.cs` lines 1-9):
```csharp
using AkariToolbox.App.Models;

namespace AkariToolbox.App.Services;

/// <summary>The compiled-in, static N-app/category app-installer catalog.</summary>
public interface IAppCatalog
{
    IReadOnlyList<AppDefinition> Apps { get; }
}
```

**Implementation pattern** — static `IReadOnlyList<T>` field-initializer list grouped by category with `//` comment banners per category (see `DebloatCatalog.cs` lines 22-93 for the exact structural idiom — one array literal, category groups separated by blank line + comment, record-positional-args per row). Port the predecessor's existing 28 apps verbatim, then append the corrected 13 new apps (per RESEARCH.md Pitfall 4/Open Questions: Epic Games and GOG Galaxy dropped as duplicates; Escape From Tarkov flagged as a D-03 exception if kept; League of Legends/Valorant default to `.NA` winget IDs).

---

### `src/AkariToolbox.App/Services/IAppInstallerService.cs` / `AppInstallerService.cs` (service, request-response + event-driven)

**Analog:** `src/AkariToolbox.Framework/Services/IScriptRunner.cs` (full file read) for the "run external process, capture/log, never throw" contract shape; predecessor `AppInstallerService.cs:35-37` (cited in RESEARCH.md Pattern 4, not re-read this session — RESEARCH.md's excerpt is sufficient: `Process.Start(winget...)` then `await proc.WaitForExitAsync()` per app).

**Contract pattern to copy** (`IScriptRunner.cs` lines 8-45 — the "returns exit code, never throws, logs via ILogConsoleService" convention):
```csharp
public interface IAppInstallerService
{
    /// <summary>
    /// Installs the given app via `winget install --id <id> --silent --accept-package-agreements
    /// --accept-source-agreements`, then runs its hardening script (if any) via
    /// IScriptRunner.RunEmbeddedScriptAsync immediately after WaitForExitAsync returns for
    /// that specific app (RESEARCH.md Pattern 4 — sequenced per-app, not batched).
    /// Never throws; returns false and logs via ILogConsoleService on any failure.
    /// </summary>
    Task<bool> InstallAsync(AppDefinition app);
}
```
Constructor-inject `IScriptRunner` (for hardening scripts) + `ILogConsoleService`, following `DebloatViewModel`'s constructor-injection convention (see below). For the raw winget process spawn itself, either call `IScriptRunner.RunProcessAsync("winget", $"install --id {app.WingetId} --silent ...")` directly (reuses the existing primitive, avoiding a second process-spawn abstraction) rather than hand-rolling a new `Process.Start` — this is the RESEARCH.md "Don't Hand-Roll" table's explicit recommendation.

---

### `src/AkariToolbox.App/Services/IContextMenuService.cs` / `ContextMenuService.cs` (service, CRUD registry orchestrator)

**Analog:** `src/AkariToolbox.Framework/Services/IRegistryService.cs` (full file read, consumed not implemented) + predecessor `MiscViewModel.cs:95-346` (verbatim excerpts already captured in RESEARCH.md Code Examples — cited directly there, not re-read this session since RESEARCH.md already extracted every needed line range).

**Interface shape:**
```csharp
public interface IContextMenuService
{
    void Add(string key);
    void Remove(string key);
}
```

**Core CRUD pattern — direct 1:1 port from predecessor, using `IRegistryService` instead of raw `Registry.*`** (RESEARCH.md Code Examples, "port target" block, verified against `IRegistryService.cs` lines 20 and 37-38 for exact method signatures `SetValue(RegistryHive, subKeyPath, valueName, value, kind)` / `DeleteSubKeyTree(RegistryHive, subKeyPath)`):
```csharp
private void AddCmdAdmin()
{
    foreach (var root in new[]
    {
        @"Directory\Shell\OpenElevatedCMD",
        @"Drive\Shell\OpenElevatedCMD",
        @"LibraryFolder\background\Shell\OpenElevatedCMD",
        @"Directory\Background\Shell\OpenElevatedCMD"
    })
    {
        _registry.SetValue(RegistryHive.ClassesRoot, root, "", "Open CMD As Administrator", RegistryValueKind.String);
        _registry.SetValue(RegistryHive.ClassesRoot, root, "Icon", "imageres.dll,-5324", RegistryValueKind.String);
        _registry.SetValue(RegistryHive.ClassesRoot, $@"{root}\Command", "", @"Powershell.exe -windowstyle hidden -Command ""Start-Process cmd.exe -ArgumentList '/s,/k,pushd,%V' -Verb RunAs""", RegistryValueKind.String);
    }
}
```
**Remove pattern** uses `_registry.DeleteSubKeyTree(RegistryHive.ClassesRoot, root)` per entry — never throws on missing (per `IRegistryService.cs` line 29-38 doc comment).

**Special case — "Kill Not Responding" hive quirk (do NOT "fix"):** Add targets `RegistryHive.ClassesRoot`, but Remove targets `RegistryHive.LocalMachine` + path `SOFTWARE\Classes\DesktopBackground\Shell` — port both call sites exactly as documented in RESEARCH.md Pattern 5's "Note" paragraph; this is intentional, not a bug.

**13th entry (Context Menu.ps1 toggle) — direct registry port, NOT `IScriptRunner`:**
```csharp
private void AddClassicContextMenu()
{
    _registry.SetValue(RegistryHive.CurrentUser,
        @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "", RegistryValueKind.String);
    _registry.SetValue(RegistryHive.LocalMachine,
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoCustomizeThisFolder", 1, RegistryValueKind.DWord);
    _registry.DeleteSubKeyTree(RegistryHive.ClassesRoot, @"Folder\shell\pintohome");
    // ... remaining ~8 operations per RESEARCH.md Pattern 6 / Code Examples
}
```
Map "Clean/Recommended" branch → `Add`, "Default" branch → `Remove` (D-10's Claude's-discretion mapping, RESEARCH.md Pattern 6 footer).

---

### `src/AkariToolbox.App/ViewModels/DownloadsViewModel.cs` (controller, request-response + fire-and-forget streaming)

**Analog:** `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs` (full file read) for constructor-injection + `[RelayCommand]` dispatch shape; `src/AkariToolbox.Framework/Navigation/INavigationAware.cs` for the nav-hook contract (file exists, confirmed via Grep — implementers: `FrameNavigationService.cs`).

**Imports/constructor-injection pattern** (`DebloatViewModel.cs` lines 1-37):
```csharp
using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.Input;
using AkariToolbox.App.Models;
using AkariToolbox.App.Services;
using AkariToolbox.Framework.Services;
using AkariToolbox.Framework.ViewModels;

namespace AkariToolbox.App.ViewModels;

public partial class DownloadsViewModel : ViewModelBase, INavigationAware
{
    private readonly IAppCatalog _catalog;
    private readonly IAppInstallerService _installer;
    private readonly IPostInstallService _postInstall;
    private readonly ILogConsoleService _log;

    public DownloadsViewModel(IAppCatalog catalog, IAppInstallerService installer,
        IPostInstallService postInstall, ILogConsoleService log)
    {
        _catalog = catalog; _installer = installer; _postInstall = postInstall; _log = log;
        Title = "Downloads";
        Apps = catalog.Apps.Select(a => new AppItem { Name = a.Name, Description = a.Description, Category = a.Category, WingetId = a.WingetId }).ToList();
    }
```

**Fire-and-forget nav-trigger pattern (D-06)** — copy verbatim from RESEARCH.md Pattern 1 (already a concrete, ready-to-use code block citing `INavigationAware.OnNavigatedTo`'s synchronous signature):
```csharp
public void OnNavigatedTo(object? parameter)
{
    _ = EnsurePostInstallSilentlyAsync();
}

private async Task EnsurePostInstallSilentlyAsync()
{
    try
    {
        var ok = await _postInstall.EnsurePostInstallAsync();
        if (!ok) _log.Log("[DOWNLOADS] PostInstall mirror incomplete — some files failed to download.");
    }
    catch (Exception ex)
    {
        _log.Log($"[DOWNLOADS] PostInstall mirror failed: {ex.Message}");
    }
}
```

**Error handling pattern** — mirror `DebloatViewModel.ExecuteAsync`'s `catch (FileNotFoundException ex)` + `_log.Log(...)` convention (lines 107-113) for the "Install Selected" command path, plus the `ConcurrentDictionary<string, SemaphoreSlim>` per-key lock pattern (lines 30, 93-94) to prevent double-install races if the user double-clicks "Install Selected".

---

### `src/AkariToolbox.App/ViewModels/MiscViewModel.cs` (controller, request-response)

**Analog:** `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs` (full file read)

**Constructor + RelayCommand dispatch pattern** (lines 24-37, 62-119) — same shape as Downloads:
```csharp
public partial class MiscViewModel : ViewModelBase
{
    private readonly IContextMenuService _contextMenu;
    private readonly IDialogService _dialogService;
    private readonly ILogConsoleService _log;

    public MiscViewModel(IContextMenuService contextMenu, IDialogService dialogService, ILogConsoleService log)
    {
        _contextMenu = contextMenu; _dialogService = dialogService; _log = log;
        Title = "Misc";
        Items = BuildItems(); // 13 MiscItem rows, flat list
    }

    [RelayCommand]
    private Task AddAsync(MiscItem item) => ExecuteAsync(item, isRemove: false);

    [RelayCommand]
    private Task RemoveAsync(MiscItem item) => ExecuteAsync(item, isRemove: true);
}
```

**Confirmation-gate pattern (D-11)** — copy directly from RESEARCH.md Code Examples (verbatim, ready-to-use), which itself mirrors `DebloatViewModel.ExecuteAsync` lines 79-91's `action.RequiresConfirmation && !isUndo` gate:
```csharp
[RelayCommand]
private async Task AddAsync(MiscItem item)
{
    if (item.Key == "take_own")
    {
        var confirmed = await _dialogService.ConfirmAsync(
            item.Title,
            "This grants broad Everyone:FullControl permissions recursively on the selected file/folder. Continue?");
        if (!confirmed) return;
    }
    _contextMenu.Add(item.Key);
    item.IsAdded = true;
    RestartExplorer();
}
```

**Explorer-restart helper** — small static/private helper method called after every Add/Remove (RESEARCH.md Architectural Responsibility Map row "Explorer restart after Misc Add/Remove" — predecessor's `RestartExplorer()` kills all `explorer` processes and relaunches; port as a private method on `MiscViewModel`, not a new service, per RESEARCH.md's own recommendation).

---

### `src/AkariToolbox.App/Services/PostInstallService.cs` (MODIFIED — D-07 SHA256 gate)

**Analog:** itself (full file read, 265 lines — small enough for single read, no re-read needed)

**Exact modification target** — `DownloadFileAsync` at lines 226-242 currently has no verification call. Add `expectedSha256` parameter and gate per RESEARCH.md Pattern 2 (already a complete, ready-to-use code block citing exact before/after):
```csharp
private async Task<bool> DownloadFileAsync(string url, string destPath, string label, string expectedSha256)
{
    try
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        var http = httpClientFactory.CreateClient("PostInstall");
        var bytes = await http.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(destPath, bytes);

        if (!await VerifyFileSha256Async(destPath, expectedSha256))
        {
            log.Log($"[POSTINSTALL] Integrity check FAILED for {label} — deleting corrupted/tampered file.");
            File.Delete(destPath);
            return false;
        }

        log.Log($"[POSTINSTALL] OK {label} ({bytes.Length / 1024} KB, SHA256 verified)");
        return true;
    }
    catch (Exception ex)
    {
        log.Log($"[POSTINSTALL] FAIL {label}: {ex.Message}");
        return false;
    }
}
```
`VerifyFileSha256Async` already exists at lines 245-264 — no change needed there, only the call site above and threading `expectedSha256` through the `AllFiles` loop (lines 198-216), which needs to become a manifest lookup (`Dictionary<string,string>` keyed by relative path, per D-08 — see Shared Patterns below) instead of a bare `string[]`.

---

### `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs` (MODIFIED — DI registration)

**Analog:** itself (full file read, 60 lines)

**Exact pattern to extend** — the file's own established convention of appending one `services.AddSingleton<TInterface, TImpl>()` line per new catalog/service, with a `//` comment explaining why it's registered here rather than reflection-scanned (lines 53-56 for `IDebloatCatalog`'s precedent):
```csharp
// Downloads' compiled-in app-installer catalog (DOWNLOADS-02) — same rationale as
// IDebloatCatalog above (static catalog, not per-app handler classes).
services.AddSingleton<IAppCatalog, AppCatalog>();
services.AddSingleton<IAppInstallerService, AppInstallerService>();

// Misc's 12+1 context-menu Add/Remove orchestrator (MISC-01) — thin wrapper over the
// already-registered IRegistryService, not an ITweakHandler (no GetState/SetState).
services.AddSingleton<IContextMenuService, ContextMenuService>();
```
No new `AddHttpClient` needed — the `"PostInstall"` named client (lines 40-44) is already registered and reused as-is for D-06/D-07.

---

## Shared Patterns

### Registry-squatting-safe writes (all Misc entries)
**Source:** `src/AkariToolbox.Framework/Services/IRegistryService.cs` (lines 11-58, full interface read)
**Apply to:** `ContextMenuService.cs` — every one of the 13 entries
```csharp
void SetValue(RegistryHive hive, string subKeyPath, string valueName, object value, RegistryValueKind kind);
void DeleteSubKeyTree(RegistryHive hive, string subKeyPath); // never throws on missing
```
Never call raw `Microsoft.Win32.Registry.*` directly — always go through `IRegistryService`, per RESEARCH.md's "Don't Hand-Roll" table.

### Confirmation-gate pattern (Take Ownership only, D-11)
**Source:** `src/AkariToolbox.App/ViewModels/DebloatViewModel.cs` lines 79-91 (`action.RequiresConfirmation && !isUndo` gate) + `src/AkariToolbox.Framework/Services/IDialogService.cs` lines 21-25 (`ConfirmAsync` signature, full file read)
**Apply to:** `MiscViewModel.AddAsync` — gate only the `"take_own"` key, per D-11.

### Embedded-script execution (D-04 hardening scripts)
**Source:** `src/AkariToolbox.Framework/Services/IScriptRunner.cs` lines 28-45 (full file read)
**Apply to:** `AppInstallerService.InstallAsync` — after `RunProcessAsync("winget", ...)` succeeds, call `_scriptRunner.RunEmbeddedScriptAsync(app.HardeningResourceSuffix)` if non-null. Same extract-to-temp-then-`powershell.exe -NoProfile -ExecutionPolicy Bypass -File`-then-cleanup mechanism already used for all 28 Debloat scripts — no new process-management code.

### Fire-and-forget async on page navigation (D-06)
**Source:** `src/AkariToolbox.Framework/Navigation/INavigationAware.cs` (confirmed interface location via Grep; synchronous `OnNavigatedTo(object? parameter)` contract per RESEARCH.md Pattern 1's citation)
**Apply to:** `DownloadsViewModel` only — wrap the async call in a discarded `_ = ...Async()` task, never `async void` on the interface method itself, and always try/catch inside the wrapped method so no unobserved task exception surfaces.

### DI registration — one call site, comment-annotated
**Source:** `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs` (full file read, 60 lines)
**Apply to:** All 3 new services (`IAppCatalog`, `IAppInstallerService`, `IContextMenuService`) — append `AddSingleton` calls with a short rationale comment, matching the file's existing self-documenting style; do not create a second registration method.

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| `PostInstallManifest.json` (embedded resource, D-08 SHA256 manifest) | config (data) | file-I/O | No existing checked-in hash-manifest file exists anywhere in the codebase to pattern-match against — this is genuinely new data, authored via a one-off utility per RESEARCH.md Open Question 5. Structure it as a simple `Dictionary<string,string>` (relative path → hex SHA256), loaded the same way other embedded resources are (see `IScriptRunner.RunEmbeddedScriptAsync`'s resource-resolution convention, lines 36-42, for the "check own assembly then AppDomain" pattern if the manifest is embedded rather than a loose file). |
| `1 Installers.ps1` / `4 Context Menu.ps1` extraction (source scripts, not codebase files) | n/a | n/a | These are source material in `C:/Users/isleap/Desktop/AkariOS Tweaks/`, not existing codebase analogs — RESEARCH.md already extracted every needed line range from them directly (Code Examples section), so no further reading was done here per the "stop re-reading" rule. |

## Metadata

**Analog search scope:** `src/AkariToolbox.App/Services/*.cs`, `src/AkariToolbox.App/ViewModels/*.cs`, `src/AkariToolbox.App/Models/*.cs`, `src/AkariToolbox.Framework/Services/*.cs`, `src/AkariToolbox.Framework/Navigation/*.cs`
**Files scanned:** 14 read in full this session (`DebloatCatalog.cs`, `IDebloatCatalog.cs`, `DebloatViewModel.cs`, `TweakHandlerRegistration.cs`, `IRegistryService.cs`, `IScriptRunner.cs`, `DebloatAction.cs`, `DebloatActionItem.cs`, `IDialogService.cs`, `PostInstallService.cs`) + 2 Glob listings + 1 Grep + CONTEXT.md/RESEARCH.md (already contain verbatim excerpts from predecessor files `MiscViewModel.cs`, `DownloadsViewModel.cs`, `AppInstallerService.cs`, `AppItem.cs`, so those were not re-read — RESEARCH.md's Code Examples section is the authoritative excerpt source for predecessor code)
**Pattern extraction date:** 2026-09-02
