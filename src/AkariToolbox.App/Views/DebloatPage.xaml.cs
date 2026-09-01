using Microsoft.UI.Xaml.Controls;
using AkariToolbox.App.ViewModels;

namespace AkariToolbox.App.Views;

public sealed partial class DebloatPage : Page
{
    /// <summary>View model used by x:Bind bindings.</summary>
    public DebloatViewModel ViewModel { get; }

    public DebloatPage(DebloatViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }
}
