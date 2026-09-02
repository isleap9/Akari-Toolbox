# D-04 post-install hardening for Nvidia App (XP8CLZL93F5Z4P, msstore-sourced winget install).
# Source: `4 Installers/1 Installers.ps1` lines 546-547 — Start Menu shortcut cleanup only.
# OS-standard Start Menu Programs root path, independent of the actual install location
# (04-RESEARCH.md Pitfall 1 does not apply here).
Move-Item -Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\NVIDIA Corporation\NVIDIA App.lnk" -Destination "$env:ProgramData\Microsoft\Windows\Start Menu\Programs" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\NVIDIA Corporation" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Nvidia App Start Menu shortcut cleanup complete."
