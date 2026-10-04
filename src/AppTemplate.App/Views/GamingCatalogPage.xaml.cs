using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using AkariToolbox.Services;
using AkariToolbox.ViewModels;

namespace AkariToolbox.Views;

public sealed partial class GamingCatalogPage : Page
{
    /// <summary>Localized string accessor used by x:Bind function bindings.</summary>
    public LocalizedStrings Strings { get; }

    /// <summary>
    /// Typed view model. Page-level x:Bind needs a strongly-typed root, so the view model is
    /// exposed as a property rather than only being pushed into an untyped DataContext.
    /// </summary>
    public GamingCatalogViewModel ViewModel { get; }

    public GamingCatalogPage(GamingCatalogViewModel viewModel)
    {
        Strings = App.Services.GetRequiredService<LocalizedStrings>();
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }

    /// <summary>Keystroke path: suggestions from the first character, list filters live.</summary>
    private void OnSearchTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            ViewModel.UpdateSearch(sender.Text);
    }

    /// <summary>Tapping a suggestion filters the list; it never ticks a row.</summary>
    private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is string suggestion)
        {
            sender.Text = suggestion;
            ViewModel.ChooseSuggestion(suggestion);
        }
    }

    /// <summary>Enter applies whatever text is present as the filter.</summary>
    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.UpdateSearch(args.QueryText);
    }
}
