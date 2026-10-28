using AutoMapper;
using EskaCMS.Entities.Entity;
using EskaCMS.Core.Models.SitesCultures;
using EskaCMS.Sites.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EskaCMS.Entities.Entity.Core;

namespace EskaCMS.Sites.Helpers
{
    public class SiteMapperProfile : Profile
    {
        public SiteMapperProfile()
        {
            CreateMap<SitesCulturesViewModel, SitesCultures>().ReverseMap();
        }
        
    }
}
