using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

using EskaCMS.Entities.Entity;
using static EskaCMS.Entities.Enum.GeneralEnums;
using EskaCMS.Core.Extensions;
using EskaCMS.Filters.ActionFilters;
using EskaCMS.Menus.Models;
using EskaCMS.Menus.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.APIs.Controllers
{
    [Route("api/Menus")]
    [ApiController]
    //  [Authorize]
    public class MenuController : ControllerBase
    {

        private readonly IMenu _MenuRepository;
        private readonly IMenuRole _MenuRolesRepository;
        private readonly IWorkContext _workcontext;

        public MenuController(IMenu MenuRepository, IMenuRole MenuRolesRepository, IWorkContext workcontext)
        {
            _MenuRepository = MenuRepository;
            _MenuRolesRepository = MenuRolesRepository;
            _workcontext = workcontext;
        }



        [HttpGet]
        [Route("GetMenuBreadCrumb/{MenuId}")]
        // [Authorize]
        public async Task<IActionResult> GetMenuBreadCrumb(long MenuId)
        {
            try
            {

                return Ok(await _MenuRepository.GetMenuBreadCrumb(MenuId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        //[HttpGet]
        //[Route("GetMenuCategories")]
        //public async Task<IActionResult> GetSiteMenus()
        //{
        //    try
        //    {
        //        var SiteId = long.Parse(Request.Headers["SiteId"].ToString());
        //        return Ok(await _MenuRepository.GetSiteMenuCategories(SiteId));
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc);
        //    }
        //}



        //[HttpPost]
        //[Route("AddSiteMenuCategory")]
        //public async Task<IActionResult> AddSiteMenu(MenuViewModel vm)
        //{
        //    try
        //    {

        //        var SiteId = int.Parse(Request.Headers["siteId"].ToString());
        //        var UserId = int.Parse(Request.Headers["UserId"]);
        //        var result = await _MenuRepository.AddMenuCategory(SiteId, UserId, vm.Name);

        //        return Ok(result);
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc);
        //    }
        //}


        //[HttpPost]
        //[Route("EditSiteMenuCategory")]
        //public async Task<IActionResult> EditMenu(MenuViewModel menu)
        //{
        //    try
        //    {
        //        var UserId = int.Parse(Request.Headers["UserId"]);
        //        var result = await _MenuRepository.EditSiteMenuCategory(UserId, menu);

        //        return Ok(result);
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc);
        //    }
        //}

        [HttpGet]
        [Route("GetMenusForWeb/{MenuGroupId}/{MenuId}/{Relation}")]
        public async Task<IActionResult> GetMenusForWeb(long MenuGroupId, long MenuId, EMenuRelation Relation)
        {
            try
            {


                var result = await _MenuRepository.GetMenusForWeb(MenuGroupId, MenuId, Relation);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpPost]
        [Route("DuplicateMenuCategory")]
        public async Task<IActionResult> DuplicateMenuCategory(MenuViewModel vm)
        {
            try
            {

                vm.SiteId = int.Parse(Request.Headers["siteId"].ToString());
                vm.UserId = await _workcontext.GetCurrentUserId();

                var result = await _MenuRepository.DuplicateMenuCategory(vm);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetMenusByCategoryId/{CategoryId}")]
        [Authorize]
        public async Task<IActionResult> GetMenusByCategoryId(long CategoryId)
        {
            try
            {

                return Ok(await _MenuRepository.GetMenusByCategoryIdForWebAsync(CategoryId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetMenusByGroupId/{GroupId}/{ActiveSlug?}")]
        [Authorize]
        public async Task<IActionResult> GetMenusByGroupId(long GroupId, string? ActiveSlug)
        {
            try
            {
                // need to remove active slug
                return Ok(await _MenuRepository.GetMenusByGroupId(GroupId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        //[HttpPost]
        //[Route("DeleteSiteMenuCategory/{id}")]
        //public async Task<IActionResult> DeleteSiteMenuCategory(int id)
        //{
        //    try
        //    {
        //        var UserId = int.Parse(Request.Headers["UserId"]);
        //        var result = await _MenuRepository.DeleteSiteMenuCategory(UserId, id);

        //        return Ok(result);
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc);
        //    }
        //}




        [HttpGet]
        [Route("GetMenusByCategoryId/{id}/{status}")]
        [Authorize]
        public async Task<IActionResult> GetSiteCategoryMenu(long id, EStatus status)
        {
            try
            {

                SourceMenuType sourceType = (SourceMenuType)int.Parse(Request.Headers["source"]);

                return Ok(await _MenuRepository.GetMenusByCategoryId(id, status, await _workcontext.GetCurrentUserId(), sourceType));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetTabsByCategoryId/{CatId}")]
        [Authorize]
        public IActionResult GetTabsByCategoryId(long CatId)
        {
            try
            {
                return Ok(_MenuRepository.GetTabsByCategoryId(CatId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        //[HttpGet]
        //[Route("GetSubMenu/{catId}/{id}/{idType}")]
        //public async Task<IActionResult> GetSubMenu(long catId, long id, string idType)
        //{
        //    try
        //    {
        //        return Ok(await _MenuRepository.GetSubMenu(catId, id, idType));
        //    }
        //    catch (Exception exc)
        //    {
        //        return BadRequest(exc);
        //    }
        //}

        [HttpGet]
        [Route("GetMenuByIdWithChilds/{id}")]
        public async Task<IActionResult> GetMenuByIdWithChilds(long id)
        {
            try
            {
                return Ok(await _MenuRepository.GetMenuByIdWithChilds(id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetMenusBySiteAndMenuType/{Type}")]
        public async Task<IActionResult> GetMenusBySiteAndMenuType(EMenuTypes Type)
        {

            try
            {
                var SiteId = long.Parse(Request.Headers["siteId"].ToString());
                return Ok(await _MenuRepository.GetMenusBySiteIdAndMenuType(SiteId, Type));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }



        [HttpGet]
        [Route("GetMenuById/{id}")]
        public async Task<IActionResult> GetMenuById(long id)
        {
            try
            {
                return Ok(await _MenuRepository.GetMenuById(id));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetParentMenus/{MenuId}")]
        public async Task<IActionResult> GetParentMenus(long MenuId)
        {
            try
            {
                return Ok(await _MenuRepository.GetParentMenus(MenuId));
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("AddMenu")]
        public async Task<IActionResult> AddCategoryMenu(MenuViewModel menu)
        {
            try
            {

                menu.SiteId = await _workcontext.GetCurrentSiteIdAsync();
                menu.UserId = await _workcontext.GetCurrentUserId();

                var result = await _MenuRepository.AddMenu(menu);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpPost]
        [Route("AddEditMenuForCategories")]
        public async Task<IActionResult> AddEditMenuForCategories(List<MenuViewModel> menu)
        {
            try
            {
                var SiteId = await _workcontext.GetCurrentSiteIdAsync();
                var UserId = await _workcontext.GetCurrentUserId();
                var result = await _MenuRepository.AddEditMenu(menu, SiteId, UserId);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpGet]
        [Route("GetMenuByCategoryCultureParentId/{CultureParentId}")]
        public async Task<IActionResult> GetMenuByCategoryCultureParentId(long CultureParentId)
        {
            try
            {

                var result = await _MenuRepository.GetMenuByCategoryCultureParentId(CultureParentId);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }
        [HttpPost]
        [Route("AddEditMenuForMultiPage")]
        public async Task<IActionResult> AddEditMenuForMultiPage(List<MenuViewModel> menu)
        {
            try
            {
                var SiteId = await _workcontext.GetCurrentSiteIdAsync();
                var UserId = await _workcontext.GetCurrentUserId();
                var result = await _MenuRepository.AddEditMenu(menu, SiteId, UserId);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("EditMenu/{id}")]
        public async Task<IActionResult> EditMenu(int id, MenuViewModel menu)
        {
            try
            {
                var UserId = Convert.ToInt32(await _workcontext.GetCurrentUserId());
                var result = await _MenuRepository.EditMenu(UserId, id, menu);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("EditMenuStatus/{id}/{statusId}")]
        public async Task<IActionResult> EditMenuStatus(int id, int statusId)
        {
            try
            {
                var UserId = Convert.ToInt32(await _workcontext.GetCurrentUserId());
                var result = await _MenuRepository.EditMenuStatus(UserId, id, statusId);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpPost]
        [Route("publishAllMenus")]
        public async Task<IActionResult> publishAllMenus(publishMenuViewModel menuObj)
        {
            try
            {
                var menuList = await _MenuRepository.publishAllMenus(menuObj, await _workcontext.GetCurrentUserId(), await _workcontext.GetCurrentSiteIdAsync());

                return Ok(menuList);
                //Page.CompanyId = long.Parse(Request.Headers["CompanyId"].ToString());
            }
            catch (Exception exc)
            {
                return BadRequest(exc.Message);
            }
        }


        //End bulk update,delete,publish

        [HttpDelete]
        [Route("DeleteMenu/{id}")]
        public async Task<IActionResult> DeleteMenu(int id)
        {
            try
            {
                //  var UserId = int.Parse(Request.Headers["UserId"]);
                var result = await _MenuRepository.DeleteMenu(await _workcontext.GetCurrentUserId(), id);

                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        [HttpGet]
        [Route("GetMenuAssignedRoles/{Id}")]
        public async Task<IActionResult> GetMenuAssignedRoles(long Id)
        {
            try
            {

                var result = await _MenuRolesRepository.GetMenuAssignedRoles(Id, await _workcontext.GetCurrentSiteIdAsync());
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Route("GetMenuByParentPageId/{ParentPageId}")]
        public async Task<IActionResult> GetMenuByParentPageId(long ParentPageId)
        {
            try
            {

                var result = await _MenuRepository.GetMenuByParentPageId(ParentPageId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }



        [HttpPost]
        [Route("SaveMenuAssginedRoles")]
        public async Task<IActionResult> SaveMenuAssginedRoles(MenuRolesViewModel vm)
        {
            try
            {
                vm.UserId = await _workcontext.GetCurrentUserId();
                var result = await _MenuRolesRepository.SaveMenuAssginedRoles(vm);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        //added by Mahmoud to save order of the menu
        [HttpPost]
        [Route("SwapAndAddMenu")]
        public async Task<IActionResult> SwapAndAddMenu(MenuOrderViewModel orderModel)
        {
            try
            {

                var result = await _MenuRepository.SwapAndAddMenu(orderModel, await _workcontext.GetCurrentSiteIdAsync());
                if (result == true)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest("Could not find the required data");
                }

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("ChangeMenuSort")]
        public async Task<IActionResult> ChangeMenuSort(MenuGroupWithSortVM Model)
        {
            try
            {

                var result = await _MenuRepository.ChangeMenuSort(Model);
                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        #region MenuGroups Apis


        [HttpGet]
        [Authorize]
        [Route("GetMenuGroups")]
        public async Task<IActionResult> GetMenuGroups()
        {
            try
            {
                long SiteId = await _workcontext.GetCurrentSiteIdAsync();

                var result = await _MenuRepository.GetMenuGroupsList(SiteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Authorize]
        [Route("GetMenuGroupsByCategoryId/{CategoryId}")]
        public async Task<IActionResult> GetMenuGroupsByCategoryId(long CategoryId)
        {
            try
            {
                long SiteId = await _workcontext.GetCurrentSiteIdAsync();

                var result = await _MenuRepository.GetMenuGroupsList(SiteId, CategoryId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpGet]
        [Authorize]
        [Route("GetMenuGroupById/{GroupId}")]
        public async Task<IActionResult> GetMenuGroupById(long GroupId)
        {
            try
            {
                long SiteId = await _workcontext.GetCurrentSiteIdAsync();

                var result = await _MenuRepository.GetMenuGroupById(GroupId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }


        [HttpPost]
        [Authorize]
        [Route("UpsertMenuGroup")]
        public async Task<IActionResult> UpsertMenuGroup(UpsertMenuGroupVM NewMenuGroup)
        {
            try
            {

                long SiteId = await _workcontext.GetCurrentSiteIdAsync();
                long UserId = await _workcontext.GetCurrentUserId();

                var result = await _MenuRepository.UpsertMenuGroup(NewMenuGroup, SiteId, UserId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }



        [HttpPost]
        [Authorize]
        [Route("CloneMenuGroup")]
        public async Task<IActionResult> CloneMenuGroupWithMenuItems(CloneMenuGroupVM model)
        {
            try
            {

                long SiteId = await _workcontext.GetCurrentSiteIdAsync();
                long UserId = await _workcontext.GetCurrentUserId();

                var result = await _MenuRepository.CloneMenuGroup(model, UserId, SiteId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }





        [HttpPost]
        [Authorize]
        [Route("AddMenuItemToGroup")]
        public async Task<IActionResult> AddMenuItemToGroup(AddMenuItemToGroupVM NewMenuGroup)
        {
            try
            {
                long SiteId = await _workcontext.GetCurrentSiteIdAsync();
                long UserId = await _workcontext.GetCurrentUserId();

                var result = await _MenuRepository.AddMenuItemToGroup(NewMenuGroup, UserId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }



        [HttpGet]
        [Authorize]
        [Route("DeleteMenuGroup/{GroupId}")]
        public async Task<IActionResult> DeleteMenuGroup(long GroupId)
        {
            try
            {
                long UserId = await _workcontext.GetCurrentUserId();

                var result = await _MenuRepository.UpdateMenuGroupStatus(GroupId, EStatus.Deleted, UserId);
                return Ok(result);
            }
            catch (Exception exc)
            {
                return BadRequest(exc);
            }
        }

        #endregion


    }
}