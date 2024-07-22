using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmamiInboundMail.Console.Models
{
    public class FtpConfig
    {
        public string Email { get; set; }
        public string FtpUrl { get; set; }
        public string FtpUsername { get; set; }
        public string FtpPassword { get; set; }
        public bool MarkAsread { get; set; } = false;
        public bool DeleteAfterRead { get; set; } = false;
        public string Password { get; set; }
        public string[] FileFormats { get; set; }
        public string FolderPath { get; set; }
        public string Plant { get; set; }
        public bool IncludeOriginalFileName { get; set; } = false;
    }
}
