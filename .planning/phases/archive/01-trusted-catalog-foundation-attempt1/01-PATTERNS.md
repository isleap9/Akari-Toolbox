# Phase 1: Trusted Catalog Foundation - Pattern Map

**Mapped:** 2026-10-03
**Files analyzed:** 6 new + 3 extended/edited (from RESEARCH.md §File Touch List)
**Analogs found:** 9 / 9 (all git-tracked; verified via `git ls-files`)

## File Classification

| New/Modified File | Role | Data Flow | Closest Analog | Match Quality |
|---|---|---|---|---|
| `src/AppTemplate.App/Tweaks/GamingCatalog.cs` (NEW) | model (descriptor catalog) | request-response (in-memory LINQ) | `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs` | role-match |
| `src/AppTemplate.App/Tweaks/GamingJournal.cs` (NEW) | service (snapshot persistence) | file-I/O | `src/AppTemplate.Framework/Services/FileSettingsStorage.cs` | data-flow-match |
| `src/AppTemplate.App/ViewModels/GamingCatalogViewModel.cs` (NEW) | viewmodel + item class | request-response + CRUD | `src/AppTemplate.App/ViewModels/TweaksViewModel.cs` | exact |
| `src/AppTemplate.App/Views/GamingCatalogPage.xaml` (NEW) | view (page) | request-response | `src/AppTemplate.App/Views/GamingPage.xaml` | exact |
| `src/AppTemplate.App/Views/GamingCatalogPage.xaml.cs` (NEW) | view code-behind | request-response | `src/AppTemplate.App/Views/CheckPage.xaml.cs` | exact |
| `src/AppTemplate.Tests/GamingCatalogTests.cs` (NEW) | test | batch | `src/AppTemplate.Tests/SettingsServiceTests.cs` | role-match |
| `src/AppTemplate.App/Tweaks/GamingActions.cs` (EXTEND) | service (native ops) | CRUD (registry) | itself — `ReadPreemption`/`SetPreemption` (lines 35-61) | exact (in-place) |
| `src/AppTemplate.App/App.xaml.cs` (EDIT) | config (DI registration) | request-response | itself — `BuildHost` lines 147-175 | exact (in-place) |
| `src/AppTemplate.App/MainWindow.xaml.cs` (EDIT) | config (nav registration) | request-response | itself — `NavItems` lines 82-96 | exact (in-place) |

## Pattern Assignments

### `Tweaks/GamingCatalog.cs` (NEW — model, in-memory descriptor list)

**Analog:** `src/AppTemplate.App/Tweaks/AkariTweakCatalog.cs` (Toggle factory + `fso` Game Mode/DVR key set)

**Imports pattern** — file has no usings (relies on `ImplicitUsings`); namespace is file-scoped:
```csharp
namespace AkariToolbox.Tweaks;
```

**Factory pattern** (`AkariTweakCatalog.cs` lines 543-547) — copy for `FromNative`/`FromSharedRow`:
```csharp
private static TweakToggle Toggle(
    string key, string title, string description, string stateKey,
    Func<bool> read, Action on, Action off) =>
    new(key, title, description, stateKey, read,
        enabled => { if (enabled) on(); else off(); });
```
Reuse guidance: mirror this private-factory + static seed-list shape, but return the new `GamingCatalogEntry` record (with `Group`, 4 explanation strings, `RequiresReboot`, `Footprint`, optional `SharedRowId`) instead of `TweakToggle`. Seed list uses collection-expression syntax `[...]` per convention. See RESEARCH.md §1 for the recommended record shape.

**Game Mode / DVR seed source** (`AkariTweakCatalog.cs` lines 297-329) — extract, don't move:
```csharp
Toggle("fso", "Disable FSO and Gamebar", "Toggle FSO and Gamebar On or Off", "DisableFSO",
    read: () => RegRead.Dword(@"HKCU\System\GameConfigStore", "GameDVR_FSEBehaviorMode") == 2,
    on: () =>
    {
        D(@"HKCU\Software\Microsoft\GameBar", "ShowStartupPanel", 0);
        // ... GameDVR_* keys, AllowGameDVR, AppCaptureEnabled, BcastDVRUserService ...
    },
```
Reuse guidance: `ReadGameMode`/`SetGameMode` + `ReadGameDvr`/`SetGameDvr` are carved out of this key set (GameBar `AllowAutoGameMode`/`AutoGameModeEnabled` = Game Mode; `GameDVR_Enabled`/`AllowGameDVR`/`AppCaptureEnabled`/`BcastDVRUserService` = DVR). The `fso` toggle itself is untouched (D-01). Write-through helpers `D`/`S`/`DelV`/`Svc` live at `AkariTweakCatalog.cs:562-572` — reuse via `TweakAction.RegDword(...).Apply()` directly.

**Shared-row shape** (`src/AppTemplate.App/Tweaks/ControlPanelRows.Generated.cs` line 8 — READ-ONLY, never edit):
```csharp
public sealed record CpTweakRow(string Id, string Title, string Tab, string Section, bool Revertible, string Optimize, string Default);
```
Reuse guidance: `FromSharedRow` resolves `ControlPanelRows.All` by `Id` once at build time (fail-fast on rename), delegates `Read` to `ControlPanelRows.ReadRowOptimized(row)` and `Apply` to `ControlPanelRows.ApplyRow(row, on)`. Store only the `SharedRowId` + resolved reference — never duplicate `.reg` text (D-04).

---

### `Tweaks/GamingJournal.cs` (NEW — service, file-I/O snapshot journal)

**Analog:** `src/AppTemplate.Framework/Services/FileSettingsStorage.cs` (atomic JSON persistence) + `src/AppTemplate.App/Tweaks/AkariToolState.cs` (`RegRead` null-on-missing semantics)

**Atomic-save pattern** (`FileSettingsStorage.cs` lines 90-99) — copy verbatim for journal writes:
```csharp
private void Save(Dictionary<string, string> dictionary)
{
    var json = JsonSerializer.Serialize(dictionary, SerializerOptions);

    // Write to a temp file then atomically move it into place, so a crash mid-write
    // can never leave a corrupt settings.json behind.
    var tempPath = _filePath + ".tmp";
    File.WriteAllText(tempPath, json);
    File.Move(tempPath, _filePath, overwrite: true);
}
```
Reuse guidance: journal file is `%LOCALAPPDATA%\AkariToolbox\gaming-journal.json` (beside `SettingsFolder`/`SettingsFilePath` in `App.xaml.cs:37-41`). `JsonSerializerOptions` with `WriteIndented = true` is the file's `static readonly SerializerOptions` (lines 11-14). Lock around load+save with a private `object _lock` (lines 16, 31, 40).

**Corrupt-degrade pattern** (`FileSettingsStorage.cs` lines 70-88):
```csharp
try
{
    var json = File.ReadAllText(_filePath);
    var dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(json, SerializerOptions);
    return dictionary ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
catch
{
    // A missing, corrupt or unreadable file falls back to empty settings.
    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
```
Reuse guidance: corrupt journal → empty journal (+ warning InfoBar), never throw. Typed schema carries a `Version` int; old versions are ignored-with-warning.

**Absent-vs-zero capture** (`AkariToolState.cs` lines 98-123) — the capture protocol hinges on this:
```csharp
/// <summary>DWORD value, or null when the key/value is missing.</summary>
public static int? Dword(string fullPath, string name) { ... return key?.GetValue(name) is int i ? i : null; }
/// <summary>True when the value exists (any kind).</summary>
public static bool HasValue(string fullPath, string name) { ... return key?.GetValue(name) is not null; }
```
Reuse guidance: capture must check `HasValue` first — `JournalValue.Kind is null` means "absent" and revert does `DeleteValue`. Conflating absent with `0` leaves residue (RESEARCH.md P9). `RegRead.ServiceStart` (lines 125-127) covers service-`Start` footprints.

**Registry path resolution** (`TweakAction.cs` lines 82-102, `RegistryPath.Resolve`) — reuse for capture AND revert write-back (handles `HKLM\...`, `HKCU\...`, `HKCR`, `HKU`, `HKCC`):
```csharp
public static (RegistryKey baseKey, string subPath) Resolve(string fullPath)
```

**`.reg`-body footprint parser** (`ControlPanelRows.cs` lines 47-74, `ParseProbe` — READ-ONLY source, generalize in journal):
```csharp
if (line.StartsWith('[') && line.EndsWith(']')) { key = line[1..^1]; continue; }
// ...
if (data.StartsWith("dword:", StringComparison.OrdinalIgnoreCase) &&
    int.TryParse(data["dword:".Length..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int dword))
    return new Probe(key, name, true, dword, string.Empty);
if (data.Length >= 2 && data.StartsWith('"') && data.EndsWith('"'))
    return new Probe(key, name, false, 0, data[1..^1]);
```
Reuse guidance: put the full-body enumerator (all `(key, name, kind)` triples, not just the first) in `GamingJournal.cs` as `internal static` — `ControlPanelRows.Generated.cs` says "do not edit by hand" and `ControlPanelRows.cs` probe-first-value shape must not be repurposed for capture (RESEARCH.md P1). Note `-` deletion lines in Default bodies need no capture: capture reads *live state*, not the body.

**Multi-value completeness rule:** preemption touches 6 values (`GamingActions.cs:43-48`) but `ReadPreemption` probes only 2 (`GamingActions.cs:35-37`). Journal completeness is driven by `Footprint`, never by the read probe.

**Why NOT `AkariToolState` / `settings.json` for the journal:** `AkariToolState` is single-bit toggle memory (`Save`/`Clear`/`Has`/`SetInt`/`GetInt`, lines 17-25) — binary blobs would need hex encoding and subkey-per-row enumeration. `settings.json` is a flat `Dictionary<string,string>` for user prefs. Both rejected in RESEARCH.md §2; dedicated versioned JSON file is the decision.

---

### `ViewModels/GamingCatalogViewModel.cs` (NEW — viewmodel + row item)

**Analog:** `src/AppTemplate.App/ViewModels/TweaksViewModel.cs` (bulk run-site `ApplyRowAsync` + `CpRowItem` co-location + `SetRowsBusy` fan-out + `OnNavigatedTo` refresh loop)

**Class + ctor pattern** (`TweaksViewModel.cs` lines 17-51):
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
Reuse guidance: `public partial class GamingCatalogViewModel : ViewModelBase, INavigationAware`; ctor injects `IInfoBarService` + `IStatusService` + `IDialogService` (dialog needed for the D-15 restore-point offer — see `CheckViewModel` ctor lines 24-29 for the 3-service ctor shape). `using CommunityToolkit.Mvvm.ComponentModel;` + `using CommunityToolkit.Mvvm.Input;` at top; `partial` is load-bearing for source generation. `Title` set in ctor.

**Bulk run-site pattern** (`TweaksViewModel.cs` lines 158-195) — the closest precedent for `ApplySelectedAsync`:
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
```
Reuse guidance: Phase 1 bulk loop wraps this in a sequential `foreach` over ticked rows (catalog order, D-11): per row `await Task.Run(() => { GamingJournal.Capture(row.Entry); row.Entry.Apply(true); })` → `MarkApplied` → `_status.Report(100.0*done/total, $"Applying {done} of {total}: {title}")`; on exception record + `break` (D-14 stop-at-first-failure — deliberate override of ARCHITECTURE.md Pattern 3 continue-on-error). Ticks never cleared (D-12). `finally` unlocks + `Complete()`. See RESEARCH.md §5 for the full loop sketch + UI-SPEC copy strings.

**IsBusy fan-out pattern** (`TweaksViewModel.cs` lines 188-195; `GamingViewModel.cs` lines 67-71):
```csharp
private void SetRowsBusy(bool value)
{
    foreach (CpTabGroup group in TabGroups)
        foreach (CpSectionGroup section in group.Sections)
            foreach (CpRowItem row in section.Rows)
                row.IsBusy = value;
}
```
```csharp
partial void OnIsBusyChanged(bool value)
{
    foreach (GamingButton button in ToolButtons)
        button.IsBusy = value;
}
```
Reuse guidance: new VM fans `IsBusy` to all `GamingCatalogRowItem.IsBusy` during apply/revert. Note `TweaksViewModel.IsBusy` is hand-written over `_busy` (lines 35-39) because it fans out via `SetRowsBusy`; `GamingViewModel` uses `[ObservableProperty] bool _isBusy` + `OnIsBusyChanged` partial instead. Planner's choice — but if row fan-out is needed, the hand-written `SetProperty` shape avoids re-entrancy surprises.

**Per-toggle run idiom** (`GamingViewModel.cs` lines 202-229, `ApplyToggle`): guard `IsBusy` → `_status.Start` → `await Task.Run(apply)` → InfoBar → `catch` with read-back rollback → `finally` unlock + `Complete`. At bulk scale the catch-path rollback becomes journal replay, not read-back rollback.

**Dialog-report step** (`CheckViewModel.cs` lines 86-109, `RunAsync`): same run-site shape plus `await _dialogs.ShowInfoAsync(title, guidance)` after work. Precedent for the restore-point *offer* step, but the offer uses `ShowAsync` with primary+secondary (see Shared Patterns).

**Hydration-guard pattern** (`TweaksViewModel.cs` lines 53-81; `GamingViewModel.cs` lines 84-105, 130-134):
```csharp
public void OnNavigatedTo(object? parameter)
{
    _loading = true;
    try { /* ... row.RefreshFromSystem() ... */ }
    finally { _loading = false; }
}
private static bool Safe(Func<bool> read)
{
    try { return read(); }
    catch { return false; }
}
```
Reuse guidance: live-state refresh on every `OnNavigatedTo`; every `On<Prop>Changed` handler starts with `if (_loading) return;` (plus bounds checks for index handlers). Wrap every `Read()` in `Safe`/`SafeNullable` so a probe never crashes navigation.

**Row-item pattern** (`TweaksViewModel.cs` lines 231-290, `CpRowItem` — same-file co-location):
```csharp
public sealed partial class CpRowItem : ObservableObject
{
    // ctor takes row + apply delegate + isLoading gate
    [ObservableProperty]
    private bool _isOn;
    [ObservableProperty]
    private bool _isBusy;
    /// <summary>False while an apply is in flight, so the row's controls disable themselves.</summary>
    public bool IsEnabled => !IsBusy;
    [RelayCommand]
    private Task ApplyAsync() => IsOneWay ? _apply(this, true) : Task.CompletedTask;
    partial void OnIsBusyChanged(bool value) => OnPropertyChanged(nameof(IsEnabled));
    public void RefreshFromSystem() { /* live probe first, persisted fallback */ }
    partial void OnIsOnChanged(bool value)
    {
        if (_isLoading())
            return;
        _ = _apply(this, value);
    }
}
```
Reuse guidance: `GamingCatalogRowItem` lives in the same file. Differences per RESEARCH.md §5: `[ObservableProperty] bool _isSelected` (TwoWay-bound `CheckBox` — **no item command needed**, unlike `GamingButton`/`CpRowItem` which need commands for buttons/toggle-apply); `IsSelected` is never mutated by apply/revert paths (D-12). Live-state display refreshed via `RefreshFromSystem()` with `Safe()` wrappers. The XML comment on `ApplyAsync` documents *why* the command lives on the item (no `RelativeSource AncestorType` in WinUI) — carry that comment style forward.

**Search state:** VM holds per-group `ObservableCollection<RowItem>` rebuilt on filter change; group headers collapse when empty; "No matching tweaks" empty state when all four are empty. Keystroke path touches only the in-memory descriptor list — never call `Read()` on keystroke (established anti-pattern). Suggest ranking: `StartsWith` first, then `Contains`, `OrdinalIgnoreCase`, `.Take(8)` — see RESEARCH.md §3/`Suggest()` sketch.

---

### `Views/GamingCatalogPage.xaml` (NEW — page)

**Analog:** `src/AppTemplate.App/Views/GamingPage.xaml` (row grid + `AkariCard` groups + `DataTemplate x:Bind`)

**Page-container pattern** (`GamingPage.xaml` lines 21-24):
```xml
<ScrollViewer VerticalScrollBarVisibility="Auto" HorizontalScrollBarVisibility="Disabled">
<StackPanel Padding="32" Spacing="16" MaxWidth="1000" HorizontalAlignment="Left">
```
Reuse guidance: identical container for the new page (UI-SPEC layout contract). `xmlns:models="using:AkariToolbox.ViewModels"` import for `x:DataType`.

**Row-grid pattern** (`GamingPage.xaml` lines 26-45) — copy per catalog row, CheckBox replaces ToggleSwitch:
```xml
<Border Style="{StaticResource AkariCard}">
<StackPanel Spacing="16">
<Grid ColumnSpacing="16">
<Grid.ColumnDefinitions>
<ColumnDefinition Width="*" />
<ColumnDefinition Width="Auto" />
</Grid.ColumnDefinitions>
<StackPanel Grid.Column="0" VerticalAlignment="Center" Spacing="2">
<TextBlock Style="{StaticResource AkariCardTitle}" Text="Disable Preemption (NVIDIA)" />
<TextBlock
Style="{StaticResource AkariCardDescription}"
Text="Lower latency at the cost of stability on NVIDIA GPUs." />
</StackPanel>
<ToggleSwitch
MinWidth="0"
Grid.Column="1"
VerticalAlignment="Center"
IsEnabled="{Binding IsBusy, Mode=OneWay, Converter={StaticResource InvertedBool}}"
IsOn="{Binding DisablePreemption, Mode=TwoWay}" />
</Grid>
```
Reuse guidance: two columns (`*` + `Auto`, `ColumnSpacing="16"`); left text stack (`Spacing="2"`) holds title (`AkariCardTitle`) + 4 inline explanation lines (`AkariCardDescription`, `TextWrapping="Wrap"` — style already wraps) + `Requires restart` caption badge (`AkariCaption`); right `CheckBox IsChecked="{x:Bind IsSelected, Mode=TwoWay}"` (+ state label). Page root uses `{Binding}` (page-level `x:Bind` dies with WMC9999); `x:Bind` only inside `DataTemplate` with `x:DataType="...GamingCatalogRowItem"`.

**DataTemplate x:Bind pattern** (`GamingPage.xaml` lines 208-216):
```xml
<DataTemplate x:DataType="models:GamingButton">
<Button
Margin="4"
Command="{x:Bind RunToolCommand, Mode=OneTime}"
Content="{x:Bind Label, Mode=OneTime}"
IsEnabled="{x:Bind IsEnabled, Mode=OneWay}" />
</DataTemplate>
```

**Style catalog** (`src/AppTemplate.App/Views/Shared/PageStyles.xaml` lines 13-46): `AkariCard` (Border, card fill + stroke, `CornerRadius 8`, `Padding 16`), `AkariCardTitle` (Body + SemiBold + ellipsis), `AkariCardDescription` (Body + wrap + secondary fill), `AkariSectionHeader` (Subtitle + top margin), `AkariCaption` (Caption + wrap), `AkariPageMargin` (32). No hardcoded colors/fonts — every brush from `{ThemeResource ...}`.

**Top bar:** `AutoSuggestBox` (`PlaceholderText="Search gaming tweaks"`) + `Apply selected (N)` (`Style="{StaticResource AccentButtonStyle}"`, `IsEnabled` bound to selection-count > 0 && !IsBusy) + `Revert all changes` (default button style — revert is NOT destructive-styled per UI-SPEC color contract). No existing `AutoSuggestBox` usage in the codebase (grep: zero matches) — event/property names (`TextChanged` + `AutoSuggestionBoxTextChangeReason.UserInput`, `SuggestionChosen`, `QuerySubmitted`) must be verified against the WASDK 2.3.1 SDK contracts at plan time; fallback is `TextBox` + `ListView` popup (RESEARCH.md A1).

---

### `Views/GamingCatalogPage.xaml.cs` (NEW — code-behind)

**Analog:** `src/AppTemplate.App/Views/CheckPage.xaml.cs` (3-line code-behind)

```csharp
public sealed partial class CheckPage : Page
{
    /// <summary>Localized string accessor used by x:Bind function bindings.</summary>
    public LocalizedStrings Strings { get; }

    /// <summary>
    /// Typed view model. Page-level x:Bind needs a strongly-typed root, so the view model is
    /// exposed as a property rather than only being pushed into an untyped DataContext.
    /// </summary>
    public CheckViewModel ViewModel { get; }

    public CheckPage(CheckViewModel viewModel)
    {
        Strings = App.Services.GetRequiredService<LocalizedStrings>();
        ViewModel = viewModel;
        InitializeComponent();
        DataContext = ViewModel;
    }
}
```
Reuse guidance: identical shape with `GamingCatalogViewModel`. Copy the XML doc comments. `using Microsoft.Extensions.DependencyInjection;` needed for `GetRequiredService`.

---

### `App.xaml.cs` + `MainWindow.xaml.cs` (EDITS — registration)

**Analog:** in-place — `App.xaml.cs` lines 147-175, `MainWindow.xaml.cs` lines 82-96.

```csharp
// AkariToolbox view models (smallest-first registration; all transient).
builder.Services.AddTransient<SettingsViewModel>();
// ...
builder.Services.AddTransient<WindowsViewModel>();

// Pages (created through DI).
builder.Services.AddTransient<HomePage>();
// ...
builder.Services.AddTransient<SettingsPage>();
```
```csharp
public IReadOnlyList<NavigationItem> NavItems { get; } =
[
    new("Home", " E80F", typeof(HomePage)),
    new("Akari OS Tweaks", " E713", typeof(AkariTweaksPage)),
    new("Gaming Tweaks", " E7FC", typeof(GamingPage)),
    // ...
];
```
Reuse guidance: add `AddTransient<GamingCatalogViewModel>()` + `AddTransient<GamingCatalogPage>()` (transient, beside existing registrations); one `new("Gaming Catalog", "<glyph>", typeof(GamingCatalogPage))` entry adjacent to "Gaming Tweaks" (exact slot is planner's choice; Segoe Fluent Icons `FontIcon` string like neighbors). **No `.csproj` edit** — wildcard globs cover new files. `verify-pages.ps1` enumerates nav items dynamically (`FindAll`, prints count — no hardcoded 13), so no script edit is expected; planner confirms at plan time.

---

### `Tweaks/GamingActions.cs` (EXTEND — Game Mode / DVR natives)

**Analog:** in-place — `ReadPreemption`/`SetPreemption` (lines 35-61) is the symmetry template:
```csharp
public static bool ReadPreemption() =>
    RegRead.Dword(Scheduler, "EnablePreemption") is 0 ||
    RegRead.Dword(Nvlddmkm, "DisablePreemption") is 1;

public static void SetPreemption(bool disable)
{
    if (disable)
    {
        Dword(Scheduler, "EnablePreemption", 0);
        // ... 5 more nvlddmkm values ...
        AkariToolState.Save("DisablePreemption");
    }
    else
    {
        // ... symmetric restore ...
        AkariToolState.Clear("DisablePreemption");
    }
}
```
Private write helpers at lines 22-29 (`Dword`/`Sz`/`Bin` → `TweakAction.Reg*...Apply()`). Reuse guidance: `ReadGameMode`/`SetGameMode` + `ReadGameDvr`/`SetGameDvr` follow the same read-probe + symmetric-set + `AkariToolState.Save/Clear` shape, keyed on the `fso` key set (`AkariTweakCatalog.cs:297-329`). Note the elevated-HKCU caveat: the app runs elevated so HKCU is the elevation account's hive — GameBar/GameDVR keys under HKCU may need `AkariToolState.OpenRealHkcu` (lines 55-63, interactive-user SID via explorer token) rather than plain `TweakAction.RegDword`. Planner must decide per key.

**Anti-precedent (why the journal exists):** `RevertNetworkOptimization` (lines 179-184) is `netsh winsock reset` — does NOT restore the ~57 NIC values `ApplyNetworkOptimization` wrote (lines 144-177, `*SpeedDuplex`-gated per-adapter loop + PowerShell binding-disable + 10 netsh globals). Bespoke inverse methods are the debt the journal retires; do not add new `Revert*` methods.

---

### `Tests/GamingCatalogTests.cs` (NEW — test)

**Analog:** `src/AppTemplate.Tests/SettingsServiceTests.cs` (xUnit fact shape)

```csharp
using AppTemplate.Framework.Services;
using Xunit;

namespace AppTemplate.Tests;

public class SettingsServiceTests
{
    [Fact]
    public async Task MemorySettingsStorage_round_trips_values()
    {
        var storage = new MemorySettingsStorage();
        await storage.WriteAsync("key", "value");
        Assert.Equal("value", await storage.ReadAsync("key"));
    }
```
Reuse guidance: test-method naming is `UnitUnderTest_condition_expectedOutcome` (snake_case segments). File is `<Area>Tests.cs` in `src/AppTemplate.Tests/`; doubles are `private sealed` helpers at the bottom of the file. Cover: catalog integrity (ids unique, shared IDs resolve, 4 explanation lines non-empty, no registry paths in copy), suggest ranking (prefix > substring, cap 8, title-only, 1-char trigger, empty query = no-filter), journal round-trip on HKCU scratch keys (capture → mutate → revert → sentinel restored, incl. absent→delete), bulk semantics with fake entries (stop-at-first-failure leaves completed-prefix journal; revert replays reverse). All UI-free — runnable under `dotnet test` (UI types throw under testhost). Quick filter: `dotnet test src/AppTemplate.Tests --filter GamingCatalog`.

---

## Shared Patterns

### Run-site orchestration (applies to: ViewModel apply/revert commands)
**Sources:** `TweaksViewModel.ApplyRowAsync` (`TweaksViewModel.cs:158-186`), `GamingViewModel.ApplyToggle` (`GamingViewModel.cs:202-229`), `CheckViewModel.RunAsync` (`CheckViewModel.cs:86-109`)
Shape: `if (IsBusy) return;` → `IsBusy=true` + row fan-out → `_status.Start(title)` → `await Task.Run(work)` → InfoBar success/failure → `finally { unlock; IsBusy=false; _status.Complete(); }`. Every VM run site pairs `Start/Complete` with an InfoBar report. Long work NEVER on the UI thread.

### Progress footer (applies to: bulk apply progress, D-13)
**Source:** `src/AppTemplate.Framework/Services/IStatusService.cs` (`StatusService`, lines 22-69)
`Start(message)` is `Interlocked` ref-counted (overlapping runs don't flicker); `Report(double value, string? message)` flips to determinate (`IsIndeterminate=false`); `Complete()` decrements, clears at zero. Shell binds it in `MainWindow.xaml:79-101` (`StatusText(...)`/`StatusFooterVisibility(...)` x:Bind function bindings + `ProgressBar Value`/`IsIndeterminate`). New code uses `Report(done/total, ...)` per row; single-row runs stay indeterminate.

### Restore-point offer dialog (applies to: pre-apply offer, D-15)
**Source:** `src/AppTemplate.Framework/Services/IDialogService.cs` (`IDialogService.ShowAsync`, lines 12-18; `DialogService` gate lines 98-119)
```csharp
Task<ContentDialogResult> ShowAsync(string title, string content,
    string primaryText = "OK", string? secondaryText = null, string? cancelText = null);
```
VMs never touch UI types — dialogs go through `IDialogService` (`Func<XamlRoot?>` provider registered in `App.BuildHost`, `App.xaml.cs:180`; `SemaphoreSlim(1,1)` serializes overlapping requests). Offer copy: primary `"Create restore point and continue"`, secondary `"Skip and apply"`, cancel `null` (UI-SPEC copy contract). Primary → `await Task.Run(RestorePointActions.CreateRestorePoint)`; failure → warning InfoBar → **continue** (offered-not-forced, D-15).

### Restore-point creation (applies to: offer primary path)
**Source:** `src/AppTemplate.App/Tweaks/RestorePointActions.cs` (lines 17-50)
WMI `SystemRestore` class (`Enable` + `CreateRestorePoint` with `RestorePointType=12 MODIFY_SETTINGS`, `EventType=100 BEGIN_SYSTEM_CHANGE`); throws readable `InvalidOperationException` on disabled protection (caught at line 45-49 and re-wrapped). `using` disposes all WMI objects (`ManagementClass`, params, result). Caller catches and downgrades to warning — never blocks apply.

### `.reg` import for shared rows (applies to: `FromSharedRow` apply path)
**Source:** `src/AppTemplate.App/Tweaks/NativeOps.cs` (`ImportRegContent`, lines 896-907)
Writes UTF-16 LE with BOM to `%SystemRoot%\Temp\akaritoolbox_<guid>.reg`, imports via `regedit.exe /S` (waits), deletes temp. `ControlPanelRows.ApplyRow(row, optimize)` (line 44-45) picks `Optimize` vs `Default` body. New code calls `ApplyRow`, never re-implements import. Note the `?`→`$` swap param for Control Panel script quoting.

### Error handling (applies to: all VM run sites + capture/revert)
Per-command catch: `catch (Exception ex) { _infoBar.Show("<Title> failed", ex.Message); }` with `finally { IsBusy=false; _status.Complete(); }`. Probes wrapped in `Safe`/`SafeNullable` returning fallbacks. Revert continues past single-row failures with per-row ok/fail collection (intentional asymmetry vs apply's stop-at-first-failure). Logging/file paths never throw (`FileLoggerProvider.Write`/`Prune` swallow; corrupt journal → empty).

### MVVM + XAML binding (applies to: new VM + page)
`partial` class + `[ObservableProperty]` backing fields (`_camelCase` → `PascalCase`) + `[RelayCommand]` private methods + `On<Prop>Changed` partials with `if (_loading) return;` guard. Page root `{Binding}` + `DataContext = ViewModel`; `x:Bind` only in `DataTemplate` with `x:DataType`; item commands on the item class (no `RelativeSource AncestorType`). `ToggleSwitch`/`CheckBox` rows set `MinWidth="0"`. Styles from `PageStyles.xaml` only; system theme resources only; `Spacing`/`ColumnSpacing` multiples of 4 (page `Padding 32`, card gaps `16`, text-stack `2`).

## No Analog Found

| File | Role | Data Flow | Reason |
|---|---|---|---|
| Search/suggest UI (`AutoSuggestBox`) | component | request-response | Zero `AutoSuggestBox` usage in the codebase (grep: no matches). Planner uses RESEARCH.md §3 + UI-SPEC Interaction Contract; verify event/property names against WASDK 2.3.1 SDK contracts at plan time. |
| Snapshot journal replay semantics | service | file-I/O + CRUD | No per-value snapshot/replay exists (`RevertNetworkOptimization` is the anti-precedent). Schema + capture/revert protocol come from RESEARCH.md §2; only the persistence mechanics (atomic save, corrupt-degrade) have analogs. |

## Metadata

**Analog search scope:** `src/AppTemplate.App/ViewModels/`, `src/AppTemplate.App/Tweaks/`, `src/AppTemplate.App/Views/`, `src/AppTemplate.App/Views/Shared/`, `src/AppTemplate.Framework/Services/`, `src/AppTemplate.Tests/`, `src/AppTemplate.App/App.xaml.cs`, `src/AppTemplate.App/MainWindow.xaml(.cs)`
**Files scanned:** ~25 (stopped at strong matches per early-stopping rule)
**Git-tracking gate:** all 23 named analog paths verified via `git ls-files` (non-empty = tracked); no mirror paths emitted
**Pattern extraction date:** 2026-10-03

## PATTERN MAPPING COMPLETE

**Phase:** 1 - Trusted Catalog Foundation
**Files classified:** 9 (6 new, 2 edits, 1 extend)
**Analogs found:** 9 / 9 (2 partial — search UI and journal semantics lean on RESEARCH.md)

### Coverage
- Files with exact analog: 5 (ViewModel, page XAML, code-behind, GamingActions extend, both registration edits)
- Files with role-match analog: 2 (GamingCatalog descriptor, GamingCatalogTests)
- Files with data-flow-match analog: 1 (GamingJournal persistence mechanics)
- Files with no analog: 2 partial (AutoSuggestBox UI, journal replay semantics — RESEARCH.md patterns apply)

### Key Patterns Identified
- New page registers as transient VM + page in `App.BuildHost` with one `NavItems` entry; no `.csproj` edit (wildcard globs); `verify-pages.ps1` enumerates dynamically
- Bulk runner copies `TweaksViewModel.ApplyRowAsync` (IsBusy guard + `SetRowsBusy` fan-out + `StatusService Start/Report/Complete` + `Task.Run` + InfoBar), with D-14 stop-at-first-failure overriding ARCHITECTURE.md continue-on-error
- Descriptor catalog mirrors `AkariTweakCatalog.Toggle` factory; shared rows cross-referenced by ID via `ControlPanelRows.All`, never duplicated; generated file never touched
- Journal is a dedicated versioned JSON file with atomic temp+move writes and corrupt→empty degrade; capture distinguishes absent (`DeleteValue` on revert) from zero via `RegRead.HasValue`; footprint drives completeness, never the probe
- Page root `{Binding}` + `DataContext`; `x:Bind` only in `DataTemplate`; item state on the item class; `CheckBox IsSelected` TwoWay with sticky ticks; `AkariCard`/`AkariSectionHeader`/`AkariCaption` styles only

### File Created
`.planning/phases/01-trusted-catalog-foundation/01-PATTERNS.md`

### Ready for Planning
Pattern mapping complete. Planner can now reference analog patterns in PLAN.md files.
