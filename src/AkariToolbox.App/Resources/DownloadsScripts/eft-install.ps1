# D-03 direct-CDN-install exception for Escape From Tarkov — no winget package exists for
# this title (04-RESEARCH.md live verification), so this is the *entire* install mechanism
# for this catalog entry, not a D-04 hardening add-on.
#
# T-04-09 mitigation (threat model): the vendor build changes over time, so there is no
# stable SHA256 to pin (unlike PostInstall's fixed-content assets, D-08). Instead, this
# script verifies the downloaded installer's Authenticode signature before ever running it
# under elevation — CLAUDE.md's "never execute unverified downloaded binary under elevation"
# rule, satisfied via signature trust rather than hash pinning. A Valid status proves the
# binary's bytes are unmodified since signing by a certificate chaining to a Windows-trusted
# root, catching a tampered or corrupted download.

$tempExePath = Join-Path $env:SystemRoot "Temp\Escape From Tarkov.exe"

# Download over HTTPS only — do not relax TLS validation.
Invoke-WebRequest -Uri "https://prod.escapefromtarkov.com/launcher/download" -OutFile $tempExePath

# Verify Authenticode signature before any execution under elevation.
$sig = Get-AuthenticodeSignature -FilePath $tempExePath
if ($sig.Status -ne 'Valid') {
    Write-Output "[EFT-INSTALL] Authenticode signature verification FAILED (Status: $($sig.Status)) — deleting untrusted installer, aborting."
    Remove-Item $tempExePath -Force -ErrorAction SilentlyContinue
    exit 1
}

Write-Host "Installing: Escape From Tarkov..."

# Install silently, wait for completion.
Start-Process -Wait $tempExePath -ArgumentList "/VERYSILENT /NORESTART"

# Create desktop shortcut.
$WshShell = New-Object -comObject WScript.Shell
$Desktop = (New-Object -ComObject Shell.Application).Namespace('shell:Desktop').Self.Path
$Shortcut = $WshShell.CreateShortcut("$Desktop\Battlestate Games Launcher.lnk")
$Shortcut.TargetPath = "$env:SystemDrive\Battlestate Games\BsgLauncher\BsgLauncher.exe"
$Shortcut.WorkingDirectory = "$env:SystemDrive\Battlestate Games\BsgLauncher"
$Shortcut.Save()

# Cleaner Start Menu shortcut path (OS-standard Start Menu Programs root).
Move-Item -Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Battlestate Games\Battlestate Games Launcher.lnk" -Destination "$env:ProgramData\Microsoft\Windows\Start Menu\Programs" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\Battlestate Games" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Escape From Tarkov installed successfully."
