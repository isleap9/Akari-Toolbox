# Technology Stack

**Analysis Date:** 2026-10-03

## Languages

**Primary:**
- C# (latest LangVersion, nullable + implicit usings enabled) - all app, framework, and test code in `src/AppTemplate.App/`, `src/AppTemplate.Framework/`, `src/AppTemplate.Tests/`
- XAML (WinUI 3 / Windows App SDK dialect) - all views in `src/AppTemplate.App/Views/*.xaml`, `src/AppTemplate.App/Views/Shared/*.xaml`, `src/AppTemplate.App/MainWindow.xaml`, `src/AppTemplate.App/App.xaml`
- PowerShell 5.1/7 (build/ops scripts only, never executed by the app at runtime except via generated `powershell.exe` invocations) - `build-and-run.ps1`, `check-parity.ps1`, `verify-pages.ps1`, `shot-pages.ps1`, `port-windows-page.ps1`

**Secondary:**
- MSBuild XML (project / props / manifest configuration) - `Directory.Build.props`, `Directory.Packages.props`, `src/AppTemplate.App/AppTemplate.App.csproj`, `src/AppTemplate.Framework/AppTemplate.Framework.csproj`, `src/AppTemplate.Tests/AppTemplate.Tests.csproj`, `src/AppTemplate.App/app.manifest`
- JSON (settings + NuGet/dev config) - runtime `%LOCALAPPDATA%\AkariToolbox\settings.json` (see `src/AppTemplate.Framework/Services/FileSettingsStorage.cs`), `global.json`, `nuget.config`
- Embedded data blobs treated as text resources (`.reg`, `.xml`, `.nip`, GPU profiles) - `src/AppTemplate.App/Tweaks/Data/**/*` loaded via `src/AppTemplate.App/Tweaks/NativeOps.cs` (`LoadWindowsData` / `LoadGraphicsData` / `LoadAdvancedData`)

## Runtime

**Environment:**
- .NET SDK 10.0.400 pinned via `global.json` (`rollForward: latestMinor`, `allowPrerelease: false`)
- Target framework `net10.0-windows10.0.26100.0` declared in `Directory.Build.props`
- Windows-only: `UseWinUI=true`, `TargetFramework` is Windows-specific, `RuntimeIdentifier` `win-x64` / `win-x86` / `win-arm64` (platform-conditional in `Directory.Build.props`); app project pins `PlatformTarget=x64` + `RuntimeIdentifier=win-x64` in `src/AppTemplate.App/AppTemplate.App.csproj`
- Requires Windows 10/11 x64 and elevation (`requireAdministrator` in `src/AppTemplate.App/app.manifest`)

**Package Manager:**
- NuGet with Central Package Management (`ManagePackageVersionsCentrally=true`, `CentralPackageTransitivePinningEnabled=true` in `Directory.Packages.props`)
- Single feed: `nuget.org` only (`nuget.config` clears then adds `https://api.nuget.org/v3/index.json`)
- Lockfile: missing (no `packages.lock.json` committed; restore is floating within centrally pinned versions)

## Frameworks

**Core:**
- Windows App SDK 2.3.1 (`Microsoft.WindowsAppSDK`) - WinUI 3 XAML runtime, windowing, imaging (`Windows.Graphics.Imaging` used in `src/AppTemplate.App/Tweaks/NativeOps.cs`), app lifecycle (`Microsoft.Windows.AppLifecycle` single-instance in `src/AppTemplate.App/App.xaml.cs`)
- Windows SDK Build Tools 10.0.26100.4654 (`Microsoft.Windows.SDK.BuildTools`, MSIX variant 1.7.251221100) - XAML compiler / packaging tooling (unpackaged mode, `WindowsPackageType=None`)
- CommunityToolkit.Mvvm 8.4.2 - `[ObservableProperty]` / `[RelayCommand]` MVVM in all `src/AppTemplate.App/ViewModels/*.cs` and `src/AppTemplate.Framework/ViewModels/*.cs`
- Microsoft.Extensions.Hosting 10.0.11 + DependencyInjection 10.0.11 (+ Abstractions) - generic host built in `src/AppTemplate.App/App.xaml.cs` (`BuildHost()`), DI registrations for all pages/view models/services/navigation
- Microsoft.Extensions.Logging 10.0.11 (+ Abstractions, Debug provider) + custom `FileLoggerProvider` in `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs` - daily rolling file logs under `%LOCALAPPDATA%\AkariToolbox\logs\`
- Microsoft.Extensions.Configuration 10.0.11 + Json 10.0.11 - host configuration pipeline (declared centrally in `Directory.Packages.props`; hosting pulls it in transitively)
- Microsoft.Xaml.Behaviors.WinUI.Managed 3.0.1 - `InvokeCommandOnLoadedBehavior`, `ListViewScrollIntoViewBehavior`, `TextBoxSelectAllOnFocusBehavior` in `src/AppTemplate.Framework/Behaviors/*.cs`
- System.Management 10.0.12 - WMI reads (`Win32_OperatingSystem`, `Win32_Processor`, `Win32_VideoController`, `Win32_ComputerSystem`) in `src/AppTemplate.App/Tweaks/SystemInfo.cs` and `src/AppTemplate.App/Tweaks/WindowsActions.System.cs`

**Testing:**
- xUnit 2.9.3 + `xunit.runner.visualstudio` 3.1.4 + `Microsoft.NET.Test.Sdk` 17.14.1 - test runner for `src/AppTemplate.Tests/`
- Moq 4.20.72 - mocking `IDialogService`, `ISettingsService`, etc. in `src/AppTemplate.Tests/`

**Build/Dev:**
- MSBuild / `dotnet` CLI (SDK 10.0.400) - `dotnet build src/AppTemplate.App/AppTemplate.App.csproj -c Debug|Release`
- Custom XAML workaround target `SetAkariXamlLocalAssembly` (Before `MarkupCompilePass2`) in `src/AppTemplate.App/AppTemplate.App.csproj` - fixes page-level `x:Bind` / `x:DataType` WMC9999 resolution
- PowerShell helper scripts: `build-and-run.ps1` (kill apphost lock, build with zero-error/zero-warning gate, launch), `verify-pages.ps1` (UIA click-through of all 13 nav items), `check-parity.ps1` (WPF text parity diff), `port-windows-page.ps1`, `shot-pages.ps1`

## Key Dependencies

**Critical:**
- `Microsoft.WindowsAppSDK` 2.3.1 - entire UI + `AppInstance.FindOrRegisterForKey("AkariToolbox")` single-instance + `WindowNative.GetWindowHandle` in `src/AppTemplate.App/App.xaml.cs`
- `CommunityToolkit.Mvvm` 8.4.2 - source-generated observable properties/commands; `NoWarn` includes `MVVMTK0045` in `src/AppTemplate.App/AppTemplate.App.csproj` (self-contained, not AOT)
- `Microsoft.Extensions.Hosting` 10.0.11 - composes `AddMvvmFramework()` (see `src/AppTemplate.Framework/ServiceCollectionExtensions.cs`) + app-level registrations in `src/AppTemplate.App/App.xaml.cs`
- `System.Management` 10.0.12 - hardware identity and This-PC card; disposal discipline is load-bearing (see `src/AppTemplate.App/Tweaks/SystemInfo.cs`)

**Infrastructure:**
- `Microsoft.Extensions.Logging.Debug` 10.0.11 - debug-trace sink alongside file logger in `src/AppTemplate.App/App.xaml.cs`
- `Microsoft.Extensions.Configuration.Json` 10.0.11 - host config stage (no `appsettings.json` shipped; settings persist via `FileSettingsStorage` JSON instead)
- `Microsoft.Xaml.Behaviors.WinUI.Managed` 3.0.1 - XAML behaviors wired in views
- `Microsoft.Windows.SDK.BuildTools` 10.0.26100.4654 - build-time XAML compilation
- `System.Text.Json` (in-box .NET) - `settings.json` serialization in `src/AppTemplate.Framework/Services/FileSettingsStorage.cs`, NVIDIA driver lookup payload in `src/AppTemplate.App/Tweaks/DriverActions.cs`
- `System.Net.Http` (`HttpClient`, in-box) - singleton `Http` in `src/AppTemplate.App/Tweaks/NativeOps.cs` (`Download`, `HttpGetString`, `DownloadWithHeaders`, 10-min timeout)

## Configuration

**Environment:**
- No `.env` files, no user secrets, no cloud config. Existence check: no `.env*`, `credentials.*`, or `secrets/` present in repo root.
- Runtime settings file created on first run: `%LOCALAPPDATA%\AkariToolbox\settings.json` via `src/AppTemplate.Framework/Services/FileSettingsStorage.cs` (key/value string dictionary, atomic temp-file + move writes). Instantiated as `new FileSettingsStorage("AkariToolbox")` in `src/AppTemplate.App/App.xaml.cs`.
- Log directory created on first run: `%LOCALAPPDATA%\AkariToolbox\logs\` via `FileLoggerProvider` in `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs` (1 MB cap per file, newest 10 kept, `app-yyyyMMdd.log` naming).
- Manifest elevation: `src/AppTemplate.App/app.manifest` sets `requestedExecutionLevel level="requireAdministrator"`; all file pickers therefore use Win32 `GetOpenFileNameW` (`src/AppTemplate.App/Tweaks/NativeOps.cs` `PickFile`) / `Microsoft.Win32.OpenFileDialog` instead of WinUI 3 pickers.
- Packaging: `WindowsPackageType=None` + `WindowsAppSDKSelfContained=true` (unpackaged, self-contained, no Windows App Runtime install needed) in `src/AppTemplate.App/AppTemplate.App.csproj`.

**Build:**
- `global.json` - SDK pin (10.0.400)
- `Directory.Build.props` - TFM, LangVersion, nullable, WinUI flag, Windows SDK version, platform/RID mapping, default `x64`
- `Directory.Packages.props` - all NuGet versions centrally (WindowsAppSDK, BuildTools, MVVM, Extensions, Behaviors, xunit/Moq/Test SDK, System.Management)
- `nuget.config` - clears feeds, restores from nuget.org only
- `AppTemplate.slnx` - solution listing `src/AppTemplate.App`, `src/AppTemplate.Framework`, `src/AppTemplate.Tests`
- `src/AppTemplate.App/app.manifest` - elevation + Win10/11 compatibility + PerMonitorV2 DPI
- `src/AppTemplate.App/AppTemplate.App.csproj` - `WinExe`, assembly `AkariToolbox.App`, namespace `AkariToolbox.*`, explicit `Compile`/`Page` globs (no default items), `EmbeddedResource` for `Tweaks/Data/**/*`, XAML local-assembly workaround target

## Platform Requirements

**Development:**
- Windows 10/11 x64 with .NET 10.0.400 SDK installed
- Ability to run elevated builds (app manifest requires admin; apphost file lock requires kill-before-rebuild, handled by `build-and-run.ps1`)
- NuGet access to `https://api.nuget.org/v3/index.json`
- PowerShell for `build-and-run.ps1` / `verify-pages.ps1` / `check-parity.ps1`
- Build gate is `Errors: 0 Warnings: 0` (XAML diagnostics parsed from build log, not just MSBuild exit code)

**Production:**
- Windows 10/11 x64, administrator privileges (every tweak writes HKLM / services / drivers / WMI)
- Unpackaged self-contained single exe: `src/AppTemplate.App/bin/x64/Debug|Release/net10.0-windows10.0.26100.0/win-x64/AkariToolbox.App.exe` (display name "Akari Toolbox")
- No external runtime install (Windows App SDK self-contained), no installer, no MSIX
- Writes only to: Windows Registry (HKLM/HKCU), `%SystemRoot%\Temp` staging, `%LOCALAPPDATA%\AkariToolbox\` (settings + logs), Start Menu / Desktop shortcuts

---

*Stack analysis: 2026-10-03*
