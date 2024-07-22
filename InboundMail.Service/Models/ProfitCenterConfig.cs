using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmamiInboundMail.Service.Models
{
    public class ProfitCenterConfig
    {
        public int ID { get; set; }
        public string ProfitCenter { get; set; }
        public string EmailID { get; set; }
        public bool IsDeleted { get; set; }
    }
}
