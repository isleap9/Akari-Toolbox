using System.Diagnostics;
using System.Linq;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

// ITweakHandler here is a thin routing wrapper only; SetDefenderAsync's internals below
// are a port of the predecessor's TweakService.cs per D-01 — see 01-RESEARCH.md 'Defender
// carry-over scope'.
/// <summary>
/// The two-phase Windows Defender disable/re-enable workflow (TWEAKS-02) — the 32nd and
/// final tweak. Per D-01 (CONTEXT.md, an explicit, twice-repeated user directive) this is
/// NOT decomposed into the registry/service-primitive pattern the other 31 handlers use;
/// <see cref="SetDefenderAsync"/> and everything it calls remains a port of the
/// predecessor's <c>TweakService.cs</c> two-phase Defender workflow (Tamper Protection
/// gate, cab+ps1 install, post-reboot phase 2).
///
/// Two mechanisms were deliberately replaced (per explicit project-owner direction) with
/// native, self-contained equivalents adapted from the sibling AkariTool repo:
/// <list type="bullet">
/// <item>Elevation: <see cref="ElevationService.RunAsSystem"/> (native SYSTEM
/// impersonation) replaces the predecessor's <c>MinSudo.exe</c>/<c>PowerRun.exe</c>
/// external binaries — no external executable is launched to gain SYSTEM rights any more
/// (closes CR-01/CR-03 from 01-REVIEW.md). This also makes <see cref="SetState"/> block
/// for the duration of the operation, since the native path no longer needs the
/// generated-.bat-plus-RunOnce indirection that motivated the original fire-and-forget
/// shape.</item>
/// <item>Asset delivery: <c>NoDefender.cab</c> and <c>DisableDefender.ps1</c> are embedded
/// assembly resources (see the .csproj) instead of being downloaded on demand into
/// <c>C:\PostInstall\</c> — per explicit project-owner direction, the Defender workflow has
/// no runtime dependency on <c>IPostInstallService</c> at all. Because the bytes are fixed
/// at build time rather than fetched from the network, the SHA256 integrity gate that
/// guarded the downloaded copies (T-01-SC) is no longer meaningful here and was removed —
/// embedded resources are not a network trust boundary the way a runtime download is.</item>
/// </list>
/// Neither change is a decomposition into the <see cref="ITweakHandler"/>
/// registry/service-primitive pattern (that remains out of scope, SEC-01/v2) — they are
/// internal implementation swaps inside the still-special-cased Defender handler. The
/// overall two-phase Defender *workflow* shape (Tamper Protection gate, cab+ps1 install,
/// RunOnce-scheduled phase 2) is unchanged from the predecessor's.
/// </summary>
public sealed class DefenderTweakHandler(ILogConsoleService log, IRegistryService registry) : ITweakHandler
{
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

    // CR-01 fix (01-REVIEW.md): must block for the duration of the operation, matching
    // every other handler's contract, so TweakCatalog.SetStateAsync's per-key semaphore
    // (held for the duration of `Task.Run(() => handler.SetState(enabled))`) actually
    // serializes concurrent Defender toggles instead of releasing almost instantly while
    // the real work continues in the background.
    public void SetState(bool disable) => SetDefenderAsync(disable).GetAwaiter().GetResult();

    private async Task SetDefenderAsync(bool disable)
    {
        try
        {
            if (disable)
            {
                if (GetState()) return;

                log.Log("[DEFENDER] Disabling Windows Defender...");
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

                var cabTemp = await ExtractEmbeddedAsync(".NoDefender.cab", "NoDefender.cab");
                var ps1Temp = await ExtractEmbeddedAsync(".DisableDefender.ps1", "DisableDefender.ps1");
                try
                {
                    File.Copy(cabTemp, WinNoDefenderCab, overwrite: true);

                    log.Log("[DEFENDER] Installing NoDefender (30-60s)...");
                    await DefenderRunElevatedPsFileAsync(ps1Temp);
                }
                finally
                {
                    try { File.Delete(cabTemp); } catch { /* best-effort temp cleanup */ }
                    try { File.Delete(ps1Temp); } catch { /* best-effort temp cleanup */ }
                }

                // CR-01/CR-03 fix: schedule the native post-reboot phase 2 via a RunOnce
                // entry that re-launches this app itself (--defender-phase2), instead of
                // generating an AkariDefenderCleanup.bat full of unverified PowerRun.exe
                // invocations.
                log.Log("[DEFENDER] Scheduling native post-reboot phase 2...");
                DefenderPhase2Scheduler.ScheduleRunOnce();

                registry.SetValue(RegistryHive.CurrentUser, DefenderStateKey, DefenderStateValue, 1, RegistryValueKind.DWord);
                log.Log("[DEFENDER] Phase 1 complete. Please restart now.");
                log.Log("[DEFENDER] On next login, Phase 2 will finish disabling Defender automatically.");
            }
            else
            {
                // No PostInstall dependency for re-enable either: native SYSTEM
                // impersonation needs no downloaded/embedded file at all.
                log.Log("[DEFENDER] Re-enabling Windows Defender...");
                log.Log("[DEFENDER] Restoring Defender package (30-60s)...");

                await DefenderRunElevatedPsAsync(
                    $"if (Test-Path '{WinNoDefenderCab}') " +
                    $"{{ Remove-WindowsPackage -Online -PackagePath '{WinNoDefenderCab}' -NoRestart }}");

                log.Log("[DEFENDER] Restoring Defender services (native SYSTEM writes)...");
                var restoreOk = ElevationService.RunAsSystem(() =>
                {
                    foreach (var svc in DefenderServices)
                    {
                        try
                        {
                            Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\{svc}",
                                "Start", 2, RegistryValueKind.DWord);
                        }
                        catch (Exception ex)
                        {
                            log.Log($"[DEFENDER]   {svc} FAILED — {ex.Message}");
                        }
                    }
                }, log.Log);

                if (!restoreOk)
                {
                    log.Log("[DEFENDER] ERROR: Could not acquire SYSTEM to restore Defender services.");
                }

                registry.DeleteValue(RegistryHive.CurrentUser, DefenderStateKey, DefenderStateValue);
                log.Log("[DEFENDER] Defender re-enabled. Restart required.");
            }
        }
        catch (Exception ex)
        {
            log.Log($"[DEFENDER] ERROR: {ex.Message}");
        }
    }

    /// <summary>
    /// The real post-reboot phase-2 work — equivalent to the sibling AkariTool repo's
    /// <c>DefenderService.RunPhase2Native</c>. With ELAM lifted after the phase-1 reboot,
    /// writes every Defender service key to Start=4, disables real-time monitoring, strips
    /// SmartScreen (registry + binary takeover), sets CI/policy + DeviceGuard keys, and
    /// disables the Defender scheduled tasks — all inside a single native SYSTEM
    /// impersonation (no PowerRun.exe/MinSudo.exe). A failure on one step is logged and
    /// does not abort the rest. Intended to be invoked headlessly when the app is
    /// relaunched with the <c>--defender-phase2 &lt;token&gt;</c> arguments that
    /// <see cref="DefenderPhase2Scheduler.ScheduleRunOnce"/> schedules.
    /// </summary>
    /// <param name="providedToken">
    /// The token argument the relaunch was invoked with. T-01-17 (01-SECURITY.md security
    /// audit): a bare <c>--defender-phase2</c> flag is a static, discoverable trigger — any
    /// process already holding a full-Administrator token could otherwise invoke this
    /// directly and silently disable Defender/SmartScreen with no UI trace and no Tamper
    /// Protection re-check. This method refuses to do anything unless
    /// <paramref name="providedToken"/> matches the single-use token
    /// <see cref="DefenderPhase2Scheduler.ScheduleRunOnce"/> persisted when Phase 1 actually
    /// completed — verified and consumed via <see cref="DefenderPhase2Scheduler.ConsumeToken"/>
    /// before any registry/service mutation happens.
    /// </param>
    public static void RunPhase2Native(string? providedToken, Action<string> log)
    {
        // Belt-and-braces: RunOnce already self-clears when Windows runs it, but clear it
        // explicitly too, matching the reference implementation.
        DefenderPhase2Scheduler.ClearRunOnce();

        if (!DefenderPhase2Scheduler.ConsumeToken(providedToken))
        {
            log("[PHASE2] ERROR: missing or invalid phase-2 token — refusing to run. This invocation was not scheduled by a completed Phase 1 disable.");
            return;
        }

        bool ok = ElevationService.RunAsSystem(() =>
        {
            // 1. ELAM service keys -> Start=4 (disabled). Post-reboot these are writable.
            log("[PHASE2] Setting Defender service keys to Start=4...");
            foreach (var svc in DefenderServices)
            {
                try
                {
                    Registry.SetValue($@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Services\{svc}",
                        "Start", 4, RegistryValueKind.DWord);
                    log($"[PHASE2]   {svc} Start=4 OK");
                }
                catch (Exception ex) { log($"[PHASE2]   {svc} Start=4 FAILED — {ex.Message}"); }
            }

            // 2. Disable real-time monitoring (ordinary elevated-context PowerShell call —
            // not MinSudo/PowerRun; this whole block already runs as SYSTEM).
            log("[PHASE2] Disabling real-time monitoring...");
            RunProcess("powershell.exe",
                "-NonInteractive -NoLogo -NoProfile -C \"Set-MpPreference -DisableRealtimeMonitoring 1\"", log);

            // 3. Remove SecurityHealth systray Run entry.
            log("[PHASE2] Removing SecurityHealth Run entry...");
            try
            {
                using var run = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
                run?.DeleteValue("SecurityHealth", throwOnMissingValue: false);
            }
            catch (Exception ex) { log($"[PHASE2]   SecurityHealth delete FAILED — {ex.Message}"); }

            // 4. SmartScreen takeover — registry (native) + binary rename (stock tools).
            log("[PHASE2] Disabling SmartScreen...");
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer",
                "SmartScreenEnabled", "Off", RegistryValueKind.String);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\System",
                "EnableSmartScreen", 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\SmartScreen",
                "ConfigureAppInstallControlEnabled", 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Policies\Microsoft\Windows Defender\SmartScreen",
                "EnableSmartScreen", 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\AppHost",
                "EnableWebContentEvaluation", 0, RegistryValueKind.DWord);

            var sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var smartScreen = Path.Combine(sys32, "smartscreen.exe");
            var smartScreenOld = Path.Combine(sys32, "smartscreen.exe.old");
            RunProcess("taskkill.exe", "/f /im smartscreen.exe", log);
            if (!File.Exists(smartScreenOld) && File.Exists(smartScreen))
            {
                RunProcess("takeown.exe", $"/F \"{smartScreen}\" /A", log);
                RunProcess("icacls.exe", $"\"{smartScreen}\" /grant Administrators:F", log);
                try
                {
                    File.Copy(smartScreen, smartScreenOld, overwrite: false);
                    File.Delete(smartScreen);
                    log("[PHASE2]   smartscreen.exe renamed to .old");
                }
                catch (Exception ex) { log($"[PHASE2]   smartscreen.exe takeover FAILED — {ex.Message}"); }
            }

            // 5. CI/Policy + DeviceGuard keys.
            log("[PHASE2] Setting CI/Policy and DeviceGuard keys...");
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\CI\Policy",
                "VerifiedAndReputablePolicyState", 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows Defender",
                "PUAProtection", 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\CI\Config",
                "VulnerableDriverBlocklistEnable", 0, RegistryValueKind.DWord);
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity",
                "Enabled", 0, RegistryValueKind.DWord);

            // 6. Disable Defender scheduled tasks.
            log("[PHASE2] Disabling Defender scheduled tasks...");
            foreach (var task in DefenderScheduledTasks)
                RunProcess("schtasks.exe", $"/change /disable /TN \"{task}\"", log);
        }, log);

        log(ok ? "[PHASE2] Native phase-2 SYSTEM block completed."
               : "[PHASE2] Native phase-2 FAILED to acquire SYSTEM — see errors above.");
    }

    /// <summary>Synchronous process run for the headless/native phase-2 path; logs a non-zero exit code.</summary>
    private static void RunProcess(string file, string args, Action<string> log)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var proc = Process.Start(psi)!;
            proc.WaitForExit();
            if (proc.ExitCode != 0) log($"[PHASE2]   '{file} {args}' exit code {proc.ExitCode}");
        }
        catch (Exception ex) { log($"[PHASE2]   '{file} {args}' threw — {ex.Message}"); }
    }

    /// <summary>
    /// Extracts an embedded resource (matched by a suffix such as ".NoDefender.cab") to a
    /// file in the temp folder and returns its path. Self-contained — no PostInstall
    /// folder dependency, per explicit project-owner direction.
    /// </summary>
    private static async Task<string> ExtractEmbeddedAsync(string endsWith, string destFileName)
    {
        var asm = typeof(DefenderTweakHandler).Assembly;
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(endsWith, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"Embedded resource not found: {endsWith}");
        var dest = Path.Combine(Path.GetTempPath(), destFileName);
        await using var rs = asm.GetManifestResourceStream(name)!;
        await using var fs = File.Create(dest);
        await rs.CopyToAsync(fs);
        return dest;
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
}
