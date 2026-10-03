# Akari Toolbox

A WinUI 3 desktop app for applying the FR33THY Ultimate tweak set: Windows appearance,
debloat/privacy, drivers, GPU settings, services, scheduling, and more. All tweaks run as
native registry / service / driver / WMI operations — no scripts or bundled binaries are
ever executed.

Requires **Windows 10/11 x64** and **administrator** privileges (every tweak writes
HKLM / services / drivers). The app is unpackaged and self-contained, so no Windows App
Runtime install is needed.

## Pages

Navigation order (Settings is pinned to the pane footer):

Home, Akari OS Tweaks, Gaming Tweaks, 1 - Check, 2 - Refresh, 3 - Setup, 4 - Installers,
5 - Graphics, 6 - Windows, 7 - Hardware, 8 - Advanced, Individual Tweaks (169 Control
Panel rows in 4 collapsible groups), Settings.

Home shows a Safety card (restore point + desktop shortcut), a This PC card (OS / CPU /
GPU / uptime / privileges / .NET, plus memory and system-drive usage bars), and tiles
linking to all 11 destinations.

## Solution layout

```
AppTemplate.slnx
├── src/
│   ├── AppTemplate.App         The app (display name "Akari Toolbox",
│   │   │                       assembly AkariToolbox.App, namespace AkariToolbox.*)
│   │   ├── Views/              13 pages + Shared/
│   │   ├── ViewModels/         13 page view models
│   │   ├── Tweaks/             Native action ports (registry/services/drivers/WMI)
│   │   │   └── Data/           Embedded data files (e.g. GPU profiles)
│   │   ├── Services/           LocalizedStrings
│   │   ├── Helpers/            NativeOps, SystemInfo, dialogs, misc
│   │   └── Assets/             AkariLogo.ico/.png
│   ├── AppTemplate.Framework   Shared MVVM shell (navigation, settings, logging,
│   │                           converters, InfoBar/dialog/status services)
│   └── AppTemplate.Tests       xUnit tests for the framework
├── Directory.Packages.props    Central package management
├── Directory.Build.props
├── global.json                 .NET SDK pin (10.0.400)
├── build-and-run.ps1           Kill, build, launch helper
├── check-parity.ps1            Diffs Text=/Content= strings vs. the WPF original
└── verify-pages.ps1            Clicks all 13 nav items via UIA and reports content
```

## Tech stack

- .NET 10 (`net10.0-windows10.0.26100.0`), Windows App SDK 2.3.1, WinUI 3
- CommunityToolkit.Mvvm 8.4.2 (`[ObservableProperty]`, `[RelayCommand]`)
- Microsoft.Extensions: Hosting, DependencyInjection, Logging
- System.Management (WMI reads in `SystemInfo`)
- Tests: xUnit 2.9.3, Moq 4.20.72, Microsoft.NET.Test.Sdk 17.14.1

Unpackaged (`WindowsPackageType=None`), self-contained
(`WindowsAppSDKSelfContained=true`), x64 only, `requireAdministrator` manifest.

## Build & run

```powershell
& './build-and-run.ps1'            # kill running copy, build, launch (Debug)
& './build-and-run.ps1' -NoLaunch  # build only
& './build-and-run.ps1' -Release   # Release build + launch
```

Build must report `Errors: 0  Warnings: 0`. Kill `AkariToolbox.App` before rebuilding
(the script does this) or the apphost file lock fails the build. Output:
`src/AppTemplate.App/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AkariToolbox.App.exe`.

## Shell

`MainWindow` wires a custom title bar, a `NavigationView`, a shared `InfoBar`, a Mica
backdrop that follows the theme, and a persistent status footer (`IStatusService`:
"Ready" when idle, operation name + progress bar while a tweak runs). Launching always
opens **Home**. Native WinUI 3 styling only — no custom theme dictionaries or hardcoded
colors.

## App notes

- Settings live in `%LOCALAPPDATA%\AkariToolbox\settings.json`; theme (System / Light /
  Dark) and language apply immediately.
- `ILogger` output goes to `%LOCALAPPDATA%\AkariToolbox\logs\` (daily rolling files,
  ~1 MB cap, newest 10 kept); unhandled exceptions are logged there and shown in a
  friendly dialog.
- Single-instance key: `AkariToolbox`.
- Safe-boot re-entry flows (`--ddu-auto`, `--defender-optimize`, `--services-off`, …)
  run headless before single-instance registration, then exit.
- Pages are created through DI by `FrameNavigationService`; dialogs are abstracted
  behind `IDialogService`. Because the manifest requires elevation, file pickers use
  `Microsoft.Win32.OpenFileDialog` (WinUI 3 pickers don't work elevated).

## Notes

- **UI types can't be instantiated in testhost.** `new Page()` / `new Frame()` throw
  `COMException` in `dotnet test` even when the WinAppSDK runtime is bootstrapped.
  Tests cover only framework logic that doesn't require a XAML object.
- Page-level `x:Bind` can't resolve its root type (WMC9999); pages use `{Binding}`,
  with `x:Bind` only inside `DataTemplate` (`x:DataType`).
- `dotnet build` errors like `WMC9999` can hide the real issue; prefer building the
  solution and reading the full XamlCompiler diagnostics rather than stopping at the
  first error line.

## License

MIT — see `LICENSE`.
