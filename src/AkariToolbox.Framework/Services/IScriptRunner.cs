namespace AkariToolbox.Framework.Services;

/// <summary>
/// Runs an external process (bcdedit, DISM, powershell.exe, etc.), streaming every
/// captured stdout/stderr line to <see cref="ILogConsoleService"/> so no background
/// operation's output is ever silently swallowed (this app's core value statement).
/// </summary>
public interface IScriptRunner
{
    /// <summary>
    /// Runs <paramref name="fileName"/> with <paramref name="arguments"/>, returning its
    /// exit code. Returns <c>-1</c> — never throws — on timeout or any exception, logging
    /// the reason via <see cref="ILogConsoleService"/> first.
    /// </summary>
    Task<int> RunProcessAsync(string fileName, string arguments, TimeSpan? timeout = null);

    /// <summary>
    /// Runs <paramref name="fileName"/> with <paramref name="arguments"/> and returns its
    /// captured stdout (e.g. for parsing <c>bcdedit /enum {current}</c> output). The full
    /// captured output is still logged once via <see cref="ILogConsoleService"/> on
    /// completion, so it is never silently swallowed from the console — only the
    /// per-line-as-it-streams behavior of <see cref="RunProcessAsync"/> is skipped in
    /// favor of buffering. Returns an empty string — never throws — on timeout or any
    /// exception, logging the reason via <see cref="ILogConsoleService"/> first.
    /// </summary>
    Task<string> RunProcessCaptureOutputAsync(string fileName, string arguments, TimeSpan? timeout = null);

    /// <summary>
    /// Extracts an embedded resource whose name ends with <paramref name="resourceSuffix"/>
    /// to a GUID-suffixed temp file, runs it via
    /// <c>powershell.exe -NoProfile -ExecutionPolicy Bypass -File &lt;temp&gt; {arguments}</c>,
    /// and deletes the temp file in a <c>finally</c> block regardless of success/failure.
    /// Generalizes the extract-to-temp-then-run pattern first proven privately in
    /// <c>DefenderTweakHandler.ExtractEmbeddedAsync</c>, now promoted to a shared primitive
    /// per this class's own doc comment ("A later phase that needs to run an embedded script
    /// should add that capability then") — this is that phase. Throws
    /// <see cref="FileNotFoundException"/> if no embedded resource matches
    /// <paramref name="resourceSuffix"/>.
    /// </summary>
    Task<int> RunEmbeddedScriptAsync(string resourceSuffix, string? arguments = null, TimeSpan? timeout = null);
}
