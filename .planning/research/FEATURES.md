# Feature Research

**Domain:** Windows system-tweak / debloat / gaming-optimization desktop utilities
**Researched:** 2026-08-31
**Confidence:** MEDIUM (web-sourced, cross-checked across multiple independent tools/reviews; no official vendor API docs exist for this category — see Sources)

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist in any credible tool in this category. Missing these makes the tool feel unsafe or amateurish compared to WinUtil, ShutUp10, Win11Debloat, and similar.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Toggle-based registry/service tweaks, grouped by category | Every reviewed tool (WinUtil "Tweaks" tab, ShutUp10's ~300 settings, Akari's existing 32 OS tweaks) presents tweaks as discrete on/off switches, not a monolithic script | LOW | Akari already has this pattern; port as-is |
| Instant state feedback per toggle (reflects actual current registry/service state, not just "last clicked" state) | Users distrust tools that show a toggle as ON when the underlying setting was reverted by a Windows Update or another tool — ShutUp10 Premium's differentiator is literally re-detecting drift | MEDIUM | Requires reading actual state on page load/navigation, not just persisting UI state — a common gap in weaker tools |
| Bulk bloatware/app removal list (Xbox apps, Copilot, Teams, Clipchamp, Bing apps, etc.) | WinUtil removes 40+ apps; this is the single most-searched-for feature in the category | LOW–MEDIUM | Akari's 28 PowerShell-backed debloat actions already cover this; keep PowerShell-backed approach (proven, matches ecosystem norm) |
| "Standard/safe" vs "advanced/risky" tweak separation | WinUtil explicitly buckets tweaks this way; reviewers call out tools that don't as unsafe for less-technical users | LOW | Akari's existing categorization (OS Tweaks vs Gaming Tweaks vs Debloat) partially achieves this — worth making risk level explicit per-toggle in UI copy/tooltips |
| Pre-change safety net (system restore point and/or reversibility) | Universally cited across every reviewed tool as the #1 safety practice; ShutUp10, WinUtil, and AppBuster all create a restore point automatically before applying changes | MEDIUM | Akari does not currently do this per PROJECT.md — see Anti-Features/pitfalls note below; strongly worth adding even within v1 parity scope since it's a safety property, not a new tweak |
| Context menu customization (add/remove entries) | Consistently offered by dedicated context-menu tools (Context Menu X, Windows 11 Classic Context Menu, Context Menu Manager) as a standalone feature category | LOW–MEDIUM | Akari's 12 context-menu entries already match this; note Windows 11's "modern" context menu can't be edited via simple registry entries — only the classic/legacy menu is registry-editable, which is presumably how the predecessor already does it |
| Elevation/admin requirement clearly required, not silently assumed | All comparable tools either self-elevate or clearly document the requirement, since registry/service/Defender changes fail silently or confusingly without it | LOW | Akari already requires `requireAdministrator` — carry forward |
| Quick-launch grid to companion utilities (GPU vendor tools, monitoring, benchmarking) | Gaming-tuning tools consistently pair tweaks with quick access to NVIDIA/AMD control panels and third-party utilities rather than reimplementing GPU tuning themselves | LOW | Akari already has this pattern — validates it as the right approach vs. building GPU overclocking natively |
| Windows Update / driver-safe tweaks that don't remove core plumbing | Tools that let users disable WU entirely or gut WebView2/Edge dependencies are the most-cited cause of breakage in post-mortems | N/A (behavior, not a feature) | Applies to what NOT to expose as a toggle — see Pitfalls below |

### Differentiators (Competitive Advantage)

Features that set a tool apart from the baseline WinUtil/ShutUp10 shape. Not required, but valuable — and several map directly to things Akari's predecessor already does that generic tools don't.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Two-phase guided Defender-disable workflow (tamper protection → real-time protection, with explicit warnings at each step) | Most competing tools either don't touch Defender at all (too risky/liability) or do it as a single blunt PowerShell one-liner with no guardrails; Akari's staged approach with visible phase state is more transparent and matches how Windows actually gates the change (tamper protection must be off before real-time protection can be toggled) | MEDIUM | Already exists in predecessor — port faithfully; this is a genuine differentiator worth calling out in UX (clear warning copy at each phase) rather than downplaying |
| Self-healing post-install asset downloader (mirrors a curated asset folder from GitHub, no-op if already present) | Generic debloat tools don't ship curated driver/playbook/tool bundles at all — this is closer to an OEM support-tool pattern (like a vendor's "recovery/setup" folder) than anything in WinUtil/ShutUp10 | MEDIUM | Existing `PostInstallService` pattern is sound (idempotent mirror-if-missing); worth keeping the "self-healing" framing since it directly matches Akari OS's actual deployment model |
| Curated GPU/gaming quick-launch grid bundling both toggle tweaks and third-party tool launchers in one page | Competing gaming-tweak tools (small single-purpose utilities like Win32PrioritySeparation toggles) are fragmented; Akari's single page consolidating SvcHost split threshold, Win32 priority separation, service config dropdowns, AND a launcher grid is more cohesive than the fragmented ecosystem | MEDIUM | Ecosystem confirms these are genuinely popular "ritual" tweaks even though their real-world FPS impact is debated — framing matters: present as "commonly recommended tuning" rather than overpromising measurable gains |
| Product identity tied to a specific OS distribution (Akari OS) with app-specific first-run behavior | No competing tool is bundled with/tailored to a specific custom Windows build — this is a structural differentiator, not a feature per se | LOW | Not a UI feature but shapes positioning; PostInstall mirror being a no-op on real Akari OS is a nice detail worth preserving |
| Deep "Ultimate" tweak tier (v2, ~110 scripts across Check/Refresh/Setup/Installers/Graphics/Windows/Hardware/Advanced + curated SHA256-verified third-party tool bundle) | Goes considerably beyond WinUtil/ShutUp10's scope into driver management, BIOS-adjacent guidance, and hardware overclocking — no mainstream competitor combines debloat + this depth of hardware tooling in one app | HIGH | Explicitly out of scope for v1 per PROJECT.md; flagged here only so the roadmap is aware this is where genuine differentiation will live in v2 |

### Anti-Features (Commonly Requested, Often Problematic)

Features that seem good but are consistently cited as sources of breakage or bad UX in this specific category.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|------------------|-------------|
| "Remove everything Microsoft-related" / nuke Microsoft Store, WebView2, or Edge dependencies | Users searching "debloat" often want maximum removal | The most consistently cited failure mode across post-mortems: gutting Store/WebView2/Edge breaks app installs, in-app browsers, and even unrelated Win32 apps that silently depend on WebView2; several tools have had to add "repair Store" instructions specifically because of this | Keep debloat scope to user-facing bloat apps (Copilot, Clipchamp, Xbox apps, promotional Store apps) and leave Store/WebView2/Edge runtime components alone; if Edge itself must go, document it as an advanced/risky action with explicit warning, not a default toggle |
| Disabling Windows Update entirely (vs. deferring/pausing) | Users want to stop unwanted feature updates or forced restarts | Tampering with Update/servicing stack is called out as one of the top five failure modes — leaves machines unpatchable and can break future updates/repairs in ways that are hard to reverse | Offer update *deferral*/active-hours controls (supported Windows mechanisms) rather than fully disabling the Update service/orchestrator |
| One-click "disable Defender permanently, no warnings" | Perceived as a quick performance/annoyance fix | High risk of malware exposure and, per security research, is one of the most common vectors abused when documented naively; blunt single-step disables (as opposed to guided two-phase) get flagged in security write-ups as dangerous patterns to publish | Keep the existing two-phase, warning-gated workflow; consider defaulting to Defender *exclusions* for specific folders as a lower-risk alternative surfaced alongside the full-disable option |
| Continuous background "auto-reapply my tweaks" monitoring/enforcement daemon (like ShutUp10 Premium's protection mode) | Sounds valuable — "keep my settings even after Windows Updates re-enable them" | Adds a persistent background service, additional attack surface, and complexity disproportionate to a debloat/tweak utility; ShutUp10 gates this behind a paid Premium tier specifically because it's a different product tier, not a baseline expectation | Ship a simple "re-check state" refresh action per page instead of a background daemon; document that a full re-run after major feature updates is expected (matches ecosystem norm) |
| Registry tweaks with no visible risk level or explanation ("just trust the toggle") | Reduces UI surface / feels simpler to build | Reviewers consistently penalize tools that don't distinguish safe/standard from advanced/risky tweaks — users can't make informed choices and blame the tool when something breaks | Carry forward category separation (OS Tweaks / Gaming Tweaks / Debloat / Misc) and add brief risk-level or explanatory copy per toggle where the predecessor lacks it |
| Native GPU overclocking / fan-curve / voltage control built into the app | Feels like a natural extension of "gaming tweaks" | Vendor GPU tools (NVIDIA/AMD control panels, MSI Afterburner) already do this safely with vendor-specific hardware knowledge; reimplementing it is high risk (hardware damage potential) for low differentiation | Keep the existing quick-launch grid pattern — link to vendor/third-party tools rather than reimplementing tuning logic |

## Feature Dependencies

```
[Two-phase Defender disable]
    └──requires──> [Elevation / requireAdministrator]
                       └──requires──> [App manifest change ported from framework default]

[Instant state feedback per toggle]
    └──requires──> [Registry/service state-reader service, called on navigation]
                       └──enhances──> [All toggle-based tweak pages: OS Tweaks, Gaming Tweaks, Debloat]

[Self-healing PostInstall asset downloader]
    └──requires──> [Network access + GitHub source availability]
    └──enhances──> [Downloads page: playbooks/drivers/utility links]

[Context-menu add/remove entries]
    └──conflicts──> [Windows 11 "modern" context menu] (only classic/legacy menu is registry-editable)

[System-restore-point-before-apply safety net]
    └──enhances──> [All tweak/debloat pages] (not currently present per PROJECT.md — recommended addition)

[Gaming quick-launch tool grid]
    └──enhances──> [Gaming Tweaks toggles] (tweaks + launchers on same page, not separate concerns)

[v2 "Ultimate" tweak tier]
    └──requires──> [v1 parity port complete] (explicit sequencing decision in PROJECT.md)
```

### Dependency Notes

- **Two-phase Defender disable requires elevation:** both phases modify Defender policy/registry keys that fail or throw access-denied without admin rights — this is why the app-wide `requireAdministrator` manifest change is a hard prerequisite, not a nice-to-have.
- **Instant state feedback requires a state-reader service, not just persisted UI state:** the single most-cited quality gap between "toy" and "trustworthy" tools in this category is toggles that lie about current state after an external change (Windows Update, another tool, manual registry edit). This should be treated as a cross-cutting service used by all three toggle-based pages (OS Tweaks, Gaming Tweaks, Debloat), not implemented per-page.
- **Context-menu entries conflict with the Windows 11 modern context menu:** simple registry-based add/remove only works reliably against the classic/legacy menu (accessed via Shift+right-click or "Show more options" unless the classic menu is made default). This isn't a bug to fix — it's a known ecosystem constraint every context-menu tool works around; document the same limitation rather than trying to inject entries into the modern menu.
- **Restore-point safety net enhances every tweak/debloat surface:** it's not a feature of one page but a cross-cutting safety property. Given it's universally present in every credible competitor and PROJECT.md doesn't currently list it, this is worth flagging explicitly for requirements/roadmap discussion even though v1 scope is "parity" — omitting it is itself a category-level pitfall (see PITFALLS.md).
- **v2 Ultimate tier requires v1 completion:** already an explicit sequencing decision in PROJECT.md; included here only to confirm the dependency direction is correct from a feature-landscape perspective (deeper hardware/driver tooling is a natural v2 extension of the same page structure, not a prerequisite for v1).

## MVP Definition

### Launch With (v1 — parity per PROJECT.md)

Minimum viable product is explicitly feature parity with AkariOS Companion, ported cleanly to WinUI 3 MVVM. No new tweaks.

- [x] Home/dashboard landing page — entry point users expect
- [x] Akari OS Tweaks (32 registry-backed toggles) with two-phase Defender disable — core category-defining feature
- [x] Gaming Tweaks (GPU/latency/service toggles, dropdowns, third-party tool quick-launch grid) — matches ecosystem's "tweaks + launcher grid" pattern
- [x] Debloat (28 PowerShell-backed actions) moved into ViewModel/Service — table stakes bulk-removal feature, now architecturally correct
- [x] Downloads (self-healing PostInstall mirror + playbooks/drivers/links) — genuine differentiator, keep as-is
- [x] Misc (12 context-menu entries + extra tools) — table stakes for this category
- [x] Elevated execution (`requireAdministrator`) — non-negotiable prerequisite for every other feature

### Add After Validation (v1.x)

Features to consider once the parity port is stable and validated in real use — these are safety/trust upgrades, not new tweak categories, so they fit the "correctness over expansion" spirit of Core Value without violating the v1 parity scope decision.

- [ ] Automatic system-restore-point creation before applying tweaks/debloat — trigger: any user or reviewer feedback about wanting to undo a change; this is the single most universal safety feature across the entire category and its absence is the most notable gap versus WinUtil/ShutUp10
- [ ] Explicit risk-level labeling per toggle (safe/standard vs advanced/risky) — trigger: once parity ships, low-cost UI/copy pass that closes the "toggle separation" table-stakes gap if the predecessor's grouping doesn't already make this clear enough

### Future Consideration (v2+)

Features to defer until after v1 parity ships and is validated, per PROJECT.md's explicit "Ultimate" tier deferral.

- [ ] Deep "Ultimate" tweak collection (~110 scripts: Check/Refresh/Setup/Installers/Graphics/Windows/Hardware/Advanced) — defer because it's an order-of-magnitude scope increase (BIOS updates, driver management, hardware overclocking) explicitly called out as v2 in PROJECT.md
- [ ] Curated SHA256-verified third-party tool bundle (7-Zip, Autoruns, CPU-Z, CRU, DDU, GPU-Z, HWiNFO, MSI Afterburner, NVIDIA Profile Inspector, Prime95, vcredist) — defer alongside the Ultimate tier since it's sourced from the same v2 material
- [ ] Continuous background tweak-enforcement/drift-protection (ShutUp10 Premium-style) — defer indefinitely unless user research specifically asks for it; flagged as an anti-feature for this tool's scope (adds a background service and attack surface disproportionate to value)

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|----------------------|----------|
| Toggle-based OS/gaming/debloat tweaks (ported) | HIGH | MEDIUM (port, not build) | P1 |
| Two-phase Defender disable workflow (ported) | HIGH | LOW (port, existing logic) | P1 |
| Self-healing PostInstall downloader (ported) | HIGH | LOW (port, existing logic) | P1 |
| Context-menu add/remove (ported) | MEDIUM | LOW (port, existing logic) | P1 |
| Gaming tool quick-launch grid (ported) | MEDIUM | LOW (port, existing logic) | P1 |
| Instant/accurate state feedback (verify ported logic reads real state, not cached UI state) | HIGH | MEDIUM | P1 |
| System-restore-point-before-apply | HIGH | MEDIUM | P2 |
| Explicit risk-level labeling per toggle | MEDIUM | LOW | P2 |
| Deep "Ultimate" tweak tier | HIGH (for power users) | HIGH | P3 |
| Curated verified third-party tool bundle | MEDIUM | MEDIUM | P3 |
| Background drift-protection daemon | LOW (niche) | HIGH | P3 / do-not-build |

**Priority key:**
- P1: Must have for launch (v1 parity)
- P2: Should have, add when possible (v1.x safety upgrades)
- P3: Nice to have, future consideration (v2 deep tooling)

## Competitor Feature Analysis

| Feature | WinUtil (Chris Titus Tech) | O&O ShutUp10(++) | Akari Toolbox Approach |
|---------|------------------------------|-------------------|---------------------------|
| Tweak presentation | Tabs: Install / Tweaks / Config / Updates / MicroWin; Tweaks split Standard vs Advadvanced | ~300 privacy settings in one categorized list with recommendation levels | Multi-page split by domain (OS Tweaks / Gaming / Debloat / Misc) — more task-oriented than WinUtil's tab model, closer to ShutUp10's categorization but domain-split rather than privacy-only |
| Debloat scope | 40+ apps removed via PowerShell, plus optional custom debloated ISO (MicroWin) | Not a debloat tool — privacy-focused only | 28 PowerShell-backed debloat actions; no ISO-creation feature (not in predecessor scope, reasonable to leave out) |
| Safety net | Restore point created automatically before changes; per-tweak undo | Restore point created automatically before changes; Premium adds continuous drift protection | Currently none per PROJECT.md — recommended P2 addition (see MVP section) |
| Security-sensitive toggle (Defender/AV) | Not offered — most general debloat tools avoid touching Defender directly, citing risk | Not offered — ShutUp10 is privacy-scoped, not AV-scoped | Two-phase guided Defender disable with explicit warnings — a genuine differentiator none of the mainstream tools attempt this deliberately |
| Gaming-specific tuning | Not a focus — WinUtil is general-purpose | Not offered | Dedicated Gaming Tweaks page (SvcHost split threshold, Win32 priority separation, service config) + third-party tool launcher grid — a distinct category strength |
| Post-install asset/driver bundling | Not offered | Not offered | Self-healing PostInstall mirror (drivers/playbooks/utilities) tied to a specific OS build — unique to Akari's OEM-adjacent positioning |
| Context menu editing | Not offered | Not offered | 12 context-menu add/remove entries — matches dedicated single-purpose tools (Context Menu X, Classic Context Menu), bundled into a broader utility instead of standalone |
| Distribution model | Free, open-source PowerShell script | Free (Premium tier for enforcement) | Elevated, self-contained unpackaged EXE — matches predecessor's "just run the exe" model |

## Sources

- [ChrisTitusTech/winutil (GitHub)](https://github.com/christitustech/winutil) — MEDIUM confidence, official project repo
- [Windows Utility in 2026 — Everything That's Changed (christitus.com)](https://christitus.com/winutil-in-2026/) — MEDIUM confidence, primary author's own site
- [O&O ShutUp10 official features page (oo-software.com)](https://www.oo-software.com/en/shutup10/features) — MEDIUM confidence, vendor site (cross-checked against manuals.oo-software.com)
- [O&O ShutUp10 Premium press release](https://www.oo-software.com/en/press_releases/ooshutup10_prem) — MEDIUM confidence, vendor source for the drift-protection/Premium differentiator
- [Win-10-Smart-Debloat-Tools and related AlternativeTo listings](https://www.alternativeto.net/software/win11debloat/) — LOW–MEDIUM confidence, aggregator/community listing
- [Sycnex/Windows10Debloater (GitHub)](https://github.com/Sycnex/Windows10Debloater) — MEDIUM confidence, widely-used community project
- [Safe Windows 11 Debloat: 5 Common Failure Modes (windowsforum.com)](https://windowsforum.com/threads/safe-windows-11-debloat-5-common-failure-modes-and-how-to-avoid-them.404205/) — MEDIUM confidence (community forum, cross-checked against MakeUseOf and Microsoft Q&A reports of Store breakage)
- [I tried debloating Windows 11 and it came with 3 drawbacks (MakeUseOf)](https://www.makeuseof.com/tried-debloating-windows-11-it-came-with-drawbacks/) — MEDIUM confidence, independent tech publication
- [Microsoft Store Not Found/Missing After Debloating Windows 10 (Microsoft Q&A)](https://learn.microsoft.com/en-us/answers/questions/3790047/microsoft-store-not-found-missing-after-debloating) — HIGH confidence, official Microsoft support forum reporting real-world breakage
- [Win32PrioritySeparation for Gaming: 0x26 Explained (FPSHeaven)](https://fpsheaven.com/blogs/news/win32priorityseparation) — LOW–MEDIUM confidence, enthusiast site, cross-checked against Blur Busters forum discussion
- [keoy7am/Win32PrioritySeparationTool (GitHub)](https://github.com/keoy7am/Win32PrioritySeparationTool) — MEDIUM confidence, existence of dedicated single-purpose tool confirms this is a recognized standalone feature in the ecosystem
- [Protect security settings with tamper protection (Microsoft Learn)](https://learn.microsoft.com/en-us/defender-endpoint/prevent-changes-to-security-settings-with-tamper-protection) — HIGH confidence, official Microsoft documentation
- [Breaking through Defender's Gates (Altered Security)](https://www.alteredsecurity.com/post/disabling-tamper-protection-and-other-defender-mde-components) — MEDIUM confidence, security research firm, corroborates two-phase gating behavior
- [Context Menu X (msappx.com)](https://msappx.com/context-menu) and [Windows 11 Classic Context Menu (MajorGeeks)](https://www.majorgeeks.com/files/details/windows_11_classic_context_menu.html) — LOW–MEDIUM confidence, vendor/download-site pages, corroborate the classic-vs-modern context menu registry-editability constraint
- Predecessor codebase context (`AkariOS-Companion`) as described in `.planning/PROJECT.md` — HIGH confidence, first-party project source

---
*Feature research for: Windows system-tweak / debloat / gaming-optimization desktop utilities*
*Researched: 2026-08-31*
