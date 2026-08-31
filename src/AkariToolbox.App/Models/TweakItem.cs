using CommunityToolkit.Mvvm.ComponentModel;

namespace AkariToolbox.App.Models;

/// <summary>A boolean system tweak rendered as a labelled ToggleSwitch.</summary>
public partial class TweakItem : ObservableObject
{
    /// <summary>Stable key the tweak catalog uses to read/write the underlying state.</summary>
    public string Key { get; init; } = "";
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";

    [ObservableProperty]
    private bool _isOn;
}
