using System.Collections.ObjectModel;
using Microsoft.UI.Dispatching;
using AkariToolbox.App.Models;
using AkariToolbox.App.Services;
using AkariToolbox.Framework.Services;
using AkariToolbox.Framework.Threading;
using AkariToolbox.Framework.ViewModels;

namespace AkariToolbox.App.ViewModels;

/// <summary>
/// Drives the Akari OS Tweaks page. Builds one <see cref="TweakItem"/> per
/// <see cref="ITweakCatalog.Handlers"/> entry (already Order-sorted), reads each
/// item's live state in parallel without blocking construction (APP-05), and
/// writes through the catalog whenever the user flips a toggle.
/// </summary>
public partial class AkariOSTweaksViewModel : ViewModelBase
{
    private readonly ITweakCatalog _catalog;
    private readonly ILogConsoleService _log;
    private readonly DispatcherQueue _dispatcher;

    public AkariOSTweaksViewModel(ITweakCatalog catalog, ILogConsoleService log)
    {
        _catalog = catalog;
        _log = log;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        Title = "Akari OS Tweaks";

        foreach (var handler in _catalog.Handlers)
        {
            var item = new TweakItem
            {
                Key = handler.Key,
                Title = handler.Title,
                Description = handler.Description,
                IsOn = false,
            };

            item.PropertyChanged += OnTweakItemPropertyChanged;
            Tweaks.Add(item);

            // Read the live state in parallel, off the constructor's critical path,
            // then marshal the result back to the UI thread individually. TryGetStateAsync
            // never throws (T-01-16): a handler whose GetState() fails is caught, logged,
            // and defaults to false, so one throwing handler cannot prevent the other 31
            // from rendering correctly.
            _ = TryGetStateAsync(_catalog, _log, handler).ContinueWith(
                task => _dispatcher.RunOnUIThreadAsync(() => item.IsOn = task.Result),
                TaskScheduler.Default);
        }
    }

    public ObservableCollection<TweakItem> Tweaks { get; } = [];

    /// <summary>
    /// Reads <paramref name="handler"/>'s live state via <paramref name="catalog"/>, catching
    /// and logging any exception via <paramref name="log"/> instead of letting it escape
    /// unobserved (T-01-16). Defaults to <c>false</c> on failure rather than leaving the
    /// corresponding toggle in an indeterminate state. Internal and static (not private/instance)
    /// so it can be exercised directly by tests without needing a live
    /// <see cref="DispatcherQueue"/> — constructing this ViewModel requires
    /// <see cref="DispatcherQueue.GetForCurrentThread"/> to succeed, which throws outside a
    /// real WinRT-activated UI thread (e.g. a plain xunit test host).
    /// </summary>
    internal static async Task<bool> TryGetStateAsync(ITweakCatalog catalog, ILogConsoleService log, ITweakHandler handler)
    {
        try
        {
            return await catalog.GetStateAsync(handler.Key).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            log.Log($"[TWEAK] {handler.Key} GetState failed: {ex.Message}");
            return false;
        }
    }

    private void OnTweakItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TweakItem.IsOn) || sender is not TweakItem item)
        {
            return;
        }

        // Fire-and-forget: the catalog serializes per-key internally and the toggle
        // already reflects the user's intent. Failures are logged, never swallowed
        // silently.
        _ = _catalog.SetStateAsync(item.Key, item.IsOn).ContinueWith(
            task =>
            {
                if (task.IsFaulted)
                {
                    _log.Log($"[TWEAK ERROR] {item.Key}: {task.Exception?.GetBaseException().Message}");
                }
            },
            TaskScheduler.Default);
    }
}
