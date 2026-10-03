using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native helpers for the operations that are more than a registry write: winget
/// installs, .lnk creation (via the WScript.Shell COM object, no extra references),
/// hardware identification, and launching cached executables. Used from
/// <see cref="TweakAction.Custom"/> delegates in the catalog.
/// </summary>
public static class NativeOps
{
    public static string Expand(string path) => Environment.ExpandEnvironmentVariables(path);

    private static readonly string WinGetPackages =
        Expand(@"%LOCALAPPDATA%\Microsoft\WinGet\Packages");

    public static string PackagePath(string packageFolder, params string[] parts)
    {
        string root = Path.Combine(WinGetPackages, packageFolder);
        return parts.Length == 0 ? root : Path.Combine(new[] { root }.Concat(parts).ToArray());
    }

    /// <summary>Run winget hidden and wait. Errors are swallowed to match the scripts' try/catch.</summary>
    private static void Winget(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            using Process? p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch
        {
            // ignored  mirrors the PowerShell try { } catch { }
        }
    }

    /// <summary>Force a clean (re)install: remove the winget install entry, then install pinned.</summary>
    public static void WingetInstall(string id, string? productCode = null, string? location = null)
    {
        if (!string.IsNullOrEmpty(productCode))
            Winget($"uninstall --product-code {productCode} --silent");

        string args = $"install \"{id}\" --silent --accept-package-agreements --accept-source-agreements --disable-interactivity --no-upgrade";
        if (!string.IsNullOrEmpty(location))
            args += $" --location \"{location}\"";
        Winget(args);
    }

    public static void WingetUninstall(string idOrCodeArg) => Winget($"uninstall {idOrCodeArg} --silent");

    /// <summary>Create a .lnk shortcut using the WScript.Shell COM object (late-bound, no COM refs).</summary>
    public static void CreateShortcut(string linkPath, string targetPath, string? workingDirectory = null, string? arguments = null, bool runAsAdmin = false)
    {
        try
        {
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
                return;

            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell is null)
                return;

            dynamic shortcut = shell.CreateShortcut(linkPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = workingDirectory ?? Path.GetDirectoryName(targetPath) ?? string.Empty;
            if (!string.IsNullOrEmpty(arguments))
                shortcut.Arguments = arguments;
            shortcut.Save();

            if (runAsAdmin)
            {
                // Flip the "Run as administrator" bit (byte 0x15 |= 0x20), like Akari-Tool.
                try
                {
                    byte[] bytes = File.ReadAllBytes(linkPath);
                    bytes[0x15] |= 0x20;
                    File.WriteAllBytes(linkPath, bytes);
                }
                catch { }
            }
        }
        catch { }
    }

    /// <summary>Write an internet shortcut (.url) pointing at a URL/protocol (e.g. roblox://placeId=0).</summary>
    public static void CreateUrlShortcut(string linkPath, string url)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        File.WriteAllText(linkPath, $"[InternetShortcut]\r\nURL={url}\r\n");
    }

    public static string StartMenuShortcut(string name) =>
        Expand($@"%ProgramData%\Microsoft\Windows\Start Menu\Programs\{name}.lnk");

    public static string DesktopShortcut(string name) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{name}.lnk");

    /// <summary>Launch an executable (or any shell target) without waiting.</summary>
    public static void Launch(string path, string? arguments = null)
    {
        try
        {
            var psi = new ProcessStartInfo { FileName = path, UseShellExecute = true };
            if (!string.IsNullOrEmpty(arguments))
                psi.Arguments = arguments;
            Process.Start(psi);
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>Motherboard product string, read from the BIOS registry (no WMI dependency).</summary>
    public static string BaseBoardProduct() =>
        Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";

    /// <summary>Open a web search for the given query.</summary>
    public static void WebSearch(string query) =>
        Launch($"https://www.google.com/search?q={Uri.EscapeDataString(query)}");

    /// <summary>Restart the machine straight into firmware/BIOS (shutdown /r /fw /t 0).</summary>
    public static void RestartToBios() =>
        Process.Start(new ProcessStartInfo("shutdown", "/r /fw /t 0") { UseShellExecute = false, CreateNoWindow = true });

    /// <summary>Restart the machine now (shutdown /r /t 0).</summary>
    public static void Restart() =>
        Process.Start(new ProcessStartInfo("shutdown", "/r /t 0") { UseShellExecute = false, CreateNoWindow = true });

    /// <summary>Prompt for a line of text. Returns null if cancelled/blank.</summary>
    public static string? Prompt(string message, string title)
    {
        try
        {
            string inner =
                $"$m='{message.Replace("'", "''")}';$t='{title.Replace("'", "''")}';" +
                "Add-Type -AssemblyName Microsoft.VisualBasic;" +
                "[Microsoft.VisualBasic.Interaction]::InputBox($m,$t,'')";
            string b64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(inner));
            string result = RunToolCapture("powershell.exe", $"-NoProfile -STA -EncodedCommand {b64}");
            result = result.Trim();
            return string.IsNullOrWhiteSpace(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Ask for a drive letter and return it as "X:\" (or null if cancelled).</summary>
    public static string? PromptDriveRoot(string message)
    {
        string? letter = Prompt(message, "USB Drive Letter");
        if (letter is null)
            return null;
        letter = letter.TrimEnd(':', '\\', '/').Trim();
        return $"{letter}:\\";
    }

    /// <summary>Drop a SetupComplete.cmd onto a bootable USB's sources\$OEM$\$$\Setup\Scripts folder.</summary>
    public static void BuildSetupCompleteUsb(string cmdContent)
    {
        string? usb = PromptDriveRoot("Enter USB Drive Letter");
        if (usb is null)
            throw new OperationCanceledException("No USB drive letter entered.");

        string scripts = Path.Combine(usb, "sources", "$OEM$", "$$", "Setup", "Scripts");
        Directory.CreateDirectory(scripts);
        File.WriteAllText(Path.Combine(scripts, "SetupComplete.cmd"), cmdContent);
        Launch(scripts);
    }

    // --- Processes, services, tasks -------------------------------------------

    /// <summary>Kill running processes by image name (no .exe), ignoring errors.</summary>
    public static void StopProcess(params string[] names)
    {
        foreach (string name in names)
        {
            foreach (Process p in Process.GetProcessesByName(name))
            {
                try { p.Kill(true); } catch { }
            }
        }
    }

    /// <summary>Enumerate service names from the registry (no ServiceController dependency).</summary>
    public static IEnumerable<string> ServiceNamesMatching(string substring)
    {
        using RegistryKey? svc = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
        if (svc is null)
            yield break;
        foreach (string name in svc.GetSubKeyNames())
            if (name.Contains(substring, StringComparison.OrdinalIgnoreCase))
                yield return name;
    }

    public static void StopAndDeleteServicesMatching(string substring)
    {
        foreach (string name in ServiceNamesMatching(substring).ToList())
        {
            RunTool("sc", $"stop \"{name}\"");
            RunTool("sc", $"delete \"{name}\"");
        }
    }

    /// <summary>Run a hidden system tool and wait (sc, reg, schtasks, manage-bde, powercfg).</summary>
    public static void RunTool(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using Process? p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch { }
    }

    /// <summary>Run a hidden system tool and return its stdout.</summary>
    public static string RunToolCapture(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using Process? p = Process.Start(psi);
            if (p is null)
                return "";
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            return output;
        }
        catch { return ""; }
    }

    /// <summary>Delete every scheduled task whose name matches the substring.</summary>
    public static void DeleteScheduledTasksMatching(string substring)
    {
        // schtasks CSV: "TaskName","Next Run Time","Status"
        string csv = RunToolCapture("schtasks", "/query /fo csv /nh");
        foreach (string line in csv.Split('\n'))
        {
            string trimmed = line.Trim().Trim('"');
            if (trimmed.Length == 0)
                continue;
            string taskName = trimmed.Split("\",\"")[0];
            if (taskName.Contains(substring, StringComparison.OrdinalIgnoreCase))
                RunTool("schtasks", $"/delete /tn \"{taskName}\" /f");
        }
    }

    /// <summary>Run a single built-in Windows cmdlet inline (used only where no API/CLI exists, e.g. MMAgent).</summary>
    public static void PowerShellCommand(string command) =>
        RunTool("powershell.exe", $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"{command}\"");

    /// <summary>
    /// Write a multi-line script to a temp .ps1 and run it hidden (waits). Used for the handful of
    /// tweaks that are irreducibly cmdlet-driven  Appx/DISM package (un)installs, scheduled-task
    /// enumeration, the Edge uninstaller  where no Win32/CLI equivalent exists.
    /// </summary>
    public static void RunPowerShellScript(string scriptText)
    {
        Directory.CreateDirectory(SystemRootTemp);
        string file = Path.Combine(SystemRootTemp, $"akaritoolbox_{Guid.NewGuid():N}.ps1");
        File.WriteAllText(file, scriptText, new UTF8Encoding(true));
        RunTool("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{file}\"");
        try { File.Delete(file); } catch { }
    }

    // --- Registry helpers ------------------------------------------------------

    /// <summary>Delete registry subkeys under a parent whose (default) value matches a substring.</summary>
    public static void RemoveSubkeysByDefaultValueLike(string parentPath, string substring)
    {
        var (baseKey, sub) = RegistryPath.Resolve(parentPath);
        using RegistryKey? parent = baseKey.OpenSubKey(sub, writable: true);
        if (parent is null)
            return;
        foreach (string child in parent.GetSubKeyNames())
        {
            using RegistryKey? c = parent.OpenSubKey(child);
            string? def = c?.GetValue(null)?.ToString();
            if (def is not null && def.Contains(substring, StringComparison.OrdinalIgnoreCase))
                parent.DeleteSubKeyTree(child, throwOnMissingSubKey: false);
        }
    }

    /// <summary>Delete value names under a key that match a substring.</summary>
    public static void RemoveValuesLike(string keyPath, string substring)
    {
        var (baseKey, sub) = RegistryPath.Resolve(keyPath);
        using RegistryKey? key = baseKey.OpenSubKey(sub, writable: true);
        if (key is null)
            return;
        foreach (string name in key.GetValueNames())
            if (name.Contains(substring, StringComparison.OrdinalIgnoreCase))
                key.DeleteValue(name, throwOnMissingValue: false);
    }

    /// <summary>Load a hive, import a .reg file into it, then unload  mirrors reg load/import/unload.</summary>
    public static void RegLoadImportUnload(string mountPoint, string hiveFile, string regFileContent)
    {
        string reg = Path.Combine(Expand("%SystemRoot%"), "Temp", "akaritoolbox_import.reg");
        File.WriteAllText(reg, regFileContent);
        if (RunToolExitCode("reg", $"load \"{mountPoint}\" \"{hiveFile}\"") == 0)
        {
            RunTool("reg", $"import \"{reg}\"");
            GC.Collect();
            System.Threading.Thread.Sleep(2000);
            RunTool("reg", $"unload \"{mountPoint}\"");
        }
    }

    private static int RunToolExitCode(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using Process? p = Process.Start(psi);
            if (p is null)
                return -1;
            p.WaitForExit();
            return p.ExitCode;
        }
        catch { return -1; }
    }

    // --- Clipboard (Win32, thread-safe; replaces WPF Clipboard) ----------------

    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    public static void SetClipboard(string text)
    {
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
                return;
            try
            {
                EmptyClipboard();
                int bytes = (text.Length + 1) * sizeof(char);
                IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes);
                if (hMem == IntPtr.Zero)
                    return;
                IntPtr target = GlobalLock(hMem);
                if (target == IntPtr.Zero)
                    return;
                Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                Marshal.WriteInt16(target, text.Length * sizeof(char), 0);
                GlobalUnlock(hMem);
                SetClipboardData(CF_UNICODETEXT, hMem);
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch { }
    }

    // --- Downloads -------------------------------------------------------------

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(10) };

    /// <summary>Download a file (IWR equivalent). Creates the destination directory.</summary>
    public static void Download(string url, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var response = Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using var fs = File.Create(destination);
        response.Content.CopyToAsync(fs).GetAwaiter().GetResult();
    }

    /// <summary>HTTP GET returning the response body as a string, with optional headers.</summary>
    public static string HttpGetString(string url, IDictionary<string, string>? headers = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        if (headers is not null)
            foreach (var kv in headers)
                req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        using var resp = Http.Send(req);
        resp.EnsureSuccessStatusCode();
        return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    public static void DownloadWithHeaders(string url, string destination, IDictionary<string, string> headers)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        foreach (var kv in headers)
            req.Headers.TryAddWithoutValidation(kv.Key, kv.Value);
        using var resp = Http.Send(req, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        using var fs = File.Create(destination);
        resp.Content.CopyToAsync(fs).GetAwaiter().GetResult();
    }

    // --- File picker (Win32 common dialog; replaces WPF OpenFileDialog) --------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int lStructSize;
        public IntPtr hwndOwner;
        public IntPtr hInstance;
        public string lpstrFilter;
        public string lpstrCustomFilter;
        public int nMaxCustFilter;
        public int nFilterIndex;
        public string lpstrFile;
        public int nMaxFile;
        public string lpstrFileTitle;
        public int nMaxFileTitle;
        public string lpstrInitialDir;
        public string lpstrTitle;
        public int Flags;
        public short nFileOffset;
        public short nFileExtension;
        public string lpstrDefExt;
        public IntPtr lCustData;
        public IntPtr lpfnHook;
        public string lpTemplateName;
        public IntPtr pvReserved;
        public int dwReserved;
        public int flagsEx;
    }

    private const int OFN_FILEMUSTEXIST = 0x00001000;
    private const int OFN_PATHMUSTEXIST = 0x00000800;
    private const int OFN_EXPLORER = 0x00080000;

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetOpenFileNameW(ref OpenFileName lpofn);

    /// <summary>Show a Win32 Open File dialog; returns the chosen path or null.</summary>
    public static string? PickFile(string title, string filter = "All Files (*.*)|*.*")
    {
        try
        {
            string filterNative = filter.Replace('|', '\0') + "\0\0";
            var buffer = new StringBuilder(4096);
            var ofn = new OpenFileName
            {
                lStructSize = Marshal.SizeOf<OpenFileName>(),
                lpstrFilter = filterNative,
                nFilterIndex = 1,
                lpstrFile = buffer.ToString(),
                nMaxFile = 4096,
                lpstrTitle = title,
                Flags = OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_EXPLORER,
            };
            if (GetOpenFileNameW(ref ofn))
                return buffer.ToString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static string SevenZipExe => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.exe");

    /// <summary>Extract an archive (or self-extracting exe) with 7-Zip.</summary>
    public static void SevenZipExtract(string archive, string outDir) =>
        RunTool(SevenZipExe, $"x \"{archive}\" -o\"{outDir}\" -y");

    /// <summary>Extract a .zip to a folder, overwriting existing files.</summary>
    public static void Unzip(string zipPath, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, destinationDir, overwriteFiles: true);
    }

    public static string ProgramFilesX86 => Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
    public static string SystemRootTemp => Path.Combine(Expand("%SystemRoot%"), "Temp");

    // --- GPU / display adapter enumeration -------------------------------------

    public const string DisplayClassGuid = "{4d36e968-e325-11ce-bfc1-08002be10318}";

    /// <summary>
    /// All immediate subkeys of the display-adapter class, except *Configuration keys.
    /// Skips children that cannot be opened writable  the class key carries a locked-down
    /// `Properties` child (SYSTEM-only ACL) that the PowerShell scripts' Get-ChildItem drops
    /// on its access error, so writing to it would fail the whole action.
    /// </summary>
    public static IEnumerable<string> DisplayClassSubkeys(string controlSet = "CurrentControlSet")
    {
        string basePath = $@"SYSTEM\{controlSet}\Control\Class\{DisplayClassGuid}";
        using RegistryKey? root = Registry.LocalMachine.OpenSubKey(basePath);
        if (root is null)
            yield break;
        foreach (string name in root.GetSubKeyNames())
        {
            if (name.EndsWith("Configuration", StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                using RegistryKey? child = root.OpenSubKey(name, writable: true);
                if (child is null)
                    continue;
            }
            catch
            {
                continue;
            }
            yield return $@"HKLM\{basePath}\{name}";
        }
    }

    /// <summary>Four-digit adapter subkeys (0000, 0001, ) of the display class.</summary>
    public static IEnumerable<string> DisplayAdapterNumberedKeys(string controlSet = "CurrentControlSet")
    {
        string basePath = $@"SYSTEM\{controlSet}\Control\Class\{DisplayClassGuid}";
        using RegistryKey? root = Registry.LocalMachine.OpenSubKey(basePath);
        if (root is null)
            yield break;
        foreach (string name in root.GetSubKeyNames())
            if (name.Length == 4 && name.All(char.IsDigit))
                yield return $@"HKLM\{basePath}\{name}";
    }

    /// <summary>
    /// Instance IDs (e.g. PCI\VEN_10DE...\...) of every display-class device instance under
    /// <c>Enum</c>  the native equivalent of <c>Get-PnpDevice -Class Display</c>. ClassGUID
    /// lives on the instance key, at any enumerator depth (PCI\, ROOT\, ).
    /// </summary>
    public static IEnumerable<string> DisplayDeviceInstanceIds(string controlSet = "ControlSet001")
    {
        using RegistryKey? root = Registry.LocalMachine.OpenSubKey($@"SYSTEM\{controlSet}\Enum");
        if (root is null)
            yield break;
        var found = new List<string>();
        WalkDisplayDevices(root, string.Empty, found);
        foreach (string id in found)
            yield return id;
    }

    private static void WalkDisplayDevices(RegistryKey key, string prefix, List<string> found)
    {
        foreach (string name in key.GetSubKeyNames())
        {
            try
            {
                using RegistryKey? sub = key.OpenSubKey(name);
                if (sub is null)
                    continue;
                string path = prefix.Length == 0 ? name : $@"{prefix}\{name}";
                if (string.Equals(sub.GetValue("ClassGUID")?.ToString(), DisplayClassGuid, StringComparison.OrdinalIgnoreCase))
                    found.Add(path);
                WalkDisplayDevices(sub, path, found);
            }
            catch { }
        }
    }

    /// <summary>Full paths of every descendant key named <paramref name="leafName"/> under the display class (e.g. UMD, power_v1).</summary>
    public static IEnumerable<string> DisplayClassDescendantKeys(string leafName, string controlSet = "ControlSet001")
    {
        string basePath = $@"SYSTEM\{controlSet}\Control\Class\{DisplayClassGuid}";
        using RegistryKey? root = Registry.LocalMachine.OpenSubKey(basePath);
        if (root is null)
            yield break;
        var results = new List<string>();
        Walk(root, $@"HKLM\{basePath}", leafName, results);
        foreach (string r in results)
            yield return r;
    }

    private static void Walk(RegistryKey key, string path, string leafName, List<string> results)
    {
        foreach (string child in key.GetSubKeyNames())
        {
            string childPath = $@"{path}\{child}";
            if (string.Equals(child, leafName, StringComparison.OrdinalIgnoreCase))
                results.Add(childPath);
            try
            {
                using RegistryKey? sub = key.OpenSubKey(child);
                if (sub is not null)
                    Walk(sub, childPath, leafName, results);
            }
            catch { }
        }
    }

    /// <summary>Set a DWORD value on a key and every descendant key beneath it.</summary>
    public static void SetDwordAllDescendants(string basePath, string name, int value)
    {
        var (baseKey, sub) = RegistryPath.Resolve(basePath);
        using RegistryKey? root = baseKey.OpenSubKey(sub, writable: true);
        if (root is null)
            return;
        SetDwordRecurse(root, name, value);
    }

    private static void SetDwordRecurse(RegistryKey key, string name, int value)
    {
        try { key.SetValue(name, value, RegistryValueKind.DWord); } catch { }
        foreach (string child in key.GetSubKeyNames())
        {
            try
            {
                using RegistryKey? sub = key.OpenSubKey(child, writable: true);
                if (sub is not null)
                    SetDwordRecurse(sub, name, value);
            }
            catch { }
        }
    }

    /// <summary>Promote (show) every taskbar tray icon that is currently hidden.</summary>
    public static void PromoteAllTrayIcons()
    {
        using RegistryKey? root = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", writable: true);
        if (root is null)
            return;
        foreach (string child in root.GetSubKeyNames())
        {
            using RegistryKey? sub = root.OpenSubKey(child, writable: true);
            if (sub?.GetValue("IsPromoted") is int promoted && promoted != 0)
                sub.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
        }
    }

    /// <summary>Enable MSI mode (MSISupported=1) for every display device.</summary>
    public static void EnableGpuMsiMode(int value = 1)
    {
        foreach (string id in DisplayDeviceInstanceIds())
        {
            var (baseKey, sub) = RegistryPath.Resolve(
                $@"HKLM\SYSTEM\ControlSet001\Enum\{id}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties");
            using RegistryKey key = baseKey.CreateSubKey(sub, writable: true);
            key.SetValue("MSISupported", value, RegistryValueKind.DWord);
        }
    }

    // --- Files -----------------------------------------------------------------

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool DeleteFileW(string lpFileName);

    /// <summary>Remove the Zone.Identifier "mark of the web" from every file in a folder (Unblock-File).</summary>
    public static void UnblockFolder(string directory)
    {
        if (!Directory.Exists(directory))
            return;
        foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            try { DeleteFileW($"{file}:Zone.Identifier"); } catch { }
        }
    }

    public static void SetReadOnly(string path, bool readOnly)
    {
        try
        {
            var attr = File.GetAttributes(path);
            File.SetAttributes(path, readOnly ? attr | FileAttributes.ReadOnly : attr & ~FileAttributes.ReadOnly);
        }
        catch { }
    }

    public static void WriteAllText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    public static void MoveFile(string source, string destination)
    {
        try
        {
            if (File.Exists(source))
                File.Move(source, destination, overwrite: true);
        }
        catch { }
    }

    public static void DeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch { }
    }

    /// <summary>Delete the *contents* of a folder (files and subfolders), leaving the folder itself.</summary>
    public static void EmptyDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;
        foreach (string file in Directory.EnumerateFiles(path))
            try { File.Delete(file); } catch { }
        foreach (string dir in Directory.EnumerateDirectories(path))
            try { Directory.Delete(dir, recursive: true); } catch { }
    }

    /// <summary>Recursively copy a directory (Copy-Item -Recurse -Force), ignoring per-file errors.</summary>
    public static void CopyDirectory(string source, string destination)
    {
        if (!Directory.Exists(source))
            return;
        Directory.CreateDirectory(destination);
        foreach (string dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, destination));
        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            try { File.Copy(file, file.Replace(source, destination), overwrite: true); } catch { }
    }

    // --- Services / scheduled tasks -------------------------------------------

    /// <summary>Stop a service by name (sc stop), ignoring errors.</summary>
    public static void StopService(string name) => RunTool("sc.exe", $"stop \"{name}\"");

    public static void DisableScheduledTask(string nameContains) => ChangeScheduledTasks(nameContains, "/Disable");
    public static void EnableScheduledTask(string nameContains) => ChangeScheduledTasks(nameContains, "/Enable");

    private static void ChangeScheduledTasks(string nameContains, string action)
    {
        string csv = RunToolCapture("schtasks", "/query /fo csv /nh");
        foreach (string line in csv.Split('\n'))
        {
            string trimmed = line.Trim().Trim('"');
            if (trimmed.Length == 0)
                continue;
            string taskName = trimmed.Split("\",\"")[0];
            if (taskName.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                RunTool("schtasks", $"/Change /TN \"{taskName}\" {action}");
        }
    }

    // --- Images (WinUI imaging  no WPF/System.Drawing dependency) -------------

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    /// <summary>Create a solid-black PNG the size of the primary screen.</summary>
    public static void CreateBlackImage(string path)
    {
        int w = Math.Max(1, GetSystemMetrics(SM_CXSCREEN));
        int h = Math.Max(1, GetSystemMetrics(SM_CYSCREEN));
        SaveBlackImagePng(path, w, h);
    }

    /// <summary>Overwrite every .png / .bmp in a folder tree with a black image of the same size.</summary>
    public static void BlackenImagesInFolder(string folder)
    {
        if (!Directory.Exists(folder))
            return;
        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            string ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext != ".png" && ext != ".bmp")
                continue;
            try
            {
                using var stream = File.OpenRead(file).AsRandomAccessStream();
                var decoder = Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream).AsTask().GetAwaiter().GetResult();
                SaveBlackImagePng(file, (int)decoder.PixelWidth, (int)decoder.PixelHeight);
            }
            catch { }
        }
    }

    private static void SaveBlackImagePng(string path, int width, int height)
    {
        if (width <= 0 || height <= 0)
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None).AsRandomAccessStream();
        var encoder = Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(
            Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream).AsTask().GetAwaiter().GetResult();
        int stride = width * 4;
        var pixels = new byte[stride * height];
        for (int i = 0; i < pixels.Length; i += 4)
            pixels[i + 3] = 0xFF;
        encoder.SetPixelData(
            Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,
            Windows.Graphics.Imaging.BitmapAlphaMode.Straight,
            (uint)width, (uint)height, 96.0, 96.0, pixels);
        encoder.FlushAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>manage-bde -off on every ready fixed drive.</summary>
    public static void DisableBitLockerAllDrives()
    {
        foreach (DriveInfo d in DriveInfo.GetDrives())
        {
            if (d.DriveType == DriveType.Fixed && d.IsReady)
                RunTool("manage-bde", $"-off {d.Name.TrimEnd('\\')}");
        }
    }

    // --- Embedded data blobs ---------------------------------------------------

    /// <summary>
    /// Load one of the verbatim <c>Tweaks\Data\</c> blobs (a .reg ported from an
    /// Ultimate script) by its file name, e.g. "ControlPanelOptimize.reg".
    /// </summary>
    public static string LoadWindowsData(string fileName) => LoadDataResource("Windows", fileName);

    /// <summary>Load one of the verbatim <c>Tweaks\Data\Graphics\</c> blobs by its file name.</summary>
    public static string LoadGraphicsData(string fileName) => LoadDataResource("Graphics", fileName);

    /// <summary>Load one of the verbatim <c>Tweaks\Data\Advanced\</c> blobs by its file name.</summary>
    public static string LoadAdvancedData(string fileName) => LoadDataResource("Advanced", fileName);

    private static string LoadDataResource(string category, string fileName)
    {
        string resource = $"AkariToolbox.Tweaks.Data.{category}.{fileName}";
        using Stream? s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new FileNotFoundException($"Embedded resource not found: {resource}");
        using var reader = new StreamReader(s);
        return reader.ReadToEnd();
    }

    // --- .reg import (mirrors Set-Content + regedit /S) ------------------------

    /// <summary>Write .reg text to a temp file and import it with <c>regedit /S</c> (waits).</summary>
    /// <param name="dollarFromQuestion">
    /// Replace every '?' with '$' first  the Control Panel script stores '$' as '?' inside a
    /// double-quoted here-string, then swaps them back before importing.
    /// </param>
    public static void ImportRegContent(string regText, bool dollarFromQuestion = false)
    {
        if (dollarFromQuestion)
            regText = regText.Replace('?', '$');
        string file = Path.Combine(SystemRootTemp, $"akaritoolbox_{Guid.NewGuid():N}.reg");
        Directory.CreateDirectory(SystemRootTemp);
        // reg files are ANSI/UTF-16; regedit reads UTF-16 or ANSI. Write UTF-8 with BOM-less
        // is risky for regedit, so use Unicode (UTF-16 LE with BOM) which regedit accepts.
        File.WriteAllText(file, regText, new UnicodeEncoding(false, true));
        RunTool("regedit.exe", $"/S \"{file}\"");
        try { File.Delete(file); } catch { }
    }

    // --- TrustedInstaller execution --------------------------------------------

    /// <summary>
    /// Run a PowerShell command as the TrustedInstaller service account, by temporarily
    /// repointing the service's binary path (the Ultimate scripts' <c>Run-Trusted</c>).
    /// </summary>
    public static void RunAsTrustedInstaller(string powershellCommand)
    {
        StopTrustedInstaller();

        string defaultBinPath = Path.Combine(Expand("%SystemRoot%"), "servicing", "TrustedInstaller.exe");
        string b64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(powershellCommand));

        RunTool("sc.exe", $"config TrustedInstaller binPath= \"cmd.exe /c powershell.exe -encodedcommand {b64}\"");
        RunTool("sc.exe", "start TrustedInstaller");
        RunTool("sc.exe", $"config TrustedInstaller binpath= \"{defaultBinPath}\"");

        StopTrustedInstaller();
    }

    private static void StopTrustedInstaller()
    {
        RunTool("sc.exe", "stop TrustedInstaller");
        RunTool("taskkill", "/im trustedinstaller.exe /f");
    }

    // --- Registry tree helpers -------------------------------------------------

    /// <summary>Full HKLM-prefixed paths of every descendant key named <paramref name="leafName"/> under <paramref name="basePath"/>.</summary>
    public static IEnumerable<string> DescendantKeyPaths(string basePath, string leafName)
    {
        var (baseKey, sub) = RegistryPath.Resolve(basePath);
        using RegistryKey? root = baseKey.OpenSubKey(sub);
        if (root is null)
            yield break;
        var results = new List<string>();
        Walk(root, basePath, leafName, results);
        foreach (string r in results)
            yield return r;
    }

    /// <summary>Four-digit ("0000", "0001", ) subkeys of a device setup class, as full HKLM paths.</summary>
    public static IEnumerable<string> NumberedClassSubkeys(string classGuid, string controlSet = "ControlSet001")
    {
        string basePath = $@"SYSTEM\{controlSet}\Control\Class\{classGuid}";
        using RegistryKey? root = Registry.LocalMachine.OpenSubKey(basePath);
        if (root is null)
            yield break;
        foreach (string name in root.GetSubKeyNames())
            if (name.Length == 4 && name.All(char.IsDigit))
                yield return $@"HKLM\{basePath}\{name}";
    }

    /// <summary>Set every currently-shown ("promoted") tray icon to <paramref name="value"/> (1 = show, 0 = hide).</summary>
    public static void SetTrayIconsPromoted(int value)
    {
        using RegistryKey? root = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", writable: true);
        if (root is null)
            return;
        foreach (string child in root.GetSubKeyNames())
        {
            using RegistryKey? sub = root.OpenSubKey(child, writable: true);
            if (sub?.GetValue("IsPromoted") is int promoted && promoted != 0)
                sub.SetValue("IsPromoted", value, RegistryValueKind.DWord);
        }
    }

    // --- Files / attributes ----------------------------------------------------

    /// <summary>attrib +h / -h on a folder and, recursively, everything inside it.</summary>
    public static void SetFolderHidden(string folder, bool hidden)
    {
        if (!Directory.Exists(folder))
            return;
        string flag = hidden ? "+h" : "-h";
        RunTool("attrib", $"{flag} \"{folder}\"");
        RunTool("attrib", $"{flag} \"{folder}\\*.*\" /s /d");
    }

    /// <summary>Decode a PEM/base64 blob (certutil -decode style) to a binary file.</summary>
    public static void DecodeCertToFile(string pemText, string destination)
    {
        var sb = new StringBuilder();
        foreach (string line in pemText.Split('\n'))
            if (!line.Contains("-----"))
                sb.Append(line.Trim());
        byte[] bytes = Convert.FromBase64String(sb.ToString());
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.WriteAllBytes(destination, bytes);
    }

    // --- Services / powercfg / compiler ---------------------------------------

    /// <summary>Stop, disable and delete a Windows service by display/service name.</summary>
    public static void StopAndDeleteService(string name)
    {
        RunTool("sc.exe", $"stop \"{name}\"");
        RunTool("sc.exe", $"config \"{name}\" start= disabled");
        RunTool("sc.exe", $"delete \"{name}\"");
    }

    /// <summary>Create an auto-start service and start it (sc create + start).</summary>
    public static void CreateAndStartService(string name, string binaryPath)
    {
        RunTool("sc.exe", $"create \"{name}\" binPath= \"{binaryPath}\" start= auto");
        RunTool("sc.exe", $"start \"{name}\"");
    }

    /// <summary>Compile a single .cs file to an .exe with the in-box .NET Framework C# compiler.</summary>
    public static void CompileWithCsc(string sourceFile, string outputExe)
    {
        string csc = Path.Combine(Expand("%SystemRoot%"), "Microsoft.NET", "Framework64", "v4.0.30319", "csc.exe");
        RunTool(csc, $"-out:\"{outputExe}\" \"{sourceFile}\"");
    }

    /// <summary>Duplicate/activate a power scheme then delete every other scheme (leaves the active one).</summary>
    public static void DeleteAllOtherPowerSchemes()
    {
        string list = RunToolCapture("powercfg", "/L");
        foreach (Match m in Regex.Matches(list, @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}"))
            RunTool("powercfg", $"/delete {m.Value}");
    }

    /// <summary>Write an autounattend.xml (with the chosen account name) to a USB root.</summary>
    public static void BuildAutounattendUsb(string templateWithAtPlaceholder)
    {
        string? username = Prompt("Enter Account Name (No Spaces)", "Autounattend Account");
        if (username is null)
            throw new OperationCanceledException("No account name entered.");

        string xml = templateWithAtPlaceholder.Replace("@", username);

        string? usb = PromptDriveRoot("Enter USB Drive Letter");
        if (usb is null)
            throw new OperationCanceledException("No USB drive letter entered.");

        string dest = Path.Combine(usb, "autounattend.xml");
        File.WriteAllText(dest, xml, new UTF8Encoding(false));
        Launch(usb);
    }
}
