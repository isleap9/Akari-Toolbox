Clear-Host

Write-Host "BitLocker: Off..."

# disable bitlocker
try {
Get-BitLockerVolume |
Where-Object {
$_.ProtectionStatus -eq "On" -or $_.VolumeStatus -ne "FullyDecrypted"
} |
ForEach-Object {
Disable-BitLocker -MountPoint $_.MountPoint -ErrorAction SilentlyContinue | Out-Null
}
} catch { }

# open settings
Start-Process control.exe -ArgumentList "/name microsoft.bitlockerdriveencryption"

manage-bde -status

exit
