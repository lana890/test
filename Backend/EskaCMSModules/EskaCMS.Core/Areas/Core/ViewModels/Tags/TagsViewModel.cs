using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Areas.Core.ViewModels.Tags
{
    public class TagsViewModel
    {
        public string Title { get; set; }
    }
    public class TagsEntityViewModel
    {
        public IList<TagsViewModel>  tags { get; set; }
        public EEntity EntityType { get; set; }
        public long EntityId { get; set; }
    }
    public class GetTagsViewModel
    {
        public string Title { get; set; }

        public EEntity? EntityType { get; set; }
        public long EntityId { get; set; }
        public long TagsId { get; set; }

    }


    public class SearchViewModel
    {
        public string SearchValue { get; set; }

        public EEntity? EntityType { get; set; }
    }

    public class SearchEntitesViewModel
    {
        public List<long>? EntityIds { get; set; }

        public EEntity? EntityType { get; set; }
    }
    public class GetAllTagsDto
    {

        public long? Id { get; set; }
        public string Title { get; set; }

        public List<TagsEntityViewModel> TagsEntities { get; set; }

    }

}
