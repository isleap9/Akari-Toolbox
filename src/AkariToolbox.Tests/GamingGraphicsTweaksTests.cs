using AkariToolbox.App.Services;
using AkariToolbox.App.Services.TweakHandlers;
using AkariToolbox.Framework.Services;
using Microsoft.Win32;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Covers <see cref="P0StateTweakHandler"/> and <see cref="MsiModeTweakHandler"/> —
/// ported from <c>5 Graphics/8 P0 State.ps1</c> and <c>5 Graphics/9 Msi Mode.ps1</c>
/// (02-CONTEXT.md D-04). Exercises both against hand-rolled fakes (matching this
/// project's existing test-double style — no Moq call sites exist elsewhere in this
/// suite) rather than real registry/process calls.
/// </summary>
public class GamingGraphicsTweaksTests
{
    private const string GpuDisplayClassGuid = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    [Fact]
    public void P0State_GetState_returns_true_only_when_every_adapter_has_DisableDynamicPstate_1()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid, "1234", "5678");
        registry.Seed(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\1234", "DisableDynamicPstate", 1);
        registry.Seed(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\5678", "DisableDynamicPstate", 1);
        var handler = new P0StateTweakHandler(registry);

        Assert.True(handler.GetState());
    }

    [Fact]
    public void P0State_GetState_returns_false_when_one_adapter_is_off()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid, "1234", "5678");
        registry.Seed(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\1234", "DisableDynamicPstate", 1);
        registry.Seed(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\5678", "DisableDynamicPstate", 0);
        var handler = new P0StateTweakHandler(registry);

        Assert.False(handler.GetState());
    }

    [Fact]
    public void P0State_GetState_returns_false_when_one_adapter_value_is_absent()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid, "1234", "5678");
        registry.Seed(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\1234", "DisableDynamicPstate", 1);
        // "5678" intentionally never seeded — value is absent.
        var handler = new P0StateTweakHandler(registry);

        Assert.False(handler.GetState());
    }

    [Fact]
    public void P0State_SetState_true_writes_1_to_every_adapter()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid, "1234", "5678");
        var handler = new P0StateTweakHandler(registry);

        handler.SetState(true);

        Assert.Equal(1, registry.GetValue(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\1234", "DisableDynamicPstate"));
        Assert.Equal(1, registry.GetValue(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\5678", "DisableDynamicPstate"));
    }

    [Fact]
    public void P0State_SetState_false_writes_0_not_delete()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid, "1234");
        var handler = new P0StateTweakHandler(registry);

        handler.SetState(false);

        Assert.Equal(0, registry.GetValue(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\1234", "DisableDynamicPstate"));
        Assert.False(registry.WasDeleted(RegistryHive.LocalMachine, $@"{GpuDisplayClassGuid}\1234", "DisableDynamicPstate"));
    }

    [Fact]
    public void P0State_metadata_is_Order_101_Category_Gaming()
    {
        var handler = new P0StateTweakHandler(new FakeRegistryService());

        Assert.Equal(101, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
    }

    [Fact]
    public void MsiMode_SetState_true_writes_MSISupported_1_under_CurrentControlSet_for_each_instance_id()
    {
        var registry = new FakeRegistryService();
        var scriptRunner = new FakeScriptRunner("GPU\\INSTANCE\\ID1\nGPU\\INSTANCE\\ID2\n");
        var handler = new MsiModeTweakHandler(registry, scriptRunner);

        handler.SetState(true);

        const string path1 = @"SYSTEM\CurrentControlSet\Enum\GPU\INSTANCE\ID1\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
        const string path2 = @"SYSTEM\CurrentControlSet\Enum\GPU\INSTANCE\ID2\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";

        Assert.Equal(1, registry.GetValue(RegistryHive.LocalMachine, path1, "MSISupported"));
        Assert.Equal(1, registry.GetValue(RegistryHive.LocalMachine, path2, "MSISupported"));
        Assert.DoesNotContain("ControlSet001", path1, StringComparison.Ordinal);
        Assert.Contains("CurrentControlSet", path1, StringComparison.Ordinal);
    }

    [Fact]
    public void MsiMode_SetState_false_writes_MSISupported_0()
    {
        var registry = new FakeRegistryService();
        var scriptRunner = new FakeScriptRunner("GPU\\INSTANCE\\ID1\n");
        var handler = new MsiModeTweakHandler(registry, scriptRunner);

        handler.SetState(false);

        const string path1 = @"SYSTEM\CurrentControlSet\Enum\GPU\INSTANCE\ID1\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
        Assert.Equal(0, registry.GetValue(RegistryHive.LocalMachine, path1, "MSISupported"));
    }

    [Fact]
    public void MsiMode_metadata_is_Order_102_Category_Gaming()
    {
        var handler = new MsiModeTweakHandler(new FakeRegistryService(), new FakeScriptRunner(string.Empty));

        Assert.Equal(102, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
    }

    [Fact]
    public void HexStringToBytes_converts_hex_pairs_to_bytes()
    {
        Assert.Equal(new byte[] { 0x30, 0x00 }, RegistryBinaryHelpers.HexStringToBytes("3000"));
    }

    [Fact]
    public void AmdSettings_SetState_true_writes_all_10_fixed_HKCU_values()
    {
        var registry = new FakeRegistryService();
        var handler = new AmdSettingsTweakHandler(registry);

        handler.SetState(true);

        Assert.Equal((0, RegistryValueKind.DWord), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\CN", "AutoUpdate"));
        Assert.Equal((0, RegistryValueKind.DWord), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\AIM", "LaunchBugTool"));
        Assert.Equal((1, RegistryValueKind.DWord), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\DVR", "HotkeysDisabled"));
        Assert.Equal(("false", RegistryValueKind.String), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\CN", "SystemTray"));
        Assert.Equal(("false", RegistryValueKind.String), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\DVR", "ShowRSOverlay"));
        Assert.Equal(("true", RegistryValueKind.String), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\CN", "RSXBrowserUnavailable"));
        Assert.Equal(("false", RegistryValueKind.String), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\CN", "AllowWebContent"));
        Assert.Equal(("true", RegistryValueKind.String), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\CN", "CN_Hide_Toast_Notification"));
        Assert.Equal(("false", RegistryValueKind.String), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\CN", "AnimationEffect"));
        Assert.Equal(("PROFILE_CUSTOM", RegistryValueKind.String), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\CN", "WizardProfile"));
    }

    [Fact]
    public void AmdSettings_SetState_false_reverses_fixed_values_per_documented_off_behavior()
    {
        var registry = new FakeRegistryService();
        var handler = new AmdSettingsTweakHandler(registry);

        handler.SetState(false);

        // Values documented as "deleted" on Off: DeleteValue called, no lingering SetValue.
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN", "AutoUpdate"));
        Assert.Null(registry.GetValue(RegistryHive.CurrentUser, @"Software\AMD\CN", "AutoUpdate"));
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\DVR", "HotkeysDisabled"));
        Assert.Null(registry.GetValue(RegistryHive.CurrentUser, @"Software\AMD\DVR", "HotkeysDisabled"));
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN", "SystemTray"));
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\DVR", "ShowRSOverlay"));
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN", "RSXBrowserUnavailable"));
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN", "AllowWebContent"));
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN", "CN_Hide_Toast_Notification"));
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN", "AnimationEffect"));
        Assert.True(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN", "WizardProfile"));

        // The one row documented as rewritten (not deleted) on Off.
        Assert.Equal((1, RegistryValueKind.DWord), registry.GetValueWithKind(RegistryHive.CurrentUser, @"Software\AMD\AIM", "LaunchBugTool"));
        Assert.False(registry.WasDeleted(RegistryHive.CurrentUser, @"Software\AMD\AIM", "LaunchBugTool"));
    }

    [Fact]
    public void AmdSettings_SetState_true_writes_per_adapter_UMD_values_via_HexStringToBytes()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid, "1234");
        var handler = new AmdSettingsTweakHandler(registry);

        handler.SetState(true);

        var umdPath = $@"{GpuDisplayClassGuid}\1234\UMD";
        Assert.Equal(RegistryBinaryHelpers.HexStringToBytes("3000"), registry.GetValue(RegistryHive.LocalMachine, umdPath, "VSyncControl"));
        Assert.Equal(RegistryBinaryHelpers.HexStringToBytes("3200"), registry.GetValue(RegistryHive.LocalMachine, umdPath, "TFQ"));
        Assert.Equal(RegistryBinaryHelpers.HexStringToBytes("3100"), registry.GetValue(RegistryHive.LocalMachine, umdPath, "Tessellation"));
        Assert.Equal(RegistryBinaryHelpers.HexStringToBytes("3200"), registry.GetValue(RegistryHive.LocalMachine, umdPath, "Tessellation_OPTION"));
    }

    [Fact]
    public void AmdSettings_SetState_false_writes_per_adapter_UMD_off_values_and_deletes_TFQ()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, GpuDisplayClassGuid, "1234");
        var handler = new AmdSettingsTweakHandler(registry);

        handler.SetState(false);

        var umdPath = $@"{GpuDisplayClassGuid}\1234\UMD";
        Assert.Equal(RegistryBinaryHelpers.HexStringToBytes("31000000"), registry.GetValue(RegistryHive.LocalMachine, umdPath, "VSyncControl"));
        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, umdPath, "TFQ"));
        Assert.Equal(RegistryBinaryHelpers.HexStringToBytes("360034000000"), registry.GetValue(RegistryHive.LocalMachine, umdPath, "Tessellation"));
        Assert.Equal(RegistryBinaryHelpers.HexStringToBytes("30000000"), registry.GetValue(RegistryHive.LocalMachine, umdPath, "Tessellation_OPTION"));
    }

    [Fact]
    public void AmdSettings_SetState_true_deletes_then_recreates_Notification_subkey_empty()
    {
        var registry = new FakeRegistryService();
        var handler = new AmdSettingsTweakHandler(registry);

        handler.SetState(true);

        Assert.True(registry.WasSubKeyTreeDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN\Notification"));
        Assert.True(registry.WasSubKeyCreated(RegistryHive.CurrentUser, @"Software\AMD\CN\Notification"));
    }

    [Fact]
    public void AmdSettings_SetState_false_deletes_CustomResolutions_DisplayOverride_Notification_and_AlreadyNotified_subkeys()
    {
        var registry = new FakeRegistryService();
        var handler = new AmdSettingsTweakHandler(registry);

        handler.SetState(false);

        Assert.True(registry.WasSubKeyTreeDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN\CustomResolutions"));
        Assert.True(registry.WasSubKeyTreeDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN\DisplayOverride"));
        Assert.True(registry.WasSubKeyTreeDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN\Notification"));
        Assert.True(registry.WasSubKeyTreeDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN\FreeSync"));
        Assert.True(registry.WasSubKeyTreeDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN\OverlayNotification"));
        Assert.True(registry.WasSubKeyTreeDeleted(RegistryHive.CurrentUser, @"Software\AMD\CN\VirtualSuperResolution"));
    }

    [Fact]
    public void AmdSettings_GetState_returns_true_only_when_AutoUpdate_equals_0()
    {
        var registry = new FakeRegistryService();
        registry.Seed(RegistryHive.CurrentUser, @"Software\AMD\CN", "AutoUpdate", 0);
        var handler = new AmdSettingsTweakHandler(registry);

        Assert.True(handler.GetState());
    }

    [Fact]
    public void AmdSettings_GetState_returns_false_when_AutoUpdate_is_absent_or_nonzero()
    {
        var registry = new FakeRegistryService();
        var handler = new AmdSettingsTweakHandler(registry);
        Assert.False(handler.GetState());

        registry.Seed(RegistryHive.CurrentUser, @"Software\AMD\CN", "AutoUpdate", 1);
        Assert.False(handler.GetState());
    }

    [Fact]
    public void AmdSettings_metadata_is_Order_103_Category_Gaming()
    {
        var handler = new AmdSettingsTweakHandler(new FakeRegistryService());

        Assert.Equal(103, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
        Assert.Equal("gpuamdsettings", handler.Key);
    }

    private sealed class FakeRegistryService : IRegistryService
    {
        private readonly Dictionary<(RegistryHive Hive, string SubKeyPath, string ValueName), (object? Value, RegistryValueKind Kind)> _values = new();
        private readonly Dictionary<(RegistryHive Hive, string SubKeyPath), List<string>> _subKeyNames = new();
        private readonly HashSet<(RegistryHive Hive, string SubKeyPath, string ValueName)> _deletedKeys = new();
        private readonly HashSet<(RegistryHive Hive, string SubKeyPath)> _deletedSubKeyTrees = new();
        private readonly HashSet<(RegistryHive Hive, string SubKeyPath)> _createdSubKeys = new();

        public void SetSubKeyNames(RegistryHive hive, string subKeyPath, params string[] names) =>
            _subKeyNames[(hive, subKeyPath)] = names.ToList();

        public void Seed(RegistryHive hive, string subKeyPath, string valueName, object? value, RegistryValueKind kind = RegistryValueKind.DWord) =>
            _values[(hive, subKeyPath, valueName)] = (value, kind);

        public bool WasDeleted(RegistryHive hive, string subKeyPath, string valueName) =>
            _deletedKeys.Contains((hive, subKeyPath, valueName));

        public bool WasSubKeyTreeDeleted(RegistryHive hive, string subKeyPath) =>
            _deletedSubKeyTrees.Contains((hive, subKeyPath));

        public bool WasSubKeyCreated(RegistryHive hive, string subKeyPath) =>
            _createdSubKeys.Contains((hive, subKeyPath));

        public object? GetValue(RegistryHive hive, string subKeyPath, string valueName) =>
            _values.TryGetValue((hive, subKeyPath, valueName), out var v) ? v.Value : null;

        public (object? Value, RegistryValueKind Kind) GetValueWithKind(RegistryHive hive, string subKeyPath, string valueName) =>
            _values.TryGetValue((hive, subKeyPath, valueName), out var v) ? v : (null, default);

        public void SetValue(RegistryHive hive, string subKeyPath, string valueName, object value, RegistryValueKind kind) =>
            _values[(hive, subKeyPath, valueName)] = (value, kind);

        public void DeleteValue(RegistryHive hive, string subKeyPath, string valueName)
        {
            _values.Remove((hive, subKeyPath, valueName));
            _deletedKeys.Add((hive, subKeyPath, valueName));
        }

        public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath) =>
            _subKeyNames.TryGetValue((hive, subKeyPath), out var names) ? names : [];

        public void DeleteSubKeyTree(RegistryHive hive, string subKeyPath)
        {
            foreach (var key in _values.Keys.Where(k => k.Hive == hive && (k.SubKeyPath == subKeyPath || k.SubKeyPath.StartsWith(subKeyPath + "\\", StringComparison.Ordinal))).ToList())
            {
                _values.Remove(key);
            }

            // Historical fact — kept even if a later CreateSubKey recreates the same path,
            // so tests can assert "deleted then recreated" as two independent occurrences.
            _deletedSubKeyTrees.Add((hive, subKeyPath));
        }

        public void CreateSubKey(RegistryHive hive, string subKeyPath) =>
            _createdSubKeys.Add((hive, subKeyPath));

        public RegistryKey OpenRealUserHive(string subKeyPath) =>
            throw new NotSupportedException("Not needed for Gaming Graphics tests.");
    }

    private sealed class FakeScriptRunner(string captureOutput) : IScriptRunner
    {
        public Task<int> RunProcessAsync(string fileName, string arguments, TimeSpan? timeout = null) =>
            Task.FromResult(0);

        public Task<string> RunProcessCaptureOutputAsync(string fileName, string arguments, TimeSpan? timeout = null) =>
            Task.FromResult(captureOutput);

        public Task<int> RunEmbeddedScriptAsync(string resourceSuffix, string? arguments = null, TimeSpan? timeout = null) =>
            throw new NotSupportedException("Not needed for Gaming Graphics tests.");
    }
}
