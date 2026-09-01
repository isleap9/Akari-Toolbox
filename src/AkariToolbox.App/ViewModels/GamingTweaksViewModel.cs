using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using AkariToolbox.App.Models;
using AkariToolbox.App.Services;
using AkariToolbox.Framework.Services;
using AkariToolbox.Framework.Threading;
using AkariToolbox.Framework.ViewModels;

namespace AkariToolbox.App.ViewModels;

/// <summary>
/// Drives the Gaming Tweaks page. Mirrors <see cref="AkariOSTweaksViewModel"/> exactly —
/// same constructor shape, same <c>TryGetStateAsync</c>/write-through/error-correction
/// pattern — filtering <see cref="ITweakCatalog.Handlers"/> to
/// <see cref="TweakCategory.Gaming"/> instead of <see cref="TweakCategory.AkariOS"/>. This
/// ViewModel needs no further changes as later plans add more Gaming handlers — it is
/// entirely catalog-driven.
/// </summary>
public partial class GamingTweaksViewModel : ViewModelBase
{
    private readonly ITweakCatalog _catalog;
    private readonly ILogConsoleService _log;
    private readonly DispatcherQueue _dispatcher;

    public GamingTweaksViewModel(ITweakCatalog catalog, ILogConsoleService log)
    {
        _catalog = catalog;
        _log = log;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        Title = "Gaming Tweaks";

        foreach (var handler in _catalog.Handlers.Where(h => h.Category == TweakCategory.Gaming))
        {
            var item = new TweakItem
            {
                Key = handler.Key,
                Title = handler.Title,
                Description = handler.Description,
                IsOn = false,
            };

            Tweaks.Add(item);

            // Read the live state in parallel, off the constructor's critical path, then
            // marshal the result back to the UI thread individually — same pattern as
            // AkariOSTweaksViewModel (CR-02 fix: subscribe only after the initial value is
            // set, never before).
            _ = AkariOSTweaksViewModel.TryGetStateAsync(_catalog, _log, handler).ContinueWith(
                task => _dispatcher.RunOnUIThreadAsync(() =>
                {
                    item.IsOn = task.Result;
                    item.PropertyChanged += OnTweakItemPropertyChanged;
                }),
                TaskScheduler.Default);
        }
    }

    public ObservableCollection<TweakItem> Tweaks { get; } = [];

    // D-05 one-shot shortcuts (12 Resolution Refresh Rate.ps1 / 13 Hags Windowed.ps1) —
    // plain ms-settings: URI launches, not ITweakHandlers: no menu, no elevation check,
    // no state. The shell-execute-through-explorer launch style below is borrowed from
    // DefenderTweakHandler.DefenderRunElevatedPsFileAsync, the only other call site in
    // this codebase using that same ProcessStartInfo shape.
    [RelayCommand]
    private void OpenDisplaySettings() =>
        Process.Start(new ProcessStartInfo("ms-settings:display") { UseShellExecute = true });

    [RelayCommand]
    private void OpenAdvancedGraphicsSettings() =>
        Process.Start(new ProcessStartInfo("ms-settings:display-advancedgraphics") { UseShellExecute = true });

    private void OnTweakItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(TweakItem.IsOn) || sender is not TweakItem item)
        {
            return;
        }

        // Fire-and-forget: the catalog serializes per-key internally and the toggle already
        // reflects the user's intent. Failures are logged, never swallowed silently.
        //
        // Same CR-04 fix as AkariOSTweaksViewModel: on fault, re-read the real live state
        // and reflect it in the UI instead of leaving the toggle showing the requested-but-
        // never-applied state. Unsubscribe/resubscribe around the correction so setting
        // item.IsOn back to the real value doesn't re-trigger another write-through.
        _ = _catalog.SetStateAsync(item.Key, item.IsOn).ContinueWith(
            async task =>
            {
                if (task.IsFaulted)
                {
                    _log.Log($"[TWEAK ERROR] {item.Key}: {task.Exception?.GetBaseException().Message}");

                    var handler = _catalog.Handlers.FirstOrDefault(h => h.Key == item.Key);
                    if (handler is null)
                    {
                        return;
                    }

                    var real = await AkariOSTweaksViewModel.TryGetStateAsync(_catalog, _log, handler).ConfigureAwait(false);
                    await _dispatcher.RunOnUIThreadAsync(() =>
                    {
                        item.PropertyChanged -= OnTweakItemPropertyChanged;
                        item.IsOn = real;
                        item.PropertyChanged += OnTweakItemPropertyChanged;
                    });
                }
            },
            TaskScheduler.Default);
    }
}
