using AutoMapper;
using EskaCMS.Core.Extensions;
using EskaCMS.Entities.Entity.Core;
using EskaCMS.Entities.Entity.Forms;
using EskaCMS.Entities.Entity.WorkFlow;
using EskaCMS.WorkFlow.Models.Request;
using EskaCMS.WorkFlow.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.WorkFlow.MappingProfile
{
    public class WorkFlowMappingProfile : Profile
    {
    

        public WorkFlowMappingProfile()
        {
           

            CreateMap<WorkFlowSetup, WorkFlowDetailsDto>()
                .ForMember(x=>x.CreatedBy , c=>c.MapFrom(x=>x.CreatedBy != null ?  x.CreatedBy.FullName : ""))
                .ForMember(x=>x.ModifiedBy , c=>c.MapFrom(x=>x.ModifiedBy != null ? x.ModifiedBy.FullName : ""))
                .ForMember(x=>x.Name , c=>c.MapFrom((source, dest, destMember, data) => { 
                long cultureId = data.Items.ContainsKey("CultureId") ? (long)data.Items["CultureId"] : 0;

                    return source.WorkFlowDetails.Where(x=>x.CultureId == cultureId).FirstOrDefault()?.Name ?? "";

                }))
                .ReverseMap();
            
            CreateMap<Stage, StageDetailsDto>()
                .ForMember(x => x.CreatedBy, c => c.MapFrom(x => x.CreatedBy != null ? x.CreatedBy.FullName : ""))
                .ForMember(x => x.ModifiedBy, c => c.MapFrom(x => x.ModifiedBy != null ? x.ModifiedBy.FullName : ""))
                .ReverseMap();
            
            CreateMap<Stage, BasicStageInfoDto>();

            CreateMap<WorkFlowItem, WorkFlowItemDetailsDto>()
                .ForMember(x=>x.CreatedBy , c=>c.MapFrom(x=>x.CreatedBy.FullName))
                .ForMember(x=>x.Code , c=>c.MapFrom(x=>x.SubmittedForm != null ? x.SubmittedForm.Code : ""));

            CreateMap<WorkFlowItem, WorkFlowItemList>()
                .ForMember(x => x.CreatedBy, c => c.MapFrom(x => x.CreatedBy.FullName))
                .ForMember(x => x.Code, c => c.MapFrom(x => x.SubmittedForm != null ? x.SubmittedForm.Code : ""))
                .ForMember(x => x.Type, c => c.MapFrom(x => x.CurrentStage.WorkFlow.Type));
                //.ForMember(x=>x.WorkFlowName , c=>c.MapFrom(x=>x.CurrentStage.WorkFlow.Name));

            CreateMap<ItemHistory, ItemHistoryDto>()
                .ForMember(x => x.Stage, x => x.MapFrom(y => y.CurrentStage))
                .ForMember(x => x.CreatedBy, x => x.MapFrom(y => y.CreatedBy != null ? y.CreatedBy.FullName : ""));

            CreateMap<SubmittedForm, SubmittedFormDto>()
                .ForMember(x => x.Attachments, x => x.MapFrom(y => !y.Medias.Any() ? new List<SubmittedFormMediaVM>() : y.Medias.Select(s => new SubmittedFormMediaVM {ContentType = s.Media.ContentType , Filename=s.Media.Filename , Id=s.MediaId , PublicUrl=s.Media.PublicUrl,Size=s.Media.Size } ).ToList()));

            CreateMap<Media, AttachmentDto>();


            CreateMap<WorkFlowDetails, WorkFlowCutlureDetailsDto>() 
                .ForMember(x => x.CutlureCode, c => c.MapFrom(x => x.Culture.CultureId));






        }
    }
}
