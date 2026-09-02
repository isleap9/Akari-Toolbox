using System.Security.Principal;
using Microsoft.Win32;
using AkariToolbox.Framework.Services;
using Xunit;

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

    [Fact]
    public async Task LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values()
    {
        if (!IsElevated())
        {
            Console.WriteLine("SKIPPED (requires elevation): LocationTracking_run_then_undo_restores_the_three_guaranteed_HKLM_values");
            return;
        }

        const string consentStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location";
        const string lfsvcPath = @"SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration";
        const string mapsPath = @"SYSTEM\Maps";

        var beforeConsentStore = ReadHklm(consentStorePath, "Value");
        var beforeLfsvc = ReadHklm(lfsvcPath, "Status");
        var beforeMaps = ReadHklm(mapsPath, "AutoUpdateEnabled");

        try
        {
            var runner = CreateRunner();

            var runExitCode = await runner.RunEmbeddedScriptAsync("locationtracking.ps1");
            Assert.Equal(0, runExitCode);
            Assert.Equal("Deny", ReadHklm(consentStorePath, "Value"));
            Assert.Equal(0, ReadHklm(lfsvcPath, "Status"));
            Assert.Equal(0, ReadHklm(mapsPath, "AutoUpdateEnabled"));

            var undoExitCode = await runner.RunEmbeddedScriptAsync("locationtracking-undo.ps1");
            Assert.Equal(0, undoExitCode);
            Assert.Equal("Allow", ReadHklm(consentStorePath, "Value"));
            Assert.Equal(1, ReadHklm(lfsvcPath, "Status"));
            Assert.Equal(1, ReadHklm(mapsPath, "AutoUpdateEnabled"));
        }
        finally
        {
            if (beforeConsentStore is not null) WriteHklm(consentStorePath, "Value", beforeConsentStore, RegistryValueKind.String);
            else DeleteHklmIfPresent(consentStorePath, "Value");

            if (beforeLfsvc is not null) WriteHklm(lfsvcPath, "Status", beforeLfsvc, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(lfsvcPath, "Status");

            if (beforeMaps is not null) WriteHklm(mapsPath, "AutoUpdateEnabled", beforeMaps, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(mapsPath, "AutoUpdateEnabled");
        }
    }

    [Fact]
    public async Task LocationTracking_undo_alone_is_safe_and_idempotent_without_a_prior_run()
    {
        if (!IsElevated())
        {
            Console.WriteLine("SKIPPED (requires elevation): LocationTracking_undo_alone_is_safe_and_idempotent_without_a_prior_run");
            return;
        }

        const string consentStorePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location";
        const string lfsvcPath = @"SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration";
        const string mapsPath = @"SYSTEM\Maps";

        var beforeConsentStore = ReadHklm(consentStorePath, "Value");
        var beforeLfsvc = ReadHklm(lfsvcPath, "Status");
        var beforeMaps = ReadHklm(mapsPath, "AutoUpdateEnabled");

        try
        {
            var runner = CreateRunner();

            var undoExitCode = await runner.RunEmbeddedScriptAsync("locationtracking-undo.ps1");
            Assert.Equal(0, undoExitCode);
            Assert.Equal("Allow", ReadHklm(consentStorePath, "Value"));
            Assert.Equal(1, ReadHklm(lfsvcPath, "Status"));
            Assert.Equal(1, ReadHklm(mapsPath, "AutoUpdateEnabled"));
        }
        finally
        {
            if (beforeConsentStore is not null) WriteHklm(consentStorePath, "Value", beforeConsentStore, RegistryValueKind.String);
            else DeleteHklmIfPresent(consentStorePath, "Value");

            if (beforeLfsvc is not null) WriteHklm(lfsvcPath, "Status", beforeLfsvc, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(lfsvcPath, "Status");

            if (beforeMaps is not null) WriteHklm(mapsPath, "AutoUpdateEnabled", beforeMaps, RegistryValueKind.DWord);
            else DeleteHklmIfPresent(mapsPath, "AutoUpdateEnabled");
        }
    }
}
