namespace WebBanHang.ViewModels
{
    public class CheckoutItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImage { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }

        public decimal FinalPrice { get; set; }
        public decimal Total => FinalPrice * Quantity;
    }
}
