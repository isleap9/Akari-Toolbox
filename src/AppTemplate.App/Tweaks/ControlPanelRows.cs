using System.Globalization;

namespace AkariToolbox.Tweaks;

/// <summary>
/// Live-state probe + apply for the generated granular rows. The probe compares
/// the first concrete value in the Optimize body against the registry (the common
/// shape: Optimize writes a dword/string, Default deletes or resets it).
/// </summary>
public static partial class ControlPanelRows
{
    private sealed record Probe(string Key, string Name, bool IsDword, int DwordValue, string StringValue);

    private static readonly Dictionary<string, Probe?> ProbeCache = new(StringComparer.Ordinal);

    /// <returns>True when the Optimize values read back live, false when they
    /// don't, null when the row has no probed value (use persisted state).</returns>
    public static bool? ReadRowOptimized(CpTweakRow row)
    {
        Probe? probe;
        lock (ProbeCache)
        {
            if (!ProbeCache.TryGetValue(row.Id, out probe))
            {
                probe = ParseProbe(row.Optimize);
                ProbeCache[row.Id] = probe;
            }
        }
        if (probe is null)
            return null;

        try
        {
            if (probe.IsDword)
                return RegRead.Dword(probe.Key, probe.Name) == probe.DwordValue;
            return RegRead.String(probe.Key, probe.Name) == probe.StringValue;
        }
        catch
        {
            return null;
        }
    }

    public static void ApplyRow(CpTweakRow row, bool optimize) =>
        NativeOps.ImportRegContent(optimize ? row.Optimize : row.Default);

    private static Probe? ParseProbe(string regBody)
    {
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
            if (name == "@" || name == "(Default)")
                name = string.Empty;
            string data = line[(eq + 1)..].Trim();
            if (data.StartsWith("dword:", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(data["dword:".Length..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int dword))
                return new Probe(key, name, true, dword, string.Empty);
            if (data.Length >= 2 && data.StartsWith('"') && data.EndsWith('"'))
                return new Probe(key, name, false, 0, data[1..^1]);
        }
        return null;
    }
}
