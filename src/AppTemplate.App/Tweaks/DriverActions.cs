using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native port of the Ultimate "5 Graphics" driver flows (scripts 14):
/// DDU clean (safe-boot RunOnce back into this exe, never a script),
/// latest-driver installs for NVIDIA / AMD / Intel, and the debloated
/// NVIDIA install. The settings half reuses <see cref="GraphicsActions"/>.
/// </summary>
public static class DriverActions
{
    private const string FilesUrl =
        "https://github.com/FR33THYFR33THY/Ultimate/releases/download/Files/";

    private const string IntelDriverSearch =
        "https://www.intel.com/content/www/us/en/search.html#sortCriteria=%40lastmodifieddt%20descending" +
        "&f-operatingsystem_en=Windows%2011%20Family*&f-downloadtype=Drivers&cf-tabfilter=Downloads&cf-downloadsppth=Graphics";

    private static string DduDir => Path.Combine(NativeOps.SystemRootTemp, "ddu");
    private static string DduExe => Path.Combine(DduDir, "Display Driver Uninstaller.exe");

    //  7-Zip (needed to unpack DDU and the NVIDIA driver) 

    public static void EnsureSevenZip()
    {
        if (File.Exists(NativeOps.SevenZipExe))
            return;

        string installer = Path.Combine(NativeOps.SystemRootTemp, "7zip.exe");
        NativeOps.Download(FilesUrl + "7zip.exe", installer);
        TweakAction.Run(installer, "/S").Apply();

        if (!File.Exists(NativeOps.SevenZipExe))
            throw new FileNotFoundException("7-Zip installed but 7z.exe was not found.");
    }

    //  1 Driver Clean: DDU Auto / Manual 

    /// <summary>Stage DDU + config, arm RunOnce back into this exe, enter
    /// safe boot and reboot. The finish half runs via --ddu-auto/--ddu-manual.</summary>
    public static void PrepareDdu(bool auto)
    {
        EnsureSevenZip();

        string dduSelfExtract = Path.Combine(NativeOps.SystemRootTemp, "ddu.exe");
        NativeOps.Download(FilesUrl + "ddu.exe", dduSelfExtract);
        NativeOps.SevenZipExtract(dduSelfExtract, DduDir);

        string settingsDir = Path.Combine(DduDir, "Settings");
        Directory.CreateDirectory(settingsDir);
        NativeOps.WriteAllText(
            Path.Combine(settingsDir, "Settings.xml"),
            NativeOps.LoadGraphicsData("DduSettings.xml"));
        NativeOps.SetReadOnly(Path.Combine(settingsDir, "Settings.xml"), true);

        string? exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            throw new InvalidOperationException("Could not resolve the app path for the RunOnce entry.");
        TweakAction.RegString(
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            auto ? "*ddu" : "*ddumanual",
            $"\"{exe}\" --ddu-{(auto ? "auto" : "manual")}").Apply();

        NativeOps.RunTool("bcdedit", "/set {current} safeboot minimal");
        NativeOps.Restart();
    }

    /// <summary>Safe-boot finish half: leave safe boot, then run DDU
    /// (auto cleans everything and reboots; manual opens the UI).</summary>
    public static void FinishDdu(bool auto)
    {
        NativeOps.RunTool("bcdedit", "/deletevalue {current} safeboot");

        if (!File.Exists(DduExe))
            throw new FileNotFoundException($"DDU staged but not found at {DduExe}.");

        if (auto)
            TweakAction.Run(DduExe, "-CleanSoundBlaster -CleanRealtek -CleanAllGpus -Restart").Apply();
        else
            TweakAction.Run(DduExe, "").Apply();
    }

    //  Latest NVIDIA driver URL (GeForce lookup API) 

    public static string FindNvidiaDriverUrl()
    {
        const string uri = "https://gfwsl.geforce.com/services_toolkit/services/com/nvidia/services/" +
            "AjaxDriverService.php?func=DriverManualLookup&psid=120&pfid=929&osID=57" +
            "&languageCode=1033&isWHQL=1&dch=1&sort1=0&numberOfResults=1";

        using JsonDocument payload = JsonDocument.Parse(NativeOps.HttpGetString(uri));
        string version = payload.RootElement.GetProperty("IDS")[0]
            .GetProperty("downloadInfo").GetProperty("Version").GetString()
            ?? throw new InvalidOperationException("The NVIDIA lookup returned no driver version.");

        string windows = Environment.OSVersion.Version >= new Version(9, 1) ? "win10-win11" : "win8-win7";
        string arch = Environment.Is64BitOperatingSystem ? "64bit" : "32bit";
        return $"https://international.download.nvidia.com/Windows/{version}/{version}-desktop-{windows}-{arch}-international-dch-whql.exe";
    }

    /// <summary>Blocking: latest NVIDIA driver, extracted, optionally debloated,
    /// silently installed, old C:\NVIDIA gone. Settings come from GraphicsActions.</summary>
    public static void InstallNvidiaDriver(bool debloat)
    {
        EnsureSevenZip();

        string installer = Path.Combine(NativeOps.SystemRootTemp, "nvidiadriver.exe");
        NativeOps.Download(FindNvidiaDriverUrl(), installer);

        string unpacked = Path.Combine(NativeOps.SystemRootTemp, "nvidiadriver");
        NativeOps.SevenZipExtract(installer, unpacked);

        if (debloat)
        {
            foreach (string relative in NvidiaDebloatPaths)
            {
                try
                {
                    string target = Path.Combine(unpacked, relative);
                    if (Directory.Exists(target))
                        Directory.Delete(target, recursive: true);
                    else if (File.Exists(target))
                        File.Delete(target);
                }
                catch { }
            }
        }

        TweakAction.Run(Path.Combine(unpacked, "setup.exe"), "-s -noreboot -noeula -clean").Apply();
        NativeOps.DeleteDirectory(Path.Combine(NativeOps.Expand(@"%SystemDrive%\"), "NVIDIA"));
    }

    private static readonly string[] NvidiaDebloatPaths =
    {
        "Display.Nview", "FrameViewSDK", "HDAudio", "MSVCRT", "NvApp.MessageBus",
        "NvBackend", "NvContainer", "NvCpl", "NvDLISR", "NVPCF", "NvTelemetry",
        "NvVAD", "PhysX", "PPC", "ShadowPlay",
        @"NvApp\CEF", @"NvApp\osc", @"NvApp\Plugins", @"NvApp\UpgradeConsent",
        @"NvApp\www", @"NvApp\7z.dll", @"NvApp\7z.exe", @"NvApp\DarkModeCheck.exe",
        @"NvApp\InstallerExtension.dll", @"NvApp\NvApp.nvi", @"NvApp\NvAppApi.dll",
        @"NvApp\NvAppExt.dll", @"NvApp\NvConfigGenerator.dll",
    };

    //  AMD driver (minimal-setup scrape) 

    /// <summary>Blocking: scrape AMD's driver page, run the minimal-setup
    /// installer, tidy shortcuts, delete C:\AMD.</summary>
    public static void InstallAmdDriver()
    {
        var headers = new Dictionary<string, string>
        {
            ["User-Agent"] = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            ["Accept"] = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
            ["Referer"] = "https://www.amd.com/",
        };
        string page = NativeOps.HttpGetString("https://www.amd.com/en/support/download/drivers.html", headers);

        Match m = Regex.Match(page,
            @"href=""([^""]*?minimalsetup[^""]*_web\.exe)""",
            RegexOptions.IgnoreCase);
        if (!m.Success)
            throw new InvalidOperationException("No AMD minimal-setup installer link found on amd.com.");

        string installer = Path.Combine(NativeOps.SystemRootTemp, "amddriver.exe");
        NativeOps.DownloadWithHeaders(m.Groups[1].Value, installer, headers);
        TweakAction.Run(installer, "").Apply();

        string radeon = NativeOps.Expand(@"%SystemDrive%\Program Files\AMD\CNext\CNext\RadeonSoftware.exe");
        string radeonDir = Path.GetDirectoryName(radeon)!;
        NativeOps.CreateShortcut(NativeOps.DesktopShortcut("AMD Radeon Software"), radeon, radeonDir);

        string folderName = "AMD Software Adrenalin Edition";
        NativeOps.MoveFile(
            Path.Combine(NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs"), folderName, folderName + ".lnk"),
            NativeOps.StartMenuShortcut(folderName));
        NativeOps.DeleteDirectory(
            Path.Combine(NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs"), folderName));
        NativeOps.DeleteDirectory(Path.Combine(NativeOps.Expand(@"%SystemDrive%\"), "AMD"));
    }

    //  Intel driver (user picks the downloaded installer) 

    public static void OpenIntelDriverSearch() => NativeOps.Launch(IntelDriverSearch);

    /// <summary>UI thread: let the user point at the downloaded Intel installer.</summary>
    public static string? PickIntelDriver(string? initialDir = null) =>
        NativeOps.PickFile("Select the downloaded Intel driver installer");

    /// <summary>Blocking: run the chosen Intel installer, tidy shortcuts.</summary>
    public static void InstallIntelDriver(string installerPath)
    {
        TweakAction.Run(installerPath, "").Apply();

        string gfx = NativeOps.Expand(@"%SystemDrive%\Program Files\Intel\Intel Graphics Software\IntelGraphicsSoftware.exe");
        NativeOps.CreateShortcut(
            NativeOps.DesktopShortcut("Intel Graphics Software"), gfx,
            Path.GetDirectoryName(gfx));

        try { File.Delete(installerPath); } catch { }

        const string fileName = "Intel Graphics Software";
        NativeOps.MoveFile(
            Path.Combine(NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs"), "Intel", "Intel Graphics Software", fileName + ".lnk"),
            NativeOps.StartMenuShortcut(fileName));
        NativeOps.DeleteDirectory(
            Path.Combine(NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs"), "Intel"));
        NativeOps.DeleteDirectory(Path.Combine(NativeOps.Expand(@"%SystemDrive%\"), "Intel"));
        NativeOps.DeleteDirectory(Path.Combine(NativeOps.Expand(@"%SystemDrive%\"), "inteldriver"));
    }
}
