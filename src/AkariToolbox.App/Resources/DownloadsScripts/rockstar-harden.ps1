# D-04 post-install hardening for Rockstar Games (RockstarGames.Launcher, winget-installed).
# Source: `4 Installers/1 Installers.ps1` lines 629-630 — Start Menu shortcut cleanup only.
# Note: source path is user-level AppData (matching the source script exactly), unlike the
# other hardening scripts' ProgramData source path — both are still OS-standard Start Menu
# locations, independent of the actual install location (04-RESEARCH.md Pitfall 1 does not
# apply here).
Move-Item -Path "$env:AppData\Microsoft\Windows\Start Menu\Programs\Rockstar Games\Rockstar Games Launcher.lnk" -Destination "$env:ProgramData\Microsoft\Windows\Start Menu\Programs" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:AppData\Microsoft\Windows\Start Menu\Programs\Rockstar Games" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Rockstar Games Start Menu shortcut cleanup complete."
