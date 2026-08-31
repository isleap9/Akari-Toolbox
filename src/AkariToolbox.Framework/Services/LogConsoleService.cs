using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using AkariToolbox.Framework.Threading;

namespace AkariToolbox.Framework.Services;

/// <inheritdoc cref="ILogConsoleService"/>
public sealed class LogConsoleService : ILogConsoleService
{
    private readonly DispatcherQueue? _dispatcher;

    /// <summary>
    /// Captures <paramref name="dispatcher"/> at construction time. Production DI resolves
    /// this via <see cref="DispatcherQueue.GetForCurrentThread"/> captured while the
    /// singleton is first constructed on the UI thread (see
    /// <c>ServiceCollectionExtensions.Primitives.cs</c>). Passing <c>null</c> — as unit tests
    /// do, since a plain xunit thread has no <see cref="DispatcherQueue"/> — makes every
    /// <see cref="Log"/> call append synchronously instead of marshaling, which is what lets
    /// the append/no-dedup behavior be unit-tested headless. True cross-thread dispatcher
    /// marshaling is verified by this plan's manual human-check instead.
    /// </summary>
    public LogConsoleService(DispatcherQueue? dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public ObservableCollection<string> Lines { get; } = [];

    public void Log(string message)
    {
        if (_dispatcher is null || _dispatcher.HasThreadAccess)
        {
            Lines.Add(message);
            return;
        }

        // Fire-and-forget by design: callers do not await UI updates.
        _ = _dispatcher.RunOnUIThreadAsync(() => Lines.Add(message));
    }
}
