using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace AutoBackup
{
    public partial class AutoBackupService : ServiceBase
    {
        /// <summary>
        /// Backup worker
        /// </summary>
        private AutoBackupWorker _worker;

        /// <summary>
        /// Constructor
        /// </summary>
        public AutoBackupService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Service started
        /// </summary>
        protected override void OnStart(string[] args)
        {
            Utils.Log("Service started.");

            _worker = new AutoBackupWorker();
            _worker.Start();

        }

        /// <summary>
        /// Service stoped
        /// </summary>
        protected override void OnStop()
        {
            if (_worker != null)
                _worker.Stop();

            Utils.Log("Service stopped.");
        }
    }
}
