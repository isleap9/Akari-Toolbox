using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using AkariToolbox.Services;
using AkariToolbox.ViewModels;

namespace AkariToolbox.Views;

public sealed partial class AdvancedPage : Page
{
    /// <summary>Localized string accessor used by x:Bind function bindings.</summary>
    public LocalizedStrings Strings { get; }

    /// <summary>
    /// Typed view model. Page-level x:Bind needs a strongly-typed root, so the view model is
    /// exposed as a property rather than only being pushed into an untyped DataContext.
    /// </summary>
    public AdvancedViewModel ViewModel { get; }

    public AdvancedPage(AdvancedViewModel viewModel)
    {
        Strings = App.Services.GetRequiredService<LocalizedStrings>();
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }
}