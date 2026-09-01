using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.Input;
using AkariToolbox.App.Models;
using AkariToolbox.App.Services;
using AkariToolbox.Framework.Services;
using AkariToolbox.Framework.ViewModels;

namespace AkariToolbox.App.ViewModels;

/// <summary>
/// Drives the Debloat page. Deliberately NOT <see cref="ITweakCatalog"/>-shaped (D-01) —
/// Debloat has no live-state read-back, so this ViewModel is a generic, catalog-driven
/// two-method Run/Undo dispatch over <see cref="DebloatActionItem"/> rather than one
/// <c>[RelayCommand]</c> per action (28 rows makes per-action methods unwieldy, per
/// RESEARCH.md's architecture diagram recommendation). Needs no further changes as later
/// plans embed more scripts — it is entirely catalog-driven.
///
/// Unlike <see cref="GamingTweaksViewModel"/>, this ViewModel never resolves a
/// <c>DispatcherQueue</c> or calls <c>ContinueWith(..., TaskScheduler.Default)</c> — every
/// <c>await</c> inside <see cref="ExecuteAsync"/> resumes on the UI thread's captured
/// <c>SynchronizationContext</c> implicitly, so <c>item.IsRunning = ...</c> stays
/// UI-thread-safe. Do NOT add <c>.ConfigureAwait(false)</c> anywhere in this class.
/// </summary>
public partial class DebloatViewModel : ViewModelBase
{
    private readonly ILogConsoleService _log;
    private readonly IScriptRunner _scriptRunner;
    private readonly IDialogService _dialogService;
    private readonly Dictionary<string, DebloatAction> _actionsByKey;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    public DebloatViewModel(IDebloatCatalog catalog, ILogConsoleService log, IScriptRunner scriptRunner, IDialogService dialogService)
    {
        _log = log;
        _scriptRunner = scriptRunner;
        _dialogService = dialogService;
        Title = "Debloat";

        _actionsByKey = catalog.Actions.ToDictionary(a => a.Key);

        CategoryGroups = catalog.Actions
            .GroupBy(a => a.Category)
            .Select(g => new DebloatCategoryGroup
            {
                Name = g.Key,
                Actions = g.Select(a => new DebloatActionItem
                {
                    Key = a.Key,
                    Title = a.Title,
                    Description = a.Description,
                    HasUndo = a.UndoResourceSuffix is not null,
                    RequiresConfirmation = a.RequiresConfirmation,
                    UndoDownloadsUnverifiedBinary = a.UndoDownloadsUnverifiedBinary,
                }).ToList(),
            })
            .ToList();
    }

    /// <summary>5 category groups (Privacy &amp; Telemetry, System &amp; Performance, Cleanup, Explorer &amp; UI, Tools), in catalog-declared order.</summary>
    public IReadOnlyList<DebloatCategoryGroup> CategoryGroups { get; }

    [RelayCommand]
    private Task RunActionAsync(DebloatActionItem item) => ExecuteAsync(item, isUndo: false);

    [RelayCommand]
    private Task UndoActionAsync(DebloatActionItem item) => ExecuteAsync(item, isUndo: true);

    private async Task ExecuteAsync(DebloatActionItem item, bool isUndo)
    {
        var action = _actionsByKey[item.Key];
        var resourceSuffix = isUndo ? action.UndoResourceSuffix : action.RunResourceSuffix;
        if (resourceSuffix is null)
        {
            // Defensive only — the Undo button is hidden via HasUndo when this is true,
            // so this guards a programming error, not a reachable user path.
            return;
        }

        if (action.RequiresConfirmation && !isUndo)
        {
            // Confirmation gates ONLY the forward/Run direction (D-11) — undoing
            // BitLocker/Hibernation/OneDrive/Bloatware/Edge&WebView restores/re-enables
            // rather than removes, so it does not need the same friction.
            var confirmed = await _dialogService.ConfirmAsync(
                action.Title,
                $"This action makes system changes that may be difficult to reverse. Continue with \"{action.Title}\"?");
            if (!confirmed)
            {
                return;
            }
        }

        var gate = _locks.GetOrAdd(action.Key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            item.IsRunning = true;

            if (isUndo && action.UndoDownloadsUnverifiedBinary)
            {
                _log.Log($"[DEBLOAT] Launching {action.Title} (Undo) — downloaded binary is NOT SHA256/signature-verified before execution (accepted risk, D-10).");
            }

            _log.Log($"[DEBLOAT] Running: {action.Title}{(isUndo ? " (Undo)" : "")}");
            await _scriptRunner.RunEmbeddedScriptAsync(resourceSuffix);
        }
        catch (FileNotFoundException ex)
        {
            // Mirrors GamingTweaksViewModel's WR-04 fix exactly — a missing/mismatched
            // resource suffix must surface visibly in the log dock, not only reach an
            // unobserved-task-exception handler.
            _log.Log($"[DEBLOAT] ERROR: {action.Title}{(isUndo ? " (Undo)" : "")} failed to launch — {ex.Message}");
        }
        finally
        {
            item.IsRunning = false;
            gate.Release();
        }
    }
}
