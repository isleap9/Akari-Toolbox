using System.Collections.ObjectModel;
using AkariToolbox.App.Services;
using AkariToolbox.App.Services.TweakHandlers;
using AkariToolbox.Framework.Services;
using Microsoft.Win32;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Covers <see cref="DevicePowerSavingsTweakHandler"/>, <see cref="NetAdapterPowerSavingsTweakHandler"/>,
/// and <see cref="WriteCacheFlushTweakHandler"/> — ported from <c>6 Windows/25</c>, <c>26</c>,
/// and <c>28</c> (02-CONTEXT.md D-07). Exercises against a hand-rolled fake registry
/// (matching this project's existing test-double style) whose <c>SetValue</c> auto-registers
/// the written subkey path into its parent's subkey-name listing, mirroring how a real
/// registry key becomes enumerable the moment a value is written under it — this is what
/// makes the write-cache-flush round-trip test (On creates, Off finds and deletes) work
/// against a single shared fake tree.
/// </summary>
public class GamingWindowsTweaksTests
{
    private const string EnumRoot = @"SYSTEM\CurrentControlSet\Enum";

    private static void SeedFourClassDeviceTree(FakeRegistryService registry)
    {
        foreach (var deviceClass in new[] { "ACPI", "HID", "PCI", "USB" })
        {
            registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\{deviceClass}", "DEV0");
            registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\{deviceClass}\DEV0", "Device Parameters");
        }

        // Exactly one WDF match, nested alongside ACPI's Device Parameters instance.
        registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\ACPI\DEV0", "Device Parameters", "WDF");
    }

    // ---------- DevicePowerSavingsTweakHandler (Task 1) ----------

    [Fact]
    public void DevicePowerSavings_SetState_true_writes_documented_values_to_every_DeviceParameters_and_WDF_match()
    {
        var registry = new FakeRegistryService();
        SeedFourClassDeviceTree(registry);
        var handler = new DevicePowerSavingsTweakHandler(registry);

        handler.SetState(true);

        foreach (var deviceClass in new[] { "ACPI", "HID", "PCI", "USB" })
        {
            var path = $@"{EnumRoot}\{deviceClass}\DEV0\Device Parameters";
            Assert.Equal(0, registry.GetValue(RegistryHive.LocalMachine, path, "EnhancedPowerManagementEnabled"));
            Assert.Equal(0, registry.GetValue(RegistryHive.LocalMachine, path, "SelectiveSuspendOn"));
            Assert.Equal(0, registry.GetValue(RegistryHive.LocalMachine, path, "WaitWakeEnabled"));
        }

        const string wdfPath = $@"{EnumRoot}\ACPI\DEV0\WDF";
        Assert.Equal(0, registry.GetValue(RegistryHive.LocalMachine, wdfPath, "IdleInWorkingState"));
    }

    [Fact]
    public void DevicePowerSavings_SetState_true_preserves_ACPI_typo_and_uses_correct_spelling_elsewhere()
    {
        var registry = new FakeRegistryService();
        SeedFourClassDeviceTree(registry);
        var handler = new DevicePowerSavingsTweakHandler(registry);

        handler.SetState(true);

        const string acpiPath = $@"{EnumRoot}\ACPI\DEV0\Device Parameters";
        Assert.Equal(new byte[] { 0x00 }, registry.GetValue(RegistryHive.LocalMachine, acpiPath, "SeleactiveSuspendEnabled"));
        Assert.Null(registry.GetValue(RegistryHive.LocalMachine, acpiPath, "SelectiveSuspendEnabled"));

        foreach (var deviceClass in new[] { "HID", "PCI", "USB" })
        {
            var path = $@"{EnumRoot}\{deviceClass}\DEV0\Device Parameters";
            Assert.Equal(new byte[] { 0x00 }, registry.GetValue(RegistryHive.LocalMachine, path, "SelectiveSuspendEnabled"));
            Assert.Null(registry.GetValue(RegistryHive.LocalMachine, path, "SeleactiveSuspendEnabled"));
        }
    }

    [Fact]
    public void DevicePowerSavings_SetState_false_deletes_every_value_written_by_SetState_true()
    {
        var registry = new FakeRegistryService();
        SeedFourClassDeviceTree(registry);
        var handler = new DevicePowerSavingsTweakHandler(registry);
        handler.SetState(true);

        handler.SetState(false);

        const string acpiPath = $@"{EnumRoot}\ACPI\DEV0\Device Parameters";
        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, acpiPath, "EnhancedPowerManagementEnabled"));
        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, acpiPath, "SeleactiveSuspendEnabled"));
        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, acpiPath, "SelectiveSuspendOn"));
        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, acpiPath, "WaitWakeEnabled"));

        const string hidPath = $@"{EnumRoot}\HID\DEV0\Device Parameters";
        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, hidPath, "SelectiveSuspendEnabled"));

        const string wdfPath = $@"{EnumRoot}\ACPI\DEV0\WDF";
        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, wdfPath, "IdleInWorkingState"));
    }

    [Fact]
    public void DevicePowerSavings_GetState_returns_false_when_no_matches_exist()
    {
        var registry = new FakeRegistryService();
        var handler = new DevicePowerSavingsTweakHandler(registry);

        Assert.False(handler.GetState());
    }

    [Fact]
    public void DevicePowerSavings_GetState_returns_true_after_SetState_true()
    {
        var registry = new FakeRegistryService();
        SeedFourClassDeviceTree(registry);
        var handler = new DevicePowerSavingsTweakHandler(registry);

        handler.SetState(true);

        Assert.True(handler.GetState());
    }

    [Fact]
    public void DevicePowerSavings_metadata_is_Order_105_Category_Gaming()
    {
        var handler = new DevicePowerSavingsTweakHandler(new FakeRegistryService());

        Assert.Equal(105, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
        Assert.Equal("devpowersavings", handler.Key);
    }

    // ---------- NetAdapterPowerSavingsTweakHandler (Task 2) ----------

    private const string NetworkAdapterClassGuid = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

    private static readonly string[] ExpectedRegSzValueNames =
    [
        "AdvancedEEE", "*EEE", "EEELinkAdvertisement", "SipsEnabled", "ULPMode", "GigaLite",
        "EnableGreenEthernet", "PowerSavingMode", "S5WakeOnLan", "*WakeOnMagicPacket",
        "*ModernStandbyWoLMagicPacket", "*WakeOnPattern", "WakeOnLink",
    ];

    [Fact]
    public void NetAdapterPowerSavings_SetState_true_writes_PnPCapabilities_and_all_RegSz_values_to_every_adapter()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, NetworkAdapterClassGuid, "1234", "5678");
        var handler = new NetAdapterPowerSavingsTweakHandler(registry);

        handler.SetState(true);

        foreach (var adapter in new[] { "1234", "5678" })
        {
            var path = $@"{NetworkAdapterClassGuid}\{adapter}";
            Assert.Equal(24, registry.GetValue(RegistryHive.LocalMachine, path, "PnPCapabilities"));
            foreach (var valueName in ExpectedRegSzValueNames)
            {
                Assert.Equal("0", registry.GetValue(RegistryHive.LocalMachine, path, valueName));
            }
        }
    }

    [Fact]
    public void NetAdapterPowerSavings_SetState_false_deletes_all_13_values_from_every_adapter()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, NetworkAdapterClassGuid, "1234");
        var handler = new NetAdapterPowerSavingsTweakHandler(registry);
        handler.SetState(true);

        handler.SetState(false);

        const string path = $@"{NetworkAdapterClassGuid}\1234";
        Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, path, "PnPCapabilities"));
        foreach (var valueName in ExpectedRegSzValueNames)
        {
            Assert.True(registry.WasDeleted(RegistryHive.LocalMachine, path, valueName));
        }
    }

    [Fact]
    public void NetAdapterPowerSavings_GetState_returns_false_when_no_adapters_found()
    {
        var registry = new FakeRegistryService();
        var handler = new NetAdapterPowerSavingsTweakHandler(registry);

        Assert.False(handler.GetState());
    }

    [Fact]
    public void NetAdapterPowerSavings_metadata_is_Order_106_Category_Gaming()
    {
        var handler = new NetAdapterPowerSavingsTweakHandler(new FakeRegistryService());

        Assert.Equal(106, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
        Assert.Equal("netpowersavings", handler.Key);
    }

    // ---------- WriteCacheFlushTweakHandler (Task 3) ----------

    private static void SeedScsiNvmeDeviceParameters(FakeRegistryService registry)
    {
        registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\SCSI", "DEV0");
        registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\SCSI\DEV0", "Device Parameters");
        registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\NVME", "DEV0");
        registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\NVME\DEV0", "Device Parameters");
    }

    [Fact]
    public void WriteCacheFlush_SetState_true_creates_child_Disk_subkey_and_writes_CacheIsPowerProtected()
    {
        var registry = new FakeRegistryService();
        SeedScsiNvmeDeviceParameters(registry);
        var handler = new WriteCacheFlushTweakHandler(registry);

        handler.SetState(true);

        const string scsiDiskPath = $@"{EnumRoot}\SCSI\DEV0\Device Parameters\Disk";
        const string nvmeDiskPath = $@"{EnumRoot}\NVME\DEV0\Device Parameters\Disk";
        Assert.Equal(1, registry.GetValue(RegistryHive.LocalMachine, scsiDiskPath, "CacheIsPowerProtected"));
        Assert.Equal(1, registry.GetValue(RegistryHive.LocalMachine, nvmeDiskPath, "CacheIsPowerProtected"));
    }

    [Fact]
    public void WriteCacheFlush_SetState_false_deletes_subkeys_named_exactly_Disk()
    {
        var registry = new FakeRegistryService();
        registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\SCSI", "DEV0");
        registry.SetSubKeyNames(RegistryHive.LocalMachine, $@"{EnumRoot}\SCSI\DEV0", "Disk");
        registry.Seed(RegistryHive.LocalMachine, $@"{EnumRoot}\SCSI\DEV0\Disk", "CacheIsPowerProtected", 1);
        var handler = new WriteCacheFlushTweakHandler(registry);

        handler.SetState(false);

        const string diskPath = $@"{EnumRoot}\SCSI\DEV0\Disk";
        Assert.True(registry.WasSubKeyTreeDeleted(RegistryHive.LocalMachine, diskPath));
        Assert.Null(registry.GetValue(RegistryHive.LocalMachine, diskPath, "CacheIsPowerProtected"));
    }

    [Fact]
    public void WriteCacheFlush_SetState_false_finds_and_deletes_what_SetState_true_created_round_trip()
    {
        var registry = new FakeRegistryService();
        SeedScsiNvmeDeviceParameters(registry);
        var handler = new WriteCacheFlushTweakHandler(registry);
        handler.SetState(true);

        handler.SetState(false);

        const string scsiDiskPath = $@"{EnumRoot}\SCSI\DEV0\Device Parameters\Disk";
        const string nvmeDiskPath = $@"{EnumRoot}\NVME\DEV0\Device Parameters\Disk";
        Assert.Null(registry.GetValue(RegistryHive.LocalMachine, scsiDiskPath, "CacheIsPowerProtected"));
        Assert.Null(registry.GetValue(RegistryHive.LocalMachine, nvmeDiskPath, "CacheIsPowerProtected"));
        Assert.False(handler.GetState());
    }

    [Fact]
    public void WriteCacheFlush_metadata_is_Order_108_Category_Gaming()
    {
        var handler = new WriteCacheFlushTweakHandler(new FakeRegistryService());

        Assert.Equal(108, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
        Assert.Equal("writecacheflush", handler.Key);
    }

    // ---------- NetworkIpv4OnlyTweakHandler (Task 1) ----------

    private static readonly string[] ExpectedDisableComponentIds =
    [
        "ms_lldp", "ms_lltdio", "ms_implat", "ms_rspndr", "ms_tcpip6", "ms_server", "ms_msclient", "ms_pacer",
    ];

    [Fact]
    public void NetworkIpv4Only_SetState_true_disables_the_8_documented_component_ids()
    {
        var scriptRunner = new FakeScriptRunner();
        var handler = new NetworkIpv4OnlyTweakHandler(scriptRunner);

        handler.SetState(true);

        var disableCalls = scriptRunner.Calls
            .Where(c => c.FileName == "powershell.exe" && c.Arguments.Contains("Disable-NetAdapterBinding", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(8, disableCalls.Count);
        foreach (var id in ExpectedDisableComponentIds)
        {
            Assert.Single(disableCalls, c => c.Arguments.EndsWith($"{id}\"", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void NetworkIpv4Only_SetState_false_enables_exactly_9_component_ids_including_ms_tcpip()
    {
        var scriptRunner = new FakeScriptRunner();
        var handler = new NetworkIpv4OnlyTweakHandler(scriptRunner);

        handler.SetState(false);

        var enableCalls = scriptRunner.Calls
            .Where(c => c.FileName == "powershell.exe" && c.Arguments.Contains("Enable-NetAdapterBinding", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(9, enableCalls.Count);
        foreach (var id in ExpectedDisableComponentIds)
        {
            Assert.Single(enableCalls, c => c.Arguments.EndsWith($"{id}\"", StringComparison.Ordinal));
        }

        // ms_tcpip is the 9th ID, absent from the On/disable branch — assert it is present
        // exactly once and distinct from ms_tcpip6 (EndsWith avoids a Contains false-match).
        Assert.Single(enableCalls, c => c.Arguments.EndsWith("ms_tcpip\"", StringComparison.Ordinal));
        Assert.Single(enableCalls, c => c.Arguments.EndsWith("ms_tcpip6\"", StringComparison.Ordinal));
    }

    [Fact]
    public void NetworkIpv4Only_GetState_returns_true_when_ms_tcpip6_binding_is_disabled()
    {
        var scriptRunner = new FakeScriptRunner { CaptureOutputResponder = (_, _) => "False" };
        var handler = new NetworkIpv4OnlyTweakHandler(scriptRunner);

        Assert.True(handler.GetState());
    }

    [Fact]
    public void NetworkIpv4Only_GetState_returns_false_when_binding_enabled_or_unparseable()
    {
        var enabledRunner = new FakeScriptRunner { CaptureOutputResponder = (_, _) => "True" };
        Assert.False(new NetworkIpv4OnlyTweakHandler(enabledRunner).GetState());

        var emptyRunner = new FakeScriptRunner { CaptureOutputResponder = (_, _) => string.Empty };
        Assert.False(new NetworkIpv4OnlyTweakHandler(emptyRunner).GetState());
    }

    [Fact]
    public void NetworkIpv4Only_metadata_is_Order_107_Category_Gaming()
    {
        var handler = new NetworkIpv4OnlyTweakHandler(new FakeScriptRunner());

        Assert.Equal(107, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
        Assert.Equal("netipv4only", handler.Key);
    }

    // ---------- PowerPlanTweakHandler (Task 2) ----------

    private const string PowerPlanExportDirName = "AkariToolbox-PowerPlanBackup";

    private static string PowerPlanExportDir() => Path.Combine(Path.GetTempPath(), PowerPlanExportDirName);

    private static void CleanupPowerPlanExportDir()
    {
        try
        {
            var dir = PowerPlanExportDir();
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // Best-effort test cleanup only.
        }
    }

    [Fact]
    public void PowerPlan_SetState_true_exports_every_existing_scheme_before_any_delete_call()
    {
        CleanupPowerPlanExportDir();
        try
        {
            var registry = new FakeRegistryService();
            var scriptRunner = new FakeScriptRunner
            {
                CaptureOutputResponder = (_, arguments) => arguments == "/L"
                    ? "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)\r\n" +
                      "Power Scheme GUID: a1841308-3541-4fab-bc81-f71556f20b4a  (Power saver)\r\n"
                    : string.Empty,
            };
            var handler = new PowerPlanTweakHandler(scriptRunner, registry, new FakeLogConsoleService());

            handler.SetState(true);

            var exportIndexes = IndexesWhere(scriptRunner.Calls, c => c.Arguments.StartsWith("-export", StringComparison.Ordinal));
            var deleteIndexes = IndexesWhere(scriptRunner.Calls, c => c.Arguments.StartsWith("/delete", StringComparison.Ordinal));

            Assert.Equal(2, exportIndexes.Count);
            Assert.Equal(2, deleteIndexes.Count);
            Assert.True(exportIndexes.Max() < deleteIndexes.Min());
        }
        finally
        {
            CleanupPowerPlanExportDir();
        }
    }

    [Fact]
    public void PowerPlan_SetState_false_falls_back_to_restoredefaultschemes_when_no_session_export_exists()
    {
        CleanupPowerPlanExportDir();
        try
        {
            var registry = new FakeRegistryService();
            var scriptRunner = new FakeScriptRunner();
            var log = new FakeLogConsoleService();
            var handler = new PowerPlanTweakHandler(scriptRunner, registry, log);

            handler.SetState(false);

            Assert.Contains(scriptRunner.Calls, c => c.FileName == "powercfg" && c.Arguments == "-restoredefaultschemes");
            Assert.DoesNotContain(scriptRunner.Calls, c => c.Arguments.StartsWith("-import", StringComparison.Ordinal));
            Assert.NotEmpty(log.Messages);
        }
        finally
        {
            CleanupPowerPlanExportDir();
        }
    }

    [Fact]
    public void PowerPlan_SetState_false_imports_session_backup_instead_of_restoredefaultschemes_when_export_exists()
    {
        CleanupPowerPlanExportDir();
        var exportDir = PowerPlanExportDir();
        Directory.CreateDirectory(exportDir);
        File.WriteAllText(Path.Combine(exportDir, "381b4222-f694-41f0-9685-ff5bb260df2e.pow"), "fake-pow-contents");

        try
        {
            var registry = new FakeRegistryService();
            var scriptRunner = new FakeScriptRunner();
            var handler = new PowerPlanTweakHandler(scriptRunner, registry, new FakeLogConsoleService());

            handler.SetState(false);

            Assert.Contains(scriptRunner.Calls, c => c.FileName == "powercfg" && c.Arguments.StartsWith("-import", StringComparison.Ordinal));
            Assert.DoesNotContain(scriptRunner.Calls, c => c.Arguments == "-restoredefaultschemes");
            Assert.False(Directory.Exists(exportDir));
        }
        finally
        {
            CleanupPowerPlanExportDir();
        }
    }

    [Fact]
    public void PowerPlan_GetState_returns_true_when_active_scheme_output_contains_custom_guid()
    {
        var scriptRunner = new FakeScriptRunner
        {
            CaptureOutputResponder = (_, arguments) => arguments == "/getactivescheme"
                ? "Power Scheme GUID: 99999999-9999-9999-9999-999999999999  (Akari Toolbox Gaming)"
                : string.Empty,
        };
        var handler = new PowerPlanTweakHandler(scriptRunner, new FakeRegistryService(), new FakeLogConsoleService());

        Assert.True(handler.GetState());
    }

    [Fact]
    public void PowerPlan_metadata_is_Order_109_Category_Gaming()
    {
        var handler = new PowerPlanTweakHandler(new FakeScriptRunner(), new FakeRegistryService(), new FakeLogConsoleService());

        Assert.Equal(109, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
        Assert.Equal("powerplan", handler.Key);
    }

    private static List<int> IndexesWhere(IReadOnlyList<(string FileName, string Arguments)> calls, Func<(string FileName, string Arguments), bool> predicate) =>
        calls.Select((call, index) => (call, index)).Where(x => predicate(x.call)).Select(x => x.index).ToList();

    // ---------- TimerResolutionTweakHandler (Task 3) ----------

    private const string KernelKey = @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel";
    private const string CscPath = @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe";

    [Fact]
    public void TimerResolution_SetState_true_logs_failure_and_skips_compilation_when_csc_missing()
    {
        var scriptRunner = new FakeScriptRunner();
        var log = new FakeLogConsoleService();
        var handler = new TimerResolutionTweakHandler(
            scriptRunner, new FakeRegistryService(), new FakeWindowsServiceController(), log,
            fileExists: _ => false);

        handler.SetState(true);

        Assert.DoesNotContain(scriptRunner.Calls, c => c.FileName == CscPath);
        Assert.NotEmpty(log.Messages);
    }

    [Fact]
    public void TimerResolution_SetState_true_compiles_via_csc_when_compiler_present()
    {
        var scriptRunner = new FakeScriptRunner();
        var handler = new TimerResolutionTweakHandler(
            scriptRunner, new FakeRegistryService(), new FakeWindowsServiceController(), new FakeLogConsoleService(),
            fileExists: _ => true,
            writeAllText: (_, _) => { });

        handler.SetState(true);

        var compileCall = Assert.Single(scriptRunner.Calls, c => c.FileName == CscPath);
        Assert.Contains(@"C:\Windows\SetTimerResolutionService.exe", compileCall.Arguments, StringComparison.Ordinal);
        Assert.Contains(@"C:\Windows\SetTimerResolutionService.cs", compileCall.Arguments, StringComparison.Ordinal);
    }

    [Fact]
    public void TimerResolution_GetState_returns_true_only_when_GlobalTimerResolutionRequests_equals_1()
    {
        var registry = new FakeRegistryService();
        var handler = new TimerResolutionTweakHandler(new FakeScriptRunner(), registry, new FakeWindowsServiceController(), new FakeLogConsoleService());

        registry.Seed(RegistryHive.LocalMachine, KernelKey, "GlobalTimerResolutionRequests", 1);
        Assert.True(handler.GetState());

        registry.Seed(RegistryHive.LocalMachine, KernelKey, "GlobalTimerResolutionRequests", 0);
        Assert.False(handler.GetState());
    }

    [Fact]
    public void TimerResolution_metadata_is_Order_110_Category_Gaming()
    {
        var handler = new TimerResolutionTweakHandler(new FakeScriptRunner(), new FakeRegistryService(), new FakeWindowsServiceController(), new FakeLogConsoleService());

        Assert.Equal(110, handler.Order);
        Assert.Equal(TweakCategory.Gaming, handler.Category);
        Assert.Equal("timerresolution", handler.Key);
    }

    private sealed class FakeWindowsServiceController : IWindowsServiceController
    {
        private readonly Dictionary<string, int> _startTypes = new(StringComparer.OrdinalIgnoreCase);

        public int? GetStartType(string serviceName) =>
            _startTypes.TryGetValue(serviceName, out var value) ? value : null;

        public void SetStartType(string serviceName, int startValue) => _startTypes[serviceName] = startValue;
    }

    private sealed class FakeLogConsoleService : ILogConsoleService
    {
        public ObservableCollection<string> Lines { get; } = new();

        public List<string> Messages { get; } = new();

        public void Log(string message)
        {
            Messages.Add(message);
            Lines.Add(message);
        }
    }

    private sealed class FakeScriptRunner : IScriptRunner
    {
        public List<(string FileName, string Arguments)> Calls { get; } = new();

        public Func<string, string, string>? CaptureOutputResponder { get; set; }

        /// <summary>
        /// Exit code override per (fileName, arguments) call, defaulting to 0 (success)
        /// when unset — lets CR-01-style tests (02-REVIEW.md) simulate a failing
        /// <c>powercfg</c> call without needing a real process.
        /// </summary>
        public Func<string, string, int>? ExitCodeResponder { get; set; }

        public Task<int> RunProcessAsync(string fileName, string arguments, TimeSpan? timeout = null)
        {
            Calls.Add((fileName, arguments));

            // CR-01 fix (02-REVIEW.md): PowerPlanTweakHandler now verifies
            // File.Exists(exportPath) after every "-export" call, not just the exit
            // code — simulate powercfg actually writing its output file so that check
            // exercises its real success path here instead of always failing against
            // this in-memory fake.
            if (string.Equals(fileName, "powercfg", StringComparison.Ordinal) &&
                arguments.StartsWith("-export \"", StringComparison.Ordinal))
            {
                var start = arguments.IndexOf('"') + 1;
                var end = arguments.IndexOf('"', start);
                if (end > start)
                {
                    File.WriteAllText(arguments[start..end], "fake-export-contents");
                }
            }

            return Task.FromResult(ExitCodeResponder?.Invoke(fileName, arguments) ?? 0);
        }

        public Task<string> RunProcessCaptureOutputAsync(string fileName, string arguments, TimeSpan? timeout = null)
        {
            Calls.Add((fileName, arguments));
            return Task.FromResult(CaptureOutputResponder?.Invoke(fileName, arguments) ?? string.Empty);
        }

        public Task<int> RunEmbeddedScriptAsync(string resourceSuffix, string? arguments = null, TimeSpan? timeout = null) =>
            throw new NotSupportedException("Not needed for Gaming Windows tests.");
    }

    private sealed class FakeRegistryService : IRegistryService
    {
        private readonly Dictionary<(RegistryHive Hive, string SubKeyPath, string ValueName), object?> _values = new();
        private readonly Dictionary<(RegistryHive Hive, string SubKeyPath), List<string>> _subKeyNames = new();
        private readonly HashSet<(RegistryHive Hive, string SubKeyPath, string ValueName)> _deletedKeys = new();
        private readonly HashSet<(RegistryHive Hive, string SubKeyPath)> _deletedSubKeyTrees = new();

        public void SetSubKeyNames(RegistryHive hive, string subKeyPath, params string[] names) =>
            _subKeyNames[(hive, subKeyPath)] = names.ToList();

        public void Seed(RegistryHive hive, string subKeyPath, string valueName, object? value) =>
            _values[(hive, subKeyPath, valueName)] = value;

        public bool WasDeleted(RegistryHive hive, string subKeyPath, string valueName) =>
            _deletedKeys.Contains((hive, subKeyPath, valueName));

        public bool WasSubKeyTreeDeleted(RegistryHive hive, string subKeyPath) =>
            _deletedSubKeyTrees.Contains((hive, subKeyPath));

        public object? GetValue(RegistryHive hive, string subKeyPath, string valueName) =>
            _values.TryGetValue((hive, subKeyPath, valueName), out var v) ? v : null;

        public void SetValue(RegistryHive hive, string subKeyPath, string valueName, object value, RegistryValueKind kind)
        {
            _values[(hive, subKeyPath, valueName)] = value;
            RegisterSubKeyPath(hive, subKeyPath);
        }

        public void DeleteValue(RegistryHive hive, string subKeyPath, string valueName)
        {
            _values.Remove((hive, subKeyPath, valueName));
            _deletedKeys.Add((hive, subKeyPath, valueName));
        }

        public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath) =>
            _subKeyNames.TryGetValue((hive, subKeyPath), out var names) ? names : [];

        public void DeleteSubKeyTree(RegistryHive hive, string subKeyPath)
        {
            foreach (var key in _values.Keys
                         .Where(k => k.Hive == hive && (k.SubKeyPath == subKeyPath || k.SubKeyPath.StartsWith(subKeyPath + "\\", StringComparison.Ordinal)))
                         .ToList())
            {
                _values.Remove(key);
            }

            _deletedSubKeyTrees.Add((hive, subKeyPath));
            _subKeyNames.Remove((hive, subKeyPath));

            var lastSeparator = subKeyPath.LastIndexOf('\\');
            if (lastSeparator >= 0)
            {
                var parentPath = subKeyPath[..lastSeparator];
                var name = subKeyPath[(lastSeparator + 1)..];
                if (_subKeyNames.TryGetValue((hive, parentPath), out var siblingNames))
                {
                    siblingNames.Remove(name);
                }
            }
        }

        public RegistryKey OpenRealUserHive(string subKeyPath) =>
            throw new NotSupportedException("Not needed for Gaming Windows tests.");

        /// <summary>
        /// Mirrors real-registry auto-creation semantics: writing a value under a path
        /// registers that path as a child of its parent (and so on up the chain), so a
        /// later <see cref="GetSubKeyNames"/> walk discovers what a prior write created —
        /// this is what lets the write-cache-flush round-trip test find the "Disk" subkey
        /// the On path created without the test manually re-seeding it.
        /// </summary>
        private void RegisterSubKeyPath(RegistryHive hive, string subKeyPath)
        {
            var lastSeparator = subKeyPath.LastIndexOf('\\');
            if (lastSeparator < 0)
            {
                return;
            }

            var parentPath = subKeyPath[..lastSeparator];
            var name = subKeyPath[(lastSeparator + 1)..];

            if (!_subKeyNames.TryGetValue((hive, parentPath), out var names))
            {
                names = [];
                _subKeyNames[(hive, parentPath)] = names;
            }

            if (!names.Contains(name, StringComparer.Ordinal))
            {
                names.Add(name);
            }

            RegisterSubKeyPath(hive, parentPath);
        }
    }
}
