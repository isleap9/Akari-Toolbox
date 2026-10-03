# External Integrations

**Analysis Date:** 2026-10-03

## APIs & External Services

**Driver / tool downloads (runtime `HttpClient`, no SDK):**
- FR33THY Ultimate GitHub release bucket - bulk source for all tweak payloads/installers/diagnostics
  - Base URL: `https://github.com/FR33THYFR33THY/Ultimate/releases/download/Files/` (constants named `FilesUrl` in `src/AppTemplate.App/Tweaks/CheckActions.cs`, `src/AppTemplate.App/Tweaks/GraphicsActions.cs`, `src/AppTemplate.App/Tweaks/DriverActions.cs`, `src/AppTemplate.App/Tweaks/InstallerActions.cs`)
  - Client: shared singleton `HttpClient` (`Timeout = 10 min`) in `src/AppTemplate.App/Tweaks/NativeOps.cs` (`Download`, `DownloadWithHeaders`, `HttpGetString`)
  - Files pulled: `cpuz.exe`, `gpuz.exe`, `occt.exe`, `furmark.zip` (`src/AppTemplate.App/Tweaks/CheckActions.cs`); `nvp.appx`, `inspector.exe`, `7zip.exe`, `directx.exe`, GPU profile blobs (`src/AppTemplate.App/Tweaks/GraphicsActions.cs`); `7zip.exe`, `ddu.exe` (`src/AppTemplate.App/Tweaks/DriverActions.cs`); ~28 installer exes/msis (`steam.exe`, `epic.msi`, `chrome.exe`, `firefox.exe`, …) in `src/AppTemplate.App/Tweaks/InstallerActions.cs`
  - Auth: none (public release artifacts)
- NVIDIA GeForce lookup API - resolves latest WHQL driver version at runtime
  - Endpoint: `https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/AjaxDriverService.php?func=DriverManualLookup&...` parsed as JSON in `src/AppTemplate.App/Tweaks/DriverActions.cs` (`FindNvidiaDriverUrl`)
  - Download host constructed from version: `https://international.download.nvidia.com/Windows/{version}/{version}-desktop-{windows}-{arch}-international-dch-whql.exe`
  - Auth: none
- AMD driver page scrape - finds minimal-setup installer link by regex
  - Page: `https://www.amd.com/en/support/download/drivers.html` with browser `User-Agent`/`Accept`/`Referer: https://www.amd.com/` headers in `src/AppTemplate.App/Tweaks/DriverActions.cs` (`InstallAmdDriver`)
  - Pattern: `href="([^"]*?minimalsetup[^"]*_web\.exe)"`; download reuses same headers via `NativeOps.DownloadWithHeaders`
  - Auth: none
- Intel driver search (browser handoff only, no API call)
  - URL opened in default browser: `https://www.intel.com/content/www/us/en/search.html#sortCriteria=...&f-downloadtype=Drivers...` in `src/AppTemplate.App/Tweaks/DriverActions.cs` (`OpenIntelDriverSearch`); user picks the downloaded installer via Win32 file dialog (`PickIntelDriver`)
- Mozilla add-ons CDN - uBlock Origin side-load for every Firefox profile
  - URL: `https://addons.mozilla.org/firefox/downloads/latest/ublock-origin/latest.xpi` downloaded to `C:\Program Files\Mozilla Firefox\distribution\extensions\uBlock0@raymondhill.net.xpi` in `src/AppTemplate.App/Tweaks/InstallerActions.cs` (`InstallFirefox`)
  - Auth: none
- Third-party utility hosts (direct downloads / browser launches, no SDK):
  - `https://github.com/LordOfMice/hidusbf/raw/refs/heads/master/hidusbf.zip` and `https://github.com/cakama3a/Polling/releases/download/1.3.1.4/Polling.exe` in `src/AppTemplate.App/Tweaks/HardwareActions.cs`
  - Browser-only launches (via `NativeOps.Launch` / `TweakAction.Open`): `https://youtu.be/zwPEDXteJYQ` + `https://github.com/FR33THYFR33THY/Ultimate` (`src/AppTemplate.App/ViewModels/HomeViewModel.cs`); `https://cpstest.org/polling-rate-test`, `https://www.testufo.com/framerates...`, `https://www.waveform.com/tools/bufferbloat`, `https://pcpartpicker.com/user/fr33thy/saved` (`src/AppTemplate.App/Tweaks/HardwareActions.cs`); `https://schneegans.de/windows/unattend-generator/` (`src/AppTemplate.App/Tweaks/RefreshActions.cs`); Google search fallback `https://www.google.com/search?q=...` (`src/AppTemplate.App/Tweaks/NativeOps.cs` `WebSearch`)
  - Edge Web Store CRX reference embedded in `src/AppTemplate.App/Tweaks/SetupActions.cs` (`odfafepnkmbhccpbejgmiehpchacaeak;https://edge.microsoft.com/extensionwebstorebase/v1/crx`)

**OS tooling invoked as subprocesses (not network APIs but external programs):**
- `winget install/uninstall` (hidden, `--silent --accept-package-agreements --disable-interactivity --no-upgrade`) in `src/AppTemplate.App/Tweaks/NativeOps.cs` (`Winget`, `WingetInstall`, `WingetUninstall`); package cache read from `%LOCALAPPDATA%\Microsoft\WinGet\Packages` (`PackagePath`)
- `sc.exe` (service stop/config/delete/create/start, TrustedInstaller repointing), `schtasks` (CSV query + delete/enable/disable), `bcdedit` (safeboot, NX, hypervisor/VBS flags), `reg` / `regedit.exe /S` (hive load, `.reg` import), `powercfg` (scheme list/delete), `manage-bde -off` (BitLocker), `shutdown /r` (+ `/fw`), `attrib`, `taskkill`, `csc.exe` (.NET Framework compiler) - all wrapped by `RunTool` / `RunToolCapture` / `PowerShellCommand` / `RunPowerShellScript` in `src/AppTemplate.App/Tweaks/NativeOps.cs`
- `powershell.exe -NoProfile` (inline cmdlet for MMAgent/MpPreference, temp-`.ps1` for Appx/DISM package ops, Edge uninstaller, `InputBox` prompt bridge) in `src/AppTemplate.App/Tweaks/NativeOps.cs`

## Data Storage

**Databases:**
- None. No SQLite, no EF Core, no remote database, no ORM. State is registry + flat files only.

**File Storage:**
- Local filesystem only:
  - Settings: `%LOCALAPPDATA%\AkariToolbox\settings.json` (key/value JSON) via `src/AppTemplate.Framework/Services/FileSettingsStorage.cs`; folder/file paths defined in `src/AppTemplate.App/App.xaml.cs` (`SettingsFolder`, `SettingsFilePath`)
  - Logs: `%LOCALAPPDATA%\AkariToolbox\logs\app-yyyyMMdd.log` via `src/AppTemplate.Framework/Logging/FileLoggerProvider.cs` (wired in `src/AppTemplate.App/App.xaml.cs` `BuildHost`)
  - Staging: `%SystemRoot%\Temp` (`SystemRootTemp` in `src/AppTemplate.App/Tweaks/NativeOps.cs`) for downloads (`nvidiadriver.exe`, `amddriver.exe`, `7zip.exe`, `ddu.exe`, temp `.reg`/`.ps1`), DDU dir, 7-Zip extraction
  - Embedded payloads shipped inside exe: `src/AppTemplate.App/Tweaks/Data/**/*` (`EmbeddedResource` in `src/AppTemplate.App/AppTemplate.App.csproj`; `DduSettings.xml`, `.reg` blobs, GPU `.nip` profiles) read by `NativeOps.LoadWindowsData/LoadGraphicsData/LoadAdvancedData`
  - Shortcuts written to `%ProgramData%\Microsoft\Windows\Start Menu\Programs\` and Desktop via WScript.Shell COM + `.url` files (`src/AppTemplate.App/Tweaks/NativeOps.cs` `CreateShortcut`, `CreateUrlShortcut`, `StartMenuShortcut`, `DesktopShortcut`)
  - No cloud storage, no blob SDK, no sync

**Caching:**
- None beyond in-process static caches: OS/CPU/GPU strings cached per process in `src/AppTemplate.App/Tweaks/SystemInfo.cs` (`_osCache`, `_cpuCache`, `_gpuCache`); no `IMemoryCache`, no Redis, no HTTP cache headers

## Authentication & Identity

**Auth Provider:**
- Custom (Windows identity only, no login, no OAuth, no tokens)
  - Implementation: `WindowsIdentity.GetCurrent()` + `WindowsPrincipal.IsInRole(Administrator)` in `src/AppTemplate.App/Tweaks/SystemInfo.cs` (`Privileges()`); elevation enforced by `requireAdministrator` in `src/AppTemplate.App/app.manifest`
  - Single-instance identity key `AkariToolbox` via `AppInstance.FindOrRegisterForKey` in `src/AppTemplate.App/App.xaml.cs`
  - Headless safe-boot re-entry tokens are CLI flags, not credentials: `--ddu-auto`, `--ddu-manual`, `--defender-optimize`, `--defender-default`, `--defender-disable`, `--defender-enable`, `--services-off`, `--services-on` parsed from `Environment.GetCommandLineArgs()` in `src/AppTemplate.App/App.xaml.cs`

## Monitoring & Observability

**Error Tracking:**
- None (no Sentry/AppInsights/Crashpad). Unhandled `XAML` / `AppDomain` / `TaskScheduler` exceptions are logged to the file sink and shown in a dialog: `src/AppTemplate.App/App.xaml.cs` (`OnUnhandledException`, `OnAppDomainUnhandledException`, `OnUnobservedTaskException`)

**Logs:**
- `Microsoft.Extensions.Logging` with two providers wired in `src/AppTemplate.App/App.xaml.cs`: `AddDebug()` + custom `FileLoggerProvider` (`src/AppTemplate.Framework/Logging/FileLoggerProvider.cs`) writing `app-yyyyMMdd.log` (~1 MB cap, newest 10 retained). Consumed via `ILogger<T>` in view models and `Services`/`Helpers`; user-visible surfacing is `IInfoBarService` / `IDialogService` / `IStatusService` in `src/AppTemplate.Framework/Services/`

## CI/CD & Deployment

**Hosting:**
- No hosted backend. Desktop-only WinExe, unpackaged + self-contained (`WindowsPackageType=None`, `WindowsAppSDKSelfContained=true` in `src/AppTemplate.App/AppTemplate.App.csproj`), x64 single file output under `src/AppTemplate.App/bin/x64/.../win-x64/AkariToolbox.App.exe`

**CI Pipeline:**
- None detected (no `.github/` workflows, no `azure-pipelines.yml`, no `Dockerfile`). Local gates only: `build-and-run.ps1` (zero-error/zero-warning build + launch), `verify-pages.ps1` (UIA page sweep), `check-parity.ps1` (WPF string parity). `nuget.config` pins restore to nuget.org for any future CI.

## Environment Configuration

**Required env vars:**
- None. No `API_KEY`, no connection strings, no `IConfiguration` keys required at startup. `Host.CreateApplicationBuilder()` runs with defaults; `%SystemRoot%`, `%SystemDrive%`, `%LOCALAPPDATA%`, `%ProgramData%` are read from the OS at runtime (see `src/AppTemplate.App/Tweaks/NativeOps.cs` `Expand`, `SystemRootTemp`, `WinGetPackages`), not configured.
- Secrets location: not applicable - repo contains no `.env`, no user-secrets, no certificate/key files; `nuget.config` carries no credentials.

## Webhooks & Callbacks

**Incoming:**
- None. No HTTP server, no webhook endpoint, no `IHostedService` listener. Single-instance activation redirect (`RedirectActivationToAsync` in `src/AppTemplate.App/App.xaml.cs`) is the only cross-process callback and it just foregrounds `MainWindow`.

**Outgoing:**
- None on a schedule or event bus. All outbound HTTPS is user-initiated (clicking Install/Download/Check/Driver buttons) via `NativeOps.Download*` / `HttpGetString` / `NativeOps.Launch(url)` as listed above. No telemetry pings, no update-check endpoint, no crash-report upload.

---

*Integration audit: 2026-10-03*
