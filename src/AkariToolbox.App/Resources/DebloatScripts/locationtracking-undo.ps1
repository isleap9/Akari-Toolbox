# Enable Location Tracking - Undo
if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location") {
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\location" -Name "Value" -Value "Allow" -Force -ErrorAction SilentlyContinue
}
if (Test-Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}") {
    Set-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Sensor\Overrides\{BFA794E4-F964-4FDB-90F6-51056BFE4B44}" -Name "SensorPermissionState" -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue
}
if (Test-Path "HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration") {
    Set-ItemProperty -Path "HKLM:\SYSTEM\CurrentControlSet\Services\lfsvc\Service\Configuration" -Name "Status" -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue
}
if (Test-Path "HKLM:\SYSTEM\Maps") {
    Set-ItemProperty -Path "HKLM:\SYSTEM\Maps" -Name "AutoUpdateEnabled" -Value 1 -Type DWord -Force -ErrorAction SilentlyContinue
}
Write-Host "Location tracking enabled."
