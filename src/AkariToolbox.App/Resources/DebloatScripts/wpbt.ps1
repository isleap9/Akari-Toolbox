# Disable Windows Platform Binary Table (WPBT)
$path = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager"
Set-ItemProperty -Path $path -Name "DisableWpbtExecution" -Value 1 -Type DWord -Force
Write-Host "WPBT disabled."