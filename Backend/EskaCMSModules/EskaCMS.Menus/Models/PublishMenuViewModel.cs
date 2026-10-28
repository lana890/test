using static EskaCMS.Entities.Enum.GeneralEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Menus.Models
{
   public class publishMenuViewModel
    {
        public List<long> MenuIds { get; set; }
        public Entities.Enum.GeneralEnums.EStatus status { get; set; }
    }
}
