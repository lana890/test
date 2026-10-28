using EskaCMS.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models.SitesCultures
{
    public class SitesCulturesViewModel
    {
        public SitesCulturesViewModel(IWorkContext workContext)
        {
            CreationDate = DateTimeOffset.Now;
        }
        public long CreatedById { get; set; }
        public long? Id { get; set; }
        public long SiteId { get; set; }
        public string CultureCodeId { get; set; }
        public string CultureName { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
    public class SitesCulturesGetAllVM
    {

        public long CreatedById { get; set; }
        public long? Id { get; set; }
        public long SiteId { get; set; }
        public string CultureCodeId { get; set; }
        public string Culture { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
    public class SiteUsedCulture
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public EStatus Status { get; set; }
    }
    public class StatusSiteCulturesVM
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
