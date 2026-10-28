using AutoMapper;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models;
using EskaCommerce.Catalog.Areas.Catalog.ViewModels;
using EskaCMS.Entities.Entity;
using EskaCommerce.UserCatalog.Dtos;
using EskaCMS.Entities.Entity.Catalog;
using EskaCMS.Entities.Entity.UserCatalog;
using System.Linq;

namespace EskaCMS.WebHost.Extensions
{
    public partial class CoreMappingProfile : Profile
    {
        public CoreMappingProfile()
        {
            CreateMap<PlanCreate, Plan>()
                .ForMember(x=>x.PlanDetails , y=>y.Ignore()).ReverseMap();
            CreateMap<Plan, PlanOutput>()
                .ForMember(x => x.Name, x => x.MapFrom(x => x.PlanDetails == null ? "" : (x.PlanDetails.Select(d => d.Name).FirstOrDefault() ?? "")))
                .ReverseMap();
            CreateMap<Plan, PlanDetailsVM>()
                .ForMember(x=>x.Versions,y=>y.Ignore());
            
            CreateMap<PlansVersions, PlansVersionsCreate>().ReverseMap();
            CreateMap<PlansVersions, PlansVersionsOutput>().ReverseMap();
            //CreateMap<PlansVersionsVM, PlansVersions>()
            //    .ForMember(x => x.PlanProductVersions, x => x.Ignore())
            //    .ReverseMap();



            CreateMap<PlansVersions, PlansVersionsVM>()
                .ForMember(x => x.PlanProducts, x => x.Ignore());
            CreateMap<PlansVersions, PlansVersionsOutput>()
                .ForMember(x=>x.PlanProducts , x => x.MapFrom(x => x.PlanProductVersions));
            CreateMap<PlanProductVersion, PlanProductVersionOutput>()

                .ForMember(x => x.ProductName, x => x.MapFrom(x => x.Product == null ? "" : (x.Product.ProductDetails.Select(d => d.Name).FirstOrDefault() ?? "")))
                .ReverseMap();
            CreateMap<PlanProductVersion, PlanProductVersionCreate>().ReverseMap();
            CreateMap<UserPlans, UserPlansCreate>().ReverseMap();
            CreateMap<UserPlans, UserPlansOutput>().ReverseMap();
            CreateMap<ProviderTimeSlot, ProviderTimeSlotCreateVM>().ReverseMap();
            CreateMap<EskaCMS.Entities.Entity.Core.MediaFolders, MediaStructureItem>().ReverseMap(); 
        }
    }
}
