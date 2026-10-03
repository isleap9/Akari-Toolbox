using AkariToolbox.Tweaks;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AkariToolbox.ViewModels;

/// <summary>
/// 1  Check page: native ports of Ultimate "1 Check" (BIOS, OCCT, drive / RAM /
/// GPU checklists, CPU and GPU benches). Long work runs off-thread with the
/// page locked; guidance shows in a dialog like Akari-Tool.
/// </summary>
public partial class CheckViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private readonly IDialogService _dialogs;

    [ObservableProperty]
    private bool _isBusy;

    public CheckViewModel(IInfoBarService infoBar, IStatusService status, IDialogService dialogs)
    {
        _infoBar = infoBar;
        _status = status;
        _dialogs = dialogs;
    }

    public void OnNavigatedTo(object? parameter)
    {
    }

    public void OnNavigatedFrom()
    {
    }

    [RelayCommand]
    private void RunBiosCheck()
    {
        try
        {
            if (CheckActions.RunBiosCheck())
                NativeOps.RestartToBios();
        }
        catch (Exception ex)
        {
            _infoBar.Show("BIOS Check  failed", ex.Message);
        }
    }

    [RelayCommand]
    private Task RunPcCheck() => RunAsync("PC Check (OCCT)", CheckActions.InstallAndRunOcct, CheckActions.PcGuidance);

    [RelayCommand]
    private async Task RunStorageCheck()
    {
        try
        {
            await _dialogs.ShowInfoAsync(
                "Storage Check",
                "STORAGE CHECK\n- Keep SSD's at least 10% free\n- Stick with internal SSD's or NVME's\n" +
                "- Avoid installing windows & games on HDD's & external drives\n\n" +
                CheckActions.DriveSpaceSummary());
            CheckActions.OpenMyComputer();
        }
        catch (Exception ex)
        {
            _infoBar.Show("Storage Check  failed", ex.Message);
        }
    }

    [RelayCommand]
    private Task RunRamCheck() => RunAsync("RAM Check", CheckActions.DownloadAndRunCpuZ, CheckActions.RamGuidance);

    [RelayCommand]
    private Task RunGpuCheck() => RunAsync("GPU Check", CheckActions.DownloadAndRunGpuZ, CheckActions.GpuGuidance);

    [RelayCommand]
    private Task RunCpuBench() => RunAsync("CPU Bench", CheckActions.DownloadAndRunOcctBench, CheckActions.BenchGuidance);

    [RelayCommand]
    private Task RunGpuBench() => RunAsync("GPU Bench", CheckActions.DownloadAndRunFurMark, CheckActions.BenchGuidance);

    private async Task RunAsync(string title, Action work, string? guidance)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        _status.Start($"{title}...");
        try
        {
            await Task.Run(work);
            if (!string.IsNullOrEmpty(guidance))
                await _dialogs.ShowInfoAsync(title, guidance);
            _infoBar.Show($"{title}  done", "See the guidance window for next steps.");
        }
        catch (Exception ex)
        {
            _infoBar.Show($"{title}  failed", ex.Message);
        }
        finally
        {
            IsBusy = false;
            _status.Complete();
        }
    }
}
