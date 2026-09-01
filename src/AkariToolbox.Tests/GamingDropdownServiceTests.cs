using AkariToolbox.App.Services;
using AkariToolbox.Framework.Services;
using Microsoft.Win32;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Covers <see cref="GamingDropdownService"/> — the D-09 SvcHost split threshold /
/// Win32 Priority Separation dropdown presets (02-05-PLAN.md, "research-proposed"
/// checkpoint option). Exercises against a hand-rolled fake (matching this
/// project's existing test-double style — see GamingGraphicsTweaksTests), never a
/// real registry.
/// </summary>
public class GamingDropdownServiceTests
{
    private const string SvcHostControlPath = @"SYSTEM\CurrentControlSet\Control";
    private const string SvcHostValueName = "SvcHostSplitThresholdInKB";
    private const string Win32PriorityControlPath = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
    private const string Win32PriorityValueName = "Win32PrioritySeparation";

    [Fact]
    public void SvcHostPresets_has_10_entries_Default_first_and_128GB_last()
    {
        var service = new GamingDropdownService(new FakeRegistryService());

        Assert.Equal(10, service.SvcHostPresets.Count);
        Assert.Equal("Default", service.SvcHostPresets[0].Label);
        Assert.Null(service.SvcHostPresets[0].ValueKb);
        Assert.Equal("128 GB", service.SvcHostPresets[^1].Label);
        Assert.Equal(134_217_728, service.SvcHostPresets[^1].ValueKb);
    }

    [Fact]
    public void Win32PriorityPresets_has_13_entries()
    {
        var service = new GamingDropdownService(new FakeRegistryService());

        Assert.Equal(13, service.Win32PriorityPresets.Count);
        Assert.Equal("Short, Fixed, High boost (2A)", service.Win32PriorityPresets[0].Label);
        Assert.Equal(0x2A, service.Win32PriorityPresets[0].ValueHex);
        Assert.Equal("Legacy/Advanced (06)", service.Win32PriorityPresets[^1].Label);
        Assert.Equal(0x06, service.Win32PriorityPresets[^1].ValueHex);
    }

    [Fact]
    public void GetSvcHostPresetIndex_returns_Default_index_when_value_absent()
    {
        var registry = new FakeRegistryService();
        var service = new GamingDropdownService(registry);

        Assert.Equal(0, service.GetSvcHostPresetIndex());
    }

    [Fact]
    public void GetSvcHostPresetIndex_returns_exact_match_index()
    {
        var registry = new FakeRegistryService();
        registry.Seed(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName, 8_388_608);
        var service = new GamingDropdownService(registry);

        Assert.Equal(2, service.GetSvcHostPresetIndex()); // "8 GB"
    }

    [Fact]
    public void GetSvcHostPresetIndex_ties_break_toward_lower_preset()
    {
        // Exactly between 4GB (4,194,304, index 1) and 8GB (8,388,608, index 2).
        var registry = new FakeRegistryService();
        registry.Seed(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName, 6_291_456);
        var service = new GamingDropdownService(registry);

        Assert.Equal(1, service.GetSvcHostPresetIndex()); // "4 GB" (lower), not "8 GB"
    }

    [Fact]
    public void GetSvcHostPresetIndex_selects_nearest_non_tied_preset()
    {
        var registry = new FakeRegistryService();
        registry.Seed(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName, 9_000_000);
        var service = new GamingDropdownService(registry);

        Assert.Equal(2, service.GetSvcHostPresetIndex()); // closer to "8 GB" (8,388,608) than "12 GB" (12,582,912)
    }

    [Fact]
    public void SetSvcHostPreset_Default_deletes_value_never_writes_a_literal()
    {
        var registry = new FakeRegistryService();
        var service = new GamingDropdownService(registry);

        service.SetSvcHostPreset(0);

        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName));
        Assert.Null(registry.GetValue(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName));
        Assert.Equal(0, registry.SetValueCallCount);
    }

    [Fact]
    public void SetSvcHostPreset_writes_the_presets_ValueKb()
    {
        var registry = new FakeRegistryService();
        var service = new GamingDropdownService(registry);

        service.SetSvcHostPreset(4); // "16 GB"

        Assert.Equal(16_777_216, registry.GetValue(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName));
        Assert.False(registry.WasDeleted(RegistryHive.LocalMachine, SvcHostControlPath, SvcHostValueName));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10)]
    public void SetSvcHostPreset_out_of_range_index_performs_zero_writes(int index)
    {
        var registry = new FakeRegistryService();
        var service = new GamingDropdownService(registry);

        service.SetSvcHostPreset(index);

        Assert.Equal(0, registry.SetValueCallCount);
        Assert.Equal(0, registry.DeleteValueCallCount);
    }

    [Fact]
    public void GetWin32PriorityPresetIndex_returns_exact_match_index()
    {
        var registry = new FakeRegistryService();
        registry.Seed(RegistryHive.LocalMachine, Win32PriorityControlPath, Win32PriorityValueName, 0x28);
        var service = new GamingDropdownService(registry);

        Assert.Equal(2, service.GetWin32PriorityPresetIndex()); // "Short, Fixed, No boost (28)"
    }

    [Fact]
    public void GetWin32PriorityPresetIndex_ties_break_toward_lower_preset()
    {
        // Exactly between 0x26=38 (index 3) and 0x28=40 (index 2); 39 isn't a preset value.
        var registry = new FakeRegistryService();
        registry.Seed(RegistryHive.LocalMachine, Win32PriorityControlPath, Win32PriorityValueName, 39);
        var service = new GamingDropdownService(registry);

        Assert.Equal(3, service.GetWin32PriorityPresetIndex()); // "Short, Variable, High boost (26)" (0x26=38), lower than 40
    }

    [Fact]
    public void GetWin32PriorityPresetIndex_treats_absent_value_as_zero_and_selects_nearest()
    {
        var registry = new FakeRegistryService();
        var service = new GamingDropdownService(registry);

        Assert.Equal(12, service.GetWin32PriorityPresetIndex()); // "Legacy/Advanced (06)" (0x06=6) is nearest to 0
    }

    [Fact]
    public void SetWin32PriorityPreset_writes_the_presets_ValueHex_never_deletes()
    {
        var registry = new FakeRegistryService();
        var service = new GamingDropdownService(registry);

        service.SetWin32PriorityPreset(0); // "Short, Fixed, High boost (2A)"

        Assert.Equal(0x2A, registry.GetValue(RegistryHive.LocalMachine, Win32PriorityControlPath, Win32PriorityValueName));
        Assert.Equal(0, registry.DeleteValueCallCount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(13)]
    public void SetWin32PriorityPreset_out_of_range_index_performs_zero_writes(int index)
    {
        var registry = new FakeRegistryService();
        var service = new GamingDropdownService(registry);

        service.SetWin32PriorityPreset(index);

        Assert.Equal(0, registry.SetValueCallCount);
        Assert.Equal(0, registry.DeleteValueCallCount);
    }

    private sealed class FakeRegistryService : IRegistryService
    {
        private readonly Dictionary<(RegistryHive Hive, string SubKeyPath, string ValueName), object?> _values = new();
        private readonly HashSet<(RegistryHive Hive, string SubKeyPath, string ValueName)> _deletedKeys = new();

        public int SetValueCallCount { get; private set; }

        public int DeleteValueCallCount { get; private set; }

        public void Seed(RegistryHive hive, string subKeyPath, string valueName, object? value) =>
            _values[(hive, subKeyPath, valueName)] = value;

        public bool WasDeleted(RegistryHive hive, string subKeyPath, string valueName) =>
            _deletedKeys.Contains((hive, subKeyPath, valueName));

        public object? GetValue(RegistryHive hive, string subKeyPath, string valueName) =>
            _values.TryGetValue((hive, subKeyPath, valueName), out var v) ? v : null;

        public void SetValue(RegistryHive hive, string subKeyPath, string valueName, object value, RegistryValueKind kind)
        {
            _values[(hive, subKeyPath, valueName)] = value;
            SetValueCallCount++;
        }

        public void DeleteValue(RegistryHive hive, string subKeyPath, string valueName)
        {
            _values.Remove((hive, subKeyPath, valueName));
            _deletedKeys.Add((hive, subKeyPath, valueName));
            DeleteValueCallCount++;
        }

        public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath) => [];

        public RegistryKey OpenRealUserHive(string subKeyPath) =>
            throw new NotSupportedException("Not needed for GamingDropdownService tests.");
    }
}
