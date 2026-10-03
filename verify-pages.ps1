# Clicks every NavigationView item in order and reports how much text each page rendered.
# A page that still shows only its skeleton has a single TextBlock and no tiles.
param(
    [string]$Exe = 'G:\Tools\akari-tool-winui3-mvvm\src\AppTemplate.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\AkariBase.App.exe'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, WindowsBase

Get-Process -Name 'AkariBase.App' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800
Start-Process -FilePath $Exe | Out-Null
Start-Sleep -Seconds 12

$proc = Get-Process -Name 'AkariBase.App' -ErrorAction Stop
$root = [System.Windows.Automation.AutomationElement]::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
if (-not $win) { throw "No window for pid $($proc.Id)" }
Write-Host "Window: '$($win.Current.Name)'"

$navCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ListItem)
$items = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $navCond)

Write-Host ("Nav items found: {0}" -f $items.Count)
Write-Host ''
Write-Host ('{0,-4} {1,-22} {2,7} {3,7} {4,7}' -f '#', 'Nav item', 'Texts', 'Tiles', 'Toggles')
Write-Host ('-' * 52)

for ($i = 0; $i -lt $items.Count; $i++) {
    $item = $items.Item($i)
    $name = $item.Current.Name

    try {
        $selPattern = $item.GetCurrentPattern(
            [System.Windows.Automation.SelectionItemPattern]::Pattern)
        $selPattern.Select()
    } catch {
        Write-Host ("{0,-4} {1,-22} SELECT FAILED: {2}" -f $i, $name, $_.Exception.Message)
        continue
    }
    Start-Sleep -Milliseconds 900

    $txtCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Text)
    $btnCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)
    $tglCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::CheckBox)

    $texts = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $txtCond)
    $btns  = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $btnCond)
    $tgls  = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $tglCond)

    # Every visible description line carries wrapped body copy; count non-empty texts only.
    $nonEmpty = 0
    foreach ($t in $texts) { if ($t.Current.Name.Trim()) { $nonEmpty++ } }

    Write-Host ('{0,-4} {1,-22} {2,7} {3,7} {4,7}' -f $i, $name, $nonEmpty, $btns.Count, $tgls.Count)
}

Write-Host ''
if (Get-Process -Name 'AkariBase.App' -ErrorAction SilentlyContinue) {
    Write-Host 'Process still alive after the full sweep: OK'
} else {
    Write-Host 'PROCESS DIED'
}