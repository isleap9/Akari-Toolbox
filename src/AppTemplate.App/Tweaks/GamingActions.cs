using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native rewrites of every batch-driven action on the Gaming Tweaks page. The old app
/// shelled out to the PostInstall repo's .bat files (network optimize, AMD Dwords,
/// AUTO DSCP, ); everything here does the same registry / netsh /
/// nvidia-smi work directly  no .bat or .exe from that repo is ever executed, so the
/// user can drop in their own replacements under C:\PostInstall later. The Ultimate
/// "5 Graphics" scripts live next door in <see cref="GraphicsActions"/>.
/// </summary>
public static class GamingActions
{
    private const string Scheduler = @"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler";
    private const string Nvlddmkm = @"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm";

    //  Registry write helpers 

    private static void Dword(string path, string name, int value) =>
        TweakAction.RegDword(path, name, value).Apply();

    private static void Sz(string path, string name, string value) =>
        TweakAction.RegString(path, name, value).Apply();

    private static void Bin(string path, string name, string hex) =>
        TweakAction.RegBinaryHex(path, name, hex).Apply();

    // 
    // Disable Preemption (NVIDIA)   ported 1:1 from the old GamingTweaks.cs
    // 

    public static bool ReadPreemption() =>
        RegRead.Dword(Scheduler, "EnablePreemption") is 0 ||
        RegRead.Dword(Nvlddmkm, "DisablePreemption") is 1;

    public static void SetPreemption(bool disable)
    {
        if (disable)
        {
            Dword(Scheduler, "EnablePreemption", 0);
            Dword(Nvlddmkm, "DisablePreemption", 1);
            Dword(Nvlddmkm, "DisableCudaContextPreemption", 1);
            Dword(Nvlddmkm, "EnableCEPreemption", 0);
            Dword(Nvlddmkm, "DisablePreemptionOnS3S4", 1);
            Dword(Nvlddmkm, "ComputePreemption", 0);
            AkariToolState.Save("DisablePreemption");
        }
        else
        {
            Dword(Scheduler, "EnablePreemption", 1);
            Dword(Nvlddmkm, "DisablePreemption", 0);
            Dword(Nvlddmkm, "DisableCudaContextPreemption", 0);
            Dword(Nvlddmkm, "EnableCEPreemption", 1);
            Dword(Nvlddmkm, "DisablePreemptionOnS3S4", 0);
            Dword(Nvlddmkm, "ComputePreemption", 1);
            AkariToolState.Clear("DisablePreemption");
        }
    }

    // 
    // Network Optimization  native rewrite of the two Network bats.
    // The forced reboot the bats performed is intentionally NOT ported; the UI
    // tells the user a reboot may be required instead.
    // 

    public static bool ReadNetworkOptimization() => AkariToolState.Has("NetworkOptimization");

    private static readonly (string Name, string Kind, string Value)[] NicValues =
    {
        ("*DeviceSleepOnDisconnect", "s", "0"),
        ("*EEE", "s", "0"),
        ("*FlowControl", "s", "0"),
        ("*IPChecksumOffloadIPv4", "s", "3"),
        ("*InterruptModeration", "s", "0"),
        ("*LsoV2IPv4", "s", "1"),
        ("*LsoV2IPv6", "s", "1"),
        ("*NumRssQueues", "s", "2"),
        ("*PMARPOffload", "s", "1"),
        ("*PMNSOffload", "s", "1"),
        ("*PriorityVLANTag", "s", "1"),
        ("*RSS", "s", "1"),
        ("*WakeOnMagicPacket", "s", "0"),
        ("AutoPowerSaveModeEnabled", "s", "0"),
        ("*WakeOnPattern", "s", "0"),
        ("*ReceiveBuffers", "s", "2048"),
        ("*TransmitBuffers", "s", "2048"),
        ("*TCPChecksumOffloadIPv4", "s", "3"),
        ("*TCPChecksumOffloadIPv6", "s", "3"),
        ("*UDPChecksumOffloadIPv4", "s", "3"),
        ("*UDPChecksumOffloadIPv6", "s", "3"),
        ("DMACoalescing", "s", "0"),
        ("EEELinkAdvertisement", "s", "0"),
        ("EeePhyEnable", "s", "0"),
        ("ITR", "s", "0"),
        ("ReduceSpeedOnPowerDown", "s", "0"),
        ("PowerDownPll", "s", "0"),
        ("WaitAutoNegComplete", "s", "0"),
        ("WakeOnLink", "s", "0"),
        ("WakeOnSlot", "s", "0"),
        ("WakeUpModeCap", "s", "0"),
        ("AdvancedEEE", "s", "0"),
        ("EnableGreenEthernet", "s", "0"),
        ("GigaLite", "s", "0"),
        ("PnPCapabilities", "d", "24"),
        ("PowerSavingMode", "s", "0"),
        ("S5WakeOnLan", "s", "0"),
        ("SavePowerNowEnabled", "s", "0"),
        ("ULPMode", "s", "0"),
        ("WolShutdownLinkSpeed", "s", "2"),
        ("LogLinkStateEvent", "s", "16"),
        ("WakeOnMagicPacketFromS5", "s", "0"),
        ("Ultra Low Power Mode", "s", "Disabled"),
        ("System Idle Power Saver", "s", "Disabled"),
        ("Selective Suspend", "s", "Disabled"),
        ("Selective Suspend Idle Timeout", "s", "60"),
        ("Link Speed Battery Saver", "s", "Disabled"),
        ("*SelectiveSuspend", "s", "0"),
        ("EnablePME", "s", "0"),
        ("TxIntDelay", "s", "0"),
        ("TxDelay", "s", "0"),
        ("EnableModernStandby", "s", "0"),
        ("*ModernStandbyWoLMagicPacket", "s", "0"),
        ("EnableLLI", "s", "1"),
        ("*SSIdleTimeout", "s", "60"),
    };

    private static readonly string[] NetshCommands =
    {
        "int tcp set global dca=enabled",
        "int tcp set global netdma=enabled",
        "interface isatap set state disabled",
        "int tcp set global timestamps=disabled",
        "int tcp set global rss=enabled",
        "int tcp set global nonsackrttresiliency=disabled",
        "int tcp set global initialRto=2000",
        "int tcp set supplemental template=custom icw=10",
        "interface ip set interface ethernet currenthoplimit=64",
        "int ip set global taskoffload=enabled",
    };

    public static void ApplyNetworkOptimization()
    {
        // NIC advanced-property values: same rule as the bat  only touch values that
        // already exist on adapters that expose *SpeedDuplex.
        using RegistryKey? root = Registry.LocalMachine.OpenSubKey(
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}");
        if (root is not null)
        {
            foreach (string sub in root.GetSubKeyNames())
            {
                using RegistryKey? key = root.OpenSubKey(sub, writable: true);
                if (key is null || key.GetValue("*SpeedDuplex") is null)
                    continue;

                foreach ((string name, string kind, string value) in NicValues)
                {
                    if (key.GetValue(name) is null)
                        continue;
                    key.SetValue(name, value,
                        kind == "d" ? RegistryValueKind.DWord : RegistryValueKind.String);
                }
            }
        }

        // The bat's powershell one-liner (escape-hatch cmdlet, invoked inline).
        NativeOps.PowerShellCommand(
            "disable-netadapterbinding -name '*' -componentid " +
            "vmware_bridge, ms_lldp, ms_lltdio, ms_implat, ms_tcpip6, ms_rspndr, ms_server, ms_msclient");

        foreach (string cmd in NetshCommands)
            NativeOps.RunTool("netsh.exe", cmd);

        AkariToolState.Save("NetworkOptimization");
    }

    public static void RevertNetworkOptimization()
    {
        // Old revert bat ran `netsh winsock reset` under MinSudo; elevated is enough here.
        NativeOps.RunTool("netsh.exe", "winsock reset");
        AkariToolState.Clear("NetworkOptimization");
    }

    // 
    // Game Mode + Game DVR  carved out of the fso toggle's key set
    // (AkariTweakCatalog fso), leaving fso itself untouched (D-01).
    // HKCU user keys go through the interactive-user hive (OpenRealHkcu),
    // because the app runs elevated and plain HKCU would be the wrong hive.
    // 

    private const string GameBarUser = @"Software\Microsoft\GameBar";
    private const string GameStoreUser = @"System\GameConfigStore";
    private const string GameDvrUser = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR";

    /// <summary>True when Game Mode is enabled for the interactive user.</summary>
    public static bool ReadGameMode() =>
        (AkariToolState.RealHkcuDword(GameBarUser, "AllowAutoGameMode") ?? 1) == 1 &&
        (AkariToolState.RealHkcuDword(GameBarUser, "AutoGameModeEnabled") ?? 1) == 1;

    /// <summary>Enables or disables Game Mode, with write-through toggle memory.</summary>
    public static void SetGameMode(bool enable)
    {
        if (enable)
        {
            AkariToolState.SetRealHkcuDword(GameBarUser, "AllowAutoGameMode", 1);
            AkariToolState.SetRealHkcuDword(GameBarUser, "AutoGameModeEnabled", 1);
            AkariToolState.Save("GameMode");
        }
        else
        {
            AkariToolState.SetRealHkcuDword(GameBarUser, "AllowAutoGameMode", 0);
            AkariToolState.SetRealHkcuDword(GameBarUser, "AutoGameModeEnabled", 0);
            AkariToolState.Clear("GameMode");
        }
    }

    /// <summary>True when Game DVR capture is disabled (the optimized state).</summary>
    public static bool ReadGameDvr() =>
        (AkariToolState.RealHkcuDword(GameStoreUser, "GameDVR_Enabled") ?? 1) == 0 &&
        (AkariToolState.RealHkcuDword(GameDvrUser, "AppCaptureEnabled") ?? 1) == 0;

    /// <summary>Disables or restores Game DVR capture, with write-through toggle memory.</summary>
    public static void SetGameDvr(bool disable)
    {
        const string policy = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR";
        const string service = @"HKLM\SYSTEM\CurrentControlSet\Services\BcastDVRUserService";
        if (disable)
        {
            AkariToolState.SetRealHkcuDword(GameStoreUser, "GameDVR_Enabled", 0);
            AkariToolState.SetRealHkcuDword(GameDvrUser, "AppCaptureEnabled", 0);
            TweakAction.RegDword(policy, "AllowGameDVR", 0).Apply();
            TweakAction.RegDword(service, "Start", 4).Apply();
            AkariToolState.Save("GameDvr");
        }
        else
        {
            AkariToolState.SetRealHkcuDword(GameStoreUser, "GameDVR_Enabled", 1);
            AkariToolState.SetRealHkcuDword(GameDvrUser, "AppCaptureEnabled", 1);
            TweakAction.RegDeleteValue(policy, "AllowGameDVR").Apply();
            TweakAction.RegDword(service, "Start", 3).Apply();
            AkariToolState.Clear("GameDvr");
        }
    }

    /// <summary>
    /// Live NIC footprint for the network optimization: same adapter rule as
    /// <see cref="ApplyNetworkOptimization"/> (only adapters exposing *SpeedDuplex,
    /// only values that already exist). netsh globals and binding removals are
    /// uncaptured side effects, disclosed in the catalog's RevertsBy copy.
    /// </summary>
    public static IReadOnlyList<(string Path, string Name)> EnumerateNetworkFootprint()
    {
        var result = new List<(string Path, string Name)>();
        try
        {
            using RegistryKey? root = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}");
            if (root is null)
                return result;

            foreach (string sub in root.GetSubKeyNames())
            {
                using RegistryKey? key = root.OpenSubKey(sub, writable: false);
                if (key is null || key.GetValue("*SpeedDuplex") is null)
                    continue;

                string path = $@"HKLM\SYSTEM\CurrentControlSet\Control\Class\{{4d36e972-e325-11ce-bfc1-08002be10318}}\{sub}";
                foreach ((string name, _, _) in NicValues)
                {
                    if (key.GetValue(name) is not null)
                        result.Add((path, name));
                }
            }
        }
        catch
        {
            // Footprint enumeration is best-effort; capture journals what it can.
        }
        return result;
    }

    // 
    // Misc dropdowns
    // 

    public static readonly long[] SvcHostValues = { 380000, 4194304, 8388608, 16777216, 33554432, 67108864, 134217728, 268435456 };

    public static void ApplySvcHost(long kb) =>
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB", (int)kb);

    public static int? ReadSvcHost() =>
        RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Control", "SvcHostSplitThresholdInKB");

    public static readonly int[] Win32Values = { 38, 42, 40, 22, 6 };

    public static void ApplyWin32Priority(int value) =>
        Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation", value);

    public static int? ReadWin32Priority() =>
        RegRead.Dword(@"HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl", "Win32PrioritySeparation");

    // 
    // AUTO DSCP & FSE  native rewrite of the interactive bat:
    // profile name prompt  exe picker  QoS policy  FSE question  GpuPreference.
    // 

    /// <returns>true when a policy was written; false when the user cancelled.</returns>
    public static bool AutoDscpFse()
    {
        string? profile = NativeOps.Prompt(
            "Enter a profile name for the QoS policy (letters and digits only):", "AUTO DSCP");
        if (profile is null)
            return false;
        if (!Regex.IsMatch(profile, "^[0-9a-zA-Z]+$"))
            throw new ArgumentException("The profile name must contain only letters and digits.");

        string? exe = NativeOps.PickFile("Select the main game executable",
            "Executables (*.exe)|*.exe|All Files (*.*)|*.*");
        if (exe is null)
            return false;

        const string qosRoot = @"HKLM\SOFTWARE\Policies\Microsoft\Windows\QoS";
        string qos = $@"{qosRoot}\{profile}";
        Sz(qos, "Version", "1.0");
        Sz(qos, "Application Name", exe);
        Sz(qos, "Protocol", "*");
        Sz(qos, "Local Port", "*");
        Sz(qos, "Local IP", "*");
        Sz(qos, "Local IP Prefix Length", "*");
        Sz(qos, "Remote Port", "*");
        Sz(qos, "Remote IP", "*");
        Sz(qos, "Remote IP Prefix Length", "*");
        Sz(qos, "DSCP Value", "46");
        Sz(qos, "Throttle Rate", "-1");

        if (AkariToolbox.Helpers.NativeMessageBox.Confirm($"Enable exclusive fullscreen for:\n\n{exe}?", "AUTO DSCP & FSE"))
            Sz(@"HKCU\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers",
                exe, "~ DISABLEDXMAXIMIZEDWINDOWEDMODE HIGHDPIAWARE");

        // Always written, same as the bat's FSENO fall-through.
        Sz(@"HKCU\SOFTWARE\Microsoft\DirectX\UserGpuPreferences", exe, "GpuPreference=2;");
        return true;
    }
}

