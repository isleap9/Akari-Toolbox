using System.Security.Cryptography;
using System.Text.Json;
using AkariToolbox.Framework.Services;

namespace AkariToolbox.App.Services;

/// <summary>
/// Downloads the entire AkariOS PostInstall folder from GitHub and mirrors it to
/// <c>C:\PostInstall\</c> on the local machine — ported 1:1 from the predecessor's
/// <c>static PostInstallService</c> (Akari Tool Premium origin), converted to an
/// injectable singleton (constructor-injects <see cref="IHttpClientFactory"/> and
/// <see cref="ILogConsoleService"/> in place of the predecessor's static, lazily-cached
/// <c>HttpClient</c> field and static <c>App.Tool?.Log</c> calls — per CLAUDE.md's
/// <c>IHttpClientFactory</c> requirement).
///
/// Source: https://github.com/isleap9/PostInstall (main branch)
/// Local root: C:\PostInstall\
///
/// On AkariOS all files are already present — no downloads occur.
/// On stock Windows / a fresh VM, the entire folder is downloaded on first use (~30 MB).
/// </summary>
public sealed class PostInstallService(IHttpClientFactory httpClientFactory, ILogConsoleService log) : IPostInstallService
{
    private const string RawBase =
        "https://raw.githubusercontent.com/isleap9/PostInstall/main/PostInstall/";

    public string LocalRoot => @"C:\PostInstall";

    // Full asset manifest — copied verbatim from the predecessor's PostInstallService.cs
    // (~130 entries), unchanged. Not currently consumed by any Phase 1 handler (Defender's
    // cab+ps1 payload is embedded directly instead); this list is Phase 4/DOWNLOADS-01
    // scope, kept as-is ahead of that page's asset-mirror work.
    private static readonly string[] AllFiles =
    {
        "AntiCheat/Disable DEP NX (default).bat",
        "AntiCheat/Enable DEP NX (needed for faceit).bat",
        "AntiCheat/Set DEP NX to optin (should work for valorant).bat",
        "Change Username/Change Name.bat",
        "Defender/DisableDefender.ps1",
        "Defender/DisableDefenderServices.bat",
        "Defender/EnableDefender.ps1",
        "Defender/EnableDefenderServices.bat",
        "Defender/NoDefender.cab",
        "GPU/AMD/AMD Dwords by imribiy.bat",
        "GPU/AMD/Mpo_disable.reg",
        "GPU/AMD/disable_dx11navi.exe",
        "GPU/AMD/radeon software slimmer/7-Zip/7z.dll",
        "GPU/AMD/radeon software slimmer/7-Zip/7z.exe",
        "GPU/AMD/radeon software slimmer/7-Zip/License.txt",
        "GPU/AMD/radeon software slimmer/ControlzEx.dll",
        "GPU/AMD/radeon software slimmer/MahApps.Metro.IconPacks.Core.dll",
        "GPU/AMD/radeon software slimmer/MahApps.Metro.IconPacks.FontAwesome.dll",
        "GPU/AMD/radeon software slimmer/MahApps.Metro.dll",
        "GPU/AMD/radeon software slimmer/Microsoft.Win32.Registry.dll",
        "GPU/AMD/radeon software slimmer/Microsoft.Win32.TaskScheduler.dll",
        "GPU/AMD/radeon software slimmer/Microsoft.Xaml.Behaviors.dll",
        "GPU/AMD/radeon software slimmer/Newtonsoft.Json.dll",
        "GPU/AMD/radeon software slimmer/RadeonSoftwareSlimmer.exe",
        "GPU/AMD/radeon software slimmer/RadeonSoftwareSlimmer.exe.config",
        "GPU/AMD/radeon software slimmer/RadeonSoftwareSlimmer.pdb",
        "GPU/AMD/radeon software slimmer/System.Diagnostics.EventLog.dll",
        "GPU/AMD/radeon software slimmer/System.IO.Abstractions.dll",
        "GPU/AMD/radeon software slimmer/System.Security.AccessControl.dll",
        "GPU/AMD/radeon software slimmer/System.Security.Principal.Windows.dll",
        "GPU/AMD/radeon software slimmer/System.ServiceProcess.ServiceController.dll",
        "GPU/AMD/radeon software slimmer/TestableIO.System.IO.Abstractions.Wrappers.dll",
        "GPU/AMD/radeon software slimmer/TestableIO.System.IO.Abstractions.dll",
        "GPU/AMD/radeon software slimmer/de/MahApps.Metro.resources.dll",
        "GPU/AMD/radeon software slimmer/de/Microsoft.Win32.TaskScheduler.resources.dll",
        "GPU/AMD/radeon software slimmer/es/Microsoft.Win32.TaskScheduler.resources.dll",
        "GPU/AMD/radeon software slimmer/fr/Microsoft.Win32.TaskScheduler.resources.dll",
        "GPU/AMD/radeon software slimmer/it/Microsoft.Win32.TaskScheduler.resources.dll",
        "GPU/AMD/radeon software slimmer/pl/Microsoft.Win32.TaskScheduler.resources.dll",
        "GPU/AMD/radeon software slimmer/ru/Microsoft.Win32.TaskScheduler.resources.dll",
        "GPU/AMD/radeon software slimmer/zh-CN/Microsoft.Win32.TaskScheduler.resources.dll",
        "GPU/AMD/radeon software slimmer/zh-Hant/Microsoft.Win32.TaskScheduler.resources.dll",
        "GPU/Nvidia/!Disable HDCP.bat",
        "GPU/Nvidia/!Disable telemetry (Breaks Geforce).bat",
        "GPU/Nvidia/!No ECC.bat",
        "GPU/Nvidia/!P-State 0.bat",
        "GPU/Nvidia/!Unrestricted Clock Policy by Cancerogeno.bat",
        "GPU/Nvidia/NIP/Reference.xml",
        "GPU/Nvidia/NIP/Settings.nip",
        "GPU/Nvidia/NIP/nvidiaProfileInspector.exe",
        "GPU/Nvidia/NVCleanstall_1.18.0.exe",
        "GPU/Nvidia/Nvidia Debloating Guide by AMIT.url",
        "GPU/Nvidia/mpo disable.bat",
        "GPU/Nvidia/mpo enable.bat",
        "Mitigations/InSpectre.exe",
        "Others/Action Center/Disable_Action_Center(default).reg",
        "Others/Action Center/Enable_Action_Center.reg",
        "Others/Alt-Tab/Classic-AltTab(Default).bat",
        "Others/Alt-Tab/Immersive-AltTab.bat",
        "Others/Boot Configuration/bootmenupolicy legacy(Default).bat",
        "Others/Boot Configuration/bootmenupolicy-standard.bat",
        "Others/DMA remapping/Disable DMA Remapping (can apply to more stuff than done by script).bat",
        "Others/DMA remapping/Enable DMA Remapping.bat",
        "Others/Drives/support windows on ssd(Default).reg",
        "Others/Drives/support-windows-on-hdd.reg",
        "Others/Network/AkariOS Default Network Settings.bat",
        "Others/Network/Revert Network Tweaks.bat",
        "Others/Network/Run this if you had to install a network driver.bat",
        "Others/SerializeTimerExpiration/Disable SerializeTimerExpiration.reg",
        "Others/SerializeTimerExpiration/Enable SerializeTimerExpiration.reg",
        "Others/Startmenu/disable startmenu 10 and server.bat",
        "Others/Startmenu/disable startmenu 11.bat",
        "Others/Startmenu/enable startmenu 11.bat",
        "Others/Startmenu/enable startmenu.bat",
        "Others/SvcHostSplitThresholdInKB/Set SvcHostSplitThresholdInKB To Default Value.bat",
        "Others/SvcHostSplitThresholdInKB/Set SvcHostSplitThresholdInKB To ffffffff.bat",
        "Resync/resync.bat",
        "Services/AkariOS-Default-services.reg",
        "Services/Windows-Default-services.reg",
        "Services/exes enable.bat",
        "Services/exes.bat",
        "Services/minimal-services.reg",
        "Tweaks/!Bufferbloat Test.url",
        "Tweaks/!Discord Debloat.url",
        "Tweaks/!Network Adapter settings.url",
        "Tweaks/Auto DSCP & FSE.bat",
        "Tweaks/Autoruns.exe",
        "Tweaks/CRU/CRU.exe",
        "Tweaks/CRU/reset-all.exe",
        "Tweaks/CRU/restart.exe",
        "Tweaks/CRU/restart64.exe",
        "Tweaks/Change resolution without immersive cp.lnk",
        "Tweaks/DevManView.exe",
        "Tweaks/DeviceCleanup.exe",
        "Tweaks/DeviceCleanupCmd.exe",
        "Tweaks/Interrupt Affinity Policy Tool.exe",
        "Tweaks/MSI Mode Utility.exe",
        "Tweaks/MeasureSleep.exe",
        "Tweaks/MinSudo.exe",
        "Tweaks/Mouse Movement Recorder.exe",
        "Tweaks/Mouse Polling Test/MouseTester.exe",
        "Tweaks/NSudo.exe",
        "Tweaks/PowerRun.exe",
        "Tweaks/PowerSettingsExplorer.exe",
        "Tweaks/Process explorer/Process Explorer.exe",
        "Tweaks/ReservedCpuSets.dll",
        "Tweaks/ReservedCpuSets.exe",
        "Tweaks/SCEWIN/5.05.01.0002/Export.bat",
        "Tweaks/SCEWIN/5.05.01.0002/Import.bat",
        "Tweaks/SCEWIN/5.05.01.0002/SCEWIN_64.exe",
        "Tweaks/SCEWIN/5.05.01.0002/amifldrv64.sys",
        "Tweaks/SCEWIN/5.05.01.0002/amigendrv64.sys",
        "Tweaks/hidusbf/DRIVER/1kHz.cmd",
        "Tweaks/hidusbf/DRIVER/2kHz-4kHz.cmd",
        "Tweaks/hidusbf/DRIVER/4kHz-8kHz.cmd",
        "Tweaks/hidusbf/DRIVER/98ME/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64/1khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64/2khz-4khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64/4khz-8khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64/nopatch/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64_AS/1khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64_AS/2khz-4khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64_AS/4khz-8khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64_AS/NoPatch/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/AMD64_AS/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/HIDUSBF.INF",
        "Tweaks/hidusbf/DRIVER/HIDUSBFU.INF",
        "Tweaks/hidusbf/DRIVER/HIDUSBF_AS.INF",
        "Tweaks/hidusbf/DRIVER/NTX86/1khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTX86/2khz-4khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTX86/4khz-8khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTX86/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTX86/nopatch/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTx86_AS/1khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTx86_AS/2khz-4khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTx86_AS/4khz-8khz/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTx86_AS/NoPatch/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/NTx86_AS/hidusbf.sys",
        "Tweaks/hidusbf/DRIVER/Setup.exe",
        "Tweaks/hidusbf/DRIVER/nopatch.cmd",
        "Tweaks/hidusbf/DRIVER/sx64.exe",
        "Tweaks/hidusbf/README.2kHz-8kHz.ENG.TXT",
        "Tweaks/hidusbf/README.ENG.TXT",
        "Tweaks/hidusbf/README.RUS.TXT",
        "Tweaks/hidusbf/SweetLow.CER",
        "Tweaks/serviwin.exe",
    };

    /// <summary>
    /// Test seam (matches the project's existing <c>InternalsVisibleTo("AkariToolbox.Tests")</c>
    /// pattern) — the single source of truth for both the manifest-authoring step and
    /// <c>PostInstallIntegrityTests</c>' completeness test, so <c>Resources/PostInstallManifest.json</c>
    /// can never silently drift out of sync with <see cref="AllFiles"/>.
    /// </summary>
    internal static IReadOnlyList<string> RelativeFilePaths => AllFiles;

    // Lazily-loaded, populated once from the embedded Resources/PostInstallManifest.json
    // (D-07/D-08). Maps each AllFiles relative path to its pinned lowercase-hex SHA256 digest.
    private Dictionary<string, string>? _manifest;

    public bool IsFullyInstalled =>
        AllFiles.All(f => File.Exists(Path.Combine(LocalRoot, f.Replace('/', '\\'))));

    public async Task<bool> EnsurePostInstallAsync()
    {
        if (IsFullyInstalled)
        {
            log.Log("[POSTINSTALL] PostInstall folder already complete.");
            return true;
        }

        log.Log("[POSTINSTALL] Downloading PostInstall folder from GitHub (~30 MB)...");

        var manifest = LoadManifest();

        int downloaded = 0, skipped = 0, failed = 0;

        foreach (var relativePath in AllFiles)
        {
            var localPath = Path.Combine(LocalRoot, relativePath.Replace('/', '\\'));

            if (File.Exists(localPath))
            {
                skipped++;
                continue;
            }

            var label = Path.GetFileName(relativePath);

            if (!manifest.TryGetValue(relativePath, out var expectedSha256))
            {
                log.Log($"[POSTINSTALL] No pinned SHA256 in manifest for {relativePath} — skipping (treated as failed).");
                failed++;
                continue;
            }

            var urlPath = string.Join("/",
                relativePath.Split('/').Select(Uri.EscapeDataString));
            var url = RawBase + urlPath;

            bool ok = await DownloadFileAsync(url, localPath, label, expectedSha256);
            if (ok) downloaded++;
            else failed++;
        }

        log.Log($"[POSTINSTALL] Done — {downloaded} downloaded, {skipped} already present, {failed} failed.");

        if (failed > 0)
            log.Log("[POSTINSTALL] Some files failed. Check your internet connection and try again.");

        return failed == 0;
    }

    /// <summary>
    /// Loads and caches the embedded SHA256 manifest, resolved by manifest-resource-name
    /// suffix match on <c>"PostInstallManifest.json"</c> — the same resolution convention
    /// <see cref="AkariToolbox.Framework.Services.ScriptRunner.FindEmbeddedResource"/> uses
    /// for embedded scripts.
    /// </summary>
    private Dictionary<string, string> LoadManifest()
    {
        if (_manifest is not null)
        {
            return _manifest;
        }

        var asm = typeof(PostInstallService).Assembly;
        var resourceName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("PostInstallManifest.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            log.Log("[POSTINSTALL] Embedded PostInstallManifest.json not found — integrity verification cannot proceed.");
            _manifest = new Dictionary<string, string>();
            return _manifest;
        }

        using var stream = asm.GetManifestResourceStream(resourceName)!;
        _manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? new Dictionary<string, string>();
        return _manifest;
    }

    private async Task<bool> DownloadFileAsync(string url, string destPath, string label, string expectedSha256)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            var http = httpClientFactory.CreateClient("PostInstall");
            var bytes = await http.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(destPath, bytes);

            if (!await VerifyFileSha256Async(destPath, expectedSha256))
            {
                log.Log($"[POSTINSTALL] Integrity check FAILED for {label} — deleting corrupted/tampered file.");
                File.Delete(destPath);
                return false;
            }

            log.Log($"[POSTINSTALL] OK {label} ({bytes.Length / 1024} KB, SHA256 verified)");
            return true;
        }
        catch (Exception ex)
        {
            log.Log($"[POSTINSTALL] FAIL {label}: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyFileSha256Async(string filePath, string expectedHexSha256)
    {
        if (!File.Exists(filePath))
        {
            log.Log($"[POSTINSTALL] Integrity check: file not found: {filePath}");
            return false;
        }

        using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream);
        var computed = Convert.ToHexStringLower(hash);

        if (!string.Equals(computed, expectedHexSha256, StringComparison.OrdinalIgnoreCase))
        {
            log.Log($"[POSTINSTALL] Integrity check FAILED for {filePath}");
            return false;
        }

        return true;
    }
}
