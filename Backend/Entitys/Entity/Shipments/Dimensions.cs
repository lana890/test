using EskaCMS.Entities.Entity.Core;
using EskaCMS.Infrastructure.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCMS.Entities.Entity.Shipments
{
    public class Dimensions:EntityBase
    {
        public string Name { get; set; }
        public decimal Weight { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Length { get; set; }
        public EStatus Status { get; set; }
        public long CreatedById { get; set; }
        public long? ModifiedById { get; set; }

        public User CreatedBy { get; set; }

        public DateTimeOffset CreationDate { get; set; }

        public DateTimeOffset? ModificationDate { get; set; }


        public User ModifiedBy { get; set; }
        public long SiteId { get; set; }
        public  Site Site { get; set; }
        public EDimensionType DimensionType { get; set; }
        public int NumberOfPieces { get; set; }
        public IList<ProductDimensions>? ProductDimensions { get; set; }

        [NotMapped]
        public long ProductId { get; set; }

    }
}
