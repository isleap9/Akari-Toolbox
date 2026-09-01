using CommunityToolkit.Mvvm.ComponentModel;

namespace AkariToolbox.App.Models;

/// <summary>
/// Display/bindable row for a single Debloat action. Mirrors <see cref="TweakItem"/>'s
/// shape, but the mutable observable state is <see cref="IsRunning"/> — a busy indicator —
/// not a toggle-state <c>IsOn</c>, since Debloat actions have no on/off state (D-01).
/// </summary>
public sealed partial class DebloatActionItem : ObservableObject
{
    /// <summary>Stable key the ViewModel uses to resolve the underlying <see cref="DebloatAction"/>.</summary>
    public string Key { get; init; } = "";

    public string Title { get; init; } = "";

    public string Description { get; init; } = "";

    /// <summary>
    /// The ONLY gate on whether the Undo button renders (D-02) — true when the underlying
    /// action has a non-null <c>UndoResourceSuffix</c>. There is no session-tracking flag;
    /// Undo is always available regardless of whether Run was ever clicked this session.
    /// </summary>
    public bool HasUndo { get; init; }

    public bool RequiresConfirmation { get; init; }

    /// <summary>
    /// D-10 accepted-risk flag mirrored from <see cref="DebloatAction.UndoDownloadsUnverifiedBinary"/> —
    /// true when this row's Undo branch downloads an unverified installer binary, driving
    /// the page's per-row risk caption (RESEARCH.md Pitfall 5).
    /// </summary>
    public bool UndoDownloadsUnverifiedBinary { get; init; }

    [ObservableProperty]
    private bool _isRunning;
}

/// <summary>One category header's worth of <see cref="DebloatActionItem"/>s, in declared catalog order.</summary>
public sealed class DebloatCategoryGroup
{
    public required string Name { get; init; }

    public required IReadOnlyList<DebloatActionItem> Actions { get; init; }
}
