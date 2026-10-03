using System.Collections.ObjectModel;
using AkariToolbox.Tweaks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;

namespace AkariToolbox.ViewModels;

/// <summary>
/// 4  Installers page: the Ultimate "4 Installers" grid (apps + GPU tools).
/// Each button downloads its installer from FR33THY's release and runs it with
/// the script's silent arguments, off-thread with the page locked.
/// </summary>
public partial class InstallersViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<InstallerButton> LauncherButtons { get; } = [];

    public ObservableCollection<InstallerButton> ToolButtons { get; } = [];

    public InstallersViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
        Title = "Installers";

        foreach (var item in InstallerActions.Launchers)
            LauncherButtons.Add(CreateTile(item));
        foreach (var item in InstallerActions.GpuTools)
            ToolButtons.Add(CreateTile(item));
    }

    /// <summary>Creates a tile that knows how to lock itself while its installer runs.</summary>
    private InstallerButton CreateTile(InstallerActions.InstallerItem item)
    {
        var button = new InstallerButton(item.Label);
        button.RunAsync = () => InstallOn(button, item);
        return button;
    }

    [RelayCommand]
    private Task RunButton(InstallerButton? button) =>
        button is null ? Task.CompletedTask : button.RunAsync();

    public void OnNavigatedTo(object? parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }

    private async Task InstallOn(InstallerButton button, InstallerActions.InstallerItem item)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{item.Label}...");
        button.IsBusy = true;
        try
        {
            await Task.Run(item.Install);
            _infoBar.Show($"{item.Label}  installed", "Follow the installer's own steps if it is still open.");
        }
        catch (Exception ex)
        {
            _infoBar.Show($"{item.Label}  failed", ex.Message);
        }
        finally
        {
            button.IsBusy = false;
            IsBusy = false;
            _status.Complete();
        }
    }
}

/// <summary>One installer tile: downloads + runs its app off-thread.</summary>
public sealed partial class InstallerButton : ObservableObject
{
    public InstallerButton(string label) => Label = label;

    public string Label { get; }

    /// <summary>Set by the view model to this tile's install action.</summary>
    internal Func<Task> RunAsync { get; set; } = () => Task.CompletedTask;

    /// <summary>True while an installer is downloading and running.</summary>
    [ObservableProperty]
    private bool _isBusy;

    public bool IsEnabled => !IsBusy;

    /// <summary>
    /// Runs this installer. WPF reached the page's RunButtonCommand through
    /// RelativeSource AncestorType=Page; WinUI DataTemplates have no equivalent, so the
    /// command lives on the tile and delegates to the same InstallOn path.
    /// </summary>
    [RelayCommand]
    private Task RunInstaller() => RunAsync();

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));
}
