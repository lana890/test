using AutoMapper;
using EskaCMS.Entities.Entity;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels.Category.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCommerce.Catalog.Mapping.CategoryProfile
{
    public class CategoryProfileMapper : Profile
    {
       public CategoryProfileMapper()
      {
            CreateMap<Category, CategoryAdminViewModel>()
                .ForMember(x=>x.ThumbnailImage,x=>x.MapFrom(x=>x.ThumbnailImage==null?new Media(): x.ThumbnailImage))
                .ForMember(x=>x.CoverImage,x=>x.MapFrom(x=>x.CoverImage==null?new Media(): x.CoverImage))
                .ForMember(x=>x.ParentId,x=>x.MapFrom(x=>x.ParentId))
                .ForMember(x=>x.ParentName,x=>x.MapFrom(x=>x.Parent==null?"": x.Parent.CategoryDetails.OrderByDescending(x=>x.CultureId).FirstOrDefault().Name))
                .ForMember(x=>x.WarehouseIds,x=>x.MapFrom(x=>x.CategoryWarehouses==null?new List<long>(): x.CategoryWarehouses.Select(x=>x.WarehouseId).ToList()));

            CreateMap<Media, MediaViewModel>()
             .ForMember(x => x.FileName, c => c.MapFrom(f =>  f.Filename))
             .ForMember(x => x.PublicUrl, c => c.MapFrom(f =>  f.PublicUrl));
            
            CreateMap<CategoryDetails, CategoryDetailsAdminViewModel>()
               .ForMember(x => x.CultureCode, c => c.MapFrom(f => f.Culture == null ? "" : f.Culture.CultureId))
               .ForMember(x => x.Slug, c => c.MapFrom(f => f.SlugInfo == null ? "" : f.SlugInfo.Slug));
            
            CreateMap<Category, AllCategoryViewModel>();
            
            CreateMap<CategoryDetails, CategoryDetailsViewModel>()
                 .ForMember(x => x.Culture, c => c.MapFrom(f => f.Culture == null ? new SitesCultures() : f.Culture))
                 .ForMember(x => x.SlugInfo, c => c.MapFrom(f => f.SlugInfo == null ? new SlugInfo() : f.SlugInfo));

            CreateMap<Category, CategoryWebViewModel>()
                .ForMember(x => x.ThumbnailImage, x => x.MapFrom(y => y.ThumbnailImage ?? new Media()))
                .ForMember(x => x.CoverImage, x => x.MapFrom(y => y.CoverImage ?? new Media()))
                .ForMember(x => x.CultureCode, x => x.MapFrom(y => y.CategoryDetails.Select(x => x.Culture).Select(x=>x.CultureId).FirstOrDefault() ?? ""))
                .ForMember(x => x.MetaDescription, x => x.MapFrom(y => y.CategoryDetails.Select(x => x.MetaDescription).FirstOrDefault() ?? ""))
                .ForMember(x => x.CategoryTemplateId, x => x.MapFrom(y => y.CategoryDetails.Select(x => x.CategoryTemplateId).FirstOrDefault()))
                .ForMember(x => x.Description, x => x.MapFrom(y => y.CategoryDetails.Select(x => x.Description).FirstOrDefault() ?? ""))
                .ForMember(x => x.Slug, x => x.MapFrom(y => y.CategoryDetails.Select(x=>x.SlugInfo).Select(x=>x.Slug).FirstOrDefault() ?? ""))
                .ForMember(x => x.MetaTitle, x => x.MapFrom(y => y.CategoryDetails.Select(x => x.MetaTitle).FirstOrDefault() ?? ""))
                .ForMember(x => x.MetaKeywords, x => x.MapFrom(y => y.CategoryDetails.Select(x => x.MetaKeywords).FirstOrDefault() ?? ""))
                .ForMember(x => x.Name, x => x.MapFrom(y => y.CategoryDetails.Select(x=>x.Name).FirstOrDefault() ?? ""))
                .ForMember(x => x.CultureId, x => x.MapFrom(x => x.CategoryDetails.Select(x=>x.CultureId).FirstOrDefault()??0))
            ;
            //bad Performance
            //CreateMap<Category, CategoryWebViewModel>()
            //  .ForMember(x => x.ThumbnailImage, x => x.MapFrom(x => x.ThumbnailImage == null ? new Media() : x.ThumbnailImage))
            //  .ForMember(x => x.CultureCode, x => x.MapFrom(x => x.CategoryDetails == null ? "" : x.CategoryDetails.FirstOrDefault().Culture == null ? "" : x.CategoryDetails.FirstOrDefault().Culture.CultureId))
            //  .ForMember(x => x.MetaDescription, x => x.MapFrom(x => x.CategoryDetails == null ? "" : x.CategoryDetails.FirstOrDefault().MetaDescription))
            //  .ForMember(x => x.CategoryTemplateId, x => x.MapFrom(x => x.CategoryDetails == null ? null : x.CategoryDetails.FirstOrDefault().CategoryTemplateId))
            //  .ForMember(x => x.CultureId, x => x.MapFrom(x => x.CategoryDetails == null ? null : x.CategoryDetails.FirstOrDefault().CultureId))
            //  .ForMember(x => x.Description, x => x.MapFrom(x => x.CategoryDetails == null ? null : x.CategoryDetails.FirstOrDefault().Description))
            //  .ForMember(x => x.Slug, x => x.MapFrom(x => x.CategoryDetails == null ? "" : x.CategoryDetails.FirstOrDefault().SlugInfo == null ? "" : x.CategoryDetails.FirstOrDefault().SlugInfo.Slug))
            //  .ForMember(x => x.MetaTitle, x => x.MapFrom(x => x.CategoryDetails == null ? "" : x.CategoryDetails.FirstOrDefault().MetaTitle))
            //  .ForMember(x => x.MetaKeywords, x => x.MapFrom(x => x.CategoryDetails == null ? "" : x.CategoryDetails.FirstOrDefault().MetaKeywords))
            //  .ForMember(x => x.Name, x => x.MapFrom(x => x.CategoryDetails == null ? "" : x.CategoryDetails.FirstOrDefault().Name));

            CreateMap<Category, CategoryListVm>().ForMember(x => x.CategoryDetails, x => x.MapFrom(x => x.CategoryDetails.OrderByDescending(x => x.CultureId).FirstOrDefault()));
            
            CreateMap<CategoryDetails, CategoryDetailsListViewModel>()
           .ForMember(x => x.CultureCode, c => c.MapFrom(f => f.Culture == null ? "" : f.Culture.CultureId));


        }
    }
}
