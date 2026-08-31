using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
using Windows.Storage;

namespace AkariToolbox.Framework.Services;

/// <summary>
/// File/folder picking for unpackaged, elevated WinUI 3 apps. Built on the
/// WinAppSDK-native, WindowId-based picker namespace (see the <c>using</c> above)
/// instead of the legacy hwnd/InitializeWithWindow-based picker API, which is
/// documented by Microsoft to crash under <c>requireAdministrator</c> elevation
/// (RESEARCH Pitfall 2, microsoft/WindowsAppSDK#2504).
/// </summary>
public interface IFilePickerService
{
    /// <summary>Opens a multi-select file picker. Returns null when cancelled.</summary>
    Task<IReadOnlyList<StorageFile>?> PickOpenFilesAsync(
        IReadOnlyList<string> fileTypeFilters,
        string? suggestedStartLocation = null);

    /// <summary>Opens a single-select file picker. Returns null when cancelled.</summary>
    Task<StorageFile?> PickOpenFileAsync(
        IReadOnlyList<string> fileTypeFilters,
        string? suggestedStartLocation = null);

    /// <summary>Opens a save-as picker. Returns null when cancelled.</summary>
    Task<StorageFile?> PickSaveFileAsync(
        string suggestedFileName,
        IReadOnlyList<string> fileTypeFilters,
        string? suggestedStartLocation = null);

    /// <summary>Opens a single-select folder picker. Returns null when cancelled.</summary>
    Task<StorageFolder?> PickSingleFolderAsync(string? suggestedStartLocation = null);

    /// <summary>Opens a multi-select folder picker. Returns null when cancelled.</summary>
    Task<IReadOnlyList<StorageFolder>?> PickMultipleFoldersAsync(string? suggestedStartLocation = null);
}

public sealed class FilePickerService : IFilePickerService
{
    private readonly Func<WindowId> _windowIdProvider;

    public FilePickerService(Func<WindowId> windowIdProvider)
    {
        _windowIdProvider = windowIdProvider ?? throw new ArgumentNullException(nameof(windowIdProvider));
    }

    public async Task<IReadOnlyList<StorageFile>?> PickOpenFilesAsync(
        IReadOnlyList<string> fileTypeFilters,
        string? suggestedStartLocation = null)
    {
        var picker = CreateFileOpenPicker(fileTypeFilters, suggestedStartLocation);
        var results = await picker.PickMultipleFilesAsync();
        if (results is null)
        {
            return null;
        }

        var files = new List<StorageFile>(results.Count);
        foreach (var result in results)
        {
            files.Add(await StorageFile.GetFileFromPathAsync(result.Path));
        }

        return files;
    }

    public async Task<StorageFile?> PickOpenFileAsync(
        IReadOnlyList<string> fileTypeFilters,
        string? suggestedStartLocation = null)
    {
        var picker = CreateFileOpenPicker(fileTypeFilters, suggestedStartLocation);
        var result = await picker.PickSingleFileAsync();
        return result is null ? null : await StorageFile.GetFileFromPathAsync(result.Path);
    }

    public async Task<StorageFile?> PickSaveFileAsync(
        string suggestedFileName,
        IReadOnlyList<string> fileTypeFilters,
        string? suggestedStartLocation = null)
    {
        var picker = new FileSavePicker(_windowIdProvider())
        {
            SuggestedFileName = suggestedFileName,
        };

        if (TryGetStartLocation(suggestedStartLocation, out var startLocation))
        {
            picker.SuggestedStartLocation = startLocation;
        }

        foreach (var filter in fileTypeFilters)
        {
            picker.FileTypeChoices.Add(Path.GetExtension(filter), [filter]);
        }

        var result = await picker.PickSaveFileAsync();
        return result is null ? null : await StorageFile.GetFileFromPathAsync(result.Path);
    }

    public async Task<StorageFolder?> PickSingleFolderAsync(string? suggestedStartLocation = null)
    {
        var picker = CreateFolderPicker(suggestedStartLocation);
        var result = await picker.PickSingleFolderAsync();
        return result is null ? null : await StorageFolder.GetFolderFromPathAsync(result.Path);
    }

    public async Task<IReadOnlyList<StorageFolder>?> PickMultipleFoldersAsync(string? suggestedStartLocation = null)
    {
        var picker = CreateFolderPicker(suggestedStartLocation);
        var results = await picker.PickMultipleFoldersAsync();
        if (results is null)
        {
            return null;
        }

        var folders = new List<StorageFolder>(results.Count);
        foreach (var result in results)
        {
            folders.Add(await StorageFolder.GetFolderFromPathAsync(result.Path));
        }

        return folders;
    }

    private FileOpenPicker CreateFileOpenPicker(IReadOnlyList<string> fileTypeFilters, string? suggestedStartLocation)
    {
        var picker = new FileOpenPicker(_windowIdProvider())
        {
            ViewMode = PickerViewMode.List,
        };

        if (TryGetStartLocation(suggestedStartLocation, out var startLocation))
        {
            picker.SuggestedStartLocation = startLocation;
        }

        foreach (var filter in fileTypeFilters)
        {
            picker.FileTypeFilter.Add(filter);
        }

        return picker;
    }

    private FolderPicker CreateFolderPicker(string? suggestedStartLocation)
    {
        var picker = new FolderPicker(_windowIdProvider());

        if (TryGetStartLocation(suggestedStartLocation, out var startLocation))
        {
            picker.SuggestedStartLocation = startLocation;
        }

        return picker;
    }

    private static bool TryGetStartLocation(string? suggestedStartLocation, out PickerLocationId location)
    {
        switch (suggestedStartLocation?.ToLowerInvariant())
        {
            case "documents":
                location = PickerLocationId.DocumentsLibrary;
                return true;
            case "pictures":
                location = PickerLocationId.PicturesLibrary;
                return true;
            case "downloads":
                location = PickerLocationId.Downloads;
                return true;
            default:
                location = PickerLocationId.ComputerFolder;
                return false;
        }
    }
}
