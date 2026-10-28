using AutoMapper;
using EskaCMS.Entities.Entity.Comments;
using EskaCMS.Entities.Enum;
using EskaCommerce.Comments.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCommerce.Comments.ProfileComment
{
    public class MappingComment : Profile
    {
        public MappingComment()
        {
            CreateMap<Comment, CommentAdminViewModel>()
            .ForMember(x => x.Type, x => x.MapFrom(t => t.ParentId == null ? "Comment" : "Reply"))
            .ForMember(x => x.CommenterName, x => x.MapFrom(t => t.User.FullName))
            .ForMember(x => x.UserId, x => x.MapFrom(t => t.UserId))
            .ForMember(x => x.CreationDate, x => x.MapFrom(t => t.CreatedOn));

            CreateMap<Comment, CommentWebViewModel>()
         .ForMember(x => x.CommenterName, x => x.MapFrom(t => t.User.FullName))
         .ForMember(x => x.CreationDate, x => x.MapFrom(t => t.CreatedOn))
         .ForMember(x=>x.Replies,x=>x.MapFrom(d=>d.Replies.Where(r => r.Status == CommentStatus.Approved).OrderByDescending(x=>x.Id).ToList()));

        }
    }
}
