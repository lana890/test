using AutoMapper;
using EskaCMS.Entities.Entity.LoyaltyPoints;
using EskaCommerce.LoyaltyPoints.Models.Request;
using EskaCommerce.LoyaltyPoints.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.LoyaltyPoints.MappingProfile
{
    public class LoyaltyPointsMappingProfile : Profile
    {
        public LoyaltyPointsMappingProfile()
        {

            CreateMap<LoyaltyPointsSetup,LoyaltySetupDto>()
                .ReverseMap(); 
            
            CreateMap<LoyaltyPointsSetup,LoyaltyPointsSetupHistory>()
                .ForMember(x=>x.LoyaltyPointsSetupId , c=> c.MapFrom(l=>l.Id))
                .ForMember(x=>x.Id, c=> c.Ignore());


            CreateMap<LoyaltyPointsSetupHistory, LoyaltyHistoryDto>()
                .ForMember(x => x.CreatedBy, c => c.MapFrom(x => x.CreatedBy.FullName))
                .ForMember(x => x.Status, c => c.MapFrom(s => s.Status.ToString()))
                .ForMember(x => x.LoyalitySource, c => c.MapFrom(s => s.LoyalitySource.ToString()))
                .ForMember(x => x.ProductName, c => c.MapFrom(x => x.ProductId.HasValue ? x.Product.ProductDetails.Where(y => y.ProductId == x.ProductId).FirstOrDefault().Name : null));

            CreateMap<LoyaltyPointsSetup, LoyaltySetupDetailsDto>()
                 .ForMember(x => x.CreatedBy, c => c.MapFrom(x => x.CreatedBy.FullName))
                .ForMember(x => x.Status, c => c.MapFrom(s => s.Status.ToString()))
                .ForMember(x => x.LoyalitySource_Converted, c => c.MapFrom(s => s.LoyalitySource.ToString()))
                .ForMember(x => x.ModificationDate, c => c.MapFrom(s => s.ModificationDate.HasValue ? s.ModificationDate : null))
                .ForMember(x => x.ModifiedBy, c => c.MapFrom(s => s.ModifiedById.HasValue ? s.ModifiedBy.FullName : null))
                 .ForMember(x => x.ProductName, c => c.MapFrom(x => x.ProductId.HasValue ? x.Product.ProductDetails.Where(y => y.ProductId == x.ProductId).FirstOrDefault().Name : null));

            CreateMap<PointsConversionConfig, PointsConfigDetailsDto>()
                .ForMember(x => x.CreatedBy, c => c.MapFrom(l => l.CreatedBy.FullName))
                .ForMember(x => x.ModifiedBy, c => c.MapFrom(l => l.ModifiedBy.FullName));


            CreateMap<LoyaltyWalletTransactions, LoyaltyTransactionsDto>()
                .ForMember(x=>x.OperationType , x=>x.MapFrom(y=>y.OperationType.ToString()));
            

        }
        
    }
}
