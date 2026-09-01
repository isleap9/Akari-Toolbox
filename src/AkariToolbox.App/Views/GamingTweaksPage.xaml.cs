using Microsoft.UI.Xaml.Controls;
using AkariToolbox.App.ViewModels;

namespace AkariToolbox.App.Views;

public sealed partial class GamingTweaksPage : Page
{
    /// <summary>View model used by x:Bind bindings.</summary>
    public GamingTweaksViewModel ViewModel { get; }

    public GamingTweaksPage(GamingTweaksViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }
}
