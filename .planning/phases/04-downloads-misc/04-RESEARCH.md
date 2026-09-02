# Phase 4: Downloads & Misc - Research

**Researched:** 2026-09-02
**Domain:** Self-healing GitHub asset mirror (HttpClient + SHA256), winget-backed app-installer catalog extension, classic Windows context-menu registry management
**Confidence:** HIGH (all core primitives already exist and were read directly this session; all 15 new winget package IDs verified live against the real winget catalog; the 12 context-menu registry patterns are a direct line-cited carry from the predecessor)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**Downloads Page — App Installer Catalog**
- **D-01:** The predecessor's winget-based app-installer catalog (`DownloadsViewModel`/`AppInstallerService` — searchable/filterable ItemsControl, categories, multi-select + "Install Selected") is ported as the Downloads page's core UI and mechanism, NOT replaced. This is DOWNLOADS-02's real implementation despite REQUIREMENTS.md's stale "playbooks, drivers, and utility links" wording — the user confirmed "keep the downloads the same."
- **D-02:** 15 new apps are added to the catalog, sourced from `4 Installers/1 Installers.ps1`'s 24-entry list minus the 9 already present in the existing catalog (Chrome, Firefox, Discord, Steam, Brave, 7-Zip already exist as winget entries — do not duplicate): Roblox, Battle.net, Electronic Arts, Epic Games, Escape From Tarkov, Frame View, GOG Galaxy, League of Legends, Nvidia App, OBS Studio, Onboard Memory Manager, PotPlayer, Rockstar Games, Ubisoft Connect, Valorant.
- **D-03:** New apps install via winget package ID (matching the existing catalog's mechanism), NOT the source script's direct-CDN-download method — consistent single install mechanism across the whole catalog. — **Reversibility:** reversible — swapping an app's install path from winget-ID to direct-download later is a localized change per catalog entry, not a mechanism-wide rewrite.
- **D-04:** After each new app installs via winget, port `1 Installers.ps1`'s per-app post-install hardening steps (disable telemetry/hardware-acceleration where the script does it, remove auto-launch-at-login registry/scheduled-task entries, delete vendor-installed scheduled tasks/services the script removes, clean up shortcut placement) as a follow-up step — this is the real privacy/perf value-add the source script provides beyond a bare winget install, and the user wants it kept, not dropped for simplicity.
- **D-05 [Claude's Discretion]:** Exact category assignment for the 15 new apps (most are "Gaming"; Frame View/OBS Studio/PotPlayer/Onboard Memory Manager could be "Gaming" or a new "Utilities"/"Streaming" grouping) — Claude/research proposes during planning.

**Downloads Page — PostInstall Mirror**
- **D-06:** `IPostInstallService.EnsurePostInstallAsync()` (already built, Phase 1) is wired to auto-trigger silently on first Downloads-page visit — matches the predecessor's on-demand internal-invocation pattern (it was never a page action in the predecessor, just called internally by other features).
- **D-07:** Each downloaded file is verified via the already-built `IPostInstallService.VerifyFileSha256Async` before being treated as successfully mirrored — new integrity guarantee the predecessor never had. This explicitly diverges from Phase 2's D-06 "no added verification" precedent for other downloaded content: the primitive already exists here (unlike Phase 2's driver-tool scripts), so using it is not extra work, and the user confirmed they want it used. — **Reversibility:** reversible — the verification call can be removed later without touching the download/mirror logic itself.
- **D-08 [Claude's Discretion]:** Exact per-file expected-SHA256 source (a checked-in manifest alongside the file-path list, vs. fetched from the GitHub repo at runtime) — technical implementation, not user vision. Research during planning.

**Misc Page — Context Menu Entries**
- **D-09:** The predecessor's 12 `MiscViewModel` custom-command context-menu entries (Open CMD as Admin, Open PowerShell as Admin, Take Ownership, Control Panel shortcut, File Hash submenu, Kill Not Responding, Windows Tools, Shut Down menu, .pow file association, Run with Priority, Change Resolution, Reboot to BIOS) are ported as direct carries — matches MISC-01 exactly (12 entries, independent add/remove per entry).
- **D-10:** `6 Windows/4 Context Menu.ps1` (classic-menu-restore + built-in-shell-item declutter — a single Clean/Default 2-state toggle, NOT 12 independent entries) is added as a 13th Misc entry, alongside the original 12, not replacing any of them. Confirmed explicitly by the user after initial ambiguity — this script does something structurally different (Windows-11-menu-behavior restoration + built-in-item removal) from the predecessor's 12 "add a custom command" entries, so both stand.
- **D-11:** Only the "Take Ownership" entry gets a confirmation dialog (Phase 3's `IDialogService.ConfirmAsync` pattern) before applying — it recursively grants broad ACL permissions (`icacls /grant *S-1-3-4:F /t`) and is the one genuinely destructive entry if run against the wrong folder. The other 12 (including the new Context Menu.ps1 toggle) stay zero-friction, matching the predecessor's one-click behavior — they're additive/reversible shell entries with low blast radius.

**Misc Page — Extra Tools (MISC-02)**
- **D-12 [informational]:** MISC-02 ("extra misc tools") has no existing implementation anywhere in the predecessor or the "Ultimate" collection to port from — the user confirmed to defer it, not invent content. See Deferred Ideas.

### Claude's Discretion
- D-05: Exact category assignment for the 15 new apps.
- D-08: Exact per-file expected-SHA256 source (checked-in manifest vs. fetched at runtime).

### Deferred Ideas (OUT OF SCOPE)
- **MISC-02 ("extra misc tools")** — no existing implementation anywhere (predecessor or Ultimate collection) to port from. User confirmed: defer rather than invent content. Needs a REQUIREMENTS.md follow-up edit (same retirement/scope-note pattern as Phase 2's GAMING-02) — either mark MISC-02 as not satisfied by this phase's v1 scope, or leave open for a future phase once concrete tool candidates exist.
- The remaining 9 branches of `1 Installers.ps1` not pulled into D-02 (apps already present in the existing winget catalog) — no action needed, not deferred, just excluded as duplicates. **Research correction to this framing: see Open Questions — this "9" count is internally inconsistent with D-02's actual 15-name list; verify during planning, not implementation.**
- The other 4 scripts in `4 Installers/` (`2 MSI Afterburner.ps1`, `3 Nvidia Profile Inspector.ps1`, `4 More Clock Tool.ps1`, `5 CRU SRE.ps1`) — not examined this discussion, not pulled into scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| DOWNLOADS-01 | On first use, the app automatically downloads and mirrors the PostInstall asset folder from GitHub to `C:\PostInstall\` if not already present, no-op if it is | `IPostInstallService`/`PostInstallService` already fully implemented (read this session) — only needs an `INavigationAware.OnNavigatedTo` wiring on `DownloadsViewModel` plus a SHA256 manifest for D-07. See Architecture Patterns §1, §2. |
| DOWNLOADS-02 | User can access playbooks, drivers, and recommended utility links from the Downloads page — **actually implemented as**: the winget app-installer catalog per D-01 | Predecessor `DownloadsViewModel`/`AppInstallerService`/`AppItem` read in full this session; 15 new winget IDs verified live via `winget show`/`winget search`. See Standard Stack, Package/Winget Legitimacy Audit, Architecture Patterns §3-§4. |
| MISC-01 | User can add or remove each of the 12 context-menu entries (classic/legacy Windows context menu) | Predecessor `MiscViewModel.cs` read in full this session (all 12 Add/Remove implementations, lines 1-347) — direct-carry via existing `IRegistryService`, no new registry primitive needed. See Architecture Patterns §5, Code Examples. |
| MISC-02 | User can access the extra misc tools from the Misc page | **Deferred per D-12** — no implementation exists to port; flag for REQUIREMENTS.md follow-up, do not build placeholder content. |
</phase_requirements>

## Summary

Phase 4 is almost entirely a **port-and-wire** phase, not a build-from-scratch phase: every hard primitive it needs already exists in the current codebase (`IPostInstallService`, `IRegistryService`, `IScriptRunner`, `IDialogService`, `IHttpClientFactory`'s `"PostInstall"` named client) or already exists verbatim in the predecessor (`MiscViewModel`'s 12 registry Add/Remove pairs, `DownloadsViewModel`/`AppInstallerService`/`AppItem`'s winget catalog shape). The two genuinely new pieces of work are: (1) sourcing and verifying 15 new winget package IDs plus their per-app PowerShell hardening steps from `1 Installers.ps1`, and (2) porting `4 Context Menu.ps1`'s ~20 registry operations (across a Clean/Default 2-state toggle) as a 13th Misc entry.

This session verified all 15 new winget package IDs live against the installed `winget` CLI (v1.29.290) and found four real gaps the planner must resolve, not invent around: **Escape From Tarkov has no winget package at all** (conflicts with D-03's "winget ID, not CDN download" mandate for that one app); **Nvidia App** exists only as an opaque Microsoft Store ID (`XP8CLZL93F5Z4P`, no human-readable moniker, no plain `winget` source hit); **League of Legends and Valorant** exist only as region-specific IDs (`RiotGames.LeagueOfLegends.NA`, `RiotGames.Valorant.NA`, etc.) with no generic ID; and **"Epic Games" and "GOG Galaxy" in D-02's 15-name list are literal duplicates** of `EpicGames.EpicGamesLauncher` and `GOG.Galaxy`, which already exist in the predecessor's ported 28-app catalog — adding them again would violate D-02's own "do not duplicate" instruction. All four are flagged in Open Questions with a recommended default so planning is not blocked.

For the PostInstall mirror (DOWNLOADS-01/D-06/D-07), the only new work is wiring `EnsurePostInstallAsync()` into `DownloadsViewModel.OnNavigatedTo` (fire-and-forget, logged via `ILogConsoleService`, never blocking the page) and producing a SHA256 manifest for D-07 — this session confirmed via a live GitHub API call that the upstream `isleap9/PostInstall` repo publishes **no** checksum manifest of its own, so D-08 resolves to "checked-in manifest authored by this app," not "fetched at runtime."

**Primary recommendation:** Treat this phase as three independent, low-risk port tracks (winget catalog extension, PostInstall mirror wiring, Misc registry entries) that share almost no code — plan them as separate waves/plans rather than one monolithic Downloads+Misc plan, since their only common dependency is the already-built `IRegistryService`/`IScriptRunner`/`IPostInstallService` primitives.

## Architectural Responsibility Map

| Capability | Primary Tier | Secondary Tier | Rationale |
|------------|-------------|----------------|-----------|
| Winget app-installer catalog (search/filter/multi-select/install) | Application Service Layer (`AppInstallerService`-equivalent) | View/ViewModel (filter/search state) | Matches existing `ITweakCatalog`/`IDebloatCatalog` two-tier pattern — catalog is a static data + orchestration layer, ViewModel is presentation-only |
| Per-app post-install hardening (D-04) | System Primitive / Mutation Layer (`IScriptRunner`, embedded `.ps1`) | — | Registry/scheduled-task/service/shortcut mutations are exactly what `IScriptRunner.RunEmbeddedScriptAsync` already exists for (Pattern 2, ARCHITECTURE.md) — no new primitive |
| PostInstall asset mirror (DOWNLOADS-01) | Application Service Layer (`IPostInstallService`, already built) | — | Independent of the tweak/debloat framework by design (ARCHITECTURE.md "PostInstall self-heal" data flow) — only touches filesystem + GitHub |
| SHA256 integrity verification (D-07) | System Primitive Layer (`IPostInstallService.VerifyFileSha256Async`, already built) | — | Already a generic, file-path-agnostic primitive — no new code, just a call site |
| Context-menu Add/Remove (MISC-01, 12 entries + 13th toggle) | System Primitive / Mutation Layer (`IRegistryService`) | Application Service Layer (a thin `IContextMenuService` orchestrator, per ARCHITECTURE.md's already-recommended `Services/Misc/IContextMenuService.cs`) | Every one of the 12 predecessor entries is pure `RegistryKey.CreateSubKey`/`DeleteSubKeyTree` — no process spawn, no PowerShell needed (confirmed by reading `MiscViewModel.cs` in full) |
| Classic-menu-restore toggle (D-10, 13th entry) | System Primitive Layer (`IRegistryService`) — **not** `IScriptRunner` | — | `4 Context Menu.ps1`'s body is exclusively `reg add`/`reg delete`/one `regedit.exe /S` import — directly portable to `IRegistryService.SetValue`/`DeleteValue`/`DeleteSubKeyTree` calls, avoiding an unnecessary PowerShell process spawn for what is ultimately ~20 registry writes (see Architecture Patterns §6) |
| Explorer restart after Misc Add/Remove | View/ViewModel or a small shared helper | — | Predecessor's `RestartExplorer()` (kill all `explorer` processes, relaunch) is a UI-refresh side effect, not a registry primitive — keep it as a small static helper the ViewModel calls after every Add/Remove, matching predecessor behavior 1:1 |

## Standard Stack

### Core (unchanged — no new NuGet packages for this phase)

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| `System.Net.Http` (`HttpClient` via `IHttpClientFactory`) | In-box | PostInstall asset download | Already registered as the named `"PostInstall"` client [VERIFIED: src/AkariToolbox.App/Services/TweakHandlerRegistration.cs:40-44 — `services.AddHttpClient("PostInstall", c => { c.DefaultRequestHeaders.Add("User-Agent", "AkariToolbox"); c.Timeout = TimeSpan.FromMinutes(10); });`] — no new registration needed |
| `System.Security.Cryptography` (`SHA256`) | In-box | D-07 integrity verification | `IPostInstallService.VerifyFileSha256Async` already implemented [VERIFIED: src/AkariToolbox.App/Services/PostInstallService.cs:245-264] — streams the file, computes hex digest, case-insensitive compare, never throws |
| `Microsoft.Win32.Registry` via `IRegistryService` | In-box | All 12 Misc entries + the 13th Context-Menu-toggle entry | `IRegistryService` already supports `RegistryHive.ClassesRoot` (the .NET `RegistryHive` enum includes `ClassesRoot`) plus `GetValue`/`SetValue`/`DeleteValue`/`DeleteSubKeyTree`/`CreateSubKey` [VERIFIED: src/AkariToolbox.Framework/Services/IRegistryService.cs:12-58] — no new registry primitive required, every predecessor `Registry.ClassesRoot.CreateSubKey(...)`/`DeleteSubKeyTree(...)` call maps 1:1 |
| `System.Diagnostics.Process` via `IScriptRunner` | In-box, wrapped | winget install shell-out + D-04 hardening scripts | `IScriptRunner.RunEmbeddedScriptAsync(resourceSuffix, arguments, timeout)` already handles extract-embedded-resource-to-temp + `powershell.exe -NoProfile -ExecutionPolicy Bypass -File` + cleanup [VERIFIED: src/AkariToolbox.Framework/Services/IScriptRunner.cs:28-45] |
| `winget` (external CLI, already present on the target OS) | v1.29.290 confirmed installed this session | App install mechanism (D-03) | [VERIFIED: winget CLI, ran `winget --version` this session — returned `v1.29.290`] |

**No new NuGet packages needed for this phase.** Every primitive DOWNLOADS-01/02 and MISC-01 need already exists in the codebase or the in-box BCL.

### Winget/Package Legitimacy Audit

> This phase installs external *applications* via `winget`, not NuGet/npm/PyPI packages — the standard ecosystem `package-legitimacy check` tool (npm/pypi/crates) does not apply to the `winget` ecosystem. Instead, every package ID below was verified live this session via `winget show --id <id> --exact` / `winget search "<query>"` against the real, installed winget client and its default `winget`/`msstore` sources.

| App (D-02 name) | Winget ID (verified) | Publisher (from `winget show`) | Verdict | Disposition |
|---|---|---|---|---|
| Roblox | `Roblox.Roblox` | Roblox Corporation | OK | Approved — matches [ASSUMED] guess in CONTEXT.md, now [VERIFIED: winget CLI] |
| Battle.net | `Blizzard.BattleNet` | Blizzard Entertainment | OK | Approved — matches CONTEXT.md guess, now [VERIFIED: winget CLI] |
| Electronic Arts (EA app) | `ElectronicArts.EADesktop` | Electronic Arts | OK | Approved — CONTEXT.md had no guess; ID discovered and verified this session |
| Epic Games | `EpicGames.EpicGamesLauncher` | Epic Games, Inc. | OK, but **DUPLICATE** | See Open Questions — identical ID already in the existing 28-app catalog [VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/DownloadsViewModel.cs:107 — `("Epic Games Launcher", "Store and launcher for Epic titles", "Gaming", "EpicGames.EpicGamesLauncher"),`] |
| Escape From Tarkov | *(none found)* | — | **SLOP-equivalent: not on winget at all** | REMOVED from winget-mechanism scope — see Open Questions |
| Frame View | `Nvidia.FrameView` | NVIDIA | OK | Approved — CONTEXT.md flagged as "unconfirmed," now [VERIFIED: winget CLI] |
| GOG Galaxy | `GOG.Galaxy` | GOG.com | OK, but **DUPLICATE** | See Open Questions — identical ID already in the existing 28-app catalog [VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/DownloadsViewModel.cs:106 — `("GOG Galaxy", "DRM-free game platform with cross-play features", "Gaming", "GOG.Galaxy"),`] |
| League of Legends | *(no generic ID — region-specific only, e.g.)* `RiotGames.LeagueOfLegends.NA` | Riot Games | SUS-equivalent: region ambiguity | Flagged — planner must pick a default region or add a region picker; see Open Questions |
| Nvidia App | `XP8CLZL93F5Z4P` (msstore source only) | NVIDIA (Microsoft Store listing) | SUS-equivalent: non-standard source/ID shape | Flagged — recommend `checkpoint:human-verify` before relying on this install path; see Open Questions |
| OBS Studio | `OBSProject.OBSStudio` | OBS Project | OK | Approved — matches CONTEXT.md guess, [VERIFIED: winget CLI] |
| Onboard Memory Manager | `Logitech.OnboardMemoryManager` | Logitech | OK | Approved — CONTEXT.md flagged as unconfirmed, now [VERIFIED: winget CLI] |
| PotPlayer | `Daum.PotPlayer` (**not** `PotPlayer.PotPlayer`) | Daum | OK | Approved with corrected ID — no such ID as `PotPlayer.PotPlayer` exists |
| Rockstar Games | `RockstarGames.Launcher` (**not** `Rockstar.RockstarGamesLauncher`) | Rockstar Games | OK | Approved with corrected ID |
| Ubisoft Connect | `Ubisoft.Connect` | Ubisoft | OK | Approved — matches CONTEXT.md guess, [VERIFIED: winget CLI] |
| Valorant | *(no generic ID — region-specific only, e.g.)* `RiotGames.Valorant.NA` | Riot Games | SUS-equivalent: region ambiguity | Flagged — same as League of Legends; see Open Questions |

**Packages removed due to no-winget-package verdict:** Escape From Tarkov (D-03's winget mechanism cannot apply to this one app — see Open Questions for the two remediation options).
**Packages flagged as suspicious (non-standard source/ID shape or ambiguous target):** Nvidia App (msstore-only opaque ID), League of Legends (region-specific ID), Valorant (region-specific ID) — planner should add a `checkpoint:human-verify` task before wiring these three into the catalog's install path, or pick and document the default (see Open Questions).

**Postinstall script check (Node.js-equivalent risk for this ecosystem):** N/A — `winget install` itself is the install mechanism; no `postinstall`-script-equivalent risk exists at the catalog layer. The genuine analogous risk is the **D-04 hardening scripts themselves** (embedded `.ps1`, run after install) — these are authored by this project from a known, already-reviewed source script (`1 Installers.ps1`), not fetched at runtime, so they carry the same trust level as the existing 28 Debloat scripts, not a third-party postinstall hook.

## Architecture Patterns

### System Architecture Diagram

```
                    ┌─────────────────────────────────────────┐
                    │  Downloads Page (View)                    │
                    │  ┌─────────────┐  ┌─────────────────────┐│
                    │  │ Search/Cat  │  │ ItemsControl:        ││
                    │  │ filter bar  │  │ AppItem grid + ✓select││
                    │  └──────┬──────┘  └──────────┬───────────┘│
                    └─────────┼────────────────────┼────────────┘
                              │ x:Bind               │ "Install Selected"
                              ▼                      ▼
                 ┌────────────────────────────────────────────┐
                 │ DownloadsViewModel (ObservableObject,        │
                 │ INavigationAware)                            │
                 │  OnNavigatedTo(param) ──► fire-and-forget    │
                 │     _postInstall.EnsurePostInstallAsync()    │──┐
                 │  InstallSelectedCommand ──► catalog.InstallAsync│
                 └──────────────┬─────────────────────┬────────┘  │
                                │                       │           │
                     ┌──────────▼─────────┐   ┌─────────▼────────┐ │
                     │ AppInstallerService │   │ IPostInstallService│◄┘
                     │ (winget catalog)    │   │ (already built)    │
                     │ - winget install    │   │ - download+SHA256  │
                     │   --id X --silent   │   │   mirror to        │
                     │ - IScriptRunner.Run │   │   C:\PostInstall\  │
                     │   EmbeddedScript    │   └─────────┬──────────┘
                     │   Async(hardening)  │             │
                     └──────────┬──────────┘             ▼
                                │                  GitHub raw.githubusercontent.com/
                                ▼                  isleap9/PostInstall/main/PostInstall/
                          winget.exe / powershell.exe
                          (embedded .ps1 hardening scripts)


                    ┌─────────────────────────────────────────┐
                    │  Misc Page (View)                         │
                    │  ItemsControl: 13 rows, Add/Remove toggle │
                    └──────────────────┬────────────────────────┘
                                       │ x:Bind [RelayCommand]
                                       ▼
                 ┌────────────────────────────────────────────┐
                 │ MiscViewModel (ObservableObject)              │
                 │  Add(key)/Remove(key) ──► IContextMenuService │
                 │  key=="take_own" ──► IDialogService.ConfirmAsync│
                 │  after every Add/Remove ──► RestartExplorer() │
                 └──────────────────┬─────────────────────────┘
                                    ▼
                        ┌────────────────────────┐
                        │ IContextMenuService      │
                        │  (thin orchestrator)     │
                        └──────────┬───────────────┘
                                   ▼
                        ┌────────────────────────┐
                        │ IRegistryService          │
                        │  SetValue/DeleteValue/    │
                        │  DeleteSubKeyTree/         │
                        │  CreateSubKey (HKCR)      │
                        └──────────┬───────────────┘
                                   ▼
                          Windows Registry (HKCR)
```

### Recommended Project Structure

```
src/AkariToolbox.App/
├── Models/
│   ├── AppItem.cs                    # NEW — port of predecessor, ObservableObject w/ IsSelected
│   ├── MiscItem.cs                   # NEW — port of predecessor's MiscItem (Key/Title/Description)
├── Services/
│   ├── IAppCatalog.cs / AppCatalog.cs      # NEW — static 28+15=43-app catalog (mirrors IDebloatCatalog pattern)
│   ├── IAppInstallerService.cs / AppInstallerService.cs  # NEW — winget install + D-04 hardening dispatch
│   ├── IContextMenuService.cs / ContextMenuService.cs    # NEW — 12+1 Add/Remove over IRegistryService
├── Resources/
│   ├── DownloadsScripts/              # NEW — one .ps1 per D-04 hardening step, keyed like DebloatScripts
│   │   ├── roblox-harden.ps1
│   │   ├── battlenet-harden.ps1
│   │   └── ... (15 total, only for apps whose script branch has a hardening step beyond bare install)
├── ViewModels/
│   ├── DownloadsViewModel.cs          # NEW — implements INavigationAware for D-06 auto-trigger
│   ├── MiscViewModel.cs               # NEW
└── Views/
    ├── DownloadsPage.xaml / .xaml.cs  # NEW
    └── MiscPage.xaml / .xaml.cs       # NEW
```

### Pattern 1: Silent auto-trigger on first navigation (D-06)

**What:** `DownloadsViewModel` implements `INavigationAware` and calls `IPostInstallService.EnsurePostInstallAsync()` fire-and-forget from `OnNavigatedTo`, logging via `ILogConsoleService` — never blocking page render, never showing a modal/progress UI unless a download is actually in flight.
**When to use:** Exactly once, on `DownloadsViewModel`. `INavigationAware.OnNavigatedTo(object? parameter)` is synchronous [VERIFIED: src/AkariToolbox.Framework/Navigation/INavigationAware.cs:9 — `void OnNavigatedTo(object? parameter);`], so the async call must be dispatched as `_ = RunEnsurePostInstallAsync()` (discarded task, not awaited) rather than making `OnNavigatedTo` itself `async void` directly on the interface method (safer to keep the interface signature untouched and wrap in a private async method).
**Example:**
```csharp
// New — DownloadsViewModel.cs, following the framework's existing INavigationAware contract
public partial class DownloadsViewModel : ViewModelBase, INavigationAware
{
    private readonly IPostInstallService _postInstall;
    private readonly ILogConsoleService _log;

    public void OnNavigatedTo(object? parameter)
    {
        // Fire-and-forget: OnNavigatedTo is sync (framework contract), never block page load.
        // No UI progress surface required — success criterion #1 says "automatically... no-op
        // if already present" with no mention of a visible progress affordance.
        _ = EnsurePostInstallSilentlyAsync();
    }

    private async Task EnsurePostInstallSilentlyAsync()
    {
        try
        {
            var ok = await _postInstall.EnsurePostInstallAsync();
            if (!ok)
            {
                _log.Log("[DOWNLOADS] PostInstall mirror incomplete — some files failed to download.");
            }
        }
        catch (Exception ex)
        {
            // Never let a network failure surface as an unhandled/unobserved task exception
            // (Pitfall: PostInstall self-heal must degrade gracefully — see PITFALLS.md).
            _log.Log($"[DOWNLOADS] PostInstall mirror failed: {ex.Message}");
        }
    }
}
```

### Pattern 2: SHA256 verification is a post-download gate, not a separate pass (D-07)

**What:** `PostInstallService.EnsurePostInstallAsync()`'s existing per-file download loop [VERIFIED: src/AkariToolbox.App/Services/PostInstallService.cs:198-224] does not currently call `VerifyFileSha256Async` — it only checks `File.Exists` before skipping and logs OK/FAIL by exception. D-07 requires adding a verification call **after** each successful download, before counting it as `downloaded++`; a hash mismatch should be treated the same as a download failure (delete the bad file, count as `failed++`, do not leave a corrupt/tampered file at the destination path).
**When to use:** Inside `DownloadFileAsync`, immediately after `File.WriteAllBytesAsync`.
**Example:**
```csharp
// Modified DownloadFileAsync — adds D-07's integrity gate after the existing write
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
**D-08 resolution:** A live GitHub API call this session (`api.github.com/repos/isleap9/PostInstall/contents/PostInstall`) confirmed the upstream repo publishes **no** checksum/manifest file of its own — only the raw asset folders (AntiCheat, Defender, GPU, Others, Resync, Services, Tweaks, etc.). This means D-08 must resolve to **a checked-in manifest authored by this app** (a `Dictionary<string,string>` or embedded JSON mapping each of the ~130 `AllFiles` relative paths to its expected SHA256, computed once at authoring time by downloading each file and hashing it), not "fetched from GitHub at runtime" — there is nothing to fetch. Flag this as an authoring task: someone must download all ~130 files once and compute+pin their hashes before D-07 can be implemented; this is real, non-trivial setup work the plan must account for as its own step, not something a code-only task can produce from thin air.

### Pattern 3: Extend the existing static catalog pattern, don't rearchitect it

**What:** `DownloadsViewModel`'s `SeedApps()` uses a `(Name, Desc, Cat, Pkg)[]` tuple array [VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/DownloadsViewModel.cs:82-113] — the exact same shape as this project's already-ported `DebloatCatalog.cs` (`IReadOnlyList<DebloatAction> Actions`). Port this catalog the same way `DebloatCatalog` was ported: a static `IAppCatalog`/`AppCatalog` class holding all 28 existing + 13 genuinely-new (15 minus the 2 duplicates, see Open Questions) entries as an `IReadOnlyList<AppDefinition>` record list, registered as a DI singleton alongside `IDebloatCatalog` in `TweakHandlerServiceCollectionExtensions` (the one existing App-project registration call site).
**When to use:** For the whole Downloads catalog (existing 28 + new entries).
**Example:**
```csharp
// New — AppDefinition.cs, mirrors DebloatAction.cs's record shape
public sealed record AppDefinition(
    string Name,
    string Description,
    string Category,
    string WingetId,
    string? HardeningResourceSuffix = null); // D-04: null when winget install alone is sufficient
```

### Pattern 4: Post-install hardening runs after `WaitForExitAsync`, sequenced per-app

**What:** `AppInstallerService.InstallAsync` (predecessor) already does `Process.Start(winget...)` then `await proc.WaitForExitAsync()` per app [VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/AppInstallerService.cs:35-37]. D-04's hardening steps must run **after** this await completes for that specific app, not batched at the end — several hardening steps assume the app just finished installing (e.g. killing a freshly-spawned tray process, moving a Start Menu shortcut winget/the installer just created).
**When to use:** Immediately after each app's `WaitForExitAsync()` returns, before moving to the next app in the batch.
**Trade-offs:** Hardening steps that assume a specific install path (e.g. `1 Installers.ps1`'s Battle.net branch assumes `C:\Program Files (x86)\Battle.net\Battle.net Launcher.exe` because the *original script* passed `--installpath=` explicitly) may not match wherever `winget install <id> --silent` actually places the app, since D-03 drops the script's own direct-download-with-custom-path approach in favor of winget's default install location. **Each hardening script must be spot-checked against winget's actual install path for that app**, not assumed identical to the source script's hardcoded path — this is a real per-app verification task, not a mechanical port.

### Pattern 5: The 12 Misc entries need zero new registry primitives — direct 1:1 port

**What:** Every one of `MiscViewModel`'s 12 Add/Remove pairs [VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/MiscViewModel.cs:95-346] is exclusively `Registry.ClassesRoot.CreateSubKey(...)`/`.SetValue(...)` for Add, and `Registry.ClassesRoot.OpenSubKey(..., true)?.DeleteSubKeyTree(name, throwOnMissingSubKey: false)` for Remove. `IRegistryService` already exposes `SetValue(RegistryHive, subKeyPath, valueName, value, kind)` and `DeleteSubKeyTree(RegistryHive, subKeyPath)` with the exact same registry-squatting-safe semantics (never throws on missing) [VERIFIED: src/AkariToolbox.Framework/Services/RegistryService.cs:47-61 — `DeleteSubKeyTree` opens the parent then calls `parent?.DeleteSubKeyTree(name, throwOnMissingSubKey: false)`]. `RegistryHive.ClassesRoot` is a standard member of the .NET `Microsoft.Win32.RegistryHive` enum, so no interface change is needed — just call `registry.SetValue(RegistryHive.ClassesRoot, @"Directory\Shell\OpenElevatedCMD", "", "Open CMD As Administrator", RegistryValueKind.String)` etc.
**When to use:** All 12 predecessor entries.
**Trade-offs:** `RegistryKey.CreateSubKey`/`OpenBaseKey(hive, RegistryView.Registry64)` in the current `RegistryService` always opens `RegistryView.Registry64` [VERIFIED: src/AkariToolbox.Framework/Services/RegistryService.cs:20,27,35,42,58,65 — every method opens `RegistryKey.OpenBaseKey(hive, RegistryView.Registry64)`]. `HKEY_CLASSES_ROOT` is a merged view (not hive-redirected the way HKLM/HKCU can be), so `Registry64` view is correct and matches the predecessor's default (non-view-qualified) `Registry.ClassesRoot` calls on 64-bit Windows.
**Note on `kill_nr` (Kill Not Responding) hive quirk:** the predecessor's Add uses `Registry.ClassesRoot.CreateSubKey(...)` but its Remove explicitly targets `Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\DesktopBackground\Shell", true)` instead of `Registry.ClassesRoot` [VERIFIED: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/MiscViewModel.cs:201-207 (Add uses `Registry.ClassesRoot.CreateSubKey(@"DesktopBackground\shell\KillNotResponding")`) and :315 (Remove uses `Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\DesktopBackground\Shell", true)?.DeleteSubKeyTree("KillNotResponding", throwOnMissingSubKey: false);`)]. This is **not a bug to fix** — `HKEY_CLASSES_ROOT` under an elevated process with no per-user redirection resolves to `HKEY_LOCAL_MACHINE\SOFTWARE\Classes`, so both paths land on the same physical key. Port both call sites as-is (`RegistryHive.ClassesRoot` for Add, `RegistryHive.LocalMachine` + `SOFTWARE\Classes\...` for Remove) rather than "cleaning up" to a single hive — changing it risks a silent behavior divergence on non-standard registry redirection setups the predecessor was already tested against.

### Pattern 6: The 13th entry (Context Menu.ps1 toggle) ports as registry calls, not an embedded script

**What:** `4 Context Menu.ps1`'s "Clean" branch (10 registry operations) and "Default" branch (10 reversing operations, one of which imports a `.reg` file via `Regedit.exe /S` for two CLSID command-state handlers) are **all** `reg add`/`reg delete`/one `regedit.exe /S` call [VERIFIED: C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/4 Context Menu.ps1:17-127, full text read this session]. None of these require PowerShell's own logic (no loops, no conditionals, no external tool orchestration) — they are directly portable to `IRegistryService.SetValue`/`DeleteValue` calls, matching Pattern 5's approach for the other 12 entries, **not** wrapped in an embedded `.ps1` run via `IScriptRunner` (which would be the heavier, process-spawning path used for the 28 Debloat scripts).
**When to use:** For D-10's 13th entry specifically.
**Trade-offs:** The one `Regedit.exe /S` import in the "Default" (off/reverse) branch [VERIFIED: same file, lines 73-101 — writes a two-key `.reg` fragment for `pintohome`/`pintohomefile` `CommandStateHandler`/`MUIVerb`/`SkipCloudDownload` values, then `Regedit.exe /S` imports it] can be replaced by direct `IRegistryService.SetValue` calls for each of the 6 named values across the 2 keys — no need to write a temp `.reg` file and shell out to `regedit.exe` at all, since every value in that fragment is a plain string/DWORD `SetValue` call. This is a genuine simplification opportunity over both the source script and the `IScriptRunner` path.
**"Add"/"Remove" mapping (Claude's call per CONTEXT.md's code_context note):** Map the script's "1. Context Menu: Clean (Recommended)" branch to this entry's `Add`, and "2. Context Menu: Default" to `Remove` — consistent with the other 12 entries' Add=apply-the-customization / Remove=restore-Windows-default semantics.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Registry-squatting-safe reads/writes for the 12+1 Misc entries | A new registry wrapper or raw `Microsoft.Win32.Registry` calls in `MiscViewModel`/`ContextMenuService` | `IRegistryService` (already exists, already registry-squatting-safe per Anti-Pattern 4 in ARCHITECTURE.md) | Every write already opens-then-creates and every delete never throws on missing — reimplementing this in a new service duplicates a solved problem and risks reintroducing the exact anti-pattern the rest of the codebase already avoided |
| SHA256 file verification | A new hashing helper | `IPostInstallService.VerifyFileSha256Async` (already exists, already unit-tested — `PostInstallIntegrityTests.cs`) | Generic file-path/hash-pair signature, already proven never to throw on a missing file |
| Embedded-script extraction + PowerShell process management for D-04 hardening | A new "AppHardeningRunner" process-spawning class | `IScriptRunner.RunEmbeddedScriptAsync` (already exists — extract-to-temp, `-NoProfile -ExecutionPolicy Bypass`, cleanup in `finally`) | Exact same shape as the 28 already-ported Debloat scripts — no new process-management code needed |
| A ".reg file + regedit.exe /S" import path for D-10's toggle | Writing a temp `.reg` file and shelling to `regedit.exe`, mirroring the source script literally | `IRegistryService.SetValue` calls for each of the handful of named values | The source script's `.reg`-file-plus-`regedit.exe` approach is itself a workaround for authoring convenience in raw PowerShell — C# can set each named value directly with no temp file and no extra process |
| Winget package existence verification | Trusting CONTEXT.md's `[ASSUMED]` package-ID guesses or training-data winget IDs without checking | `winget show --id <id> --exact` / `winget search "<name>"` against the real installed CLI (already done this session for all 15 apps — see the Winget Legitimacy Audit table) | winget IDs frequently differ from the obvious guess (`PotPlayer.PotPlayer` does not exist; the real ID is `Daum.PotPlayer`) — an unverified guess fails silently at runtime per the existing Debloat-page pitfall about swallowed non-zero exit codes |

**Key insight:** This phase's biggest risk is not missing infrastructure — it's **treating CONTEXT.md's `[ASSUMED]` winget-ID guesses as ground truth without verification**, and **treating D-02's "15 new apps, 9 duplicates excluded" framing as internally consistent when it is not** (see Open Questions). Both are now resolved by live verification this session; the planner should not need to re-derive them.

## Common Pitfalls

### Pitfall 1: D-04 hardening scripts assume install paths that winget won't necessarily produce

**What goes wrong:** `1 Installers.ps1`'s branches frequently hardcode a Start Menu/Desktop shortcut path or `Program Files` install path that matches *that script's own* silent-install invocation (e.g. Battle.net's `--installpath="C:\Program Files (x86)\Battle.net"`). D-03 replaces that install call with `winget install --id Blizzard.BattleNet --silent ...`, which may install to a different default location or produce a differently-named/placed shortcut.
**Why it happens:** The hardening steps were authored against the script's own installer invocation, not against winget's installer behavior for the same underlying app.
**How to avoid:** For each of the (up to) 13 genuinely-new apps, verify winget's actual install/shortcut location (e.g. via a manual `winget install` on a test VM, or by reading the winget manifest's `InstallLocation`/`Shortcuts` metadata) before porting the hardening script verbatim. Where paths differ, adjust the hardening script's paths — do not assume 1:1 fidelity.
**Warning signs:** A hardening step's `Move-Item`/`Remove-Item` on a shortcut path silently no-ops (`-ErrorAction SilentlyContinue` in the source scripts masks this) because the path doesn't exist.
**Phase to address:** This phase, per-app, during the D-04 hardening script authoring step.

### Pitfall 2: Region-specific Riot IDs will silently install the wrong regional client if a plain string match is assumed

**What goes wrong:** `RiotGames.LeagueOfLegends`/`RiotGames.Valorant` do not exist as bare IDs — only `.NA`, `.EU`, `.EUNE`, `.KR`, etc. suffixed variants exist. If the catalog entry is authored with a guessed bare ID, `winget install` will fail to resolve the package entirely (not silently install the wrong region — it will just fail), and if the exit code isn't checked the failure could look like a silent no-op success.
**Why it happens:** Predecessor catalog conventions assume one ID per app; Riot's actual winget listings are per-server.
**How to avoid:** Pick and hardcode one region (recommend `.NA` as the existing catalog's other geographically-unscoped app conventions imply a US/NA-default audience, matching Akari OS's likely primary audience) and document the choice in the catalog entry's description ("League of Legends (NA server)"), or surface a secondary region picker if the planner wants full parity — flagged in Open Questions for a locked decision.
**Warning signs:** `winget install --id RiotGames.LeagueOfLegends` (no region suffix) returns "No package found" — check exit code, don't assume success from lack of exception (matches the existing "check exit code" pitfall already documented in PITFALLS.md Pitfall 4).
**Phase to address:** This phase, catalog authoring.

### Pitfall 3: Nvidia App's msstore-sourced ID may need an explicit `--source msstore` flag and different accept-agreement semantics

**What goes wrong:** `winget show --id XP8CLZL93F5Z4P --exact` only resolves when the `msstore` source is queried; the predecessor's `AppInstallerService.InstallAsync` never passes `--source`, relying on winget's default cross-source search. A plain-ID install without `--source msstore` risks ambiguous-source resolution errors, and Microsoft Store-sourced apps may not honor `--accept-package-agreements`/`--silent` identically to a normal `winget`-source EXE/MSI installer.
**Why it happens:** NVIDIA does not publish "NVIDIA App" to the standard community `winget` source — only via the Microsoft Store, which winget can install through but with different flag semantics.
**How to avoid:** Test this specific install path manually before shipping; pass `--source msstore` explicitly for this one catalog entry rather than relying on default source resolution; treat as its own `checkpoint:human-verify` item per the Package Legitimacy Audit above.
**Warning signs:** `winget install --id XP8CLZL93F5Z4P --silent` exits non-zero or hangs waiting for a Store consent prompt that `--silent` can't suppress.
**Phase to address:** This phase, catalog authoring + manual verification.

### Pitfall 4: Adding "Epic Games" and "GOG Galaxy" as new catalog rows creates literal duplicate entries

**What goes wrong:** If D-02's 15-name list is implemented literally, the Downloads page will show two rows for the exact same app with the exact same winget ID (`EpicGames.EpicGamesLauncher` and `GOG.Galaxy` each appearing twice) — directly contradicting D-02's own "do not duplicate" instruction and creating a confusing double-entry in "Install Selected" (installing the same app twice, or worse, two `AppItem` rows independently toggling `IsSelected` for what winget will treat as one already-installed package).
**Why it happens:** CONTEXT.md's D-02 explanatory text names only 6 of the 9 already-present duplicates explicitly ("Chrome, Firefox, Discord, Steam, Brave, 7-Zip already exist... plus 3 more") and then independently lists "Epic Games" and "GOG Galaxy" among the "15 new" without cross-checking against the predecessor's actual existing Gaming-category rows.
**How to avoid:** Drop "Epic Games" and "GOG Galaxy" from the 15-name list during planning (reducing it to 13 genuinely new apps) — see Open Questions for the exact recommendation.
**Warning signs:** Grep the final `AppCatalog`/`SeedApps` list for duplicate `WingetId` values before considering the catalog complete.
**Phase to address:** This phase, catalog authoring — flag as a required correction, not an open-ended question, since the underlying fact (both IDs already exist in the ported 28-app catalog) is independently verified.

### Pitfall 5: The upstream PostInstall repo has no manifest to fetch — D-08's "fetched at runtime" option does not exist

**What goes wrong:** If the planner assumes D-08 can simply "fetch expected hashes from GitHub," they will find nothing to fetch and either block or invent an ad-hoc verification scheme.
**Why it happens:** The repo (confirmed via a live GitHub API directory listing this session) contains only asset folders, no `checksums.txt`/`manifest.json`/`.sha256` sidecar files anywhere at the root.
**How to avoid:** Treat D-08 as resolved to "checked-in manifest, authored by this app" (see Architecture Pattern 2) and budget a one-time authoring task (download all ~130 files, compute SHA256, embed the mapping) as part of this phase's plan — it is not a pure-code task.
**Warning signs:** A plan step that says "fetch manifest.json from the PostInstall repo" without first confirming such a file exists — it does not.
**Phase to address:** This phase, DOWNLOADS-01/D-07/D-08 planning.

## Code Examples

### The 12 Misc entries — verbatim registry shape to port (predecessor source, read in full this session)

```csharp
// Source: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/MiscViewModel.cs:95-111
// (AddCmdAdmin — representative of the 12-entry pattern; every entry follows this
// CreateSubKey→SetValue("")→SetValue("Icon")→CreateSubKey("Command")→SetValue("") shape)
private static void AddCmdAdmin()
{
    foreach (var root in new[]
    {
        @"Directory\Shell\OpenElevatedCMD",
        @"Drive\Shell\OpenElevatedCMD",
        @"LibraryFolder\background\Shell\OpenElevatedCMD",
        @"Directory\Background\Shell\OpenElevatedCMD"
    })
    {
        using var key = Registry.ClassesRoot.CreateSubKey(root);
        key.SetValue("", "Open CMD As Administrator");
        key.SetValue("Icon", "imageres.dll,-5324");
        using var cmd = key.CreateSubKey("Command");
        cmd.SetValue("", @"Powershell.exe -windowstyle hidden -Command ""Start-Process cmd.exe -ArgumentList '/s,/k,pushd,%V' -Verb RunAs""");
    }
}

// Port target — using IRegistryService, no new primitives:
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

### Take Ownership — the one entry requiring D-11's confirmation gate

```csharp
// Source: C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/MiscViewModel.cs:131-150
// Grants icacls *S-1-3-4:F (Everyone:FullControl) recursively — the genuinely destructive
// entry per D-11. The Remove path is symmetric DeleteSubKeyTree, already registry-squatting-safe.
private static void AddTakeOwnership()
{
    using var file = Registry.ClassesRoot.CreateSubKey(@"*\shell\TakeOwnership");
    file.SetValue("", "Take Ownership");
    file.SetValue("HasLUAShield", "");
    // ... (NoWorkingDirectory, NeverDefault, command with takeown+icacls /grant *S-1-3-4:F)
}
```
D-11's confirmation gate wraps the ViewModel's `Add("take_own")` call site only — matching `DebloatViewModel.ExecuteAsync`'s existing `action.RequiresConfirmation && !isUndo` pattern [VERIFIED: src/AkariToolbox.App/ViewModels/DebloatViewModel.cs:79-91]:
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
    RestartExplorer();
}
```

### D-10's 13th entry — the "Clean" branch's 10 registry operations, direct-portable (no PowerShell)

```powershell
# Source: C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/4 Context Menu.ps1:21-56 (Clean/Add branch)
cmd /c "reg add `"HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32`" /ve /t REG_SZ /d `"`" /f"
cmd /c "reg add `"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer`" /v `"NoCustomizeThisFolder`" /t REG_DWORD /d `"1`" /f"
cmd /c "reg delete `"HKCR\Folder\shell\pintohome`" /f"
# ... (7 more reg add/delete calls, all plain string/DWORD values)
```
```csharp
// Port target — direct IRegistryService calls, no reg.exe/regedit.exe process spawn
private void AddClassicContextMenu()
{
    _registry.SetValue(RegistryHive.CurrentUser,
        @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "", RegistryValueKind.String);
    _registry.SetValue(RegistryHive.LocalMachine,
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoCustomizeThisFolder", 1, RegistryValueKind.DWord);
    _registry.DeleteSubKeyTree(RegistryHive.ClassesRoot, @"Folder\shell\pintohome");
    // ... remaining 7 operations, same 1:1 mapping
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|---------------|--------|
| Predecessor's raw `Registry.ClassesRoot`/`Registry.LocalMachine` calls scattered in `MiscViewModel.cs` | `IRegistryService` abstraction (already built Phase 1) | Phase 1 of this port | Enables unit testing of `ContextMenuService` with a fake `IRegistryService`, per ARCHITECTURE.md's Two-tier service abstraction pattern |
| Predecessor's `AppInstallerService` as an un-injected `sealed class` instantiated directly | DI-registered `IAppInstallerService` singleton, matching `IDebloatCatalog`'s registration pattern | This phase | Consistent with every other service in the app; enables future test doubles |
| Winget package IDs guessed from training data (`PotPlayer.PotPlayer`, `Rockstar.RockstarGamesLauncher`) | Live-verified IDs (`Daum.PotPlayer`, `RockstarGames.Launcher`) | This research session | Guessed IDs would fail every install silently unless exit codes are checked — verified IDs work on the first try |

**Deprecated/outdated:** None — this phase has no framework/library version drift to account for; every primitive it touches was already current as of Phase 1-3's stack research.

## Assumptions Log

| # | Claim | Section | Risk if Wrong |
|---|-------|---------|---------------|
| A1 | Recommending `.NA` as the default region for League of Legends/Valorant winget IDs | Common Pitfalls #2, Open Questions | If the target audience is not NA-majority, the installed client defaults to the wrong game server region — low functional risk (user can still play, just via a different regional client that may need re-auth), but worth a locked decision rather than silent default |
| A2 | Recommending the 13 new-apps-minus-2-duplicates count as the corrected D-02 scope | Open Questions | If the user actually wants "Epic Games"/"GOG Galaxy" re-added as intentional duplicates for some reason (e.g. a different flavor/branding), dropping them would under-deliver against a literal reading of D-02 — low risk, since a duplicate winget ID row is very unlikely to be an intentional decision, but flagging for explicit confirmation rather than silently deciding |
| A3 | Recommending `checkpoint:human-verify` gates for Nvidia App / League of Legends / Valorant rather than blocking the whole phase on them | Package Legitimacy Audit, Open Questions | If the planner instead wants Escape From Tarkov/Nvidia App/League/Valorant dropped from scope entirely rather than gated, this recommendation overshoots — low risk, reversible at planning time |
| A4 | D-04 hardening steps should be authored as embedded `.ps1` fragments (Pattern 2/`IScriptRunner`) rather than ported to C#, unlike the 13th Misc entry which IS recommended for direct C# port | Architecture Patterns §4 vs §6 | If the planner disagrees and wants the hardening steps also hand-ported to C# (since many are pure registry/service/scheduled-task operations that `IRegistryService`/`IWindowsServiceController`-equivalent could handle), the embedded-script approach is not wrong, just more conservative/parity-preserving — low risk, a legitimate implementation choice either way |

**If this table is empty:** N/A — table is populated above.

## Open Questions

1. **League of Legends / Valorant have no generic winget ID — which region should the catalog default to?**
   - What we know: Confirmed via `winget search` this session — only region-suffixed IDs exist (`RiotGames.LeagueOfLegends.NA/.EU/.EUNE/.JP/.KR/.LA1/.LA2/.OC1/.PBE`, similarly for Valorant `.AP/.BR/.EU/.KR/.LATAM/.NA`).
   - What's unclear: Which region Akari OS's actual user base needs — this is a product/audience question, not a technical one.
   - Recommendation: Default to the `.NA` variant (matching the existing catalog's English-only, US-CDN-sourced app conventions) and label the catalog row "League of Legends (NA)"/"Valorant (NA)" so the region is visible, not hidden. Flag as a locked decision needed from the user during `/gsd-discuss-phase` follow-up or accept as Claude's discretion if not re-opened.

2. **Escape From Tarkov has no winget package at all — how should D-03's "winget ID, not CDN download" mandate apply to this one app?**
   - What we know: Confirmed via `winget search "Tarkov"`/`"Escape from Tarkov"`/`"Battlestate"` this session — zero results. Battlestate Games does not publish to winget.
   - What's unclear: Whether the user wants (a) this one app dropped from the 15-app scope entirely (reducing to 12 apps that actually support D-03's mechanism), or (b) this one app kept as an explicit, documented exception to D-03 using the source script's original direct-CDN-download method (`IWR "https://prod.escapefromtarkov.com/launcher/download" -OutFile ...`).
   - Recommendation: Treat as an explicit, single-app exception to D-03 (option b) — the user's D-03 rationale was "consistent single install mechanism," but a hard technical blocker (no winget package exists) is a different situation than a style preference; dropping a named, locked-decision app silently seems worse than one clearly-flagged exception. Confirm with the user during planning if this reasoning is contested.

3. **"Epic Games" and "GOG Galaxy" in D-02's list are literal duplicates of existing catalog rows — drop them?**
   - What we know: Verified this session — `EpicGames.EpicGamesLauncher` and `GOG.Galaxy` already exist verbatim in the predecessor's ported 28-app catalog (Gaming category), with the exact same winget IDs the new-app research would otherwise produce.
   - What's unclear: Whether this was a deliberate oversight in the CONTEXT.md discussion (the discussion's "9 already-present" count and its 6 named examples don't fully enumerate all 9, missing these two plus Notepad++) or an intentional inclusion for some unstated reason.
   - Recommendation: Drop both from the "new apps to add" list during planning — reduces D-02's addition count from 15 to 13 (12 if Escape From Tarkov is also dropped per Question 2's option a). Document this correction explicitly in the plan so it's traceable back to this research, not silently different from CONTEXT.md's literal text.

4. **Nvidia App's Microsoft Store-only listing — install via winget's msstore source, or skip entirely?**
   - What we know: No plain `winget`-source package exists; only `XP8CLZL93F5Z4P` via the `msstore` source, which may have different `--silent`/agreement-acceptance semantics than the rest of the catalog's EXE/MSI-based winget installs.
   - What's unclear: Whether msstore-sourced silent install actually works headlessly in this app's elevated, unpackaged context (untested this session — no VM available to actually run `winget install --source msstore` and observe behavior).
   - Recommendation: Include it, but gate behind a `checkpoint:human-verify` task in the plan (per the Package Legitimacy Audit) so the executor confirms msstore silent-install actually completes without a stuck consent prompt before considering the catalog entry done.

5. **D-08's SHA256 manifest — who/what authors the ~130-entry hash list, and when?**
   - What we know: No upstream manifest exists to fetch (confirmed via live GitHub API call). The manifest must be authored by downloading all ~130 files and hashing them.
   - What's unclear: Whether this authoring step happens as part of this phase's plan execution (an executor task that runs a one-time script against the live GitHub repo and commits the resulting manifest), or is treated as a separate manual/ops task outside the GSD phase workflow.
   - Recommendation: Include it as an explicit plan task — a small one-off C#/PowerShell utility (not shipped in the app) that downloads each `AllFiles` entry, computes SHA256, and emits a JSON manifest embedded as a resource. Budget real time for this — it is a ~30MB download over ~130 files, not instant.

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| `winget` CLI | DOWNLOADS-02 (app installer catalog, all 43 apps) | ✓ | v1.29.290 (confirmed this session via `winget --version`) | — |
| Network access to `raw.githubusercontent.com` | DOWNLOADS-01 (PostInstall mirror) | ✓ (confirmed via live GitHub API call this session) | — | Already handled: `IsFullyInstalled`/`EnsurePostInstallAsync` degrade gracefully (per-file try/catch, non-zero `failed` count logged, no unhandled exception) [VERIFIED: src/AkariToolbox.App/Services/PostInstallService.cs:196-224, 226-242] |
| `explorer.exe` process (for context-menu Add/Remove's restart step) | MISC-01 (all 13 entries) | ✓ (standard on any interactive Windows session) | — | Predecessor's `RestartExplorer()` assumes at least one `explorer` process exists — matches `IRegistryService.OpenRealUserHive`'s existing hard-throw-if-absent convention elsewhere in this codebase (D-14 precedent), so no new fallback needed |

**Missing dependencies with no fallback:** None.
**Missing dependencies with fallback:** None — all three dependencies above are confirmed present/handled.

## Security Domain

### Applicable ASVS Categories

| ASVS Category | Applies | Standard Control |
|---------------|---------|-----------------|
| V2 Authentication | No | This phase has no auth surface |
| V3 Session Management | No | N/A |
| V4 Access Control | Yes | Take Ownership's `icacls /grant *S-1-3-4:F` is a broad ACL grant — mitigated by D-11's confirmation dialog, already the correct control for this risk |
| V5 Input Validation | Yes | All catalog/registry values are static, compiled-in strings (winget IDs, registry paths) — no user-supplied input flows into any `Process.Start`/`RegistryKey` call in this phase, eliminating injection risk by construction (matches the existing Debloat/Tweaks pattern) |
| V6 Cryptography | Yes | SHA256 verification (D-07) for PostInstall downloads — `System.Security.Cryptography.SHA256` (already used, in-box, never hand-rolled) |

### Known Threat Patterns for this phase's stack

| Pattern | STRIDE | Standard Mitigation |
|---------|--------|---------------------|
| Compromised/spoofed PostInstall GitHub mirror serving malicious assets | Tampering | HTTPS (`raw.githubusercontent.com`, already used) + D-07's SHA256 pinned-manifest verification (this phase's actual new work) — matches PITFALLS.md's "Downloading PostInstall assets over plain HTTP or without verifying" security mistake, now closed by D-07 |
| Registry-squatting via context-menu key creation under a namespace another process claims | Tampering / Elevation of Privilege | `IRegistryService.SetValue`'s open-then-create pattern (already implemented, no new work) — matches ARCHITECTURE.md Anti-Pattern 4/PITFALLS.md's registry-squatting guidance |
| Overly broad ACL grant via Take Ownership | Elevation of Privilege | D-11's `IDialogService.ConfirmAsync` gate before applying — already the correct, already-decided mitigation; no additional control needed |
| Winget package-ID confusion (installing a wrong/malicious lookalike package due to a typo'd ID) | Spoofing | This session's live `winget show --exact`/`winget search` verification against the real winget catalog for all 15 new IDs — closes this risk for the apps in this phase; any future catalog additions should repeat this verification step, not trust a training-data guess |
| Embedded D-04 hardening `.ps1` scripts tampered post-build | Tampering | Same mitigation as the existing 28 Debloat scripts — scripts are embedded assembly resources (not loose sidecar files), extracted to temp only at run time via `IScriptRunner` — no new risk surface introduced beyond what's already accepted for Debloat |

## Sources

### Primary (HIGH confidence)
- Direct reading of current codebase this session: `IPostInstallService.cs`, `PostInstallService.cs`, `IRegistryService.cs`, `RegistryService.cs`, `IScriptRunner.cs`, `IDialogService.cs`, `DebloatCatalog.cs`, `DebloatAction.cs`, `DebloatActionItem.cs`, `DebloatViewModel.cs`, `DefenderTweakHandler.cs`, `App.xaml.cs`, `HomeViewModel.cs`, `TweakHandlerRegistration.cs`, `ILogConsoleService.cs`, `INavigationAware.cs`, `PostInstallIntegrityTests.cs`, `.planning/config.json`
- Direct reading of predecessor codebase this session: `DownloadsViewModel.cs`, `AppInstallerService.cs`, `AppItem.cs`, `MiscViewModel.cs` (full file, all 12 Add/Remove pairs)
- Direct reading of new source scripts this session: `4 Installers/1 Installers.ps1` (full file, all 24 branches), `6 Windows/4 Context Menu.ps1` (full file, both branches)
- `winget --version`, `winget show --id <id> --exact`, `winget search "<query>"` — run live against the installed winget CLI (v1.29.290) this session for all 15 new apps
- `api.github.com/repos/isleap9/PostInstall/contents/PostInstall` — live GitHub API call this session, confirming no checksum manifest exists upstream

### Secondary (MEDIUM confidence)
- `.planning/research/STACK.md`, `ARCHITECTURE.md`, `FEATURES.md`, `PITFALLS.md` — project-level research from 2026-08-31, cross-referenced for patterns (two-tier service abstraction, PostInstall self-heal data flow, registry-squatting guidance)
- `.planning/PROJECT.md` — Key Decisions table, confirming `github.com/isleap9/PostInstall` repo identity and the GAMING-02/PostInstall-deprecation scoping distinction

### Tertiary (LOW confidence)
- None — every claim in this document was either read directly from a source file this session or verified via a live tool call.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages, every primitive already read directly in the codebase
- Architecture: HIGH — direct-carry pattern confirmed by reading both the predecessor source and the current codebase's existing analogous services (`DebloatCatalog`/`DebloatViewModel`)
- Pitfalls: HIGH — all 5 pitfalls are grounded in this session's live verification (winget CLI results, GitHub API call), not speculation

**Research date:** 2026-09-02
**Valid until:** 30 days for the codebase-internal findings (stable); winget package ID verification (Standard Stack/Legitimacy Audit) should be re-checked if planning is delayed more than ~2 weeks, since winget catalog entries and app publishers' packaging can change

---
*Phase: 4-Downloads & Misc*
*Researched: 2026-09-02*
