using System.Collections.Concurrent;

namespace AkariToolbox.App.Services;

/// <inheritdoc cref="ITweakCatalog"/>
public sealed class TweakCatalog : ITweakCatalog
{
    private readonly Dictionary<string, ITweakHandler> _handlersByKey;
    private readonly Dictionary<string, bool> _priorState = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly object _priorStateLock = new();

    public TweakCatalog(IEnumerable<ITweakHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        Handlers = handlers.OrderBy(h => h.Order).ToList();
        _handlersByKey = Handlers.ToDictionary(h => h.Key);
    }

    public IReadOnlyList<ITweakHandler> Handlers { get; }

    public Task<bool> GetStateAsync(string key)
    {
        var handler = ResolveHandler(key);
        // A live read has no side effects — no lock needed.
        return Task.Run(handler.GetState);
    }

    public async Task SetStateAsync(string key, bool enabled)
    {
        var handler = ResolveHandler(key);
        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var current = await Task.Run(handler.GetState).ConfigureAwait(false);
            if (current == enabled)
            {
                // Already at the requested state — no-op, satisfies idempotency.
                return;
            }

            lock (_priorStateLock)
            {
                if (!_priorState.ContainsKey(key))
                {
                    // First mutation of this key in the current app session —
                    // capture the real prior value so a later revert restores it
                    // (TWEAKS-03), rather than a hardcoded default.
                    _priorState[key] = current;
                }
            }

            await Task.Run(() => handler.SetState(enabled)).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private ITweakHandler ResolveHandler(string key) =>
        _handlersByKey.TryGetValue(key, out var handler)
            ? handler
            : throw new KeyNotFoundException($"No ITweakHandler registered for key '{key}'.");
}
