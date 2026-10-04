using AkariToolbox.Tweaks;
using Microsoft.Win32;
using Xunit;

namespace AppTemplate.Tests;

/// <summary>
/// Tracer-slice coverage for the Gaming Catalog (BRW-01/BRW-02/SFT-01):
/// catalog integrity, title-only prefix-first search ranking, journal
/// round-trip on HKCU scratch keys, and bulk stop-at-first-failure semantics.
/// All tests are UI-free; no entry Read/Apply delegate touching real gaming
/// keys is ever invoked — fakes and isolated scratch keys only.
/// </summary>
public class GamingCatalogTests
{
    // ---- Catalog integrity ----

    [Fact]
    public void Catalog_ids_are_unique_and_stable()
    {
        string[] ids = GamingCatalogEntry.All.Select(e => e.Id).ToArray();

        Assert.Equal(ids.Length, ids.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("disable-preemption", ids);
        Assert.Contains("network-optimization", ids);
        Assert.Contains("game-mode", ids);
        Assert.Contains("game-dvr", ids);
    }

    [Fact]
    public void Catalog_groups_cover_all_four_sections()
    {
        GamingGroup[] groups = GamingCatalogEntry.All.Select(e => e.Group).Distinct().ToArray();

        Assert.Contains(GamingGroup.System, groups);
        Assert.Contains(GamingGroup.Network, groups);
        Assert.Contains(GamingGroup.Gpu, groups);
        Assert.Contains(GamingGroup.Background, groups);
    }

    [Fact]
    public void Catalog_shared_row_ids_resolve_against_control_panel_rows()
    {
        var shared = GamingCatalogEntry.All.Where(e => e.SharedRowId is not null).ToList();

        Assert.NotEmpty(shared);
        foreach (GamingCatalogEntry entry in shared)
        {
            Assert.NotNull(entry.SharedRow);
            Assert.Contains(ControlPanelRows.All, r => r.Id == entry.SharedRowId);
        }
    }

    [Fact]
    public void Catalog_every_entry_has_four_non_empty_explanation_lines()
    {
        foreach (GamingCatalogEntry entry in GamingCatalogEntry.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.WhatItDoes), entry.Id);
            Assert.False(string.IsNullOrWhiteSpace(entry.WhyItHelpsGaming), entry.Id);
            Assert.False(string.IsNullOrWhiteSpace(entry.Risk), entry.Id);
            Assert.False(string.IsNullOrWhiteSpace(entry.RevertsBy), entry.Id);
        }
    }

    [Fact]
    public void Catalog_explanations_name_no_implementation_locations()
    {
        string[] forbidden = ["HKLM", "HKCU", "HKEY_", "regedit"];
        foreach (GamingCatalogEntry entry in GamingCatalogEntry.All)
        {
            string copy = string.Join("\n",
                entry.WhatItDoes, entry.WhyItHelpsGaming, entry.Risk, entry.RevertsBy);
            foreach (string marker in forbidden)
                Assert.DoesNotContain(marker, copy, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Catalog_scheduler_row_requires_reboot_while_mode_and_dvr_do_not()
    {
        GamingCatalogEntry preemption = GamingCatalogEntry.All.First(e => e.Id == "disable-preemption");
        GamingCatalogEntry gameMode = GamingCatalogEntry.All.First(e => e.Id == "game-mode");
        GamingCatalogEntry gameDvr = GamingCatalogEntry.All.First(e => e.Id == "game-dvr");

        Assert.True(preemption.RequiresReboot);
        Assert.False(gameMode.RequiresReboot);
        Assert.False(gameDvr.RequiresReboot);
    }

    // ---- Search ranking (BRW-01; D-06 through D-09) ----

    [Fact]
    public void Suggest_prefix_matches_rank_above_substring_matches()
    {
        IReadOnlyList<GamingCatalogEntry> results = GamingCatalogEntry.Suggest("game");

        Assert.NotEmpty(results);
        Assert.Equal("game-mode", results[0].Id);
        Assert.StartsWith("game", results[0].Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Suggest_matches_titles_only_not_explanations()
    {
        // "journal" appears in every entry's RevertsBy line but in no title.
        Assert.Empty(GamingCatalogEntry.Suggest("journal"));
        Assert.Empty(GamingCatalogEntry.Suggest("scheduler"));
    }

    [Fact]
    public void Suggest_triggers_from_first_character_and_caps_at_eight()
    {
        Assert.NotEmpty(GamingCatalogEntry.Suggest("g"));
        Assert.True(GamingCatalogEntry.Suggest("e").Count <= 8);
        Assert.Equal(2, GamingCatalogEntry.Suggest("e", cap: 2).Count);
    }

    [Fact]
    public void Suggest_empty_query_means_no_filter()
    {
        Assert.Empty(GamingCatalogEntry.Suggest(string.Empty));
        Assert.Empty(GamingCatalogEntry.Suggest("   "));

        foreach (GamingCatalogEntry entry in GamingCatalogEntry.All)
            Assert.True(GamingCatalogEntry.MatchesFilter(entry, string.Empty));
    }

    [Fact]
    public void MatchesFilter_is_case_insensitive_substring_on_title()
    {
        GamingCatalogEntry mode = GamingCatalogEntry.All.First(e => e.Id == "game-mode");

        Assert.True(GamingCatalogEntry.MatchesFilter(mode, "GAME"));
        Assert.True(GamingCatalogEntry.MatchesFilter(mode, "Mode"));
        Assert.False(GamingCatalogEntry.MatchesFilter(mode, "dvr"));
    }

    // ---- Journal round-trip (SFT-01 revert half; D-14/D-16) ----

    [Fact]
    public void Journal_capture_then_mutate_then_revert_restores_sentinel()
    {
        string subKey = NewScratchSubKey();
        string path = "HKCU\\Software\\AkariToolboxTests\\GamingCatalog\\" + subKey;
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: true))
                key.SetValue("Sentinel", unchecked((int)0xDEADBEEF), RegistryValueKind.DWord);

            var store = NewScratchStore();
            GamingCatalogEntry entry = ScratchEntry("round-trip-sentinel", path, "Sentinel");

            store.Capture(entry);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: true))
                key.SetValue("Sentinel", 0, RegistryValueKind.DWord);
            store.MarkApplied(entry.Id);

            IReadOnlyList<RevertResult> results = store.RevertAll();

            Assert.Single(results);
            Assert.True(results[0].Ok);
            using RegistryKey? verify = OpenScratch(subKey);
            Assert.Equal(unchecked((int)0xDEADBEEF), verify?.GetValue("Sentinel"));
        }
        finally
        {
            DropScratch(subKey);
        }
    }

    [Fact]
    public void Journal_absent_value_reverts_by_deleting_not_zero_filling()
    {
        string subKey = NewScratchSubKey();
        string path = "HKCU\\Software\\AkariToolboxTests\\GamingCatalog\\" + subKey;
        try
        {
            var store = NewScratchStore();
            GamingCatalogEntry entry = ScratchEntry("round-trip-absent", path, "Ghost");

            store.Capture(entry);
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: true))
                key.SetValue("Ghost", 7, RegistryValueKind.DWord);
            store.MarkApplied(entry.Id);

            IReadOnlyList<RevertResult> results = store.RevertAll();

            Assert.Single(results);
            Assert.True(results[0].Ok);
            using RegistryKey? verify = OpenScratch(subKey);
            Assert.Null(verify?.GetValue("Ghost"));
        }
        finally
        {
            DropScratch(subKey);
        }
    }

    [Fact]
    public void Journal_multi_value_row_captures_every_footprint_value()
    {
        string subKey = NewScratchSubKey();
        string path = "HKCU\\Software\\AkariToolboxTests\\GamingCatalog\\" + subKey;
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: true))
            {
                key.SetValue("First", 1, RegistryValueKind.DWord);
                key.SetValue("Second", 2, RegistryValueKind.DWord);
                key.SetValue("Label", "keep-me", RegistryValueKind.String);
            }

            var store = NewScratchStore();
            GamingCatalogEntry entry = GamingCatalogEntry.FromNative(
                "round-trip-multi", GamingGroup.System, "Scratch Multi",
                "Scratch.", "Scratch.", "Scratch.", "Scratch.", false,
                () => false, _ => { },
                footprint: [(path, "First"), (path, "Second"), (path, "Label")]);

            JournalEntry captured = store.Capture(entry);

            Assert.Equal(3, captured.Values.Count);
            store.MarkApplied(entry.Id);

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: true))
            {
                key.SetValue("First", 100, RegistryValueKind.DWord);
                key.SetValue("Second", 200, RegistryValueKind.DWord);
                key.SetValue("Label", "changed", RegistryValueKind.String);
            }
            Assert.True(store.RevertAll().All(r => r.Ok));

            using RegistryKey? verify = OpenScratch(subKey);
            Assert.Equal(1, verify?.GetValue("First"));
            Assert.Equal(2, verify?.GetValue("Second"));
            Assert.Equal("keep-me", verify?.GetValue("Label"));
        }
        finally
        {
            DropScratch(subKey);
        }
    }

    [Fact]
    public void Journal_corrupt_file_degrades_to_empty()
    {
        string file = Path.Combine(Path.GetTempPath(), "gaming-journal-corrupt-" + Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(file, "{{not valid json");
            var store = new GamingJournalStore(file);

            Assert.Empty(store.AppliedEntries);
            Assert.True(store.LastLoadWasReset);
            Assert.Empty(store.RevertAll());
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Journal_revert_replays_in_reverse_apply_order()
    {
        string subKey = NewScratchSubKey();
        string path = "HKCU\\Software\\AkariToolboxTests\\GamingCatalog\\" + subKey;
        try
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: true))
                key.SetValue("Order", 0, RegistryValueKind.DWord);

            var store = NewScratchStore();
            GamingCatalogEntry first = ScratchEntry("order-first", path, "Order");
            GamingCatalogEntry second = ScratchEntry("order-second", path, "Order");

            store.Capture(first); // captures Order = 0
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: true))
                key.SetValue("Order", 1, RegistryValueKind.DWord);
            store.MarkApplied(first.Id);

            store.Capture(second); // captures Order = 1
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: true))
                key.SetValue("Order", 2, RegistryValueKind.DWord);
            store.MarkApplied(second.Id);

            // Reverse replay writes 1 then 0; forward order would leave 1 behind.
            Assert.True(store.RevertAll().All(r => r.Ok));
            using RegistryKey? verify = OpenScratch(subKey);
            Assert.Equal(0, verify?.GetValue("Order"));
        }
        finally
        {
            DropScratch(subKey);
        }
    }

    // ---- Bulk semantics (SFT-01 apply half; D-11/D-12/D-14) ----

    [Fact]
    public async Task Runner_stop_at_first_failure_leaves_completed_prefix_journal()
    {
        var store = NewScratchStore();
        var applied = new List<string>();
        GamingCatalogEntry ok = FakeEntry("bulk-ok", applied);
        GamingCatalogEntry fail = GamingCatalogEntry.FromNative(
            "bulk-fail", GamingGroup.System, "Bulk Fail",
            "Fake.", "Fake.", "Fake.", "Fake.", false,
            () => false, _ => throw new InvalidOperationException("boom"));
        GamingCatalogEntry never = FakeEntry("bulk-never", applied);

        GamingRunner.BulkResult result =
            await GamingRunner.RunAsync([ok, fail, never], store);

        Assert.Equal(1, result.Done);
        Assert.Same(fail, result.FailedEntry);
        Assert.Equal("boom", result.FailedMessage);
        Assert.DoesNotContain("bulk-never", applied);
        Assert.Equal(["bulk-ok"], store.AppliedEntries.Select(e => e.CatalogId));
    }

    [Fact]
    public async Task Runner_success_applies_all_and_reports_progress()
    {
        var store = NewScratchStore();
        var applied = new List<string>();
        var seen = new List<GamingRunner.BulkProgress>();
        var progress = new Progress<GamingRunner.BulkProgress>(p => seen.Add(p));

        GamingRunner.BulkResult result = await GamingRunner.RunAsync(
            [FakeEntry("bulk-a", applied), FakeEntry("bulk-b", applied)], store, progress);

        Assert.Equal(2, result.Done);
        Assert.Null(result.FailedEntry);
        Assert.Equal(["bulk-a", "bulk-b"], applied);
        Assert.Equal(2, seen.Count);
        Assert.Equal((2, 2), (seen[1].Done, seen[1].Total));
        Assert.Equal(2, store.AppliedEntries.Count);
    }

    // ---- Helpers (private sealed doubles and scratch isolation live here) ----

    private static GamingCatalogEntry FakeEntry(string id, List<string> applied) =>
        GamingCatalogEntry.FromNative(
            id, GamingGroup.System, "Fake " + id,
            "Fake.", "Fake.", "Fake.", "Fake.", false,
            () => false, _ => applied.Add(id));

    private static GamingCatalogEntry ScratchEntry(string id, string path, string name) =>
        GamingCatalogEntry.FromNative(
            id, GamingGroup.System, "Scratch " + id,
            "Scratch.", "Scratch.", "Scratch.", "Scratch.", false,
            () => false, _ => { },
            footprint: [(path, name)]);

    private static GamingJournalStore NewScratchStore() => new(
        Path.Combine(Path.GetTempPath(), "gaming-journal-test-" + Guid.NewGuid() + ".json"));

    private static string NewScratchSubKey() => "scratch-" + Guid.NewGuid().ToString("N");

    private static RegistryKey? OpenScratch(string subKey) =>
        Registry.CurrentUser.OpenSubKey(
            "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, writable: false);

    private static void DropScratch(string subKey)
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree(
                "Software\\AkariToolboxTests\\GamingCatalog\\" + subKey, throwOnMissingSubKey: false);
        }
        catch
        {
            // Best-effort cleanup; the GUID suffix keeps leftovers isolated.
        }
    }
}
