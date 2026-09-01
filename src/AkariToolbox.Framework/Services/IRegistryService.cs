using Microsoft.Win32;

namespace AkariToolbox.Framework.Services;

/// <summary>
/// Registry-squatting-safe wrapper over <see cref="RegistryKey"/> — every read is
/// null-checked (never throws on a missing key), every write opens-then-creates
/// rather than blindly creating, and the elevated-process real-user-HKCU trick is
/// isolated behind <see cref="OpenRealUserHive"/> so it is written and tested once.
/// </summary>
public interface IRegistryService
{
    /// <summary>Reads a value, returning <c>null</c> if the key or value does not exist.</summary>
    object? GetValue(RegistryHive hive, string subKeyPath, string valueName);

    /// <summary>
    /// Writes a value. Opens the sub-key for write first; only creates it when the
    /// parent path is legitimately absent (never a blind unconditional create).
    /// </summary>
    void SetValue(RegistryHive hive, string subKeyPath, string valueName, object value, RegistryValueKind kind);

    /// <summary>Deletes a value if present; no-op (never throws) if it is already absent.</summary>
    void DeleteValue(RegistryHive hive, string subKeyPath, string valueName);

    /// <summary>Returns direct child subkey names under subKeyPath, or an empty list if absent.</summary>
    IReadOnlyList<string> GetSubKeyNames(RegistryHive hive, string subKeyPath);

    /// <summary>
    /// Deletes <paramref name="subKeyPath"/> and its entire subtree if present; no-op
    /// (never throws) if it is already absent — mirrors <see cref="DeleteValue"/>'s
    /// never-throws-on-missing convention (registry-squatting-safe).
    /// </summary>
    void DeleteSubKeyTree(RegistryHive hive, string subKeyPath);

    /// <summary>
    /// Opens (creating if needed) <paramref name="subKeyPath"/> with no value set — used
    /// for the "delete then recreate empty" pattern (e.g. AMD Settings' Notification
    /// subkey). Idempotent: a no-op if the subkey already exists.
    /// </summary>
    void CreateSubKey(RegistryHive hive, string subKeyPath);

    /// <summary>
    /// Opens (creating if needed) a sub-key under the real interactive user's HKCU hive,
    /// even though this process is elevated (whose own <c>Registry.CurrentUser</c> may
    /// resolve to a different hive). Ported from the predecessor's
    /// <c>CreateRealHkcuSubKey</c> — hard-throws <see cref="InvalidOperationException"/>
    /// if no <c>explorer.exe</c> process exists, by design (D-14), no fallback.
    /// </summary>
    RegistryKey OpenRealUserHive(string subKeyPath);
}
