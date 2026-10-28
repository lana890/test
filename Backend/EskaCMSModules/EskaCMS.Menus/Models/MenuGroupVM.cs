using EskaCMS.Entities.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Menus;

namespace EskaCMS.Menus.Models
{
    public class MenuGroupVM
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public IList<Menu> Menus { get; set; }

        public EStatus Status { get; set; }
        public long SiteId { get; set; }

        public virtual Category Category { get; set; }
        public virtual string CultureCode { get; set; }

        public int MenuItemsCount { get; set; }
        public long CategoryId{ get; set; }
    }


    public class UpsertMenuGroupVM
    {
        public long Id { get; set; }
        public string Name { get; set; }


        public long CultureId { get; set; }


        public long CategoryId { get; set; }
    }


    public class CloneMenuGroupVM
    {
        public string Name { get; set; }


        public long SourceGroupId { get; set; }
    }



    public class AddMenuItemToGroupVM
    {
        public long MenuGroupId { get; set; }
        public long MenuItemId { get; set; }
    }
}
