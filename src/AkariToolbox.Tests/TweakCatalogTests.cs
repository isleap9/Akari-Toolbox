using AkariToolbox.App.Services;
using Xunit;

namespace AkariToolbox.Tests;

/// <summary>A configurable fake <see cref="ITweakHandler"/> for exercising <see cref="TweakCatalog"/>.</summary>
public sealed class FakeTweakHandler(string key, bool initialState, int order = 0) : ITweakHandler
{
    private bool _state = initialState;

    public int GetStateCallCount { get; private set; }
    public int SetStateCallCount { get; private set; }
    public bool? LastSetStateValue { get; private set; }

    public string Key => key;
    public string Title => key;
    public string Description => key;
    public int Order => order;
    public TweakCategory Category => TweakCategory.AkariOS;

    public bool GetState()
    {
        GetStateCallCount++;
        return _state;
    }

    public void SetState(bool enabled)
    {
        SetStateCallCount++;
        LastSetStateValue = enabled;
        _state = enabled;
    }
}

public class TweakCatalogTests
{
    [Fact]
    public async Task SetStateAsync_same_value_is_noop()
    {
        var handler = new FakeTweakHandler("wifi", initialState: true);
        var catalog = new TweakCatalog([handler]);

        await catalog.SetStateAsync("wifi", true);

        Assert.Equal(0, handler.SetStateCallCount);
        Assert.True(handler.GetStateCallCount > 0);
    }

    [Fact]
    public async Task SetStateAsync_reads_state_before_writing()
    {
        var handler = new FakeTweakHandler("wifi", initialState: false);
        var catalog = new TweakCatalog([handler]);

        await catalog.SetStateAsync("wifi", true);

        Assert.Equal(1, handler.SetStateCallCount);
        Assert.True(handler.GetStateCallCount >= 1);
        Assert.True(handler.LastSetStateValue);
    }

    [Fact]
    public async Task SetStateAsync_concurrent_calls_for_same_key_do_not_overlap()
    {
        var handler = new SlowFakeTweakHandler("wifi", initialState: false);
        var catalog = new TweakCatalog([handler]);

        var first = catalog.SetStateAsync("wifi", true);
        var second = catalog.SetStateAsync("wifi", false);
        await Task.WhenAll(first, second);

        Assert.False(handler.ObservedOverlap);
    }

    [Fact]
    public async Task GetStateAsync_reads_live_state()
    {
        var handler = new FakeTweakHandler("wifi", initialState: true);
        var catalog = new TweakCatalog([handler]);

        var state = await catalog.GetStateAsync("wifi");

        Assert.True(state);
    }

    [Fact]
    public void Handlers_are_sorted_by_order_ascending()
    {
        var handlerB = new FakeTweakHandler("b", initialState: false, order: 2);
        var handlerA = new FakeTweakHandler("a", initialState: false, order: 0);
        var handlerC = new FakeTweakHandler("c", initialState: false, order: 1);

        var catalog = new TweakCatalog([handlerB, handlerA, handlerC]);

        Assert.Equal(["a", "c", "b"], catalog.Handlers.Select(h => h.Key));
    }

    [Fact]
    public void Handlers_order_values_are_unique_and_monotonic_with_insertion()
    {
        var handlers = new ITweakHandler[]
        {
            new FakeTweakHandler("wifi", initialState: false, order: 0),
        };

        var catalog = new TweakCatalog(handlers);

        var orders = catalog.Handlers.Select(h => h.Order).ToList();
        Assert.Equal(orders.Distinct().Count(), orders.Count);
        Assert.Equal(orders.OrderBy(o => o), orders);
    }

    [Fact]
    public void Empty_handler_set_does_not_throw()
    {
        var catalog = new TweakCatalog([]);

        Assert.Empty(catalog.Handlers);
    }

    /// <summary>A fake handler whose SetState artificially takes time, to detect overlapping calls.</summary>
    private sealed class SlowFakeTweakHandler(string key, bool initialState) : ITweakHandler
    {
        private bool _state = initialState;
        private int _inFlight;

        public bool ObservedOverlap { get; private set; }

        public string Key => key;
        public string Title => key;
        public string Description => key;
        public int Order => 0;
        public TweakCategory Category => TweakCategory.AkariOS;

        public bool GetState() => _state;

        public void SetState(bool enabled)
        {
            if (Interlocked.Increment(ref _inFlight) > 1)
            {
                ObservedOverlap = true;
            }

            Thread.Sleep(20);
            _state = enabled;

            Interlocked.Decrement(ref _inFlight);
        }
    }
}
