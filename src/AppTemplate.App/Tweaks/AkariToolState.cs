using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Persistent tweak state, ported from the old AkariOS Companion (HKCU\Software\AkariTool).
/// Toggles write through here so their position survives app restarts, and small UI choices
/// (dropdown indices, gaming state) persist the same way they did in the old app.
/// </summary>
public static class AkariToolState
{
    private static readonly RegistryKey Store =
        Registry.CurrentUser.CreateSubKey(@"Software\AkariTool", RegistryKeyPermissionCheck.ReadWriteSubTree);

    public static void Save(string key) => Store.SetValue(key, 1);

    public static void Clear(string key) => Store.DeleteValue(key, throwOnMissingValue: false);

    public static bool Has(string key) => Store.GetValue(key) is not null;

    public static void SetInt(string key, int value) => Store.SetValue(key, value, RegistryValueKind.DWord);

    public static int GetInt(string key, int fallback) => Store.GetValue(key) is int i ? i : fallback;

    //  Real interactive-user HKCU 
    // The app runs elevated, so HKCU is the elevation account's hive. For tweaks that
    // must land on the *interactive* user (Start menu search, transparency), open
    // explorer.exe's token and write under HKU\<SID>\...  ported from the old
    // TweakService.CreateRealHkcuSubKey.

    [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    private const uint TOKEN_QUERY = 0x0008;

    private static string? InteractiveUserSid()
    {
        try
        {
            Process? explorer = Process.GetProcessesByName("explorer").FirstOrDefault();
            if (explorer is null || !OpenProcessToken(explorer.Handle, TOKEN_QUERY, out IntPtr token))
                return null;
            using WindowsIdentity identity = new(token);
            return identity.User?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Open (creating if needed) a subkey in the interactive user's hive.</summary>
    public static RegistryKey? OpenRealHkcu(string subKey)
    {
        string? sid = InteractiveUserSid();
        if (sid is null)
            return Registry.CurrentUser.CreateSubKey(subKey, writable: true);

        RegistryKey hku = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
        return hku.CreateSubKey($@"{sid}\{subKey}", writable: true);
    }

    public static int? RealHkcuDword(string subKey, string name)
    {
        using RegistryKey? key = OpenRealHkcu(subKey);
        return key?.GetValue(name) is int i ? i : null;
    }

    public static void SetRealHkcuDword(string subKey, string name, int value)
    {
        using RegistryKey? key = OpenRealHkcu(subKey);
        key?.SetValue(name, value, RegistryValueKind.DWord);
    }
}

/// <summary>Small registry read helpers used by the toggle state readers.</summary>
public static class RegRead
{
    private static (RegistryKey baseKey, string sub)? Resolve(string fullPath)
    {
        int slash = fullPath.IndexOf('\\');
        if (slash < 0) return null;
        string root = fullPath[..slash].ToUpperInvariant();
        string sub = fullPath[(slash + 1)..];
        RegistryKey? baseKey = root switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => Registry.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => Registry.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => Registry.ClassesRoot,
            "HKU" or "HKEY_USERS" => Registry.Users,
            _ => null,
        };
        return baseKey is null ? null : (baseKey, sub);
    }

    /// <summary>DWORD value, or null when the key/value is missing.</summary>
    public static int? Dword(string fullPath, string name)
    {
        var resolved = Resolve(fullPath);
        if (resolved is null) return null;
        using RegistryKey? key = resolved.Value.baseKey.OpenSubKey(resolved.Value.sub);
        return key?.GetValue(name) is int i ? i : null;
    }

    /// <summary>String value, or null when missing.</summary>
    public static string? String(string fullPath, string name)
    {
        var resolved = Resolve(fullPath);
        if (resolved is null) return null;
        using RegistryKey? key = resolved.Value.baseKey.OpenSubKey(resolved.Value.sub);
        return key?.GetValue(name)?.ToString();
    }

    /// <summary>True when the value exists (any kind).</summary>
    public static bool HasValue(string fullPath, string name)
    {
        var resolved = Resolve(fullPath);
        if (resolved is null) return false;
        using RegistryKey? key = resolved.Value.baseKey.OpenSubKey(resolved.Value.sub);
        return key?.GetValue(name) is not null;
    }

    /// <summary>Start value of a Windows service (HKLM\SYSTEM\CurrentControlSet\Services\name).</summary>
    public static int? ServiceStart(string serviceName) =>
        Dword($@"HKLM\SYSTEM\CurrentControlSet\Services\{serviceName}", "Start");
}
