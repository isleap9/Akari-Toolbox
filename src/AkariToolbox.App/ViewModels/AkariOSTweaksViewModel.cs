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
            // then marshal the result back to the UI thread individually.
            // (ITweakCatalog.GetStateAsync already dispatches its own live read via
            // Task.Run internally, so no extra Task.Run wrapper is needed here.)
            _ = _catalog.GetStateAsync(handler.Key).ContinueWith(
                task =>
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        _ = _dispatcher.RunOnUIThreadAsync(() => item.IsOn = task.Result);
                    }
                    else
                    {
                        _log.Log($"[TWEAK ERROR] {handler.Key}: {task.Exception?.GetBaseException().Message}");
                    }
                },
                TaskScheduler.Default);
        }
    }

    public ObservableCollection<TweakItem> Tweaks { get; } = [];

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
