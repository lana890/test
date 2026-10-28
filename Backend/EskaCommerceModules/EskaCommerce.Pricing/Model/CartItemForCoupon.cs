namespace EskaCommerce.Pricing.Services
{
    public class CartItemForCoupon
    {
        public long ProductId { get; set; }

        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ProductName { get; set; }
    }
}
