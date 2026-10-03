using System.IO;
using Microsoft.Win32;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Which Control Panel preset is live on this machine.
/// </summary>
public enum ControlPanelPreset
{
    AkariDefault,
    WindowsDefault,
    Mixed,
}

/// <summary>
/// Native port of Ultimate "6 Windows" script 22 (Control Panel Settings).
/// "AkariOS Default" runs its Optimize branch, "Windows Default" its Default branch 
/// the same combobox contract the old Companion's Services section had (fixed
/// service/reg bundle per option, no .ps1 invoked). The two big applet .reg blobs
/// live verbatim in Tweaks\Data\Windows and are imported with regedit /S exactly
/// like the script's Set-Content + Regedit.exe /S sequence.
/// </summary>
public static class ControlPanelActions
{
    public static readonly string[] Options = { "AkariOS Default", "Windows Default" };

    private const string CdpUserSvc = @"HKLM\SYSTEM\ControlSet001\Services\CDPUserSvc";
    private const string CloudStoreCurrent =
        @"HKCU\Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current";
    private const string DefragTask = @"\Microsoft\Windows\Defrag\ScheduledDefrag";

    private const string RemoveCapabilityConsentDb =
        @"Remove-Item ""$env:ProgramData\Microsoft\Windows\CapabilityAccessManager\CapabilityConsentStorage.db*"" -Force";

    // The script's $stop list  processes killed before the settings.dat step.
    private static readonly string[] AppActionProcesses =
    {
        "AppActions", "CrossDeviceResume", "DesktopStickerEditorWin32Exe", "DiscoveryHubApp",
        "FESearchHost", "SearchHost", "SoftLandingTask", "TextInputHost",
        "VisualAssistExe", "WebExperienceHostApp", "WindowsBackupClient", "WindowsMigration",
    };

    private static readonly string CbsSettingsDat =
        NativeOps.Expand(@"%LocalAppData%\Packages\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\Settings\settings.dat");

    /// <summary>Optimize branch: camsvc stop + capability-DB wipe, CDPUserSvc=4, applet
    /// reg, defrag task off, consolelock 0, priority-notifications off, app actions.</summary>
    public static void ApplyAkariDefault()
    {
        StopCamAndWipeConsentDb();
        TweakAction.RegDword(CdpUserSvc, "Start", 4).Apply();

        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("ControlPanelOptimize.reg"), dollarFromQuestion: true);

        StopCamAndWipeConsentDb();

        NativeOps.DisableScheduledTask("ScheduledDefrag");
        NativeOps.RunTool("powercfg", "/setacvalueindex scheme_current sub_none consolelock 0");
        NativeOps.RunTool("powercfg", "/setdcvalueindex scheme_current sub_none consolelock 0");

        DisablePriorityNotifications();

        NativeOps.StopProcess(AppActionProcesses);
        System.Threading.Thread.Sleep(2000);
        NativeOps.RegLoadImportUnload(@"HKLM\Settings", CbsSettingsDat,
            NativeOps.LoadWindowsData("AppActions.reg"));
    }

    /// <summary>Default branch: mirror  CDPUserSvc=2, default applet reg, defrag task on,
    /// consolelock 1, CloudStore Current deleted, CBS settings.dat deleted.</summary>
    public static void ApplyWindowsDefault()
    {
        StopCamAndWipeConsentDb();
        TweakAction.RegDword(CdpUserSvc, "Start", 2).Apply();

        NativeOps.ImportRegContent(NativeOps.LoadWindowsData("ControlPanelDefault.reg"), dollarFromQuestion: true);

        StopCamAndWipeConsentDb();

        NativeOps.EnableScheduledTask("ScheduledDefrag");
        NativeOps.RunTool("powercfg", "/setacvalueindex scheme_current sub_none consolelock 1");
        NativeOps.RunTool("powercfg", "/setdcvalueindex scheme_current sub_none consolelock 1");

        TweakAction.RegDeleteKey(CloudStoreCurrent).Apply();

        NativeOps.StopProcess(AppActionProcesses);
        System.Threading.Thread.Sleep(2000);
        try { File.Delete(CbsSettingsDat); } catch { }
    }

    /// <summary>Live-state probe: CDPUserSvc Start (4 = Akari, 2 = Windows) plus the
    /// ScheduledDefrag task state. Reads on every page visit, like the gaming toggles.</summary>
    public static ControlPanelPreset ReadPreset()
    {
        int? cdp = RegRead.Dword(CdpUserSvc, "Start");
        bool? defragDisabled = ReadDefragDisabled();

        if (cdp is 4 && defragDisabled is true)
            return ControlPanelPreset.AkariDefault;
        if (cdp is 2 && defragDisabled is false)
            return ControlPanelPreset.WindowsDefault;
        return ControlPanelPreset.Mixed;
    }

    private static void StopCamAndWipeConsentDb()
    {
        NativeOps.StopService("camsvc");
        NativeOps.RunAsTrustedInstaller(RemoveCapabilityConsentDb);
    }

    /// <summary>The script's "disable set priority notifications" step: for every
    /// {guid}$ subkey under CloudStore \Current, write the fixed quiet-hours
    /// priority-only Data blob. Skipped silently when the key is absent.</summary>
    private static void DisablePriorityNotifications()
    {
        List<string> guids = new();
        try
        {
            using RegistryKey? current = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\CloudStore\Store\DefaultAccount\Current");
            if (current is null)
                return;
            foreach (string name in current.GetSubKeyNames())
            {
                if (name.StartsWith('{') && name.EndsWith("}$", StringComparison.OrdinalIgnoreCase))
                    guids.Add(name.Split('$')[0]);
            }
        }
        catch { return; }

        guids = guids.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (guids.Count == 0)
            return;

        var sb = new System.Text.StringBuilder("Windows Registry Editor Version 5.00\n\n; disable set priority notifications\n");
        foreach (string guid in guids)
        {
            sb.Append("\n\n[HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\CloudStore\\Store\\DefaultAccount\\Current\\");
            sb.Append(guid);
            sb.Append("$windows.data.donotdisturb.quiethoursprofile$quiethoursprofilelist\\windows.data.donotdisturb.quiethoursprofile$microsoft.quiethoursprofile.priorityonly]\n");
            foreach (string line in PriorityOnlyDataLines)
                sb.Append(line).Append('\n');
        }
        NativeOps.ImportRegContent(sb.ToString());
    }

    // Verbatim Data payload from script 22 (lines 1593-1612), minus the trailing "$"
    // placeholders  those are already literal in the appended key path above.
    private static readonly string[] PriorityOnlyDataLines =
    {
        "\"Data\"=hex(3):43,42,01,00,0A,02,01,00,2A,06,DF,B8,B4,CC,06,2A,2B,0E,D0,03,\\",
        "  43,42,01,00,C2,0A,01,CD,14,06,02,05,00,00,01,01,02,00,03,01,04,00,CC,32,12,\\",
        "  05,28,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,53,00,63,\\",
        "  00,72,00,65,00,65,00,6E,00,53,00,6B,00,65,00,74,00,63,00,68,00,5F,00,38,00,\\",
        "  77,00,65,00,6B,00,79,00,62,00,33,00,64,00,38,00,62,00,62,00,77,00,65,00,21,\\",
        "  00,41,00,70,00,70,00,29,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,\\",
        "  00,2E,00,57,00,69,00,6E,00,64,00,6F,00,77,00,73,00,41,00,6C,00,61,00,72,00,\\",
        "  6D,00,73,00,5F,00,38,00,77,00,65,00,6B,00,79,00,62,00,33,00,64,00,38,00,62,\\",
        "  00,62,00,77,00,65,00,21,00,41,00,70,00,70,00,31,4D,00,69,00,63,00,72,00,6F,\\",
        "  00,73,00,6F,00,66,00,74,00,2E,00,58,00,62,00,6F,00,78,00,41,00,70,00,70,00,\\",
        "  5F,00,38,00,77,00,65,00,6B,00,79,00,62,00,33,00,64,00,38,00,62,00,62,00,77,\\",
        "  00,65,00,21,00,4D,00,69,00,63,00,72,00,6F,00,73,00,6F,00,66,00,74,00,2E,00,\\",
        "  58,00,62,00,6F,00,78,00,41,00,70,00,70,00,2D,4D,00,69,00,63,00,72,00,6F,00,\\",
        "  73,00,6F,00,66,00,74,00,2E,00,58,00,62,00,6F,00,78,00,47,00,61,00,6D,00,69,\\",
        "  00,6E,00,67,00,4F,00,76,00,65,00,72,00,6C,00,61,00,79,00,5F,00,38,00,77,00,\\",
        "  65,00,6B,00,79,00,62,00,33,00,64,00,38,00,62,00,62,00,77,00,65,00,21,00,41,\\",
        "  00,70,00,70,00,29,57,00,69,00,6E,00,64,00,6F,00,77,00,73,00,2E,00,53,00,79,\\",
        "  00,73,00,74,00,65,00,6D,00,2E,00,4E,00,65,00,61,00,72,00,53,00,68,00,61,00,\\",
        "  72,00,65,00,45,00,78,00,70,00,65,00,72,00,69,00,65,00,6E,00,63,00,65,00,52,\\",
        "  00,65,00,63,00,65,00,69,00,76,00,65,00,00,00,00,00",
    };

    /// <returns>true when the ScheduledDefrag task is disabled, false when enabled,
    /// null when the task cannot be queried.</returns>
    private static bool? ReadDefragDisabled()
    {
        try
        {
            string list = NativeOps.RunToolCapture("schtasks",
                $"/query /tn \"{DefragTask}\" /fo LIST /v");
            foreach (string line in list.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("Status:", StringComparison.OrdinalIgnoreCase))
                {
                    string status = trimmed["Status:".Length..].Trim();
                    if (status.Contains("Disabled", StringComparison.OrdinalIgnoreCase))
                        return true;
                    if (status.Contains("Ready", StringComparison.OrdinalIgnoreCase) ||
                        status.Contains("Running", StringComparison.OrdinalIgnoreCase) ||
                        status.Contains("Queued", StringComparison.OrdinalIgnoreCase))
                        return false;
                    return null;
                }
            }
            return null;
        }
        catch { return null; }
    }
}
