Clear-Host

Write-Host "BitLocker: On..."

# open settings
Start-Process control.exe -ArgumentList "/name microsoft.bitlockerdriveencryption"

manage-bde -status

exit
