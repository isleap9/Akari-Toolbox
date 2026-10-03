using System.IO;
using System.Management;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native port of the restore-point flow (Akari-Tool BtnHomeRestorePoint, which
/// itself wraps Ultimate "6 Windows" script 36): allow more than one point per
/// 24h, enable System Protection on the system drive, then create the point 
/// all through the WMI SystemRestore class. No scripts involved.
/// </summary>
public static class RestorePointActions
{
    private const string SystemRestorePath = @"\\.\ROOT\DEFAULT:SystemRestore";

    /// <summary>Create a system restore point. Throws with a readable message on failure.</summary>
    public static void CreateRestorePoint(string description = "AkariToolbox")
    {
        string drive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";

        // Allow more than one restore point per 24h (like the scripts' reg add).
        TweakAction.RegDword(
            @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore",
            "SystemRestorePointCreationFrequency", 0).Apply();

        try
        {
            using var cls = new ManagementClass(SystemRestorePath);
            using var enableParams = cls.GetMethodParameters("Enable");
            enableParams["DriveLetter"] = drive;
            cls.InvokeMethod("Enable", enableParams, null);

            using var createParams = cls.GetMethodParameters("CreateRestorePoint");
            createParams["Description"] = description;
            createParams["RestorePointType"] = (uint)12; // MODIFY_SETTINGS
            createParams["EventType"] = (uint)100;        // BEGIN_SYSTEM_CHANGE
            using ManagementBaseObject? result = cls.InvokeMethod("CreateRestorePoint", createParams, null);

            uint rc = result is not null && result["ReturnValue"] is not null
                ? Convert.ToUInt32(result["ReturnValue"])
                : 1;
            if (rc != 0)
                throw new InvalidOperationException($"SystemRestore returned code {rc}. System Protection may be turned off.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException(
                $"Could not create a restore point automatically. System Protection may be turned off. ({ex.Message})", ex);
        }
    }
}
