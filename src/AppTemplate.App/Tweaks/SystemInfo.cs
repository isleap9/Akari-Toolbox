using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Native "This PC" reads for the Home page (the Akari-Tool THIS PC card:
/// Win32_OperatingSystem / Win32_Processor / Win32_VideoController /
/// Win32_ComputerSystem), via WMI. No scripts, no bundled tools.
/// </summary>
public static class SystemInfo
{
    private static string? _osCache;
    private static string? _cpuCache;
    private static string? _gpuCache;

    public static string OsSummary()
    {
        if (_osCache is not null)
            return _osCache;
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption, BuildNumber FROM Win32_OperatingSystem");
            using ManagementObjectCollection results = searcher.Get();
            foreach (ManagementObject os in results.Cast<ManagementObject>())
            {
                using (os)
                {
                    string caption = (os["Caption"]?.ToString() ?? "").Replace("Microsoft ", "").Trim();
                    string build = os["BuildNumber"]?.ToString() ?? "";
                    _osCache = string.IsNullOrEmpty(build) ? caption : $"{caption} (build {build})";
                    return _osCache;
                }
            }
        }
        catch { }
        return "Unknown";
    }

    public static string CpuName()
    {
        if (_cpuCache is not null)
            return _cpuCache;
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            using ManagementObjectCollection results = searcher.Get();
            foreach (ManagementObject cpu in results.Cast<ManagementObject>())
            {
                using (cpu)
                {
                    string? name = cpu["Name"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(name))
                    {
                        _cpuCache = name;
                        return name;
                    }
                }
            }
        }
        catch { }
        return "Unknown";
    }

    public static string GpuNames()
    {
        if (_gpuCache is not null)
            return _gpuCache;
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
            using ManagementObjectCollection results = searcher.Get();
            var names = new List<string>();
            foreach (ManagementObject g in results.Cast<ManagementObject>())
            {
                using (g)
                {
                    string? name = g["Name"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                        names.Add(name);
                }
            }
            if (names.Count > 0)
            {
                _gpuCache = string.Join(", ", names);
                return _gpuCache;
            }
        }
        catch { }
        return "Unknown";
    }

    public static string RamTotal()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            using ManagementObjectCollection results = searcher.Get();
            foreach (ManagementObject cs in results.Cast<ManagementObject>())
            {
                using (cs)
                {
                    if (cs["TotalPhysicalMemory"] is not null)
                    {
                        double gb = Convert.ToDouble(cs["TotalPhysicalMemory"]) / (1024 * 1024 * 1024);
                        return $"{Math.Round(gb)} GB";
                    }
                }
            }
        }
        catch { }
        return "Unknown";
    }

    public static string MachineName()
    {
        try
        {
            return $"{Environment.MachineName} \\ {Environment.UserName}";
        }
        catch { return "Unknown"; }
    }

    public static string Uptime()
    {
        try
        {
            TimeSpan up = TimeSpan.FromMilliseconds(Environment.TickCount64);
            return $"{(int)up.TotalHours} h {up.Minutes} min";
        }
        catch { return "Unknown"; }
    }

    public static string Privileges()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Administrator" : "Standard user";
        }
        catch { return "Unknown"; }
    }

    public static string DotNetVersion()
    {
        try { return Environment.Version.ToString(3); }
        catch { return "Unknown"; }
    }

    public static (double Percent, string Text) MemoryUse()
    {
        try
        {
            var status = new MemoryStatusEx();
            if (GlobalMemoryStatusEx(status))
            {
                double totalGb = status.TotalPhys / 1024.0 / 1024.0 / 1024.0;
                double usedGb = (status.TotalPhys - status.AvailPhys) / 1024.0 / 1024.0 / 1024.0;
                double percent = status.TotalPhys == 0 ? 0 : 100.0 * (status.TotalPhys - status.AvailPhys) / status.TotalPhys;
                return (percent, $"{usedGb:F1} GB of {totalGb:F1} GB in use");
            }
        }
        catch { }
        return (0, "Unknown");
    }

    public static (double Percent, string Text) SystemDriveUse()
    {
        try
        {
            string? root = Path.GetPathRoot(Environment.SystemDirectory);
            if (!string.IsNullOrEmpty(root))
            {
                var drive = new DriveInfo(root);
                if (drive.IsReady && drive.TotalSize > 0)
                {
                    double totalGb = drive.TotalSize / 1024.0 / 1024.0 / 1024.0;
                    double freeGb = drive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
                    double percent = 100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize;
                    return (percent, $"{freeGb:F1} GB free of {totalGb:F1} GB");
                }
            }
        }
        catch { }
        return (0, "Unknown");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);
}
