using Microsoft.UI.Xaml.Controls;
using AkariToolbox.App.ViewModels;

namespace AkariToolbox.App.Views;

public sealed partial class AkariOSTweaksPage : Page
{
    /// <summary>View model used by x:Bind bindings.</summary>
    public AkariOSTweaksViewModel ViewModel { get; }

    public AkariOSTweaksPage(AkariOSTweaksViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }
}
