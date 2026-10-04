namespace AkariToolbox.Tweaks;

/// <summary>
/// Sequential bulk apply over ticked catalog rows (SFT-01 apply half, D-11/D-14).
/// UI-free so the stop-at-first-failure and completed-prefix journal semantics are
/// covered by unit tests; the view model only orchestrates dialogs and progress.
/// Ticks are never mutated here (D-12: ticks stay sticky for re-run/inspect).
/// </summary>
public static class GamingRunner
{
    /// <summary>Determinate progress for one completed row.</summary>
    public sealed record BulkProgress(int Done, int Total, string Title);

    /// <summary>Outcome: how many rows applied before the run ended, plus the
    /// first failure (null when every row applied).</summary>
    public sealed record BulkResult(int Done, GamingCatalogEntry? FailedEntry, string? FailedMessage);

    /// <summary>
    /// Applies <paramref name="entries"/> in order: capture, apply, journal —
    /// per row. Stops at the first failure, leaving a completed-prefix journal
    /// that <see cref="GamingJournalStore.RevertAll"/> replays in reverse.
    /// </summary>
    public static async Task<BulkResult> RunAsync(
        IReadOnlyList<GamingCatalogEntry> entries,
        GamingJournalStore store,
        IProgress<BulkProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(store);

        int done = 0;
        foreach (GamingCatalogEntry entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await Task.Run(() =>
                {
                    store.Capture(entry);
                    entry.Apply(true);
                    store.MarkApplied(entry.Id);
                }, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return new BulkResult(done, entry, ex.Message);
            }
            done++;
            progress?.Report(new BulkProgress(done, entries.Count, entry.Title));
        }
        return new BulkResult(done, null, null);
    }
}
