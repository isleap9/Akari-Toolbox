# Phase 01: Trusted Catalog Foundation - Pattern Map

**Mapped:** 2026-10-04
**Files analyzed:** 7 (5 new + 2 modified)
**Analogs found:** 7 / 7

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|-------------------|------|-----------|----------------|---------------|
| `src/AppTemplate.App/Tweaks/GamingTweakDescriptor.cs` (NEW) | model | transform | `src/AppTemplate.App/Tweaks/TweakToggle.cs` | role-match |
| `src/AppTemplate.App/Tweaks/GamingCatalog.cs` (NEW) | config | CRUD | `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs` | exact |
| `src/AppTemplate.App/Tweaks/RowRevertJournal.cs` (NEW, if journal chosen) | service | CRUD | `src/AppTemplate.App/Tweaks/AkariToolState.cs` + `src/AppTemplate.App/Tweaks/ControlPanelRows.cs` | role-match |
| `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs` (NEW, incl. `CatalogRowItem` + `CatalogSectionGroup`) | viewmodel | request-response | `src/AppTemplate.App/ViewModels/TweaksViewModel.cs` (`CpRowItem`, `CpSectionGroup`, `SetRowsBusy`) | exact |
| `src/AppTemplate.App/Views/GamingCatalogPage.xaml` + `.xaml.cs` (NEW) | component (view) | request-response | `src/AppTemplate.App/Views/TweaksPage.xaml` + `TweaksPage.xaml.cs` | exact |
| `src/AppTemplate.App/App.xaml.cs` (MODIFY: DI registration) | config | request-response | `src/AppTemplate.App/App.xaml.cs` lines 147-175 (existing) | exact |
| `src/AppTemplate.App/MainWindow.xaml.cs` (MODIFY: NavItems entry) | config | request-response | `src/AppTemplate.App/MainWindow.xaml.cs` lines 82-96 (existing) | exact |

## Pattern Assignments

### `src/AppTemplate.App/Tweaks/GamingTweakDescriptor.cs` (model, transform)

**Analog:** `src/AppTemplate.App/Tweaks/TweakToggle.cs`

**Imports pattern** — TweakToggle.cs has NO usings (ImplicitUsings only); file-scoped namespace (`src/AppTemplate.App/Tweaks/TweakToggle.cs:1`):
```csharp
namespace AkariToolbox.Tweaks;
```

**Core descriptor pattern** (`src/AppTemplate.App/Tweaks/TweakToggle.cs:8-30`):
```csharp
public sealed class TweakToggle(
    string key,
    string title,
    string description,
    string stateKey,
    Func<bool> read,
    Action<bool> apply)
{
    /// <summary>Stable id used by the old app ("wifi", "defender", ).</summary>
    public string Key { get; } = key;

    public string Title { get; } = title;
    public string Description { get; } = description;

    /// <summary>The HKCU\Software\AkariTool value name this toggle persists to.</summary>
    public string StateKey { get; } = stateKey;

    /// <summary>Live system state  true means the toggle is ON.</summary>
    public Func<bool> Read { get; } = read;

    /// <summary>Applies the change for ON/OFF and updates the persisted state key.</summary>
    public Action<bool> Apply { get; } = apply;
}
```

**What to copy / what to extend:** Copy the primary-constructor + `Func<bool> Read` / `Action<bool> Apply` pairing verbatim. ADD per RESEARCH.md Pattern 1: `OneLiner` (ONE gamer sentence, D-05), `WhyItHelps`/`Risk`/`HowItReverts` (expander, BRW-02), `RecommendedLabel`/`DefaultLabel` (tooltip copy naming target values, D-06), `IReadOnlyList<RegFootprint> Footprint`, `bool RequiresReboot` (D-13), `string[] SharedRowIds` (D-12 cross-ref). Do NOT extend `TweakToggle` in place — its positional 6-arg constructor feeds 32 catalog entries; a new `sealed record` beside it keeps the gaming catalog independent. `/// <summary>` on every public member (project convention). Value equality via `record` (convention: `NavigationEntry`, messages).

**Auth pattern:** N/A (no auth in this app — single-user elevated tool).

**Error handling:** Descriptor itself throws nothing; fallible `Read` is wrapped at the call site by `Safe`/`SafeNullable` (see Shared Patterns).

---

### `src/AppTemplate.App/Tweaks/GamingCatalog.cs` (config, CRUD)

**Analog:** `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs`

**Imports pattern** — no usings block (`src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs:1-2`):
```csharp
namespace AkariToolbox.Tweaks;
```

**Core catalog pattern** (`src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs:9-19`):
```csharp
public static class AkariTweakCatalog
{
    public static IReadOnlyList<TweakToggle> All { get; } = Build();

    private static IReadOnlyList<TweakToggle> Build() =>
    [
        //  Connectivity / services 
        Toggle("wifi", "Disable WiFi", "Toggle WiFi On or Off", "DisableWiFi",
            read: () => RegRead.ServiceStart("WlanSvc") == 4,
            on: () => { Svc("WlanSvc", 4); Svc("vwififlt", 4); Svc("netprofm", 4); Svc("NlaSvc", 4); },
            off: () => { Svc("WlanSvc", 2); Svc("vwififlt", 1); Svc("netprofm", 3); Svc("NlaSvc", 2); }),
```

**Helper-facade pattern** (`src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs:543-575`):
```csharp
private static TweakToggle Toggle(
    string key, string title, string description, string stateKey,
    Func<bool> read, Action on, Action off) =>
    new(key, title, description, stateKey, read,
        enabled => { if (enabled) on(); else off(); });

private static void Svc(string service, int start) =>
    D($@"HKLM\SYSTEM\CurrentControlSet\Services\{service}", "Start", start);

private static void D(string path, string name, long value) =>
    TweakAction.RegDword(path, name, value).Apply();

private static void S(string path, string name, string value) =>
    TweakAction.RegString(path, name, value).Apply();

private static void DelV(string path, string name) =>
    TweakAction.RegDeleteValue(path, name).Apply();

private static void Exec(string fileName, string arguments) =>
    TweakAction.Run(fileName, arguments).Apply();
```

**What to copy:** `public static class GamingCatalog` + `All` + `Build()` with collection expression `[...]`, grouped by `// ---- Section dividers ----` comments (`//  Connectivity / services ` style), private `Toggle`-style factory folding `on`/`off` into one `apply` delegate, same `Svc`/`D`/`S`/`DelV`/`Exec` thin wrappers over `TweakAction.*(...).Apply()`. Seed 8–12 rows (gaming set + Game Mode + DVR per D-10); group System / Network / GPU / Background (D-11).

**Pitfall-critical excerpts to mirror per row:**
- Elevated-HKCU split — Game DVR/Game Bar rows MUST use `RealHkcu*`, not `HKCU\...` writes (`src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs:379-397`):
```csharp
Toggle("transparency", "Transparency Effects", "Toggle transparency effects", "TransparencyEffects",
    read: () => AkariToolState.RealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency") == 1,
    on: () => AkariToolState.SetRealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 1),
    off: () => AkariToolState.SetRealHkcuDword(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0)),
```
- Persisted-state reads for machine-specific-stock rows (`src/AppTemplate.App/Tweaks/GamingActions.cs:69`): `public static bool ReadNetworkOptimization() => AkariToolState.Has("NetworkOptimization");` — mark Custom-pill rows honestly (Pitfall 5).
- Cross-reference, don't duplicate (D-12): HAGS bodies already exist in `Tweaks/Data/Windows/ControlPanelOptimize.reg:1019` / `ControlPanelDefault.reg:962`; descriptor points via `SharedRowIds`, apply via `ControlPanelRows.ApplyRow(row, optimize)` (`src/AppTemplate.App/Tweaks/ControlPanelRows.cs:44-45`).

---

### `src/AppTemplate.App/Tweaks/RowRevertJournal.cs` (service, CRUD) — only if journal chosen (D-08 discretion)

**Analogs:** `src/AppTemplate.App/Tweaks/AkariToolState.cs` (persistence) + `src/AppTemplate.App/Tweaks/ControlPanelRows.cs` (probe/apply)

**Persistence pattern** (`src/AppTemplate.App/Tweaks/AkariToolState.cs:17-25`):
```csharp
public static void Save(string key) => Store.SetValue(key, 1);

public static void Clear(string key) => Store.DeleteValue(key, throwOnMissingValue: false);

public static bool Has(string key) => Store.GetValue(key) is not null;

public static void SetInt(string key, int value) => Store.SetValue(key, value, RegistryValueKind.DWord);

public static int GetInt(string key, int fallback) => Store.GetValue(key) is int i ? i : fallback;
```

**Probe pattern** (`src/AppTemplate.App/Tweaks/ControlPanelRows.cs:18-46`):
```csharp
public static bool? ReadRowOptimized(CpTweakRow row)
{
    // ...ProbeCache lookup omitted...
    try
    {
        if (probe.IsDword)
            return RegRead.Dword(probe.Key, probe.Name) == probe.DwordValue;
        return RegRead.String(probe.Key, probe.Name) == probe.StringValue;
    }
    catch
    {
        return null;
    }
}

public static void ApplyRow(CpTweakRow row, bool optimize) =>
    NativeOps.ImportRegContent(optimize ? row.Optimize : row.Default);
```

**What to copy:** Row-scoped first-capture-wins store: capture a row's footprint ONCE (before its first apply this session), never overwrite until reverted — D-08 forbids bulk-prefix form. Key by descriptor `Key`; persist via `AkariToolState`-style `HKCU\Software\AkariTool` helpers or in-memory dictionary. Default to journal unless a seed row's Default body is proven lossless (RESEARCH.md A5).

---

### `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs` (viewmodel, request-response)

**Analog:** `src/AppTemplate.App/ViewModels/TweaksViewModel.cs` — exact match (grouped rows + item-command + per-row busy + hydration guard)

**Imports pattern** (`src/AppTemplate.App/ViewModels/TweaksViewModel.cs:1-7`):
```csharp
using System.Collections.ObjectModel;
using AkariToolbox.Tweaks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AppTemplate.Framework.Navigation;
using AppTemplate.Framework.Services;
using AppTemplate.Framework.ViewModels;
```

**Class + DI shape** (`src/AppTemplate.App/ViewModels/TweaksViewModel.cs:17-51`):
```csharp
public partial class TweaksViewModel : ViewModelBase, INavigationAware
{
    private readonly IInfoBarService _infoBar;
    private readonly IStatusService _status;
    private bool _loading;
    private bool _busy;
    // ...
    public TweaksViewModel(IInfoBarService infoBar, IStatusService status)
    {
        _infoBar = infoBar;
        _status = status;
        Title = "Tweaks";
        BuildRowGroups();
    }
```
New VM: `public partial class GamingCatalogViewModel : ViewModelBase, INavigationAware`, ctor takes `(IInfoBarService infoBar, IStatusService status, IDialogService dialogs)` — `IDialogService` added for the D-09 first-apply restore offer (`CheckViewModel.cs:17-29` shows the 3-service ctor). Add `private bool _restoreOffered;` session flag.

**Grouped-section build pattern** (`src/AppTemplate.App/ViewModels/TweaksViewModel.cs:135-152`):
```csharp
private void BuildRowGroups()
{
    foreach (string tab in TabOrder)
    {
        CpTweakRow[] tabRows = ControlPanelRows.All.Where(r => r.Tab == tab).ToArray();
        if (tabRows.Length == 0)
            continue;
        var group = new CpTabGroup($"{tab} ({tabRows.Length} settings)");
        foreach (var section in tabRows.GroupBy(r => r.Section))
        {
            var sectionGroup = new CpSectionGroup(section.Key);
            foreach (CpTweakRow row in section)
                sectionGroup.Rows.Add(new CpRowItem(row, ApplyRowAsync, () => _loading));
            group.Sections.Add(sectionGroup);
        }
        TabGroups.Add(group);
    }
}
```
Copy for `System / Network / GPU / Background` groups (D-11): `CatalogSectionGroup` mirrors `CpSectionGroup(string title)` primary-constructor + `ObservableCollection<CatalogRowItem> Rows` (`src/AppTemplate.App/ViewModels/TweaksViewModel.cs:224-229`).

**Per-row run site — THE canonical copy target, adapted** (`src/AppTemplate.App/ViewModels/TweaksViewModel.cs:158-195`):
```csharp
internal async Task ApplyRowAsync(CpRowItem row, bool optimize)
{
    if (IsBusy)
        return;

    IsBusy = true;
    _status.Start($"{row.Row.Title}...");
    SetRowsBusy(true);
    try
    {
        await Task.Run(() => ControlPanelRows.ApplyRow(row.Row, optimize));
        if (row.IsRevertible)
            AkariToolState.SetInt("CpRow:" + row.Row.Id, optimize ? 1 : 0);
        _infoBar.Show($"{row.Row.Title}  applied", "Restart to apply.");
    }
    catch (Exception ex)
    {
        _loading = true;
        try { row.RefreshFromSystem(); }
        finally { _loading = false; }
        _infoBar.Show($"{row.Row.Title}  failed", ex.Message);
    }
    finally
    {
        SetRowsBusy(false);
        IsBusy = false;
        _status.Complete();
    }
}

/// <summary>Locks or unlocks every row's controls, matching the WPF page's page-wide IsBusy.</summary>
private void SetRowsBusy(bool value)
{
    foreach (CpTabGroup group in TabGroups)
        foreach (CpSectionGroup section in group.Sections)
            foreach (CpRowItem row in section.Rows)
                row.IsBusy = value;
}
```
**Adaptation (binding per UI-SPEC hard rule 5):** lock ONLY the affected row (`row.IsBusy = true`), NOT `SetRowsBusy(true)` page-wide — other rows stay interactive. Keep everything else: `_status.Start/Complete` pairing, `Task.Run` off UI thread, `RefreshFromSystem()` rollback under `_loading` guard on failure, `"{Title} applied/failed"` InfoBar copy. `RequiresReboot` rows append `"Restart to apply."` note.

**Row-item class pattern — mirror exactly** (`src/AppTemplate.App/ViewModels/TweaksViewModel.cs:231-290`):
```csharp
/// <summary>One granular row: a live-state toggle, or an Apply button when one-way.</summary>
public sealed partial class CpRowItem : ObservableObject
{
    private readonly Func<CpRowItem, bool, Task> _apply;
    private readonly Func<bool> _isLoading;

    public CpTweakRow Row { get; }
    // ...
    [ObservableProperty]
    private bool _isOn;

    /// <summary>Set while a tweak is being applied, so the row's toggle and Apply button lock.</summary>
    [ObservableProperty]
    private bool _isBusy;

    public CpRowItem(CpTweakRow row, Func<CpRowItem, bool, Task> apply, Func<bool> isLoading)
    {
        Row = row;
        _apply = apply;
        _isLoading = isLoading;
    }

    /// <summary>False while an apply is in flight, so the row's controls disable themselves.</summary>
    public bool IsEnabled => !IsBusy;

    [RelayCommand]
    private Task ApplyAsync() => IsOneWay ? _apply(this, true) : Task.CompletedTask;

    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));

    /// <summary>Live probe first, persisted choice as fallback.</summary>
    public void RefreshFromSystem()
    {
        try
        {
            bool? live = ControlPanelRows.ReadRowOptimized(Row);
            IsOn = live ?? AkariToolState.GetInt("CpRow:" + Row.Id, 0) == 1;
        }
        catch
        {
            IsOn = AkariToolState.GetInt("CpRow:" + Row.Id, 0) == 1;
        }
    }

    partial void OnIsOnChanged(bool value)
    {
        if (_isLoading())
            return;
        _ = _apply(this, value);
    }
}
```
New `CatalogRowItem : ObservableObject` copies this 1:1 and ADDS: `SetRecommendedCommand` / `SetDefaultCommand` (`[RelayCommand]` → `_apply(this, Target.Recommended/Default)`), badge-pill state property derived from post-apply `RefreshFromSystem()` (D-07 — never request-echo), `RequiresReboot` passthrough, expander-bound footprint strings.

**Item-command rationale comment to carry over** (`src/AppTemplate.App/ViewModels/GamingViewModel.cs:359-363`):
```csharp
/// One button in the Tools grid. WPF reached the page's RunButtonCommand through
/// RelativeSource AncestorType=Page; WinUI DataTemplates have no equivalent, so the command
/// lives on the tile and delegates to the same Run path.
```

**Hydration pattern** (`src/AppTemplate.App/ViewModels/TweaksViewModel.cs:53-81` + `src/AppTemplate.App/ViewModels/GamingViewModel.cs:84-105`):
```csharp
public void OnNavigatedTo(object? parameter)
{
    _loading = true;
    try
    {
        // ...per-row RefreshFromSystem() loop...
        foreach (CpTabGroup tab in TabGroups)
        {
            foreach (CpSectionGroup section in tab.Sections)
            {
                foreach (CpRowItem row in section.Rows)
                    row.RefreshFromSystem();
            }
        }
    }
    finally
    {
        _loading = false;
    }
}
```
Copy with `CatalogSectionGroup`/`CatalogRowItem` types; reads run synchronously here for registry probes but MUST move into `Task.Run` for WMI/NIC-heavy rows (Pitfall 4: "reads are slow — refresh off-thread"). `Safe`/`SafeNullable` wrappers for probes (`src/AppTemplate.App/ViewModels/GamingViewModel.cs:130-134`, `TweaksViewModel.cs:87-91`).

**Change-handler guard** (`src/AppTemplate.App/ViewModels/GamingViewModel.cs:140-146`):
```csharp
partial void OnDisablePreemptionChanged(bool value)
{
    if (_loading) return;
    ApplyToggle("Disable Preemption (NVIDIA)", value, ...);
}
```
Every `On<Prop>Changed` in the new VM/item gets `if (_loading/_isLoading()) return;` first line (Pitfall 2).

**Per-row toggle-run alternative shape** (`src/AppTemplate.App/ViewModels/GamingViewModel.cs:202-229`):
```csharp
private async void ApplyToggle(
    string title, bool target,
    Action<bool> apply, Func<bool> read,
    Action<bool> rollback, string note)
{
    if (IsBusy)
        return;

    IsBusy = true;
    _status.Start($"{title}...");
    try
    {
        await Task.Run(() => apply(target));
        _infoBar.Show($"{title}  applied", note);
    }
    catch (Exception ex)
    {
        _loading = true;
        try { rollback(Safe(read)); }
        finally { _loading = false; }
        _infoBar.Show($"{title}  failed", ex.Message);
    }
    finally
    {
        IsBusy = false;
        _status.Complete();
    }
}
```

**Restore-point offer gate (D-09) — compose two existing patterns:**
Offer dialog via `IDialogService.ConfirmAsync` (`src/AppTemplate.Framework/Services/IDialogService.cs:82-90`):
```csharp
public async Task<bool> ConfirmAsync(
    string title,
    string message,
    string confirmText = "Confirm",
    string? cancelText = "Cancel")
{
    var result = await ShowAsync(title, message, confirmText, cancelText: cancelText);
    return result == ContentDialogResult.Primary;
}
```
Creation via `RestorePointActions` off-thread with warn-and-proceed (`src/AppTemplate.App/ViewModels/HomeViewModel.cs:94-117`):
```csharp
/// <summary>Native restore point (WMI SystemRestore). Slow  off-thread with the page locked.</summary>
[RelayCommand]
private async Task CreateRestorePointAsync()
{
    if (IsBusy)
        return;

    IsBusy = true;
    _status.Start("Creating restore point...");
    try
    {
        await Task.Run(() => RestorePointActions.CreateRestorePoint("Akari Toolbox"));
        _infoBar.Show("Restore point  created", "You can roll back from System Protection.");
    }
    catch (Exception ex)
    {
        _infoBar.Show("Restore point  failed", ex.Message);
    }
    finally
    {
        IsBusy = false;
        _status.Complete();
    }
}
```
New flow: on first apply per session (`if (!_restoreOffered)`), `await _dialogs.ConfirmAsync("Create a restore point?", "<UI-SPEC body>", "Create restore point", "Skip")` → if confirmed, `await Task.Run(() => RestorePointActions.CreateRestorePoint("Akari Toolbox"))` catching to `_infoBar.Show("Restore point skipped", ex.Message)` → proceed with apply regardless; set `_restoreOffered = true` either way. Restore op itself (`src/AppTemplate.App/Tweaks/RestorePointActions.cs:17-51`): frequency-0 reg write + WMI `Enable` + `CreateRestorePoint` with `using`-disposed `ManagementClass`/params/result — copy disposal discipline.

**Search/filter (D-14) — title-only prefix-first, in-VM LINQ (no new control pattern):**
```csharp
IEnumerable<GamingTweakDescriptor> Search(string query)
{
    if (string.IsNullOrWhiteSpace(query)) return GamingCatalog.All;
    string q = query.Trim();
    return GamingCatalog.All
        .Where(d => d.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(d => d.Title.StartsWith(q, StringComparison.OrdinalIgnoreCase))
        .ThenBy(d => d.Title);
}
IEnumerable<string> Suggest(string query) => Search(query).Take(8).Select(d => d.Title);
```
Suggestions cap 8, trigger from first character, tap-to-filter never applies; empty state "No matching tweaks" + "Try a different search — matching is by tweak title only." (UI-SPEC copy contract).

---

### `src/AppTemplate.App/Views/GamingCatalogPage.xaml` (+ `.xaml.cs`) (component, request-response)

**Analogs:** `src/AppTemplate.App/Views/TweaksPage.xaml` (grouped rows + DataTemplate binding) + `src/AppTemplate.App/Views/Shared/PageStyles.xaml` (card styles)

**Page-shell pattern** (`src/AppTemplate.App/Views/TweaksPage.xaml:1-20`):
```xml
<Page
  x:Class="AkariToolbox.Views.TweaksPage"
  xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
  xmlns:models="using:AkariToolbox.ViewModels">

  <!--
  Binding note: the page's own controls use classic {Binding}; the collapsible groups,
  their sections and the rows use x:Bind inside DataTemplates. Page-level x:Bind needs
  MarkupCompilePass2 ... (WMC9999); x:Bind inside a DataTemplate
  resolves from x:DataType and works ...
  -->
  <ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
  <StackPanel Padding="32" Spacing="16" MaxWidth="1000" HorizontalAlignment="Left">
```
Copy shell verbatim (change `x:Class` + comment). Page-level `{Binding}` for search box + groups; `x:Bind` ONLY inside `DataTemplate` with `x:DataType`. `ToggleSwitch` gets `MinWidth="0"`.

**Grouped-list pattern** (`src/AppTemplate.App/Views/TweaksPage.xaml:68-75`, `83-101`):
```xml
  <ItemsControl ItemsSource="{Binding TabGroups, Mode=OneWay}">
  ...
  <DataTemplate x:DataType="models:CpTabGroup">
  ...
  <ItemsControl
  ItemsSource="{x:Bind Sections, Mode=OneWay}"
  Visibility="{x:Bind IsExpanded, Mode=OneWay, Converter={StaticResource BoolToVisibility}}">
```
Copy nesting: outer `ItemsControl` (`{Binding Sections}`) → section `DataTemplate x:DataType="models:CatalogSectionGroup"` → inner `ItemsControl` (`{x:Bind Rows}`) → row `DataTemplate x:DataType="models:CatalogRowItem"`.

**Row-card pattern** (`src/AppTemplate.App/Views/TweaksPage.xaml:92-145`):
```xml
  <Border Style="{StaticResource AkariCard}">
  <StackPanel Spacing="16">
  ...
  <DataTemplate x:DataType="models:CpRowItem">
  <StackPanel Spacing="8">

  <Grid ColumnSpacing="16">
  <Grid.ColumnDefinitions>
  <ColumnDefinition Width="*" />
  <ColumnDefinition Width="Auto" />
  </Grid.ColumnDefinitions>

  <TextBlock ... Text="{x:Bind Row.Title, Mode=OneWay}" ... />

  <ToggleSwitch
  MinWidth="0"
  Grid.Column="1"
  ...
  IsEnabled="{x:Bind IsEnabled, Mode=OneWay}"
  IsOn="{x:Bind IsOn, Mode=TwoWay}"
  Visibility="{x:Bind IsRevertible, Mode=OneWay, Converter={StaticResource BoolToVisibility}}" />
```
Extend per UI-SPEC §68 row anatomy: title (`AkariCardTitle`) + control right; ONE-liner (`AkariCardDescription`, max 2 visual lines); pill row (`Recommended`/`Default`/`Custom` + `Restart required`); quick-set pair (green "Set to Recommended" default-button glyph E735 + grey "Set to Default", tooltips = `RecommendedLabel`/`DefaultLabel`); slim `Expander` bar (info icon + "Technical details" + chevron, collapsed) with Consolas-12px selectable registry rows + Current/Recommended/Default + "Open in Registry Editor"; applying = `ProgressRing` overlay (`IsApplying`); notices = quiet card-attached `InfoBar` strip (`HasStatusBanner`), never modal. `Expander` is new surface (no existing XAML — grep for `Expander|AutoSuggestBox|CollectionViewSource` in `src/` returns nothing) but first-party in WindowsAppSDK 2.3.1.

**Code-behind pattern** (`src/AppTemplate.App/Views/TweaksPage.xaml.cs:9-26`):
```csharp
public sealed partial class TweaksPage : Page
{
    /// <summary>Localized string accessor used by x:Bind function bindings.</summary>
    public LocalizedStrings Strings { get; }

    public TweaksViewModel ViewModel { get; }

    public TweaksPage(TweaksViewModel viewModel)
    {
        Strings = App.Services.GetRequiredService<LocalizedStrings>();
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }
}
```
Copy with `GamingCatalogPage` / `GamingCatalogViewModel`. Needs `using Microsoft.Extensions.DependencyInjection; using Microsoft.UI.Xaml.Controls; using AkariToolbox.Services; using AkariToolbox.ViewModels;`.

**Card styles — reuse, do not duplicate** (`src/AppTemplate.App/Views/Shared/PageStyles.xaml:13-45`):
```xml
    <Style x:Key="AkariCard" TargetType="Border"> ... CornerRadius 8, Padding 16 ... </Style>
    <Style x:Key="AkariCardTitle" ... BasedOn BodyTextBlockStyle, SemiBold, CharacterEllipsis />
    <Style x:Key="AkariCardDescription" ... secondary fill, Wrap />
    <Style x:Key="AkariSectionHeader" ... SubtitleTextBlockStyle, Margin 0,8,0,0 />
    <Style x:Key="AkariCaption" ... CaptionTextBlockStyle, Wrap />
    <Thickness x:Key="AkariPageMargin">32,32,32,32</Thickness>
```
Full excerpts: lines 14-20 (`AkariCard`), 23-26 (`AkariCardTitle`), 29-32 (`AkariCardDescription`), 35-37 (`AkariSectionHeader`), 40-42 (`AkariCaption`), 45 (`AkariPageMargin`). Registry paths inside expander: Consolas 12px + `IsTextSelectionEnabled="True"` (UI-SPEC typography exception).

---

### `src/AppTemplate.App/App.xaml.cs` + `src/AppTemplate.App/MainWindow.xaml.cs` (MODIFY)

**DI pattern** (`src/AppTemplate.App/App.xaml.cs:147-175`):
```csharp
builder.Services.AddTransient<GamingViewModel>();
// ...
builder.Services.AddTransient<GamingPage>();
```
Add `AddTransient<GamingCatalogViewModel>()` after `GamingViewModel`, `AddTransient<GamingCatalogPage>()` after `GamingPage`.

**Nav pattern** (`src/AppTemplate.App/MainWindow.xaml.cs:82-96`):
```csharp
public IReadOnlyList<NavigationItem> NavItems { get; } =
[
    new("Home", "\uE80F", typeof(HomePage)),
    new("Akari OS Tweaks", "\uE713", typeof(AkariTweaksPage)),
    new("Gaming Tweaks", "\uE7FC", typeof(GamingPage)),
    ...
```
Add `new("Gaming Catalog", "\uE7FC", typeof(GamingCatalogPage))` after Gaming Tweaks (D-11: dedicated page in nav).

---

## Shared Patterns

### MVVM Toolkit idioms (applies to: VM + all item classes)
**Source:** `src/AppTemplate.App/ViewModels/GamingViewModel.cs:26-71`, `TweaksViewModel.cs:243-248,268`
- `partial` class + `using CommunityToolkit.Mvvm.ComponentModel; using CommunityToolkit.Mvvm.Input;`
- State = `[ObservableProperty] private bool _isBusy;` → `IsBusy`; derived `IsEnabled => !IsBusy` + `partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));`
- Verbs = `[RelayCommand] private Task XxxAsync()`
- Guard every change handler: `if (_loading) return;` (+ bounds checks for indices)
```csharp
[ObservableProperty]
private bool _isBusy;

public bool IsEnabled => !IsBusy;

partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));
```

### Run-site InfoBar + Status pairing (applies to: every apply/revert/restore path)
**Source:** `src/AppTemplate.App/ViewModels/GamingViewModel.cs:210-228`, `TweaksViewModel.cs:163-185`, `HomeViewModel.cs:101-116`
```csharp
_status.Start($"{title}...");
try
{
    await Task.Run(() => apply(target));
    _infoBar.Show($"{title}  applied", note);
}
catch (Exception ex)
{
    _infoBar.Show($"{title}  failed", ex.Message);
}
finally
{
    _status.Complete();
}
```
Success → `Show("{title}  applied", note)`; failure → `Show("{title}  failed", ex.Message)`; `Complete()` in `finally`. Never surface stack traces. Reboot rows: note = `"Restart to apply."`.

### Live-read Safe wrappers (applies to: hydration + rollback)
**Source:** `src/AppTemplate.App/ViewModels/GamingViewModel.cs:130-134`, `TweaksViewModel.cs:87-91`
```csharp
private static bool Safe(Func<bool> read)
{
    try { return read(); }
    catch { return false; }
}
private static int? SafeNullable(Func<int?> read)
{
    try { return read(); }
    catch { return null; }
}
```

### Native-ops allow-list (applies to: every seed row)
**Source:** `src/AppTemplate.App/Tweaks/TweakAction.cs:18-79`
`RegDword/RegQword/RegString/RegExpandString/RegBinary/RegBinaryHex`, `RegDeleteValue/RegDeleteKey`, `Run` (hidden+waited system tools), `Launch`/`Open` (user-visible), `Custom` (WMI escape hatch). No scripts, no bundled binaries — planner must reject any row needing a network fetch or binary launch.

### WMI disposal + off-thread (applies to: restore point, any WMI/system probe)
**Source:** `src/AppTemplate.App/Tweaks/RestorePointActions.cs:28-37`
```csharp
using var cls = new ManagementClass(SystemRestorePath);
using var enableParams = cls.GetMethodParameters("Enable");
enableParams["DriveLetter"] = drive;
cls.InvokeMethod("Enable", enableParams, null);

using var createParams = cls.GetMethodParameters("CreateRestorePoint");
createParams["Description"] = description;
createParams["RestorePointType"] = (uint)12; // MODIFY_SETTINGS
createParams["EventType"] = (uint)100;        // BEGIN_SYSTEM_CHANGE
using ManagementBaseObject? result = cls.InvokeMethod("CreateRestorePoint", createParams, null);
```
Every `ManagementClass`/`ManagementObject`/`ManagementObjectCollection` in `using`; all slow work in `Task.Run`.

### XAML binding hard rules (applies to: new page XAML)
1. Page root `{Binding}` + `DataContext = ViewModel`; 2. `x:Bind` only in `DataTemplate` with `x:DataType`; 3. `ToggleSwitch MinWidth="0"`; 4. `InvertedBooleanConverter` for negation; 5. per-row `IsBusy` only, never page-wide lock for one row's apply.

## No Analog Found

| File | Role | Data Flow | Reason |
|------|------|-----------|--------|
| Technical-details `Expander` XAML block | component | request-response | Genuinely new surface — repo grep for `Expander\|AutoSuggestBox\|CollectionViewSource` in `src/` returns zero matches. Build from `CpRowItem` DataTemplate base + first-party WinUI 3 `Expander` (`Microsoft.UI.Xaml.Controls`, WindowsAppSDK 2.3.1); follow winhance-patterns.md §3 (slim bar, collapsed, Consolas selectable rows, Open-in-RegEdit button) + UI-SPEC §§68/83. |
| Search `AutoSuggestBox`/suggestion dropdown XAML | component | request-response | Same — no existing search XAML. Planner picks `AutoSuggestBox` vs `TextBox`+popup (discretion); ranking LINQ above is the contract (D-14). |

## Metadata

**Analog search scope:** `src/AppTemplate.App/ViewModels/`, `src/AppTemplate.App/Tweaks/`, `src/AppTemplate.App/Views/`, `src/AppTemplate.Framework/Services/`; all analog paths verified git-tracked via `git ls-files` (15/15).
**Files scanned:** 15 (GamingViewModel, TweaksViewModel, CheckViewModel, HomeViewModel, TweakToggle, AkariTweakCatalog, ControlPanelRows, GamingActions, RestorePointActions, AkariToolState, TweakAction, TweaksPage.xaml[.cs], PageStyles.xaml, App.xaml.cs, MainWindow.xaml.cs, IDialogService.cs)
**Pattern extraction date:** 2026-10-04
