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

    public RegistryKey OpenRealUserHive(string subKeyPath)
    {
        var explorer = Process.GetProcessesByName("explorer").FirstOrDefault()
            ?? throw new InvalidOperationException("explorer.exe not found.");

        if (!OpenProcessToken(explorer.Handle, 8, out var token))
        {
            throw new InvalidOperationException("Could not open explorer process token.");
        }

        using var identity = new WindowsIdentity(token);
        var sid = identity.User!.Value;
        var hku = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Default);
        return hku.CreateSubKey($@"{sid}\{subKeyPath}", writable: true)!;
    }
}
