# Screenshots each page in NavigationView order so the rendered layout can be eyeballed.
param(
    [string]$OutDir = "$env:TEMP\akari-shots",
    [string]$Exe = 'G:\Tools\akari-tool-winui3-mvvm\src\AppTemplate.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\AkariBase.App.exe'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, WindowsBase
Add-Type -AssemblyName System.Drawing

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

Get-Process -Name 'AkariBase.App' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 800
Start-Process -FilePath $Exe | Out-Null
Start-Sleep -Seconds 12

$proc = Get-Process -Name 'AkariBase.App' -ErrorAction Stop
$root = [System.Windows.Automation.AutomationElement]::RootElement
$pidCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
$win = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $pidCond)

$listCond = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
    [System.Windows.Automation.ControlType]::ListItem)

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Win32 {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

$h = [IntPtr]$proc.MainWindowHandle
[Win32]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 500

$rect = New-Object Win32+RECT
[Win32]::GetWindowRect($h, [ref]$rect) | Out-Null
$w = $rect.Right - $rect.Left
$ht = $rect.Bottom - $rect.Top
Write-Host "Window rect: $($rect.Left),$($rect.Top) ${w}x${ht}"

$items = $win.FindAll([System.Windows.Automation.TreeScope]::Descendants, $listCond)
$safe = @('Home','AkariOSTweaks','GamingTweaks','1Check','2Refresh','3Setup','4Installers',
          '5Graphics','6Windows','7Hardware','8Advanced','IndividualTweaks','Settings')

for ($i = 0; $i -lt $items.Count; $i++) {
    $item = $items.Item($i)
    $sel = $item.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $sel.Select()
    Start-Sleep -Milliseconds 1200

    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $ht)))
    $path = Join-Path $OutDir ("{0:d2}-{1}.png" -f $i, $safe[$i])
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    Write-Host "saved $path"
}