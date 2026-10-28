using EskaCMS.Infrastructure.Models.MeiliSearchVM;
using EskaCMS.Infrastructure.Modules.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Search.Model
{ 
    public class MeiliSearchInputVM
    {
        public string SearchValue { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
        public long culture { get; set; }
        public EEntity[] EntityTypes { get; set; }
    

    }
}


