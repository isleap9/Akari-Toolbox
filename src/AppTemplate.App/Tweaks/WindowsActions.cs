using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Management.Deployment;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native ports of Ultimate "6 Windows" scripts 124 (appearance, debloat,
/// checks, settings/sound). Every script branch is registry / WinRT / CLI 
/// Appx work goes through <see cref="PackageManager"/>, capabilities/features
/// through dism.exe, never a script file. Each toggle carries a live-state
/// probe (the `Read*` methods) for navigation-time reads.
/// </summary>
public static partial class WindowsActions
{
    public static readonly string[] StartMenuLayoutOptions = { "25H2 (New)", "24H2 (Old)" };

    private static void Dword(string path, string name, int value) =>
        TweakAction.RegDword(path, name, value).Apply();

    private static void Sz(string path, string name, string value) =>
        TweakAction.RegString(path, name, value).Apply();

    private static void DelValue(string path, string name) =>
        TweakAction.RegDeleteValue(path, name).Apply();

    private static void DelKey(string path) =>
        TweakAction.RegDeleteKey(path).Apply();

    private static bool Like(string text, string pattern) =>
        Regex.IsMatch(text, "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
            RegexOptions.IgnoreCase);

    //  WinRT Appx (replaces every Get/Remove/AppxPackage pipeline) 

    private static void WaitWinRt(IAsyncInfo operation)
    {
        while (operation.Status == AsyncStatus.Started)
            System.Threading.Thread.Sleep(100);
        if (operation.Status is AsyncStatus.Error or AsyncStatus.Canceled)
        {
            Exception? error = operation.ErrorCode;
            throw error ?? new InvalidOperationException("The package operation was cancelled.");
        }
    }

    /// <summary>Remove every installed package whose name matches (all users, like the elevated scripts).</summary>
    private static void RemoveAppxMatching(Func<string, bool> match)
    {
        var pm = new PackageManager();
        foreach (var pkg in pm.FindPackages())
        {
            if (!match(pkg.Id.Name))
                continue;
            try { WaitWinRt(pm.RemovePackageAsync(pkg.Id.FullName, RemovalOptions.RemoveForAllUsers)); }
            catch { }
        }
    }

    /// <summary>Every package manifest known on this machine: current user, all
    /// other local users (mirrors Get-AppxPackage -AllUsers) and staged
    /// provisioned packages (covers store apps removed for everyone).</summary>
    private static IEnumerable<(string Name, string Manifest)> AllAppxManifests()
    {
        var pm = new PackageManager();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var collected = new List<(string Name, string Manifest)>();

        void Collect(IEnumerable<Windows.ApplicationModel.Package> packages)
        {
            List<Windows.ApplicationModel.Package> batch;
            try
            {
                batch = packages.ToList();
            }
            catch { return; }
            foreach (var pkg in batch)
            {
                try
                {
                    if (!seen.Add(pkg.Id.FullName))
                        continue;
                    string manifest = Path.Combine(pkg.InstalledPath, "AppXManifest.xml");
                    if (File.Exists(manifest))
                        collected.Add((pkg.Id.Name, manifest));
                }
                catch { }
            }
        }

        try { Collect(pm.FindPackages()); } catch { }

        // Other local users' packages (Get-AppxPackage -AllUsers equivalent).
        try
        {
            using Microsoft.Win32.RegistryKey? users = Microsoft.Win32.Registry.Users;
            if (users is not null)
            {
                foreach (string sid in users.GetSubKeyNames())
                {
                    if (!sid.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase))
                        continue;
                    try { Collect(pm.FindPackagesForUser(sid)); } catch { }
                }
            }
        }
        catch { }

        // Staged provisioned packages (present on disk, registered nowhere).
        try { Collect(pm.FindProvisionedPackages()); } catch { }

        return collected;
    }

    internal static bool IsAppxInstalled(string name)
    {
        var pm = new PackageManager();
        try
        {
            foreach (var pkg in pm.FindPackages())
            {
                if (pkg.Id.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }
        return false;
    }

    /// <summary>Re-register every known package whose name matches (the Add-AppxPackage -Register flow).</summary>
    private static void RegisterAppxMatching(Func<string, bool> match)
    {
        var pm = new PackageManager();
        foreach ((string name, string manifest) in AllAppxManifests())
        {
            if (!match(name))
                continue;
            try { WaitWinRt(pm.RegisterPackageAsync(new Uri(manifest), null, DeploymentOptions.None)); }
            catch { }
        }
    }

    /// <summary>Silent Add-AppxPackage for a downloaded .appx/.msixbundle (throws on failure).</summary>
    private static void InstallAppxPackage(string path)
    {
        var pm = new PackageManager();
        try
        {
            WaitWinRt(pm.AddPackageAsync(new Uri(path), null, DeploymentOptions.None));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"The package could not be installed: {ex.Message}", ex);
        }
    }

    //  1 Start Menu Taskbar 

    public static void StartMenuTaskbarClean()
    {
        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("StartMenuTaskbarClean.reg"));

        DelKey(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Taskband");
        NativeOps.DeleteDirectory(NativeOps.Expand(@"%AppData%\Microsoft\Internet Explorer\Quick Launch"));

        NativeOps.SetTrayIconsPromoted(1);

        foreach (string folder in StartMenuAccessibilityFolders())
            NativeOps.SetFolderHidden(folder, hidden: true);

        ApplyWin10StartLayout("StartMenuLayoutClean.xml");
        ImportStart2Bin();

        Dword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Start", "AllAppsViewMode", 2);

        NativeOps.StopProcess("explorer");
    }

    public static void StartMenuTaskbarDefault()
    {
        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("StartMenuTaskbarDefault.reg"));

        NativeOps.SetTrayIconsPromoted(0);

        foreach (string folder in StartMenuAccessibilityFolders())
            NativeOps.SetFolderHidden(folder, hidden: false);

        ApplyWin10StartLayout("StartMenuLayoutDefault.xml");

        try { File.Delete(NativeOps.Expand(@"%LocalAppData%\Packages\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\LocalState\start2.bin")); } catch { }

        Dword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Start", "AllAppsViewMode", 0);

        NativeOps.StopProcess("explorer");
    }

    public static bool ReadStartMenuClean() =>
        RegRead.Dword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Start", "AllAppsViewMode") is 2;

    private static string[] StartMenuAccessibilityFolders() =>
    [
        NativeOps.Expand(@"%AppData%\Microsoft\Windows\Start Menu\Programs\Accessibility"),
        NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs\Accessibility"),
        NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs\Accessories"),
    ];

    private static void ApplyWin10StartLayout(string xmlResource)
    {
        const string layoutFile = @"C:\Windows\StartMenuLayout.xml";
        try { File.Delete(layoutFile); } catch { }
        NativeOps.WriteAllText(layoutFile, NativeOps.LoadWindowsData(xmlResource));

        foreach (string root in new[] { "HKLM", "HKCU" })
        {
            string key = $@"{root}\SOFTWARE\Policies\Microsoft\Windows\Explorer";
            Dword(key, "LockedStartLayout", 1);
            Sz(key, "StartLayoutFile", layoutFile);
        }

        NativeOps.StopProcess("explorer");
        System.Threading.Thread.Sleep(5000);

        foreach (string root in new[] { "HKLM", "HKCU" })
            Dword($@"{root}\SOFTWARE\Policies\Microsoft\Windows\Explorer", "LockedStartLayout", 0);

        try { File.Delete(layoutFile); } catch { }
    }

    private static void ImportStart2Bin()
    {
        string localState = NativeOps.Expand(@"%LocalAppData%\Packages\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\LocalState");
        if (!Directory.Exists(localState))
            return;
        string bin = Path.Combine(localState, "start2.bin");
        try { File.Delete(bin); } catch { }
        NativeOps.DecodeCertToFile(NativeOps.LoadWindowsData("start2.txt"), bin);
    }

    //  2 Start Menu Layout 

    public static void ApplyStartMenuLayout(int index) =>
        NativeOps.ImportRegContent(NativeOps.LoadWindowsData(index == 0 ? "NewStartMenu.reg" : "OldStartMenu.reg"));

    public static int ReadStartMenuLayout() =>
        RegRead.Dword(@"HKLM\SYSTEM\ControlSet001\Control\FeatureManagement\Overrides\14\2792562829", "EnabledState") is 2 ? 0 : 1;

    //  3 Start Menu Shortcuts 

    public static void CreateStartMenuShortcuts()
    {
        string allUsers = NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs");
        string perUser = NativeOps.Expand(@"%AppData%\Microsoft\Windows\Start Menu\Programs");

        NativeOps.CreateShortcut(Path.Combine(allUsers, "Start Menu Shortcuts 1.lnk"), allUsers);
        NativeOps.CreateShortcut(Path.Combine(allUsers, "Start Menu Shortcuts 2.lnk"), perUser);
        NativeOps.CreateShortcut(Path.Combine(allUsers, "Startup Programs 1.lnk"), Path.Combine(perUser, "Startup"));
        NativeOps.CreateShortcut(Path.Combine(allUsers, "Startup Programs 2.lnk"), Path.Combine(allUsers, "StartUp"));
        NativeOps.CreateShortcut(Path.Combine(allUsers, "Recycle Bin.lnk"), "::{645ff040-5081-101b-9f08-00aa002f954e}");

        NativeOps.Launch(allUsers);
        NativeOps.Launch(perUser);
    }

    //  4 Context Menu 

    public static void ContextMenuClean()
    {
        Sz(@"HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "");
        Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoCustomizeThisFolder", 1);
        DelKey(@"HKCR\Folder\shell\pintohome");
        DelKey(@"HKCR\*\shell\pintohomefile");
        DelKey(@"HKCR\exefile\shellex\ContextMenuHandlers\Compatibility");
        const string blocked = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
        Sz(blocked, "{9F156763-7844-4DC4-B2B1-901F640F5155}", "");
        Sz(blocked, "{09A47860-11B0-4DA5-AFA5-26D86198A780}", "");
        Sz(blocked, "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}", "");
        DelKey(@"HKCR\Folder\ShellEx\ContextMenuHandlers\Library Location");
        DelKey(@"HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\ModernSharing");
        Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "NoPreviousVersionsPage", 1);
        DelKey(@"HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo");
        DelKey(@"HKCR\UserLibraryFolder\shellex\ContextMenuHandlers\SendTo");
    }

    public static void ContextMenuDefault()
    {
        DelKey(@"HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}");
        DelValue(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoCustomizeThisFolder");
        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("ContextMenuDefault.reg"));
        Sz(@"HKCR\exefile\shellex\ContextMenuHandlers\Compatibility", "", "{1d27f844-3a1f-4410-85ac-14651078412d}");
        DelKey(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked");
        Sz(@"HKCR\Folder\ShellEx\ContextMenuHandlers\Library Location", "", "{3dad6c5d-2167-4cae-9914-f99e41c12cfa}");
        Sz(@"HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\ModernSharing", "", "{e2bf9676-5f8f-435c-97eb-11607a5bedf7}");
        DelValue(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "NoPreviousVersionsPage");
        Sz(@"HKCR\AllFilesystemObjects\shellex\ContextMenuHandlers\SendTo", "", "{7BA4C740-9E81-11CF-99D3-00AA004AE837}");
        Sz(@"HKCR\UserLibraryFolder\shellex\ContextMenuHandlers\SendTo", "", "{7BA4C740-9E81-11CF-99D3-00AA004AE837}");
    }

    public static bool ReadContextMenuClean() =>
        RegRead.String(@"HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "") is not null;

    //  5 Theme Black 

    public static void ApplyThemeBlack() =>
        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("BlackTheme.reg"));

    public static void ApplyThemeDefault() =>
        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("DefaultTheme.reg"));

    public static bool ReadThemeBlack() =>
        RegRead.Dword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme") is 0;

    //  6 Sign-out / Lock Screen wallpaper 

    public static void SignoutWallpaperBlack()
    {
        const string black = @"C:\Windows\Black.jpg";
        NativeOps.CreateBlackImage(black);
        const string csp = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP";
        Sz(csp, "LockScreenImagePath", black);
        Dword(csp, "LockScreenImageStatus", 1);
        Sz(@"HKCU\Control Panel\Desktop", "Wallpaper", black);
        NativeOps.RunTool("rundll32.exe", "user32.dll, UpdatePerUserSystemParameters");
        Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon", 1);
    }

    public static void SignoutWallpaperDefault()
    {
        DelKey(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP");
        Sz(@"HKCU\Control Panel\Desktop", "Wallpaper", @"C:\Windows\Web\Wallpaper\Windows\img0.jpg");
        NativeOps.RunTool("rundll32.exe", "user32.dll, UpdatePerUserSystemParameters");
        try { File.Delete(@"C:\Windows\Black.jpg"); } catch { }
        DelValue(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\System", "DisableAcrylicBackgroundOnLogon");
    }

    public static bool ReadSignoutWallpaperBlack() =>
        RegRead.String(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP", "LockScreenImagePath") == @"C:\Windows\Black.jpg";

    //  7 User Account Pictures 

    public static void UserAccountPicturesBlack()
    {
        string source = NativeOps.Expand(@"%SystemDrive%\ProgramData\Microsoft\User Account Pictures");
        string backup = NativeOps.Expand(@"%SystemDrive%\ProgramData\User Account Pictures");
        if (!Directory.Exists(backup))
            NativeOps.CopyDirectory(source, backup);
        NativeOps.BlackenImagesInFolder(source);
    }

    public static void UserAccountPicturesDefault()
    {
        string backup = NativeOps.Expand(@"%SystemDrive%\ProgramData\User Account Pictures");
        string dest = NativeOps.Expand(@"%SystemDrive%\ProgramData\Microsoft\User Account Pictures");
        NativeOps.CopyDirectory(backup, dest);
    }

    public static bool ReadAccountPicturesBlack() =>
        Directory.Exists(NativeOps.Expand(@"%SystemDrive%\ProgramData\User Account Pictures"));

    //  8 Widgets 

    public static void WidgetsOff()
    {
        Dword(@"HKLM\SOFTWARE\Microsoft\PolicyManager\default\NewsAndInterests\AllowNewsAndInterests", "value", 0);
        Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
        NativeOps.StopProcess("Widgets", "WidgetService");
    }

    public static void WidgetsDefault()
    {
        Dword(@"HKLM\SOFTWARE\Microsoft\PolicyManager\default\NewsAndInterests\AllowNewsAndInterests", "value", 1);
        DelKey(@"HKLM\SOFTWARE\Policies\Microsoft\Dsh");
    }

    public static bool ReadWidgetsOff() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests") is 0;

    //  9 Copilot 

    public static void CopilotOff()
    {
        NativeOps.StopProcess("Copilot");
        RemoveAppxMatching(name => name.Contains("Copilot", StringComparison.OrdinalIgnoreCase));
        Dword(@"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
        Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
    }

    public static void CopilotDefault()
    {
        RegisterAppxMatching(name => name.Contains("Copilot", StringComparison.OrdinalIgnoreCase));
        DelKey(@"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot");
        DelKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot");
    }

    public static bool ReadCopilotOff() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot") is 1;

    //  10/11/12 one-shot openers 

    public static void OpenGameMode() => TweakAction.Open("ms-settings:gaming-gamemode").Apply();

    public static void OpenPointerPrecision() => TweakAction.Launch("control.exe", "main.cpl ,2").Apply();

    public static void OpenScalingSettings() => TweakAction.Open("ms-settings:display-advanced").Apply();

    //  13 Bloatware hub 

    public static void RemoveAllBloatware()
    {
        RemoveBloatwareUwpApps();
        RemoveBloatwareCapabilities();
        RemoveBloatwareOptionalFeatures();

        foreach (string task in new[] { "OneDrive", "PLUGScheduler" })
            NativeOps.RunTool("schtasks", $"/delete /tn \"{task}\" /f");

        foreach (string name in new[] { "*Microsoft GameInput*", "*Update for x64-based Windows Systems*", "*Microsoft Update Health Tools*" })
            UninstallMsiByDisplayName(name);

        NativeOps.StopAndDeleteServicesMatching("brlapi");
        NativeOps.RunTool("takeown", $"/f \"{NativeOps.Expand(@"%SystemRoot%\brltty")}\" /r /d y");
        NativeOps.RunTool("icacls", $"\"{NativeOps.Expand(@"%SystemRoot%\brltty")}\" /grant *S-1-5-32-544:F /t");
        NativeOps.DeleteDirectory(NativeOps.Expand(@"%SystemRoot%\brltty"));

        NativeOps.StopProcess("OneDrive");
        NativeOps.RunTool(NativeOps.Expand(@"%SystemRoot%\System32\OneDriveSetup.exe"), "-uninstall");
        NativeOps.RunTool(NativeOps.Expand(@"%SystemRoot%\SysWOW64\OneDriveSetup.exe"), "-uninstall");

        DelKey(@"HKLM\SYSTEM\ControlSet001\Services\uhssvc");
    }

    private static void RemoveBloatwareUwpApps()
    {
        string[] keep =
        [
            "*CBS*", "*Microsoft.AV1VideoExtension*", "*Microsoft.AVCEncoderVideoExtension*",
            "*Microsoft.HEIFImageExtension*", "*Microsoft.HEVCVideoExtension*",
            "*Microsoft.MPEG2VideoExtension*", "*Microsoft.Paint*", "*Microsoft.RawImageExtension*",
            "*Microsoft.SecHealthUI*", "*Microsoft.VP9VideoExtensions*", "*Microsoft.WebMediaExtensions*",
            "*Microsoft.WebpImageExtension*", "*Microsoft.Windows.Photos*",
            "*Microsoft.Windows.ShellExperienceHost*", "*Microsoft.Windows.StartMenuExperienceHost*",
            "*Microsoft.WindowsNotepad*", "*NVIDIACorp.NVIDIAControlPanel*",
            "*windows.immersivecontrolpanel*",
        ];
        RemoveAppxMatching(name => !keep.Any(k => Like(name, k)));
    }

    private static void RemoveBloatwareCapabilities()
    {
        string[] keep =
        [
            "*Microsoft.Windows.Ethernet*", "*Microsoft.Windows.MSPaint*", "*Microsoft.Windows.Notepad*",
            "*Microsoft.Windows.Notepad.System*", "*Microsoft.Windows.Wifi*", "*NetFX3*",
            "*VBSCRIPT*", "*WMIC*", "*Windows.Client.ShellComponents*",
        ];
        foreach (string capability in DismCapabilitiesInstalled())
        {
            if (keep.Any(k => Like(capability, k)))
                continue;
            NativeOps.RunTool("dism", $"/online /remove-capability /capabilityname:{capability} /norestart");
        }
    }

    private static string[] DismCapabilitiesInstalled()
    {
        string output = NativeOps.RunToolCapture("dism", "/online /get-capabilities /format:table");
        var names = new List<string>();
        bool installed = false;
        foreach (string line in output.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("Capability Identity", StringComparison.OrdinalIgnoreCase))
            {
                string name = t["Capability Identity".Length..].Trim().TrimStart(':').Trim();
                if (name.Length > 0)
                    names.Add(name.Split(' ')[0]);
                installed = false;
            }
            else if (t.StartsWith("State", StringComparison.OrdinalIgnoreCase))
            {
                installed = t.Contains("Installed", StringComparison.OrdinalIgnoreCase);
                if (!installed && names.Count > 0)
                    names.RemoveAt(names.Count - 1);
            }
        }
        return names.ToArray();
    }

    private static void RemoveBloatwareOptionalFeatures()
    {
        string[] keep =
        [
            "*DirectPlay*", "*LegacyComponents*", "*NetFx3*", "*NetFx4*", "*NetFx4-AdvSrvs*",
            "*NetFx4ServerFeatures*", "*SearchEngine-Client-Package*", "*Server-Shell*",
            "*Windows-Defender*", "*Server-Drivers-General*", "*ServerCore-Drivers-General*",
            "*ServerCore-Drivers-General-WOW64*", "*Server-Gui-Mgmt*", "*WirelessNetworking*",
        ];
        foreach (string feature in DismFeaturesEnabled())
        {
            if (keep.Any(k => Like(feature, k)))
                continue;
            NativeOps.RunTool("dism", $"/online /disable-feature /featurename:{feature} /norestart");
        }
    }

    private static string[] DismFeaturesEnabled()
    {
        string output = NativeOps.RunToolCapture("dism", "/online /get-features /format:table");
        var names = new List<string>();
        string? pending = null;
        foreach (string line in output.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("Feature Name", StringComparison.OrdinalIgnoreCase))
                pending = t["Feature Name".Length..].Trim().TrimStart(':').Trim().Split(' ')[0];
            else if (pending is not null && t.StartsWith("State", StringComparison.OrdinalIgnoreCase))
            {
                if (t.Contains("Enabled", StringComparison.OrdinalIgnoreCase) && pending.Length > 0)
                    names.Add(pending);
                pending = null;
            }
        }
        return names.ToArray();
    }

    private static void UninstallMsiByDisplayName(string displayNameLike)
    {
        foreach (string keyPath in new[]
        {
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        })
        {
            var (baseKey, sub) = RegistryPath.Resolve(keyPath);
            using Microsoft.Win32.RegistryKey? root = baseKey.OpenSubKey(sub);
            if (root is null)
                continue;
            foreach (string child in root.GetSubKeyNames())
            {
                using Microsoft.Win32.RegistryKey? app = root.OpenSubKey(child);
                object? display = app?.GetValue("DisplayName");
                if (display is string name && Like(name, displayNameLike))
                    NativeOps.RunTool("msiexec.exe", $"/x {child} /qn /norestart");
            }
        }
    }

    public static void InstallStore()
    {
        // 1) Re-register the Store from its package files if they're still on disk.
        RegisterAppxMatching(name => name.Contains("WindowsStore", StringComparison.OrdinalIgnoreCase));

        // 2) If the Store is still gone, trigger Windows' built-in Store reinstall.
        // wsreset only kicks the reinstall off asynchronously, so poll for the
        // package instead of checking once (a single immediate check races it).
        if (!IsAppxInstalled("Microsoft.WindowsStore"))
        {
            NativeOps.RunTool("wsreset.exe", "-i");
            DateTime deadline = DateTime.UtcNow.AddMinutes(3);
            while (DateTime.UtcNow < deadline)
            {
                System.Threading.Thread.Sleep(5000);
                if (IsAppxInstalled("Microsoft.WindowsStore"))
                    break;
            }
        }
        if (!IsAppxInstalled("Microsoft.WindowsStore"))
            throw new InvalidOperationException("The Store is still missing after reinstall.");

        Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsStore\WindowsUpdate", "AutoDownload", 2);
        NativeOps.StopProcess("WinStore.App", "backgroundTaskHost", "StoreDesktopExtension");
        System.Threading.Thread.Sleep(1000);

        string dat = NativeOps.Expand(@"%LocalAppData%\Packages\Microsoft.WindowsStore_8wekyb3d8bbwe\Settings\settings.dat");
        NativeOps.RegLoadImportUnload(@"HKLM\Settings", dat, NativeOps.LoadWindowsData("WindowsStore.reg"));
        NativeOps.Launch("ms-windows-store:settings");
    }

    public static void InstallWinget()
    {
        // Fast path: Store present  open the App Installer product page.
        if (IsAppxInstalled("Microsoft.WindowsStore"))
        {
            NativeOps.Launch("ms-windows-store://pdp?&productid=9nblggh4nns1");
            return;
        }

        if (NativeOps.RunToolCapture("where", "winget").Contains("winget", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("winget is already installed.");

        string temp = Path.GetTempPath();
        string runtime = Path.Combine(temp, "WindowsAppRuntimeInstall-x64.exe");
        NativeOps.Download("https://aka.ms/windowsappsdk/1.8/1.8.260804001/windowsappruntimeinstall-x64.exe", runtime);
        NativeOps.RunTool(runtime, "");

        string bundle = Path.Combine(temp, "DesktopAppInstaller.msixbundle");
        NativeOps.Download("https://aka.ms/getwinget", bundle);
        InstallAppxPackage(bundle);

        System.Threading.Thread.Sleep(2000);
        if (!NativeOps.RunToolCapture("where", "winget").Contains("winget", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Automatic install failed. Open the Store manually and get 'App Installer'.");
    }

    public static void CheckInstalledApps() => TweakAction.Open("ms-settings:appsfeatures").Apply();

    public static void InstallAllUwpApps() =>
        RegisterAppxMatching(_ => true);

    public static void OpenUwpFeatures() => TweakAction.Open("ms-settings:optionalfeatures").Apply();

    public static void OpenLegacyFeatures() =>
        TweakAction.Launch(NativeOps.Expand(@"%SystemRoot%\System32\OptionalFeatures.exe")).Apply();

    public static void InstallOneDrive()
    {
        NativeOps.RunTool(NativeOps.Expand(@"%SystemRoot%\SysWOW64\OneDriveSetup.exe"), "");
        NativeOps.RunTool(NativeOps.Expand(@"%SystemRoot%\System32\OneDriveSetup.exe"), "");
    }

    public static void InstallRemoteDesktop()
    {
        string exe = Path.Combine(NativeOps.SystemRootTemp, "RemoteDesktopConnection.exe");
        NativeOps.Download("https://go.microsoft.com/fwlink/?linkid=2247659", exe);
        NativeOps.RunTool(exe, "");
    }

    public static void InstallSnippingTool()
    {
        string exe = Path.Combine(NativeOps.SystemRootTemp, "SnippingTool.exe");
        NativeOps.Download(
            "https://download.microsoft.com/download/f/4/e/f4e03465-34d1-49b6-af1a-2816ca4a2402/installers_signed/snippingtool_setup_x64.exe",
            exe);
        NativeOps.RunTool(exe, "");
        RegisterAppxMatching(name => name.Contains("Microsoft.ScreenSketch", StringComparison.OrdinalIgnoreCase));
    }

    //  1418 Bloatware checks 

    public static void OpenLegacyAppsCheck() =>
        TweakAction.Launch(NativeOps.Expand(@"%SystemRoot%\system32\appwiz.cpl")).Apply();

    public static void OpenLegacyFeaturesCheck() =>
        TweakAction.Launch(NativeOps.Expand(@"%SystemRoot%\system32\optionalfeatures.exe")).Apply();

    public static void OpenUwpAppsCheck() => TweakAction.Open("ms-settings:appsfeatures").Apply();

    public static void OpenTaskManager() => TweakAction.Launch("taskmgr").Apply();

    //  19 Game Bar 

    public static void GameBarOff()
    {
        NativeOps.StopProcess("GameBar");
        RemoveAppxMatching(name =>
            name.Contains("Gaming", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Xbox", StringComparison.OrdinalIgnoreCase));
        NativeOps.RunTool("sc.exe", "stop GameInputSvc");
        NativeOps.StopProcess("gamingservices", "gamingservicesnet", "GameInputRedistService");
        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("GameBarOff.reg"));
        NativeOps.RunAsTrustedInstaller(
            "reg add \"HKLM\\SOFTWARE\\Microsoft\\WindowsRuntime\\ActivatableClassId\\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter\" /v \"ActivationType\" /t REG_DWORD /d \"0\" /f");
    }

    public static void GameBarDefault()
    {
        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("GameBarOn.reg"));
        NativeOps.RunAsTrustedInstaller(
            "reg add \"HKLM\\SOFTWARE\\Microsoft\\WindowsRuntime\\ActivatableClassId\\Windows.Gaming.GameBar.PresenceServer.Internal.PresenceWriter\" /v \"ActivationType\" /t REG_DWORD /d \"1\" /f");
        RegisterAppxMatching(name =>
            name.Contains("Gaming", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Xbox", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Store", StringComparison.OrdinalIgnoreCase));
        NativeOps.WingetInstall("Microsoft.EdgeWebView2Runtime");
        NativeOps.WingetInstall("Microsoft.Gaming.GamingServicesRepairTool",
            "Microsoft.Gaming.GamingServicesRepairTool_Microsoft.Winget.Source_8wekyb3d8bbwe");
        NativeOps.Launch(NativeOps.PackagePath(
            "Microsoft.Gaming.GamingServicesRepairTool_Microsoft.Winget.Source_8wekyb3d8bbwe", "gamingrepairtool.exe"));
    }

    public static bool ReadGameBarOff() =>
        RegRead.Dword(@"HKCU\Software\Microsoft\GameBar", "UseNexusForGameBarEnabled") is 0;

    //  20 Edge & WebView 

    public static void EdgeUninstall()
    {
        UnregisterEdgeUpdateServices();
        RemoveEdgeAppxAndFiles();
        RemoveEdgeLegacyPackage();
    }

    private static void UnregisterEdgeUpdateServices()
    {
        NativeOps.StopProcess("backgroundTaskHost", "Copilot", "CrossDeviceResume", "GameBar",
            "MicrosoftEdgeUpdate", "msedge", "msedgewebview2", "OneDrive", "RuntimeBroker", "SearchHost");
        foreach (Process p in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                if (p.ProcessName.Contains("edge", StringComparison.OrdinalIgnoreCase))
                    p.Kill(true);
            }
            catch { }
        }

        foreach (string special in new[] { "LocalApplicationData", "ProgramFilesX86", "ProgramFiles" })
        {
            string folder = Environment.GetFolderPath(
                special == "LocalApplicationData" ? Environment.SpecialFolder.LocalApplicationData :
                special == "ProgramFilesX86" ? Environment.SpecialFolder.ProgramFilesX86 :
                Environment.SpecialFolder.ProgramFiles);
            string pattern = Path.Combine(folder, "Microsoft", "EdgeUpdate");
            if (!Directory.Exists(pattern))
                continue;
            foreach (string updater in Directory.GetFiles(pattern, "MicrosoftEdgeUpdate.exe", SearchOption.AllDirectories))
            {
                NativeOps.RunTool(updater, "/unregsvc");
                NativeOps.RunTool(updater, "/uninstall");
            }
        }
    }

    private static void RemoveEdgeAppxAndFiles()
    {
        foreach (string location in new[]
        {
            @"HKCU\SOFTWARE\Microsoft\EdgeUpdate", @"HKLM\SOFTWARE\Microsoft\EdgeUpdate",
            @"HKCU\SOFTWARE\Policies\Microsoft\EdgeUpdate", @"HKLM\SOFTWARE\Policies\Microsoft\EdgeUpdate",
        })
        {
            try
            {
                var (baseKey, sub) = RegistryPath.Resolve(location);
                baseKey.DeleteSubKeyTree(sub, throwOnMissingSubKey: false);
            }
            catch { }
        }

        RemoveAppxMatching(name =>
            name.Contains("MicrosoftEdge", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Edge", StringComparison.OrdinalIgnoreCase));

        try { Directory.Delete(NativeOps.Expand(@"%SystemDrive%\Program Files (x86)\Microsoft"), recursive: true); } catch { }
        try { File.Delete(NativeOps.Expand(@"%SystemDrive%\Users\Public\Desktop\Microsoft Edge.lnk")); } catch { }

        NativeOps.StopAndDeleteServicesMatching("Edge");
    }

    private static void RemoveEdgeLegacyPackage()
    {
        const string packages = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\Packages";
        var (baseKey, sub) = RegistryPath.Resolve(packages);
        using Microsoft.Win32.RegistryKey? root = baseKey.OpenSubKey(sub);
        if (root is null)
            return;
        foreach (string child in root.GetSubKeyNames())
        {
            if (!child.Contains("Microsoft-Windows-Internet-Browser-Package", StringComparison.OrdinalIgnoreCase))
                continue;
            Dword($@"{packages}\{child}", "Visibility", 1);
            DelKey($@"{packages}\{child}\Owners");
            NativeOps.RunTool("dism", $"/online /Remove-Package /PackageName:{child} /quiet /norestart");
        }
    }

    public static void EdgeReinstallDefault()
    {
        NativeOps.WingetInstall("Microsoft.Edge");
        NativeOps.WingetInstall("Microsoft.EdgeWebView2Runtime");
        // Same optimized Edge policies the Setup page applies.
        const string edge = @"HKLM\SOFTWARE\Policies\Microsoft\Edge";
        Sz($@"{edge}\ExtensionInstallForcelist", "1",
            "odfafepnkmbhccpbejgmiehpchacaeak;https://edge.microsoft.com/extensionwebstorebase/v1/crx");
        Dword(edge, "HardwareAccelerationModeEnabled", 0);
        Dword(edge, "BackgroundModeEnabled", 0);
        Dword(edge, "StartupBoostEnabled", 0);
        NativeOps.RemoveSubkeysByDefaultValueLike(@"HKLM\Software\Microsoft\Active Setup\Installed Components", "Edge");
        NativeOps.RemoveValuesLike(@"HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce", "msedge");
        NativeOps.StopAndDeleteServicesMatching("Edge");
        NativeOps.DeleteScheduledTasksMatching("Edge");
        const string bho = @"{1FD49718-1D00-4B19-AF5F-070AF6D5D54C}";
        DelKey($@"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{bho}");
        DelKey($@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{bho}");
    }

    public static bool ReadEdgeUninstalled()
    {
        var (baseKey, sub) = RegistryPath.Resolve(
            @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft Edge");
        using Microsoft.Win32.RegistryKey? key = baseKey.OpenSubKey(sub);
        return key is null;
    }

    //  21 Notepad Settings 

    public static void NotepadSettingsOn()
    {
        NativeOps.StopProcess("Notepad");
        System.Threading.Thread.Sleep(2000);
        string dat = NativeOps.Expand(@"%LocalAppData%\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\Settings\settings.dat");
        NativeOps.RegLoadImportUnload(@"HKLM\Settings", dat, NativeOps.LoadWindowsData("NotepadSettings.reg"));
    }

    public static void NotepadSettingsDefault()
    {
        NativeOps.StopProcess("Notepad");
        System.Threading.Thread.Sleep(2000);
        try { File.Delete(NativeOps.Expand(@"%LocalAppData%\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\Settings\settings.dat")); } catch { }
    }

    public static bool ReadNotepadOn() =>
        File.Exists(NativeOps.Expand(@"%LocalAppData%\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\Settings\settings.dat"));

    //  22 Control Panel Settings (delegates to the Services-combobox port) 

    public static void ControlPanelOptimize() => ControlPanelActions.ApplyAkariDefault();

    public static void ControlPanelDefault() => ControlPanelActions.ApplyWindowsDefault();

    public static bool ReadControlPanelOptimized() =>
        ControlPanelActions.ReadPreset() == ControlPanelPreset.AkariDefault;

    //  23/24 Sound 

    public static void OpenSoundPanel() => TweakAction.Launch("mmsys.cpl").Apply();

    public static void LoudnessEqUnhide()
    {
        NativeOps.RunTool("net", "stop audiosrv /y");
        NativeOps.RunTool("net", "stop AudioEndpointBuilder /y");

        const string render = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render";
        var (baseKey, sub) = RegistryPath.Resolve(render);
        using (Microsoft.Win32.RegistryKey? root = baseKey.OpenSubKey(sub))
        {
            if (root is not null)
            {
                foreach (string guid in root.GetSubKeyNames())
                {
                    Sz($@"{render}\{guid}\FxProperties",
                        "{d04e05a6-594b-4fb6-a80d-01af5eed7d1d},3",
                        "{5860E1C5-F95C-4a7a-8EC8-8AEF24F379A1}");
                }
            }
        }

        NativeOps.RunTool("net", "start audiosrv");
        NativeOps.RunTool("net", "start AudioEndpointBuilder");
        NativeOps.Launch("mmsys.cpl");
    }
}
