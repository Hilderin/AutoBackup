using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoBackup
{
    /// <summary>
    /// Worker for the auto backup
    /// </summary>
    public class AutoBackupWorker
    {
        /// <summary>
        /// Information on the last destination
        /// </summary>
        private static SharedFolderConnection _connection = null;
        private static string _lastDestination;
        private static string _lastUsername;
        private static int _lastPasswordHash;


        /// <summary>
        /// Indicate if we need to stop the process
        /// </summary>
        private bool _mustStop = false;


        /// <summary>
        /// Start the thread
        /// </summary>
        public void Start()
        {
            Thread thread = new Thread(DoItMain);
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// Stop the thread
        /// </summary>
        public void Stop()
        {
            _mustStop = true;

        }

        /// <summary>
        /// Retore
        /// </summary>
        public bool Restore()
        {
            Config config = ReadConfig();
            if (config == null)
            {
                Log("No config file.");
                return false;
            }


            //We at least have a destination folder?
            if (String.IsNullOrEmpty(config.Source) || String.IsNullOrEmpty(config.Destination))
            {
                Log("Source or Destination not set");
                return false;
            }


            if (!Directory.Exists(config.Destination))
            {
                Log("Backup folder not found: " + config.Destination);
                return false;
            }


            return Restore(config.Destination, config.Source, config.Destination, config.SecretKey);

        }


        /// <summary>
        /// Restore
        /// </summary>
        private bool Restore(string backupFolder, string restoreFolder, string rootBackup, string secretKey)
        {
            bool result = true;

            if (!Directory.Exists(restoreFolder))
                Directory.CreateDirectory(restoreFolder);

            //Files....
            foreach (string filename in Directory.EnumerateFiles(backupFolder))
            {
                string restoreFile = Path.Combine(restoreFolder, Path.GetFileName(filename));
                if (!String.IsNullOrEmpty(secretKey))
                    CryptoHelper.FileDecrypt(filename, restoreFile, secretKey);
                else
                    File.Copy(filename, restoreFile, true);
                Log("Restored: " + filename + " to " + restoreFile);
            }

            //Folders...
            foreach (string folder in Directory.EnumerateDirectories(backupFolder))
            {
                //We don't want to restore the deleted folder!
                if (folder.Equals(Path.Combine(rootBackup, ".deleted"), StringComparison.OrdinalIgnoreCase))
                    continue;


                if (!Restore(folder, Path.Combine(restoreFolder, Path.GetFileName(folder)), rootBackup, secretKey))
                    result = false;
            }

            return result;
        }

        /// <summary>
        /// Main thread
        /// </summary>
        private void DoItMain()
        {
            try
            {
                Log("DoItMain - Starting...");
                DateTime lastCheck = DateTime.MinValue;
                DateTime lastBackup = DateTime.MinValue;


                while (!_mustStop)
                {
                    //We check each 5 minutes...
                    if (DateTime.Now.Subtract(lastCheck).TotalMinutes >= 5)
                    {
                        //Backup each 12 hours
                        if (DateTime.Now.Subtract(lastBackup).TotalHours >= 12)
                        {
                            if (ExecuteBackup())
                            {
                                Log("DoItMain - ExecuteBackup OK.");
                                lastBackup = DateTime.Now;
                            }
                        }
                    }

                    lastCheck = DateTime.Now;
                    Thread.Sleep(1000);
                }

                Log("DoItMain - Stopped!");
            }
            catch (Exception ex)
            {
                Log("DoItMain - Error: " + ex.ToString());
            }
            finally
            {
                try
                {
                    if (_connection != null)
                        _connection.Dispose();
                }
                catch { }
            }
        }

        /// <summary>
        /// Execute the backup
        /// </summary>
        /// <returns></returns>
        private bool ExecuteBackup()
        {
            try
            {
                ////Search the source path...
                //string sourcePath = GetPathToBackup();

                ////If no source path, we return, nothing to do
                //if (String.IsNullOrEmpty(sourcePath))
                //    return false;


                Config config = ReadConfig();
                if (config == null)
                {
                    Log("No config file.");
                    return false;
                }

                //We at least have a destination folder?
                if (String.IsNullOrEmpty(config.Source) || String.IsNullOrEmpty(config.Destination))
                {
                    Log("Source or Destination not set");
                    return false;
                }

                try
                {
                    if (!Directory.Exists(config.Source))
                    {
                        Log("Source not available: " + config.Source);
                        return false;
                    }
                }
                catch (Exception exSource)
                {
                    Log("Source not available: " + config.Source + " - Error: " + exSource.Message);
                    return false;
                }

                //Creds?
                ConnectToDestination(config);

                //We do it...
                return ExecuteBackup(config.Source, config.Destination, config.Destination, config.SecretKey);


            }
            catch (Exception ex)
            {
                Log("ExecuteBackup - Error: " + ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Connect to destination
        /// </summary>
        private void ConnectToDestination(Config config)
        {

            //Need to create a new connection?
            if (_connection == null || config.Destination != _lastDestination || config.Username != _lastUsername || config.Password.GetHashCode() != _lastPasswordHash)
            {
                if (_connection != null)
                    _connection.Dispose();


                if (!String.IsNullOrEmpty(config.Username))
                {
                    //We really have a connection to create...
                    Log("Connecting to " + config.Destination + "...");
                    NetworkCredential creds = new NetworkCredential(config.Username, config.Password);
                    _connection = new SharedFolderConnection(config.Destination, creds);
                }
            }


            _lastDestination = config.Destination;
            _lastUsername = config.Username;
            _lastPasswordHash = config.Password.GetHashCode();

        }


        /// <summary>
        /// Execute the backup
        /// </summary>
        private bool ExecuteBackup(string source, string destination, string rootDestination, string secretKey)
        {
            try
            {
                bool result = true;

                //Backup files...
                foreach (string filename in Directory.EnumerateFiles(source))
                {
                    string destFileName = Path.Combine(destination, Path.GetFileName(filename));
                    try
                    {
                        //Destination not existing or destination is older?
                        if (!File.Exists(destFileName) || File.GetLastWriteTime(filename).CompareTo(File.GetLastWriteTime(destFileName)) > 0)
                        {
                            if (!Directory.Exists(destination))
                                Directory.CreateDirectory(destination);

                            if (!String.IsNullOrEmpty(secretKey))
                                CryptoHelper.FileEncrypt(filename, destFileName, secretKey);
                            else
                                File.Copy(filename, destFileName, true);
                            Log("ExecuteBackup - File backuped: " + filename + " to " + destFileName);
                        }
                    }
                    catch (Exception exFile)
                    {
                        Log("ExecuteBackup (" + filename + " to " + destFileName + ") - Error: " + exFile.ToString());
                        result = false;
                    }
                }

                //Sub folders...
                foreach (string directoryName in Directory.EnumerateDirectories(source))
                {
                    if (!ExecuteBackup(directoryName, Path.Combine(destination, Path.GetFileName(directoryName)), rootDestination, secretKey))
                        result = false;
                }


                if (Directory.Exists(destination))
                {
                    //Removed files...
                    foreach (string filename in Directory.EnumerateFiles(destination))
                    {
                        string sourceFileName = Path.Combine(source, Path.GetFileName(filename));
                        try
                        {
                            //Source not existing... the file has been removed...
                            if (!File.Exists(sourceFileName))
                            {
                                string subFolder = destination.Substring(rootDestination.Length);
                                if (subFolder.StartsWith("\\"))
                                    subFolder = subFolder.Substring(1);

                                string destinationDeleted = Path.Combine(rootDestination, ".deleted", subFolder);
                                if (!Directory.Exists(destinationDeleted))
                                    Directory.CreateDirectory(destinationDeleted);

                                string destFileName = Path.Combine(destinationDeleted, Path.GetFileName(filename));
                                if (File.Exists(destFileName))
                                    File.Delete(destFileName);
                                File.Move(filename, destFileName);
                                Log("ExecuteBackup - File deleted: " + filename + " to " + destFileName);
                            }
                        }
                        catch (Exception exFile)
                        {
                            Log("ExecuteBackup (deletion of " + filename + ") - Error: " + exFile.ToString());
                            result = false;
                        }
                    }


                    //Removed sub folders...
                    foreach (string directoryName in Directory.EnumerateDirectories(destination))
                    {
                        //We don't want to redelete the deleted folder!
                        if (directoryName.Equals(Path.Combine(rootDestination, ".deleted"), StringComparison.OrdinalIgnoreCase))
                            continue;

                        string sourceDirectoryName = Path.Combine(source, Path.GetFileName(directoryName));
                        try
                        {
                            //Source not existing... the file has been removed...
                            if (!Directory.Exists(sourceDirectoryName))
                            {
                                string subFolder = directoryName.Substring(rootDestination.Length);
                                if (subFolder.StartsWith("\\"))
                                    subFolder = subFolder.Substring(1);

                                string destinationDeleted = Path.Combine(rootDestination, ".deleted", subFolder);

                                if (!Directory.Exists(Path.GetDirectoryName(destinationDeleted)))
                                    Directory.CreateDirectory(Path.GetDirectoryName(destinationDeleted));

                                if (Directory.Exists(destinationDeleted))
                                    Directory.Delete(destinationDeleted, true);

                                Directory.Move(directoryName, destinationDeleted);

                                Log("ExecuteBackup - Folder deleted: " + directoryName + " to " + destinationDeleted);
                            }
                        }
                        catch (Exception exFile)
                        {
                            Log("ExecuteBackup (deletion of folder " + directoryName + ") - Error: " + exFile.ToString());
                            result = false;
                        }
                    }
                }

                return result;

            }
            catch (Exception ex)
            {
                Log("ExecuteBackup (" + source + " to " + destination + ") - Error: " + ex.ToString());
                return false;
            }
        }

        ///// <summary>
        ///// Get the path to backup
        ///// </summary>
        //private string GetPathToBackup()
        //{
            
        //    foreach (DriveInfo drive in DriveInfo.GetDrives())
        //    {
        //        if (drive.DriveType == DriveType.Removable)
        //        {
        //            string volumeLabel = drive.VolumeLabel;
        //            if (String.IsNullOrEmpty(volumeLabel))
        //                volumeLabel = "USBDrive";

        //            //Removing invalid characters from string...
        //            volumeLabel = volumeLabel.Trim(Path.GetInvalidFileNameChars());
        //            volumeLabel = volumeLabel.Trim(Path.GetInvalidPathChars());

        //            return Path.Combine(drive.RootDirectory.FullName, volumeLabel);
        //        }
        //    }

        //    return null;
        //}

        /// <summary>
        /// Read the config on disk
        /// </summary>
        private Config ReadConfig()
        {
            try
            {
                string configPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Config.json");

                if (!File.Exists(configPath))
                {
                    Log("No config file: " + configPath);
                    return null;
                }

                return JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));

            }
            catch (Exception ex)
            {
                Log("ReadConfig - Error: " + ex.ToString());
                return null;
            }

        }

        /// <summary>
        /// Log on disk
        /// </summary>
        private void Log(string data)
        {
            Console.WriteLine(data);
            Utils.Log(data);
        }

    }
}
