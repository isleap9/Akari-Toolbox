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
