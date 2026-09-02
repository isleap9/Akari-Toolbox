# D-04 post-install hardening for PotPlayer (Daum.PotPlayer, winget-installed).
# Source: `4 Installers/1 Installers.ps1` lines 610-611 — Start Menu shortcut cleanup only.
# OS-standard Start Menu Programs root path, independent of the actual install location
# (04-RESEARCH.md Pitfall 1 does not apply here).
Move-Item -Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\PotPlayer\PotPlayer 64 bit.lnk" -Destination "$env:ProgramData\Microsoft\Windows\Start Menu\Programs" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\PotPlayer" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "PotPlayer Start Menu shortcut cleanup complete."
