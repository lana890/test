using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.ViewModels.LoggedInUser
{
    public class SearchParamsVM
    {

        public int? PageSize { get; set; } 
        public int? PageNumber { get; set; }
        public string SearchText { get; set; }
        public ETypeLogin? TypeLogin { get; set; }
    }
}
