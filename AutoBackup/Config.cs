using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBackup
{
    public class Config
    {
        /// <summary>
        /// Source path
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Destination path
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// Username for the Destination
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Password for the Destination
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Secret key
        /// </summary>
        public string SecretKey { get; set; }
    }
}
