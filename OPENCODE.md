# Akari Toolbox

WinUI 3 / MVVM port of **AkariBase** — a clean front-end for the FR33THY
Ultimate tweak set. Tunes Windows appearance, debloat/privacy, drivers, GPU
settings, services, scheduling and more, with native registry/service/driver
operations (no scripts or bundled binaries are ever executed).

## Layout

- `src/AppTemplate.App/` — the app: `Views/` (13 pages), `ViewModels/`,
  `Tweaks/` (native action ports), `Services/`, `Helpers/`, `Assets/`.
- `src/AppTemplate.Framework/` — shared MVVM shell (navigation, settings,
  logging, converters, InfoBar/dialog/status services).
- `src/AppTemplate.Tests/` — tests.
- `build-and-run.ps1` — kill, build, launch helper.
- `check-parity.ps1` — diffs every `Text=`/`Content=` string between each WPF
  page and its port (only intentional product renames may differ).
- `verify-pages.ps1` — clicks all 13 nav items via UIA and reports per-page
  content counts.

## Pages (WPF order)

Home, Akari OS Tweaks, Gaming Tweaks, 1 Check, 2 Refresh, 3 Setup,
4 Installers, 5 Graphics, 6 Windows, 7 Hardware, 8 Advanced,
Individual Tweaks (169 Control Panel rows in 4 collapsible groups), Settings.

## Key behaviors

- Unpackaged, self-contained, requires administrator
  (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`).
- Display name **Akari Toolbox** (assembly `AkariToolbox.App`,
  namespace `AkariToolbox.*`).
- Safe-boot re-entry flows (`--ddu-auto`, `--defender-optimize`,
  `--services-off`, …) run headless via `Environment.GetCommandLineArgs()`
  before single-instance registration, then exit.
- Single-instance key + settings folder: `AkariToolbox`.
- Native WinUI 3 styling only: system theme resources, native text styles,
  `Spacing`/`ColumnSpacing` layout, default buttons. No custom theme
  dictionaries or hardcoded colors.
- ToggleSwitch rows use `MinWidth="0"` (default template reserves dead
  width that breaks right alignment); tile grids use fixed `ItemWidth`
  (`ItemsWrapGrid` sizes all tiles to the first item).
- `SystemInfo` WMI reads must dispose `ManagementObjectCollection` and
  every `ManagementObject`, or revisit queries deadlock; OS/CPU/GPU strings
  are cached per process.
- Status footer: `IStatusService`/`StatusService` (ref-counted,
  registered in `AddMvvmFramework`) drives a persistent shell footer in
  `MainWindow` — "Ready" when idle, operation name + progress bar while a
  tweak runs. Every VM run site wraps work with `Start(...)`/`Complete()`;
  item-level runners delegate to the wired parent paths.
- x:Bind notes: page-level x:Bind cannot resolve its root type (WMC9999),
  so pages use `{Binding}` and `x:Bind` only inside `DataTemplate`
  (x:DataType); item commands live on the item classes (no
  `RelativeSource AncestorType=Page` equivalent); x:Bind has no negation,
  use the `InvertedBool` converter. Converters cannot be used with x:Bind
  on `MainWindow` (Window is not a FrameworkElement for converter lookup) —
  use x:Bind function bindings instead.

## Home page

Copied from the WinUI-3-MVVM-Framework reference layout: title, greeting,
Safety card (restore point + desktop shortcut), This PC card (Windows /
Processor / Graphics / Uptime / Privileges / .NET facts plus memory and
system-drive bars), and a single Tools `GridView` (230x96 tiles) covering
all 11 destinations. The reference's Open Settings / About / Refresh row
was omitted; `SystemInfo` gained uptime, privileges, .NET version, memory
and drive usage reads (kernel32 P/Invoke + DriveInfo).

## Shell layout (MainWindow)

Custom title bar, `NavigationView` content, full-width status footer pinned
to `SolidBackgroundFillColorBaseBrush` (matches the nav pane), InfoBar last.
Footer grid is text + progress bar, both `Auto`-sized so the bar sits next
to the text.

## Build & run

```powershell
& './build-and-run.ps1'           # kill, build, launch
& './build-and-run.ps1' -NoLaunch # build only
```

Build must report `Errors: 0  Warnings: 0`. Kill `AkariToolbox.App` before
rebuilding (the script does this) or the apphost file lock fails the build.
Output: `src/AppTemplate.App/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/AkariToolbox.App.exe`.
