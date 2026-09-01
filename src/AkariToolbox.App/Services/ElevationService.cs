using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AkariToolbox.App.Services;

/// <summary>
/// In-process privilege escalation: runs native C# code while impersonating SYSTEM,
/// replacing the external MinSudo/PowerRun executables the predecessor shelled out to.
///
/// Ported from the sibling AkariTool repo's <c>ElevationService</c> (per explicit
/// project-owner direction — closes CR-01/CR-03 from the 01-REVIEW.md code review):
/// only <see cref="RunAsSystem"/> and the privilege-enable plumbing it needs are ported.
/// The TrustedInstaller/<c>ServiceController</c>-based path is intentionally NOT ported —
/// Defender's native phase-2/re-enable writes only ever need a SYSTEM token (duplicated
/// from winlogon.exe), never TrustedInstaller, so there is no reason to add the
/// <c>System.ServiceProcess.ServiceController</c> package for this handler.
///
/// The process must already be running elevated (Administrator) — impersonation borrows
/// an existing identity, it does not bypass UAC.
///
/// IMPORTANT: impersonation is per-thread state. The action passed to
/// <see cref="RunAsSystem"/> must be fully synchronous; awaiting inside it can resume on
/// another thread that carries the original (unelevated) identity.
/// </summary>
public static class ElevationService
{
    // ── Win32 constants ──────────────────────────────────────────────────────
    private const uint TOKEN_DUPLICATE                   = 0x0002;
    private const uint TOKEN_QUERY                       = 0x0008;
    private const uint TOKEN_ADJUST_PRIVILEGES           = 0x0020;
    private const uint MAXIMUM_ALLOWED                   = 0x02000000;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    private const int SecurityImpersonation = 2; // SECURITY_IMPERSONATION_LEVEL
    private const int TokenImpersonation    = 2; // TOKEN_TYPE

    private const string SE_DEBUG_NAME = "SeDebugPrivilege";
    // SeBackup/SeRestore give the token DACL-bypass on registry keys via Windows'
    // backup/restore semantics. SYSTEM holds both but they are DISABLED by default;
    // enabling them lets native writes hit the handful of unusually-locked keys a
    // normal SYSTEM access check (no bypass) can't clear. Ported harmless-but-unused
    // by the current Defender callers (they don't pass enableBackupRestore: true).
    private const string SE_BACKUP_NAME       = "SeBackupPrivilege";
    private const string SE_RESTORE_NAME      = "SeRestorePrivilege";
    private const uint   SE_PRIVILEGE_ENABLED = 0x0002;

    // ── P/Invoke ─────────────────────────────────────────────────────────────
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateTokenEx(
        IntPtr existingToken, uint desiredAccess, IntPtr tokenAttributes,
        int impersonationLevel, int tokenType, out IntPtr newToken);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ImpersonateLoggedOnUser(IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RevertToSelf();

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool LookupPrivilegeValue(string? systemName, string name, out LUID luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr tokenHandle, [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
        ref TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES { public LUID Luid; public uint Attributes; }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID_AND_ATTRIBUTES Privilege; }

    // ═════════════════════════════════════════════════════════════════════════
    // PUBLIC API
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Runs <paramref name="action"/> while impersonating the SYSTEM account, using a
    /// token duplicated from winlogon.exe. All registry writes inside the action go
    /// through <see cref="Microsoft.Win32.Registry"/> and inherit SYSTEM rights.
    /// </summary>
    /// <param name="enableBackupRestore">
    /// When true, enables SeBackupPrivilege + SeRestorePrivilege on the impersonated
    /// SYSTEM token before the action runs, giving registry writes DACL-bypass. OPT-IN —
    /// off by default; Defender's native writes don't need it.
    /// </param>
    /// <returns>true when the impersonation was established and the action ran to completion.</returns>
    public static bool RunAsSystem(Action action, Action<string>? log = null, bool enableBackupRestore = false)
    {
        try
        {
            // SeDebugPrivilege is required to open winlogon's process token.
            if (!EnablePrivilege(SE_DEBUG_NAME, log))
                return false;

            var winlogon = Process.GetProcessesByName("winlogon");
            if (winlogon.Length == 0)
            {
                log?.Invoke("Elevation: winlogon.exe not found — cannot acquire a SYSTEM token.");
                return false;
            }

            return RunAsProcessToken(winlogon[0].Id, action, "SYSTEM", log, enableBackupRestore);
        }
        catch (Exception ex)
        {
            log?.Invoke($"Elevation: RunAsSystem failed — {ex.Message}");
            return false;
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CORE
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Opens the target process, duplicates its access token into an impersonation
    /// token, attaches it to the current thread and runs the action. Every handle is
    /// released and <c>RevertToSelf</c> is called in a finally block — a leaked
    /// impersonation token would leave the whole thread running elevated.
    /// </summary>
    private static bool RunAsProcessToken(int pid, Action action, string label, Action<string>? log,
                                          bool enableBackupRestore = false)
    {
        IntPtr process = IntPtr.Zero, token = IntPtr.Zero, dup = IntPtr.Zero;
        bool impersonating = false;

        try
        {
            process = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (process == IntPtr.Zero)
            {
                log?.Invoke($"Elevation: OpenProcess({label}, pid {pid}) failed — {Win32Error()}");
                return false;
            }

            if (!OpenProcessToken(process, TOKEN_DUPLICATE | TOKEN_QUERY, out token))
            {
                log?.Invoke($"Elevation: OpenProcessToken({label}) failed — {Win32Error()}");
                return false;
            }

            // A borrowed token cannot be attached directly; it must be duplicated
            // into a token of our own with impersonation semantics.
            if (!DuplicateTokenEx(token, MAXIMUM_ALLOWED, IntPtr.Zero,
                                  SecurityImpersonation, TokenImpersonation, out dup))
            {
                log?.Invoke($"Elevation: DuplicateTokenEx({label}) failed — {Win32Error()}");
                return false;
            }

            if (!ImpersonateLoggedOnUser(dup))
            {
                log?.Invoke($"Elevation: ImpersonateLoggedOnUser({label}) failed — {Win32Error()}");
                return false;
            }
            impersonating = true;

            // Enable SeBackup/SeRestore on the SAME token now attached to the thread
            // (dup) — NOT the process token — so the action's registry writes get the
            // DACL bypass. Logged rather than fatal: if enablement fails we still want
            // the action to run and surface which specific writes then fail.
            if (enableBackupRestore)
            {
                bool b = EnablePrivilegeOnToken(dup, SE_BACKUP_NAME, log);
                bool r = EnablePrivilegeOnToken(dup, SE_RESTORE_NAME, log);
                log?.Invoke($"Elevation: DACL-bypass on {label} token — " +
                            $"SeBackupPrivilege={(b ? "enabled" : "FAILED")}, " +
                            $"SeRestorePrivilege={(r ? "enabled" : "FAILED")}.");
            }

            action();
            return true;
        }
        catch (Exception ex)
        {
            log?.Invoke($"Elevation: action under {label} threw — {ex.Message}");
            return false;
        }
        finally
        {
            // Drop the elevated identity before anything else can run on this thread.
            if (impersonating && !RevertToSelf())
                log?.Invoke($"Elevation: RevertToSelf failed — {Win32Error()}");

            if (dup     != IntPtr.Zero) CloseHandle(dup);
            if (token   != IntPtr.Zero) CloseHandle(token);
            if (process != IntPtr.Zero) CloseHandle(process);
        }
    }

    /// <summary>
    /// Enables a named privilege on the current process token (SeDebugPrivilege is
    /// present-but-disabled by default for elevated processes, so it only needs to be
    /// switched on, not granted). Opens the process token then delegates the actual
    /// LookupPrivilegeValue/AdjustTokenPrivileges work to <see cref="EnablePrivilegeOnToken"/>.
    /// </summary>
    private static bool EnablePrivilege(string privilege, Action<string>? log)
    {
        IntPtr token = IntPtr.Zero;
        try
        {
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out token))
            {
                log?.Invoke($"Elevation: OpenProcessToken(self) failed — {Win32Error()}");
                return false;
            }

            return EnablePrivilegeOnToken(token, privilege, log);
        }
        catch (Exception ex)
        {
            log?.Invoke($"Elevation: EnablePrivilege({privilege}) threw — {ex.Message}");
            return false;
        }
        finally
        {
            if (token != IntPtr.Zero) CloseHandle(token);
        }
    }

    /// <summary>
    /// Enables a named privilege on an ALREADY-OPEN token handle (which must carry
    /// TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY). Shared core used by both
    /// <see cref="EnablePrivilege"/> (process token) and the SeBackup/SeRestore
    /// enablement inside <see cref="RunAsProcessToken"/> (the duplicated impersonation
    /// token attached to the thread) — one mechanism, two token sources. The caller owns
    /// the handle's lifetime; this method does not close it.
    /// </summary>
    private static bool EnablePrivilegeOnToken(IntPtr token, string privilege, Action<string>? log)
    {
        if (!LookupPrivilegeValue(null, privilege, out var luid))
        {
            log?.Invoke($"Elevation: LookupPrivilegeValue({privilege}) failed — {Win32Error()}");
            return false;
        }

        var tp = new TOKEN_PRIVILEGES
        {
            PrivilegeCount = 1,
            Privilege = new LUID_AND_ATTRIBUTES { Luid = luid, Attributes = SE_PRIVILEGE_ENABLED }
        };

        // AdjustTokenPrivileges reports success even when the privilege was not held,
        // so the last error has to be checked explicitly.
        if (!AdjustTokenPrivileges(token, false, ref tp, (uint)Marshal.SizeOf<TOKEN_PRIVILEGES>(),
                                   IntPtr.Zero, IntPtr.Zero))
        {
            log?.Invoke($"Elevation: AdjustTokenPrivileges({privilege}) failed — {Win32Error()}");
            return false;
        }

        int err = Marshal.GetLastWin32Error();
        if (err != 0) // ERROR_NOT_ALL_ASSIGNED (1300) lands here
        {
            log?.Invoke($"Elevation: {privilege} not assigned (error {err}).");
            return false;
        }

        return true;
    }

    private static string Win32Error()
    {
        int code = Marshal.GetLastWin32Error();
        return $"Win32 error {code} ({new System.ComponentModel.Win32Exception(code).Message})";
    }
}
