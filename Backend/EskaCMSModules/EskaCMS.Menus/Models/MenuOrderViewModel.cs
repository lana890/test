using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Menus.Models
{
    public class MenuOrderViewModel
    {
        //added by Mahmoud to save order of the menu
        public long? sourceParentId { get; set; }
        public long sourceID { get; set; }
        public long? destinationParentId { get; set; }
        public long destinationID { get; set; }
        public string type { get; set; }

        public long? newOrder { get; set; }
        public long? categoryId { get; set; }
    }



    public class MenuGroupWithSortVM
    {
        public long MenuGroupId { get; set; }
        public List<MenuSortVM> SortedList { get; set; }

    }


    public class MenuSortVM
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public long Order { get; set; }

    }




}
