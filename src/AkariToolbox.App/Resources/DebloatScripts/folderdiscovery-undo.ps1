# Enable Folder Discovery - Undo
$path = "HKCU:\Software\Classes\Local Settings\Software\Microsoft\Windows\Shell\Bags\AllFolders\Shell"
Remove-ItemProperty -Path $path -Name "FolderType" -Force -ErrorAction SilentlyContinue
Write-Host "Folder Discovery enabled (default restored)."
