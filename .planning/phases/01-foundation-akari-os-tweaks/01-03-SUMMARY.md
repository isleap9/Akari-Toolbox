---
phase: 01-foundation-akari-os-tweaks
plan: 03
subsystem: infra
tags: [winui3, file-picker, elevation, windows-app-sdk, mvvm]

# Dependency graph
requires:
  - phase: 01-foundation-akari-os-tweaks
    provides: "01-01 (renamed WinUI-3-MVVM-Framework skeleton, elevation manifest) and 01-02 (ILogConsoleService, system primitives) that this plan builds on"
provides:
  - "Elevation-safe IFilePickerService implementation on Microsoft.Windows.Storage.Pickers (APP-04)"
  - "PickSingleFolderAsync/PickMultipleFoldersAsync folder-picker support (new interface members)"
  - "Temporary debug picker smoke-test button wired to ILogConsoleService (D-13)"
affects: [downloads-page, misc-page]

actuals:
  tokens: 2854
  tasks: 2
  commits: 2

tech-stack:
  added: []
  patterns:
    - "WindowId-based picker construction (Microsoft.Windows.Storage.Pickers) instead of IntPtr hwnd + InitializeWithWindow.Initialize"
    - "PickFileResult/PickFolderResult.Path bridged back to StorageFile/StorageFolder via GetFileFromPathAsync/GetFolderFromPathAsync so the public interface shape is unchanged"

key-files:
  created: []
  modified:
    - src/AkariToolbox.Framework/Services/IFilePickerService.cs
    - src/AkariToolbox.App/App.xaml.cs
    - src/AkariToolbox.Framework/ServiceCollectionExtensions.cs
    - src/AkariToolbox.App/MainWindow.xaml
    - src/AkariToolbox.App/MainWindow.xaml.cs

key-decisions:
  - "Confirmed exact Microsoft.Windows.Storage.Pickers API shape (PickMultipleFilesAsync/PickMultipleFoldersAsync return IAsyncOperation<IReadOnlyList<PickFileResult/PickFolderResult>>) via ilspycmd decompilation of the installed WinAppSDK 2.3.x projection DLL rather than trusting research-doc prose alone, since the multi-select return shape wasn't explicitly spelled out in RESEARCH.md"
  - "Placed the temporary D-13 smoke-test button in the log dock's Expander header row (not the title bar) to avoid title-bar drag-region/hit-test interaction concerns"

patterns-established:
  - "Elevation-safe picker construction: new Microsoft.Windows.Storage.Pickers.FileOpenPicker/FileSavePicker/FolderPicker(windowId) — no InitializeWithWindow call needed"

requirements-completed: [APP-04]

coverage:
  - id: D1
    description: "IFilePickerService reimplemented on Microsoft.Windows.Storage.Pickers (WindowId-based), replacing the elevation-crashing Windows.Storage.Pickers + InitializeWithWindow implementation"
    requirement: "APP-04"
    verification:
      - kind: other
        ref: "dotnet build AkariToolbox.slnx -c Debug"
        status: pass
      - kind: other
        ref: "grep -n \"InitializeWithWindow\\|^using Windows.Storage.Pickers;\" src/AkariToolbox.Framework/Services/IFilePickerService.cs (no match)"
        status: pass
    human_judgment: false
  - id: D2
    description: "PickSingleFolderAsync/PickMultipleFoldersAsync added to IFilePickerService satisfying APP-04's file/folder picker wording"
    requirement: "APP-04"
    verification:
      - kind: other
        ref: "dotnet build AkariToolbox.slnx -c Debug"
        status: pass
    human_judgment: false
  - id: D3
    description: "Temporary debug picker smoke-test button opens the elevation-safe picker without crashing, disables itself while pending, and logs the result/cancellation to ILogConsoleService"
    requirement: "APP-04"
    verification: []
    human_judgment: true
    rationale: "Requires launching the actual self-contained, elevated exe on a real Windows desktop and clicking the button — no GUI/elevated-launch capability exists in this non-interactive worktree execution environment. IsEnabled disable/re-enable and log-message content were verified by code review only; the actual COMException-free elevated launch (RESEARCH Pitfall 2) is unverified pending a manual run."

duration: 25min
completed: 2026-09-01
status: complete
---

# Phase 1 Plan 03: Elevation-Safe File Picker Summary

**Replaced the framework's elevation-crashing `Windows.Storage.Pickers`-based `IFilePickerService` with `Microsoft.Windows.Storage.Pickers` (WindowId-based), added folder-picker support, and wired a temporary debug smoke-test button into the log dock.**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-09-01T01:45:00+02:00 (approx.)
- **Completed:** 2026-09-01T01:53:27+02:00
- **Tasks:** 2
- **Files modified:** 5

## Accomplishments
- `FilePickerService` rebuilt on `Microsoft.Windows.Storage.Pickers.{FileOpenPicker,FileSavePicker,FolderPicker}`, each constructed with a `WindowId` instead of the old `IntPtr` hwnd + `InitializeWithWindow.Initialize` pattern that Microsoft documents as crashing under `requireAdministrator` elevation (RESEARCH Pitfall 2, `microsoft/WindowsAppSDK#2504`)
- `IFilePickerService` extended with `PickSingleFolderAsync`/`PickMultipleFoldersAsync`, closing the gap where the framework's original interface had no folder-pick methods at all
- `App.xaml.cs`'s `BuildHost()` now registers a `Func<Microsoft.UI.WindowId>` provider (replacing the prior `Func<IntPtr>`) consumed by the picker's constructor
- Added a temporary, clearly-labeled "Picker smoke test (temporary, remove in Phase 4)" button to the log dock's header row, disabled for the duration of the pending picker `Task` and re-enabled in a `finally` block to prevent a double-invoke race, logging the picked path or "cancelled" via `ILogConsoleService`

## Task Commits

Each task was committed atomically:

1. **Task 1: Swap IFilePickerService to Microsoft.Windows.Storage.Pickers** - `1cab633` (feat)
2. **Task 2: Debug smoke-test button + elevated verification** - `5d41257` (feat)

**Plan metadata:** (this commit)

## Files Created/Modified
- `src/AkariToolbox.Framework/Services/IFilePickerService.cs` - Elevation-safe picker implementation; added folder-pick methods
- `src/AkariToolbox.App/App.xaml.cs` - `Func<WindowId>` DI registration replacing `Func<IntPtr>`
- `src/AkariToolbox.Framework/ServiceCollectionExtensions.cs` - Doc-comment update reflecting the new `Func<WindowId>` requirement
- `src/AkariToolbox.App/MainWindow.xaml` - Temporary picker smoke-test button in the log dock header
- `src/AkariToolbox.App/MainWindow.xaml.cs` - `IFilePickerService` constructor injection + `OnPickerSmokeTestClick` handler

## Decisions Made
- Verified the exact `Microsoft.Windows.Storage.Pickers` API surface (constructor signatures, `PickMultipleFilesAsync`/`PickMultipleFoldersAsync` return types) by decompiling the installed `Microsoft.WindowsAppSDK.Foundation` projection DLL with `ilspycmd` rather than relying solely on RESEARCH.md prose, since the multi-select return collection type wasn't explicitly documented there. Confirmed: `PickMultipleFilesAsync()` → `IAsyncOperation<IReadOnlyList<PickFileResult>>`, `PickMultipleFoldersAsync()` → `IAsyncOperation<IReadOnlyList<PickFolderResult>>`, both `PickFileResult`/`PickFolderResult` expose a `string Path`.
- Put the temporary D-13 debug button in the log dock's `Expander` header row rather than the custom title bar, avoiding any interaction with the title bar's drag-region hit-testing (`SetTitleBar(AppTitleBar)`).

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. The plan's Task 2 `<verify>` includes a `<human-check>` requiring an elevated manual launch and interactive click-test of the picker button — this cannot be performed in this non-interactive, GUI-less worktree execution environment. The automated `dotnet build` verification passed, and the `IsEnabled` disable/re-enable and log-message logic were confirmed correct by code review, but the actual COMException-free elevated launch (the core claim of RESEARCH Pitfall 2's fix) is unverified pending a manual run by the user on a real Windows desktop. Documented as `D3` with `human_judgment: true` in the coverage block above so `verify-work` routes it to the user rather than silently auto-passing.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

`IFilePickerService` is elevation-safe and interface-complete (file + folder, single + multi) ahead of Phase 4's Downloads page, which will be its first real consumer. The temporary debug button should be removed when that consumer is wired in (comment marks the removal point in both XAML and code-behind). Recommend the user perform one elevated manual launch + button click before treating APP-04 as fully closed — the automated build proves the code compiles against the correct API, but only a real elevated run proves the crash (RESEARCH Pitfall 2) is actually avoided.

---
*Phase: 01-foundation-akari-os-tweaks*
*Completed: 2026-09-01*
