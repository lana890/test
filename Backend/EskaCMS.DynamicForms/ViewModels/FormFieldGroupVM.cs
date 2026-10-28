using EskaCMS.Entities.Entity;
using EskaCMS.Entities.Entity.DynamicForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.DynamicForms.ViewModels
{
    public class FormFieldGroupVM
    {
        public long? Id { get; set; }
        public string Name { get; set; }


        public EStatus Status { get; set; }

        public long FormItemId { get; set; }

        public long cultureId { get; set; }
        public IList<FormFieldVM>? FormFields { get; set; }
        public IList<FormFieldDetailsVM> FormFieldDetails { get; set; }
    }
}
