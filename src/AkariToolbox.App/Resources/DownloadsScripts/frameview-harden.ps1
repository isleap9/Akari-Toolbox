# D-04 post-install hardening for Frame View (Nvidia.FrameView, winget-installed).
# Source: `4 Installers/1 Installers.ps1` lines 356-357 — Start Menu shortcut cleanup only.
# OS-standard Start Menu Programs root path, independent of winget's actual FrameView
# install location (04-RESEARCH.md Pitfall 1 does not apply here).
Move-Item -Path "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\NVIDIA FrameView\FrameView.lnk" -Destination "$env:ProgramData\Microsoft\Windows\Start Menu\Programs" -Force -ErrorAction SilentlyContinue
Remove-Item "$env:ProgramData\Microsoft\Windows\Start Menu\Programs\NVIDIA FrameView" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Frame View Start Menu shortcut cleanup complete."
