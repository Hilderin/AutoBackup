using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace AutoBackup
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();

            
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            //Starting service after installation
            ServiceController sc = new ServiceController("AutoBackup");
            sc.Start();
        }

    }
}
