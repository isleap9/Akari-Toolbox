# Phase 4: Downloads & Misc - Context

**Gathered:** 2026-09-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 4 delivers two pages: a Downloads page combining (a) the already-built `IPostInstallService` self-healing `C:\PostInstall\` asset mirror (DOWNLOADS-01 — backend complete since Phase 1, needs only UI wiring + trigger), auto-run silently on first visit with SHA256 integrity verification, and (b) the predecessor's winget-based app-installer catalog, kept as the Downloads page's core UI/mechanism and expanded with 15 new apps sourced from the "Ultimate" collection's `4 Installers/1 Installers.ps1`. A Misc page ports the predecessor's 12 context-menu add/remove entries (MISC-01) and adds a 13th entry — a classic-context-menu-restore toggle sourced from `6 Windows/4 Context Menu.ps1` — as a new, distinct decluttering feature, not a replacement for the 12. MISC-02 ("extra misc tools") has no existing implementation anywhere to port from and is explicitly deferred.

Requirements covered: DOWNLOADS-01, DOWNLOADS-02, MISC-01, MISC-02 (MISC-02 deferred — see `<deferred>`).

**Key discovery from this discussion:** REQUIREMENTS.md's DOWNLOADS-02 wording ("playbooks, drivers, and recommended utility links") does not match anything the predecessor's actual `DownloadsPage`/`DownloadsViewModel` implements — that code is a winget app-installer catalog (28 apps across Browsers/Comms/Dev/Gaming/Utilities categories), unrelated to PostInstall folder browsing or "links." The user confirmed: keep the app-installer catalog as DOWNLOADS-02's real shape; the "playbooks/drivers/links" wording in REQUIREMENTS.md is stale copy from the predecessor's Home-card description text, not an accurate feature spec.

</domain>

<decisions>
## Implementation Decisions

### Downloads Page — App Installer Catalog
- **D-01:** The predecessor's winget-based app-installer catalog (`DownloadsViewModel`/`AppInstallerService` — searchable/filterable ItemsControl, categories, multi-select + "Install Selected") is ported as the Downloads page's core UI and mechanism, NOT replaced. This is DOWNLOADS-02's real implementation despite REQUIREMENTS.md's stale "playbooks, drivers, and utility links" wording — the user confirmed "keep the downloads the same."
- **D-02:** 15 new apps are added to the catalog, sourced from `4 Installers/1 Installers.ps1`'s 24-entry list minus the 9 already present in the existing catalog (Chrome, Firefox, Discord, Steam, Brave, 7-Zip already exist as winget entries — do not duplicate): Roblox, Battle.net, Electronic Arts, Epic Games, Escape From Tarkov, Frame View, GOG Galaxy, League of Legends, Nvidia App, OBS Studio, Onboard Memory Manager, PotPlayer, Rockstar Games, Ubisoft Connect, Valorant.
- **D-03:** New apps install via winget package ID (matching the existing catalog's mechanism), NOT the source script's direct-CDN-download method — consistent single install mechanism across the whole catalog. — **Reversibility:** reversible — swapping an app's install path from winget-ID to direct-download later is a localized change per catalog entry, not a mechanism-wide rewrite.
- **D-04:** After each new app installs via winget, port `1 Installers.ps1`'s per-app post-install hardening steps (disable telemetry/hardware-acceleration where the script does it, remove auto-launch-at-login registry/scheduled-task entries, delete vendor-installed scheduled tasks/services the script removes, clean up shortcut placement) as a follow-up step — this is the real privacy/perf value-add the source script provides beyond a bare winget install, and the user wants it kept, not dropped for simplicity.
- **D-05 [Claude's Discretion]:** Exact category assignment for the 15 new apps (most are "Gaming"; Frame View/OBS Studio/PotPlayer/Onboard Memory Manager could be "Gaming" or a new "Utilities"/"Streaming" grouping) — Claude/research proposes during planning.

### Downloads Page — PostInstall Mirror
- **D-06:** `IPostInstallService.EnsurePostInstallAsync()` (already built, Phase 1) is wired to auto-trigger silently on first Downloads-page visit — matches the predecessor's on-demand internal-invocation pattern (it was never a page action in the predecessor, just called internally by other features).
- **D-07:** Each downloaded file is verified via the already-built `IPostInstallService.VerifyFileSha256Async` before being treated as successfully mirrored — new integrity guarantee the predecessor never had. This explicitly diverges from Phase 2's D-06 "no added verification" precedent for other downloaded content: the primitive already exists here (unlike Phase 2's driver-tool scripts), so using it is not extra work, and the user confirmed they want it used. — **Reversibility:** reversible — the verification call can be removed later without touching the download/mirror logic itself.
- **D-08 [Claude's Discretion]:** Exact per-file expected-SHA256 source (a checked-in manifest alongside the file-path list, vs. fetched from the GitHub repo at runtime) — technical implementation, not user vision. Research during planning.

### Misc Page — Context Menu Entries
- **D-09:** The predecessor's 12 `MiscViewModel` custom-command context-menu entries (Open CMD as Admin, Open PowerShell as Admin, Take Ownership, Control Panel shortcut, File Hash submenu, Kill Not Responding, Windows Tools, Shut Down menu, .pow file association, Run with Priority, Change Resolution, Reboot to BIOS) are ported as direct carries — matches MISC-01 exactly (12 entries, independent add/remove per entry).
- **D-10:** `6 Windows/4 Context Menu.ps1` (classic-menu-restore + built-in-shell-item declutter — a single Clean/Default 2-state toggle, NOT 12 independent entries) is added as a 13th Misc entry, alongside the original 12, not replacing any of them. Confirmed explicitly by the user after initial ambiguity — this script does something structurally different (Windows-11-menu-behavior restoration + built-in-item removal) from the predecessor's 12 "add a custom command" entries, so both stand.
- **D-11:** Only the "Take Ownership" entry gets a confirmation dialog (Phase 3's `IDialogService.ConfirmAsync` pattern) before applying — it recursively grants broad ACL permissions (`icacls /grant *S-1-3-4:F /t`) and is the one genuinely destructive entry if run against the wrong folder. The other 12 (including the new Context Menu.ps1 toggle) stay zero-friction, matching the predecessor's one-click behavior — they're additive/reversible shell entries with low blast radius.

### Misc Page — Extra Tools (MISC-02)
- **D-12 [informational]:** MISC-02 ("extra misc tools") has no existing implementation anywhere in the predecessor or the "Ultimate" collection to port from — the user confirmed to defer it, not invent content. See `<deferred>`.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### New source — Ultimate collection additions (from this discussion)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/4 Installers/1 Installers.ps1` — 24-app installer menu; source for D-02's 15 new catalog entries and D-04's post-install hardening steps. Self-elevating, `Read-Host` console-menu shape (same extraction technique as Phase 2/3's D-04/D-06-style scripts) — strip the menu, keep each branch's download+install+hardening body.
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/4 Context Menu.ps1` — 2-branch classic-menu-restore/declutter toggle; source for D-10's 13th Misc entry. Branch 1 ("Clean/Recommended") is the enabled/on state, branch 2 ("Default") is off.

### Predecessor source (port from, read-only)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/DownloadsViewModel.cs` — the app-installer catalog (28 existing apps, category filter, search, multi-select) — D-01's port target
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/AppInstallerService.cs` — winget install invocation logic backing `DownloadsViewModel` (not yet read this discussion — read in full during research)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Models/AppItem.cs` — app catalog entry model shape (not yet read this discussion — read during research)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/MiscViewModel.cs` — all 12 context-menu Add/Remove implementations (raw `Microsoft.Win32.Registry` calls) — D-09's port target, read in full this discussion
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Views/DownloadsPage.xaml` / `MiscPage.xaml` — UI layout reference (not yet read this discussion — read during research)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/PostInstallService.cs` — the original static PostInstallService this phase's `IPostInstallService` was ported from (confirms the ~140-file manifest and GitHub raw-content source URL)

### Current codebase — reusable primitives
- `src/AkariToolbox.App/Services/IPostInstallService.cs` / `PostInstallService.cs` — already-built, injectable port of the predecessor's static service; `EnsurePostInstallAsync()` (D-06) and `VerifyFileSha256Async()` (D-07) are ready to consume as-is, no new backend work needed
- `src/AkariToolbox.Framework/Services/IDialogService.cs` — for the Take Ownership confirmation dialog (D-11), same pattern as Phase 3's D-11
- `src/AkariToolbox.Framework/Services/IScriptRunner.cs` — likely how the extracted `1 Installers.ps1`/`4 Context Menu.ps1` branch bodies get invoked, matching Phase 2/3's embedded-resource-extract-and-run pattern
- `src/AkariToolbox.App/Services/DebloatCatalog.cs` — reference for the Run(+Undo)-catalog pattern if Misc's 13 entries end up modeled as a catalog rather than a flat ViewModel list (Claude's call during planning)

### Project-level docs
- `.planning/PROJECT.md` — Key Decisions table; Phase 1 D-13 flagged the elevation-safe `IFilePickerService` as having no real UI consumer yet, "deferred to whichever future phase (likely Downloads/Misc) first wires an actual picker-using feature" — evaluate during planning whether this phase is that consumer (e.g. picking a custom install/mirror destination), though no such need surfaced in this discussion
- `.planning/REQUIREMENTS.md` — DOWNLOADS-01/02, MISC-01/02 exact wording; DOWNLOADS-02's "playbooks, drivers, and recommended utility links" phrase is stale/inaccurate per this discussion's D-01 finding — flag for a REQUIREMENTS.md follow-up edit correcting the description to match the actual app-installer-catalog shape (same pattern as Phase 2's D-12 GAMING-02 retirement note); MISC-02 needs a similar follow-up per D-12
- `.planning/ROADMAP.md` — Phase 4 success criteria
- `.planning/phases/02-gaming-tweaks/02-CONTEXT.md` — D-01 established that `C:\PostInstall\` is "fully deprecated and no longer maintained by the user" for Gaming Tweaks purposes specifically; this phase's DOWNLOADS-01 mirror work is NOT in conflict with that — D-01 there was scoped to "Gaming Tweaks must not depend on it," not a statement that the mirror concept itself is obsolete. D-08 there also flagged unclaimed `6 Windows` scripts as Debloat/Misc candidates — `4 Context Menu.ps1` (D-10 here) is one of those.
- `.planning/phases/01-foundation-akari-os-tweaks/01-CONTEXT.md` — D-13, the picker consumer note referenced above

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `IPostInstallService`/`PostInstallService` — fully built, DI-registered, ready to consume (D-06/D-07) — no new backend work for DOWNLOADS-01
- Phase 2/3's non-interactive-menu-extraction technique (strip `Read-Host`/`Write-Host` menu scaffolding, invoke the chosen branch's body directly) — needed for both `1 Installers.ps1` (24 branches, using 15) and `4 Context Menu.ps1` (2 branches)
- Phase 3's `IDialogService.ConfirmAsync` confirmation-gate pattern — reference for D-11's Take Ownership gate

### Established Patterns
- Predecessor's `DownloadsViewModel` category-filter + search `ICollectionView` pattern — reasonable to translate directly to WinUI 3, extending `Categories`/`SeedApps()` with the 15 new entries (D-02)
- Predecessor's `MiscViewModel` flat `Add(key)`/`Remove(key)` switch-dispatch over raw `Microsoft.Win32.Registry` calls, followed by an explorer.exe kill+restart — direct-carry pattern for the 12 existing entries (D-09); the new 13th entry (D-10) needs the same Add/Remove shape even though its underlying script only has 2 branches, not a natural add/remove pair (Claude's call: map "Clean/Recommended" → Add/On, "Default" → Remove/Off)

### Integration Points
- Home dashboard's "Downloads" and "Misc" cards (already present, disabled, per Phase 1 D-09/D-10) flip to enabled once this phase ships
- `App.xaml.cs` DI container already registers `IPostInstallService` — no new registration needed for that half of Downloads; the app-installer half needs whatever `AppInstallerService`-equivalent gets ported

</code_context>

<specifics>
## Specific Ideas

- The 15 new Downloads apps and their likely winget package IDs (to confirm during research, not all verified this discussion): Roblox (`Roblox.Roblox`), Battle.net (`Blizzard.BattleNet`), Epic Games (`EpicGames.EpicGamesLauncher`), GOG Galaxy (`GOG.Galaxy`), League of Legends (Riot's launcher has no stable winget ID historically — verify), OBS Studio (`OBSProject.OBSStudio`), Steam already present. EA app, Escape From Tarkov, Frame View, Nvidia App, Onboard Memory Manager, PotPlayer, Rockstar Games, Ubisoft Connect, Valorant winget-ID availability is unconfirmed — research task.
- `1 Installers.ps1`'s hardening steps are per-app and vary in kind (registry policy writes, scheduled-task removal, service removal, shortcut relocation/cleanup, config-file seeding) — not a single reusable helper, each app's block needs its own porting pass.
- `4 Context Menu.ps1`'s "Clean" branch touches ~10 distinct registry locations (CLSID override, NoCustomizeThisFolder policy, pintohome, pintohomefile, Compatibility handler, 3 Shell-Extensions-Blocked GUIDs, Library Location handler, ModernSharing handler, NoPreviousVersionsPage policy, SendTo handlers) — its "Default" branch reverses each one individually (not always a clean delete/re-add pair — e.g. Compatibility handler needs importing a specific CLSID value back, not just deleting a block value).

</specifics>

<deferred>
## Deferred Ideas

- **MISC-02 ("extra misc tools")** — no existing implementation anywhere (predecessor or Ultimate collection) to port from. User confirmed: defer rather than invent content. Needs a REQUIREMENTS.md follow-up edit (same retirement/scope-note pattern as Phase 2's GAMING-02) — either mark MISC-02 as not satisfied by this phase's v1 scope, or leave open for a future phase once concrete tool candidates exist.
- The remaining 9 branches of `1 Installers.ps1` not pulled into D-02 (apps already present in the existing winget catalog: Chrome, Firefox, Discord, Steam, Brave, 7-Zip, plus 3 more from the 24-list that overlap) — no action needed, not deferred, just excluded as duplicates.
- The other 4 scripts in `4 Installers/` (`2 MSI Afterburner.ps1`, `3 Nvidia Profile Inspector.ps1`, `4 More Clock Tool.ps1`, `5 CRU SRE.ps1`) — not examined this discussion, not pulled into scope. Possible future Downloads-catalog or Gaming-Tweaks-adjacent candidates if a later discussion wants them.

</deferred>

---

*Phase: 4-Downloads & Misc*
*Context gathered: 2026-09-02*
