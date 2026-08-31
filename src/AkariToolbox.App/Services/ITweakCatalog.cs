namespace AkariToolbox.App.Services;

/// <summary>
/// Orchestrates every registered <see cref="ITweakHandler"/>: exposes the
/// Order-sorted handler list, and centralizes the capture-then-write sequencing
/// (read live state before mutating, store the real prior value once per
/// session, skip no-op writes) so no individual handler re-implements the
/// ordering (TWEAKS-01/TWEAKS-03).
/// </summary>
public interface ITweakCatalog
{
    /// <summary>All registered handlers, sorted by <see cref="ITweakHandler.Order"/> ascending.</summary>
    IReadOnlyList<ITweakHandler> Handlers { get; }

    /// <summary>Reads the live current state for the tweak identified by <paramref name="key"/>.</summary>
    Task<bool> GetStateAsync(string key);

    /// <summary>
    /// Applies <paramref name="enabled"/> for the tweak identified by <paramref name="key"/>.
    /// No-ops if the live state already equals <paramref name="enabled"/>. Captures the
    /// real prior value the first time this key is mutated in the current app session.
    /// </summary>
    Task SetStateAsync(string key, bool enabled);
}
