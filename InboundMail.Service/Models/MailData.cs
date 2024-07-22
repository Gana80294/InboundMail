using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmamiInboundMail.Service.Models
{
    public class MailData
    {
        public string FromAddress { get; set; }
        public string Subject { get; set; }
        public DateTime MailDate { get; set; }
    }
}
