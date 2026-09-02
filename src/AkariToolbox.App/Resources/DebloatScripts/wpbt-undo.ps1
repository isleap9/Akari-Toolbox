# Enable WPBT (Windows Platform Binary Table) - Undo
$path = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager"
Remove-ItemProperty -Path $path -Name "DisableWpbtExecution" -Force -ErrorAction SilentlyContinue
Write-Host "WPBT (Windows Platform Binary Table) enabled."
