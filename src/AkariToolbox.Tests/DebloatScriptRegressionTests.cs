using System.Security.Principal;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;
using Xunit;
using AppEntry = AkariToolbox.App.App;

namespace AkariToolbox.Tests;

/// <summary>
/// Proves the CR-01..CR-06 fixes (03-REVIEW.md) by reading real system state — not
/// <see cref="IScriptRunner"/> call wiring, which <see cref="DebloatCatalogTests"/> already
/// covers and which 03-VERIFICATION.md proved is structurally blind to this bug class (a
/// broken Undo script can exit 0 and print a success message while touching the wrong
/// registry key/hive/env-var/ACL entirely).
///
/// Facts that touch <c>HKLM</c> or a machine-scope environment variable require an elevated
/// test process (the same privilege the shipped app itself requires per APP-01/
/// <c>requireAdministrator</c>) and self-skip, with a printed reason, when the test process
/// is not elevated — rather than failing the default <c>dotnet test</c> run or fabricating a
/// false pass. Every fact that mutates real machine state restores the pre-test value in a
/// <c>finally</c> block regardless of pass/fail outcome.
/// </summary>
public class DebloatScriptRegressionTests
{
    private static ScriptRunner CreateRunner() => new(new LogConsoleService(dispatcher: null));

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static object? ReadHklm(string subKeyPath, string valueName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(subKeyPath);
        return key?.GetValue(valueName);
    }

    private static void WriteHklm(string subKeyPath, string valueName, object value, RegistryValueKind kind)
    {
        using var key = Registry.LocalMachine.CreateSubKey(subKeyPath, writable: true);
        key!.SetValue(valueName, value, kind);
    }

    private static void DeleteHklmIfPresent(string subKeyPath, string valueName)
    {
        using var key = Registry.LocalMachine.OpenSubKey(subKeyPath, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    private static object? ReadHkcu(string subKeyPath, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKeyPath);
        return key?.GetValue(valueName);
    }

    private static void WriteHkcu(string subKeyPath, string valueName, object value, RegistryValueKind kind)
    {
        using var key = Registry.CurrentUser.CreateSubKey(subKeyPath, writable: true);
        key!.SetValue(valueName, value, kind);
    }

    private static void DeleteHkcuIfPresent(string subKeyPath, string valueName)
    {
        using var key = Registry.CurrentUser.OpenSubKey(subKeyPath, writable: true);
        key?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    [SkippableFact]
    public async Task LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values()
    {
        Skip.IfNot(IsElevated(), "requires elevation");

        const string consentStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location";
        const string sensorOverridePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}";
        const string lfsvcPath = @"SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration";
        const string mapsPath = @"SYSTEM\Maps";

        var beforeConsentStore = ReadHklm(consentStorePath, "Value");
        var beforeSensor = ReadHklm(sensorOverridePath, "SensorPermissionState");
        var beforeLfsvc = ReadHklm(lfsvcPath, "Status");
        var beforeMaps = ReadHklm(mapsPath, "AutoUpdateEnabled");

        try
        {
            var runner = CreateRunner();

            var runExitCode = await runner.RunEmbeddedScriptAsync("locationtracking.ps1");
            Assert.Equal(0, runExitCode);
            Assert.Equal("Deny", ReadHklm(consentStorePath, "Value"));
            Assert.Equal(0, ReadHklm(sensorOverridePath, "SensorPermissionState"));
            Assert.Equal(0, ReadHklm(lfsvcPath, "Status"));
            Assert.Equal(0, ReadHklm(mapsPath, "AutoUpdateEnabled"));

            var undoExitCode = await runner.RunEmbeddedScriptAsync("locationtracking-undo.ps1");
            Assert.Equal(0, undoExitCode);
            Assert.Equal("Allow", ReadHklm(consentStorePath, "Value"));
            Assert.Equal(1, ReadHklm(sensorOverridePath, "SensorPermissionState"));
            Assert.Equal(1, ReadHklm(lfsvcPath, "Status"));
            Assert.Equal(1, ReadHklm(mapsPath, "AutoUpdateEnabled"));
        }
        finally
        {
            if (beforeConsentStore is not null) WriteHklm(consentStorePath, "Value", beforeConsentStore, RegistryValueKind.String);
            else DeleteHklmIfPresent(consentStorePath, "Value");

            if (beforeSensor is not null) WriteHklm(sensorOverridePath, "SensorPermissionState", beforeSensor, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(sensorOverridePath, "SensorPermissionState");

            if (beforeLfsvc is not null) WriteHklm(lfsvcPath, "Status", beforeLfsvc, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(lfsvcPath, "Status");

            if (beforeMaps is not null) WriteHklm(mapsPath, "AutoUpdateEnabled", beforeMaps, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(mapsPath, "AutoUpdateEnabled");
        }
    }

    [SkippableFact]
    public async Task LocationTracking_undo_alone_is_safe_and_idempotent_without_a_prior_run()
    {
        Skip.IfNot(IsElevated(), "requires elevation");

        const string consentStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location";
        const string sensorOverridePath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}";
        const string lfsvcPath = @"SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration";
        const string mapsPath = @"SYSTEM\Maps";

        var beforeConsentStore = ReadHklm(consentStorePath, "Value");
        var beforeSensor = ReadHklm(sensorOverridePath, "SensorPermissionState");
        var beforeLfsvc = ReadHklm(lfsvcPath, "Status");
        var beforeMaps = ReadHklm(mapsPath, "AutoUpdateEnabled");

        try
        {
            var runner = CreateRunner();

            var undoExitCode = await runner.RunEmbeddedScriptAsync("locationtracking-undo.ps1");
            Assert.Equal(0, undoExitCode);
            Assert.Equal("Allow", ReadHklm(consentStorePath, "Value"));
            Assert.Equal(1, ReadHklm(sensorOverridePath, "SensorPermissionState"));
            Assert.Equal(1, ReadHklm(lfsvcPath, "Status"));
            Assert.Equal(1, ReadHklm(mapsPath, "AutoUpdateEnabled"));
        }
        finally
        {
            if (beforeConsentStore is not null) WriteHklm(consentStorePath, "Value", beforeConsentStore, RegistryValueKind.String);
            else DeleteHklmIfPresent(consentStorePath, "Value");

            if (beforeSensor is not null) WriteHklm(sensorOverridePath, "SensorPermissionState", beforeSensor, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(sensorOverridePath, "SensorPermissionState");

            if (beforeLfsvc is not null) WriteHklm(lfsvcPath, "Status", beforeLfsvc, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(lfsvcPath, "Status");

            if (beforeMaps is not null) WriteHklm(mapsPath, "AutoUpdateEnabled", beforeMaps, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(mapsPath, "AutoUpdateEnabled");
        }
    }

    [SkippableFact]
    public async Task ConsumerFeatures_run_then_undo_restores_DisableWindowsConsumerFeatures_policy()
    {
        Skip.IfNot(IsElevated(), "requires elevation");

        const string policyPath = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent";
        const string explorerAdvancedPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        const string contentDeliveryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";

        var before = ReadHklm(policyPath, "DisableWindowsConsumerFeatures");

        // consumerfeatures-undo.ps1 unconditionally writes these 8 HKCU values as a
        // documented "supplementary step" (03-REVIEW.md WR-01) in addition to reversing
        // the one HKLM policy value this fact asserts on. They must be snapshotted and
        // restored too, or every elevated run of this test permanently flips these
        // content-delivery/suggested-apps settings on the executing user's machine.
        var beforeShowSync = ReadHkcu(explorerAdvancedPath, "ShowSyncProviderNotifications");
        var beforeContentDeliveryAllowed = ReadHkcu(contentDeliveryPath, "ContentDeliveryAllowed");
        var beforeOemPreInstalledAppsEnabled = ReadHkcu(contentDeliveryPath, "OemPreInstalledAppsEnabled");
        var beforePreInstalledAppsEnabled = ReadHkcu(contentDeliveryPath, "PreInstalledAppsEnabled");
        var beforePreInstalledAppsEverEnabled = ReadHkcu(contentDeliveryPath, "PreInstalledAppsEverEnabled");
        var beforeSilentInstallAppsEnabled = ReadHkcu(contentDeliveryPath, "SilentInstallAppsEnabled");
        var beforeSoftLandingEnabled = ReadHkcu(contentDeliveryPath, "SoftLandingEnabled");
        var beforeSubscribedContentEnabled = ReadHkcu(contentDeliveryPath, "SubscribedContentEnabled");

        try
        {
            var runner = CreateRunner();

            var runExitCode = await runner.RunEmbeddedScriptAsync("consumerfeatures.ps1");
            Assert.Equal(0, runExitCode);
            Assert.Equal(1, ReadHklm(policyPath, "DisableWindowsConsumerFeatures"));

            var undoExitCode = await runner.RunEmbeddedScriptAsync("consumerfeatures-undo.ps1");
            Assert.Equal(0, undoExitCode);
            Assert.Null(ReadHklm(policyPath, "DisableWindowsConsumerFeatures"));
            Assert.Equal(1, ReadHkcu(explorerAdvancedPath, "ShowSyncProviderNotifications"));
            Assert.Equal(1, ReadHkcu(contentDeliveryPath, "ContentDeliveryAllowed"));
            Assert.Equal(1, ReadHkcu(contentDeliveryPath, "OemPreInstalledAppsEnabled"));
            Assert.Equal(1, ReadHkcu(contentDeliveryPath, "PreInstalledAppsEnabled"));
            Assert.Equal(1, ReadHkcu(contentDeliveryPath, "PreInstalledAppsEverEnabled"));
            Assert.Equal(1, ReadHkcu(contentDeliveryPath, "SilentInstallAppsEnabled"));
            Assert.Equal(1, ReadHkcu(contentDeliveryPath, "SoftLandingEnabled"));
            Assert.Equal(1, ReadHkcu(contentDeliveryPath, "SubscribedContentEnabled"));
        }
        finally
        {
            if (before is not null) WriteHklm(policyPath, "DisableWindowsConsumerFeatures", before, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(policyPath, "DisableWindowsConsumerFeatures");

            if (beforeShowSync is not null) WriteHkcu(explorerAdvancedPath, "ShowSyncProviderNotifications", beforeShowSync, RegistryValueKind.DWord);
            else DeleteHkcuIfPresent(explorerAdvancedPath, "ShowSyncProviderNotifications");

            if (beforeContentDeliveryAllowed is not null) WriteHkcu(contentDeliveryPath, "ContentDeliveryAllowed", beforeContentDeliveryAllowed, RegistryValueKind.DWord);
            else DeleteHkcuIfPresent(contentDeliveryPath, "ContentDeliveryAllowed");

            if (beforeOemPreInstalledAppsEnabled is not null) WriteHkcu(contentDeliveryPath, "OemPreInstalledAppsEnabled", beforeOemPreInstalledAppsEnabled, RegistryValueKind.DWord);
            else DeleteHkcuIfPresent(contentDeliveryPath, "OemPreInstalledAppsEnabled");

            if (beforePreInstalledAppsEnabled is not null) WriteHkcu(contentDeliveryPath, "PreInstalledAppsEnabled", beforePreInstalledAppsEnabled, RegistryValueKind.DWord);
            else DeleteHkcuIfPresent(contentDeliveryPath, "PreInstalledAppsEnabled");

            if (beforePreInstalledAppsEverEnabled is not null) WriteHkcu(contentDeliveryPath, "PreInstalledAppsEverEnabled", beforePreInstalledAppsEverEnabled, RegistryValueKind.DWord);
            else DeleteHkcuIfPresent(contentDeliveryPath, "PreInstalledAppsEverEnabled");

            if (beforeSilentInstallAppsEnabled is not null) WriteHkcu(contentDeliveryPath, "SilentInstallAppsEnabled", beforeSilentInstallAppsEnabled, RegistryValueKind.DWord);
            else DeleteHkcuIfPresent(contentDeliveryPath, "SilentInstallAppsEnabled");

            if (beforeSoftLandingEnabled is not null) WriteHkcu(contentDeliveryPath, "SoftLandingEnabled", beforeSoftLandingEnabled, RegistryValueKind.DWord);
            else DeleteHkcuIfPresent(contentDeliveryPath, "SoftLandingEnabled");

            if (beforeSubscribedContentEnabled is not null) WriteHkcu(contentDeliveryPath, "SubscribedContentEnabled", beforeSubscribedContentEnabled, RegistryValueKind.DWord);
            else DeleteHkcuIfPresent(contentDeliveryPath, "SubscribedContentEnabled");
        }
    }

    [SkippableFact]
    public async Task Ps7Telemetry_run_then_undo_restores_the_machine_scope_env_var()
    {
        Skip.IfNot(IsElevated(), "requires elevation");

        var before = Environment.GetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", EnvironmentVariableTarget.Machine);

        try
        {
            var runner = CreateRunner();

            var runExitCode = await runner.RunEmbeddedScriptAsync("ps7telemetry.ps1");
            Assert.Equal(0, runExitCode);
            Assert.Equal("1", Environment.GetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", EnvironmentVariableTarget.Machine));

            var undoExitCode = await runner.RunEmbeddedScriptAsync("ps7telemetry-undo.ps1");
            Assert.Equal(0, undoExitCode);
            Assert.Null(Environment.GetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", EnvironmentVariableTarget.Machine));
        }
        finally
        {
            Environment.SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", before, EnvironmentVariableTarget.Machine);
        }
    }

    [SkippableFact]
    public async Task Wpbt_run_then_undo_restores_DisableWpbtExecution()
    {
        Skip.IfNot(IsElevated(), "requires elevation");

        const string sessionManagerPath = @"SYSTEM\CurrentControlSet\Control\Session Manager";
        var before = ReadHklm(sessionManagerPath, "DisableWpbtExecution");

        try
        {
            var runner = CreateRunner();

            var runExitCode = await runner.RunEmbeddedScriptAsync("wpbt.ps1");
            Assert.Equal(0, runExitCode);
            Assert.Equal(1, ReadHklm(sessionManagerPath, "DisableWpbtExecution"));

            var undoExitCode = await runner.RunEmbeddedScriptAsync("wpbt-undo.ps1");
            Assert.Equal(0, undoExitCode);
            Assert.Null(ReadHklm(sessionManagerPath, "DisableWpbtExecution"));
        }
        finally
        {
            if (before is not null) WriteHklm(sessionManagerPath, "DisableWpbtExecution", before, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(sessionManagerPath, "DisableWpbtExecution");
        }
    }

    [Fact]
    public async Task FolderDiscovery_run_then_undo_restores_FolderType_under_HKCU()
    {
        const string bagsPath = @"Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell";
        var before = ReadHkcu(bagsPath, "FolderType");

        try
        {
            var runner = CreateRunner();

            var runExitCode = await runner.RunEmbeddedScriptAsync("folderdiscovery.ps1");
            Assert.Equal(0, runExitCode);
            Assert.Equal("NotSpecified", ReadHkcu(bagsPath, "FolderType"));

            var undoExitCode = await runner.RunEmbeddedScriptAsync("folderdiscovery-undo.ps1");
            Assert.Equal(0, undoExitCode);
            Assert.Null(ReadHkcu(bagsPath, "FolderType"));
        }
        finally
        {
            if (before is not null) WriteHkcu(bagsPath, "FolderType", before, RegistryValueKind.String);
            else DeleteHkcuIfPresent(bagsPath, "FolderType");
        }
    }

    [Fact]
    public void StoreSearch_undo_script_contains_the_icacls_remove_deny_fix()
    {
        var resourceNames = typeof(AppEntry).Assembly.GetManifestResourceNames();
        var resourceName = resourceNames.Single(n => n.EndsWith("storesearch-undo.ps1", StringComparison.OrdinalIgnoreCase));

        using var stream = typeof(AppEntry).Assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
        var scriptText = reader.ReadToEnd();

        Assert.Contains("icacls", scriptText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/remove:d", scriptText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Everyone", scriptText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StoreSearch_icacls_deny_then_remove_d_round_trips_on_a_scratch_file()
    {
        var runner = CreateRunner();
        var scratchPath = Path.Combine(Path.GetTempPath(), $"akaritoolbox-storesearch-acl-test-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(scratchPath, "akaritoolbox storesearch ACL round-trip scratch file");

        try
        {
            // Baseline call — establishes icacls works against this file, no assertion needed.
            await runner.RunProcessCaptureOutputAsync("icacls", $"\"{scratchPath}\"");

            var denyExitCode = await runner.RunProcessAsync("icacls", $"\"{scratchPath}\" /deny Everyone:F");
            Assert.Equal(0, denyExitCode);

            var afterDenyOutput = await runner.RunProcessCaptureOutputAsync("icacls", $"\"{scratchPath}\"");
            Assert.Contains("Everyone", afterDenyOutput, StringComparison.Ordinal);
            Assert.Contains("(N)", afterDenyOutput, StringComparison.Ordinal);

            var removeExitCode = await runner.RunProcessAsync("icacls", $"\"{scratchPath}\" /remove:d Everyone");
            Assert.Equal(0, removeExitCode);

            var afterRemoveOutput = await runner.RunProcessCaptureOutputAsync("icacls", $"\"{scratchPath}\"");
            Assert.DoesNotContain("Everyone", afterRemoveOutput, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(scratchPath);
        }
    }
}
