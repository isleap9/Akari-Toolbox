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

/// <summary>
/// Ported from <c>6 Windows/27 Network IPv4 Only.ps1</c> (02-CONTEXT.md D-07). No raw
/// registry access — shells to PowerShell's <c>NetAdapterBinding</c> cmdlets exclusively,
/// matching the source script's own approach exactly (it never touches the registry
/// directly for this tweak). The On and Off component-ID lists are deliberately
/// asymmetric: Off re-enables the same 8 IDs the On branch disables, plus a 9th
/// (<c>ms_tcpip</c>) that the On branch never touches — matching
/// <c>27 ...:27,53</c>'s own lists exactly, not a copy-paste oversight.
/// </summary>
public sealed class NetworkIpv4OnlyTweakHandler(IScriptRunner scriptRunner) : ITweakHandler
{
    private static readonly string[] BindingComponentIds =
    [
        "ms_lldp", "ms_lltdio", "ms_implat", "ms_rspndr", "ms_tcpip6", "ms_server", "ms_msclient", "ms_pacer",
    ];

    public string Key => "netipv4only";

    public string Title => "Force IPv4-Only Networking";

    public string Description => "Disable IPv6 and other non-essential network bindings for lower latency";

    public int Order => 107;

    public TweakCategory Category => TweakCategory.Gaming;

    public bool GetState()
    {
        var output = scriptRunner.RunProcessCaptureOutputAsync(
                "powershell.exe",
                "-NoProfile -Command \"(Get-NetAdapterBinding -Name '*' -ComponentID ms_tcpip6 | Select-Object -First 1).Enabled\"")
            .GetAwaiter().GetResult();

        // A disabled binding means the tweak is "on" — treat any unparseable/empty
        // output (e.g. no adapters present) as "off" rather than throwing.
        return string.Equals(output.Trim(), "False", StringComparison.OrdinalIgnoreCase);
    }

    public void SetState(bool enable)
    {
        var cmdlet = enable ? "Disable-NetAdapterBinding" : "Enable-NetAdapterBinding";
        var ids = enable ? BindingComponentIds : [.. BindingComponentIds, "ms_tcpip"];

        foreach (var id in ids)
        {
            scriptRunner.RunProcessAsync(
                    "powershell.exe",
                    $"-NoProfile -Command \"{cmdlet} -Name '*' -ComponentID {id}\"")
                .GetAwaiter().GetResult();
        }
    }
}

/// <summary>
/// Ported from <c>6 Windows/29 Power Plan.ps1</c> (02-CONTEXT.md D-07) — the phase's most
/// consequential handler. As authored, the source script's "revert" (Off branch) is
/// <c>powercfg -restoredefaultschemes</c>, which deletes every power scheme on the system
/// (including ones this app never touched) and restores only the 3 Windows-shipped
/// defaults. RESEARCH.md Assumption A4 flags this as unsatisfiable for GAMING-01's literal
/// "same real-state and revert behavior as Tweaks" requirement, so this handler is
/// deliberately hardened beyond the source script (this plan's Flagged Assumptions,
/// resolved not deferred): every pre-existing scheme is exported via
/// <c>powercfg -export</c> to a per-session temp folder BEFORE any <c>powercfg /delete</c>
/// call, and Off imports them back via <c>powercfg -import</c> rather than the destructive
/// <c>-restoredefaultschemes</c> call. The naive destructive fallback only runs — and is
/// logged as such — when no session-scoped export exists (e.g. the app restarted since
/// SetState(true) last ran; the export is session-scoped, matching this codebase's existing
/// session-scoped <c>_priorState</c> convention, not a permanent on-disk backup).
///
/// [Rule 3 - Blocking] This plan's Task 2 action text declares the constructor as
/// <c>(IScriptRunner, IRegistryService)</c>, but its own SetState(false) fallback text
/// requires logging via <see cref="ILogConsoleService"/> when no session export exists —
/// added here to make that possible, matching Task 3's constructor, which already
/// includes it.
/// </summary>
public sealed class PowerPlanTweakHandler(IScriptRunner scriptRunner, IRegistryService registry, ILogConsoleService log) : ITweakHandler
{
    private const string CustomSchemeGuid = "99999999-9999-9999-9999-999999999999";
    private const string UltimatePerformanceBaseGuid = "e9a42b02-d5df-448d-aa00-03f14749eb61";

    private const string PowerKey = @"SYSTEM\CurrentControlSet\Control\Power";
    private const string FlyoutMenuSettingsKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings";
    private const string SessionManagerPowerKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";
    private const string PowerThrottlingKey = @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling";

    // Every powercfg /setacvalueindex + /setdcvalueindex pair the On branch applies against
    // the custom scheme, ported verbatim from 29 Power Plan.ps1:65-227 (SubGroup GUID,
    // Setting GUID, Value string kept exactly as authored — the source itself mixes
    // "0x........" and bare zero-padded decimal forms).
    private static readonly (string SubGroup, string Setting, string Value)[] AcDcValueIndexPairs =
    [
        ("0012ee47-9041-4b5d-9b77-535fba8b1442", "6738e2c4-e8a5-4a42-b16a-e040e769756e", "0x00000000"), // hard disk turn off after
        ("0d7dbae2-4294-402a-ba8e-26777e8488cd", "309dce9b-bef4-4119-9921-a851fb12f0f4", "001"), // desktop background slideshow paused
        ("19cbb8fa-5279-450e-9fac-8a3d5fedd0c1", "12bbebe6-58d6-4636-95bb-3217ef867c1a", "000"), // wireless adapter power saving mode maximum performance
        ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "29f6c1db-86da-48c5-9fdb-f2b67b1f44da", "0x00000000"), // sleep after
        ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "94ac6d29-73ce-41a6-809f-6363ba21b47e", "000"), // allow hybrid sleep off
        ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "9d7815a6-7ee4-497e-8888-515a05f02364", "0x00000000"), // hibernate after
        ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d", "000"), // allow wake timers disable
        ("2a737441-1930-4402-8d77-b2bebba308a3", "0853a681-27c8-4100-a2fd-82013e970683", "0x00000000"), // hub selective suspend timeout 0
        ("2a737441-1930-4402-8d77-b2bebba308a3", "48e6b7a6-50f5-4782-a5d4-53bb8f07e226", "000"), // usb selective suspend setting disabled
        ("2a737441-1930-4402-8d77-b2bebba308a3", "d4e98f31-5ffe-4ce1-be31-1b38b384c009", "000"), // usb 3 link power management off
        ("4f971e89-eebd-4455-a8de-9e59040e7347", "a7066653-8d6c-40a8-910e-a1f54b84c7e5", "002"), // power buttons and lid: start menu power button shut down
        ("501a4d13-42af-4429-9fd1-a8218c268e20", "ee12f906-d277-404b-b6da-e5fa1a576df5", "000"), // pci express link state power management off
        ("54533251-82be-4824-96c1-47b60b740d00", "893dee8e-2bef-41e0-89c6-b55d0929964c", "0x00000064"), // minimum processor state 100%
        ("54533251-82be-4824-96c1-47b60b740d00", "94d3a615-a899-4ac5-ae2b-e4d8f634367f", "001"), // system cooling policy active
        ("54533251-82be-4824-96c1-47b60b740d00", "bc5038f7-23e0-4960-96da-33abaf5935ec", "0x00000064"), // maximum processor state 100%
        ("54533251-82be-4824-96c1-47b60b740d00", "0cc5b647-c1df-4637-891a-dec35c318583", "0x00000064"), // processor performance core parking min cores 100%
        ("54533251-82be-4824-96c1-47b60b740d00", "ea062031-0e34-4ff1-9b6d-eb1059334028", "0x00000064"), // processor performance core parking max cores 100%
        ("7516b95f-f776-4464-8c53-06167f40cc99", "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e", "600"), // turn off display after 10 min (oled protection)
        ("7516b95f-f776-4464-8c53-06167f40cc99", "aded5e82-b909-4619-9949-f5d71dac0bcb", "0x00000064"), // display brightness 100%
        ("7516b95f-f776-4464-8c53-06167f40cc99", "f1fbfde2-a960-4165-9f88-50667911ce96", "0x00000064"), // dimmed display brightness 100%
        ("7516b95f-f776-4464-8c53-06167f40cc99", "fbd9aa66-9553-4097-ba44-ed6e9d65eab8", "000"), // enable adaptive brightness off
        ("9596fb26-9850-41fd-ac3e-f7c3c00afd4b", "10778347-1370-4ee0-8bbd-33bdacaade49", "001"), // video playback quality bias: performance bias
        ("9596fb26-9850-41fd-ac3e-f7c3c00afd4b", "34c7b99f-9a6d-4b3c-8dc7-b6693b78cef4", "000"), // when playing video: optimize video quality off
        ("44f3beca-a7c0-460e-9df2-bb8b99e0cba6", "3619c3f2-afb2-4afc-b0e9-e7fef372de36", "002"), // intel(r) graphics power plan maximum performance
        ("c763b4ec-0e50-4b6b-9bed-2b92a6ee884e", "7ec1751b-60ed-4588-afb5-9819d3d77d90", "003"), // amd power slider overlay best performance
        ("f693fb01-e858-4f00-b20f-f30e12ac06d6", "191f65b5-d45c-4a4f-8aae-1ab8bfd980e6", "001"), // ati powerplay settings maximize performance
        ("e276e160-7cb0-43c6-b20b-73f5dce39954", "a1662ab2-9d34-4e53-ba8b-2639b9e20857", "003"), // switchable dynamic graphics global settings maximize performance
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "5dbb7c9f-38e9-40d2-9749-4f8a0e9f640f", "000"), // critical battery notification off
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "637ea02f-bbcb-4015-8e2c-a1c7b9c0b546", "000"), // critical battery action do nothing
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "8183ba9a-e910-48da-8769-14ae6dc1170a", "0x00000000"), // low battery level 0%
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "9a66d8d7-4ff7-4ef9-b5a2-5a326ca2a469", "0x00000000"), // critical battery level 0%
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "bcded951-187b-4d05-bccc-f7e51960c258", "000"), // low battery notification off
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "d8742dcb-3e6a-4b3c-b3fe-374623cdcf06", "000"), // low battery action do nothing
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "f3c5027d-cd16-4930-aa6b-90db844a8f00", "0x00000000"), // reserve battery level 0%
        ("de830923-a562-41af-a086-e3a2c6bad2da", "13d09884-f74e-474a-a852-b6bde8ad03a8", "0x00000064"), // low screen brightness when using battery saver disable
        ("de830923-a562-41af-a086-e3a2c6bad2da", "e69653ca-cf7f-4f05-aa73-cb833fa90ad4", "0x00000000"), // turn battery saver on automatically: never
    ];

    // The 4 PowerSettings "Attributes" visibility keys the source toggles hidden(1)/shown(0),
    // 29 Power Plan.ps1:95,106,134,142 (On, unhide) and :257,260,263,266 (Off, re-hide).
    private static readonly (string SubGroup, string Setting)[] AttributesVisibilityKeys =
    [
        ("2a737441-1930-4402-8d77-b2bebba308a3", "0853a681-27c8-4100-a2fd-82013e970683"), // hub selective suspend timeout
        ("2a737441-1930-4402-8d77-b2bebba308a3", "d4e98f31-5ffe-4ce1-be31-1b38b384c009"), // usb 3 link power management
        ("54533251-82be-4824-96c1-47b60b740d00", "0cc5b647-c1df-4637-891a-dec35c318583"), // processor performance core parking min cores
        ("54533251-82be-4824-96c1-47b60b740d00", "ea062031-0e34-4ff1-9b6d-eb1059334028"), // processor performance core parking max cores
    ];

    public string Key => "powerplan";

    public string Title => "Gaming Power Plan";

    public string Description => "Create and activate a custom high-performance power scheme, disabling hibernate/sleep/fast-boot/power-throttling";

    public int Order => 109;

    public TweakCategory Category => TweakCategory.Gaming;

    // "Is the app's own custom scheme currently active" — an approximation of "real prior
    // state", not a literal snapshot, matching how VpnTweakHandler/BluetoothTweakHandler
    // already document the same GetState caveat (Phase 1 STATE.md).
    public bool GetState()
    {
        var output = scriptRunner.RunProcessCaptureOutputAsync("powercfg", "/getactivescheme").GetAwaiter().GetResult();
        return output.Contains(CustomSchemeGuid, StringComparison.OrdinalIgnoreCase);
    }

    public void SetState(bool enable)
    {
        if (enable)
        {
            EnableInternal();
        }
        else
        {
            DisableInternal();
        }
    }

    private void EnableInternal()
    {
        var listOutput = scriptRunner.RunProcessCaptureOutputAsync("powercfg", "/L").GetAwaiter().GetResult();
        var existingSchemeGuids = ParseSchemeGuids(listOutput)
            .Where(guid => !string.Equals(guid, CustomSchemeGuid, StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Assumption A4 hardening — export every pre-existing scheme BEFORE any delete
        // call, never present in the source script.
        var exportDir = GetExportDir();
        Directory.CreateDirectory(exportDir);
        foreach (var guid in existingSchemeGuids)
        {
            var exportPath = Path.Combine(exportDir, $"{guid}.pow");
            scriptRunner.RunProcessAsync("powercfg", $"-export \"{exportPath}\" {guid}").GetAwaiter().GetResult();
        }

        scriptRunner.RunProcessAsync("powercfg", $"/duplicatescheme {UltimatePerformanceBaseGuid} {CustomSchemeGuid}").GetAwaiter().GetResult();
        scriptRunner.RunProcessAsync("powercfg", $"/setactive {CustomSchemeGuid}").GetAwaiter().GetResult();

        foreach (var guid in existingSchemeGuids)
        {
            scriptRunner.RunProcessAsync("powercfg", $"/delete {guid}").GetAwaiter().GetResult();
        }

        scriptRunner.RunProcessAsync("powercfg", "/hibernate off").GetAwaiter().GetResult();
        registry.SetValue(RegistryHive.LocalMachine, PowerKey, "HibernateEnabled", 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, PowerKey, "HibernateEnabledDefault", 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, FlyoutMenuSettingsKey, "ShowLockOption", 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, FlyoutMenuSettingsKey, "ShowSleepOption", 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, SessionManagerPowerKey, "HiberbootEnabled", 0, RegistryValueKind.DWord);
        registry.SetValue(RegistryHive.LocalMachine, PowerThrottlingKey, "PowerThrottlingOff", 1, RegistryValueKind.DWord);

        foreach (var (subGroup, setting) in AttributesVisibilityKeys)
        {
            registry.SetValue(RegistryHive.LocalMachine, PowerSettingsPath(subGroup, setting), "Attributes", 0, RegistryValueKind.DWord);
        }

        foreach (var (subGroup, setting, value) in AcDcValueIndexPairs)
        {
            scriptRunner.RunProcessAsync("powercfg", $"/setacvalueindex {CustomSchemeGuid} {subGroup} {setting} {value}").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("powercfg", $"/setdcvalueindex {CustomSchemeGuid} {subGroup} {setting} {value}").GetAwaiter().GetResult();
        }
    }

    private void DisableInternal()
    {
        var exportDir = GetExportDir();
        var exportedFiles = Directory.Exists(exportDir) ? Directory.GetFiles(exportDir, "*.pow") : [];

        if (exportedFiles.Length > 0)
        {
            foreach (var file in exportedFiles)
            {
                scriptRunner.RunProcessAsync("powercfg", $"-import \"{file}\"").GetAwaiter().GetResult();
            }

            // Re-activate a restored scheme before deleting the app's own custom scheme —
            // powercfg refuses to delete the currently active scheme.
            var restoredGuid = Path.GetFileNameWithoutExtension(exportedFiles[0]);
            scriptRunner.RunProcessAsync("powercfg", $"/setactive {restoredGuid}").GetAwaiter().GetResult();
            scriptRunner.RunProcessAsync("powercfg", $"/delete {CustomSchemeGuid}").GetAwaiter().GetResult();

            try { Directory.Delete(exportDir, recursive: true); } catch { /* best-effort cleanup */ }
        }
        else
        {
            log.Log("[POWER-PLAN] No session-scoped power-scheme backup found — falling back to powercfg -restoredefaultschemes (destructive: restores only the 3 Windows-shipped default schemes; any other pre-existing custom scheme is unrecoverable).");
            scriptRunner.RunProcessAsync("powercfg", "-restoredefaultschemes").GetAwaiter().GetResult();
        }

        scriptRunner.RunProcessAsync("powercfg", "/hibernate on").GetAwaiter().GetResult();
        registry.DeleteValue(RegistryHive.LocalMachine, PowerKey, "HibernateEnabled");
        registry.SetValue(RegistryHive.LocalMachine, PowerKey, "HibernateEnabledDefault", 1, RegistryValueKind.DWord);
        registry.DeleteSubKeyTree(RegistryHive.LocalMachine, FlyoutMenuSettingsKey);
        registry.SetValue(RegistryHive.LocalMachine, SessionManagerPowerKey, "HiberbootEnabled", 1, RegistryValueKind.DWord);
        registry.DeleteSubKeyTree(RegistryHive.LocalMachine, PowerThrottlingKey);

        foreach (var (subGroup, setting) in AttributesVisibilityKeys)
        {
            registry.SetValue(RegistryHive.LocalMachine, PowerSettingsPath(subGroup, setting), "Attributes", 1, RegistryValueKind.DWord);
        }
    }

    private static string PowerSettingsPath(string subGroup, string setting) =>
        $@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\{subGroup}\{setting}";

    // Per-session (not permanent) export folder — matches this codebase's existing
    // session-scoped _priorState convention (RESEARCH.md Assumption A4).
    private static string GetExportDir() =>
        Path.Combine(Path.GetTempPath(), "AkariToolbox-PowerPlanBackup");

    private static IReadOnlyList<string> ParseSchemeGuids(string powercfgListOutput) =>
        Regex.Matches(powercfgListOutput, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")
            .Select(m => m.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
