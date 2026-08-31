using CommunityToolkit.Mvvm.Input;
using AkariToolbox.App.Views;
using AkariToolbox.Framework.Navigation;
using AkariToolbox.Framework.ViewModels;

namespace AkariToolbox.App.ViewModels;

/// <summary>A single destination card on the Home dashboard.</summary>
public sealed class HomeCard
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string Glyph { get; init; } = "";      // Segoe Fluent Icons glyph
    public Type Target { get; init; } = typeof(HomePage);
    public bool IsEnabled { get; init; } = true;
}

public partial class HomeViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;

    public HomeViewModel(INavigationService navigation)
    {
        _navigation = navigation;
        Title = "Home";
    }

    /// <summary>
    /// All 5 destination cards from day one (D-09) — only Akari OS Tweaks is built
    /// in Phase 1; the other 4 render as visibly disabled "Coming soon" until their
    /// own phase ships.
    /// </summary>
    public IReadOnlyList<HomeCard> Cards { get; } =
    [
        new HomeCard { Title = "Gaming Tweaks",   Description = "GPU, latency & service tuning for peak FPS",  Glyph = "", Target = typeof(HomePage), IsEnabled = false },
        new HomeCard { Title = "Akari OS Tweaks", Description = "Toggle deep system modifications & services", Glyph = "", Target = typeof(AkariOSTweaksPage), IsEnabled = true },
        new HomeCard { Title = "Debloat",         Description = "Run 28 PowerShell-backed debloat actions",    Glyph = "", Target = typeof(HomePage), IsEnabled = false },
        new HomeCard { Title = "Downloads",       Description = "Playbooks, drivers & recommended utilities",  Glyph = "", Target = typeof(HomePage), IsEnabled = false },
        new HomeCard { Title = "Misc",            Description = "Context-menu entries & extra tools",         Glyph = "", Target = typeof(HomePage), IsEnabled = false },
    ];

    [RelayCommand]
    private void Open(Type pageType) => _navigation.NavigateTo(pageType);
}
