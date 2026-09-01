using System.Text.RegularExpressions;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

/// <summary>
/// Gaming-category <see cref="ITweakHandler"/>s sourced from the "5 Graphics" folder of
/// the user's "Ultimate" tweak collection (02-CONTEXT.md D-04). Every handler here targets
/// <c>CurrentControlSet</c>, never <c>ControlSet001</c>, even where its source .ps1 script
/// hardcodes the latter (RESEARCH.md Pitfall 1) — consistent with every existing Phase 1
/// handler's convention.
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

    public bool GetState() => throw new NotImplementedException();

    public void SetState(bool enabled) => throw new NotImplementedException();
}

/// <summary>
/// Ported from <c>5 Graphics/9 Msi Mode.ps1</c> (02-CONTEXT.md D-04). No in-repo primitive
/// enumerates PnP devices (RESEARCH.md Pattern 4) — enumerates GPU <c>InstanceId</c>s via a
/// non-interactive <c>Get-PnpDevice -Class Display</c> process spawn through
/// <see cref="IScriptRunner"/> rather than adding a new dependency. Targets
/// <c>CurrentControlSet</c>, deviating from <c>9 Msi Mode.ps1:22-28</c>'s own hardcoded
/// <c>ControlSet001</c> per this plan's prohibition (RESEARCH.md Pitfall 1).
/// </summary>
public sealed class MsiModeTweakHandler(IRegistryService registry, IScriptRunner scriptRunner) : ITweakHandler
{
    public string Key => "gpumsimode";

    public string Title => "GPU MSI Mode";

    public string Description => "Enable Message-Signaled Interrupts on every detected GPU";

    public int Order => 102;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState() => throw new NotImplementedException();

    public void SetState(bool enabled) => throw new NotImplementedException();

    private async Task<IReadOnlyList<string>> GetGpuInstanceIdsAsync()
    {
        var output = await scriptRunner.RunProcessCaptureOutputAsync(
            "powershell.exe",
            "-NoProfile -Command \"(Get-PnpDevice -Class Display).InstanceId\"");

        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
