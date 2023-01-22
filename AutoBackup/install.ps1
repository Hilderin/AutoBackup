
New-Service -Name "USBAutoBackup" -BinaryPathName "C:\Projects\USBAutoBackup\USBAutoBackup\bin\Debug\USBAutoBackup.exe"

Start-Service -Name "USBAutoBackup"