# D-04 post-install hardening for Ubisoft Connect (Ubisoft.Connect, winget-installed).
# Source: `4 Installers/1 Installers.ps1` lines 697-698 — Start Menu shortcut cleanup only.
# Note: source path is user-level AppData (matching the source script exactly) — still an
# OS-standard Start Menu location, independent of the actual install location
# (04-RESEARCH.md Pitfall 1 does not apply here).
Move-Item -Path "$env:AppData\Microsoft\Windows\Start Menu\Programs\Ubisoft\Ubisoft Connect\Ubisoft Connect.lnk" -Destination "$env:ProgramData\Microsoft\Windows\Start Menu\Programs" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:AppData\Microsoft\Windows\Start Menu\Programs\Ubisoft" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Ubisoft Connect Start Menu shortcut cleanup complete."
