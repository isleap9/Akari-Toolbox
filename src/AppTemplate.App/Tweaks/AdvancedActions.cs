using System.IO;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native ports of Ultimate "8 Advanced" scripts 116 and 1819 (Defender,
/// Firewall, Spectre, DEP, download warnings, MMAgent, ReBar, per-app launchers,
/// MPO, flip modes, ULPS, WHQL bypass, keyboard shortcuts, shell search, NVMe).
/// Script 17 (Services) lives in <see cref="AdvancedActions.Services.cs"/> with
/// its 273-entry service table. Safe-boot flows re-enter this exe via --arg.
/// </summary>
public static partial class AdvancedActions
{
    public static readonly string[] ReBarOptions = { "Default (Per-Game Whitelist)", "Force On", "Force Off" };
    public static readonly string[] FlipOptions = { "FSO (Default)", "FSE" };
    public static readonly string[] ComposedFlipOptions = { "Independent Flip (Default)", "Composed Independent Flip" };

    private const string FirewallPublic = @"HKLM\System\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile";
    private const string FirewallStandard = @"HKLM\System\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile";
    private const string MemoryManagement = @"HKLM\SYSTEM\ControlSet001\Control\Session Manager\Memory Management";
    private const string GameConfigStore = @"HKCU\System\GameConfigStore";

    private static void Dword(string path, string name, int value) =>
        TweakAction.RegDword(path, name, value).Apply();

    private static void Sz(string path, string name, string value) =>
        TweakAction.RegString(path, name, value).Apply();

    private static void DelValue(string path, string name) =>
        TweakAction.RegDeleteValue(path, name).Apply();

    private static void DelKey(string path) =>
        TweakAction.RegDeleteKey(path).Apply();

    //  1 Defender (full disable / enable, safe-boot payload in C#) 

    public static void ApplyDefenderDisable()
    {
        Dword(@"HKCU\SOFTWARE\Microsoft\Edge\SmartScreenEnabled", "", 0);
        Dword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
        ArmSafeBoot("*defenderdisable", "--defender-disable");
    }

    public static void ApplyDefenderEnable()
    {
        Dword(@"HKCU\SOFTWARE\Microsoft\Edge\SmartScreenEnabled", "", 1);
        Dword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 1);
        ArmSafeBoot("*defenderenable", "--defender-enable");
    }

    private static void ArmSafeBoot(string runOnceName, string arg)
    {
        string? exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            throw new InvalidOperationException("Could not resolve the app path for the RunOnce entry.");
        TweakAction.RegString(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce", runOnceName, $"\"{exe}\" {arg}").Apply();
        NativeOps.RunTool("bcdedit", "/set {current} safeboot minimal");
        NativeOps.Restart();
    }

    public static void FinishDefenderDisable()
    {
        foreach (var entry in DefenderDisableTable)
            WindowsActions.ApplyDefenderEntry(entry);

        NativeOps.StopProcess("smartscreen");
        NativeOps.RunAsTrustedInstaller(
            @"Move-Item -Path 'C:\Windows\System32\smartscreen.exe' -Destination 'C:\Windows\smartscreen.exe' -Force");
        DelKey(@"HKLM\SOFTWARE\Classes\Directory\shellex\ContextMenuHandlers\EPP");
        DelKey(@"HKLM\SOFTWARE\Classes\Drive\shellex\ContextMenuHandlers\EPP");
        DelKey(@"HKLM\SOFTWARE\Classes\*\shellex\ContextMenuHandlers\EPP");
        DelValue(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "SecurityHealth");

        NativeOps.RunTool("bcdedit", "/deletevalue allowedinmemorysettings");
        NativeOps.RunTool("bcdedit", "/deletevalue isolatedcontext");
        NativeOps.RunTool("bcdedit", "/deletevalue hypervisorlaunchtype");
        DelValue(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity");

        NativeOps.RunTool("bcdedit", "/deletevalue {current} safeboot");
        NativeOps.Restart();
    }

    public static void FinishDefenderEnable()
    {
        foreach (var entry in DefenderEnableTable)
            WindowsActions.ApplyDefenderEntry(entry);

        NativeOps.RunAsTrustedInstaller(
            @"Move-Item -Path 'C:\Windows\smartscreen.exe' -Destination 'C:\Windows\System32\smartscreen.exe' -Force");
        const string epp = "{09A47860-11B0-4DA5-AFA5-26D86198A780}";
        Sz(@"HKLM\SOFTWARE\Classes\Directory\shellex\ContextMenuHandlers\EPP", "", epp);
        Sz(@"HKLM\SOFTWARE\Classes\Drive\shellex\ContextMenuHandlers\EPP", "", epp);
        Sz(@"HKLM\SOFTWARE\Classes\*\shellex\ContextMenuHandlers\EPP", "", epp);
        TweakAction.RegExpandString(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
            "SecurityHealth", @"%windir%\system32\SecurityHealthSystray.exe").Apply();

        NativeOps.RunTool("bcdedit", "/deletevalue {current} safeboot");
        NativeOps.Restart();
    }

    private static readonly WindowsActions.DefenderEntry[] DefenderDisableTable =
    [
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Real-Time Protection", "DisableAsyncScanOnOpen", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Spynet", "SpyNetReporting", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Spynet", "SubmitSamplesConsent", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Features", "TamperProtection", "dword", "4"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access", "EnableControlledFolderAccess", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender Security Center\Notifications", "DisableEnhancedNotifications", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender Security Center\Virus and threat protection", "NoActionNotificationDisabled", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender Security Center\Virus and threat protection", "SummaryNotificationDisabled", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender Security Center\Virus and threat protection", "FilesBlockedNotificationDisabled", "dword", "1"),
        new(@"HKCU\SOFTWARE\Microsoft\Windows Defender Security Center\Account protection", "DisableNotifications", "dword", "1"),
        new(@"HKCU\SOFTWARE\Microsoft\Windows Defender Security Center\Account protection", "DisableDynamiclockNotifications", "dword", "1"),
        new(@"HKCU\SOFTWARE\Microsoft\Windows Defender Security Center\Account protection", "DisableWindowsHelloNotifications", "dword", "1"),
        new(@"HKLM\System\ControlSet001\Services\SharedAccess\Epoch", "Epoch", "dword", "1231"),
        new(@"HKLM\System\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile", "DisableNotifications", "dword", "1"),
        new(@"HKLM\System\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile", "DisableNotifications", "dword", "1"),
        new(@"HKLM\System\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile", "DisableNotifications", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender", "VerifiedAndReputableTrustModeEnabled", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender", "SmartLockerMode", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender", "PUAProtection", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Control\AppID\Configuration\SMARTLOCKER", "START_PENDING", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Control\AppID\Configuration\SMARTLOCKER", "ENABLED", "binary", "0000000000000000"),
        new(@"HKLM\System\ControlSet001\Control\CI\Policy", "VerifiedAndReputablePolicyState", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "sz", "Off"),
        new(@"HKCU\SOFTWARE\Microsoft\Edge\SmartScreenEnabled", "", "dword", "0"),
        new(@"HKCU\SOFTWARE\Microsoft\Edge\SmartScreenPuaEnabled", "", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "CaptureThreatWindow", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyMalicious", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyPasswordReuse", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyUnsafeApp", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "ServiceEnabled", "dword", "0"),
        new(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Control\Session Manager\kernel", "MitigationOptions", "binary", "222222000001000000000000000000000000000000000000"),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "ChangedInBootCycle", "delvalue", ""),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "WasEnabledBy", "delvalue", ""),
        new(@"HKLM\SYSTEM\CurrentControlSet\Control\Lsa", "RunAsPPL", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Control\CI\Config", "VulnerableDriverBlocklistEnable", "dword", "0"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WdNisSvc", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WinDefend", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\MDCoreSvc", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\wscsvc", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\webthreatdefsvc", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\webthreatdefusersvc", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\Sense", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\SecurityHealthService", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WdBoot", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WdFilter", "Start", "dword", "4"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WdNisDrv", "Start", "dword", "4"),
    ];

    private static readonly WindowsActions.DefenderEntry[] DefenderEnableTable =
    [
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Real-Time Protection", "DisableAsyncScanOnOpen", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Spynet", "SpyNetReporting", "dword", "2"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Spynet", "SubmitSamplesConsent", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Features", "TamperProtection", "dword", "5"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Windows Defender Exploit Guard\Controlled Folder Access", "EnableControlledFolderAccess", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender Security Center\Notifications", "DisableEnhancedNotifications", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender Security Center\Virus and threat protection", "NoActionNotificationDisabled", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender Security Center\Virus and threat protection", "SummaryNotificationDisabled", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender Security Center\Virus and threat protection", "FilesBlockedNotificationDisabled", "dword", "0"),
        new(@"HKCU\SOFTWARE\Microsoft\Windows Defender Security Center\Account protection", "DisableNotifications", "dword", "0"),
        new(@"HKCU\SOFTWARE\Microsoft\Windows Defender Security Center\Account protection", "DisableDynamiclockNotifications", "dword", "0"),
        new(@"HKCU\SOFTWARE\Microsoft\Windows Defender Security Center\Account protection", "DisableWindowsHelloNotifications", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Services\SharedAccess\Epoch", "Epoch", "dword", "1228"),
        new(@"HKLM\System\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile", "DisableNotifications", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile", "DisableNotifications", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile", "DisableNotifications", "dword", "0"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender", "VerifiedAndReputableTrustModeEnabled", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender", "SmartLockerMode", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender", "PUAProtection", "dword", "2"),
        new(@"HKLM\System\ControlSet001\Control\AppID\Configuration\SMARTLOCKER", "START_PENDING", "dword", "4"),
        new(@"HKLM\System\ControlSet001\Control\AppID\Configuration\SMARTLOCKER", "ENABLED", "binary", "0400000000000000"),
        new(@"HKLM\System\ControlSet001\Control\CI\Policy", "VerifiedAndReputablePolicyState", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", "sz", "Warn"),
        new(@"HKCU\SOFTWARE\Microsoft\Edge\SmartScreenEnabled", "", "dword", "1"),
        new(@"HKCU\SOFTWARE\Microsoft\Edge\SmartScreenPuaEnabled", "", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "CaptureThreatWindow", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyMalicious", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyPasswordReuse", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "NotifyUnsafeApp", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\WTDS\Components", "ServiceEnabled", "dword", "1"),
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender", "PUAProtection", "dword", "1"),
        new(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", "dword", "1"),
        new(@"HKLM\System\ControlSet001\Control\Session Manager\kernel", "MitigationOptions", "binary", "111111000001000000000000000000000000000000000000"),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "ChangedInBootCycle", "delvalue", ""),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", "dword", "1"),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "WasEnabledBy", "dword", "2"),
        new(@"HKLM\SYSTEM\CurrentControlSet\Control\Lsa", "RunAsPPL", "dword", "2"),
        new(@"HKLM\System\ControlSet001\Control\CI\Config", "VulnerableDriverBlocklistEnable", "dword", "1"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WdNisSvc", "Start", "dword", "3"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WinDefend", "Start", "dword", "2"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\MDCoreSvc", "Start", "dword", "2"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\wscsvc", "Start", "dword", "2"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\webthreatdefsvc", "Start", "dword", "3"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\webthreatdefusersvc", "Start", "dword", "2"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\Sense", "Start", "dword", "3"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\SecurityHealthService", "Start", "dword", "2"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WdBoot", "Start", "dword", "0"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WdFilter", "Start", "dword", "0"),
        new(@"HKLM\SYSTEM\ControlSet001\Services\WdNisDrv", "Start", "dword", "3"),
    ];

    public static bool ReadDefenderDisabled() =>
        RegRead.Dword(@"HKLM\SYSTEM\ControlSet001\Services\WinDefend", "Start") is 4;

    //  2 Firewall 

    public static void SetFirewallDisabled()
    {
        Dword(FirewallPublic, "EnableFirewall", 0);
        Dword(FirewallStandard, "EnableFirewall", 0);
    }

    public static void SetFirewallEnabled()
    {
        Dword(FirewallPublic, "EnableFirewall", 1);
        Dword(FirewallStandard, "EnableFirewall", 1);
    }

    public static bool ReadFirewallDisabled() =>
        RegRead.Dword(FirewallPublic, "EnableFirewall") is 0;

    //  3 Spectre / Meltdown 

    public static void SetSpectreDisabled()
    {
        Dword(MemoryManagement, "FeatureSettingsOverrideMask", 3);
        Dword(MemoryManagement, "FeatureSettingsOverride", 3);
    }

    public static void SetSpectreEnabled()
    {
        DelValue(MemoryManagement, "FeatureSettingsOverrideMask");
        DelValue(MemoryManagement, "FeatureSettingsOverride");
    }

    public static bool ReadSpectreDisabled() =>
        RegRead.Dword(MemoryManagement, "FeatureSettingsOverride") is 3 &&
        RegRead.Dword(MemoryManagement, "FeatureSettingsOverrideMask") is 3;

    //  4 Data Execution Prevention 

    public static void SetDepDisabled() => NativeOps.RunTool("bcdedit", "/set nx AlwaysOff");

    public static void SetDepEnabled() => NativeOps.RunTool("bcdedit", "/deletevalue nx");

    public static bool ReadDepDisabled()
    {
        string output = NativeOps.RunToolCapture("bcdedit", "/enum {current}");
        foreach (string line in output.Split('\n'))
        {
            string t = line.Trim();
            if (t.StartsWith("nx", StringComparison.OrdinalIgnoreCase))
                return t.Contains("AlwaysOff", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    //  5 File Download Security Warning 

    public static void SetDownloadWarningDisabled()
    {
        Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Internet Explorer\Security", "DisableSecuritySettingsCheck", 1);
        Dword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", "1806", 0);
        Dword(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", "1806", 0);
    }

    public static void SetDownloadWarningEnabled()
    {
        DelKey(@"HKLM\SOFTWARE\Policies\Microsoft\Internet Explorer");
        DelValue(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", "1806");
        Dword(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", "1806", 1);
    }

    public static bool ReadDownloadWarningDisabled() =>
        RegRead.Dword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", "1806") is 0;

    //  6 MMAgent Features 

    public static void MmAgentOff()
    {
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0);
        NativeOps.PowerShellCommand(
            "Disable-MMAgent -ApplicationLaunchPrefetching; Disable-MMAgent -ApplicationPreLaunch; " +
            "Set-MMAgent -MaxOperationAPIFiles 1; Disable-MMAgent -MemoryCompression; " +
            "Disable-MMAgent -OperationAPI; Disable-MMAgent -PageCombining");
    }

    public static void MmAgentDefault()
    {
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 3);
        NativeOps.PowerShellCommand(
            "Enable-MMAgent -ApplicationLaunchPrefetching; Enable-MMAgent -ApplicationPreLaunch; " +
            "Set-MMAgent -MaxOperationAPIFiles 512; Disable-MMAgent -MemoryCompression; " +
            "Enable-MMAgent -OperationAPI; Disable-MMAgent -PageCombining");
    }

    public static bool ReadMmAgentOff() =>
        RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher") is 0;

    //  7 ReBar Force (persisted index  driver-profile state has no reg marker) 

    public static void ApplyReBar(int index)
    {
        InstallNvidiaInspector();
        NativeOps.UnblockFolder(@"C:\ProgramData\NVIDIA Corporation\Drs");

        string nipFileName, nipContent;
        bool merge;
        if (index == 1) { nipFileName = "forceon.nip"; nipContent = ReBarForceOnNip; merge = true; }
        else if (index == 2) { nipFileName = "forceoff.nip"; nipContent = ReBarForceOffNip; merge = true; }
        else { nipFileName = "ReBarDefault.nip"; nipContent = NativeOps.LoadAdvancedData("ReBarDefault.nip"); merge = false; }

        string nip = Path.Combine(NativeOps.SystemRootTemp, nipFileName);
        NativeOps.WriteAllText(nip, nipContent);

        string inspector = InspectorExe;
        if (!File.Exists(inspector))
            throw new FileNotFoundException($"Profile Inspector installed but not found at {inspector}.");
        string args = merge ? $"-silentImport -mergeImport -silent {nip}" : $"-silentImport -silent {nip}";
        NativeOps.RunTool(inspector, args);
        NativeOps.Launch(inspector);
    }

    private static string InspectorExe => NativeOps.PackagePath(
        "Orbmu2k.nvidiaProfileInspector_Microsoft.Winget.Source_8wekyb3d8bbwe", "nvidiaProfileInspector.exe");

    private static void InstallNvidiaInspector()
    {
        NativeOps.WingetInstall("Orbmu2k.nvidiaProfileInspector",
            "Orbmu2k.nvidiaProfileInspector_Microsoft.Winget.Source_8wekyb3d8bbwe");
        string exe = InspectorExe;
        string dir = NativeOps.PackagePath("Orbmu2k.nvidiaProfileInspector_Microsoft.Winget.Source_8wekyb3d8bbwe");
        NativeOps.CreateShortcut(NativeOps.DesktopShortcut("Nvidia Profile Inspector"), exe, dir);
        NativeOps.CreateShortcut(NativeOps.StartMenuShortcut("Nvidia Profile Inspector"), exe, dir);
    }

    private const string ReBarForceOnNip = """
        <?xml version="1.0" encoding="utf-16"?>
        <ArrayOfProfile>
          <Profile>
            <ProfileName>Base Profile</ProfileName>
            <Executables/>
            <Settings>
              <ProfileSetting>
                <SettingNameInfo>rBAR - Enable</SettingNameInfo>
                <SettingID>983226</SettingID>
                <SettingValue>1</SettingValue>
                <ValueType>Dword</ValueType>
              </ProfileSetting>
            </Settings>
          </Profile>
        </ArrayOfProfile>
        """;

    private const string ReBarForceOffNip = """
        <?xml version="1.0" encoding="utf-16"?>
        <ArrayOfProfile>
          <Profile>
            <ProfileName>Base Profile</ProfileName>
            <Executables/>
            <Settings>
              <ProfileSetting>
                <SettingNameInfo>rBAR - Enable</SettingNameInfo>
                <SettingID>983226</SettingID>
                <SettingValue>0</SettingValue>
                <ValueType>Dword</ValueType>
              </ProfileSetting>
            </Settings>
          </Profile>
        </ArrayOfProfile>
        """;

    //  8/9/10 per-app affinity & priority launchers 

    public static string? PickApp() => NativeOps.PickFile("Select launcher / game / shortcut / exe");

    public static void LaunchSmtHtOff(string file)
    {
        int n = Environment.ProcessorCount;
        var chars = new char[n];
        for (int i = 0; i < n; i++)
            chars[i] = (i % 2 == 1) ? '1' : '0';
        long mask = Convert.ToInt64(new string(chars), 2);
        NativeOps.RunTool("cmd.exe", $"/c start \"\" /affinity {mask:X} \"{file}\"");
    }

    public static void LaunchCore1Thread1Off(string file)
    {
        int n = Environment.ProcessorCount;
        long mask = ((1L << n) - 1) - 3;
        NativeOps.RunTool("cmd.exe", $"/c start \"\" /affinity {mask:X} \"{file}\"");
    }

    public static void LaunchWithPriority(string file, string priority) =>
        NativeOps.RunTool("cmd.exe", $"/c start \"\" /{priority} \"{file}\"");

    //  11 MPO 

    public static void MpoOn()
    {
        DelValue(@"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode");
        Sz(@"HKCU\Software\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings", "VRROptimizeEnable=0;SwapEffectUpgradeEnable=1;");
    }

    public static void MpoOff()
    {
        Dword(@"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode", 5);
        Sz(@"HKCU\Software\Microsoft\DirectX\UserGpuPreferences", "DirectXUserGlobalSettings", "VRROptimizeEnable=0;SwapEffectUpgradeEnable=0;");
    }

    public static bool ReadMpoOff() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\Windows\Dwm", "OverlayTestMode") is 5;

    //  12 Fullscreen Mode (FSO / FSE) 

    public static void ApplyFlipFso()
    {
        Dword(GameConfigStore, "GameDVR_DXGIHonorFSEWindowsCompatible", 0);
        Dword(GameConfigStore, "GameDVR_FSEBehaviorMode", 0);
        DelValue(GameConfigStore, "GameDVR_FSEBehavior");
        Dword(GameConfigStore, "GameDVR_HonorUserFSEBehaviorMode", 0);
    }

    public static void ApplyFlipFse()
    {
        Dword(GameConfigStore, "GameDVR_DXGIHonorFSEWindowsCompatible", 1);
        Dword(GameConfigStore, "GameDVR_FSEBehaviorMode", 2);
        Dword(GameConfigStore, "GameDVR_FSEBehavior", 2);
        Dword(GameConfigStore, "GameDVR_HonorUserFSEBehaviorMode", 1);
    }

    public static int ReadFlipMode() =>
        RegRead.Dword(GameConfigStore, "GameDVR_FSEBehaviorMode") is 2 ? 1 : 0;

    //  13 Hardware Composed Independent Flip 

    public static void ApplyComposedFlip(bool hcif)
    {
        if (hcif)
            Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler", "ForceFlipTrueImmediateMode", 1);
        else
            DelKey(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler");
    }

    public static int ReadComposedFlip() =>
        RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler", "ForceFlipTrueImmediateMode") is 1 ? 1 : 0;

    //  14 AMD ULPS 

    public static void SetUlps(bool off)
    {
        foreach (string key in NativeOps.DisplayClassSubkeys("CurrentControlSet"))
            Dword(key, "EnableUlps", off ? 0 : 1);
    }

    public static bool ReadUlpsOff()
    {
        foreach (string key in NativeOps.DisplayClassSubkeys("CurrentControlSet"))
            if (RegRead.Dword(key, "EnableUlps") is 0)
                return true;
        return false;
    }

    //  15 Driver WHQL Secure Boot Bypass 

    public static void SetWhqlBypass(bool on)
    {
        if (on)
            Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy", "WHQLSettings", 1);
        else
            DelValue(@"HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy", "WHQLSettings");
    }

    public static bool ReadWhqlBypass() =>
        RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy", "WHQLSettings") is 1;

    //  16 Keyboard Shortcuts 

    public static void SetKeyboardShortcutsOff()
    {
        Dword(@"HKLM\SYSTEM\ControlSet001\Services\hidserv", "Start", 4);
        Dword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoWinKeys", 1);
        Dword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "DisabledHotkeys", 1);
        TweakAction.RegBinaryHex(@"HKLM\SYSTEM\CurrentControlSet\Control\Keyboard Layout", "Scancode Map",
            "00000000000000000700000000005be000005ce000003800000038e00000010001000d0000000000").Apply();
    }

    public static void SetKeyboardShortcutsDefault()
    {
        Dword(@"HKLM\SYSTEM\ControlSet001\Services\hidserv", "Start", 3);
        DelValue(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoWinKeys");
        DelValue(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "DisabledHotkeys");
        DelValue(@"HKLM\SYSTEM\CurrentControlSet\Control\Keyboard Layout", "Scancode Map");
    }

    public static bool ReadKeyboardOff() =>
        RegRead.Dword(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoWinKeys") is 1;

    //  18 Start / Search / Shell / Mobsync 

    public static void ShellSearchOff()
    {
        string root = NativeOps.Expand("%SystemRoot%");
        foreach (string rel in new[]
        {
            @"SystemApps\Microsoft.Windows.Search_cw5n1h2txyewy",
            @"SystemApps\ShellExperienceHost_cw5n1h2txyewy",
            @"SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy",
            @"System32\mobsync.exe",
        })
        {
            string path = Path.Combine(root, rel);
            NativeOps.RunTool("takeown.exe", $"/f \"{path}\"");
            NativeOps.RunTool("icacls.exe", $"\"{path}\" /grant *S-1-3-4:F /t /q");
        }

        NativeOps.StopProcess("SearchApp", "SearchHost", "ShellExperienceHost", "StartMenuExperienceHost", "mobsync", "explorer");

        foreach (string rel in new[]
        {
            @"SystemApps\Microsoft.Windows.Search_cw5n1h2txyewy",
            @"SystemApps\ShellExperienceHost_cw5n1h2txyewy",
            @"SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy",
            @"System32\mobsync.exe",
        })
            NativeOps.RunTool("cmd.exe", $"/c move /y \"{Path.Combine(root, rel)}\" \"{root}\"");

        Dword(@"HKLM\SOFTWARE\Microsoft\PolicyManager\default\Search\DisableSearch", "value", 1);
        Dword(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search", "DisableSearch", 1);
        Dword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0);
        Dword(@"HKLM\SYSTEM\ControlSet001\Services\WSearch", "Start", 4);

        NativeOps.RunTool("taskkill", "/F /IM explorer.exe");
        NativeOps.Launch("explorer.exe");
        System.Threading.Thread.Sleep(15000);
        NativeOps.RunTool("sc.exe", "stop WSearch");
    }

    public static void ShellSearchDefault()
    {
        string root = NativeOps.Expand("%SystemRoot%");
        (string moved, string dest)[] moves =
        [
            (@"Microsoft.Windows.Search_cw5n1h2txyewy", @"SystemApps"),
            (@"ShellExperienceHost_cw5n1h2txyewy", @"SystemApps"),
            (@"Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy", @"SystemApps"),
            (@"mobsync.exe", @"System32"),
        ];
        foreach ((string moved, _) in moves)
        {
            string path = Path.Combine(root, moved);
            NativeOps.RunTool("takeown.exe", $"/f \"{path}\"");
            NativeOps.RunTool("icacls.exe", $"\"{path}\" /grant *S-1-3-4:F /t /q");
        }
        foreach ((string moved, string dest) in moves)
            NativeOps.RunTool("cmd.exe", $"/c move /y \"{Path.Combine(root, moved)}\" \"{Path.Combine(root, dest)}\"");

        Dword(@"HKLM\SOFTWARE\Microsoft\PolicyManager\default\Search\DisableSearch", "value", 0);
        DelKey(@"HKLM\SOFTWARE\Policies\Microsoft\Windows\Windows Search");
        DelValue(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode");
        Dword(@"HKLM\SYSTEM\ControlSet001\Services\WSearch", "Start", 2);

        NativeOps.RunTool("taskkill", "/F /IM explorer.exe");
        NativeOps.Launch("explorer.exe");
    }

    public static bool ReadShellSearchOff() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\PolicyManager\default\Search\DisableSearch", "value") is 1;

    //  19 NVMe Faster Driver 

    public static void NvmeFasterDriver()
    {
        const string overrides = @"HKLM\SYSTEM\CurrentControlSet\Policies\Microsoft\FeatureManagement\Overrides";
        foreach (string v in new[] { "735209102", "3244671118", "1853569164", "156965516" })
            Dword(overrides, v, 1);
        Sz(@"HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Network\{75416E63-5912-4DFA-AE8F-3EFACCAFFB14}", "", "Storage disks");
        Sz(@"HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal\{75416E63-5912-4DFA-AE8F-3EFACCAFFB14}", "", "Storage disks");
    }

    public static void NvmeDefaultDriver()
    {
        DelKey(@"HKLM\SYSTEM\CurrentControlSet\Policies\Microsoft");
        DelKey(@"HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Network\{75416E63-5912-4DFA-AE8F-3EFACCAFFB14}");
        DelKey(@"HKLM\SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal\{75416E63-5912-4DFA-AE8F-3EFACCAFFB14}");
    }

    public static bool ReadNvmeFaster() =>
        RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Policies\Microsoft\FeatureManagement\Overrides", "735209102") is 1;
}
