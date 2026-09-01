using System.Text.RegularExpressions;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services.TweakHandlers;

/// <summary>
/// Gaming-category <see cref="ITweakHandler"/>s sourced from the "6 Windows" folder of
/// the user's "Ultimate" tweak collection (02-CONTEXT.md D-07). Every handler here
/// targets <c>CurrentControlSet</c>, never a hardcoded legacy control-set number, even
/// though every source .ps1 script in this file's scope hardcodes the latter
/// (RESEARCH.md Pitfall 1) — consistent with every existing Phase 1/Gaming handler's
/// convention.
/// </summary>
internal static class DeviceTreeEnumeration
{
    internal const string EnumRoot = @"SYSTEM\CurrentControlSet\Enum";

    /// <summary>
    /// Recursively walks every subkey path under <paramref name="rootPath"/> (root
    /// inclusive) via repeated <see cref="IRegistryService.GetSubKeyNames"/> calls —
    /// device instance paths nest several levels deep under each top-level class/bus,
    /// so a single-level enumeration is not enough (mirrors the source scripts' own
    /// <c>Get-ChildItem -Recurse</c>).
    /// </summary>
    internal static IEnumerable<string> WalkSubKeys(IRegistryService registry, string rootPath)
    {
        yield return rootPath;

        foreach (var child in registry.GetSubKeyNames(RegistryHive.LocalMachine, rootPath))
        {
            var childPath = $@"{rootPath}\{child}";
            foreach (var descendant in WalkSubKeys(registry, childPath))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Returns the full path of every child subkey literally named <paramref name="childName"/>
    /// found anywhere under <paramref name="rootPath"/> (at any depth) — the shared
    /// low-level walking primitive. Callers that need asymmetric On/Off semantics (see
    /// <see cref="WriteCacheFlushTweakHandler"/>) wrap this in two separately-named
    /// methods rather than branching on the match string at the call site, so the
    /// asymmetry documented in RESEARCH.md Pitfall 4 stays visible in the handler's own
    /// code shape, not hidden behind one generic parameterized helper.
    /// </summary>
    internal static IEnumerable<string> FindChildMatches(IRegistryService registry, string rootPath, string childName)
    {
        foreach (var parentPath in WalkSubKeys(registry, rootPath))
        {
            if (registry.GetSubKeyNames(RegistryHive.LocalMachine, parentPath).Contains(childName, StringComparer.Ordinal))
            {
                yield return $@"{parentPath}\{childName}";
            }
        }
    }
}

/// <summary>
/// Ported from <c>6 Windows/25 Device Manager Power Savings &amp; Wake.ps1</c>
/// (02-CONTEXT.md D-07). Recurses <c>ACPI</c>/<c>HID</c>/<c>PCI</c>/<c>USB</c> device
/// classes for <c>Device Parameters</c> and <c>WDF</c> matches.
/// </summary>
public sealed class DevicePowerSavingsTweakHandler(IRegistryService registry) : ITweakHandler
{
    private static readonly string[] DeviceClasses = ["ACPI", "HID", "PCI", "USB"];

    public string Key => "devpowersavings";

    public string Title => "Device Manager Power Savings";

    public string Description => "Disable power management/selective-suspend on ACPI, HID, PCI, and USB devices";

    public int Order => 105;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState()
    {
        var match = DeviceParametersMatches().FirstOrDefault();
        if (match.Path is null)
        {
            return false;
        }

        return registry.GetValue(RegistryHive.LocalMachine, match.Path, "EnhancedPowerManagementEnabled") is int v && v == 0;
    }

    public void SetState(bool enable)
    {
        foreach (var (path, deviceClass) in DeviceParametersMatches())
        {
            // ACPI branch preserves the source script's own SeleactiveSuspendEnabled
            // misspelling verbatim (RESEARCH.md Pitfall 5) — Windows never reads a value
            // by this misspelled name, so this specific write is a documented no-op for
            // ACPI devices only, matching the source script exactly rather than silently
            // "fixing" it into new, unintended real-world behavior (this plan's prohibitions).
            var selectiveSuspendValueName = deviceClass == "ACPI" ? "SeleactiveSuspendEnabled" : "SelectiveSuspendEnabled";

            if (enable)
            {
                registry.SetValue(RegistryHive.LocalMachine, path, "EnhancedPowerManagementEnabled", 0, RegistryValueKind.DWord);
                registry.SetValue(RegistryHive.LocalMachine, path, selectiveSuspendValueName, new byte[] { 0x00 }, RegistryValueKind.Binary);
                registry.SetValue(RegistryHive.LocalMachine, path, "SelectiveSuspendOn", 0, RegistryValueKind.DWord);
                registry.SetValue(RegistryHive.LocalMachine, path, "WaitWakeEnabled", 0, RegistryValueKind.DWord);
            }
            else
            {
                registry.DeleteValue(RegistryHive.LocalMachine, path, "EnhancedPowerManagementEnabled");
                registry.DeleteValue(RegistryHive.LocalMachine, path, selectiveSuspendValueName);
                registry.DeleteValue(RegistryHive.LocalMachine, path, "SelectiveSuspendOn");
                registry.DeleteValue(RegistryHive.LocalMachine, path, "WaitWakeEnabled");
            }
        }

        foreach (var path in WdfMatches())
        {
            if (enable)
            {
                registry.SetValue(RegistryHive.LocalMachine, path, "IdleInWorkingState", 0, RegistryValueKind.DWord);
            }
            else
            {
                registry.DeleteValue(RegistryHive.LocalMachine, path, "IdleInWorkingState");
            }
        }
    }

    private IEnumerable<(string Path, string DeviceClass)> DeviceParametersMatches()
    {
        foreach (var deviceClass in DeviceClasses)
        {
            var root = $@"{DeviceTreeEnumeration.EnumRoot}\{deviceClass}";
            foreach (var path in DeviceTreeEnumeration.FindChildMatches(registry, root, "Device Parameters"))
            {
                yield return (path, deviceClass);
            }
        }
    }

    private IEnumerable<string> WdfMatches()
    {
        foreach (var deviceClass in DeviceClasses)
        {
            var root = $@"{DeviceTreeEnumeration.EnumRoot}\{deviceClass}";
            foreach (var path in DeviceTreeEnumeration.FindChildMatches(registry, root, "WDF"))
            {
                yield return path;
            }
        }
    }
}

/// <summary>
/// Ported from <c>6 Windows/26 Network Adapter Power Savings &amp; Wake.ps1</c>
/// (02-CONTEXT.md D-07). Enumerates 4-digit adapter subkeys under the network-adapter
/// device-setup-class GUID, same <c>^\d{4}$</c> filter convention as
/// <c>GpuAdapterEnumeration</c> in <c>GamingGraphicsTweaks.cs</c>.
/// </summary>
internal static class NetworkAdapterEnumeration
{
    internal const string NetworkAdapterClassGuid = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

    internal static IEnumerable<string> GetAdapterSubKeys(IRegistryService registry) =>
        registry.GetSubKeyNames(RegistryHive.LocalMachine, NetworkAdapterClassGuid)
            .Where(name => Regex.IsMatch(name, @"^\d{4}$"));
}

public sealed class NetAdapterPowerSavingsTweakHandler(IRegistryService registry) : ITweakHandler
{
    // Exact value names verified directly against
    // "26 Network Adapter Power Savings & Wake.ps1" lines 26-61 (this plan's Flagged
    // Assumption instructs source verification over the plan's own paraphrased list).
    // Four names carry the NDIS-standardized "*" prefix (a legitimate registry value
    // name convention for power/wake OIDs, not a typo) and are preserved verbatim.
    private static readonly string[] RegSzValueNames =
    [
        "AdvancedEEE",
        "*EEE",
        "EEELinkAdvertisement",
        "SipsEnabled",
        "ULPMode",
        "GigaLite",
        "EnableGreenEthernet",
        "PowerSavingMode",
        "S5WakeOnLan",
        "*WakeOnMagicPacket",
        "*ModernStandbyWoLMagicPacket",
        "*WakeOnPattern",
        "WakeOnLink",
    ];

    public string Key => "netpowersavings";

    public string Title => "Network Adapter Power Savings";

    public string Description => "Disable energy-efficient Ethernet and wake-on-LAN features for lower latency";

    public int Order => 106;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState()
    {
        var adapter = NetworkAdapterEnumeration.GetAdapterSubKeys(registry).FirstOrDefault();
        if (adapter is null)
        {
            return false;
        }

        return registry.GetValue(
            RegistryHive.LocalMachine,
            $@"{NetworkAdapterEnumeration.NetworkAdapterClassGuid}\{adapter}",
            "PnPCapabilities") is int v && v == 24;
    }

    public void SetState(bool enable)
    {
        foreach (var adapter in NetworkAdapterEnumeration.GetAdapterSubKeys(registry))
        {
            var path = $@"{NetworkAdapterEnumeration.NetworkAdapterClassGuid}\{adapter}";

            if (enable)
            {
                registry.SetValue(RegistryHive.LocalMachine, path, "PnPCapabilities", 24, RegistryValueKind.DWord);
                foreach (var valueName in RegSzValueNames)
                {
                    registry.SetValue(RegistryHive.LocalMachine, path, valueName, "0", RegistryValueKind.String);
                }
            }
            else
            {
                registry.DeleteValue(RegistryHive.LocalMachine, path, "PnPCapabilities");
                foreach (var valueName in RegSzValueNames)
                {
                    registry.DeleteValue(RegistryHive.LocalMachine, path, valueName);
                }
            }
        }
    }
}

/// <summary>
/// Ported from <c>6 Windows/28 Write Cache Buffer Flushing.ps1</c> (02-CONTEXT.md D-07).
/// The On and Off paths are two genuinely separate, independently-targeted enumeration
/// methods (RESEARCH.md Pitfall 4) — On matches subkeys named exactly
/// <c>"Device Parameters"</c> and creates/writes a child <c>Disk</c> subkey there; Off
/// matches subkeys named exactly <c>"Disk"</c> directly and deletes them. Reusing one
/// enumeration helper parameterized by the match string across both directions would
/// silently no-op the asymmetric source behavior, so <see cref="SetStateOn"/> and
/// <see cref="SetStateOff"/> stay as distinct methods rather than one shared
/// implementation branching on a string/bool parameter.
/// </summary>
public sealed class WriteCacheFlushTweakHandler(IRegistryService registry) : ITweakHandler
{
    private static readonly string[] ScsiNvmeRoots =
    [
        $@"{DeviceTreeEnumeration.EnumRoot}\SCSI",
        $@"{DeviceTreeEnumeration.EnumRoot}\NVME",
    ];

    public string Key => "writecacheflush";

    public string Title => "Write Cache Buffer Flushing";

    public string Description => "Mark disk write caches as power-protected to reduce flush overhead";

    public int Order => 108;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState() =>
        DiskMatches().Any(diskPath => registry.GetValue(RegistryHive.LocalMachine, diskPath, "CacheIsPowerProtected") is int v && v == 1);

    public void SetState(bool enable)
    {
        if (enable)
        {
            SetStateOn();
        }
        else
        {
            SetStateOff();
        }
    }

    private void SetStateOn()
    {
        foreach (var deviceParamsPath in DeviceParametersMatches())
        {
            registry.SetValue(RegistryHive.LocalMachine, $@"{deviceParamsPath}\Disk", "CacheIsPowerProtected", 1, RegistryValueKind.DWord);
        }
    }

    private void SetStateOff()
    {
        foreach (var diskPath in DiskMatches())
        {
            registry.DeleteSubKeyTree(RegistryHive.LocalMachine, diskPath);
        }
    }

    private IEnumerable<string> DeviceParametersMatches() =>
        ScsiNvmeRoots.SelectMany(root => DeviceTreeEnumeration.FindChildMatches(registry, root, "Device Parameters"));

    private IEnumerable<string> DiskMatches() =>
        ScsiNvmeRoots.SelectMany(root => DeviceTreeEnumeration.FindChildMatches(registry, root, "Disk"));
}
