using static EskaCMS.Entities.Enum.GeneralEnums;

namespace EskaCommerce.Catalog.Areas.Catalog.ViewModels
{
    public class ProductSerialViewModel
    {
        public long Id { get; set; }
        public string CardNo { get; set; }
        public string BatchNo { get; set; }
        public string ControlNo { get; set; }
        public string Status { get; set; }
        public string Expiry { get; set; }
        public string Balance { get; set; }
        public long ProductId { get;  set; }
        public long? OrderId { get; set; }
    }

    public class ProductSerialTemplateViewModel
    {
        public string CardNumber { get; set; }
        public string BatchNumber { get; set; }
        public string ControlNumber { get; set; }
        public string Status { get; set; }
        public string Expiry { get; set; }
        public string Balance { get; set; }
    }
}
