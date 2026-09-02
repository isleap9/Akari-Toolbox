# Enable PowerShell 7 Telemetry - Undo
[System.Environment]::SetEnvironmentVariable("POWERSHELL_TELEMETRY_OPTOUT", $null, "Machine")
Write-Host "PowerShell 7 telemetry re-enabled (opt-in)."
