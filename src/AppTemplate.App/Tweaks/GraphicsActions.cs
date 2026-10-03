using System.IO;
using Microsoft.Win32;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native C# ports of the Ultimate "5 Graphics" scripts: 5 Nvidia Settings, 6 Amd
/// Settings, 8 Hdcp, 9 P0 State, 10 Msi Mode, 11 DirectX, 13 Resolution Refresh Rate
/// and 14 Hags Windowed. Every registry write, URL, target path and process start
/// mirrors its script 1:1 (downloads happen fresh on every run, like the scripts) 
/// but nothing .ps1 is bundled or invoked: it is all registry APIs, HttpClient and
/// process starts. The two embedded NIP profiles live in GraphicsActions.Nip.cs,
/// copied verbatim from script 5.
/// </summary>
public static partial class GraphicsActions
{
    /// <summary>The Ultimate release the scripts download their files from.</summary>
    private const string FilesUrl =
        "https://github.com/FR33THYFR33THY/Ultimate/releases/download/Files/";

    private const string NvidiaControlPanel =
        "shell:appsFolder\\NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj!NVIDIACorp.NVIDIAControlPanel";

    /// <summary>The driver's NVTweak key  written by both script 5 branches.</summary>
    private const string NvTweak =
        @"HKLM\System\ControlSet001\Services\nvlddmkm\Parameters\Global\NVTweak";

    /// <summary>Per-device MSI interrupt path (script 10).</summary>
    private const string MsiProperties =
        @"Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";

    //  Registry write helpers (same shape as GamingActions) 

    private static void Dword(string path, string name, int value) =>
        TweakAction.RegDword(path, name, value).Apply();

    private static void Sz(string path, string name, string value) =>
        TweakAction.RegString(path, name, value).Apply();

    private static void Bin(string path, string name, string hex) =>
        TweakAction.RegBinaryHex(path, name, hex).Apply();

    private static void DelValue(string path, string name) =>
        TweakAction.RegDeleteValue(path, name).Apply();

    private static void DelKey(string path) =>
        TweakAction.RegDeleteKey(path).Apply();

    /// <summary>reg add  /f with no values  recreates an emptied key (script 6).</summary>
    private static void CreateKey(string path)
    {
        var (baseKey, sub) = RegistryPath.Resolve(path);
        using RegistryKey _ = baseKey.CreateSubKey(sub);
    }

    // 
    // 5 Nvidia Settings  On / Default
    // 

    public static void NvidiaSettings(bool on)
    {
        // Common part (runs before the script's menu): fetch + install the NVIDIA
        // Control Panel appx silently, then drop the desktop shortcut.
        string appx = Path.Combine(NativeOps.SystemRootTemp, "nvp.appx");
        NativeOps.Download(FilesUrl + "nvp.appx", appx);
        InstallAppx(appx);
        NativeOps.CreateShortcut(NativeOps.DesktopShortcut("NVIDIA Control Panel"),
            NvidiaControlPanel, "shell:appsFolder");

        // Unblock the Drs profile folder (Get-ChildItem -Recurse | Unblock-File).
        NativeOps.UnblockFolder(@"C:\ProgramData\NVIDIA Corporation\Drs");

        if (on)
        {
            Dword(NvTweak, "NvCplPhysxAuto", 0);     // physx  GPU
            Dword(NvTweak, "NvDevToolsVisible", 1);  // developer settings
            // GPU performance counters for all users (every non-Configuration class key).
            foreach (string key in NativeOps.DisplayClassSubkeys())
                Dword(key, "RmProfilingAdminOnly", 0);
            Dword(NvTweak, "RmProfilingAdminOnly", 0);
            Dword(@"HKCU\Software\NVIDIA Corporation\NvTray", "StartOnLogin", 0); // no tray icon
            // Legacy sharpen off  the script writes all three keys.
            Dword(@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS", "EnableGR535", 0);
            Dword(@"HKLM\SYSTEM\ControlSet001\Services\nvlddmkm\Parameters\FTS", "EnableGR535", 0);
            Dword(@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters\FTS", "EnableGR535", 0);
        }
        else
        {
            DelValue(NvTweak, "NvCplPhysxAuto");
            DelValue(NvTweak, "NvDevToolsVisible");
            foreach (string key in NativeOps.DisplayClassSubkeys())
                DelValue(key, "RmProfilingAdminOnly");
            DelValue(NvTweak, "RmProfilingAdminOnly");
            DelKey(@"HKCU\Software\NVIDIA Corporation\NvTray"); // whole key, like reg delete  /f
            Dword(@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\FTS", "EnableGR535", 1);
            Dword(@"HKLM\SYSTEM\ControlSet001\Services\nvlddmkm\Parameters\FTS", "EnableGR535", 1);
            Dword(@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm\Parameters\FTS", "EnableGR535", 1);
        }

        // Both branches: fresh inspector download, shortcuts, write the NIP profile,
        // silent import, then open the NVIDIA Control Panel.
        string inspectorDir = Path.Combine(NativeOps.ProgramFilesX86, "Nvidia Profile Inspector");
        string inspector = Path.Combine(inspectorDir, "Nvidia Profile Inspector.exe");
        NativeOps.Download(FilesUrl + "inspector.exe", inspector);
        NativeOps.CreateShortcut(NativeOps.DesktopShortcut("Nvidia Profile Inspector"), inspector, inspectorDir);
        NativeOps.CreateShortcut(NativeOps.StartMenuShortcut("Nvidia Profile Inspector"), inspector, inspectorDir);

        string nip = Path.Combine(NativeOps.SystemRootTemp, "inspector.nip");
        File.WriteAllText(nip, on ? NvidiaNipOn : NvidiaNipDefault);
        TweakAction.Run(inspector, $"-silentImport -silent \"{nip}\"").Apply();

        NativeOps.Launch(NvidiaControlPanel);
    }

    /// <summary>
    /// Live state for the NVIDIA Settings toggle. The NVTweak values the script writes are
    /// the marker: the On branch sets NvCplPhysxAuto=0 and NvDevToolsVisible=1, the Default
    /// branch deletes them again  so their presence means the optimized profile is applied.
    /// </summary>
    public static bool ReadNvidiaSettings() =>
        RegRead.Dword(NvTweak, "NvCplPhysxAuto") is 0 &&
        RegRead.Dword(NvTweak, "NvDevToolsVisible") is 1;

    //  Add-AppxPackage, natively 

    private const string NvidiaPanelFamily = "NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj";

    /// <summary>
    /// Silent <c>Add-AppxPackage</c> via <see cref="PackageManager"/>. Like the script's
    /// <c>-ErrorAction SilentlyContinue</c>, an already-installed package is not an error;
    /// any other deployment failure is surfaced to the snackbar instead of swallowed.
    /// </summary>
    private static void InstallAppx(string path)
    {
        var packageManager = new PackageManager();
        try
        {
            WaitWinRt(packageManager.AddPackageAsync(new Uri(path), Array.Empty<Uri>(), DeploymentOptions.None));
        }
        catch (Exception ex)
        {
            if (!IsPackageInstalled(packageManager, NvidiaPanelFamily))
                throw new InvalidOperationException(
                    $"The NVIDIA Control Panel package could not be installed: {ex.Message}", ex);
        }
    }

    private static bool IsPackageInstalled(PackageManager packageManager, string familyName)
    {
        foreach (var package in packageManager.FindPackagesForUser(string.Empty, familyName))
            return true;
        return false;
    }

    /// <summary>Block on a WinRT async operation (works from any background thread).</summary>
    private static void WaitWinRt(IAsyncInfo operation)
    {
        while (operation.Status == AsyncStatus.Started)
            Thread.Sleep(100);
        if (operation.Status is AsyncStatus.Error or AsyncStatus.Canceled)
        {
            Exception? error = operation.ErrorCode;
            throw error ?? new InvalidOperationException("The package operation was cancelled.");
        }
    }

    // 
    // 6 Amd Settings  On / Default
    // 

    /// <summary>
    /// Live state for the AMD Settings toggle. The CN values the script writes are
    /// the marker: the On branch sets AutoUpdate=0 and WizardProfile=PROFILE_CUSTOM,
    /// the Default branch deletes them again  so their presence means the optimized
    /// profile is applied.
    /// </summary>
    public static bool ReadAmdSettings() =>
        RegRead.Dword(@"HKCU\Software\AMD\CN", "AutoUpdate") is 0 &&
        RegRead.String(@"HKCU\Software\AMD\CN", "WizardProfile") == "PROFILE_CUSTOM";

    public static void AmdSettings(bool on)
    {
        if (on)
        {
            // Open Adrenalin, let it settle, kill it, wait  "so settings stick".
            // Skipped when the exe is absent (no AMD driver on this machine).
            string radeon = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "AMD", "CNext", "CNext", "RadeonSoftware.exe");
            if (File.Exists(radeon))
            {
                NativeOps.Launch(radeon);
                Thread.Sleep(TimeSpan.FromSeconds(30));
                NativeOps.StopProcess("RadeonSoftware");
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }
        }

        const string cn = @"HKCU\Software\AMD\CN";
        const string aim = @"HKCU\Software\AMD\AIM";
        const string dvr = @"HKCU\Software\AMD\DVR";
        // Script 6 writes every driver-class key under ControlSet001.
        const string cs = "ControlSet001";

        if (on)
        {
            Dword(cn, "AutoUpdate", 0);                 // manual check for updates
            Dword(aim, "LaunchBugTool", 0);             // disable issue detection
            Dword(dvr, "HotkeysDisabled", 1);           // disable hotkeys
            Sz(cn, "SystemTray", "false");              // no system tray menu
            Sz(dvr, "ShowRSOverlay", "false");          // no in-game overlay
            Sz(cn, "RSXBrowserUnavailable", "true");    // no web browser
            Sz(cn, "AllowWebContent", "false");         // no advertisements
            Sz(cn, "CN_Hide_Toast_Notification", "true");
            Sz(cn, "AnimationEffect", "false");
            Sz(cn, "WizardProfile", "PROFILE_CUSTOM");  // graphics profile: custom

            foreach (string key in NativeOps.DisplayAdapterNumberedKeys(cs))
                Dword(key, "KMD_DeLagEnabled", 1);      // radeon anti-lag

            foreach (string umd in NativeOps.DisplayClassDescendantKeys("UMD", cs))
            {
                Bin(umd, "VSyncControl", "3000");         // wait for v-sync: always off
                Bin(umd, "TFQ", "3200");                  // texture filtering: performance
                Bin(umd, "Tessellation", "3100");         // override application settings
                Bin(umd, "Tessellation_OPTION", "3200");  // max tessellation: off
            }

            Sz(@"HKCU\Software\AMD\CN\CustomResolutions", "EulaAccepted", "true");
            Sz(@"HKCU\Software\AMD\CN\DisplayOverride", "EulaAccepted", "true");

            foreach (string power in NativeOps.DisplayClassDescendantKeys("power_v1", cs))
                Bin(power, "abmlevel", "00000000");      // vari-bright: maximize brightness

            foreach (string key in NativeOps.DisplayAdapterNumberedKeys(cs))
            {
                Bin(key, "IsAutoDefault", "00000000");      // manual tuning: custom
                Bin(key, "IsComponentControl", "0f000000"); // gpu/fan/vram/power tuning
            }

            // Notifications  remove: wipe the key, recreate it empty, mark as seen.
            DelKey(cn + @"\Notification");
            CreateKey(cn + @"\Notification");
            Dword(cn + @"\FreeSync", "AlreadyNotified", 1);
            Dword(cn + @"\OverlayNotification", "AlreadyNotified", 1);
            Dword(cn + @"\VirtualSuperResolution", "AlreadyNotified", 1);
        }
        else
        {
            DelValue(cn, "AutoUpdate");
            Dword(aim, "LaunchBugTool", 1);
            DelValue(dvr, "HotkeysDisabled");
            DelValue(cn, "SystemTray");
            DelValue(dvr, "ShowRSOverlay");
            DelValue(cn, "RSXBrowserUnavailable");
            DelValue(cn, "AllowWebContent");
            DelValue(cn, "CN_Hide_Toast_Notification");
            DelValue(cn, "AnimationEffect");
            DelValue(cn, "WizardProfile");

            foreach (string key in NativeOps.DisplayAdapterNumberedKeys(cs))
                Dword(key, "KMD_DeLagEnabled", 0);

            foreach (string umd in NativeOps.DisplayClassDescendantKeys("UMD", cs))
            {
                Bin(umd, "VSyncControl", "31000000");
                DelValue(umd, "TFQ");
                Bin(umd, "Tessellation", "360034000000");
                Bin(umd, "Tessellation_OPTION", "30000000");
            }

            DelKey(@"HKCU\Software\AMD\CN\CustomResolutions");
            DelKey(@"HKCU\Software\AMD\CN\DisplayOverride");

            foreach (string power in NativeOps.DisplayClassDescendantKeys("power_v1", cs))
                DelValue(power, "abmlevel");

            foreach (string key in NativeOps.DisplayAdapterNumberedKeys(cs))
            {
                Dword(key, "IsAutoDefault", 1);          // yes  a DWORD here, per the script
                Bin(key, "IsComponentControl", "00000000");
            }

            DelKey(cn + @"\Notification");
            DelKey(cn + @"\FreeSync");
            DelKey(cn + @"\OverlayNotification");
            DelKey(cn + @"\VirtualSuperResolution");
        }
    }

    // 
    // 8 Hdcp  Off / Default  (RmHdcpKeyglobZero on every non-Configuration key)
    // 

    public static void Hdcp(bool off)
    {
        int value = off ? 1 : 0;
        foreach (string key in NativeOps.DisplayClassSubkeys())
            Dword(key, "RMHdcpKeyglobZero", value);
    }

    /// <summary>Live state for the HDCP toggle: any adapter with RMHdcpKeyglobZero=1 (HDCP off).</summary>
    public static bool ReadHdcp()
    {
        foreach (string key in NativeOps.DisplayClassSubkeys())
            if (RegRead.Dword(key, "RMHdcpKeyglobZero") is 1)
                return true;
        return false;
    }

    // 
    // 9 P0 State  On / Default  (DisableDynamicPstate, always max boost clock)
    // 

    public static void P0State(bool on)
    {
        foreach (string key in NativeOps.DisplayClassSubkeys())
            Dword(key, "DisableDynamicPstate", on ? 1 : 0);
    }

    /// <summary>Live state for the P0 toggle: any adapter with DisableDynamicPstate=1.</summary>
    public static bool ReadP0State()
    {
        foreach (string key in NativeOps.DisplayClassSubkeys())
            if (RegRead.Dword(key, "DisableDynamicPstate") is 1)
                return true;
        return false;
    }

    // 
    // 10 Msi Mode  On / Off  (MSISupported on every display device instance)
    // 

    public static void MsiMode(bool on) => NativeOps.EnableGpuMsiMode(on ? 1 : 0);

    /// <summary>Live state for the MSI toggle: any display device with MSISupported=1.</summary>
    public static bool ReadMsiMode()
    {
        foreach (string id in NativeOps.DisplayDeviceInstanceIds())
            if (RegRead.Dword($@"HKLM\SYSTEM\ControlSet001\Enum\{id}\{MsiProperties}", "MSISupported") is 1)
                return true;
        return false;
    }

    // 
    // 7 Intel Settings  On / Default  (3DKeys vsync-off + low-latency per adapter)
    // 

    private const string IntelDisplayClass =
        @"SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    private const string IntelGpuPrefsValue = "SwapEffectUpgradeEnable=1;VRROptimizeEnable=0;";

    public static void IntelSettings(bool on)
    {
        if (on)
        {
            foreach (string adapter in IntelAdapterPaths())
            {
                Dword(adapter + @"\3DKeys", "Global_AsyncFlipMode", 2); // vsync off
                Dword(adapter + @"\3DKeys", "Global_LowLatency", 1);    // low latency on
            }
            TweakAction.RegString(
                @"HKCU\Software\Microsoft\DirectX\UserGpuPreferences",
                "DirectXUserGlobalSettings", IntelGpuPrefsValue).Apply();
        }
        else
        {
            foreach (string adapter in IntelAdapterPaths())
                TweakAction.RegDeleteKey(adapter + @"\3DKeys").Apply();
        }
    }

    /// <summary>Live state for the Intel toggle: any 4-digit adapter with a
    /// 3DKeys child carrying the script's vsync-off marker.</summary>
    public static bool ReadIntelSettings()
    {
        foreach (string adapter in IntelAdapterPaths())
            if (RegRead.Dword(adapter + @"\3DKeys", "Global_AsyncFlipMode") is 2)
                return true;
        return false;
    }

    /// <summary>HKLM display-class adapter paths named 00000999 (the script's filter).</summary>
    private static IEnumerable<string> IntelAdapterPaths()
    {
        using Microsoft.Win32.RegistryKey? cls = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(IntelDisplayClass);
        if (cls is null)
            yield break;
        foreach (string name in cls.GetSubKeyNames())
        {
            if (name.Length == 4 && name.All(char.IsDigit))
                yield return $@"HKLM\{IntelDisplayClass}\{name}";
        }
    }

    // 
    // 11 DirectX  7zip + DirectX runtime, installed silently
    // 

    public static void InstallDirectX()
    {
        string temp = NativeOps.SystemRootTemp;

        // 7-Zip, silently, then its context-menu config and a tidier start menu.
        string sevenZipInstaller = Path.Combine(temp, "7zip.exe");
        NativeOps.Download(FilesUrl + "7zip.exe", sevenZipInstaller);
        TweakAction.Run(sevenZipInstaller, "/S").Apply();

        Dword(@"HKCU\Software\7-Zip\Options", "ContextMenu", 259);
        Dword(@"HKCU\Software\7-Zip\Options", "CascadedMenu", 0);

        string programs = NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs");
        NativeOps.MoveFile(
            Path.Combine(programs, "7-Zip", "7-Zip File Manager.lnk"),
            Path.Combine(programs, "7-Zip File Manager.lnk"));
        NativeOps.DeleteDirectory(Path.Combine(programs, "7-Zip"));

        // DirectX runtime: download, unpack with the 7-Zip just installed, DXSETUP silent.
        string directx = Path.Combine(temp, "directx.exe");
        NativeOps.Download(FilesUrl + "directx.exe", directx);
        string unpacked = Path.Combine(temp, "directx");
        NativeOps.SevenZipExtract(directx, unpacked);
        TweakAction.Run(Path.Combine(unpacked, "DXSETUP.exe"), "/silent").Apply();
    }

    // 
    // 12 C++  every vcredist 20052022, x86 + x64, silent
    // 

    public static void InstallCppRuntimes()
    {
        string temp = NativeOps.SystemRootTemp;

        foreach (string file in new[]
        {
            "vcredist2005_x86.exe", "vcredist2005_x64.exe",
            "vcredist2008_x86.exe", "vcredist2008_x64.exe",
            "vcredist2010_x86.exe", "vcredist2010_x64.exe",
            "vcredist2012_x86.exe", "vcredist2012_x64.exe",
            "vcredist2013_x86.exe", "vcredist2013_x64.exe",
            "vcredist2015_2017_2019_2022_x86.exe", "vcredist2015_2017_2019_2022_x64.exe",
        })
        {
            NativeOps.Download(FilesUrl + file, Path.Combine(temp, file));
        }

        foreach (string file in new[] { "vcredist2005_x86.exe", "vcredist2005_x64.exe" })
            TweakAction.Run(Path.Combine(temp, file),
                "/Q /C:\"msiexec /i vcredist.msi /qn /norestart\"").Apply();
        foreach (string file in new[] { "vcredist2008_x86.exe", "vcredist2008_x64.exe" })
            TweakAction.Run(Path.Combine(temp, file), "/q").Apply();
        foreach (string file in new[]
        {
            "vcredist2010_x86.exe", "vcredist2010_x64.exe",
            "vcredist2012_x86.exe", "vcredist2012_x64.exe",
            "vcredist2013_x86.exe", "vcredist2013_x64.exe",
            "vcredist2015_2017_2019_2022_x86.exe", "vcredist2015_2017_2019_2022_x64.exe",
        })
            TweakAction.Run(Path.Combine(temp, file), "/quiet /norestart").Apply();
    }

    // 
    // 13 / 14  open the matching Windows Settings page
    // 

    public static void OpenResolutionRefreshRate() =>
        TweakAction.Open("ms-settings:display").Apply();

    public static void OpenHagsWindowed() =>
        TweakAction.Open("ms-settings:display-advancedgraphics").Apply();
}
