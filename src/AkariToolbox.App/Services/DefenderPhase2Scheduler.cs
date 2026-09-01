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
/// Uses <c>Microsoft.Win32.Registry</c> directly (not <see cref="AkariToolbox.Framework.Services.IRegistryService"/>)
/// because this is an HKLM RunOnce write with no live/read-state semantics to share with
/// the tweak-handler registry pattern — a plain, self-contained static helper matches its
/// single-purpose scope.
/// </summary>
public static class DefenderPhase2Scheduler
{
    private const string RunOnceKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
    private const string ValueName = "AkariDefenderPhase2";

    /// <summary>Schedules "&lt;exe path&gt;" --defender-phase2 to run once at next login (HKLM RunOnce).</summary>
    public static void ScheduleRunOnce()
    {
        var exe = Environment.ProcessPath!; // full path to the running AkariToolbox.exe
        using var k = Registry.LocalMachine.CreateSubKey(RunOnceKey, writable: true);
        k!.SetValue(ValueName, $"\"{exe}\" --defender-phase2", RegistryValueKind.String);
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
