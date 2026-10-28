using EskaCMS.Entities.Entity.Shipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Shipments.Models
{
    public class DimensionViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public decimal Weight { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Length { get; set; }
        public EStatus Status { get; set; }
        public string CreatedByName { get; set; }
        public DateTimeOffset CreationDate { get; set; }
        public string ModifiedByName { get; set; }
        public EDimensionType DimensionType { get; set; }
        public int NumberOfPieces { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public IList<long>? ProductIds { get; set; }
    }
    public class DimensionCreateUpdateViewModel
    {
        public long? Id { get; set; }
        public string Name { get; set; }
        public decimal Weight { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Length { get; set; }
        public EStatus Status { get; set; }
        public EDimensionType DimensionType { get; set; }
        public int NumberOfPieces { get; set; }
        public List<long>? ProductIds { get; set; }

    }

    public class DimensionFilterationViewModel
    {

        public string Name { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }
    }


    public class DimensionProductViewModel
    {

        public string Name { get; set; }
        public long ProductId { get; set; }
    }

}
