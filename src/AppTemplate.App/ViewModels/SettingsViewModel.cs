using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;

namespace AkariToolbox.ViewModels;

public partial class SettingsViewModel : ViewModelBase, INavigationAware
{
    private readonly IThemeService _theme;
    private bool _isInitialized;

    [ObservableProperty]
    private string _appVersion = string.Empty;

    [ObservableProperty]
    private string _currentTheme = string.Empty;

    [ObservableProperty]
    private bool _isLight;

    [ObservableProperty]
    private bool _isDark;

    public SettingsViewModel(IThemeService theme)
    {
        _theme = theme;
        Title = "Settings";
    }

    public void OnNavigatedTo(object? parameter)
    {
        if (!_isInitialized)
        {
            InitializeViewModel();
        }

        // Refresh in case the theme changed while away.
        RefreshThemeState();
    }

    public void OnNavigatedFrom()
    {
    }

    private void InitializeViewModel()
    {
        AppVersion = $"Akari Toolbox  v{GetAssemblyVersion()}";
        RefreshThemeState();
        _isInitialized = true;
    }

    private void RefreshThemeState()
    {
        CurrentTheme = _theme.CurrentTheme.ToString();
        IsLight = _theme.CurrentTheme == AppTheme.Light;
        IsDark = _theme.CurrentTheme == AppTheme.Dark;
    }

    private static string GetAssemblyVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    [RelayCommand]
    private void ChangeThemeLight()
    {
        if (_theme.CurrentTheme == AppTheme.Light)
        {
            return;
        }

        _ = _theme.SetThemeAsync(AppTheme.Light);
        RefreshThemeState();
    }

    [RelayCommand]
    private void ChangeThemeDark()
    {
        if (_theme.CurrentTheme == AppTheme.Dark)
        {
            return;
        }

        _ = _theme.SetThemeAsync(AppTheme.Dark);
        RefreshThemeState();
    }
}
