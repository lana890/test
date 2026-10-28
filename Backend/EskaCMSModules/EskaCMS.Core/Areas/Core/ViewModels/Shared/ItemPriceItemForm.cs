namespace EskaCMS.Core.Areas.Core.ViewModels
{
    public class ItemPriceItemForm
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public decimal? NewPrice { get; set; }

        public decimal? NewOldPrice { get; set; }
    }
}
