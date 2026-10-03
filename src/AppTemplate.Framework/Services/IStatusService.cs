using CommunityToolkit.Mvvm.ComponentModel;

namespace AppTemplate.Framework.Services;

/// <summary>
/// Observable state object that drives a global progress footer in the app shell.
/// Ref-counted so overlapping runs (page-level and tile-level) keep the footer
/// visible until every run completes. Mirrors <see cref="IInfoBarService"/>.
/// </summary>
public interface IStatusService
{
    bool IsActive { get; }
    string Message { get; }
    bool IsIndeterminate { get; }
    double Value { get; }

    void Start(string message);
    void Report(double value, string? message = null);
    void Complete();
}

public partial class StatusService : ObservableObject, IStatusService
{
    private int _count;

    [ObservableProperty]
    public partial bool IsActive { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsIndeterminate { get; set; } = true;

    [ObservableProperty]
    public partial double Value { get; set; }

    public void Start(string message)
    {
        if (Interlocked.Increment(ref _count) == 1)
        {
            Message = message;
            IsIndeterminate = true;
            Value = 0;
            IsActive = true;
        }
        else
        {
            Message = message;
        }
    }

    public void Report(double value, string? message = null)
    {
        IsIndeterminate = false;
        Value = value;
        if (message is not null)
            Message = message;
    }

    public void Complete()
    {
        if (Interlocked.Decrement(ref _count) <= 0)
        {
            _count = 0;
            IsActive = false;
        }
    }
}
