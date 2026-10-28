using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Infrastructure.Web.SmartTable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class ClassificationSearchVM
    {
        public List<Classifications> Classifications { get; set; }
        public Pagination Pagination { get; set; }
    }




}
