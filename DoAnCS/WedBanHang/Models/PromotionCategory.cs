using WedBanHang.Models;

namespace WebBanHang.Models
{
    public class PromotionCategory
    {
        public int PromotionId { get; set; }
        public Promotion Promotion { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }

}
