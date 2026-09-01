using Microsoft.Win32;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services;

/// <summary>
/// Reads/writes the two Gaming Tweaks dropdown presets directly against the
/// registry via <see cref="IRegistryService"/> — no revert/prior-capture
/// semantics, unlike <see cref="ITweakCatalog"/> (see
/// <see cref="IGamingDropdownService"/>'s doc comment for why this is not an
/// <see cref="ITweakHandler"/>). Preset lists are the D-09 checkpoint's approved
/// "research-proposed" expanded lists (02-05-PLAN.md).
/// </summary>
public sealed class GamingDropdownService(IRegistryService registry) : IGamingDropdownService
{
    private const string SvcHostControlPath = @"SYSTEM\CurrentControlSet\Control";
    private const string SvcHostValueName = "SvcHostSplitThresholdInKB";

    private const string Win32PriorityControlPath = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
    private const string Win32PriorityValueName = "Win32PrioritySeparation";

    public IReadOnlyList<(string Label, int? ValueKb)> SvcHostPresets { get; } =
    [
        ("Default", null),
        ("4 GB", 4_194_304),
        ("8 GB", 8_388_608),
        ("12 GB", 12_582_912),
        ("16 GB", 16_777_216),
        ("24 GB", 25_165_824),
        ("32 GB", 33_554_432),
        ("48 GB", 50_331_648),
        ("64 GB", 67_108_864),
        ("128 GB", 134_217_728),
    ];

    public IReadOnlyList<(string Label, int ValueHex)> Win32PriorityPresets { get; } =
    [
        ("Short, Fixed, High boost", 0x2A),
        ("Short, Fixed, Medium boost", 0x29),
        ("Short, Fixed, No boost", 0x28),
        ("Short, Variable, High boost", 0x26),
        ("Short, Variable, Medium boost", 0x25),
        ("Short, Variable, No boost", 0x24),
        ("Long, Fixed, High boost", 0x1A),
        ("Long, Fixed, Medium boost", 0x19),
        ("Long, Fixed, No boost", 0x18),
        ("Long, Variable, High boost", 0x16),
        ("Long, Variable, Medium boost", 0x15),
        ("Long, Variable, No boost", 0x14),
        ("Legacy/Advanced", 0x06),
    ];

    public int GetSvcHostPresetIndex()
    {
        var raw = registry.GetValue(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName);
        if (raw is not int liveValue)
        {
            return SvcHostPresets
                .Select((preset, index) => (preset.ValueKb, index))
                .First(x => x.ValueKb is null)
                .index;
        }

        var candidates = SvcHostPresets
            .Select((preset, index) => (preset.ValueKb, index))
            .Where(x => x.ValueKb.HasValue)
            .Select(x => (Value: x.ValueKb!.Value, x.index));

        return NearestPresetIndex(liveValue, candidates);
    }

    public void SetSvcHostPreset(int index)
    {
        var isValid = index >= 0 && index < SvcHostPresets.Count;
        if (!isValid)
        {
            return;
        }

        var preset = SvcHostPresets[index];
        if (preset.ValueKb is null)
        {
            registry.DeleteValue(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName);
        }
        else
        {
            registry.SetValue(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName, preset.ValueKb.Value, RegistryValueKind.DWord);
        }
    }

    public int GetWin32PriorityPresetIndex()
    {
        var raw = registry.GetValue(RegistryHive.LocalMachine, Win32PriorityControlPath, Win32PriorityValueName);
        var liveValue = raw is int v ? v : 0;

        var candidates = Win32PriorityPresets.Select((preset, index) => (Value: preset.ValueHex, index));

        return NearestPresetIndex(liveValue, candidates);
    }

    public void SetWin32PriorityPreset(int index)
    {
        var isValid = index >= 0 && index < Win32PriorityPresets.Count;
        if (!isValid)
        {
            return;
        }

        var preset = Win32PriorityPresets[index];
        registry.SetValue(RegistryHive.LocalMachine, Win32PriorityControlPath, Win32PriorityValueName, preset.ValueHex, RegistryValueKind.DWord);
    }

    /// <summary>
    /// Shared nearest-match implementation for both dropdowns: smallest absolute
    /// distance wins outright; an exact tie between two candidates breaks toward
    /// the lower value (this plan's explicit, deterministic tie-break contract).
    /// </summary>
    private static int NearestPresetIndex(int liveValue, IEnumerable<(int Value, int Index)> candidates) =>
        candidates
            .OrderBy(c => Math.Abs(liveValue - c.Value))
            .ThenBy(c => c.Value)
            .Select(c => c.Index)
            .First();
}
