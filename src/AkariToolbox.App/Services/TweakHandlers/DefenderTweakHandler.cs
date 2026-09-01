using System.Diagnostics;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

// ITweakHandler here is a thin routing wrapper only; SetDefenderAsync's internals below
// are an unmodified port per D-01 — see 01-RESEARCH.md 'Defender carry-over scope'.
/// <summary>
/// The two-phase Windows Defender disable/re-enable workflow (TWEAKS-02) — the 32nd and
/// final tweak. Per D-01 (CONTEXT.md, an explicit, twice-repeated user directive) this is
/// NOT decomposed into the registry/service-primitive pattern the other 31 handlers use;
/// <see cref="SetDefenderAsync"/> and everything it calls is a byte-for-byte port of the
/// predecessor's <c>TweakService.cs</c> (<c>SetDefenderAsync</c>, <c>DefenderScheduleCleanup</c>,
/// <c>IsDefenderTamperProtectionOn</c>, <c>DefenderBuildServiceBat</c>,
/// <c>DefenderRunElevatedPsFileAsync</c>, <c>DefenderRunElevatedPsAsync</c>,
/// <c>DefenderRunAsTrustedInstallerAsync</c>) — only the logging mechanism changes
/// (the predecessor's static logger call is replaced by the injected
/// <see cref="ILogConsoleService"/>), plus a new, not-ported SHA256 integrity gate
/// (T-01-SC) added ahead of the Tamper Protection check.
/// </summary>
public sealed class DefenderTweakHandler(IPostInstallService postInstall, ILogConsoleService log, IRegistryService registry) : ITweakHandler
{
    // T-01-SC mitigation constants (new, Phase-1-scoped, NOT ported from the predecessor —
    // closes BLOCKER T-01-SC from phase-plan review). These two files are fetched at
    // runtime from the pinned GitHub PostInstall repo (RESEARCH Pitfall 5); their exact
    // bytes are not known during planning, so the digests below are trust-on-first-use
    // pins computed by downloading the exact same raw.githubusercontent.com URLs
    // EnsureDefenderFilesAsync()/EnsurePostInstallAsync() use and hashing the bytes with
    // SHA256 (equivalent to running `Get-FileHash -Algorithm SHA256` against the local
    // copy once it lands in C:\PostInstall\ — no live Windows test machine was available
    // during this automated implementation pass, so this pin should be re-confirmed
    // against the actual local files during the Task 2 human real-machine check).
    // SHA256 pinned 2026-09-01 from https://raw.githubusercontent.com/isleap9/PostInstall/main/PostInstall/Defender/NoDefender.cab
    private const string ExpectedNoDefenderCabSha256 = "cb0204461effd80c450bb2bab531e1e07fda4ef06b29d80f251b250bf43e0638";
    // SHA256 pinned 2026-09-01 from https://raw.githubusercontent.com/isleap9/PostInstall/main/PostInstall/Defender/DisableDefender.ps1
    private const string ExpectedDisableDefenderPs1Sha256 = "ef4b85ae5dac8b756bc3c24d6d9bad334e0270dadcf93ea50263d2f70426d4ea";

    // Defender's own explicitly-scoped state flag (D-03/D-04 exemption, per D-01) — this
    // app deliberately never creates the predecessor's HKCU\Software\AkariTool hive
    // (Pitfall 4, applies to every other handler), so Defender gets a small dedicated
    // location instead of reusing that exact path.
    private const string DefenderStateKey = @"Software\AkariToolbox\DefenderState";
    private const string DefenderStateValue = "DisableDefender";

    private const string WinNoDefenderCab = @"C:\Windows\NoDefender.cab";

    private static readonly string[] DefenderServices =
    {
        "MsSecCore", "MsSecFlt", "MsSecWfp", "SecurityHealthService",
        "Sense", "WdBoot", "WdFilter", "WdNisDrv", "WdNisSvc",
        "WinDefend", "wscsvc", "MDCoreSvc", "SgrmAgent", "SgrmBroker",
        "webthreatdefsvc", "webthreatdefusersvc",
    };

    private static readonly string[] DefenderScheduledTasks =
    {
        @"\Microsoft\Windows\Windows Defender\Windows Defender Cache Maintenance",
        @"\Microsoft\Windows\Windows Defender\Windows Defender Cleanup",
        @"\Microsoft\Windows\Windows Defender\Windows Defender Scheduled Scan",
        @"\Microsoft\Windows\Windows Defender\Windows Defender Verification",
    };

    public string Key => "defender";

    public string Title => "Disable Defender";

    public string Description => "Toggle Windows Defender On or Off";

    public int Order => 30;

    public bool GetState() =>
        registry.GetValue(RegistryHive.CurrentUser, DefenderStateKey, DefenderStateValue) is int v && v != 0;

    // Mirrors the predecessor's own `SetDefender(bool disable) => _ = SetDefenderAsync(disable);`
    // shape exactly (TweakService.cs:828) — ITweakHandler.SetState is a synchronous contract
    // member and this tweak's real work is deliberately fire-and-forget, same as upstream.
    // The DefenderStateValue flag itself is written/removed from inside SetDefenderAsync, at
    // the same points the predecessor persisted/cleared its own equivalent "DisableDefender" flag.
    public void SetState(bool disable) => _ = SetDefenderAsync(disable);

    private async Task SetDefenderAsync(bool disable)
    {
        try
        {
            // Auto-download required files if missing (matches Akari Tool Premium).
            // On AkariOS, all files already exist and this returns instantly.
            // On a fresh VM / stock Windows, this fetches ~30MB from GitHub.
            bool filesReady = disable
                ? await postInstall.EnsureDefenderFilesAsync()
                : await postInstall.EnsureMinSudoAsync();

            if (!filesReady)
            {
                log.Log("[DEFENDER] Could not obtain required files. Check your internet connection and try again.");
                return;
            }

            if (disable)
            {
                if (GetState()) return;

                log.Log("[DEFENDER] Disabling Windows Defender...");

                // T-01-SC mitigation (new, not ported from the predecessor): verify both
                // downloaded Defender-critical assets before any copy/execution. A mismatch
                // aborts with no partial state change — same early-return shape as the
                // Tamper Protection gate below.
                var noDefenderOk = await postInstall.VerifyFileSha256Async(postInstall.NoDefenderPath, ExpectedNoDefenderCabSha256);
                var disableScriptOk = await postInstall.VerifyFileSha256Async(
                    Path.Combine(postInstall.LocalRoot, "Defender", "DisableDefender.ps1"), ExpectedDisableDefenderPs1Sha256);

                if (!noDefenderOk || !disableScriptOk)
                {
                    log.Log("[DEFENDER] ERROR: Integrity check failed for a downloaded PostInstall asset — refusing to proceed. Delete C:\\PostInstall\\Defender and retry to re-download.");
                    return;
                }

                log.Log("[DEFENDER] Checking Tamper Protection status...");

                if (IsDefenderTamperProtectionOn())
                {
                    log.Log("[DEFENDER] ERROR: Tamper Protection is ON.");
                    log.Log("[DEFENDER] Go to: Windows Security -> Virus & threat protection");
                    log.Log("[DEFENDER]   -> Manage settings -> Tamper Protection -> Off");
                    log.Log("[DEFENDER] Then try again.");
                    return;
                }

                log.Log("[DEFENDER] Tamper Protection is off — proceeding.");
                log.Log("[DEFENDER] Preparing NoDefender package...");
                File.Copy(postInstall.NoDefenderPath, WinNoDefenderCab, overwrite: true);

                log.Log("[DEFENDER] Installing NoDefender (30-60s)...");
                await DefenderRunElevatedPsFileAsync(
                    Path.Combine(postInstall.LocalRoot, @"Defender\DisableDefender.ps1"));

                log.Log("[DEFENDER] Scheduling post-reboot service cleanup...");
                await DefenderScheduleCleanup();

                registry.SetValue(RegistryHive.CurrentUser, DefenderStateKey, DefenderStateValue, 1, RegistryValueKind.DWord);
                log.Log("[DEFENDER] Phase 1 complete. Please restart now.");
                log.Log("[DEFENDER] On next login, Phase 2 will finish disabling Defender automatically.");
            }
            else
            {
                log.Log("[DEFENDER] Re-enabling Windows Defender...");
                log.Log("[DEFENDER] Restoring Defender package (30-60s)...");

                await DefenderRunElevatedPsAsync(
                    $"if (Test-Path '{WinNoDefenderCab}') " +
                    $"{{ Remove-WindowsPackage -Online -PackagePath '{WinNoDefenderCab}' -NoRestart }}");

                log.Log("[DEFENDER] Restoring Defender services...");
                await DefenderRunAsTrustedInstallerAsync(DefenderBuildServiceBat(startValue: 2));

                registry.DeleteValue(RegistryHive.CurrentUser, DefenderStateKey, DefenderStateValue);
                log.Log("[DEFENDER] Defender re-enabled. Restart required.");
            }
        }
        catch (Exception ex)
        {
            log.Log($"[DEFENDER] ERROR: {ex.Message}");
        }
    }

    private async Task DefenderScheduleCleanup()
    {
        var batPath = Path.Combine(postInstall.LocalRoot, @"Defender\AkariDefenderCleanup.bat");
        var sysCmd = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
        var powerRun = postInstall.PowerRunPath;

        var lines = new List<string>
        {
            "@echo off",
            ":: AkariTool — Defender Phase 2 cleanup (runs once after reboot)",
            "",
            ":: Disable real-time monitoring first",
            @"PowerShell -NonInteractive -NoLogo -NoProfile -C ""Set-MpPreference -DisableRealtimeMonitoring 1"" >NUL 2>nul",
            "",
            ":: Kill all Defender service registry keys (ControlSet001)",
        };

        foreach (var cmd in DefenderBuildServiceBat(startValue: 4))
            lines.Add($@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c {cmd}");

        lines.AddRange(new[]
        {
            "",
            ":: Remove SecurityHealth from Run key",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe delete ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v ""SecurityHealth"" /f",
            "",
            ":: Disable SmartScreen binary",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c taskkill /f /im smartscreen.exe",
            $@"if not exist ""%systemroot%\system32\smartscreen.exe.old"" if exist ""%systemroot%\system32\smartscreen.exe"" (",
            $@"  ""{powerRun}"" /SW:0 ""{sysCmd}"" /c takeown /F ""%systemroot%\system32\smartscreen.exe"" /A",
            $@"  ""{powerRun}"" /SW:0 ""{sysCmd}"" /c icacls ""%systemroot%\system32\smartscreen.exe"" /grant Administrators:F",
            $@"  ""{powerRun}"" /SW:0 ""{sysCmd}"" /c copy ""%systemroot%\system32\smartscreen.exe"" ""%systemroot%\system32\smartscreen.exe.old"" /v",
            $@"  ""{powerRun}"" /SW:0 ""{sysCmd}"" /c del ""%systemroot%\system32\smartscreen.exe""",
            ")",
            "",
            ":: SmartScreen registry keys",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer"" /v ""SmartScreenEnabled"" /t REG_SZ /d ""Off"" /f",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKLM\Software\Policies\Microsoft\System"" /v ""EnableSmartScreen"" /t REG_DWORD /d ""0"" /f",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows Defender\SmartScreen"" /v ""ConfigureAppInstallControlEnabled"" /t REG_DWORD /d ""0"" /f",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKLM\Software\Policies\Microsoft\Windows Defender\SmartScreen"" /v ""EnableSmartScreen"" /t REG_DWORD /d ""0"" /f",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKCU\Software\Microsoft\Windows\CurrentVersion\AppHost"" /v ""EnableWebContentEvaluation"" /t REG_DWORD /d ""0"" /f",
            "",
            ":: CI/Policy and DeviceGuard keys",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKLM\SYSTEM\ControlSet001\Control\CI\Policy"" /v ""VerifiedAndReputablePolicyState"" /t REG_DWORD /d ""0"" /f",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKLM\Software\Microsoft\Windows Defender"" /v ""PUAProtection"" /t REG_DWORD /d ""0"" /f",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKLM\SYSTEM\ControlSet001\Control\CI\Config"" /v ""VulnerableDriverBlocklistEnable"" /t REG_DWORD /d ""0"" /f",
            $@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c Reg.exe add ""HKLM\SYSTEM\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity"" /v ""Enabled"" /t REG_DWORD /d ""0"" /f",
            "",
            ":: Disable Defender scheduled tasks",
        });

        foreach (var task in DefenderScheduledTasks)
            lines.Add($@"""{powerRun}"" /SW:0 ""{sysCmd}"" /c schtasks.exe /change /disable /TN ""{task}""");

        lines.AddRange(new[]
        {
            "",
            ":: Self-cleanup",
            $@"Reg.exe delete ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"" /v ""AkariDefenderCleanup"" /f >NUL 2>nul",
            $@"(del /f /q ""%~f0"") >NUL 2>nul",
        });

        await File.WriteAllLinesAsync(batPath, lines);

        var cmdExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
        registry.SetValue(
            RegistryHive.LocalMachine,
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            "AkariDefenderCleanup",
            $"\"{cmdExe}\" /c \"{batPath}\"",
            RegistryValueKind.String);

        log.Log($"[DEFENDER] Phase 2 bat written to: {batPath}");
        log.Log("[DEFENDER] It will run automatically on next login.");
    }

    private bool IsDefenderTamperProtectionOn()
    {
        try
        {
            var val = registry.GetValue(
                RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows Defender\Features", "TamperProtection");
            return val is not int i || i != 4;
        }
        catch
        {
            return true;
        }
    }

    private static string[] DefenderBuildServiceBat(int startValue) =>
        DefenderServices
            .Select(svc =>
                $@"Reg.exe add ""HKLM\SYSTEM\ControlSet001\Services\{svc}"" /v ""Start"" /t REG_DWORD /d ""{startValue}"" /f")
            .ToArray();

    // This IS a second elevation prompt even though the app is already elevated; this is
    // intentional pre-existing predecessor behavior (RESEARCH "Known Threat Patterns"
    // table, T-01-14 accept) — do not remove or "fix" the redundant runas.
    private static async Task DefenderRunElevatedPsFileAsync(string ps1Path)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -NonInteractive -NoLogo -NoProfile -File \"{ps1Path}\"",
            UseShellExecute = true,
            Verb = "runas",
            CreateNoWindow = false,
        };
        await Process.Start(psi)!.WaitForExitAsync();
    }

    private static async Task DefenderRunElevatedPsAsync(string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NonInteractive -NoLogo -NoProfile -C \"{command}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        await Process.Start(psi)!.WaitForExitAsync();
    }

    private async Task DefenderRunAsTrustedInstallerAsync(IEnumerable<string> commands)
    {
        var tmp = Path.Combine(Path.GetTempPath(), $"AkariDef-{Guid.NewGuid():N}.bat");
        try
        {
            var lines = new List<string> { "@echo off" };
            lines.AddRange(commands);
            await File.WriteAllLinesAsync(tmp, lines);

            var psi = new ProcessStartInfo
            {
                FileName = postInstall.MinSudoPath,
                Arguments = $"--NoLogo --TrustedInstaller --Privileged cmd /c \"{tmp}\"",
                UseShellExecute = true,
                CreateNoWindow = false,
            };
            await Process.Start(psi)!.WaitForExitAsync();
        }
        finally
        {
            try { File.Delete(tmp); } catch { /* best-effort temp cleanup, matches predecessor */ }
        }
    }
}
