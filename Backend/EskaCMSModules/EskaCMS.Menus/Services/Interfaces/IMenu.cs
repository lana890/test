
using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Models;
using EskaCMS.Menus.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Entities.Entity.Menus;

namespace EskaCMS.Menus.Services.Interfaces
{
    public interface IMenu
    {
        //Task<List<Category>> GetSiteMenuCategories(long SiteId);

      //  Task<long> AddMenuCategory(int siteId, int userId, string name);
       // Task<long> EditSiteMenuCategory( int userId, MenuViewModel menu);
        //Task<long> DeleteSiteMenuCategory(  int userId, long id);
        Task<List<MenuViewModel>> GetMenusByCategoryId(long CatId, EStatus status, long userId, SourceMenuType sourceType);
          //Task<SubMenuViewModel> GetSubMenu(long catId, long id, string idType);
        Task<IList<MenuViewModel>> GetMenusBySiteIdAndMenuType(long siteId, EMenuTypes Type);
        Task<MenuViewModel> GetMenuById(long id);
        Task<MenuViewModel> GetMenuByIdWithChilds(long id);
        Task<MenuViewModel> GetParentMenus(long MenuId);
        Task<long> AddMenu(MenuViewModel menu);
        Task<bool> AddEditMenu(List<MenuViewModel> menus, long SiteId , long UserId );

        Task<bool> EditMenu(int userId, long id, MenuViewModel menu);
        //bulk

        //end bulk
        Task<bool> EditMenuStatus(int userId, long id, int statusId);
        Task<List<MenuBreadCrumbVM>> GetMenuBreadCrumb(long id);


        Task<long> DuplicateMenuCategory( MenuViewModel menu);

        Task<bool> DeleteMenu(long userId, long id);
        //added by Mahmoud to save order of the menu
        Task<bool> SwapAndAddMenu(MenuOrderViewModel menuOrder, long siteId);
        Task<bool> publishAllMenus(publishMenuViewModel menuObj, long UserId, long SiteId);
        List<TabViewModel> GetTabsByCategoryId(long CatId);

        Task<List<MenuViewModel>> GetMenusByCategoryIdForWebAsync(long CatId);

        Task<List<Menu>> ChangeMenuSort(MenuGroupWithSortVM Model);
        Task<List<Menu>> GetMenuByParentPageId(long PageId);
        Task<List<MenuViewModel>> GetMenusByGroupId(long CatId); 

        Task<List<MenuViewModel>> GetMenusForWeb(long MenuGroupId, long TargetMenuId, EMenuRelation Relation );
        #region MenuGroups
        Task<MenuGroupVM> CloneMenuGroup(CloneMenuGroupVM model, long UserId, long SiteId);

        Task<List<MenuGroupVM>> GetMenuGroupsList(long SiteId, long CategoryId = 0);

        Task<MenuGroupVM> GetMenuGroupById(long GroupId);

        Task<MenuGroupVM> UpsertMenuGroup(UpsertMenuGroupVM Model, long SiteId, long UserId);

        Task<MenuGroup> UpdateMenuGroupStatus(long GroupId, EStatus NewStatus, long UserId);

        Task<Menu> AddMenuItemToGroup(AddMenuItemToGroupVM Model, long UserId);
        Task<List<Menu>> GetMenuByCategoryCultureParentId(long cultureParentId);

        #endregion

    }
}
