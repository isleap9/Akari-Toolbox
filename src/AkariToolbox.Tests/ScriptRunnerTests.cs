using AkariToolbox.Framework.Services;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Covers this plan's must-have truth and acceptance criteria for
/// <see cref="IScriptRunner"/>: stdout/stderr is streamed to <see cref="ILogConsoleService"/>
/// (never silently swallowed), and timeouts/exceptions return <c>-1</c> without throwing.
/// </summary>
public class ScriptRunnerTests
{
    [Fact]
    public async Task RunProcessAsync_captures_stdout_and_forwards_to_log()
    {
        var log = new LogConsoleService(dispatcher: null);
        var runner = new ScriptRunner(log);

        var exitCode = await runner.RunProcessAsync("cmd.exe", "/c echo hello");

        Assert.Equal(0, exitCode);
        Assert.Contains("hello", log.Lines);
    }

    [Fact]
    public async Task RunProcessAsync_returns_minus_one_on_timeout_without_throwing()
    {
        var log = new LogConsoleService(dispatcher: null);
        var runner = new ScriptRunner(log);

        var exitCode = await runner.RunProcessAsync(
            "cmd.exe", "/c ping -n 30 127.0.0.1 >nul", TimeSpan.FromMilliseconds(1));

        Assert.Equal(-1, exitCode);
    }
}
