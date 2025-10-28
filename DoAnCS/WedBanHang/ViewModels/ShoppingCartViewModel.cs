namespace WebBanHang.ViewModels
{
    public class ShoppingCartItemViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public bool IsAvailable { get; set; }
        public int StockQuantity { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal Total => FinalPrice * Quantity;
       
    }

    public class ShoppingCartViewModel
    {
        public List<ShoppingCartItemViewModel> ActiveItems { get; set; }
        public List<ShoppingCartItemViewModel> InactiveItems { get; set; }

        public decimal GrandTotal => ActiveItems?.Sum(x => x.Total) ?? 0;
    }



}
