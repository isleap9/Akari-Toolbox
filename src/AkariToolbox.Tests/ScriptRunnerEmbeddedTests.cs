using AkariToolbox.Framework.Services;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Covers <see cref="IScriptRunner.RunEmbeddedScriptAsync"/> — the generalized
/// embedded-script primitive Plan 02-06 needs (promoted from
/// <c>DefenderTweakHandler.ExtractEmbeddedAsync</c>). Exercises the real production
/// assembly-resolution contract (<c>typeof(ScriptRunner).Assembly</c>) against the
/// <c>exit7.ps1</c> fixture embedded directly in <c>AkariToolbox.Framework</c> — not a
/// mocked resource lookup.
/// </summary>
public class ScriptRunnerEmbeddedTests
{
    private static ScriptRunner CreateRunner() => new(new LogConsoleService(dispatcher: null));

    [Fact]
    public async Task RunEmbeddedScriptAsync_runs_fixture_and_returns_real_exit_code()
    {
        var runner = CreateRunner();

        var exitCode = await runner.RunEmbeddedScriptAsync("exit7.ps1");

        Assert.Equal(7, exitCode);
    }

    [Fact]
    public async Task RunEmbeddedScriptAsync_deletes_temp_file_after_completion()
    {
        var runner = CreateRunner();
        var beforeCount = CountAkariToolboxTempFiles();

        await runner.RunEmbeddedScriptAsync("exit7.ps1");

        var afterCount = CountAkariToolboxTempFiles();
        Assert.Equal(beforeCount, afterCount);
    }

    [Fact]
    public async Task RunEmbeddedScriptAsync_missing_resource_throws_FileNotFoundException()
    {
        var runner = CreateRunner();
        const string missingSuffix = "does-not-exist-anywhere.ps1";

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(
            () => runner.RunEmbeddedScriptAsync(missingSuffix));

        Assert.Contains(missingSuffix, ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Counts leftover "AkariToolbox-*" temp files as a proxy for "no extracted script
    /// left behind" — directly asserting on a specific path would race other tests/process
    /// runs; comparing before/after counts under the same fixed prefix is deterministic
    /// enough here since RunEmbeddedScriptAsync's finally-block cleanup is synchronous with
    /// the awaited call returning.
    /// </summary>
    private static int CountAkariToolboxTempFiles() =>
        Directory.GetFiles(Path.GetTempPath(), "AkariToolbox-*").Length;
}
