using System.IO;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native port of Ultimate "1 Check" (scripts 16) plus the Akari-Tool Check tab:
/// BIOS guidance, OCCT install-and-run, drive/RAM/GPU checklists and benches.
/// Downloads come from FR33THY's GitHub release at runtime (the established
/// FilesUrl pattern); guidance dialogs are plain OS message boxes.
/// </summary>
public static class CheckActions
{
    private const string FilesUrl =
        "https://github.com/FR33THYFR33THY/Ultimate/releases/download/Files/";

    private const string OcctPackageId = "OCBase.OCCT.Personal";
    private const string OcctProductCode = "OCBase.OCCT.Personal_Microsoft.Winget.Source_8wekyb3d8bbwe";

    public const string BiosTips =
        "UPDATE BIOS & OPTIMIZE SETTINGS\n" +
        "\nINTEL CPU\n- ENABLE ram profile (XMP DOCP EXPO)\n- DISABLE c-state (K CHIPS ONLY)\n- ENABLE resizable bar (REBAR C.A.M)\n" +
        "\nAMD CPU\n- ENABLE ram profile (XMP DOCP EXPO)\n- ENABLE precision boost overdrive (PBO)\n- ENABLE resizable bar (REBAR C.A.M)\n" +
        "\nDISABLE unused features (BT/WIFI/IGPU/ETC)\n" +
        "DISABLE driver installer software (Armory Crate / MSI Utility / Gigabyte Update / Asrock Utility)\n" +
        "MAX pump and set fans to performance";

    public const string PcGuidance =
        "DRIVES\n- Keep drives at least 10% free\n- Check drive errors and device health\n" +
        "\nRAM\n- Check RAM profile is enabled\n- Verify RAM is in the correct slots\n- Confirm there is no mismatch in RAM modules\n- At least two RAM sticks (dual channel) is ideal\n" +
        "\nGPU\n- Check Video Bus is at maximum\n- Check Resizable BAR is enabled\n- Verify monitor cable is connected to the GPU\n- Confirm GPU is in the top PCIe motherboard slot\n" +
        "\nTEST\nRun a CPU, RAM & GPU stress test to check for errors.\nKeep an eye on temps and WHEA errors during this test.";

    public const string RamGuidance =
        "RAM CHECK\n- Check RAM profile is enabled\n- Verify RAM is in the correct slots\n" +
        "- Confirm there is no mismatch in RAM modules\n- At least two RAM sticks (dual channel) is ideal";

    public const string GpuGuidance =
        "GPU CHECK\n- Check Video Bus is at maximum\n- Check Resizable BAR is enabled\n" +
        "- Verify monitor cable is connected to the GPU\n- Confirm GPU is in the top PCIe motherboard slot\n- Running multiple graphics cards is not recommended";

    public const string BenchGuidance =
        "Run a stress test and watch temps, errors and WHEA errors.\n" +
        "Errors should not be ignored  they can lead to stutters, corrupted Windows, " +
        "poor performance, black/blue screens, input lag and shutdowns.";

    /// <summary>Passwordless reg + motherboard web search. Returns true when the
    /// user chose to restart into BIOS now.</summary>
    public static bool RunBiosCheck()
    {
        TweakAction.RegDword(
            @"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device",
            "DevicePasswordLessBuildVersion", 0).Apply();

        string board = NativeOps.BaseBoardProduct();
        if (!string.IsNullOrWhiteSpace(board))
            NativeOps.WebSearch(board);

        var answer = AkariToolbox.Helpers.NativeMessageBox.Confirm(
            BiosTips + "\n\nRestart to BIOS now?",
            "BIOS Settings");
        return answer;
    }

    /// <summary>Blocking: winget OCCT install, shortcuts, launch.</summary>
    public static void InstallAndRunOcct()
    {
        NativeOps.WingetInstall(OcctPackageId, OcctProductCode);

        string exe = NativeOps.PackagePath(OcctProductCode, "OCCT.exe");
        if (!File.Exists(exe))
            throw new FileNotFoundException($"OCCT installed but not found at {exe}.");

        NativeOps.CreateShortcut(
            NativeOps.DesktopShortcut("OCCT"), exe,
            Path.GetDirectoryName(exe));
        NativeOps.CreateShortcut(
            NativeOps.StartMenuShortcut("OCCT"), exe,
            Path.GetDirectoryName(exe));
        NativeOps.Launch(exe);
    }

    /// <summary>Live free-space summary for every fixed drive.</summary>
    public static string DriveSpaceSummary()
    {
        var lines = new List<string>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.Fixed || !drive.IsReady || drive.TotalSize == 0)
                    continue;
                double pct = (double)drive.AvailableFreeSpace / drive.TotalSize * 100;
                lines.Add($"{drive.Name.TrimEnd('\\')} Free space = {pct:0.0}%");
            }
            catch { }
        }
        return lines.Count > 0 ? string.Join("\n", lines) : "No fixed drives found.";
    }

    public static void OpenMyComputer() => NativeOps.Launch("explorer.exe", "shell:MyComputerFolder");

    /// <summary>Blocking: download + launch CPU-Z.</summary>
    public static void DownloadAndRunCpuZ()
    {
        string exe = Path.Combine(NativeOps.SystemRootTemp, "cpuz.exe");
        NativeOps.Download(FilesUrl + "cpuz.exe", exe);
        NativeOps.Launch(exe);
    }

    /// <summary>Blocking: download + launch GPU-Z.</summary>
    public static void DownloadAndRunGpuZ()
    {
        string exe = Path.Combine(NativeOps.SystemRootTemp, "gpuz.exe");
        NativeOps.Download(FilesUrl + "gpuz.exe", exe);
        NativeOps.Launch(exe);
    }

    /// <summary>Blocking: download + launch the portable OCCT bench.</summary>
    public static void DownloadAndRunOcctBench()
    {
        string exe = Path.Combine(NativeOps.SystemRootTemp, "occt.exe");
        NativeOps.Download(FilesUrl + "occt.exe", exe);
        NativeOps.Launch(exe);
    }

    /// <summary>Blocking: download + extract + launch FurMark.</summary>
    public static void DownloadAndRunFurMark()
    {
        string zip = Path.Combine(NativeOps.SystemRootTemp, "furmark.zip");
        string dir = Path.Combine(NativeOps.SystemRootTemp, "furmark");
        NativeOps.Download(FilesUrl + "furmark.zip", zip);
        NativeOps.Unzip(zip, dir);

        string exe = Path.Combine(dir, "FurMark_win64", "FurMark_GUI.exe");
        if (!File.Exists(exe))
            throw new FileNotFoundException($"FurMark extracted but not found at {exe}.");
        NativeOps.Launch(exe);
    }
}
