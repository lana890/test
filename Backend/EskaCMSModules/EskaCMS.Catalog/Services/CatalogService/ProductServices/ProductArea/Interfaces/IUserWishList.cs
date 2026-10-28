using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Catalog.Services.CatalogService.ProductServices.ProductArea.Interfaces
{
    public interface IUserWishList
    {
        Task<ProductViewModelForWeb> GetWishList(WishListProductVM wishListProductVM);
        Task<bool> AddWishListItem(long productId);
        Task<bool> RemoveProductFromWishList(long Id);
        Task<ProductViewModelForWeb> GetSimilarItemsForWishlist(WishListProductVM wishListProductVM);
    }
}
