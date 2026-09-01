using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

/// <summary>
/// 4 bcdedit/DISM-hybrid <see cref="ITweakHandler"/>s ported from the predecessor's
/// <c>TweakService.cs</c> (<c>SetDepNx</c>, <c>SetBootMenuPolicy</c>, <c>SetHyperV</c>,
/// <c>SetVr</c>). <see cref="DepTweakHandler"/> and <see cref="BootMenuTweakHandler"/>
/// have zero registry footprint (D-12) — their <see cref="ITweakHandler.GetState"/> parses
/// live <c>bcdedit /enum {current}</c> output via <see cref="IScriptRunner"/>.
/// <see cref="HyperVTweakHandler"/> and <see cref="VrTweakHandler"/> derive
/// <see cref="ITweakHandler.GetState"/> from their registry portion only — never re-spawning
/// bcdedit/DISM on a read (RESEARCH Pitfall 6, avoids a process-spawn-per-toggle-load
/// performance trap).
/// </summary>
public sealed class DepTweakHandler(IScriptRunner scriptRunner) : ITweakHandler
{
    public string Key => "dep";
    public string Title => "Disable DEP/NX";
    public string Description => "Toggle Data Execution Prevention";
    public int Order => 3;

    public bool GetState()
    {
        var output = scriptRunner.RunProcessCaptureOutputAsync("bcdedit", "/enum {current}").GetAwaiter().GetResult();
        var line = FindLine(output, "nx");
        return line is not null && line.Contains("AlwaysOff", StringComparison.OrdinalIgnoreCase);
    }

    public void SetState(bool disable) =>
        scriptRunner.RunProcessAsync("bcdedit", disable ? "/set NX AlwaysOff" : "/set NX OptIn").GetAwaiter().GetResult();

    internal static string? FindLine(string output, string fieldName)
    {
        foreach (var rawLine in output.Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.StartsWith(fieldName, StringComparison.OrdinalIgnoreCase))
            {
                return line;
            }
        }

        return null;
    }
}

public sealed class BootMenuTweakHandler(IScriptRunner scriptRunner) : ITweakHandler
{
    public string Key => "bootmenu";
    public string Title => "BootMenuPolicy Standard";
    public string Description => "Set Boot Menu Policy to Standard";
    public int Order => 6;

    public bool GetState()
    {
        var output = scriptRunner.RunProcessCaptureOutputAsync("bcdedit", "/enum {current}").GetAwaiter().GetResult();
        var line = DepTweakHandler.FindLine(output, "bootmenupolicy");
        return line is not null && line.Contains("Standard", StringComparison.OrdinalIgnoreCase);
    }

    public void SetState(bool standard) =>
        scriptRunner.RunProcessAsync("bcdedit.exe", standard ? "/set bootmenupolicy Standard" : "/set bootmenupolicy legacy").GetAwaiter().GetResult();
}

public sealed class HyperVTweakHandler(IScriptRunner scriptRunner, IRegistryService registry) : ITweakHandler
{
    private const string DeviceGuardPolicies = @"SOFTWARE\Policies\Microsoft\Windows\DeviceGuard";
    private const string DeviceGuardControl = @"SYSTEM\CurrentControlSet\Control\DeviceGuard";
    private const string HvciScenario = @"SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity";

    public string Key => "hyperv";
    public string Title => "Disable Hyper-V";
    public string Description => "Toggle Hyper-V On or Off";
    public int Order => 19;

    // Registry portion only (Pitfall 6) — both the disable and enable write paths
    // converge on this one key, so it is a reliable single-key representative read.
    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, HvciScenario, "Enabled") is int v && v == 0;

    public void SetState(bool disable)
    {
        if (disable)
        {
            scriptRunner.RunProcessAsync("bcdedit", "/set hypervisorlaunchtype off").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("bcdedit", "/set vm no").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("bcdedit", "/set vsmlaunchtype Off").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("bcdedit", "/set loadoptions DISABLE-LSA-ISO,DISABLE-VBS").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("DISM", "/Online /Disable-Feature:Microsoft-Hyper-V-All /Quiet /NoRestart").GetAwaiter().GetResult();

            // Predecessor asymmetry preserved intentionally: disable path writes under
            // SOFTWARE\Policies\..., enable path writes under SYSTEM\CurrentControlSet\Control\....
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardPolicies, "EnableVirtualizationBasedSecurity", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardPolicies, "RequirePlatformSecurityFeatures", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardPolicies, "HypervisorEnforcedCodeIntegrity", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardPolicies, "HVCIMATRequired", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardPolicies, "LsaCfgFlags", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardPolicies, "ConfigureSystemGuardLaunch", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardControl, "RequireMicrosoftSignedBootChain", 0, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, HvciScenario, "Enabled", 0, RegistryValueKind.DWord);
        }
        else
        {
            scriptRunner.RunProcessAsync("bcdedit", "/set hypervisorlaunchtype auto").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("bcdedit", "/deletevalue vm").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("bcdedit", "/deletevalue loadoptions").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("DISM", "/Online /Enable-Feature:Microsoft-Hyper-V-All /Quiet /NoRestart").GetAwaiter().GetResult();

            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardControl, "RequireMicrosoftSignedBootChain", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardControl, "EnableVirtualizationBasedSecurity", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, DeviceGuardControl, "RequirePlatformSecurityFeatures", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, HvciScenario, "Enabled", 1, RegistryValueKind.DWord);
        }
    }
}

public sealed class VrTweakHandler(IScriptRunner scriptRunner, IRegistryService registry) : ITweakHandler
{
    private const string ServicesKeyFormat = @"SYSTEM\ControlSet001\Services\{0}";

    private static readonly (string Svc, int EnableVal, int DisableVal)[] Services =
    [
        ("KSecPkg", 0, 4), ("LanmanWorkstation", 2, 4), ("mrxsmb", 3, 4),
        ("mrxsmb20", 3, 4), ("rdbss", 1, 4), ("srv2", 2, 4),
        ("QwaveDrv", 3, 4), ("Qwave", 3, 4), ("FontCache", 2, 4),
    ];

    public string Key => "vr";
    public string Title => "VR";
    public string Description => "Enable VR Services";
    public int Order => 16;

    // Registry portion only (Pitfall 6) — KSecPkg's enable-value (0) is distinct from
    // every other service's non-zero enable-value, making it the cleanest single-key
    // discriminator.
    public bool GetState() =>
        registry.GetValue(RegistryHive.LocalMachine, string.Format(ServicesKeyFormat, "KSecPkg"), "Start") is int v && v == 0;

    public void SetState(bool enable)
    {
        foreach (var (svc, enableVal, disableVal) in Services)
        {
            registry.SetValue(
                RegistryHive.LocalMachine,
                string.Format(ServicesKeyFormat, svc),
                "Start",
                enable ? enableVal : disableVal,
                RegistryValueKind.DWord);
        }

        scriptRunner.RunProcessAsync(
            "DISM",
            enable ? "/Online /Enable-Feature /FeatureName:SmbDirect /NoRestart" : "/Online /Disable-Feature /FeatureName:SmbDirect /NoRestart")
            .GetAwaiter().GetResult();
    }
}
