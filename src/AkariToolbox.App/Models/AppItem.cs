using CommunityToolkit.Mvvm.ComponentModel;

namespace AkariToolbox.App.Models;

/// <summary>
/// Display/bindable row for a single catalog app. Mirrors <see cref="DebloatActionItem"/>'s
/// shape — init-only display fields plus observable mutable state (<see cref="IsSelected"/>,
/// <see cref="IsInstalling"/>) driven by <see cref="ViewModels.DownloadsViewModel"/>.
/// </summary>
public sealed partial class AppItem : ObservableObject
{
    /// <summary>Display name — also the key <see cref="ViewModels.DownloadsViewModel"/> uses to resolve the underlying <see cref="AppDefinition"/>.</summary>
    public string Name { get; init; } = "";

    public string Description { get; init; } = "";

    public string Category { get; init; } = "";

    /// <summary>Winget package id, shown for display/debugging purposes only.</summary>
    public string WingetId { get; init; } = "";

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isInstalling;
}
