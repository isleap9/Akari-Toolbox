# SCRIPT CHECK INTERNET
if (!(Test-Connection -ComputerName "8.8.8.8" -Count 1 -Quiet -ErrorAction SilentlyContinue)) {
Write-Host "Internet Connection Required`n" -ForegroundColor Red
exit 1
}

# SCRIPT SILENT
$progresspreference = 'silentlycontinue'

Clear-Host

Write-Host "Edge Settings: Default..."

# remove ublock origin
# remove edge policies
cmd /c "reg delete `"HKLM\SOFTWARE\Policies\Microsoft\Edge`" /f >nul 2>&1"

# stop edge running
Stop-Process -Name "msedge" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# reset edge settings
Start-Process "msedge.exe" -ArgumentList "--restore-last-session --disable-extensions"
Start-Sleep -Seconds 2

# stop edge running
Stop-Process -Name "msedge" -Force -ErrorAction SilentlyContinue

# download edge installer
IWR "https://github.com/FR33THYFR33THY/Ultimate-Files/raw/refs/heads/main/edge.exe" -OutFile "$env:SystemRoot\Temp\edge.exe"

# start edge installer
Start-Process "$env:SystemRoot\Temp\edge.exe"

exit
