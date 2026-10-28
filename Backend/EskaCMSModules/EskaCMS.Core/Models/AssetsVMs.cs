using EskaCMS.Entities.Entity;
using EskaCMS.Infrastructure.Models;
using System;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Core.Models
{
    public class GetAssetsVM
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }


        public EAssetsType Type { get; set; }


        public DateTimeOffset CreationDate { get; set; }


        public DateTimeOffset? ModificationDate { get; set; }


        public string CreatedByName { get; set; }


        public string ModifiedByName { get; set; }
        public EStatus Status { get; set; }

    }

    public class UpsertAssetVM
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
        public string Content { get; set; }

        public EAssetsType Type { get; set; }

    }

    public class GetAssetViewModel
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
        public string Content { get; set; }

        public EAssetsType Type { get; set; }

    }

}
