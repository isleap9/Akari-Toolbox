# Parity check: every Text= and Content= string in each WPF page must appear in its WinUI port.
#
# This is the mechanical half of the 1:1 parity requirement (row titles, descriptions and
# button captions). Behavioural parity is covered by verify-pages.ps1.
param(
    [string]$WpfRoot = 'G:\Tools\akari-tool-wpf-lepo-8\src\AkariBase\Views\Pages',
    [string]$WinRoot = 'G:\Tools\akari-tool-winui3-mvvm\src\AppTemplate.App\Views'
)

$ErrorActionPreference = 'Stop'

function Get-Texts([string]$path) {
    $t = [IO.File]::ReadAllText($path)
    $rx = [regex]'(?s)\b(?:Text|Content)="([^"]*)"'
    $out = New-Object System.Collections.Generic.List[string]
    foreach ($m in $rx.Matches($t)) {
        $v = $m.Groups[1].Value
        $v = $v -replace '&amp;', '&' -replace '&lt;', '<' -replace '&gt;', '>' -replace '&quot;', '"' -replace '&apos;', "'"
        if ($v.Trim() -and -not $v.Trim().StartsWith('{')) { $out.Add($v.Trim()) }
        # "{Binding X}" is markup, not copy: the WinUI port uses {x:Bind X} inside DataTemplates,
        # so these are deliberately excluded from the comparison.
    }
    return $out
}

$totalMissing = 0
foreach ($wpf in Get-ChildItem $wpfRoot -Filter '*Page.xaml' | Sort-Object Name) {
    $win = Join-Path $WinRoot $wpf.Name
    if (-not (Test-Path $win)) {
        Write-Host ("{0,-22} NO WINUI FILE" -f $wpf.Name)
        continue
    }

    $expected = Get-Texts $wpf.FullName
    $actual = Get-Texts $win
    $actualSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach ($a in $actual) { [void]$actualSet.Add($a) }

    $missing = @($expected | Where-Object { -not $actualSet.Contains($_) } | Select-Object -Unique)

    if ($missing.Count -eq 0) {
        Write-Host ("{0,-22} OK   {1,3} strings" -f $wpf.Name, $expected.Count)
    } else {
        $totalMissing += $missing.Count
        Write-Host ("{0,-22} MISS {1,3}/{2,3}" -f $wpf.Name, $missing.Count, $expected.Count)
        foreach ($m in $missing) { Write-Host "      - $m" }
    }
}

Write-Host ''
Write-Host "Total missing strings: $totalMissing"