        # SCRIPT RUN AS ADMIN
        If (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator"))
        {Start-Process PowerShell.exe -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -File `"{0}`"" -f $PSCommandPath) -Verb RunAs
        Exit}
        $Host.UI.RawUI.WindowTitle = $myInvocation.MyCommand.Definition + " (Administrator)"
        $Host.UI.RawUI.BackgroundColor = "Black"
        $Host.PrivateData.ProgressBackgroundColor = "Black"
        $Host.PrivateData.ProgressForegroundColor = "White"
        Clear-Host

        # SCRIPT CHECK INTERNET
        if (!(Test-Connection -ComputerName "8.8.8.8" -Count 1 -Quiet -ErrorAction SilentlyContinue)) {
        Write-Host "Internet Connection Required`n" -ForegroundColor Red
        Pause
        exit
        }

        # SCRIPT SILENT
        $progresspreference = 'silentlycontinue'

# download 7zip
IWR "https://github.com/FR33THYFR33THY/Ultimate-Files/raw/refs/heads/main/7zip.exe" -OutFile "$env:SystemRoot\Temp\7zip.exe"

# install 7zip
Start-Process -Wait "$env:SystemRoot\Temp\7zip.exe" -ArgumentList "/S"

# set config for 7zip
cmd /c "reg add `"HKEY_CURRENT_USER\Software\7-Zip\Options`" /v `"ContextMenu`" /t REG_DWORD /d `"259`" /f >nul 2>&1"
cmd /c "reg add `"HKEY_CURRENT_USER\Software\7-Zip\Options`" /v `"CascadedMenu`" /t REG_DWORD /d `"0`" /f >nul 2>&1"

# cleaner 7zip start menu shortcut path
Move-Item -Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\7-Zip\7-Zip File Manager.lnk" -Destination "$env:ProgramData\Microsoft\Windows\Start Menu\Programs" -Force -ErrorAction SilentlyContinue | Out-Null
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\7-Zip" -Recurse -Force -ErrorAction SilentlyContinue | Out-Null

        Clear-Host

        Write-Host "DOWNLOAD AMD GPU DRIVER`n" -ForegroundColor Yellow
		## explorer "https://www.amd.com/en/support/download/drivers.html"
		## C:\Program Files\AMD\CNext\CNext\RadeonSoftware.exe

# download driver
Start-Sleep -Seconds 5
Start-Process "https://www.amd.com/en/support/download/drivers.html"
Pause
Clear-Host

        Write-Host ""
        Write-Host "SELECT DOWNLOADED DRIVER`n" -ForegroundColor Yellow

# select driver
Start-Sleep -Seconds 5
Add-Type -AssemblyName System.Windows.Forms
$Dialog = New-Object System.Windows.Forms.OpenFileDialog
$Dialog.Filter = "All Files (*.*)|*.*"
$Dialog.ShowDialog() | Out-Null
$InstallFile = $Dialog.FileName

        Write-Host "DEBLOATING DRIVER`n"

# extract driver with 7zip
& "$env:SystemDrive\Program Files\7-Zip\7z.exe" x "$InstallFile" -o"$env:SystemRoot\Temp\amddriver" -y | Out-Null

# edit xml files, set enabled & hidden to false
$xmlFiles = @(
"$env:SystemRoot\Temp\amddriver\Config\AMDAUEPInstaller.xml"
"$env:SystemRoot\Temp\amddriver\Config\AMDCOMPUTE.xml"
"$env:SystemRoot\Temp\amddriver\Config\AMDLinkDriverUpdate.xml"
"$env:SystemRoot\Temp\amddriver\Config\AMDRELAUNCHER.xml"
"$env:SystemRoot\Temp\amddriver\Config\AMDScoSupportTypeUpdate.xml"
"$env:SystemRoot\Temp\amddriver\Config\AMDUpdater.xml"
"$env:SystemRoot\Temp\amddriver\Config\AMDUWPLauncher.xml"
"$env:SystemRoot\Temp\amddriver\Config\EnableWindowsDriverSearch.xml"
"$env:SystemRoot\Temp\amddriver\Config\InstallUEP.xml"
"$env:SystemRoot\Temp\amddriver\Config\ModifyLinkUpdate.xml"
)
foreach ($file in $xmlFiles) {
if (Test-Path $file) {
$content = Get-Content $file -Raw
$content = $content -replace '<Enabled>true</Enabled>', '<Enabled>false</Enabled>'
$content = $content -replace '<Hidden>true</Hidden>', '<Hidden>false</Hidden>'
Set-Content $file -Value $content -NoNewline
}
}

# edit json files, set installbydefault to no
$jsonFiles = @(
"$env:SystemRoot\Temp\amddriver\Config\InstallManifest.json"
"$env:SystemRoot\Temp\amddriver\Bin64\cccmanifest_64.json"
)
foreach ($file in $jsonFiles) {
if (Test-Path $file) {
$content = Get-Content $file -Raw
$content = $content -replace '"InstallByDefault"\s*:\s*"Yes"', '"InstallByDefault" : "No"'
Set-Content $file -Value $content -NoNewline
}
}

        Write-Host "INSTALLING DRIVER`n"

# install amd driver
Start-Process -Wait "$env:SystemRoot\Temp\amddriver\Bin64\ATISetup.exe" -ArgumentList "-INSTALL -VIEW:2" -WindowStyle Hidden

# delete amdnoisesuppression startup
cmd /c "reg delete `"HKCU\Software\Microsoft\Windows\CurrentVersion\Run`" /v `"AMDNoiseSuppression`" /f >nul 2>&1"

# delete startrsx startup
cmd /c "reg delete `"HKCU\Software\Microsoft\Windows\CurrentVersion\RunOnce`" /v `"StartRSX`" /f >nul 2>&1"

# delete startcn task
Unregister-ScheduledTask -TaskName "StartCN" -Confirm:$false -ErrorAction SilentlyContinue

# delete amd crash defender service
cmd /c "sc stop `"AMD Crash Defender Service`" >nul 2>&1"
cmd /c "sc delete `"AMD Crash Defender Service`" >nul 2>&1"

# delete amd crash defender driver
cmd /c "sc stop `"amdfendr`" >nul 2>&1"
cmd /c "sc delete `"amdfendr`" >nul 2>&1"

# delete amd crash defender manager driver
cmd /c "sc stop `"amdfendrmgr`" >nul 2>&1"
cmd /c "sc delete `"amdfendrmgr`" >nul 2>&1"

# delete amd audio coprocessr dsp driver
cmd /c "sc stop `"amdacpbus`" >nul 2>&1"
cmd /c "sc delete `"amdacpbus`" >nul 2>&1"

# delete amd streaming audio function driver
cmd /c "sc stop `"AMDSAFD`" >nul 2>&1"
cmd /c "sc delete `"AMDSAFD`" >nul 2>&1"

# delete amd function driver for hd audio service driver
cmd /c "sc stop `"AtiHDAudioService`" >nul 2>&1"
cmd /c "sc delete `"AtiHDAudioService`" >nul 2>&1"

# delete amd bug report tool
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\AMD Bug Report Tool" -Recurse -Force -ErrorAction SilentlyContinue | Out-Null
Remove-Item "$env:SystemDrive\Windows\SysWOW64\AMDBugReportTool.exe" -Force -ErrorAction SilentlyContinue | Out-Null

# uninstall amd install manager
$findamdinstallmanager = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*"
$amdinstallmanager = Get-ItemProperty $findamdinstallmanager -ErrorAction SilentlyContinue |
Where-Object { $_.DisplayName -like "*AMD Install Manager*" }
if ($amdinstallmanager) {
$guid = $amdinstallmanager.PSChildName
Start-Process "msiexec.exe" -ArgumentList "/x $guid /qn /norestart" -Wait -NoNewWindow
}

# delete download
Remove-Item "$InstallFile" -Force -ErrorAction SilentlyContinue | Out-Null

# cleaner start menu shortcut path
$folderName = "AMD Software$([char]0xA789) Adrenalin Edition"
Move-Item -Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\$folderName\$folderName.lnk" -Destination "$env:ProgramData\Microsoft\Windows\Start Menu\Programs" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\$folderName" -Recurse -Force -ErrorAction SilentlyContinue

# delete old driver files
Remove-Item "$env:SystemDrive\AMD" -Recurse -Force -ErrorAction SilentlyContinue | Out-Null

        Write-Host "IMPORTING SETTINGS"
        Write-Host "IGNORE RSSERVCMD.EXE ERROR`n" -ForegroundColor Red

# open & close amd software adrenalin edition settings page so settings stick
Start-Process "$env:SystemDrive\Program Files\AMD\CNext\CNext\RadeonSoftware.exe"
Start-Sleep -Seconds 15
Stop-Process -Name "RadeonSoftware" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# import amd software adrenalin edition settings
# system
# manual check for updates
cmd /c "reg add `"HKCU\Software\AMD\CN`" /v `"AutoUpdate`" /t REG_DWORD /d `"0`" /f >nul 2>&1"

# graphics
# graphics profile - custom
cmd /c "reg add `"HKCU\Software\AMD\CN`" /v `"WizardProfile`" /t REG_SZ /d `"PROFILE_CUSTOM`" /f >nul 2>&1"

# wait for vertical refresh - always off
$basePath = "HKLM:\System\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"
$allKeys = Get-ChildItem -Path $basePath -Recurse -ErrorAction SilentlyContinue
$optionKeys = $allKeys | Where-Object { $_.PSChildName -eq "UMD" }
foreach ($key in $optionKeys) {
$regPath = $key.Name
cmd /c "reg add `"$regPath`" /v `"VSyncControl`" /t REG_BINARY /d `"3000`" /f >nul 2>&1"
}

# texture filtering quality - performance
$basePath = "HKLM:\System\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"
$allKeys = Get-ChildItem -Path $basePath -Recurse -ErrorAction SilentlyContinue
$optionKeys = $allKeys | Where-Object { $_.PSChildName -eq "UMD" }
foreach ($key in $optionKeys) {
$regPath = $key.Name
cmd /c "reg add `"$regPath`" /v `"TFQ`" /t REG_BINARY /d `"3200`" /f >nul 2>&1"
}

# tessellation mode - override application settings
# maximum tessellation level - off
$basePath = "HKLM:\System\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"
$allKeys = Get-ChildItem -Path $basePath -Recurse -ErrorAction SilentlyContinue
$optionKeys = $allKeys | Where-Object { $_.PSChildName -eq "UMD" }
foreach ($key in $optionKeys) {
$regPath = $key.Name
cmd /c "reg add `"$regPath`" /v `"Tessellation`" /t REG_BINARY /d `"3100`" /f >nul 2>&1"
cmd /c "reg add `"$regPath`" /v `"Tessellation_OPTION`" /t REG_BINARY /d `"3200`" /f >nul 2>&1"
}

# display
# accept custom resolution eula
cmd /c "reg add `"HKCU\Software\AMD\CN\CustomResolutions`" /v `"EulaAccepted`" /t REG_SZ /d `"true`" /f >nul 2>&1"

# accept overrides eula
cmd /c "reg add `"HKCU\Software\AMD\CN\DisplayOverride`" /v `"EulaAccepted`" /t REG_SZ /d `"true`" /f >nul 2>&1"

# vari-bright - maximize brightness
$basePath = "HKLM:\System\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"
$allKeys = Get-ChildItem -Path $basePath -Recurse -ErrorAction SilentlyContinue
$optionKeys = $allKeys | Where-Object { $_.PSChildName -eq "power_v1" }
foreach ($key in $optionKeys) {
$regPath = $key.Name
cmd /c "reg add `"$regPath`" /v `"abmlevel`" /t REG_BINARY /d `"00000000`" /f >nul 2>&1"
}

# preferences
# disable system tray menu
cmd /c "reg add `"HKCU\Software\AMD\CN`" /v `"SystemTray`" /t REG_SZ /d `"false`" /f >nul 2>&1"

# disable toast notifications
cmd /c "reg add `"HKCU\Software\AMD\CN`" /v `"CN_Hide_Toast_Notification`" /t REG_SZ /d `"true`" /f >nul 2>&1"

# disable animation & effects
cmd /c "reg add `"HKCU\Software\AMD\CN`" /v `"AnimationEffect`" /t REG_SZ /d `"false`" /f >nul 2>&1"

# notifications - remove
cmd /c "reg delete `"HKCU\Software\AMD\CN\Notification`" /f >nul 2>&1"
cmd /c "reg add `"HKCU\Software\AMD\CN\Notification`" /f >nul 2>&1"
cmd /c "reg add `"HKCU\Software\AMD\CN\FreeSync`" /v `"AlreadyNotified`" /t REG_DWORD /d `"1`" /f >nul 2>&1"
cmd /c "reg add `"HKCU\Software\AMD\CN\OverlayNotification`" /v `"AlreadyNotified`" /t REG_DWORD /d `"1`" /f >nul 2>&1"
cmd /c "reg add `"HKCU\Software\AMD\CN\VirtualSuperResolution`" /v `"AlreadyNotified`" /t REG_DWORD /d `"1`" /f >nul 2>&1"

        Clear-Host
        Write-Host "SET" -ForegroundColor Yellow
        Write-Host "- SOUND" -ForegroundColor Yellow
        Write-Host "- RESOLUTION" -ForegroundColor Yellow
        Write-Host "- REFRESH RATE" -ForegroundColor Yellow
        Write-Host "- PRIMARY DISPLAY`n" -ForegroundColor Yellow
		## shell:appsFolder\NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj!NVIDIACorp.NVIDIAControlPanel
    	## ms-settings:display
		## mmsys.cpl

# open display, nvidia & sound panels
try {
Start-Process "ms-settings:display"
} catch { }
try {
Start-Process shell:appsFolder\NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj!NVIDIACorp.NVIDIAControlPanel
} catch { }
Start-Process mmsys.cpl
Pause

        Clear-Host

# disable automatically manage color for apps
$basePath = "HKLM:\SYSTEM\CurrentControlSet\Control\GraphicsDrivers\MonitorDataStore"
$monitorKeys = Get-ChildItem -Path $basePath -Recurse -ErrorAction SilentlyContinue
foreach ($key in $monitorKeys) {
$regPath = $key.Name
cmd /c "reg add `"$regPath`" /v `"AutoColorManagementEnabled`" /t REG_DWORD /d `"0`" /f >nul 2>&1"
}

# enable msi mode for all gpus
$gpuDevices = Get-PnpDevice -Class Display
foreach ($gpu in $gpuDevices) {
$instanceID = $gpu.InstanceId
cmd /c "reg add `"HKLM\SYSTEM\ControlSet001\Enum\$instanceID\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties`" /v `"MSISupported`" /t REG_DWORD /d `"1`" /f >nul 2>&1"
}

# show all hidden taskbar icons
        ## ms-settings:taskbar
$notifyiconsettings = Get-ChildItem -Path 'registry::HKEY_CURRENT_USER\Control Panel\NotifyIconSettings' -Recurse -Force
foreach ($setreg in $notifyiconsettings) {
if ((Get-ItemProperty -Path "registry::$setreg").IsPromoted -eq 0) {
}
else {
Set-ItemProperty -Path "registry::$setreg" -Name 'IsPromoted' -Value 1 -Force
}
}

        Write-Host "RESTARTING`n" -ForegroundColor Red

# restart
Start-Sleep -Seconds 5
shutdown -r -t 00
