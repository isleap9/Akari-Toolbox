# Storage Sense - Disable
# https://winutil.christitus.com/dev/tweaks/z--advanced-tweaks---caution/storage/

$path = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy"
If (!(Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
Set-ItemProperty -Path $path -Name "01" -Value 0 -Type DWord -Force

Write-Host "Storage Sense disabled."