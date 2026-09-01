using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

/// <summary>
/// Gaming-category <see cref="ITweakHandler"/>s sourced from the "5 Graphics" folder of
/// the user's "Ultimate" tweak collection (02-CONTEXT.md D-04). Every handler here targets
/// <c>CurrentControlSet</c>, never a hardcoded legacy control-set number, even where its
/// source .ps1 script hardcodes the latter (RESEARCH.md Pitfall 1) — consistent with every
/// existing Phase 1 handler's convention.
/// </summary>
internal static class GpuAdapterEnumeration
{
    internal const string GpuDisplayClassGuid = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    /// <summary>
    /// Returns every 4-digit-named GPU adapter subkey under the Display device-setup-class
    /// GUID — the RESEARCH-recommended standardized filter (<c>^\d{4}$</c>), used in place
    /// of each source script's own inconsistent heuristic so every GPU-adapter-enumerating
    /// handler (Hdcp, P0State, and later Amd/Intel Settings) shares one filter
    /// (RESEARCH.md Don't Hand-Roll table).
    /// </summary>
    internal static IEnumerable<string> GetGpuAdapterSubKeys(IRegistryService registry) =>
        registry.GetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid)
            .Where(name => Regex.IsMatch(name, @"^\d{4}$"));
}

/// <summary>
/// Ported from <c>5 Graphics/7 Hdcp.ps1</c> (02-CONTEXT.md D-04) — first Gaming-category
/// handler, proves the vertical slice. The source script's "Off (Recommended)" branch
/// writes <c>RMHdcpKeyglobZero</c>=1 (forcing HDCP off); that maps 1:1 to
/// <see cref="SetState"/>'s <c>enabled</c> parameter (RESEARCH.md Pattern 2).
/// </summary>
public sealed class HdcpTweakHandler(IRegistryService registry) : ITweakHandler
{
    public string Key => "gpuhdcp";

    public string Title => "GPU HDCP Override";

    public string Description => "Force-disable HDCP on every detected GPU adapter";

    public int Order => 100;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState()
    {
        var adapters = GpuAdapterEnumeration.GetGpuAdapterSubKeys(registry).ToList();
        if (adapters.Count == 0)
        {
            return false;
        }

        return adapters.All(adapter =>
            registry.GetValue(
                RegistryHive.LocalMachine,
                $@"{GpuAdapterEnumeration.GpuDisplayClassGuid}\{adapter}",
                "RMHdcpKeyglobZero") is int v && v == 1);
    }

    public void SetState(bool enabled)
    {
        foreach (var adapter in GpuAdapterEnumeration.GetGpuAdapterSubKeys(registry))
        {
            registry.SetValue(
                RegistryHive.LocalMachine,
                $@"{GpuAdapterEnumeration.GpuDisplayClassGuid}\{adapter}",
                "RMHdcpKeyglobZero",
                enabled ? 1 : 0,
                RegistryValueKind.DWord);
        }
    }
}

/// <summary>
/// Ported from <c>5 Graphics/8 P0 State.ps1</c> (02-CONTEXT.md D-04). Same shape as
/// <see cref="HdcpTweakHandler"/>, targeting <c>DisableDynamicPstate</c> instead of
/// <c>RMHdcpKeyglobZero</c> — an explicit write, not a delete, on both branches
/// (<c>8 P0 State.ps1:26-33,56-63</c>).
/// </summary>
public sealed class P0StateTweakHandler(IRegistryService registry) : ITweakHandler
{
    public string Key => "gpup0state";

    public string Title => "GPU P0 State";

    public string Description => "Force GPUs to stay at maximum performance state";

    public int Order => 101;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState()
    {
        var adapters = GpuAdapterEnumeration.GetGpuAdapterSubKeys(registry).ToList();
        if (adapters.Count == 0)
        {
            return false;
        }

        return adapters.All(adapter =>
            registry.GetValue(
                RegistryHive.LocalMachine,
                $@"{GpuAdapterEnumeration.GpuDisplayClassGuid}\{adapter}",
                "DisableDynamicPstate") is int v && v == 1);
    }

    public void SetState(bool enabled)
    {
        foreach (var adapter in GpuAdapterEnumeration.GetGpuAdapterSubKeys(registry))
        {
            registry.SetValue(
                RegistryHive.LocalMachine,
                $@"{GpuAdapterEnumeration.GpuDisplayClassGuid}\{adapter}",
                "DisableDynamicPstate",
                enabled ? 1 : 0,
                RegistryValueKind.DWord);
        }
    }
}

/// <summary>
/// Ported from <c>5 Graphics/9 Msi Mode.ps1</c> (02-CONTEXT.md D-04). No in-repo primitive
/// enumerates PnP devices (RESEARCH.md Pattern 4) — enumerates GPU <c>InstanceId</c>s via a
/// non-interactive <c>Get-PnpDevice -Class Display</c> process spawn through
/// <see cref="IScriptRunner"/> rather than adding a new dependency. Targets
/// <c>CurrentControlSet</c>, deviating from <c>9 Msi Mode.ps1:22-28</c>'s own hardcoded
/// legacy control-set number per this plan's prohibition (RESEARCH.md Pitfall 1).
/// </summary>
public sealed class MsiModeTweakHandler(IRegistryService registry, IScriptRunner scriptRunner) : ITweakHandler
{
    public string Key => "gpumsimode";

    public string Title => "GPU MSI Mode";

    public string Description => "Enable Message-Signaled Interrupts on every detected GPU";

    public int Order => 102;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState()
    {
        var instanceIds = GetGpuInstanceIdsAsync().GetAwaiter().GetResult();
        if (instanceIds.Count == 0)
        {
            return false;
        }

        return instanceIds.All(instanceId =>
            registry.GetValue(RegistryHive.LocalMachine, BuildMsiPath(instanceId), "MSISupported") is int v && v == 1);
    }

    public void SetState(bool enabled)
    {
        var instanceIds = GetGpuInstanceIdsAsync().GetAwaiter().GetResult();
        foreach (var instanceId in instanceIds)
        {
            registry.SetValue(
                RegistryHive.LocalMachine,
                BuildMsiPath(instanceId),
                "MSISupported",
                enabled ? 1 : 0,
                RegistryValueKind.DWord);
        }
    }

    // CurrentControlSet, deviating from 9 Msi Mode.ps1:22-28's own hardcoded legacy
    // control-set number per this plan's prohibition (RESEARCH.md Pitfall 1).
    private static string BuildMsiPath(string instanceId) =>
        $@"SYSTEM\CurrentControlSet\Enum\{instanceId}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";

    private async Task<IReadOnlyList<string>> GetGpuInstanceIdsAsync()
    {
        var output = await scriptRunner.RunProcessCaptureOutputAsync(
            "powershell.exe",
            "-NoProfile -Command \"(Get-PnpDevice -Class Display).InstanceId\"");

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}

/// <summary>
/// Converts a hex string (pairs of hex characters, as authored in <c>reg.exe /t
/// REG_BINARY /d "..."</c> syntax) into a <see cref="byte"/> array — matches how
/// <c>reg.exe</c> itself parses its data argument. Shared by every REG_BINARY value in
/// <see cref="AmdSettingsTweakHandler"/>'s value table so the conversion is written once,
/// not scattered as manual byte-array literals (RESEARCH.md Don't Hand-Roll table).
/// </summary>
internal static class RegistryBinaryHelpers
{
    internal static byte[] HexStringToBytes(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }
}

/// <summary>
/// Ported from <c>5 Graphics/5 Amd Settings.ps1</c> (02-CONTEXT.md D-04) — the largest and
/// most complex Gaming toggle: 10 fixed <c>HKCU\Software\AMD\{CN,AIM,DVR}</c> values plus
/// per-adapter registry paths under the shared GPU Display class GUID
/// (<see cref="GpuAdapterEnumeration"/>). Targets <c>CurrentControlSet</c> throughout,
/// deviating from the source script's own hardcoded legacy control-set number
/// (RESEARCH.md Pitfall 1).
/// </summary>
public sealed class AmdSettingsTweakHandler(IRegistryService registry) : ITweakHandler
{
    private const string Cn = @"Software\AMD\CN";
    private const string Aim = @"Software\AMD\AIM";
    private const string Dvr = @"Software\AMD\DVR";

    public string Key => "gpuamdsettings";

    public string Title => "AMD Software Settings";

    public string Description => "Apply recommended AMD Radeon Software tweaks (disables telemetry/overlays, sets performance registry keys)";

    public int Order => 103;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState() =>
        registry.GetValue(RegistryHive.CurrentUser, Cn, "AutoUpdate") is int v && v == 0;

    public void SetState(bool enabled)
    {
        if (enabled)
        {
            ApplyOn();
        }
        else
        {
            ApplyOff();
        }
    }

    private void ApplyOn()
    {
        registry.SetValue(RegistryHive.CurrentUser, Cn, "AutoUpdate", 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.CurrentUser, Aim, "LaunchBugTool", 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.CurrentUser, Dvr, "HotkeysDisabled", 1, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.CurrentUser, Cn, "SystemTray", "false", RegistryValueKind.String);
        registry.SetValue(RegistryHive.CurrentUser, Dvr, "ShowRSOverlay", "false", RegistryValueKind.String);
        registry.SetValue(RegistryHive.CurrentUser, Cn, "RSXBrowserUnavailable", "true", RegistryValueKind.String);
        registry.SetValue(RegistryHive.CurrentUser, Cn, "AllowWebContent", "false", RegistryValueKind.String);
        registry.SetValue(RegistryHive.CurrentUser, Cn, "CN_Hide_Toast_Notification", "true", RegistryValueKind.String);
        registry.SetValue(RegistryHive.CurrentUser, Cn, "AnimationEffect", "false", RegistryValueKind.String);
        registry.SetValue(RegistryHive.CurrentUser, Cn, "WizardProfile", "PROFILE_CUSTOM", RegistryValueKind.String);

        foreach (var adapter in GpuAdapterEnumeration.GetGpuAdapterSubKeys(registry))
        {
            var adapterPath = $@"{GpuAdapterEnumeration.GpuDisplayClassGuid}\{adapter}";
            var umdPath = $@"{adapterPath}\UMD";

            registry.SetValue(RegistryHive.LocalMachine, umdPath, "VSyncControl", RegistryBinaryHelpers.HexStringToBytes("3000"), RegistryValueKind.Binary);
            registry.SetValue(RegistryHive.LocalMachine, umdPath, "TFQ", RegistryBinaryHelpers.HexStringToBytes("3200"), RegistryValueKind.Binary);
            registry.SetValue(RegistryHive.LocalMachine, umdPath, "Tessellation", RegistryBinaryHelpers.HexStringToBytes("3100"), RegistryValueKind.Binary);
            registry.SetValue(RegistryHive.LocalMachine, umdPath, "Tessellation_OPTION", RegistryBinaryHelpers.HexStringToBytes("3200"), RegistryValueKind.Binary);

            registry.SetValue(RegistryHive.LocalMachine, $@"{adapterPath}\power_v1", "abmlevel", RegistryBinaryHelpers.HexStringToBytes("00000000"), RegistryValueKind.Binary);
            registry.SetValue(RegistryHive.LocalMachine, adapterPath, "IsAutoDefault", RegistryBinaryHelpers.HexStringToBytes("00000000"), RegistryValueKind.Binary);
            registry.SetValue(RegistryHive.LocalMachine, adapterPath, "IsComponentControl", RegistryBinaryHelpers.HexStringToBytes("0f000000"), RegistryValueKind.Binary);
        }

        registry.SetValue(RegistryHive.CurrentUser, $@"{Cn}\CustomResolutions", "EulaAccepted", "true", RegistryValueKind.String);
        registry.SetValue(RegistryHive.CurrentUser, $@"{Cn}\DisplayOverride", "EulaAccepted", "true", RegistryValueKind.String);

        registry.DeleteSubKeyTree(RegistryHive.CurrentUser, $@"{Cn}\Notification");
        registry.CreateSubKey(RegistryHive.CurrentUser, $@"{Cn}\Notification");

        registry.SetValue(RegistryHive.CurrentUser, $@"{Cn}\FreeSync", "AlreadyNotified", 1, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.CurrentUser, $@"{Cn}\OverlayNotification", "AlreadyNotified", 1, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.CurrentUser, $@"{Cn}\VirtualSuperResolution", "AlreadyNotified", 1, RegistryValueKind.DWord);

        TryRestartRadeonSoftware();
    }

    private void ApplyOff()
    {
        registry.DeleteValue(RegistryHive.CurrentUser, Cn, "AutoUpdate");
        registry.SetValue(RegistryHive.CurrentUser, Aim, "LaunchBugTool", 1, RegistryValueKind.DWord);
        registry.DeleteValue(RegistryHive.CurrentUser, Dvr, "HotkeysDisabled");
        registry.DeleteValue(RegistryHive.CurrentUser, Cn, "SystemTray");
        registry.DeleteValue(RegistryHive.CurrentUser, Dvr, "ShowRSOverlay");
        registry.DeleteValue(RegistryHive.CurrentUser, Cn, "RSXBrowserUnavailable");
        registry.DeleteValue(RegistryHive.CurrentUser, Cn, "AllowWebContent");
        registry.DeleteValue(RegistryHive.CurrentUser, Cn, "CN_Hide_Toast_Notification");
        registry.DeleteValue(RegistryHive.CurrentUser, Cn, "AnimationEffect");
        registry.DeleteValue(RegistryHive.CurrentUser, Cn, "WizardProfile");

        foreach (var adapter in GpuAdapterEnumeration.GetGpuAdapterSubKeys(registry))
        {
            var adapterPath = $@"{GpuAdapterEnumeration.GpuDisplayClassGuid}\{adapter}";
            var umdPath = $@"{adapterPath}\UMD";

            registry.SetValue(RegistryHive.LocalMachine, umdPath, "VSyncControl", RegistryBinaryHelpers.HexStringToBytes("31000000"), RegistryValueKind.Binary);
            registry.DeleteValue(RegistryHive.LocalMachine, umdPath, "TFQ");
            registry.SetValue(RegistryHive.LocalMachine, umdPath, "Tessellation", RegistryBinaryHelpers.HexStringToBytes("360034000000"), RegistryValueKind.Binary);
            registry.SetValue(RegistryHive.LocalMachine, umdPath, "Tessellation_OPTION", RegistryBinaryHelpers.HexStringToBytes("30000000"), RegistryValueKind.Binary);

            registry.DeleteValue(RegistryHive.LocalMachine, $@"{adapterPath}\power_v1", "abmlevel");
            registry.SetValue(RegistryHive.LocalMachine, adapterPath, "IsAutoDefault", 1, RegistryValueKind.DWord);
            registry.SetValue(RegistryHive.LocalMachine, adapterPath, "IsComponentControl", RegistryBinaryHelpers.HexStringToBytes("00000000"), RegistryValueKind.Binary);
        }

        registry.DeleteSubKeyTree(RegistryHive.CurrentUser, $@"{Cn}\CustomResolutions");
        registry.DeleteSubKeyTree(RegistryHive.CurrentUser, $@"{Cn}\DisplayOverride");
        registry.DeleteSubKeyTree(RegistryHive.CurrentUser, $@"{Cn}\Notification");
        registry.DeleteSubKeyTree(RegistryHive.CurrentUser, $@"{Cn}\FreeSync");
        registry.DeleteSubKeyTree(RegistryHive.CurrentUser, $@"{Cn}\OverlayNotification");
        registry.DeleteSubKeyTree(RegistryHive.CurrentUser, $@"{Cn}\VirtualSuperResolution");
    }

    // Best-effort, non-blocking (5 Amd Settings.ps1:22-24): restart RadeonSoftware.exe so
    // settings stick. Never throws — AMD Radeon Software isn't guaranteed to be installed
    // or running on this machine.
    private static void TryRestartRadeonSoftware()
    {
        try
        {
            foreach (var process in Process.GetProcessesByName("RadeonSoftware"))
            {
                using (process)
                {
                    process.Kill();
                }
            }
        }
        catch
        {
            // Best-effort only — must never fail SetState.
        }
    }
}
