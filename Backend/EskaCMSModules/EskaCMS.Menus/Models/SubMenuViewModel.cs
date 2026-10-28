using System;
using System.Collections.Generic;
using System.Text;

namespace EskaCMS.Menus.Models
{

    public class SubMenuViewModel
    {
        public MenuViewModel currentMenu { get; set; }
        public MenuViewModel parentMenu { get; set; }

        public IList<MenuViewModel> menuSiblings { get; set; }
    }
}
