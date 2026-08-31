# Phase 1: Foundation & Akari OS Tweaks - Context

**Gathered:** 2026-08-31
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 1 delivers a new, elevated, rebranded "Akari Toolbox" WinUI 3 MVVM app — copied/renamed from the WinUI-3-MVVM-Framework template — with a Home dashboard and a fully working Akari OS Tweaks page (32 registry/service-backed toggles, each reflecting real live system state and reverting to the real recorded prior state, plus the Defender two-phase disable workflow ported as a direct, unmodified carry-over of the predecessor's logic). This phase also stands up every foundational piece the rest of the app depends on: elevation (`requireAdministrator`, smoke-tested against the self-contained build), the system-primitive layer (registry/service/script-runner), an elevation-safe file/folder picker, and the cross-thread dispatcher/marshaling pattern — because Tweaks is the smallest page that exercises the full stack.

Requirements covered: APP-01, APP-02, APP-03, APP-04, APP-05, HOME-01, TWEAKS-01, TWEAKS-02, TWEAKS-03.

</domain>

<decisions>
## Implementation Decisions

### Carried Forward (binding constraints from PROJECT.md / REQUIREMENTS.md)

- **D-01:** The Defender-disable code path (`TweakService.SetDefender`/`SetDefenderAsync` and everything it calls — Tamper Protection check, NoDefender package install, `RunOnce` phase-2 cleanup script) is ported as a direct, minimally-modified carry-over. Do NOT refactor, decompose into `ITweakHandler`, or rewrite this logic in Phase 1 — explicit, twice-repeated user instruction. Only the thin ViewModel/UI binding around it is new. — **Reversibility:** one-way — this is an explicit user directive, not a technical default; changing it requires the user to explicitly ask to revisit (tracked as SEC-01 in REQUIREMENTS.md v2).
- **D-02:** Native WinUI 3 Fluent 2 controls + the framework's Mica backdrop only. The predecessor's WPF-UI theme (`Themes/Colors.xaml`, `Themes/Controls.xaml`) is NOT ported — no custom color/brush resources, no WPF-UI-style visual language.
- **D-03:** Tweak state must be read from the real live system (registry/service value), not the predecessor's app-tracked "did we flip this" flag pattern (`HasState`/`SaveState`/`ClearState` against a private state hive). Each of the 32 tweak toggles needs a live-state reader derived from its actual registry/service effect, not a port of the flag-check.
- **D-04:** Before mutating any tweak (except the Defender tweak, which is exempt per D-01), the app must record the tweak's real prior value so turning it back off restores that real value, not a hardcoded default.

### Log/Status Console
- **D-05:** Keep a persistent, always-visible log/status console in the app shell — same UX intent as the predecessor's `TxtLog` + `ProgressBar`, fed by tweak actions and (later phases) script/download output.
- **D-06:** Console is a docked panel, collapsible by the user (not fixed/always-expanded) — reclaim space when not needed, still visible by default.
- **D-07:** Console is in-memory only and clears on each app launch — matches predecessor behavior exactly, no session persistence or log file in Phase 1.
- **D-08:** Console must be implemented as a framework service (e.g. an `ILogConsoleService` or similar) that ViewModels/services call into via injected interface — NOT the predecessor's anti-pattern of passing raw `TextBox`/`ProgressBar` controls into `ToolService`'s constructor. Same visible behavior, correct MVVM plumbing underneath.

### Placeholder Scaffolding (Home + Navigation)
- **D-09:** Home shows all 5 destination cards from day one — Akari OS Tweaks, Gaming Tweaks, Debloat, Downloads, Misc — even though only Tweaks is built in Phase 1. The 4 not-yet-built cards are visibly disabled/labeled "Coming soon" and non-interactive.
- **D-10:** This adds a 5th card to Home beyond the predecessor's 4 (predecessor had no Debloat card, Debloat was nav-sidebar-only) — Debloat gets equal footing with the other four in the new app.
- **D-11:** Nav sidebar follows the same pattern: all 5 destination entries appear now, with the 4 unbuilt ones disabled until their phase ships.

### Resolved During Research Review (post-RESEARCH.md, pre-plan)
- **D-12:** The `dep` tweak (pure `bcdedit /set NX`, zero registry footprint) reports live state by spawning `bcdedit /enum {current}` and parsing the NX line on page load — strict D-03 compliance preferred over a write-only exception, despite the small process-spawn latency cost. Resolves RESEARCH.md Open Question 1.
- **D-13:** The elevation-safe `IFilePickerService` (`Microsoft.Windows.Storage.Pickers`, APP-04) gets a temporary debug smoke-test button in Phase 1 so it has something concrete to click-test, even though no Phase 1 page consumes it for real — remove once Phase 4 wires a real picker consumer. Resolves RESEARCH.md Open Question 2.
- **D-14:** `CreateRealHkcuSubKey`'s hard failure when `explorer.exe` isn't running is preserved as-is (throw, no fallback) — exact parity with the predecessor, consistent with the phase's port-don't-improve posture. Resolves RESEARCH.md Open Question 3.

### Claude's Discretion
- **Defender restart UX** was raised as a possible discussion area but not selected/explored. Default: log-message-only, exact parity with the predecessor (no "Restart Now" button or other new UI affordance around the Defender workflow) — consistent with D-01's "don't touch/wrap this code path with new behavior" spirit. If the user wants a restart-prompt convenience later, that's a small, separable addition to revisit explicitly, not something to add proactively during the port.
- Exact visual treatment of "Coming soon" placeholders (grayed card vs. lock icon vs. badge text) — Claude's call, should read as clearly non-interactive without looking broken.
- Live-state-reader implementation per tweak (D-03) — deriving "is this tweak currently active" from each tweak's actual registry/service effect is a technical/research task, not a user vision question. `TweakService.cs` (see Specific Ideas below) has the per-tweak apply logic to reverse-engineer the read side from.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Predecessor source (port from, read-only — do not modify)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/TweakService.cs` — all 32 tweaks' apply logic (registry/service keys and values), including `SetDefenderAsync` (lines ~828-1017) which must be ported byte-for-byte per D-01
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ITweakService.cs` — the `GetState(key)`/`SetState(key, enabled)` contract shape (keep the shape, replace the live-state semantics per D-03)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Models/TweakItem.cs` — tweak model shape (Key/Title/Description/IsOn observable)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/AkariOSTweaksViewModel.cs` — the 32 tweak definitions (key, title, description) to carry over verbatim
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/ViewModels/HomeViewModel.cs` and `Views/HomePage.xaml` — Home card content/layout reference (4 existing cards + glyphs; add a 5th Debloat card per D-10)
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/Services/ToolService.cs` — the log/progress pattern to preserve behaviorally (D-05) but NOT structurally (D-08) — constructor-injects `TextBox`/`ProgressBar`/`TextBlock`, the anti-pattern to fix
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/MainWindow.xaml` (`TxtLog`, `LogProgress` controls, ~line 281-290) — existing log console placement reference
- `C:/Users/isleap/Documents/GitHub/AkariOS-Companion/app.manifest` — has `requireAdministrator` already; confirms the elevation manifest shape to replicate (APP-01)

### Framework template (copy/rename as starting point)
- `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/` — app shell to copy/rename to Akari Toolbox (MainWindow with NavigationView, custom title bar, Mica backdrop)
- `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.Framework/` — reusable services (`ServiceCollectionExtensions.cs` DI registration, `Navigation/FrameNavigationService.cs`, `Services/IDialogService.cs`, `Services/IFilePickerService.cs` — needs an elevation-safe replacement per APP-04, `Services/IInfoBarService.cs`, `Threading/DispatcherQueueExtensions.cs` — the marshaling pattern for APP-05, `Logging/FileLoggerProvider.cs`)
- `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/src/AppTemplate.App/app.manifest` — does NOT have `requireAdministrator`; must be added (APP-01)
- `C:/Users/isleap/Documents/GitHub/WinUI-3-MVVM-Framework/README.md` — solution layout, "Starting a new app" rename checklist

### Project-level research and decisions
- `.planning/PROJECT.md` — Key Decisions table (all four already listed as Carried Forward above), Constraints section
- `.planning/REQUIREMENTS.md` — APP-01..05, HOME-01, TWEAKS-01..03 exact wording; TWEAKS-02's "ported as-is" caveat
- `.planning/research/PITFALLS.md` — picker crash under `requireAdministrator`, Tamper Protection no-op behavior, cross-thread `COMException` risk, elevation+self-contained manifest smoke-test flag
- `.planning/research/ARCHITECTURE.md` — recommended primitive/handler/catalog decomposition (applies to the other 31 tweaks; explicitly does NOT apply to the Defender tweak per D-01)
- `.planning/research/STACK.md` — `System.ServiceProcess.ServiceController` 9.0.9 package needed; PowerShell stays process-based

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- Framework's `FrameNavigationService`/`INavigationService` — page navigation, already DI-registered
- Framework's `ViewModelBase` (CommunityToolkit.Mvvm-based) — base class for Home/Tweaks ViewModels
- Framework's `IDialogService`, `IInfoBarService`, `IWindowService` — available for any confirmation dialogs the Tweaks page needs
- Framework's `DispatcherQueueExtensions` — starting point for the cross-thread marshaling pattern (APP-05)
- Framework's `FileLoggerProvider` — rolling file logger, available if a later phase wants log persistence (not used in Phase 1 per D-07)
- Predecessor's 32 tweak definitions (key/title/description) in `AkariOSTweaksViewModel.cs` — copy verbatim, only the underlying service implementation changes

### Established Patterns
- Framework: DI via `Microsoft.Extensions.Hosting` in `App.xaml.cs`; pages instantiated through DI by `FrameNavigationService`; CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`)
- Predecessor: `ITweakService.GetState(key)`/`SetState(key, enabled)` — keep this shape, change the implementation to read/write real state (D-03/D-04)
- Predecessor: tweak toggles are a flat, 2-column `ItemsControl`/`UniformGrid` of rows (title + description + `ToggleSwitch`) — reasonable to translate directly to a WinUI 3 `ItemsRepeater`/`GridView` equivalent; no grouping/search/risk-labeling in v1 (SAFE-02 deferred)

### Integration Points
- `MainWindow`'s `NavigationView` `NavItems` — needs Home + Akari OS Tweaks entries now, plus 4 disabled placeholder entries per D-11
- `App.xaml.cs` DI container — register the new log console service (D-08), the system-primitive services (`IRegistryService`, `IWindowsServiceController`, `IScriptRunner`), and `ITweakService`/tweak catalog
- `app.manifest` — add `requireAdministrator` (currently absent in the framework template, present in the predecessor)

</code_context>

<specifics>
## Specific Ideas

- The Defender tweak is just tweak #31 of 32 in a flat toggle list — it is NOT a separate wizard/multi-step screen. Its "two-phase" nature is entirely internal to `SetDefenderAsync` (phase 1 runs synchronously in-app; phase 2 runs via a Windows `RunOnce` batch script after a user-initiated reboot, outside the app's lifetime entirely). The toggle itself looks identical to any other tweak in the UI.
- Home card glyphs use Segoe Fluent Icons codepoints already defined in the predecessor (`HomeViewModel.cs`) — reuse the same glyphs (`\uE7FC` Gaming, `\uE713` OS Tweaks, `\uE896` Downloads, `\uE712` Misc); pick a suitable glyph for the new Debloat card (D-10).
- All 32 tweak definitions and their real registry/service effects live in `TweakService.cs` — this is the single source of truth for both what to port and how to derive live-state reads per D-03.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope. (Defender restart UX was raised as a possible topic but resolved via Claude's Discretion above rather than deferred to a future phase — it's an in-scope Phase 1 UI detail, not a new capability.)

</deferred>

---

*Phase: 1-Foundation & Akari OS Tweaks*
*Context gathered: 2026-08-31*
