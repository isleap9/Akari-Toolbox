# AppTemplate — WinUI 3 MVVM starter

A minimal, polished starting point for new WinUI 3 desktop apps: a Home page and a Settings page
(with theme + language switching), a Mica backdrop for the Windows 11 look, and a reusable
framework class library with an xUnit test suite. No demo pages, no sample data.

## Solution layout

```
AppTemplate.slnx
├── src/
│   ├── AppTemplate.Framework   Reusable class library (no app-specific code)
│   ├── AppTemplate.App         App shell (Home + Settings), wiring via DI
│   └── AppTemplate.Tests       xUnit tests for the framework
├── Directory.Packages.props    Central package management (versions in one place)
├── Directory.Build.props
├── global.json                 .NET SDK pin
└── nuget.config
```

## Tech stack

- .NET 10 (`net10.0-windows10.0.26100.0`), Windows App SDK 2.3.1, WinUI 3
- CommunityToolkit.Mvvm 8.4.2 (source generators: `[ObservableProperty]`, `[RelayCommand]`)
- Microsoft.Extensions: DependencyInjection, Hosting, Logging
- Microsoft.Xaml.Behaviors.WinUI.Managed 3.0.1
- Tests: xUnit 2.9.3, Moq 4.20.72, Microsoft.NET.Test.Sdk 17.14.1

The app is **unpackaged** (`WindowsPackageType=None`) and **self-contained**
(`WindowsAppSDKSelfContained=true`), so no Windows App Runtime is required on the machine.

## Build & run

```powershell
# Build the whole solution (all 3 projects)
dotnet build AppTemplate.slnx -c Debug

# Run the tests (90 tests)
dotnet test src/AppTemplate.Tests/AppTemplate.Tests.csproj -c Debug

# Run the app
dotnet run --project src/AppTemplate.App -c Debug   # or launch the built exe
```

The app pins `<PlatformTarget>x64</PlatformTarget>` and `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`
so the self-contained WinAppSDK targets resolve a native platform regardless of solution platform.

## Starting a new app

1. Copy the repository and rename `AppTemplate` everywhere (folders, `.csproj`, `.slnx`,
   namespaces, assembly names, `App.AppName`, the settings-folder name in `App.xaml.cs`).
2. Add pages under `src/AppTemplate.App/Views` with matching view models, register them in
   `App.xaml.cs`, and add entries to `NavItems` in `MainWindow.xaml.cs`.
3. Keep app-independent code in the framework library and add tests in `AppTemplate.Tests`.

## Shell

`MainWindow` wires the custom title bar (drag region + back button), a `NavigationView` with a
pinned pane toggle (the pancake at the top of the sidebar collapses/expands the pane), a shared
`InfoBar`, and a **Mica** backdrop (`SystemBackdrop = new MicaBackdrop()`) that follows the theme.
Launching always opens the **Home** tab.

## Framework features (`src/AppTemplate.Framework`)

| Folder / file | Contents |
| --- | --- |
| `ViewModels/` | `ViewModelBase`, `ValidatableViewModelBase` (ObservableValidator wrapper with `GetError`, `CanSubmit`) |
| `Navigation/` | `FrameNavigationService`, `INavigationService`, `INavigationAware`, `NavigationEntry` |
| `Services/` | `SettingsService` + `FileSettingsStorage`; `ThemeService`, `CultureService`; `IDialogService`, `IFilePickerService`, `IInfoBarService`, `IWindowService` |
| `Messaging/` | Thin wrappers over `WeakReferenceMessenger` (`IMessage`, message classes) |
| `Collections/` | `RangeObservableCollection`, `ObservableGroupCollection`, `IncrementalLoadingCollection` |
| `Converters/` | Reusable value converters for WinUI bindings (incl. `EnumToStringConverter` for friendly enum labels) |
| `Behaviors/` | Reusable XAML behaviors |
| `Logging/` | `FileLoggerProvider` — rolling file logger (daily files, size cap, retention) |
| `Threading/` | Dispatcher helpers |
| `ObjectExtensions.cs` | `ChangeType`-style object extension used by settings |
| `ServiceCollectionExtensions.cs` | `AddMvvmFramework()` DI registration |

## App notes

- Settings live in `%LOCALAPPDATA%\AppTemplate\settings.json`; the theme (System / Light / Dark)
  and language are picked with ComboBoxes and apply immediately.
- `ILogger` output is written to `%LOCALAPPDATA%\AppTemplate\logs\` (daily rolling files,
  ~1 MB cap, newest 10 kept); unhandled exceptions are logged there and shown in a friendly
  dialog.
- The app is wired through `Microsoft.Extensions.Hosting` in `App.xaml.cs`; pages are created
  through the DI container by `FrameNavigationService`, and dialogs are abstracted behind
  `IDialogService` so the framework and its tests never depend on a concrete window.

## Notes

- **UI types can't be instantiated in testhost.** `new Page()` / `new Frame()` throw
  `COMException` in `dotnet test` even when the WinAppSDK runtime is bootstrapped. Tests cover
  only framework logic that doesn't require a XAML object; UI behavior is exercised by the app.
- `dotnet build` errors like `WMC9999` can hide the real issue; prefer building the solution
  and reading the full XamlCompiler diagnostics rather than stopping at the first error line.
