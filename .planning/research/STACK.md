# Stack Research

**Domain:** WinUI 3 desktop system-tweak/debloat utility (Windows 10/11), unpackaged self-contained, requires admin elevation
**Researched:** 2026-08-31
**Confidence:** MEDIUM

The base stack is already decided (Windows App SDK 2.3.1, WinUI 3, CommunityToolkit.Mvvm, Microsoft.Extensions.Hosting, unpackaged self-contained, `WinUI-3-MVVM-Framework` template). This document covers what to add **on top of that base** for the five capabilities the port needs: registry/service manipulation, PowerShell execution, admin elevation as an unpackaged app, integrity-verified downloads, and packaging/distribution.

## Recommended Stack

### Core Technologies (already decided — confirmed current)

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| Windows App SDK | 2.3.1 (stable channel) | WinUI 3 runtime/APIs | Confirmed current stable release (July 2026); 2.4.x is experimental-channel only — do not move to it for this project. |
| .NET | 10 (LTS) | Runtime/SDK | Matches framework template; current LTS as of this research. |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM (ObservableObject, RelayCommand/AsyncRelayCommand, Messenger) | Latest stable on the 8.x line; framework template already depends on it — no version bump needed to support this project's features. |
| Microsoft.Extensions.Hosting | current 9.x/10.x aligned to TFM | DI container, generic host, logging | Already in the framework template for DI/config; no reason to change for tweak/debloat features. |

### Supporting Libraries — new for this port

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `System.ServiceProcess.ServiceController` | 9.0.9 (or the version matching the project's net10 TFM once published; 9.x packages are net8.0+ and forward-compatible) | Query/start/stop/change Windows services (Gaming Tweaks service tuning, Debloat) | **Must be added explicitly** — `ServiceController` is NOT part of the .NET shared framework for non-`Microsoft.NET.Sdk.WindowsDesktop` projects; a WinUI 3 SDK-style project needs the NuGet package or the build fails/APIs are missing. |
| `Microsoft.Win32.Registry` APIs (`Microsoft.Win32.Registry`, `RegistryKey`) | In-box, no package needed | Read/write/delete registry keys for the 32 OS tweaks | Already available via the BCL on Windows; no NuGet reference required (do not add the standalone `Microsoft.Win32.Registry` NuGet package — it's for non-Windows TFMs, not applicable here). |
| `System.Security.Cryptography` (`SHA256`) | In-box | Compute/verify checksums on downloaded PostInstall assets | In-box, no package needed — stream the downloaded file through `SHA256.HashDataAsync`/`ComputeHash` and compare to an expected hex digest before treating the download as valid. |
| `System.Net.Http` (`HttpClient`/`IHttpClientFactory`) | In-box | Download PostInstall assets and GitHub release metadata | Register via `IHttpClientFactory` in the existing `Microsoft.Extensions.Hosting` DI container (framework already wires DI) rather than `new HttpClient()` per call — avoids socket exhaustion on repeated self-heal checks. |

### What deliberately stays OUT of the stack

| Considered | Verdict | Why not |
|------------|---------|---------|
| `Microsoft.PowerShell.SDK` (in-process PowerShell hosting via `System.Management.Automation.PowerShell`) | Do not add | Adds ~50-100MB to an already-large self-contained publish, and its runspace behaves differently from `pwsh.exe`/`powershell.exe` for some cmdlets (e.g., `Start-Job` doesn't work the same without the real executable). The predecessor's proven pattern — extract embedded `.ps1` resources to a temp path, run via `Process.Start powershell.exe -ExecutionPolicy Bypass -File <path>` — is simpler, has zero extra dependency weight, and is explicitly called out in PROJECT.md as the pattern to keep (`ToolService`). |
| Octokit.net (GitHub API client) | Do not add | The self-heal use case is a fixed, known repo/asset path (mirroring `PostInstall` from a specific GitHub repo) — a plain `HttpClient` GET against `raw.githubusercontent.com` or the release-asset download URL is enough. Octokit is worth it for apps that browse/query the GitHub API generally (search, issues, multiple repos); this app does neither. Anonymous REST API calls are rate-limited (tightened further in 2025) — prefer direct asset/raw-content URLs over the `/repos/.../releases` API to sidestep rate limits entirely. |
| Velopack (auto-update framework) | Do not add for v1 | Velopack is the modern standard for *auto-updating* .NET desktop apps, but this app already has its own GitHub-based self-heal/asset-mirror mechanism and the predecessor's proven "just run the exe" manual-distribution model. Adding Velopack would introduce a second, overlapping update mechanism. Revisit only if a future milestone wants automatic app-binary updates (not asset updates). |

## Installation

```xml
<!-- App.csproj — new package references for this port -->
<ItemGroup>
  <PackageReference Include="System.ServiceProcess.ServiceController" Version="9.0.9" />
</ItemGroup>
```

No other new NuGet packages are required — registry access, HTTP, and SHA256 are all in-box BCL APIs, and PowerShell execution stays process-based (no SDK package).

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|--------------------------|
| `Process.Start(powershell.exe, -ExecutionPolicy Bypass -File script.ps1)` for embedded PS scripts | `Microsoft.PowerShell.SDK` in-process runspace | If a future phase needs structured object return values from PowerShell (not just exit codes/stdout text), or needs to run many scripts per second without process-spawn overhead — the SDK's in-process runspace gives real PowerShell objects back to C# instead of parsed text. |
| `System.ServiceProcess.ServiceController` + `sc.exe` fallback for startup-type changes | Direct P/Invoke to the Windows Service Control Manager API (`OpenSCManager`/`ChangeServiceConfig`) | If you need to set delayed-auto-start, failure actions, or other config `ServiceController` doesn't expose and want to avoid spawning `sc.exe` as a subprocess — P/Invoke is more code but no subprocess. Not worth it here; `sc.exe` via `Process` is what the predecessor already does successfully. |
| Portable self-contained single-file EXE (`PublishSingleFile`) | Inno Setup installer | If v2 scope adds Start Menu shortcuts, an uninstaller entry in Add/Remove Programs, or bundling the ~110-script "Ultimate" tweak collection as installed content rather than embedded resources — Inno Setup natively supports `PrivilegesRequired=admin` and is the standard free Windows installer authoring tool. |
| Direct `HttpClient` GET against known asset URL | GitHub REST API (`/repos/{owner}/{repo}/releases`) via `HttpClient` or Octokit | If the self-heal logic needs to resolve "latest release" dynamically (rather than a fixed branch/path) — use the REST API with a `User-Agent` header (GitHub requires one) and accept the anonymous rate limit, or use a PAT if the check runs frequently. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|--------------|
| Adding `requestedExecutionLevel=requireAdministrator` to `app.manifest` while leaving `WindowsAppSDKSelfContained=true` on an old/cached Windows App SDK version | A known build-time manifest-merge bug (`WindowsAppSDK#3054`, `c1010001 Values of attribute 'level' not equal`) affected self-contained + custom-manifest combos on early 1.x SDK versions | Confirm the project actually resolves to Windows App SDK 2.3.1 (it should, per the framework template) — this was fixed internally at milestone 1.3, well before 2.3.1. If the build error reappears, it means a stale/pinned older SDK version is in play; check `packages.lock.json`/central package version, not a manifest workaround. |
| Packaging as MSIX (`Package.appxmanifest`) while also requiring elevation | WinUI 3's documented, still-relevant limitation: WinRT/COM per-user activation registrations aren't visible to an elevated process for **packaged** apps, so packaged+elevated WinUI 3 apps can misbehave or fail to activate WinRT types | Stay unpackaged (already the project's constraint) — unpackaged apps use the standard Win32 `requireAdministrator` manifest path (same as WPF/WinForms) and don't hit this packaged-app-specific WinRT limitation. |
| `new HttpClient()` per download/self-heal check | Socket exhaustion / DNS-staleness under repeated instantiation, a well-known .NET pitfall | Register a named/typed client via `IHttpClientFactory` in the existing DI container. |
| Raw `RegistryKey.SetValue` without existence/type checks ("registry squatting" risk, throwing on missing keys) | Can silently create a key under a namespace another (possibly malicious) process already claimed, or crash on first-run machines missing an expected key | Use `OpenSubKey`/`GetValue` with null-checks before writing, and prefer `CreateSubKey` only when the tweak is explicitly meant to create the key. |

## Stack Patterns by Variant

**If a tweak only needs a registry value flip (most of the 32 OS tweaks / gaming toggles):**
- Use `Microsoft.Win32.Registry`/`RegistryKey` directly in a `TweakService`-style class, mirroring the predecessor.
- Because it's synchronous, dependency-free, and instantly reflects "current state" for the toggle UI — no process spawn needed.

**If a tweak/debloat action needs multi-step logic, external tool invocation, or the existing predecessor script already implements it (the 28 Debloat actions, the two-phase Disable Defender workflow):**
- Keep it as an embedded `.ps1` resource extracted to a temp path and run via `Process.Start`, capturing stdout/exit code.
- Because rewriting proven, tested PowerShell logic into C# for parity-first v1 is wasted effort and risks subtle behavior changes; the port's goal is parity, not rewrite.

**If a downloaded asset's integrity matters (PostInstall mirror, any future "Ultimate" third-party tool downloads):**
- Compute SHA256 over the downloaded stream before moving it into place, compare against a pinned expected hash (checked into the app or fetched alongside the asset), and delete/retry on mismatch rather than silently accepting a partial/corrupted file.
- Because this is a system-tweak tool running elevated — executing a corrupted or tampered downloaded binary/script under admin rights is the single highest-severity failure mode in this app's threat model.

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|------------------|-------|
| `System.ServiceProcess.ServiceController` 9.0.x | .NET 10 (net10.0 TFM) | Package multi-targets net8.0+/netstandard2.0/net462; 9.0.x builds run fine on a net10.0 app (no net10.0-specific package has been observed as required — verify no build warning appears once added, and bump to a net10-targeted version if/when Microsoft ships one). |
| Windows App SDK 2.3.1 | .NET 10, `WindowsAppSDKSelfContained=true` | Confirmed as the current stable release; do not mix with 2.4.x experimental-channel packages in the same project. |
| `app.manifest` `requireAdministrator` | `WindowsAppSDKSelfContained=true` | Works on 2.3.1 — the merge-conflict bug that historically blocked this combination was fixed internally well before this version. Treat a recurrence as a signal of SDK version drift, not a fundamental incompatibility. |

## Sources

- [Distribute an unpackaged WinUI 3 app — Microsoft Learn](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app) — MEDIUM (official docs, cross-checked)
- [WindowsAppSDK Discussion #3038 — run unpackaged WinUI 3 elevated](https://github.com/microsoft/WindowsAppSDK/discussions/3038) — MEDIUM (cross-checked against #671 and Q&A thread)
- [WindowsAppSDK Discussion #671 — elevated privilege / WinRT activation limitation](https://github.com/microsoft/WindowsAppSDK/discussions/671) — MEDIUM (confirms the limitation is packaged-app-specific)
- [Microsoft Q&A — How to set up WinUI3 applications to run as administrators](https://learn.microsoft.com/en-us/answers/questions/1692811/how-to-set-up-winui3-applications-to-run-as-admini) — MEDIUM
- [WindowsAppSDK Issue #3054 (mirrored as microsoft-ui-xaml#7560) — manifest merge conflict on self-contained + requireAdministrator, fixed internally at milestone 1.3](https://github.com/microsoft/microsoft-ui-xaml/issues/7560) — MEDIUM
- [WindowsAppSDK Discussion #4553 — manifest merging for self-contained apps](https://github.com/microsoft/WindowsAppSDK/discussions/4553) — LOW (relevant mainly to non-MSBuild build systems, not this project's standard MSBuild setup)
- [What's new: SDK, WinUI, tools — Microsoft Learn, confirms Windows App SDK 2.3.1 stable / 2.4.x experimental](https://learn.microsoft.com/en-us/windows/apps/whats-new/whats-new-for-developers) — MEDIUM
- [NuGet Gallery — System.ServiceProcess.ServiceController](https://www.nuget.org/packages/System.ServiceProcess.ServiceController) — LOW (version/TFM confirmation only)
- [NuGet Gallery — CommunityToolkit.Mvvm 8.4.2](https://www.nuget.org/packages/CommunityToolkit.Mvvm) — LOW (version confirmation only)
- [NuGet Gallery — Microsoft.PowerShell.SDK](https://www.nuget.org/packages/Microsoft.PowerShell.SDK) — LOW (version/purpose confirmation only)
- [Running PowerShell from C# in 2025 — CodeCube Ventures](https://codecube.net/2025/7/powershell-from-csharp-updated/) — LOW (community source, directionally consistent with official PowerShell docs)
- [Choosing the right PowerShell NuGet package for your .NET project — Microsoft Learn](https://learn.microsoft.com/en-us/powershell/scripting/dev-cross-plat/choosing-the-right-nuget-package) — MEDIUM (official)
- [GitHub Changelog — Updated rate limits for unauthenticated requests (2025-05-08)](https://github.blog/changelog/2025-05-08-updated-rate-limits-for-unauthenticated-requests/) — MEDIUM (official GitHub source)
- [Distributing Windows applications — AugmentedMind.de](https://www.augmentedmind.de/2021/05/30/distributing-windows-applications/) — LOW (community source on portable-exe vs installer tradeoffs)
- [Read and Write Windows Registry in C# — Code Maze](https://code-maze.com/csharp-read-and-write-windows-registry/) — LOW (community source; registry-squatting guidance corroborated by Microsoft Learn's VB registry article)
- [Reading from and Writing to the Registry Using the Microsoft.Win32 Namespace — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/visual-basic/developing-apps/programming/computer-resources/reading-from-and-writing-to-the-registry-using-the-microsoft-win32-namespace) — MEDIUM (official)

---
*Stack research for: WinUI 3 system-tweak/debloat desktop utility*
*Researched: 2026-08-31*
