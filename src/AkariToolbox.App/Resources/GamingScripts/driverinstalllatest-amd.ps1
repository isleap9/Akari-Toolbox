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

Clear-Host

Write-Host "Downloading: AMD Driver Web Installer..."

# download amd driver auto detect
$DownloadAmd = Invoke-WebRequest "https://www.amd.com/en/support/download/drivers.html" -UseBasicParsing |
Select-Object -ExpandProperty Links |
Where-Object { $_.href -match "drivers\.amd\.com/drivers/installer/.*/whql/amd-software-adrenalin-edition-.*-minimalsetup-.*_web\.exe" } | Select-Object href
$spoofwebbrowser = @{
"User-Agent" = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36"
"Accept"     = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8"
"Referer"    = "https://www.amd.com/"
}
IWR $DownloadAmd.href -UseBasicParsing -Headers $spoofwebbrowser -OutFile "$env:SystemRoot\Temp\amddriver.exe" -ErrorAction SilentlyContinue | Out-Null

# open amd web driver installer
Start-Process "$env:SystemRoot\Temp\amddriver.exe"

exit
