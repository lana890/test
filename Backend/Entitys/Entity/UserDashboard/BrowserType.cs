using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.Entities.Entity.UserDashboard
{
    public  class BrowserType :EntityBaseWithCustomId
    {
        public string  Name { get; set; }
        public string Version { get; set; }
    }
}
