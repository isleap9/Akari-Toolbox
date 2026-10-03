using System.IO;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native port of Ultimate "2 Refresh" (scripts 17) following the Akari-Tool
/// Refresh tab: recovery / account / reinstall / unattend / driver-block toggle /
/// network-driver search / restart-to-BIOS. All settings URIs, winget, registry
/// and reboot APIs  no scripts.
/// </summary>
public static class RefreshActions
{
    public static void OpenRecovery() => NativeOps.Launch("ms-settings:recovery");

    public static void OpenLocalUsers() => NativeOps.Launch("netplwiz");

    public static void OpenUnattendGenerator() => NativeOps.Launch("https://schneegans.de/windows/unattend-generator/");

    /// <summary>Blocking: winget the Win10 media creation tool and launch it.</summary>
    public static void InstallAndRunMct10()
    {
        const string productCode = "Microsoft.MediaCreationTool.Windows10_Microsoft.Winget.Source_8wekyb3d8bbwe";
        NativeOps.WingetInstall("Microsoft.MediaCreationTool.Windows10", productCode);

        string exe = NativeOps.PackagePath(productCode, "MediaCreationTool10.exe");
        if (!File.Exists(exe))
            throw new FileNotFoundException($"Media Creation Tool installed but not found at {exe}.");
        NativeOps.Launch(exe);
    }

    /// <summary>Blocking: winget the Win11 media creation tool and launch it.</summary>
    public static void InstallAndRunMct11()
    {
        const string productCode = "Microsoft.MediaCreationTool_Microsoft.Winget.Source_8wekyb3d8bbwe";
        NativeOps.WingetInstall("Microsoft.MediaCreationTool", productCode);

        string exe = NativeOps.PackagePath(productCode, "MediaCreationTool.exe");
        if (!File.Exists(exe))
            throw new FileNotFoundException($"Media Creation Tool installed but not found at {exe}.");
        NativeOps.Launch(exe);
    }

    public static void SearchNetworkDriver()
    {
        string board = NativeOps.BaseBoardProduct();
        NativeOps.WebSearch(string.IsNullOrWhiteSpace(board) ? "network driver" : $"{board} network driver");
    }

    /// <summary>Ask, then restart straight into firmware.</summary>
    public static void RestartToBios()
    {
        if (AkariToolbox.Helpers.NativeMessageBox.ConfirmWarning("Restart to BIOS now?", "Restart"))
            NativeOps.RestartToBios();
    }

    //  Driver-updates block (Ultimate "2 Refresh" script 5, direct branch) 

    private static readonly (string Key, string Name, int Value)[] DriverBlockValues =
    {
        (@"HKLM\Software\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork", 1),
        (@"HKLM\Software\Policies\Microsoft\Windows\DeviceInstall\Settings", "DisableSendGenericDriverNotFoundToWER", 1),
        (@"HKLM\Software\Policies\Microsoft\Windows\DeviceInstall\Settings", "DisableSendRequestAdditionalSoftwareToWER", 1),
        (@"HKLM\Software\Policies\Microsoft\Windows\DriverSearching", "SearchOrderConfig", 0),
        (@"HKLM\Software\Policies\Microsoft\Windows\WindowsUpdate", "SetAllowOptionalContent", 0),
        (@"HKLM\Software\Policies\Microsoft\Windows\WindowsUpdate", "AllowTemporaryEnterpriseFeatureControl", 0),
        (@"HKLM\Software\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate", 1),
        (@"HKLM\Software\Policies\Microsoft\Windows\WindowsUpdate\AU", "IncludeRecommendedUpdates", 0),
        (@"HKLM\Software\Policies\Microsoft\Windows\WindowsUpdate\AU", "EnableFeaturedSoftware", 0),
    };

    public static void BlockDriverUpdates()
    {
        foreach (var (key, name, value) in DriverBlockValues)
            TweakAction.RegDword(key, name, value).Apply();
    }

    public static void UnblockDriverUpdates()
    {
        foreach (var (key, name, _) in DriverBlockValues)
            TweakAction.RegDeleteValue(key, name).Apply();
    }

    /// <summary>Blocked when the two discriminating values read back as set.</summary>
    public static bool ReadDriverUpdatesBlocked() =>
        RegRead.Dword(@"HKLM\Software\Policies\Microsoft\Windows\DriverSearching", "SearchOrderConfig") == 0 &&
        RegRead.Dword(@"HKLM\Software\Policies\Microsoft\Windows\WindowsUpdate", "ExcludeWUDriversInQualityUpdate") == 1;
}
