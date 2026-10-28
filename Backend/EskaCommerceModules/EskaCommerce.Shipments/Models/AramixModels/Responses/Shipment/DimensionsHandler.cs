using EskaCMS.Entities.Entity.Shipments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Shipments.Models.AramixModels.Responses.Shipment
{
    public class DimensionsHandler
    {
        public double Height { get; set; }
        public double Width { get; set; }
        public double Length { get; set; }

        public long ProductId { get; set; }
        public int Quentity { get; set; }
    }

    public class CartonDimensionsHandler
    {
        public long Id { get; set; }
        public decimal Height { get; set; }
        public decimal Width { get; set; }
        public decimal Length { get; set; }
        public decimal Weight { get; set; }

        public  List<long> ProductIds { get; set; }
        public int NumberOfPieces { get; set; }
        public EDimensionType DimensionType { get; set; }
    }
    public class SelectedCarton
    {
        public CartonDimensionsHandler Carton { get; set; }
        public int  Count { get; set; }
       
    }
    public class SelectedCartonViewModel
    {
        public long DimensionId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }

    }
    public class ResultCartonViewModel
    {
        public long DimensionId { get; set; }
        public int Quantity { get; set; }

    }

}
