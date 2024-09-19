using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetAdminSmartDisplay
{
    public class FtpConfig
    {
        public FtpCredentials FtpCredentials { get; set; }
    }

    public class FtpCredentials
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
