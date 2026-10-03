# Port WindowsPage.xaml from the WPF reference to WinUI 3 by mechanical substitution.
#
# The WPF page is 1080 lines of one repeated row shape (section header / card / row title /
# description / separator / toggle or button), so it translates 1:1 with no hand rewriting.
# The reference file is read-only — nothing is written back to the WPF tree.
param(
    [string]$Source = 'G:\Tools\akari-tool-wpf-lepo-8\src\AkariBase\Views\Pages\WindowsPage.xaml',
    [string]$Target = 'G:\Tools\akari-tool-winui3-mvvm\src\AppTemplate.App\Views\WindowsPage.xaml'
)

$ErrorActionPreference = 'Stop'
$nl = "`r`n"

$text = [IO.File]::ReadAllText($Source)

# The original Page open tag carries WPF-only design attributes and namespaces; rebuild it.
$bodyStart = $text.IndexOf('    <ScrollViewer')
if ($bodyStart -lt 0) { throw 'ScrollViewer not found in the reference page.' }
$body = $text.Substring($bodyStart)

# ── Page header ──────────────────────────────────────────────────────────
$header = @'
<Page
    x:Class="AppTemplate.App.Views.WindowsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:AkariBase.ViewModels">

    <!--
        6 · Windows: Appearance, Debloat & Privacy, Bloatware, Bloatware Checks,
        Settings & Sound, Performance, Quick Settings and Security & Maintenance.

        WPF-UI substitutions: ui:Card -> AkariCard Border, ui:Button -> AkariTileButton,
        ui:ToggleSwitch -> ToggleSwitch (IsChecked -> IsOn), <Separator> -> a 1px Rectangle
        filled with the card stroke brush, and the DynamicResource brushes -> the matching
        Akari ThemeResource brushes. Every row title, description and button caption is copied
        verbatim from the WPF page; the page's own controls use classic {Binding} because
        page-level x:Bind cannot resolve its root type in this project (WMC9999).
    -->
'@ -replace "`n", "`r`n"

# ── Section header: FontSize 16 SemiBold ─────────────────────────────────
$body = $body -replace '(?s)<TextBlock\s+Margin="0,24,0,0"\s+FontWeight="SemiBold"\s+FontSize="16"\s+Text="([^"]*)"\s*/>',
                     '<TextBlock Style="{StaticResource AkariSectionHeader}" Text="$1" />'

# ── Row description: secondary, wrapped ─────────────────────────────────
$body = $body -replace '(?s)<TextBlock\s+Margin="0,3,0,0"\s+Foreground="\{DynamicResource TextFillColorSecondaryBrush\}"\s+Text="([^"]*)"\s+TextWrapping="Wrap"\s*/>',
                     ('<TextBlock' + $nl + '                                Style="{StaticResource AkariCardDescription}"' + $nl +
                      '                                Text="$1" />')

# ── Row title: SemiBold ──────────────────────────────────────────────────
$body = $body -replace '<TextBlock FontWeight="SemiBold" Text="([^"]*)" />',
                     '<TextBlock Style="{StaticResource AkariCardTitle}" Text="$1" />'

# ── Cards ────────────────────────────────────────────────────────────────
$body = $body -replace '<ui:Card Margin="0,12,0,0" Padding="16">',
                     '<Border Margin="0,12,0,0" Padding="16" Style="{StaticResource AkariCard}">'
$body = $body -replace '</ui:Card>', '</Border>'

# ── Separators ───────────────────────────────────────────────────────────
$body = $body -replace '<Separator Margin="0,12" />',
                     '<Rectangle Height="1" Margin="0,12" Fill="{ThemeResource AkariCardStrokeBrush}" />'

# ── Buttons / toggle switches ────────────────────────────────────────────
$body = $body -replace '<ui:Button', '<Button'
$body = $body -replace '<ui:ToggleSwitch', '<ToggleSwitch'
$body = $body -replace '</ui:ToggleSwitch>', '</ToggleSwitch>'
$body = $body -replace 'IsChecked="\{Binding ViewModel\.', 'IsOn="{Binding '

# ── Page padding ─────────────────────────────────────────────────────────
$body = $body -replace '<StackPanel Margin="36,28,40,80">',
                     '<StackPanel Margin="{StaticResource AkariPageMargin}">'

# ── Brushes and bindings ─────────────────────────────────────────────────
$body = $body -replace 'Foreground="\{DynamicResource TextFillColorSecondaryBrush\}"',
                     'Foreground="{ThemeResource AkariTextSecondaryBrush}"'
$body = $body -replace 'Foreground="\{DynamicResource TextFillColorPrimaryBrush\}"',
                     'Foreground="{ThemeResource AkariTextPrimaryBrush}"'
$body = $body -replace '\{StaticResource InverseBool\}', '{StaticResource InvertedBool}'
$body = $body -replace '\{Binding ViewModel\.', '{Binding '

$out = $header.TrimEnd() + $nl + $body
[IO.File]::WriteAllText($Target, $out, (New-Object Text.UTF8Encoding($false)))

Write-Host "Wrote $Target ($($out.Length) chars)"
foreach ($leftover in @('ui:', 'ViewModel.', 'DynamicResource', 'Separator', 'InverseBool', 'IsChecked', 'd:Design', 'mc:Ignorable')) {
    $hits = ([regex]::Matches($out, [regex]::Escape($leftover))).Count
    Write-Host ("  leftover '{0}': {1}" -f $leftover, $hits)
}