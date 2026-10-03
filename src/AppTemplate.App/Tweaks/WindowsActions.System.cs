using System.IO;
using System.Management;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native ports of Ultimate "6 Windows" scripts 2536 (device/adapter power,
/// IPv4 bindings, write cache, power plan, timer resolution, UAC, Defender,
/// Autoruns, Cleanup, Restore Point, Core Isolation) plus live-state probes.
/// </summary>
public static partial class WindowsActions
{
    //  25 Device Manager Power Savings & Wake 

    public static void DeviceManagerPowerOff()
    {
        foreach (string bus in new[] { "ACPI", "HID", "PCI", "USB" })
        {
            string root = $@"HKLM\SYSTEM\ControlSet001\Enum\{bus}";
            foreach (string dp in NativeOps.DescendantKeyPaths(root, "Device Parameters"))
            {
                Dword(dp, "EnhancedPowerManagementEnabled", 0);
                TweakAction.RegBinary(dp, "SelectiveSuspendEnabled", new byte[] { 0x00 }).Apply();
                Dword(dp, "SelectiveSuspendOn", 0);
                Dword(dp, "WaitWakeEnabled", 0);
            }
            foreach (string wdf in NativeOps.DescendantKeyPaths(root, "WDF"))
                Dword(wdf, "IdleInWorkingState", 0);
        }
    }

    public static void DeviceManagerPowerDefault()
    {
        foreach (string bus in new[] { "ACPI", "HID", "PCI", "USB" })
        {
            string root = $@"HKLM\SYSTEM\ControlSet001\Enum\{bus}";
            foreach (string dp in NativeOps.DescendantKeyPaths(root, "Device Parameters"))
            {
                DelValue(dp, "EnhancedPowerManagementEnabled");
                DelValue(dp, "SeleactiveSuspendEnabled");
                DelValue(dp, "SelectiveSuspendEnabled");
                DelValue(dp, "SelectiveSuspendOn");
                DelValue(dp, "WaitWakeEnabled");
            }
            foreach (string wdf in NativeOps.DescendantKeyPaths(root, "WDF"))
                DelValue(wdf, "IdleInWorkingState");
        }
    }

    /// <summary>Off leaves its marker values behind; Default deletes them.</summary>
    public static bool ReadDevicePowerOff()
    {
        foreach (string bus in new[] { "ACPI", "HID", "PCI", "USB" })
        {
            string root = $@"HKLM\SYSTEM\ControlSet001\Enum\{bus}";
            foreach (string dp in NativeOps.DescendantKeyPaths(root, "Device Parameters"))
            {
                if (RegRead.Dword(dp, "EnhancedPowerManagementEnabled") is not null ||
                    RegRead.Dword(dp, "SelectiveSuspendOn") is not null ||
                    RegRead.Dword(dp, "WaitWakeEnabled") is not null)
                    return true;
            }
            foreach (string wdf in NativeOps.DescendantKeyPaths(root, "WDF"))
            {
                if (RegRead.Dword(wdf, "IdleInWorkingState") is not null)
                    return true;
            }
        }
        return false;
    }

    //  26 Network Adapter Power Savings & Wake 

    private static readonly string[] NicPowerValues =
    [
        "AdvancedEEE", "*EEE", "EEELinkAdvertisement", "SipsEnabled", "ULPMode", "GigaLite",
        "EnableGreenEthernet", "PowerSavingMode", "S5WakeOnLan", "*WakeOnMagicPacket",
        "*ModernStandbyWoLMagicPacket", "*WakeOnPattern", "WakeOnLink",
    ];

    private const string NicClass = "{4d36e972-e325-11ce-bfc1-08002be10318}";

    public static void NetworkAdapterPowerOff()
    {
        foreach (string key in NativeOps.NumberedClassSubkeys(NicClass, "ControlSet001"))
        {
            Dword(key, "PnPCapabilities", 24);
            foreach (string v in NicPowerValues)
                Sz(key, v, "0");
        }
    }

    public static void NetworkAdapterPowerDefault()
    {
        foreach (string key in NativeOps.NumberedClassSubkeys(NicClass, "ControlSet001"))
        {
            DelValue(key, "PnPCapabilities");
            foreach (string v in NicPowerValues)
                DelValue(key, v);
        }
    }

    public static bool ReadNetworkPowerOff()
    {
        foreach (string key in NativeOps.NumberedClassSubkeys(NicClass, "ControlSet001"))
        {
            if (RegRead.Dword(key, "PnPCapabilities") is 24)
                return true;
        }
        return false;
    }

    //  27 Network IPv4 Only (WMI binding settings  no script, no cmdlet) 

    private static readonly string[] Ipv4OffBindings =
        ["ms_lldp", "ms_lltdio", "ms_implat", "ms_rspndr", "ms_tcpip6", "ms_server", "ms_msclient", "ms_pacer"];

    private static readonly string[] Ipv4OnBindings =
        ["ms_lldp", "ms_lltdio", "ms_implat", "ms_tcpip", "ms_rspndr", "ms_tcpip6", "ms_server", "ms_msclient", "ms_pacer"];

    public static void NetworkIpv4Only() => SetNetBindings(Ipv4OffBindings, enable: false);

    public static void NetworkBindingsDefault() => SetNetBindings(Ipv4OnBindings, enable: true);

    private static void SetNetBindings(string[] componentIds, bool enable)
    {
        using var searcher = new ManagementObjectSearcher(
            @"\\.\ROOT\StandardCimv2", "SELECT ComponentID, Enabled FROM MSFT_NetAdapterBindingSettingData");
        bool touched = false;
        foreach (ManagementObject binding in searcher.Get().Cast<ManagementObject>())
        {
            string? id = binding["ComponentID"]?.ToString();
            if (id is null || !componentIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                continue;
            try
            {
                binding["Enabled"] = enable;
                binding.Put();
                touched = true;
            }
            catch { }
        }
        if (!touched)
            throw new InvalidOperationException("No adapter bindings could be changed.");
    }

    public static bool ReadIpv4Only()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"\\.\ROOT\StandardCimv2",
                "SELECT ComponentID, Enabled FROM MSFT_NetAdapterBindingSettingData WHERE ComponentID = 'ms_tcpip6'");
            bool any = false;
            foreach (ManagementObject binding in searcher.Get().Cast<ManagementObject>())
            {
                any = true;
                if (binding["Enabled"] is bool enabled && enabled)
                    return false;
            }
            return any;
        }
        catch { return false; }
    }

    //  28 Write Cache Buffer Flushing 

    public static void WriteCacheFlushingOff()
    {
        foreach (string bus in new[] { "SCSI", "NVME" })
        {
            string root = $@"HKLM\SYSTEM\ControlSet001\Enum\{bus}";
            foreach (string dp in NativeOps.DescendantKeyPaths(root, "Device Parameters"))
                Dword($@"{dp}\Disk", "CacheIsPowerProtected", 1);
        }
    }

    public static void WriteCacheFlushingDefault()
    {
        foreach (string bus in new[] { "SCSI", "NVME" })
        {
            string root = $@"HKLM\SYSTEM\ControlSet001\Enum\{bus}";
            foreach (string dp in NativeOps.DescendantKeyPaths(root, "Device Parameters"))
                DelKey($@"{dp}\Disk");
        }
    }

    public static bool ReadWriteCacheOff()
    {
        foreach (string bus in new[] { "SCSI", "NVME" })
        {
            string root = $@"HKLM\SYSTEM\ControlSet001\Enum\{bus}";
            foreach (string dp in NativeOps.DescendantKeyPaths(root, "Device Parameters"))
            {
                if (RegRead.Dword($@"{dp}\Disk", "CacheIsPowerProtected") is 1)
                    return true;
            }
        }
        return false;
    }

    //  29 Power Plan 

    private const string UltimateScheme = "99999999-9999-9999-9999-999999999999";

    public static void PowerPlanOn()
    {
        NativeOps.RunTool("powercfg", $"/duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 {UltimateScheme}");
        NativeOps.RunTool("powercfg", $"/SETACTIVE {UltimateScheme}");
        NativeOps.DeleteAllOtherPowerSchemes();

        NativeOps.RunTool("powercfg", "/hibernate off");
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled", 0);
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabledDefault", 0);
        Dword(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowLockOption", 0);
        Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings", "ShowSleepOption", 0);
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0);
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1);

        foreach ((string sub, string setting, string value) in PowerPlanIndices)
        {
            NativeOps.RunTool("powercfg", $"/setacvalueindex {UltimateScheme} {sub} {setting} {value}");
            NativeOps.RunTool("powercfg", $"/setdcvalueindex {UltimateScheme} {sub} {setting} {value}");
        }

        foreach (string key in HiddenPowerSettingKeys)
            Dword(key, "Attributes", 0);

        NativeOps.RunTool("powercfg", "/requestsoverride PROCESS \"chrome.exe\" DISPLAY SYSTEM AWAYMODE");
        NativeOps.RunTool("powercfg", "/requestsoverride PROCESS \"Discord.exe\" DISPLAY SYSTEM AWAYMODE");

        NativeOps.Launch("powercfg.cpl");
    }

    public static void PowerPlanDefault()
    {
        NativeOps.RunTool("powercfg", "-restoredefaultschemes");
        NativeOps.RunTool("powercfg", "/requestsoverride PROCESS \"chrome.exe\"");
        NativeOps.RunTool("powercfg", "/requestsoverride PROCESS \"Discord.exe\"");

        NativeOps.RunTool("powercfg", "/hibernate on");
        DelValue(@"HKLM\SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabled");
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Power", "HibernateEnabledDefault", 1);
        DelKey(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Explorer\FlyoutMenuSettings");
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 1);
        DelKey(@"HKLM\SYSTEM\CurrentControlSet\Control\Power\PowerThrottling");

        foreach (string key in HiddenPowerSettingKeys)
            Dword(key, "Attributes", 1);

        NativeOps.Launch("powercfg.cpl");
    }

    public static bool ReadPowerPlanOn()
    {
        string output = NativeOps.RunToolCapture("powercfg", "/getactivescheme");
        return output.Contains(UltimateScheme, StringComparison.OrdinalIgnoreCase);
    }

    private static readonly (string sub, string setting, string value)[] PowerPlanIndices =
    [
        ("0012ee47-9041-4b5d-9b77-535fba8b1442", "6738e2c4-e8a5-4a42-b16a-e040e769756e", "0x00000000"),
        ("0d7dbae2-4294-402a-ba8e-26777e8488cd", "309dce9b-bef4-4119-9921-a851fb12f0f4", "001"),
        ("19cbb8fa-5279-450e-9fac-8a3d5fedd0c1", "12bbebe6-58d6-4636-95bb-3217ef867c1a", "000"),
        ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "29f6c1db-86da-48c5-9fdb-f2b67b1f44da", "0x00000000"),
        ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "94ac6d29-73ce-41a6-809f-6363ba21b47e", "000"),
        ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "9d7815a6-7ee4-497e-8888-515a05f02364", "0x00000000"),
        ("238c9fa8-0aad-41ed-83f4-97be242c8f20", "bd3b718a-0680-4d9d-8ab2-e1d2b4ac806d", "000"),
        ("2a737441-1930-4402-8d77-b2bebba308a3", "0853a681-27c8-4100-a2fd-82013e970683", "0x00000000"),
        ("2a737441-1930-4402-8d77-b2bebba308a3", "48e6b7a6-50f5-4782-a5d4-53bb8f07e226", "000"),
        ("2a737441-1930-4402-8d77-b2bebba308a3", "d4e98f31-5ffe-4ce1-be31-1b38b384c009", "000"),
        ("4f971e89-eebd-4455-a8de-9e59040e7347", "a7066653-8d6c-40a8-910e-a1f54b84c7e5", "002"),
        ("501a4d13-42af-4429-9fd1-a8218c268e20", "ee12f906-d277-404b-b6da-e5fa1a576df5", "000"),
        ("54533251-82be-4824-96c1-47b60b740d00", "893dee8e-2bef-41e0-89c6-b55d0929964c", "0x00000064"),
        ("54533251-82be-4824-96c1-47b60b740d00", "94d3a615-a899-4ac5-ae2b-e4d8f634367f", "001"),
        ("54533251-82be-4824-96c1-47b60b740d00", "bc5038f7-23e0-4960-96da-33abaf5935ec", "0x00000064"),
        ("54533251-82be-4824-96c1-47b60b740d00", "0cc5b647-c1df-4637-891a-dec35c318583", "0x00000064"),
        ("54533251-82be-4824-96c1-47b60b740d00", "ea062031-0e34-4ff1-9b6d-eb1059334028", "0x00000064"),
        ("7516b95f-f776-4464-8c53-06167f40cc99", "3c0bc021-c8a8-4e07-a973-6b14cbcb2b7e", "600"),
        ("7516b95f-f776-4464-8c53-06167f40cc99", "aded5e82-b909-4619-9949-f5d71dac0bcb", "0x00000064"),
        ("7516b95f-f776-4464-8c53-06167f40cc99", "f1fbfde2-a960-4165-9f88-50667911ce96", "0x00000064"),
        ("7516b95f-f776-4464-8c53-06167f40cc99", "fbd9aa66-9553-4097-ba44-ed6e9d65eab8", "000"),
        ("9596fb26-9850-41fd-ac3e-f7c3c00afd4b", "10778347-1370-4ee0-8bbd-33bdacaade49", "001"),
        ("9596fb26-9850-41fd-ac3e-f7c3c00afd4b", "34c7b99f-9a6d-4b3c-8dc7-b6693b78cef4", "000"),
        ("44f3beca-a7c0-460e-9df2-bb8b99e0cba6", "3619c3f2-afb2-4afc-b0e9-e7fef372de36", "002"),
        ("c763b4ec-0e50-4b6b-9bed-2b92a6ee884e", "7ec1751b-60ed-4588-afb5-9819d3d7790", "003"),
        ("f693fb01-e858-4f00-b20f-f30e12ac06d6", "191f65b5-d45c-4a4f-8aae-1ab8bfd980e6", "001"),
        ("e276e160-7cb0-43c6-b20b-73f5dce39954", "a1662ab2-9d34-4e53-ba8b-2639b9e20857", "003"),
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "5dbb7c9f-38e9-40d2-9749-4f8a0e9f640f", "000"),
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "637ea02f-bbcb-4015-8e2c-a1c7b9c0b546", "000"),
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "8183ba9a-e910-48da-8769-14ae6dc1170a", "0x00000000"),
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "9a66d8d7-4ff7-4ef9-b5a2-5a326ca2a469", "0x00000000"),
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "bcded951-187b-4d05-bccc-f7e51960c258", "000"),
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "d8742dcb-3e6a-4b3c-b3fe-374623cdcf06", "000"),
        ("e73a048d-bf27-4f12-9731-8b2076e8891f", "f3c5027d-cd16-4930-aa6b-90db844a8f00", "0x00000000"),
        ("de830923-a562-41af-a086-e3a2c6bad2da", "13d09884-f74e-474a-a852-b6bde8ad03a8", "0x00000064"),
        ("de830923-a562-41af-a086-e3a2c6bad2da", "e69653ca-cf7f-4f05-a73-cb833fa90ad4", "0x00000000"),
    ];

    private static readonly string[] HiddenPowerSettingKeys =
    [
        @"HKLM\System\ControlSet001\Control\Power\PowerSettings\2a737441-1930-4402-8d77-b2bebba308a3\0853a681-27c8-4100-a2fd-82013e970683",
        @"HKLM\System\ControlSet001\Control\Power\PowerSettings\2a737441-1930-4402-8d77-b2bebba308a3\d4e98f31-5ffe-4ce1-be31-1b38b384c009",
        @"HKLM\System\ControlSet001\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\0cc5b647-c1df-4637-891a-dec35c318583",
        @"HKLM\System\ControlSet001\Control\Power\PowerSettings\54533251-82be-4824-96c1-47b60b740d00\ea062031-0e34-4ff1-9b6d-eb1059334028",
    ];

    //  30 Timer Resolution (compiled service) 

    private const string TimerServiceName = "Set Timer Resolution Service";

    public static void TimerResolutionOn()
    {
        string cs = @"C:\Windows\SetTimerResolutionService.cs";
        string exe = @"C:\Windows\SetTimerResolutionService.exe";
        NativeOps.WriteAllText(cs, NativeOps.LoadWindowsData("SetTimerResolutionService.cs"));
        NativeOps.CompileWithCsc(cs, exe);
        try { File.Delete(cs); } catch { }
        NativeOps.StopAndDeleteService(TimerServiceName);
        NativeOps.CreateAndStartService(TimerServiceName, exe);
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "GlobalTimerResolutionRequests", 1);
        NativeOps.Launch("taskmgr.exe");
    }

    public static void TimerResolutionDefault()
    {
        NativeOps.StopAndDeleteService(TimerServiceName);
        try { File.Delete(@"C:\Windows\SetTimerResolutionService.exe"); } catch { }
        DelValue(@"HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\kernel", "GlobalTimerResolutionRequests");
        NativeOps.Launch("taskmgr.exe");
    }

    public static bool ReadTimerResolutionOn()
    {
        var (baseKey, sub) = RegistryPath.Resolve(
            $@"HKLM\SYSTEM\CurrentControlSet\Services\{TimerServiceName}");
        using Microsoft.Win32.RegistryKey? key = baseKey.OpenSubKey(sub);
        return key is not null;
    }

    //  31 UAC 

    public static void SetUacOff() =>
        Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 0);

    public static void SetUacDefault() =>
        Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", 1);

    public static bool ReadUacOff() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA") is 0;

    //  32 Core Isolation 

    public static void OpenCoreIsolation()
    {
        TweakAction.Launch("msinfo32").Apply();
        TweakAction.Open("windowsdefender://coreisolation/").Apply();
    }

    //  33 Defender Optimize (safe-boot RunOnce into this exe) 

    private static readonly string[] DefenderScheduledTasks =
    [
        @"Microsoft\Windows\ExploitGuard\ExploitGuard MDM policy Refresh",
        @"Microsoft\Windows\Windows Defender\Windows Defender Cache Maintenance",
        @"Microsoft\Windows\Windows Defender\Windows Defender Cleanup",
        @"Microsoft\Windows\Windows Defender\Windows Defender Scheduled Scan",
        @"Microsoft\Windows\Windows Defender\Windows Defender Verification",
    ];

    /// <summary>Normal-boot half: SmartScreen/AppHost values, task states, then
    /// arm RunOnce + safe boot. The payload runs via --defender-* on re-entry.</summary>
    public static void ApplyDefenderOptimize()
    {
        Dword(@"HKCU\SOFTWARE\Microsoft\Edge\SmartScreenEnabled", "", 0);
        Dword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 0);
        foreach (string task in DefenderScheduledTasks)
            NativeOps.RunTool("schtasks", $"/Change /TN \"{task}\" /Disable");
        ArmDefenderSafeBoot("--defender-optimize");
    }

    public static void ApplyDefenderDefault()
    {
        Dword(@"HKCU\SOFTWARE\Microsoft\Edge\SmartScreenEnabled", "", 1);
        Dword(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", 1);
        foreach (string task in DefenderScheduledTasks)
            NativeOps.RunTool("schtasks", $"/Change /TN \"{task}\" /Enable");
        ArmDefenderSafeBoot("--defender-default");
    }

    private static void ArmDefenderSafeBoot(string arg)
    {
        string? exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe))
            throw new InvalidOperationException("Could not resolve the app path for the RunOnce entry.");
        TweakAction.RegString(
            @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
            arg == "--defender-optimize" ? "*defenderoptimize" : "*defenderdefault",
            $"\"{exe}\" {arg}").Apply();
        NativeOps.RunTool("bcdedit", "/set {current} safeboot minimal");
        NativeOps.Restart();
    }

    /// <summary>Safe-boot payload: the Defender .ps1 reg tables, ported 1:1 
    /// every command runs as TrustedInstaller and directly, like the script.</summary>
    public static void FinishDefenderOptimize()
    {
        foreach (var entry in DefenderOptimizeTable)
            ApplyDefenderEntry(entry);
        NativeOps.RunTool("bcdedit", "/deletevalue {current} safeboot");
        NativeOps.Restart();
    }

    public static void FinishDefenderDefault()
    {
        foreach (var entry in DefenderDefaultTable)
            ApplyDefenderEntry(entry);
        NativeOps.RunTool("bcdedit", "/deletevalue {current} safeboot");
        NativeOps.Restart();
    }

    internal readonly record struct DefenderEntry(string Key, string? Name, string Kind, string Data);

    internal static void ApplyDefenderEntry(DefenderEntry entry)
    {
        string tiCommand = entry.Kind switch
        {
            "dword" => $"reg add \"{entry.Key}\" /v \"{entry.Name}\" /t REG_DWORD /d \"{entry.Data}\" /f",
            "sz" => $"reg add \"{entry.Key}\" /v \"{entry.Name}\" /t REG_SZ /d \"{entry.Data}\" /f",
            "binary" => $"reg add \"{entry.Key}\" /v \"{entry.Name}\" /t REG_BINARY /d \"{entry.Data}\" /f",
            "delvalue" => $"reg delete \"{entry.Key}\" /v \"{entry.Name}\" /f",
            _ => throw new InvalidOperationException($"Unknown Defender entry kind {entry.Kind}."),
        };
        NativeOps.RunAsTrustedInstaller(tiCommand);

        // ...and directly, mirroring the script's second pass.
        try
        {
            switch (entry.Kind)
            {
                case "dword":
                    Dword(entry.Key, entry.Name!, int.Parse(entry.Data));
                    break;
                case "sz":
                    if (entry.Name!.Length == 0)
                        Sz(entry.Key, "", entry.Data);
                    else
                        Sz(entry.Key, entry.Name, entry.Data);
                    break;
                case "binary":
                    TweakAction.RegBinaryHex(entry.Key, entry.Name!, entry.Data).Apply();
                    break;
                case "delvalue":
                    DelValue(entry.Key, entry.Name!);
                    break;
            }
        }
        catch { }
    }

    private static readonly DefenderEntry[] DefenderOptimizeTable =
    [
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Real-Time Protection", "DisableRealtimeMonitoring", "dword", "0"),
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
        new(@"HKLM\System\ControlSet001\Control\Session Manager\kernel", "MitigationOptions", "binary", "222222000002000000020000000000000000000000000000"),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "ChangedInBootCycle", "delvalue", ""),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "WasEnabledBy", "delvalue", ""),
        new(@"HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", "delvalue", ""),
        new(@"HKLM\SYSTEM\CurrentControlSet\Control\Lsa", "RunAsPPL", "dword", "0"),
        new(@"HKLM\System\ControlSet001\Control\CI\Config", "VulnerableDriverBlocklistEnable", "dword", "0"),
    ];

    private static readonly DefenderEntry[] DefenderDefaultTable =
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
        new(@"HKLM\SOFTWARE\Microsoft\Windows Defender", "PUAProtection", "dword", "1"),
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
        new(@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\AppHost", "EnableWebContentEvaluation", "dword", "1"),
        new(@"HKLM\System\ControlSet001\Control\Session Manager\kernel", "MitigationOptions", "binary", "111111000001000000000000000000000000000000000000"),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "ChangedInBootCycle", "delvalue", ""),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", "dword", "1"),
        new(@"HKLM\System\ControlSet001\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "WasEnabledBy", "dword", "2"),
        new(@"HKLM\SYSTEM\CurrentControlSet\Control\Lsa", "RunAsPPL", "dword", "2"),
        new(@"HKLM\System\ControlSet001\Control\CI\Config", "VulnerableDriverBlocklistEnable", "dword", "1"),
    ];

    public static bool ReadDefenderOptimized() =>
        RegRead.Dword(@"HKLM\SOFTWARE\Microsoft\Windows Defender\Spynet", "SpyNetReporting") is 0;

    //  34 Autoruns check 

    public static void AutorunsCheck()
    {
        RestorePointActions.CreateRestorePoint("beforeautoruns");

        foreach (string key in new[]
        {
            @"HKCU\Software\Microsoft\Windows\CurrentVersion\RunNotification",
            @"HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce",
            @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run",
            @"HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce",
            @"HKLM\Software\Microsoft\Windows\CurrentVersion\Run",
            @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce",
            @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
        })
        {
            DelKey(key);
            var (bk, sub) = RegistryPath.Resolve(key);
            using Microsoft.Win32.RegistryKey _ = bk.CreateSubKey(sub, writable: true);
        }

        foreach (string folder in new[]
        {
            NativeOps.Expand(@"%AppData%\Microsoft\Windows\Start Menu\Programs\Startup"),
            NativeOps.Expand(@"%ProgramData%\Microsoft\Windows\Start Menu\Programs\StartUp"),
        })
        {
            NativeOps.DeleteDirectory(folder);
            Directory.CreateDirectory(folder);
        }

        RemoveNonMicrosoftTaskCache();

        NativeOps.WingetInstall("Microsoft.Sysinternals.Autoruns",
            "Microsoft.Sysinternals.Autoruns_Microsoft.Winget.Source_8wekyb3d8bbwe");
        string pkg = "Microsoft.Sysinternals.Autoruns_Microsoft.Winget.Source_8wekyb3d8bbwe";
        string exe = NativeOps.PackagePath(pkg, "Autoruns64.exe");
        string dir = NativeOps.PackagePath(pkg);
        NativeOps.CreateShortcut(NativeOps.DesktopShortcut("Autoruns"), exe, dir);
        NativeOps.CreateShortcut(NativeOps.StartMenuShortcut("Autoruns"), exe, dir);
        NativeOps.Launch(exe);
    }

    private static void RemoveNonMicrosoftTaskCache()
    {
        const string tree = @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree";
        var (baseKey, sub) = RegistryPath.Resolve(tree);
        using Microsoft.Win32.RegistryKey? root = baseKey.OpenSubKey(sub);
        if (root is not null)
        {
            foreach (string child in root.GetSubKeyNames())
            {
                if (child.Equals("Microsoft", StringComparison.OrdinalIgnoreCase))
                    continue;
                try { baseKey.DeleteSubKeyTree($@"{sub}\{child}", throwOnMissingSubKey: false); }
                catch { }
            }
        }

        string tasksPath = NativeOps.Expand(@"%SystemRoot%\System32\Tasks");
        if (Directory.Exists(tasksPath))
        {
            foreach (string entry in Directory.GetFileSystemEntries(tasksPath))
            {
                if (Path.GetFileName(entry).Equals("Microsoft", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    if (File.Exists(entry))
                        File.Delete(entry);
                    else
                        Directory.Delete(entry, recursive: true);
                }
                catch { }
            }
        }
    }

    //  35 Cleanup 

    public static void Cleanup()
    {
        NativeOps.EmptyDirectory(NativeOps.Expand(@"%USERPROFILE%\AppData\Local\Temp"));
        NativeOps.EmptyDirectory(NativeOps.Expand(@"%SystemDrive%\Windows\Temp"));
        foreach (string path in new[]
        {
            NativeOps.Expand(@"%SystemDrive%\DumpStack.log"),
            NativeOps.Expand(@"%SystemDrive%\Output.txt"),
            NativeOps.Expand(@"%SystemDrive%\PerfLogs"),
            NativeOps.Expand(@"%SystemDrive%\Windows.old"),
            NativeOps.Expand(@"%SystemDrive%\XboxGames"),
            NativeOps.Expand(@"%SystemDrive%\inetpub"),
        })
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
            NativeOps.DeleteDirectory(path);
        }
        NativeOps.RunTool("sc.exe", "stop wuauserv");
        NativeOps.EmptyDirectory(NativeOps.Expand(@"%SystemDrive%\Windows\SoftwareDistribution"));
        NativeOps.Launch("cleanmgr.exe");
    }

    //  36 Restore Point 

    public static void CreateRestorePoint()
    {
        RestorePointActions.CreateRestorePoint("backup");
        NativeOps.Launch(NativeOps.Expand(@"%SystemRoot%\system32\control.exe"), "sysdm.cpl,,4");
        NativeOps.Launch("rstrui");
    }
}
