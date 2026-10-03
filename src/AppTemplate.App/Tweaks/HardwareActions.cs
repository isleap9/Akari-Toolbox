using System.IO;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native ports of Ultimate "7 Hardware": DPI scaling with matched mouse curves
/// (combobox), background polling-rate cap (toggle), polling tests, controller
/// overclock, monitor/bufferbloat/build-guide links.
/// </summary>
public static class HardwareActions
{
    public static readonly string[] ScalingOptions =
        { "100%", "125%", "150%", "175%", "200%", "225%", "250%", "300%", "350%" };

    private readonly record struct ScalingLevel(int LogPixels, bool Enhance, string X, string Y);

    private static readonly ScalingLevel[] ScalingLevels =
    [
        new(96, false,
            "0000000000000000c0cc0c00000000008099190000000000406626000000000000333300000000000",
            "0000000000000000000038000000000000007000000000000000a800000000000000e00000000000"),
        new(120, true,
            "00000000000000000000100000000000000020000000000000003000000000000000400000000000",
            "00000000000000000000380000000000000070000000000000A800000000000000E0000000000000"),
        new(144, true,
            "0000000000000000303313000000000060662600000000009099390000000000C0CC4C0000000000",
            "0000000000000000000038000000000000007000000000000000A800000000000000E0000000000000"),
        new(168, true,
            "00000000000000006066160000000000C0CC2C000000000020334300000000008099590000000000",
            "0000000000000000000038000000000000007000000000000000A800000000000000E0000000000000"),
        new(192, true,
            "00000000000000009099190000000000203333000000000B0CC4C000000000040666600000000000",
            "0000000000000000000038000000000000007000000000000000A800000000000000E0000000000000"),
        new(216, true,
            "0000000000000000C0CC1C0000000000809939000000000040665600000000000033730000000000",
            "0000000000000000000038000000000000007000000000000000A800000000000000E0000000000000"),
        new(240, true,
            "00000000000000000000200000000000000040000000000000006000000000000000800000000000",
            "0000000000000000000038000000000000007000000000000000A800000000000000E0000000000000"),
        new(288, true,
            "00000000000000006066260000000000C0CC4C000000000020337300000000008099990000000000",
            "0000000000000000000038000000000000007000000000000000A800000000000000E0000000000000"),
        new(336, true,
            "0000000000000000C0CC2C000000000080995900000000004066860000000000003B300000000000",
            "0000000000000000000038000000000000007000000000000000A800000000000000E0000000000000"),
    ];

    public static void ApplyScaling(int index)
    {
        var level = ScalingLevels[Math.Clamp(index, 0, ScalingLevels.Length - 1)];
        const string mouse = @"HKCU\Control Panel\Mouse";
        const string desktop = @"HKCU\Control Panel\Desktop";

        TweakAction.RegString(mouse, "MouseSensitivity", "10").Apply();
        TweakAction.RegString(mouse, "MouseSpeed", level.Enhance ? "1" : "0").Apply();
        TweakAction.RegString(mouse, "MouseThreshold1", level.Enhance ? "6" : "0").Apply();
        TweakAction.RegString(mouse, "MouseThreshold2", level.Enhance ? "10" : "0").Apply();
        TweakAction.RegBinaryHex(mouse, "SmoothMouseXCurve", level.X).Apply();
        TweakAction.RegBinaryHex(mouse, "SmoothMouseYCurve", level.Y).Apply();
        TweakAction.RegDword(desktop, "Win8DpiScaling", 1).Apply();
        TweakAction.RegDword(desktop, "LogPixels", level.LogPixels).Apply();
        TweakAction.RegDword(desktop, "EnablePerProcessSystemDPI", level.Enhance ? 1 : 0).Apply();
    }

    public static int ReadScaling()
    {
        int? logPixels = RegRead.Dword(@"HKCU\Control Panel\Desktop", "LogPixels");
        if (logPixels is null)
            return 0;
        for (int i = 0; i < ScalingLevels.Length; i++)
        {
            if (ScalingLevels[i].LogPixels == logPixels.Value)
                return i;
        }
        return 0;
    }

    public static void SetPollingCapOff() =>
        TweakAction.RegDword(@"HKCU\Control Panel\Mouse", "RawMouseThrottleEnabled", 0).Apply();

    public static void SetPollingCapDefault() =>
        TweakAction.RegDeleteValue(@"HKCU\Control Panel\Mouse", "RawMouseThrottleEnabled").Apply();

    public static bool ReadPollingCapOff() =>
        RegRead.Dword(@"HKCU\Control Panel\Mouse", "RawMouseThrottleEnabled") is 0;

    public static void OpenMousePollingTest() =>
        TweakAction.Open("https://cpstest.org/polling-rate-test").Apply();

    public static void InstallControllerOverclock()
    {
        string zip = Path.Combine(NativeOps.SystemRootTemp, "hidusbf.zip");
        string dir = Path.Combine(NativeOps.ProgramFilesX86, "hidusbf");
        NativeOps.Download("https://github.com/LordOfMice/hidusbf/raw/refs/heads/master/hidusbf.zip", zip);
        NativeOps.Unzip(zip, dir);

        string inf = Path.Combine(dir, "DRIVER", "HIDUSBF_AS.INF");
        TweakAction.Run("rundll32.exe", $"setupapi.dll,InstallHinfSection DefaultInstall 132 {inf}").Apply();

        string setup = Path.Combine(dir, "DRIVER", "Setup.exe");
        string work = Path.Combine(dir, "DRIVER");
        NativeOps.CreateShortcut(NativeOps.DesktopShortcut("Setup"), setup, work);
        NativeOps.CreateShortcut(NativeOps.StartMenuShortcut("Setup"), setup, work);
    }

    public static void InstallControllerPollingTest()
    {
        string dir = Path.Combine(NativeOps.ProgramFilesX86, "Polling");
        string exe = Path.Combine(dir, "Polling.exe");
        NativeOps.Download("https://github.com/cakama3a/Polling/releases/download/1.3.1.4/Polling.exe", exe);
        NativeOps.CreateShortcut(NativeOps.DesktopShortcut("Polling"), exe, dir);
        NativeOps.CreateShortcut(NativeOps.StartMenuShortcut("Polling"), exe, dir);
        NativeOps.Launch(exe);
    }

    public static void OpenMonitorOptimization() =>
        TweakAction.Open("https://www.testufo.com/framerates#count=6&background=none&pps=1920").Apply();

    public static void OpenBufferbloatTest() =>
        TweakAction.Open("https://www.waveform.com/tools/bufferbloat").Apply();

    public static void OpenPcBuildGuide() =>
        TweakAction.Open("https://pcpartpicker.com/user/fr33thy/saved").Apply();
}
