using System.Globalization;
using System.Text.Json;
using Microsoft.Win32;

namespace AkariToolbox.Tweaks;

/// <summary>One captured registry value. A null <see cref="Kind"/> means the value
/// was absent at capture time, so revert deletes it rather than zero-filling it.</summary>
public sealed record JournalValue(
    string Path,
    string Name,
    string? Kind,
    string? DataHex,
    string? DataString,
    string[]? DataMulti);

/// <summary>All values captured for one catalog row, in capture order.</summary>
public sealed record JournalEntry(
    string CatalogId,
    DateTime TakenUtc,
    IReadOnlyList<JournalValue> Values);

/// <summary>Versioned journal document. Old versions are ignored with a warning.</summary>
public sealed record GamingJournalDoc(
    int Version,
    IReadOnlyList<JournalEntry> Entries);

/// <summary>Per-row outcome of a reverse replay.</summary>
public sealed record RevertResult(
    string CatalogId,
    bool Ok,
    string? Error);

/// <summary>
/// Snapshot-before-apply journal for the Gaming Catalog (SFT-01 revert half).
/// Captures the full footprint of each row immediately before that row's apply,
/// so stop-at-first-failure leaves a precise completed-prefix journal and
/// one-click revert replays it in reverse order. Revert continues past
/// single-row failures (unlike apply's stop-at-first-failure) so one stuck row
/// never strands the rest. Storage is a dedicated versioned JSON file with
/// atomic temp-plus-move writes; a corrupt file degrades to an empty journal.
/// </summary>
public sealed class GamingJournalStore
{
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _filePath;
    private readonly object _lock = new();
    private readonly Dictionary<string, JournalEntry> _pending = new(StringComparer.Ordinal);

    /// <summary>True when the last load found a corrupt or versioned-out file and reset to empty.</summary>
    public bool LastLoadWasReset { get; private set; }

    /// <summary>The revert authority beside settings.json (never mixed with user prefs).</summary>
    public static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AkariToolbox", "gaming-journal.json");

    public static GamingJournalStore Default => new(DefaultFilePath);

    public GamingJournalStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    /// <summary>Entries applied so far, in apply order (revert walks them in reverse).</summary>
    public IReadOnlyList<JournalEntry> AppliedEntries
    {
        get
        {
            lock (_lock)
            {
                return LoadLocked().Entries;
            }
        }
    }

    /// <summary>
    /// Snapshots every footprint value of <paramref name="entry"/> right now.
    /// Values that fail to read are skipped (never assumed absent — an assumed
    /// absent would wrongly delete user data on revert).
    /// </summary>
    public JournalEntry Capture(GamingCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var values = new List<JournalValue>();
        foreach ((string path, string name) in FootprintFor(entry))
        {
            JournalValue? captured = TryCaptureValue(path, name);
            if (captured is not null)
                values.Add(captured);
        }

        var capturedEntry = new JournalEntry(entry.Id, DateTime.UtcNow, values);
        lock (_lock)
        {
            _pending[entry.Id] = capturedEntry;
        }
        return capturedEntry;
    }

    /// <summary>
    /// Marks <paramref name="catalogId"/> applied: moves its pending capture into
    /// the persisted journal. Only completed rows are journaled (D-14 prefix).
    /// Re-capturing an ID replaces its snapshot (idempotent re-runs).
    /// </summary>
    public void MarkApplied(string catalogId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogId);
        lock (_lock)
        {
            if (!_pending.Remove(catalogId, out JournalEntry? captured))
                return;

            GamingJournalDoc current = LoadLocked();
            List<JournalEntry> entries = current.Entries
                .Where(e => e.CatalogId != catalogId).ToList();
            entries.Add(captured);
            SaveLocked(new GamingJournalDoc(CurrentVersion, entries));
        }
    }

    /// <summary>Replays applied entries in reverse order, continuing past single-row
    /// failures. Clears the journal after a fully successful revert so a repeat
    /// click reports nothing to revert.</summary>
    public IReadOnlyList<RevertResult> RevertAll()
    {
        IReadOnlyList<JournalEntry> entries;
        lock (_lock)
        {
            entries = LoadLocked().Entries;
        }

        var results = new List<RevertResult>();
        foreach (JournalEntry entry in entries.Reverse())
        {
            try
            {
                RevertEntry(entry);
                results.Add(new RevertResult(entry.CatalogId, true, null));
            }
            catch (Exception ex)
            {
                results.Add(new RevertResult(entry.CatalogId, false, ex.Message));
            }
        }

        if (results.All(r => r.Ok))
            Clear();

        return results;
    }

    /// <summary>Empties the journal (used after a fully successful revert).</summary>
    public void Clear()
    {
        lock (_lock)
        {
            SaveLocked(new GamingJournalDoc(CurrentVersion, []));
        }
    }

    /// <summary>Full footprint for an entry: dynamic hardware enumeration first,
    /// then the static list, then every value in a shared row's .reg body.</summary>
    public static IReadOnlyList<(string Path, string Name)> FootprintFor(GamingCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var footprint = new List<(string Path, string Name)>();
        if (entry.DynamicFootprint is not null)
        {
            try
            {
                footprint.AddRange(entry.DynamicFootprint());
            }
            catch
            {
                // Dynamic enumeration is best-effort; static entries still journal.
            }
        }
        footprint.AddRange(entry.Footprint);
        if (entry.SharedRow is not null)
            footprint.AddRange(ParseRegFootprint(entry.SharedRow.Optimize));
        return footprint;
    }

    /// <summary>
    /// Enumerates every (key, name) pair in a .reg body: section headers plus all
    /// assignment lines (dword, string, hex(n), and deletion markers). Generalized
    /// from the probe-first-value parser in <see cref="ControlPanelRows"/> — that
    /// file is never touched. Lines before any section header cannot be resolved
    /// and are skipped. Default-value (@) lines are skipped: they cannot be
    /// captured without risking the wrong value kind on write-back.
    /// </summary>
    internal static IReadOnlyList<(string Path, string Name)> ParseRegFootprint(string regBody)
    {
        var footprint = new List<(string Path, string Name)>();
        string? key = null;
        foreach (string rawLine in regBody.Split('\n'))
        {
            string line = rawLine.Trim().TrimEnd('\r');
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                key = line[1..^1];
                continue;
            }
            if (key is null)
                continue;
            int eq = line.IndexOf('=');
            if (eq < 0)
                continue;
            string name = line[..eq].Trim().Trim('"');
            if (name is "@" or "(Default)" or "")
                continue;
            if (!footprint.Contains((key, name)))
                footprint.Add((key, name));
        }
        return footprint;
    }

    private static JournalValue? TryCaptureValue(string path, string name)
    {
        try
        {
            using RegistryKey? key = OpenKey(path, writable: false);
            if (key is null)
                return new JournalValue(path, name, null, null, null, null);

            RegistryValueKind kind;
            object? raw;
            try
            {
                kind = key.GetValueKind(name);
                raw = key.GetValue(name);
            }
            catch (IOException)
            {
                // The value genuinely does not exist: revert must delete it.
                return new JournalValue(path, name, null, null, null, null);
            }

            return kind switch
            {
                RegistryValueKind.DWord when raw is int i =>
                    new JournalValue(path, name, "DWord", i.ToString("X8", CultureInfo.InvariantCulture), null, null),
                RegistryValueKind.QWord when raw is long l =>
                    new JournalValue(path, name, "QWord", l.ToString("X16", CultureInfo.InvariantCulture), null, null),
                RegistryValueKind.String when raw is string s =>
                    new JournalValue(path, name, "String", null, s, null),
                RegistryValueKind.ExpandString when raw is string s =>
                    new JournalValue(path, name, "ExpandString", null, s, null),
                RegistryValueKind.Binary when raw is byte[] bytes =>
                    new JournalValue(path, name, "Binary", Convert.ToHexString(bytes), null, null),
                RegistryValueKind.MultiString when raw is string[] strings =>
                    new JournalValue(path, name, "MultiString", null, null, strings),
                _ => null,
            };
        }
        catch
        {
            // Unreadable values are skipped, never assumed absent.
            return null;
        }
    }

    private static void RevertEntry(JournalEntry entry)
    {
        foreach (JournalValue value in entry.Values)
        {
            using RegistryKey? key = OpenKey(value.Path, writable: true);
            if (key is null)
                throw new IOException($"Cannot open registry key {value.Path}.");

            if (value.Kind is null)
            {
                key.DeleteValue(value.Name, throwOnMissingValue: false);
                continue;
            }

            switch (value.Kind)
            {
                case "DWord":
                    key.SetValue(value.Name,
                        Convert.ToInt32(value.DataHex, 16), RegistryValueKind.DWord);
                    break;
                case "QWord":
                    key.SetValue(value.Name,
                        Convert.ToInt64(value.DataHex, 16), RegistryValueKind.QWord);
                    break;
                case "String":
                    key.SetValue(value.Name, value.DataString ?? string.Empty, RegistryValueKind.String);
                    break;
                case "ExpandString":
                    key.SetValue(value.Name, value.DataString ?? string.Empty, RegistryValueKind.ExpandString);
                    break;
                case "Binary":
                    key.SetValue(value.Name,
                        Convert.FromHexString(value.DataHex ?? string.Empty), RegistryValueKind.Binary);
                    break;
                case "MultiString":
                    key.SetValue(value.Name, value.DataMulti ?? [], RegistryValueKind.MultiString);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown journal value kind '{value.Kind}'.");
            }
        }
    }

    /// <summary>
    /// HKCU paths resolve through the interactive-user hive (the app runs
    /// elevated, so plain HKCU would be the elevation account's hive); every
    /// other root resolves normally. Writes touch only footprint-declared keys.
    /// </summary>
    private static RegistryKey? OpenKey(string fullPath, bool writable)
    {
        int slash = fullPath.IndexOf('\\');
        if (slash < 0)
            return null;
        string root = fullPath[..slash].ToUpperInvariant();
        string sub = fullPath[(slash + 1)..];
        if (root is "HKCU" or "HKEY_CURRENT_USER")
            return AkariToolState.OpenRealHkcu(sub);

        var (baseKey, subPath) = RegistryPath.Resolve(fullPath);
        return writable
            ? baseKey.CreateSubKey(subPath, writable: true)
            : baseKey.OpenSubKey(subPath, writable: false);
    }

    private GamingJournalDoc LoadLocked()
    {
        if (!File.Exists(_filePath))
        {
            LastLoadWasReset = false;
            return new GamingJournalDoc(CurrentVersion, []);
        }

        try
        {
            string json = File.ReadAllText(_filePath);
            GamingJournalDoc? doc = JsonSerializer.Deserialize<GamingJournalDoc>(json, SerializerOptions);
            if (doc is null || doc.Version != CurrentVersion || doc.Entries is null)
            {
                LastLoadWasReset = true;
                return new GamingJournalDoc(CurrentVersion, []);
            }
            LastLoadWasReset = false;
            return doc;
        }
        catch
        {
            // A missing, corrupt or unreadable file falls back to an empty journal.
            LastLoadWasReset = true;
            return new GamingJournalDoc(CurrentVersion, []);
        }
    }

    private void SaveLocked(GamingJournalDoc doc)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(doc, SerializerOptions);

        // Write to a temp file then atomically move it into place, so a crash
        // mid-write can never leave a corrupt journal behind.
        string tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _filePath, overwrite: true);
    }
}
