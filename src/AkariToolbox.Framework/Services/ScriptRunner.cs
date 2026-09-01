using System.Diagnostics;
using System.Text;

namespace AkariToolbox.Framework.Services;

/// <summary>
/// Ported from the predecessor's <c>ToolService.RunProcess</c>. Note: this primitive
/// deliberately does not port <c>ToolService.RunScript</c>'s embedded-resource extraction —
/// no <c>.ps1</c> resources are embedded in Phase 1 (bcdedit/DISM tweaks call those exes
/// directly by name; Defender calls <c>powershell.exe -File</c> against files already on
/// disk under <c>C:\PostInstall\</c>). A later phase that needs to run an embedded script
/// should add that capability then, when there is a real call site to prove it against.
/// </summary>
public sealed class ScriptRunner(ILogConsoleService log) : IScriptRunner
{
    public async Task<int> RunProcessAsync(string fileName, string arguments, TimeSpan? timeout = null)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) log.Log(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) log.Log($"[ERROR] {e.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var waitTask = process.WaitForExitAsync();
            if (timeout is null)
            {
                await waitTask;
            }
            else if (await Task.WhenAny(waitTask, Task.Delay(timeout.Value)) != waitTask)
            {
                process.Kill(entireProcessTree: true);
                log.Log("[TIMEOUT]");
                return -1;
            }

            return process.ExitCode;
        }
        catch (Exception ex)
        {
            log.Log($"[EXCEPTION] {ex.Message}");
            return -1;
        }
    }

    public async Task<string> RunProcessCaptureOutputAsync(string fileName, string arguments, TimeSpan? timeout = null)
    {
        var output = new StringBuilder();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) log.Log($"[ERROR] {e.Data}"); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var waitTask = process.WaitForExitAsync();
            if (timeout is null)
            {
                await waitTask;
            }
            else if (await Task.WhenAny(waitTask, Task.Delay(timeout.Value)) != waitTask)
            {
                process.Kill(entireProcessTree: true);
                log.Log("[TIMEOUT]");
                return string.Empty;
            }

            var result = output.ToString();
            log.Log(result);
            return result;
        }
        catch (Exception ex)
        {
            log.Log($"[EXCEPTION] {ex.Message}");
            return string.Empty;
        }
    }

    public async Task<int> RunEmbeddedScriptAsync(string resourceSuffix, string? arguments = null, TimeSpan? timeout = null)
    {
        // Search typeof(ScriptRunner).Assembly (Framework) first — preserves the original,
        // test-proven resolution contract for resources embedded alongside ScriptRunner
        // itself (e.g. the exit7.ps1 fixture) — then fall back to every other non-dynamic
        // assembly currently loaded in the AppDomain. This lets embedded resources live in
        // AkariToolbox.App (e.g. Plan 02-06's Resources/GamingScripts/*.ps1) without callers
        // needing to know which assembly actually declared them.
        var (asm, name) = FindEmbeddedResource(resourceSuffix)
            ?? throw new FileNotFoundException($"Embedded resource not found: {resourceSuffix}");

        var temp = Path.Combine(Path.GetTempPath(), $"AkariToolbox-{Guid.NewGuid():N}-{resourceSuffix}");
        try
        {
            await using (var rs = asm.GetManifestResourceStream(name)!)
            await using (var fs = File.Create(temp))
            {
                await rs.CopyToAsync(fs);
            }

            var args = $"-NoProfile -ExecutionPolicy Bypass -File \"{temp}\" {arguments}".TrimEnd();
            return await RunProcessAsync("powershell.exe", args, timeout);
        }
        finally
        {
            try { if (File.Exists(temp)) File.Delete(temp); } catch { /* best-effort temp cleanup */ }
        }
    }

    /// <summary>
    /// Locates an embedded resource whose manifest name ends with <paramref name="resourceSuffix"/>,
    /// checking <see cref="ScriptRunner"/>'s own assembly first, then every other non-dynamic
    /// assembly loaded in the current <see cref="AppDomain"/>. Returns <c>null</c> if no match
    /// is found anywhere.
    /// </summary>
    private static (System.Reflection.Assembly Assembly, string Name)? FindEmbeddedResource(string resourceSuffix)
    {
        var primary = typeof(ScriptRunner).Assembly;
        var primaryMatch = primary.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
        if (primaryMatch is not null)
        {
            return (primary, primaryMatch);
        }

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic || asm == primary)
            {
                continue;
            }

            string? match;
            try
            {
                match = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
            }
            catch (NotSupportedException)
            {
                // Some dynamically-generated or reflection-only assemblies don't support
                // manifest resource enumeration — skip them rather than fail the search.
                continue;
            }

            if (match is not null)
            {
                return (asm, match);
            }
        }

        return null;
    }
}
