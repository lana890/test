using AutoMapper;
using EskaCMS.Entities.Entity.DynamicForms;
using EskaCMS.Infrastructure.Pagination;
using EskaCMS.DynamicForms.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Org.BouncyCastle.Asn1.X509.Qualified;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.DynamicForms.Mapping
{
    public class PricingMappingProfile : Profile
    {
        public PricingMappingProfile()
        {
            //CreateMap<FormItem, PricingMatricesWithClassificationsVM>().ReverseMap();
            CreateMap<FormItem, FormItemVM>().ReverseMap();
            CreateMap<Classifications, ClassificationVM>().ReverseMap();
            CreateMap<FormField, FormFieldVM>().ReverseMap();
            CreateMap(typeof(PagedResult<>), typeof(PagedResult<>));
            CreateMap<FormItemDetails, FormItemDetailsWithClassiffictionVM>().ReverseMap();//PricingMatrixDetailsWithClassiffictionVM
                                                                                           //   CreateMap<FormItem, PricingMatrixClassificationsVM>().ReverseMap();                                                                                   //  CreateMap< FormItem, PricingMatrixDto>().ReverseMap();
            CreateMap<FormItem, FormItemDto>().ReverseMap();
            //.ForMember(x=>x.FormItemDetails, opt=>opt.MapFrom(x => x.PricingMatrixDetailsWithClassiffictionVM)).ReverseMap();

            //     .ForMember(x => x., opt => opt.MapFrom(x => x.FormItemDetails)).ReverseMap();
            CreateMap<FormItemDetails, FormItemDetailsVM>().ReverseMap();

            CreateMap<FormField, FormFieldVM>().ReverseMap();


            CreateMap<FormFieldGroup, FormFieldGroupVM>().ReverseMap();

            CreateMap<EskaCMS.Entities.Entity.Core.Media, FormItemMediaVM>().ReverseMap();

            CreateMap<FormItem, FormItemSummaryVM>().ReverseMap();

            CreateMap<FormItemDto, FormItemSummaryVM>().ReverseMap();

            CreateMap<FormField, FromFieldDto>().ReverseMap();



            CreateMap<FormFieldDetails, FormFieldDetailsVMs>().ReverseMap();
            CreateMap<FormFieldDetails, FormFieldDetailsMainInfoVM>().ReverseMap();
            CreateMap<FormField, FormFieldMainInfoVM>().ReverseMap();
            CreateMap<Category, CategoryVM>().ReverseMap();
            CreateMap<CategoryDetails, ChildDetailsVM>().ReverseMap();
            CreateMap<CategoryDetails, ChildDetailsMainInfoVM>().ReverseMap();



            CreateMap<Classifications, FormItemsClassificationVM>()
                .ForMember(x => x.CriteriaCount, x => x.MapFrom(y => y.CriteriaList.Count)).ReverseMap();




        }

    }
}
