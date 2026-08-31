using System.Collections.ObjectModel;

namespace AkariToolbox.Framework.Services;

/// <summary>
/// In-memory, dispatcher-safe log console consumed by the app shell's collapsible
/// log dock (D-05/D-06/D-07) and any background caller (registry reads, service
/// calls, script output) that needs to report progress or failures visibly, per
/// the app's core value statement that no background operation fails silently.
/// </summary>
public interface ILogConsoleService
{
    /// <summary>All logged lines, in append order. Bound directly to the shell's log dock.</summary>
    ObservableCollection<string> Lines { get; }

    /// <summary>
    /// Appends <paramref name="message"/> unconditionally (no dedup, no filtering) —
    /// matching the predecessor's <c>TxtLog.AppendText</c> behavior (APP-05 idempotency:
    /// repeated calls with the same message produce repeated distinct entries by design).
    /// Safe to call from any thread; UI mutation is always marshaled onto the dispatcher.
    /// </summary>
    void Log(string message);
}
