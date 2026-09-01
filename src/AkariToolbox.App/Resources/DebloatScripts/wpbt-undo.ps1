# Enable WPBT (Windows Platform Binary Table) - Undo
# Remove the registry block on WPBT
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\WPBT"
If (Test-Path $regPath) {
    Remove-ItemProperty -Path $regPath -Name "Start" -Force -ErrorAction SilentlyContinue
}
Set-Service -Name "WPBT" -StartupType Automatic -ErrorAction SilentlyContinue
Start-Service -Name "WPBT" -ErrorAction SilentlyContinue
Write-Host "WPBT (Windows Platform Binary Table) enabled."
