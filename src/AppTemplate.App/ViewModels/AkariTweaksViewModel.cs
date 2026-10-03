using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;

namespace AkariToolbox.ViewModels;

/// <summary>
/// The Akari OS Tweaks page: 32 live-state toggles. On navigation every switch reads the
/// real system state (registry / services / bcdedit); flipping one applies instantly on a
/// background thread, write-throughs to HKCU\Software\AkariTool, and reports via infobar.
/// </summary>
public partial class AkariTweaksViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private bool _loading;

    public ObservableCollection<TweakToggleItem> Toggles { get; } = [];

    public AkariTweaksViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
        Title = "Akari Tweaks";
    }

    public void OnNavigatedTo(object? parameter)
    {
        RefreshFromSystem();
    }

    public void OnNavigatedFrom()
    {
    }

    /// <summary>Re-read live state into the switches (called on navigation).</summary>
    private void RefreshFromSystem()
    {
        _loading = true;
        try
        {
            if (Toggles.Count == 0)
            {
                foreach (TweakToggle toggle in AkariTweakCatalog.All)
                {
                    var item = new TweakToggleItem(toggle);
                    item.PropertyChanged += OnTogglePropertyChanged;
                    Toggles.Add(item);
                }
            }

            foreach (TweakToggleItem item in Toggles)
                item.IsOn = ReadSafe(item.Toggle);
        }
        finally
        {
            _loading = false;
        }
    }

    private static bool ReadSafe(TweakToggle toggle)
    {
        try
        {
            return toggle.Read();
        }
        catch
        {
            return false;
        }
    }

    private void OnTogglePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_loading || e.PropertyName != nameof(TweakToggleItem.IsOn) || sender is not TweakToggleItem item)
            return;

        _ = ApplyAsync(item);
    }

    private async Task ApplyAsync(TweakToggleItem item)
    {
        bool target = item.IsOn;
        item.IsBusy = true;
        _status.Start($"{item.Toggle.Title}...");
        try
        {
            await Task.Run(() => item.Toggle.Apply(target));
            _infoBar.Show(
                $"{item.Toggle.Title}  applied",
                target ? "The tweak is now ON." : "The tweak is now OFF.");
        }
        catch (Exception ex)
        {
            // Roll the switch back so it never lies about the real state.
            _loading = true;
            item.IsOn = ReadSafe(item.Toggle);
            _loading = false;

            _infoBar.Show($"{item.Toggle.Title}  failed", ex.Message);
        }
        finally
        {
            item.IsBusy = false;
            _status.Complete();
        }
    }
}

/// <summary>One switch row bound by the page (wraps a <see cref="TweakToggle"/>).</summary>
public partial class TweakToggleItem(TweakToggle toggle) : ObservableObject
{
    public TweakToggle Toggle { get; } = toggle;

    public string Key => Toggle.Key;
    public string Title => Toggle.Title;
    public string Description => Toggle.Description;

    [ObservableProperty]
    private bool _isOn;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>False while this tweak is being applied, so the switch locks itself.</summary>
    public bool IsEnabled => !IsBusy;

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));
}
