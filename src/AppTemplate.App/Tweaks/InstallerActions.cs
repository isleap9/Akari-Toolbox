using System.IO;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native port of Ultimate "4 Installers" (script 1: 27 apps + script 2: MSI
/// Afterburner): download each installer from FR33THY's GitHub release at
/// runtime and run it with the script's silent arguments. Two groups matching
/// the Akari-Tool Installers tab.
/// </summary>
public static class InstallerActions
{
    private const string FilesUrl =
        "https://github.com/FR33THYFR33THY/Ultimate/releases/download/Files/";

    public sealed record InstallerItem(string Label, Action Install);

    public static IReadOnlyList<InstallerItem> Launchers { get; } = new List<InstallerItem>
    {
        new("Steam", () => DownloadAndRun("steam.exe", "/S", wait: true)),
        new("Epic Games", () => DownloadAndRun("epic.msi", "/quiet", wait: true)),
        new("Battle.net", () => DownloadAndRun("battlenet.exe", "--lang=enUS --installpath=\"C:\\Program Files (x86)\\Battle.net\"", wait: true)),
        new("EA App", () => DownloadAndRun("ea.exe", null, wait: true)),
        new("Ubisoft Connect", () => DownloadAndRun("ubisoft.exe", "/S", wait: true)),
        new("Rockstar Games", () => DownloadAndRun("rockstar.exe", "/s /f", wait: true)),
        new("League of Legends", () => DownloadAndRun("league.exe", "--skip-to-install", wait: false)),
        new("Valorant", () => DownloadAndRun("valorant.exe", "--skip-to-install", wait: false)),
        new("Escape from Tarkov", () => DownloadAndRun("bsg.exe", "/VERYSILENT /NORESTART", wait: true)),
        new("Roblox", () => DownloadAndRun("roblox.exe", "/S", wait: false)),
        new("Google Chrome", () => DownloadAndRun("chrome.exe", "--silent --install", wait: true)),
        new("Brave", () => DownloadAndRun("brave.exe", "--system-level", wait: true)),
        new("Firefox", InstallFirefox),
        new("Discord", () => DownloadAndRunUserTemp("discord.exe", null)),
        new("Spotify", () => DownloadAndRunUserTemp("spotify.exe", null)),
        new("OBS Studio", () => DownloadAndRun("obs.exe", "/S", wait: true)),
        new("Notepad++", () => DownloadAndRun("notepad++.exe", "/S", wait: true)),
        new("7-Zip", () => DownloadAndRun("7zip.exe", "/S", wait: true)),
        new("GOG", () => DownloadAndRun("gog.exe", null, wait: true)),
        new("PotPlayer", () => DownloadAndRun("potplayer.exe", "/S /allusers", wait: true)),
        new("Onboard Mem Mgr", () => DownloadOnly(
            "omm.exe", Path.Combine(NativeOps.Expand(@"%SystemDrive%\"), "Program Files (x86)", "Onboard Memory Manager", "Onboard Memory Manager.exe"))),
        new("FrameView", () => DownloadAndRun("frameview.exe", "/s", wait: true)),
        new("Nvidia App", () => DownloadAndRun("nvidiaapp.exe", "/s", wait: true)),
        new("Helium", () => DownloadAndRun("helium.exe", "/S", wait: true)),
    };

    public static IReadOnlyList<InstallerItem> GpuTools { get; } = new List<InstallerItem>
    {
        new("MSI Afterburner", () => DownloadAndRun("msiafterburner.exe", "/S", wait: true)),
        new("Nvidia Profile Insp.", () => DownloadOnly(
            "inspector.exe", Path.Combine(NativeOps.Expand(@"%SystemDrive%\"), "Program Files (x86)", "Nvidia Profile Inspector", "Nvidia Profile Inspector.exe"))),
        new("More Clock Tool", () => DownloadOnly(
            "moreclocktool.exe", Path.Combine(NativeOps.Expand(@"%SystemDrive%\"), "Program Files (x86)", "More Clock Tool", "More Clock Tool.exe"))),
        new("CRU / SRE", InstallCru),
    };

    private static void DownloadAndRun(string file, string? args, bool wait)
    {
        string dest = Path.Combine(NativeOps.SystemRootTemp, file);
        NativeOps.Download(FilesUrl + file, dest);
        RunInstaller(dest, args, wait);
    }

    private static void DownloadAndRunUserTemp(string file, string? args)
    {
        string dest = Path.Combine(Path.GetTempPath(), file);
        NativeOps.Download(FilesUrl + file, dest);
        NativeOps.Launch(dest, args);
    }

    private static void DownloadOnly(string file, string dest)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        NativeOps.Download(FilesUrl + file, dest);
    }

    private static void RunInstaller(string exe, string? args, bool wait)
    {
        if (wait)
        {
            NativeOps.RunTool(exe, args ?? "");
        }
        else
        {
            NativeOps.Launch(exe, args);
        }
    }

    private static void InstallCru()
    {
        string dir = Path.Combine(NativeOps.Expand(@"%SystemDrive%\"), "Program Files (x86)", "CRUSRE");
        Directory.CreateDirectory(dir);
        NativeOps.Download(FilesUrl + "cru.exe", Path.Combine(dir, "CRU.exe"));
        NativeOps.Download(FilesUrl + "sre.exe", Path.Combine(dir, "SRE.exe"));
        NativeOps.Launch(Path.Combine(dir, "CRU.exe"));
    }

    private static void InstallFirefox()
    {
        string installer = Path.Combine(NativeOps.SystemRootTemp, "firefox.exe");
        NativeOps.Download(FilesUrl + "firefox.exe", installer);
        NativeOps.RunTool(installer, "/S");

        // Drop the maintenance service the same way the script does.
        NativeOps.RunTool(
            @"C:\Program Files (x86)\Mozilla Maintenance Service\uninstall.exe", "/S");

        // Force uBlock Origin for every profile via the distribution folder.
        string extDir = @"C:\Program Files\Mozilla Firefox\distribution\extensions";
        Directory.CreateDirectory(extDir);
        NativeOps.Download(
            "https://addons.mozilla.org/firefox/downloads/latest/ublock-origin/latest.xpi",
            Path.Combine(extDir, "uBlock0@raymondhill.net.xpi"));

        NativeOps.Launch(
            NativeOps.Expand(@"%SystemDrive%\Program Files\Mozilla Firefox\firefox.exe"), "--headless");
    }
}
