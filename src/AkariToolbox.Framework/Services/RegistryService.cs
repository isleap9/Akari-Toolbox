using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32;

namespace AkariToolbox.Framework.Services;

/// <inheritdoc cref="IRegistryService"/>
public sealed class RegistryService : IRegistryService
{
    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    public object? GetValue(RegistryHive hive, string subKeyPath, string valueName)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var subKey = baseKey.OpenSubKey(subKeyPath);
        return subKey?.GetValue(valueName);
    }

    public void SetValue(RegistryHive hive, string subKeyPath, string valueName, object value, RegistryValueKind kind)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var subKey = baseKey.OpenSubKey(subKeyPath, writable: true)
            ?? baseKey.CreateSubKey(subKeyPath, writable: true);
        subKey!.SetValue(valueName, value, kind);
    }

    public void DeleteValue(RegistryHive hive, string subKeyPath, string valueName)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var subKey = baseKey.OpenSubKey(subKeyPath, writable: true);
        subKey?.DeleteValue(valueName, throwOnMissingValue: false);
    }

    public IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var subKey = baseKey.OpenSubKey(subKeyPath);
        return subKey?.GetSubKeyNames() ?? [];
    }

    public void DeleteSubKeyTree(RegistryHive hive, string subKeyPath)
    {
        var lastSeparator = subKeyPath.LastIndexOf('\\');
        if (lastSeparator < 0)
        {
            return; // No parent to open — nothing safe to delete from.
        }

        var parentPath = subKeyPath[..lastSeparator];
        var name = subKeyPath[(lastSeparator + 1)..];

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var parent = baseKey.OpenSubKey(parentPath, writable: true);
        parent?.DeleteSubKeyTree(name, throwOnMissingSubKey: false);
    }

    public void CreateSubKey(RegistryHive hive, string subKeyPath)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var subKey = baseKey.CreateSubKey(subKeyPath, writable: true);
    }

    public RegistryKey OpenRealUserHive(string subKeyPath)
    {
        // WR-01 fix (01-REVIEW.md): dispose the Process object and close the native
        // token handle explicitly. WindowsIdentity's IntPtr constructor duplicates the
        // token internally, so disposing `identity` does not close our own handle —
        // without the finally block below, every call leaked one process token handle
        // for the lifetime of this long-running elevated process.
        using var explorer = Process.GetProcessesByName("explorer").FirstOrDefault()
            ?? throw new InvalidOperationException("explorer.exe not found.");

        if (!OpenProcessToken(explorer.Handle, 8, out var token))
        {
            throw new InvalidOperationException("Could not open explorer process token.");
        }

        try
        {
            using var identity = new WindowsIdentity(token);
            var sid = identity.User!.Value;
            var hku = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
            return hku.CreateSubKey($@"{sid}\{subKeyPath}", writable: true)!;
        }
        finally
        {
            CloseHandle(token);
        }
    }
}
