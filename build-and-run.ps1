# Builds and launches Akari Toolbox (Debug).
#
# Kills any running copy first â€” the WinUI apphost.exe stays locked while the app is
# alive, which makes dotnet build fail with MSB3027/MSB3021.
param(
    [switch]$NoLaunch,
    [switch]$Release
)

$ErrorActionPreference = 'Stop'

$root    = $PSScriptRoot
$project = Join-Path $root 'src\AppTemplate.App\AppTemplate.App.csproj'
$exe     = Join-Path $root 'src\AppTemplate.App\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64\AkariToolbox.App.exe'
$config  = if ($Release) { 'Release' } else { 'Debug' }

# Stop-Process returns before the OS has released the apphost file handle, which
# still shows up as MSB3026 "file is locked" a moment later. Wait for real exit.
Get-Process -Name 'AkariToolbox.App' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
for ($i = 0; $i -lt 40; $i++) {
    if (-not (Get-Process -Name 'AkariToolbox.App' -ErrorAction SilentlyContinue)) { break }
    Start-Sleep -Milliseconds 250
}

$log = Join-Path $env:TEMP 'akaribox-build.log'
dotnet build $project -c $config --no-restore *>&1 | Tee-Object -FilePath $log | Out-Null

# XAML compiler diagnostics are reported as "XamlCompiler error WMC1121", not ": error ",
# so a naive grep reports a clean build and then launches a stale binary.
$errors   = (Select-String -Path $log -Pattern ': error |XamlCompiler error |error XC|error WMC').Count
$warnings = (Select-String -Path $log -Pattern ': warning |XamlCompiler warning |warning WMC').Count

$buildFailed = (Select-String -Path $log -Pattern 'Build FAILED').Count -gt 0

Write-Host ''
Select-String -Path $log -Pattern 'Build succeeded|Build FAILED' | ForEach-Object { $_.Line }
Write-Host "  Errors: $errors   Warnings: $warnings"

if ($buildFailed -or $errors -gt 0) {
    Write-Host ''
    Write-Host 'Build errors:'
    Select-String -Path $log -Pattern ': error |XamlCompiler error |error XC' | Select-Object -First 25 | ForEach-Object { '  ' + $_.Line }
    exit 1
}

if ($NoLaunch) { exit 0 }

Write-Host ''
Write-Host "Launching $exe"
Start-Process -FilePath $exe | Out-Null
