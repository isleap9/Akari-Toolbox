using System.IO;
using System.Management;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native port of Ultimate "3 Setup" (minus nothing  Keys, Activation and
/// Convert are plain URLs / settings URIs / clipboard). Toggles carry a live
/// read; Memory Compression is a 3-way combobox like the script (Off/Enable/Check).
/// </summary>
public static class SetupActions
{
    public static readonly string[] MemCompOptions = { "Off (Recommended)", "Enable", "Check" };

    //  BitLocker (script 1) 

    public static void SetBitLockerOff()
    {
        NativeOps.DisableBitLockerAllDrives();
        NativeOps.Launch("control.exe", "/name microsoft.bitlockerdriveencryption");
    }

    public static void OpenBitLockerPanel() =>
        NativeOps.Launch("control.exe", "/name microsoft.bitlockerdriveencryption");

    /// <summary>True when any volume reports protection on or is not fully decrypted.</summary>
    public static bool ReadBitLockerOn()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"\\.\ROOT\CIMV2\Security\MicrosoftVolumeEncryption",
                "SELECT DriveLetter, ProtectionStatus, ConversionStatus FROM Win32_EncryptibleVolume");
            foreach (ManagementObject vol in searcher.Get().Cast<ManagementObject>())
            {
                uint protection = Convert.ToUInt32(vol["ProtectionStatus"] ?? 2u);
                uint conversion = Convert.ToUInt32(vol["ConversionStatus"] ?? 0u);
                if (protection == 1 || conversion != 0)
                    return true;
            }
        }
        catch { }
        return false;
    }

    //  Memory Compression (script 2; MMAgent has no Win32 API  inline cmdlet) 

    public static void SetMemCompOff() => NativeOps.PowerShellCommand("Disable-MMAgent -mc");

    public static void SetMemCompOn() => NativeOps.PowerShellCommand("Enable-MMAgent -mc");

    public static bool ReadMemCompEnabled()
    {
        string output = NativeOps.RunToolCapture("powershell.exe",
            "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"(Get-MMAgent).MemoryCompression\"");
        return output.Trim().Equals("True", StringComparison.OrdinalIgnoreCase);
    }

    public static string ReadMemCompStatus() =>
        NativeOps.RunToolCapture("powershell.exe",
            "-NoProfile -NonInteractive -WindowStyle Hidden -Command \"Get-MMAgent | Out-String\"");

    //  Background Apps (script 9) 

    public static void SetBackgroundAppsOff()
    {
        TweakAction.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2).Apply();
        TweakAction.RegDword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "BackgroundAppGlobalToggle", 0).Apply();
        TweakAction.RegDword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled", 1).Apply();
    }

    public static void SetBackgroundAppsDefault()
    {
        TweakAction.RegDeleteValue(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground").Apply();
        TweakAction.RegDeleteValue(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "BackgroundAppGlobalToggle").Apply();
        TweakAction.RegDeleteValue(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications", "GlobalUserDisabled").Apply();
    }

    public static bool ReadBackgroundAppsOff() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground") == 2;

    //  Edge Settings (script 10) 

    private const string EdgeBho = @"{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}";

    public static void OptimizeEdge()
    {
        TweakAction.RegString(@"HKLM\SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallForcelist", "1",
            "odfafepnkmbhccpbejgmiehpchacaeak;https://edge.microsoft.com/extensionwebstorebase/v1/crx").Apply();
        TweakAction.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "HardwareAccelerationModeEnabled", 0).Apply();
        TweakAction.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "BackgroundModeEnabled", 0).Apply();
        TweakAction.RegDword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 0).Apply();

        NativeOps.RemoveSubkeysByDefaultValueLike(
            @"HKLM\Software\Microsoft\Active Setup\Installed Components", "Edge");
        NativeOps.RemoveValuesLike(
            @"HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce", "msedge");
        NativeOps.StopAndDeleteServicesMatching("Edge");
        NativeOps.DeleteScheduledTasksMatching("Edge");

        TweakAction.RegDeleteKey($@"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{EdgeBho}").Apply();
        TweakAction.RegDeleteKey($@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{EdgeBho}").Apply();
    }

    /// <summary>Blocking: policy reset, Edge settings reset via its own installer.</summary>
    public static void RestoreEdgeDefault()
    {
        TweakAction.RegDeleteKey(@"HKLM\SOFTWARE\Policies\Microsoft\Edge").Apply();

        NativeOps.StopProcess("msedge");
        System.Threading.Thread.Sleep(2000);
        NativeOps.Launch("msedge.exe", "--restore-last-session --disable-extensions");
        System.Threading.Thread.Sleep(2000);
        NativeOps.StopProcess("msedge");

        string edge = Path.Combine(NativeOps.SystemRootTemp, "edge.exe");
        NativeOps.Download(
            "https://github.com/FR33THYFR33THY/Ultimate/releases/download/Files/edge.exe", edge);
        NativeOps.Launch(edge);
    }

    public static bool ReadEdgeOptimized() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled") == 0;

    //  Store Settings (script 11) 

    public static void OptimizeStore()
    {
        TweakAction.RegDword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate", "AutoDownload", 2).Apply();
        if (!WindowsActions.IsAppxInstalled("Microsoft.WindowsStore"))
            throw new InvalidOperationException("Microsoft Store isn't installed  reinstall it from the 6  Windows bloatware card first.");
        TweakAction.Open("ms-windows-store:settings").Apply();
    }

    public static void RestoreStoreDefault()
    {
        TweakAction.RegDeleteKey(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore").Apply();
        if (!WindowsActions.IsAppxInstalled("Microsoft.WindowsStore"))
            throw new InvalidOperationException("Microsoft Store isn't installed  reinstall it from the 6  Windows bloatware card first.");
        TweakAction.Open("ms-windows-store:settings").Apply();
    }

    public static bool ReadStoreOptimized() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate", "AutoDownload") == 2;

    //  One-shot openers 

    public static void OpenDateLanguage() => NativeOps.Launch("ms-settings:dateandtime");

    public static void OpenStartupApps() => NativeOps.Launch("ms-settings:startupapps");

    public static void OpenStartupTaskManager() => NativeOps.Launch("taskmgr.exe", "/0 /startup");

    /// <summary>Pause updates for 1 year (6 UX settings values), then open
    /// Windows Update settings  Ultimate "3 Setup" script 12, 1:1.</summary>
    public static void PauseUpdates()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        string pause = DateTime.UtcNow.AddDays(365).ToString("yyyy-MM-ddTHH:mm:ssZ");
        const string ux = @"HKLM\SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
        TweakAction.RegString(ux, "PauseUpdatesExpiryTime", pause).Apply();
        TweakAction.RegString(ux, "PauseFeatureUpdatesEndTime", pause).Apply();
        TweakAction.RegString(ux, "PauseFeatureUpdatesStartTime", today).Apply();
        TweakAction.RegString(ux, "PauseQualityUpdatesEndTime", pause).Apply();
        TweakAction.RegString(ux, "PauseQualityUpdatesStartTime", today).Apply();
        TweakAction.RegString(ux, "PauseUpdatesStartTime", today).Apply();
        NativeOps.Launch("ms-settings:windowsupdate");
    }

    public static void OpenActivationKeys() =>
        NativeOps.Launch("https://github.com/massgravel/Microsoft-Activation-Scripts");

    public static void OpenActivation() => NativeOps.Launch("ms-settings:activation");

    public static void ConvertHomeToPro()
    {
        NativeOps.SetClipboard("VK7JG-NPHTM-C97JM-9MPGT-3V66T");
        NativeOps.Launch("ms-settings:activation");
        NativeOps.Launch(
            NativeOps.Expand(@"%windir%\System32\SystemSettingsAdminFlows.exe"), "EnterProductKey");
    }
}
