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
}
