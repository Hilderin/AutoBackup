using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBackup
{
    /// <summary>
    /// Utils class
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Path du log
        /// </summary>
        private static string _logPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "AutoBackup.log");

        /// <summary>
        /// Log on disk
        /// </summary>
        public static void Log(string data)
        {
            try
            {
                File.AppendAllText(_logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + data + Environment.NewLine);
            }
            catch { }
        }
    }
}
