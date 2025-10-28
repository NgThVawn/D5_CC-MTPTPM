using WebBanHang.Models;
using WedBanHang.Models;
namespace WebBanHang.ViewModels
{
    public class ProductWithSalesViewModel
    {
        public Product Product { get; set; }
        public int SoldCount { get; set; }

        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        public decimal FinalPrice { get; set; }
        public Promotion? AppliedPromotion { get; set; }

    }

}
