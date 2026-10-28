using System;
using System.Collections.Generic;
using System.Text;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.BusinessModels
{
  public  class UserOptionsVM
    {
        public string PropertyName { get; set; }
        public string Value { get; set; }
        public EControlTypes ControlType { get; set; }
        public bool IsEditable { get; set; }
        public bool IsVisible { get; set; }
        public long Id { get; set; }
    }
}
