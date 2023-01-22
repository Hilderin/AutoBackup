# Auto Backup
This project is a service to help create an automatic backup on a network share for your USB drives.
You can set a secret key to encryp your files.

This program is ideal to backup automatically USB drive to a local or a network folder.
The backup will be executed automatically when the source is available and each 12h hours later after that.

## Installation

- Download the AutoBackupInstaller.msi from the releases section: https://github.com/Hilderin/AutoBackup/releases
- Install
- Create a Config.json file in the installation folder (default: C:\Program Files (x86)\AutoBackup). See below for more instructions
- Reboot or start the service in command line (run as admin): "sc start AutoBackup"


## Configuration
You need to create a configuration file named "Config.json" in the same folder as your installation (default: C:\Program Files (x86)\AutoBackup).

This is a json file with these parameters:
- Source: Source folder to be backuped Since this is a json file all '\' must be doubled.
- Destination: Destination folder where your backup files will be copied. Since this is a json file all '\' must be doubled.
- Username (optional): If your destination folder is a network path that is password protected, you need to specify a username.
- Password (optional): If your destination folder is a network path that is password protected, you need to specify a password.
- SecretKey (optionnal): Secret key if you want to encrypt your backup files.


### Exemple
```
{
    "Source": "C:\\MyFolderToBackup",
    "Destination": "\\\\networkshare\\NetworkPath",
    "Username": "NetworkUserName",
    "Password": "NetworkPassword",
    "SecretKey": "MySecretKey"
}
```