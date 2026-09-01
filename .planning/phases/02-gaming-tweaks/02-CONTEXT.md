# Phase 2: Gaming Tweaks - Context

**Gathered:** 2026-09-01
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 2 delivers a fully rebuilt Gaming Tweaks page — gaming/latency/service tweaks as live-state toggles (same real-read/real-revert guarantee as Phase 1's 32 Akari OS Tweaks), plus SvcHost split threshold and Win32 Priority Separation dropdowns. Unlike the rest of the port, this is NOT a parity port of the predecessor's `GamingTweaksViewModel`/`GamingTweaksPage.xaml.cs` — mid-discussion the user confirmed the predecessor's `C:\PostInstall\` asset mirror is fully deprecated and no longer maintained, so every predecessor gaming feature that depended on it (HDCP/network-optimization bat scripts, the Services-preset dropdown, the ~29-button third-party tool launcher grid) is replaced or dropped. The new toggle set is sourced from native PowerShell scripts the user supplied from their personal "Ultimate" tweak collection (`C:\Users\isleap\Desktop\AkariOS Tweaks\`), specifically the `5 Graphics` folder and a 6-script subset of `6 Windows`.

Requirements covered: GAMING-01. GAMING-02 is retired by this discussion (see D-12) — flagged for a REQUIREMENTS.md follow-up edit.

</domain>

<decisions>
## Implementation Decisions

### PostInstall Deprecation & Page Revamp
- **D-01:** `C:\PostInstall\` is fully deprecated and no longer maintained by the user. Gaming Tweaks must not depend on it in any form — no reading from, writing to, or launching files under that path. — **Reversibility:** one-way — explicit project-owner direction reversing the predecessor's asset-sourcing model for this page; re-introducing a PostInstall dependency here would need an explicit new decision.
- **D-02:** The Gaming Tweaks page is rebuilt from scratch, not ported 1:1 from `GamingTweaksViewModel.cs`/`GamingTweaksPage.xaml.cs`. The predecessor's dropdown mechanism (private `HKCU\Software\AkariTool` index) and `HasState`/`SaveState` toggle pattern are reference-only — not carried over, superseded by the Phase 1 `ITweakHandler` live-state pattern.
- **D-03:** Predecessor's `TweakService.SetHdcp`/`SetNetworkOptimization` (bat-script calls under `C:\PostInstall\...`) are NOT ported — replaced entirely by the native scripts below.

### Toggle Source: `5 Graphics` folder (13 scripts, 3 categories)
- **D-04:** 5 local, registry-only scripts become live-state toggle tweaks via the existing `ITweakHandler` pattern (same live-read/real-prior-value-revert rule as Phase 1's 32 tweaks): `7 Hdcp.ps1`, `8 P0 State.ps1`, `9 Msi Mode.ps1`, `5 Amd Settings.ps1`, `6 Intel Settings.ps1`.
- **D-05:** `12 Resolution Refresh Rate.ps1` and `13 Hags Windowed.ps1` are one-shot launch-shortcut actions (`Start-Process ms-settings:display` / `ms-settings:display-advancedgraphics`), not stateful toggles — no live-state read/revert applies to these two.
- **D-06:** 6 network-dependent scripts (`1 Driver Clean.ps1`, `2 Driver Install Latest.ps1`, `3 Driver Install Debloat & Settings.ps1`, `4 Nvidia Settings.ps1`, `10 DirectX.ps1`, `11 C++.ps1`) keep their live download-and-install behavior exactly as authored (they fetch installer binaries from GitHub, e.g. `FR33THYFR33THY/Ultimate-Files`, and run them). **No added SHA256/signature verification for v1** — explicit user choice, despite PROJECT.md's threat model flagging admin-run downloaded binaries as the app's top-severity risk class. — **Reversibility:** reversible — verification can be layered on later as a hardening pass without changing the UI/toggle contract.

### Toggle Source: `6 Windows` folder (scoped subset)
- **D-07:** Exactly 6 of `6 Windows`'s 36 scripts are in scope — `25 Device Manager Power Savings & Wake.ps1`, `26 Network Adapter Power Savings & Wake.ps1`, `27 Network IPv4 Only.ps1`, `28 Write Cache Buffer Flushing.ps1`, `29 Power Plan.ps1`, `30 Timer Resolution.ps1` — all local-only (no network calls), become live-state toggle tweaks via `ITweakHandler`, same rule as D-04.
- **D-08:** The remaining 30 scripts in `6 Windows` (Bloatware checks, Start Menu, Theme, Widgets, Copilot, Edge/WebView, Notepad, Control Panel, UAC, Core Isolation, Defender Optimize, Autoruns Startup, Cleanup, Restore Point, etc.) are explicitly OUT of Gaming Tweaks scope — likely candidates for Debloat (Phase 3) or Misc (Phase 4) discussions later. Not decided now.

### Dropdowns
- **D-09:** SvcHost split threshold and Win32 Priority Separation dropdowns are kept (direct registry writes, never PostInstall-dependent), but enhanced with more preset values than the predecessor's fixed lists (predecessor: Default/4GB/8GB/16GB/32GB/64GB for SvcHost; 5 hex values for Win32 priority). Exact expanded value list is Claude's/research's call — propose during planning for user approval, not specified now.
- **D-10:** The "Services preset" dropdown (AkariOS Default vs Windows Default, PostInstall-`.reg`-dependent) is dropped entirely — not replaced by anything. `8 Advanced\17 Services.ps1` (a services on/off toggle + auto restore-point script found in the same "Ultimate" collection) was explicitly considered and declined as a replacement.

### Tool Launcher Grid & GAMING-02
- **D-11:** The predecessor's ~29-button NVIDIA/AMD/Useful-Tools quick-launch grid is dropped entirely — no third-party utility launcher in Gaming Tweaks v1.
- **D-12:** REQUIREMENTS.md's GAMING-02 ("User can launch quick-access tools for NVIDIA, AMD, and other third-party utilities from a launcher grid") is retired by D-11 — it will no longer be satisfied by this phase. **Needs a REQUIREMENTS.md/PROJECT.md follow-up edit** to mark GAMING-02 as superseded/retired rather than "Pending". Not edited during this discussion — flagged for the user/next step. — **Reversibility:** one-way — retiring a written v1 requirement; reinstating a launcher grid later needs a fresh scope decision.

### Claude's Discretion
- Exact expanded preset value lists for SvcHost/Win32 Priority Separation dropdowns (D-09) — Claude/research proposes for approval before/at planning.
- How each interactive, `Read-Host`-driven console script (D-04/D-07) gets adapted into a non-interactive toggle invocation (strip the menu loop and call the underlying registry/command logic directly, vs. pipe simulated input) — technical implementation, not user vision.
- Mapping each script's "Recommended" (option 1) vs "Default" (option 2) branch to the toggle's On/Off state — expected to map 1:1 (Recommended = On), Claude to verify per-script during planning.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### New native tweak source — `5 Graphics` (replaces PostInstall-dependent gaming logic)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/1 Driver Clean.ps1` — network-dependent (D-06)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/2 Driver Install Latest.ps1` — network-dependent (D-06)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/3 Driver Install Debloat & Settings.ps1` — network-dependent (D-06), 771 lines, largest script in scope
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/4 Nvidia Settings.ps1` — network-dependent (D-06)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/5 Amd Settings.ps1` — local toggle (D-04)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/6 Intel Settings.ps1` — local toggle (D-04)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/7 Hdcp.ps1` — local toggle (D-04)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/8 P0 State.ps1` — local toggle (D-04)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/9 Msi Mode.ps1` — local toggle (D-04)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/10 DirectX.ps1` — network-dependent (D-06)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/11 C++.ps1` — network-dependent (D-06)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/12 Resolution Refresh Rate.ps1` — one-shot shortcut (D-05)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/5 Graphics/13 Hags Windowed.ps1` — one-shot shortcut (D-05)

### New native tweak source — `6 Windows` (scoped subset only, D-07)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/25 Device Manager Power Savings & Wake.ps1`
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/26 Network Adapter Power Savings & Wake.ps1`
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/27 Network IPv4 Only.ps1`
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/28 Write Cache Buffer Flushing.ps1`
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/29 Power Plan.ps1`
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/30 Timer Resolution.ps1`

### Explicitly considered and declined (do not pull into this phase without a new decision)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/6 Windows/` — remaining 30 scripts outside the #25-30 subset (D-08)
- `C:/Users/isleap/Desktop/AkariOS Tweaks/8 Advanced/17 Services.ps1` — declined as the Services-dropdown replacement (D-10)

### Predecessor source (reference only — being replaced, not ported)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/GamingTweaksViewModel.cs` — old dropdown/toggle mechanism, reference only, superseded by D-02/D-09
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/TweakService.cs` (gaming section, ~lines 1058-1118) — old `SetPreemption`/`SetHdcp`/`SetNetworkOptimization` — reference only, superseded by D-03/D-04/D-06
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Views/GamingTweaksPage.xaml.cs` — 579-line code-behind anti-pattern; explicitly the structure NOT to replicate (PROJECT.md architecture-debt callout)

### Current codebase — Phase 1 pattern to extend
- `src/AkariToolbox.App/Services/ITweakHandler.cs`
- `src/AkariToolbox.App/Services/ITweakCatalog.cs`
- `src/AkariToolbox.App/Services/TweakCatalog.cs`
- `src/AkariToolbox.App/Services/TweakHandlerRegistration.cs`
- `src/AkariToolbox.App/Services/TweakHandlers/` (`RegistryTweaksBatchA.cs`, `RegistryTweaksBatchB.cs`, `ServiceBackedTweaks.cs`, `BcdeditDismTweaks.cs`, `WifiTweakHandler.cs`) — existing handler examples to model new gaming handlers on
- `src/AkariToolbox.Framework/Services/IWindowsServiceController.cs` — built ahead of this phase for service-backed tweaks
- `src/AkariToolbox.App/Services/IPostInstallService.cs` / `PostInstallService.cs` — confirmed NOT used by Gaming Tweaks (D-01); stays reserved for Phase 4 only

### Project-level docs
- `.planning/PROJECT.md` — Key Decisions table; architecture-debt callout on `GamingTweaksPage.xaml.cs` code-behind
- `.planning/REQUIREMENTS.md` — GAMING-01/GAMING-02 wording; **GAMING-02 needs a follow-up retirement edit per D-12**
- `.planning/ROADMAP.md` — Phase 2 success criteria
- `.planning/phases/01-foundation-akari-os-tweaks/01-CONTEXT.md` — origin of the D-03/D-04 live-state-read rule and the `ITweakHandler` pattern rationale

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `ITweakHandler`/`ITweakCatalog`/`TweakHandlerRegistration` — Phase 1's extension point; new gaming toggles register as more `ITweakHandler` implementations, no catalog-plumbing changes needed
- `IWindowsServiceController` — built with this phase's service-config needs in mind, still available even though the specific "Services preset" dropdown (D-10) is dropped
- Phase 1's script-runner primitive (`IScriptRunner`, referenced in 01-CONTEXT.md) — likely how the network-dependent driver-installer scripts (D-06) get invoked, matching the embedded-resource-extract-and-run pattern already proven in Phase 1

### Established Patterns
- Live registry read + real-prior-value revert (Phase 1 D-03/D-04) — extends to all new local toggles per D-04/D-07 above
- Each source script's `Recommended`/`Default` two-branch console-menu shape maps naturally to a boolean toggle: branch 1 = enabled/on state, branch 2 = default/off state

### Integration Points
- New `ITweakHandler` implementations register via `TweakHandlerRegistration` (same DI pattern as Phase 1's batches A/B)
- Home dashboard's "Gaming Tweaks" card (`HomeViewModel.cs`) and nav sidebar entry (`MainWindow.xaml.cs`) flip from `IsEnabled: false` to `true` once this phase ships — same mechanic already used for the other placeholder cards from Phase 1

</code_context>

<specifics>
## Specific Ideas

- Each of the 13 `5 Graphics` scripts and the 6 `6 Windows` scripts is a self-elevating, interactive console script with a numbered `Write-Host`/`Read-Host` menu (option 1 = recommended/on, option 2 = default/off). These need their menu/`Read-Host` wrapper stripped for non-interactive toggle invocation — the underlying registry/command logic per branch is what gets ported or wrapped.
- `7 Hdcp.ps1` iterates every GPU class subkey (`Get-ChildItem Registry::HKLM\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-...}`) rather than the predecessor's hardcoded `\0000`/`\0001` indices — a more robust multi-GPU-safe approach worth preserving as-is rather than reverting to hardcoded subkey paths.
- Network-dependent scripts (D-06) pull installer binaries from `https://github.com/FR33THYFR33THY/Ultimate-Files` per `1 Driver Clean.ps1` — confirm during research whether all 6 scripts use the same repo or different sources.

</specifics>

<deferred>
## Deferred Ideas

- The other 30 `6 Windows` scripts (Bloatware checks, Start Menu, Theme, Widgets, Copilot, Edge/WebView, Notepad, Control Panel, UAC, Core Isolation, Defender Optimize, Autoruns Startup, Cleanup, Restore Point) — likely Debloat (Phase 3) or Misc (Phase 4) material. Not decided now.
- `8 Advanced/17 Services.ps1` (on/off services toggle + auto restore-point creation) — declined as the Gaming Services-dropdown replacement (D-10); its restore-point-creation behavior is also relevant to the already-deferred v2 SAFE-01 requirement.
- The remaining ~97 scripts across the other "Ultimate" collection folders (`1 Check`, `2 Refresh`, `3 Setup`, `4 Installers`, `7 Hardware`, and `8 Advanced` minus `17 Services.ps1`) — untouched by this discussion, still v2/ULT-01 territory per PROJECT.md unless a future phase's discussion pulls specific ones forward the way Gaming Tweaks did here.

</deferred>

---

*Phase: 2-Gaming Tweaks*
*Context gathered: 2026-09-01*
