using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Enum;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Areas.Catalog.Controllers.ProductControllers.ProductControllerArea
{
    [Route("api/UserWishList")]
    [Authorize]
    public class UserWishListController : ControllerBase
    {

        private readonly IUserWishList _userWishList;
        public UserWishListController(IUserWishList userWishList)
        {

            _userWishList = userWishList;

        }
        [HttpPost]
        [Route("GetWishList")]
        public async Task<IActionResult> GetWishList([FromBody] WishListProductVM wishListProductVM)
        {
            try
            {

                return Ok(await _userWishList.GetWishList(wishListProductVM));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("AddWishListItem/{ProductId}")]
        public async Task<IActionResult> AddWishListItem([FromRoute] long ProductId)
        {
            try
            {

                return Ok(await _userWishList.AddWishListItem(ProductId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("RemoveProductFromWishList/{Id}")]
        public async Task<IActionResult> RemoveProductFromWishList(long Id)
        {
            try
            {

                return Ok(await _userWishList.RemoveProductFromWishList(Id));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("GetSimilarItemsForWishlist")]
        public async Task<IActionResult> GetSimilarItemsForWishlist([FromBody]WishListProductVM wishListProductVM)
        {
            try
            {
                return Ok(await _userWishList.GetSimilarItemsForWishlist(wishListProductVM));
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    } 
}
