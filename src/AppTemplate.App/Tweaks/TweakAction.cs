using System.Diagnostics;
using Microsoft.Win32;

namespace AkariToolbox.Tweaks;

/// <summary>
/// A single native operation a tweak performs. Everything the Ultimate PowerShell
/// scripts did via <c>reg add</c>, <c>bcdedit</c>, <c>powercfg</c>, <c>Start-Process</c>
/// etc. is expressed here as a first-class action executed with native .NET / Win32
/// APIs  no PowerShell scripts are bundled or invoked.
/// </summary>
public abstract class TweakAction
{
    public abstract void Apply();

    // --- Factory helpers so the catalog reads declaratively -------------------

    /// <summary>reg add  /t REG_DWORD /d value</summary>
    public static TweakAction RegDword(string path, string name, long value) =>
        new RegistrySetAction(path, name, RegistryValueKind.DWord, unchecked((int)value));

    /// <summary>reg add  /t REG_QWORD /d value</summary>
    public static TweakAction RegQword(string path, string name, long value) =>
        new RegistrySetAction(path, name, RegistryValueKind.QWord, value);

    /// <summary>reg add  /t REG_SZ /d value</summary>
    public static TweakAction RegString(string path, string name, string value) =>
        new RegistrySetAction(path, name, RegistryValueKind.String, value);

    /// <summary>reg add  /t REG_EXPAND_SZ /d value</summary>
    public static TweakAction RegExpandString(string path, string name, string value) =>
        new RegistrySetAction(path, name, RegistryValueKind.ExpandString, value);

    /// <summary>reg add  /t REG_BINARY /d value</summary>
    public static TweakAction RegBinary(string path, string name, byte[] value) =>
        new RegistrySetAction(path, name, RegistryValueKind.Binary, value);

    /// <summary>reg add  /t REG_BINARY /d hexString (as reg.exe accepts it: pairs of hex digits).</summary>
    public static TweakAction RegBinaryHex(string path, string name, string hex) =>
        new RegistrySetAction(path, name, RegistryValueKind.Binary, HexToBytes(hex));

    internal static byte[] HexToBytes(string hex)
    {
        // Keep only hex digits, then take complete byte pairs (tolerant of the source data).
        Span<char> buffer = stackalloc char[hex.Length];
        int n = 0;
        foreach (char c in hex)
            if (Uri.IsHexDigit(c))
                buffer[n++] = c;
        n -= n % 2;
        var bytes = new byte[n / 2];
        for (int i = 0; i < n; i += 2)
            bytes[i / 2] = (byte)((Convert.ToInt32(buffer[i].ToString(), 16) << 4) | Convert.ToInt32(buffer[i + 1].ToString(), 16));
        return bytes;
    }

    /// <summary>reg delete  /v name</summary>
    public static TweakAction RegDeleteValue(string path, string name) =>
        new RegistryDeleteValueAction(path, name);

    /// <summary>reg delete  /f (whole key)</summary>
    public static TweakAction RegDeleteKey(string path) =>
        new RegistryDeleteKeyAction(path);

    /// <summary>Run a system executable (bcdedit, powercfg, sc, dism, shutdown, ) hidden and wait.</summary>
    public static TweakAction Run(string fileName, string arguments) =>
        new RunProcessAction(fileName, arguments, wait: true, shellExecute: false);

    /// <summary>Launch something the user should see (installer, settings page) without waiting.</summary>
    public static TweakAction Launch(string fileName, string arguments = "") =>
        new RunProcessAction(fileName, arguments, wait: false, shellExecute: true);

    /// <summary>Start-Process style shell open: URLs, ms-settings:, protocol handlers, control panels.</summary>
    public static TweakAction Open(string target) =>
        new ShellOpenAction(target);

    /// <summary>Escape hatch for the handful of WMI / cmdlet-backed operations.</summary>
    public static TweakAction Custom(Action action) =>
        new DelegateAction(action);
}

internal static class RegistryPath
{
    /// <summary>Splits "HKLM\Software\Foo" into the base key and sub path, opened writable.</summary>
    public static (RegistryKey baseKey, string subPath) Resolve(string fullPath)
    {
        int slash = fullPath.IndexOf('\\');
        string root = (slash < 0 ? fullPath : fullPath[..slash]).ToUpperInvariant();
        string sub = slash < 0 ? string.Empty : fullPath[(slash + 1)..];

        RegistryKey baseKey = root switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKU" or "HKEY_USERS" => Registry.Users,
            "HKCC" or "HKEY_CURRENT_CONFIG" => Registry.CurrentConfig,
            _ => throw new ArgumentException($"Unknown registry root: {root}"),
        };
        return (baseKey, sub);
    }
}

public sealed class RegistrySetAction(string path, string name, RegistryValueKind kind, object value) : TweakAction
{
    public override void Apply()
    {
        var (baseKey, sub) = RegistryPath.Resolve(path);
        using RegistryKey key = baseKey.CreateSubKey(sub, writable: true);
        key.SetValue(name, value, kind);
    }
}

public sealed class RegistryDeleteValueAction(string path, string name) : TweakAction
{
    public override void Apply()
    {
        var (baseKey, sub) = RegistryPath.Resolve(path);
        using RegistryKey? key = baseKey.OpenSubKey(sub, writable: true);
        key?.DeleteValue(name, throwOnMissingValue: false);
    }
}

public sealed class RegistryDeleteKeyAction(string path) : TweakAction
{
    public override void Apply()
    {
        var (baseKey, sub) = RegistryPath.Resolve(path);
        baseKey.DeleteSubKeyTree(sub, throwOnMissingSubKey: false);
    }
}

public sealed class RunProcessAction(string fileName, string arguments, bool wait, bool shellExecute) : TweakAction
{
    public override void Apply()
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = shellExecute,
            CreateNoWindow = !shellExecute,
            WindowStyle = shellExecute ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden,
        };
        using Process? p = Process.Start(psi);
        if (wait)
            p?.WaitForExit();
    }
}

public sealed class ShellOpenAction(string target) : TweakAction
{
    public override void Apply()
    {
        Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
    }
}

public sealed class DelegateAction(Action action) : TweakAction
{
    public override void Apply() => action();
}
