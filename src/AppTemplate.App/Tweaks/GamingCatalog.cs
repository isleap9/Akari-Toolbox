namespace AkariToolbox.Tweaks;

/// <summary>One of the four catalog groups on the Gaming Catalog page (D-03).</summary>
public enum GamingGroup
{
    System,
    Network,
    Gpu,
    Background,
}

/// <summary>
/// One searchable, explainable gaming tweak (BRW-01/BRW-02).
/// Native entries carry read/apply delegates plus an explicit registry footprint;
/// shared-row entries cross-reference a <see cref="CpTweakRow"/> by ID and delegate
/// probe/apply to <see cref="ControlPanelRows"/> (D-04, never duplicated .reg text).
/// Explanation lines render inline and never name implementation locations.
/// </summary>
public sealed record GamingCatalogEntry(
    string Id,
    GamingGroup Group,
    string Title,
    string WhatItDoes,
    string WhyItHelpsGaming,
    string Risk,
    string RevertsBy,
    bool RequiresReboot,
    Func<bool> Read,
    Action<bool> Apply,
    string? SharedRowId = null)
{
    /// <summary>
    /// Registry values snapshotted before apply and written back on revert.
    /// Completeness is driven by this footprint, never by the read probe.
    /// </summary>
    public IReadOnlyList<(string Path, string Name)> Footprint { get; init; } = [];

    /// <summary>
    /// Dynamic footprint for entries whose keys depend on live hardware
    /// (network adapters). Takes precedence over <see cref="Footprint"/>.
    /// </summary>
    public Func<IReadOnlyList<(string Path, string Name)>>? DynamicFootprint { get; init; }

    /// <summary>Resolved shared row for cross-referenced entries; null for natives.</summary>
    public CpTweakRow? SharedRow { get; init; }

    /// <summary>Display title for a catalog group.</summary>
    public static string GroupTitle(GamingGroup group) => group switch
    {
        GamingGroup.System => "System",
        GamingGroup.Network => "Network",
        GamingGroup.Gpu => "GPU",
        GamingGroup.Background => "Background",
        _ => group.ToString(),
    };

    /// <summary>
    /// Title-only prefix-first suggestions (D-06/D-08): StartsWith matches first,
    /// then Contains matches, each in catalog order, capped at <paramref name="cap"/>
    /// (D-07: matches from the first character; empty query means no filter).
    /// </summary>
    public static IReadOnlyList<GamingCatalogEntry> Suggest(string query, int cap = 8)
    {
        if (string.IsNullOrWhiteSpace(query) || cap <= 0)
            return [];
        List<GamingCatalogEntry> ranked = [.. All.Where(e => e.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase)),
                .. All.Where(e => !e.Title.StartsWith(query, StringComparison.OrdinalIgnoreCase)
                               && e.Title.Contains(query, StringComparison.OrdinalIgnoreCase))];
        return ranked.Take(cap).ToList();
    }

    /// <summary>True when the entry's title matches the filter (empty query matches all).</summary>
    public static bool MatchesFilter(GamingCatalogEntry entry, string query)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return string.IsNullOrWhiteSpace(query)
            || entry.Title.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Native entry factory (preemption, network opt, Game Mode, DVR):
    /// caller supplies the live probe, the symmetric apply, and the footprint.
    /// </summary>
    public static GamingCatalogEntry FromNative(
        string id, GamingGroup group, string title,
        string whatItDoes, string whyItHelpsGaming, string risk, string revertsBy,
        bool requiresReboot, Func<bool> read, Action<bool> apply,
        IReadOnlyList<(string Path, string Name)>? footprint = null,
        Func<IReadOnlyList<(string Path, string Name)>>? dynamicFootprint = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(read);
        ArgumentNullException.ThrowIfNull(apply);
        return new GamingCatalogEntry(id, group, title, whatItDoes, whyItHelpsGaming,
            risk, revertsBy, requiresReboot, read, apply)
        {
            Footprint = footprint ?? [],
            DynamicFootprint = dynamicFootprint,
        };
    }

    /// <summary>
    /// Shared-row factory (D-04): resolves nothing here — the caller passes the
    /// <see cref="CpTweakRow"/> looked up once in <see cref="ControlPanelRows.All"/>
    /// (fail-fast on rename). Probe delegates to first-value read with a persisted
    /// fallback; apply delegates to <see cref="ControlPanelRows.ApplyRow"/>.
    /// </summary>
    public static GamingCatalogEntry FromSharedRow(
        CpTweakRow row, GamingGroup group,
        string whatItDoes, string whyItHelpsGaming, string risk, string revertsBy,
        bool requiresReboot, string? titleOverride = null)
    {
        ArgumentNullException.ThrowIfNull(row);
        return new GamingCatalogEntry(row.Id, group, titleOverride ?? row.Title,
            whatItDoes, whyItHelpsGaming, risk, revertsBy, requiresReboot,
            () => ReadSharedRow(row),
            on => ControlPanelRows.ApplyRow(row, on),
            row.Id)
        {
            SharedRow = row,
        };
    }

    /// <summary>Live probe first, persisted choice as fallback, never throws.</summary>
    private static bool ReadSharedRow(CpTweakRow row)
    {
        try
        {
            return ControlPanelRows.ReadRowOptimized(row)
                ?? AkariToolState.GetInt("CpRow:" + row.Id, 0) == 1;
        }
        catch
        {
            return AkariToolState.GetInt("CpRow:" + row.Id, 0) == 1;
        }
    }

    /// <summary>
    /// Fail-fast lookup: a renamed shared row breaks the build-time catalog
    /// instead of silently dropping a gaming tweak.
    /// </summary>
    private static CpTweakRow SharedRowById(string id)
    {
        foreach (CpTweakRow row in ControlPanelRows.All)
        {
            if (row.Id == id)
                return row;
        }
        throw new KeyNotFoundException($"Shared Control Panel row '{id}' no longer exists.");
    }

    public static IReadOnlyList<GamingCatalogEntry> All { get; } = Build();

    private static IReadOnlyList<GamingCatalogEntry> Build() => [
        // ---- System ----
        FromNative(
            "disable-preemption", GamingGroup.System, "Disable Preemption (NVIDIA)",
            "Turns off GPU thread preemption so frames render without interruption.",
            "Steadier frame pacing in fast scenes on NVIDIA cards.",
            "Rare stutters or driver quirks on some setups.",
            "Puts back all 6 saved scheduler settings from the journal.",
            true,
            GamingActions.ReadPreemption,
            on => GamingActions.SetPreemption(on),
            footprint: [
                (@"HKLM\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Scheduler", "EnablePreemption"),
                (@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm", "DisablePreemption"),
                (@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm", "DisableCudaContextPreemption"),
                (@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm", "EnableCEPreemption"),
                (@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm", "DisablePreemptionOnS3S4"),
                (@"HKLM\SYSTEM\CurrentControlSet\Services\nvlddmkm", "ComputePreemption"),
            ]),
        FromNative(
            "game-mode", GamingGroup.System, "Game Mode",
            "Lets Windows prioritize your game while it is running.",
            "Fewer background interruptions while you play.",
            "Some overlays may behave differently with it on.",
            "Restores your previous Game Mode choice from the journal.",
            false,
            GamingActions.ReadGameMode,
            on => GamingActions.SetGameMode(on),
            footprint: [
                (@"HKCU\Software\Microsoft\GameBar", "AllowAutoGameMode"),
                (@"HKCU\Software\Microsoft\GameBar", "AutoGameModeEnabled"),
            ]),
        // ---- Network ----
        FromNative(
            "network-optimization", GamingGroup.Network, "Network Optimization",
            "Tunes your network adapter and connection settings for lower latency.",
            "Smoother online play with fewer lag spikes.",
            "Rare adapter quirks, and a reboot may be needed.",
            "Puts back your saved adapter settings from the journal.",
            true,
            GamingActions.ReadNetworkOptimization,
            on =>
            {
                if (on)
                    GamingActions.ApplyNetworkOptimization();
                else
                    GamingActions.RevertNetworkOptimization();
            },
            dynamicFootprint: GamingActions.EnumerateNetworkFootprint),
        // ---- GPU ----
        FromNative(
            "game-dvr", GamingGroup.Gpu, "Disable Game DVR",
            "Turns off background game recording and capture services.",
            "Frees CPU and GPU work so games hold higher frame rates.",
            "You lose instant-replay clips until you turn it back on.",
            "Restores your previous recording settings from the journal.",
            false,
            GamingActions.ReadGameDvr,
            on => GamingActions.SetGameDvr(on),
            footprint: [
                (@"HKCU\System\GameConfigStore", "GameDVR_Enabled"),
                (@"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled"),
                (@"HKLM\SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR"),
                (@"HKLM\SYSTEM\CurrentControlSet\Services\BcastDVRUserService", "Start"),
            ]),
        // ---- Background (shared-row cross-references, D-04) ----
        FromSharedRow(
            SharedRowById("disable_game_bar"), GamingGroup.Background,
            "Switches off the Game Bar overlay and its helpers.",
            "Less overlay overhead while gaming.",
            "Game Bar shortcuts stop working until re-enabled.",
            "Restores your previous Game Bar settings from the journal.",
            false),
        FromSharedRow(
            SharedRowById("disable_enable_open_xbox_game_bar_using_game"), GamingGroup.Background,
            "Stops the controller button from opening Game Bar.",
            "No accidental overlay popups mid-match.",
            "You will need the keyboard shortcut to open Game Bar.",
            "Restores your previous shortcut setting from the journal.",
            false),
        FromSharedRow(
            SharedRowById("disable_use_view_menu_as_guide_button_in_app"), GamingGroup.Background,
            "Stops View plus Menu from acting as the Guide button in apps.",
            "Buttons behave normally instead of summoning the overlay.",
            "Guide-button shortcuts in apps stop working.",
            "Restores your previous button setting from the journal.",
            false),
        FromSharedRow(
            SharedRowById("other_settings"), GamingGroup.Background,
            "Trims background recording quality and history settings.",
            "Less disk and CPU spent on clips you never watch.",
            "Saved clip quality drops until you revert.",
            "Restores every saved capture setting from the journal.",
            false,
            titleOverride: "Background Recording Settings"),
    ];
}
