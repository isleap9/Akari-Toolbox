using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AkariToolbox.App.ViewModels;

namespace AkariToolbox.App.Views;

public sealed partial class HomePage : Page
{
    /// <summary>View model used by x:Bind bindings.</summary>
    public HomeViewModel ViewModel { get; }

    public HomePage(HomeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnCardClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: Type pageType } && ViewModel.OpenCommand.CanExecute(pageType))
        {
            ViewModel.OpenCommand.Execute(pageType);
        }
    }
}
