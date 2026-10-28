using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class FormItemDetailsVM
    {

        public string Name { get; set; }
        public string Description { get; set; }
        public long CultureId { get; set; }
        public long? FormItemId { get; set; }
        public string CultureCode { get; set; }
    }
    public class FormItemDetailsWithClassiffictionVM
    {
        public string Name { get; set; }

        public string Description { get; set; }
        public long CultureId { get; set; }
        public EskaCMS.Entities.Entity.Core.SitesCultures Culture { get; set; }

        public long FormItemId { get; set; }
        public FormItemDto FormItem { get; set; }



    }

}
