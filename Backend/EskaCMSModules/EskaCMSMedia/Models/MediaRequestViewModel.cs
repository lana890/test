using EskaCMS.Entities.Enum;
using EskaCMS.Media.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Media.Models
{
    public class MediaRequestViewModel
    {

        public long? folderId { get; set; }
        public List<MediaType>? filter { get; set; }

        public int? width { get; set; }
        public int? height { get; set; }

        public PaginationMediaViewModel PaginationMedia { get; set; }
        public PaginationMediaViewModel PaginationFolder { get; set; }

        public string SearchText { get; set; }
        public List<string>? ContentType { get; set; }
        public EMediaFilter? orderFilter { get; set; } 
        public EStatus? status { get; set; } = EStatus.Published;
    }

    public class PaginationMediaViewModel
    {
        public int PageSize { get; set; }
        public int PageNumber { get; set; }


    }
}
