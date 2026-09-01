# SCRIPT CHECK INTERNET
if (!(Test-Connection -ComputerName "8.8.8.8" -Count 1 -Quiet -ErrorAction SilentlyContinue)) {
Write-Host "Internet Connection Required`n" -ForegroundColor Red
exit 1
}

# SCRIPT SILENT
$progresspreference = 'silentlycontinue'

# ALLOW PASSWORD SIGN IN
cmd /c "reg add `"HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\PasswordLess\Device`" /v `"DevicePasswordLessBuildVersion`" /t REG_DWORD /d `"0`" /f >nul 2>&1"

Clear-Host

Write-Host "Installing: All UWP Apps. Please wait..."

# install all uwp apps
Get-AppxPackage -AllUsers | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register -ErrorAction SilentlyContinue "$($_.InstallLocation)\AppXManifest.xml"} 2>$null

exit
