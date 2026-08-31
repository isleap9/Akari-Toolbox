using AkariToolbox.Framework.Services;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>
/// Covers the APP-05 must-have truths for <see cref="ILogConsoleService"/>: unconditional,
/// non-deduplicated appends. Constructed with a <c>null</c> dispatcher (as a plain xunit
/// thread has no <see cref="Microsoft.UI.Dispatching.DispatcherQueue"/>) so every
/// <see cref="ILogConsoleService.Log"/> call appends synchronously instead of marshaling —
/// this is what makes the append/no-dedup behavior testable headless. True cross-thread
/// dispatcher marshaling is verified by this plan's manual human-check instead (a real
/// DispatcherQueue only exists on a live UI thread).
/// </summary>
public class LogConsoleServiceTests
{
    [Fact]
    public void Log_appends_single_entry()
    {
        var service = new LogConsoleService(dispatcher: null);

        service.Log("hello");

        Assert.Single(service.Lines);
        Assert.Equal("hello", service.Lines[0]);
    }

    [Fact]
    public void Log_does_not_dedup_repeated_identical_messages()
    {
        var service = new LogConsoleService(dispatcher: null);

        service.Log("a");
        service.Log("a");

        Assert.Equal(2, service.Lines.Count);
        Assert.Equal("a", service.Lines[0]);
        Assert.Equal("a", service.Lines[1]);
    }

    [Fact]
    public async Task Log_from_background_thread_does_not_throw_and_appends()
    {
        var service = new LogConsoleService(dispatcher: null);

        await Task.Run(() => service.Log("from-background"));

        Assert.Contains("from-background", service.Lines);
    }
}
