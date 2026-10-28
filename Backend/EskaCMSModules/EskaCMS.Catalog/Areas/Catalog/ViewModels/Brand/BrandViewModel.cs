using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels.BrandVm
{
    public class BrandViewModel
    {
        public long Id { get; set; }

        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? ExpiryDate { get; set; }
        public long?  ThumbnailId { get; set; }
        public string? PublicUrl { get; set; }
        public EStatus Status { get; set; }

        public IList<BrandDetailsViewModel>  brandDetails { get; set; }


    }

    public class BrandDetailsViewModel
    {
        public long Id { get; set; }
        public string Slug { get; set; }

        public string Name { get; set; }
        public string CultureCode { get; set; }

        public string Description { get; set; }
        public long CultureId { get; set; }
        public long BrandTemplateId { get; set; }
        public long BrandId { get; set; }



    }


}
