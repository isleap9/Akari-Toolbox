using Microsoft.UI.Xaml.Controls;
using AkariToolbox.App.ViewModels;

namespace AkariToolbox.App.Views;

public sealed partial class DownloadsPage : Page
{
    /// <summary>View model used by x:Bind bindings.</summary>
    public DownloadsViewModel ViewModel { get; }

    public DownloadsPage(DownloadsViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }
}
