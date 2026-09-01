using Microsoft.Win32;

namespace AkariToolbox.App.Services;

/// <summary>
/// Manages the HKLM RunOnce entry that re-launches AkariToolbox in headless
/// <c>--defender-phase2</c> mode at the next interactive login. RunOnce fires once with
/// the logging-in admin's token and Windows removes the value before running it;
/// phase-2 also clears it explicitly (<see cref="ClearRunOnce"/>) as a belt-and-braces
/// guard so it can never run twice.
///
/// Ported from the sibling AkariTool repo's <c>DefenderPhase2Scheduler</c> — closes
/// CR-01/CR-03 (01-REVIEW.md) by replacing the generated <c>AkariDefenderCleanup.bat</c>
/// full of PowerRun.exe calls with a RunOnce entry that re-invokes the app itself, which
/// then runs the native SYSTEM-impersonation phase-2 work
/// (<see cref="TweakHandlers.DefenderTweakHandler.RunPhase2Native"/>).
///
/// Single-use token (T-01-17, 01-SECURITY.md security-audit finding): a bare
/// <c>--defender-phase2</c> invocation is a static, discoverable trigger — any process
/// already holding a full-Administrator token could otherwise launch it directly and
/// silently disable Defender/SmartScreen with no UI trace and no re-check of Tamper
/// Protection. <see cref="ScheduleRunOnce"/> generates a random GUID, persists it
/// separately from the RunOnce command itself, and embeds it in that command; phase-2
/// must present the matching token via <see cref="ConsumeToken"/> — which deletes the
/// persisted value on read whether it matches or not — before any system mutation runs.
/// A missing/invalid token makes the invocation a no-op.
///
/// Uses <c>Microsoft.Win32.Registry</c> directly (not <see cref="AkariToolbox.Framework.Services.IRegistryService"/>)
/// because this is an HKLM RunOnce write with no live/read-state semantics to share with
/// the tweak-handler registry pattern — a plain, self-contained static helper matches its
/// single-purpose scope.
/// </summary>
public static class DefenderPhase2Scheduler
{
    private const string RunOnceKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
    private const string ValueName = "AkariDefenderPhase2";

    private const string TokenKeyPath = @"SOFTWARE\AkariToolbox\DefenderPhase2";
    private const string TokenValueName = "Token";

    /// <summary>
    /// Generates a single-use token, persists it, and schedules
    /// "&lt;exe path&gt;" --defender-phase2 &lt;token&gt; to run once at next login (HKLM RunOnce).
    /// </summary>
    public static void ScheduleRunOnce()
    {
        var token = Guid.NewGuid().ToString("N");
        using (var tokenKey = Registry.LocalMachine.CreateSubKey(TokenKeyPath, writable: true))
        {
            tokenKey!.SetValue(TokenValueName, token, RegistryValueKind.String);
        }

        var exe = Environment.ProcessPath!; // full path to the running AkariToolbox.exe
        using var k = Registry.LocalMachine.CreateSubKey(RunOnceKey, writable: true);
        k!.SetValue(ValueName, $"\"{exe}\" --defender-phase2 {token}", RegistryValueKind.String);
    }

    /// <summary>
    /// Verifies <paramref name="providedToken"/> against the persisted token and consumes
    /// it (deletes the registry value) regardless of outcome, so it can never be presented
    /// twice. Returns <c>true</c> only when a token was scheduled and matches exactly.
    /// </summary>
    public static bool ConsumeToken(string? providedToken)
    {
        using var key = Registry.LocalMachine.OpenSubKey(TokenKeyPath, writable: true);
        var expected = key?.GetValue(TokenValueName) as string;
        key?.DeleteValue(TokenValueName, throwOnMissingValue: false);

        return !string.IsNullOrEmpty(expected)
            && !string.IsNullOrEmpty(providedToken)
            && string.Equals(expected, providedToken, StringComparison.Ordinal);
    }

    /// <summary>Removes the RunOnce entry (called by phase-2 itself after it runs).</summary>
    public static void ClearRunOnce()
    {
        using var k = Registry.LocalMachine.OpenSubKey(RunOnceKey, writable: true);
        k?.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    /// <summary>True when the phase-2 RunOnce entry is currently scheduled.</summary>
    public static bool IsScheduled()
    {
        using var k = Registry.LocalMachine.OpenSubKey(RunOnceKey, writable: false);
        return k?.GetValue(ValueName) != null;
    }
}
