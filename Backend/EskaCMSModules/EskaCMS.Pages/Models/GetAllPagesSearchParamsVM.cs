using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Pages.Models
{
    public 
        
class GetAllPagesSearchParamsVM
    {
        public string? PageName { get; set; }
        public EStatus? Status { get; set; }
        public long? PageCategory { get; set; }
        public long? CultureId { get; set; }
        public string? CultureCode { get; set; }
        public int PageNumber { get; set; } = 0;
        public int PageSize { get; set; }
        public string? SearchText { get; set; }
        public GetAllPagesSearchParamsVM()
        {
            this.PageName = null;
            this.Status = 0;
            this.PageCategory = 0;
            this.CultureId = 0;
            this.CultureCode = null;
            this.PageNumber = 0;
            this.PageSize = 20;
            this.SearchText = null;

        }
    }
}
