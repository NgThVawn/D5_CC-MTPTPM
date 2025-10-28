using System.ComponentModel.DataAnnotations;
using WedBanHang.Models;

namespace WebBanHang.Models
{
    public class ShoppingCart
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [Range(1, 1000)]
        public int Count { get; set; }  // Số lượng sản phẩm
    }
}
