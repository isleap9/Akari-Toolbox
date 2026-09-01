# Enable Storage Sense - Undo
$path = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy"
If (!(Test-Path $path)) { New-Item -Path $path -Force | Out-Null }
Set-ItemProperty -Path $path -Name "01" -Value 1 -Type DWord -Force

Write-Host "Storage Sense enabled."
