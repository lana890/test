using EskaCMS.HOC.RelatedSlug.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EskaCMS.HOC.RelatedSlug.Services.Interfcaes
{
    public interface IRelatedSlugServices
    {
        Task<List<SlugResultVm>> GetRelatedSlugsInfoBySlug(string slug);

    }
}
