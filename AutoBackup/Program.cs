using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AutoBackup
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0 && args[0] == "--noservice")
                {
                    Console.Write("Starting command line mode...");

                    AutoBackupWorker worker = new AutoBackupWorker();
                    worker.Start();

                    while (true)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
                else if (args.Length > 0 && args[0] == "restore")
                {
                    //Restore backup...
                    AutoBackupWorker worker = new AutoBackupWorker();
                    if (!worker.Restore())
                        Environment.ExitCode = 1;
                }
                else
                {
                    //Service mode...
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                new AutoBackupService()
                    };
                    ServiceBase.Run(ServicesToRun);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.ToString());
            }
        }

    }
}
