$progresspreference = 'silentlycontinue'

Clear-Host

Write-Host "Copilot: Default..."

# install copilot
Get-AppXPackage -AllUsers | Where-Object {
$_.Name -like '*Copilot*'
} | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register -ErrorAction SilentlyContinue "$($_.InstallLocation)\AppXManifest.xml"}

# copilot regedit
cmd /c "reg delete `"HKCU\Software\Policies\Microsoft\Windows\WindowsCopilot`" /f >nul 2>&1"
cmd /c "reg delete `"HKLM\SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot`" /f >nul 2>&1"

exit
