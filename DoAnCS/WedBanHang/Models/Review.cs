using System.ComponentModel.DataAnnotations;
using WedBanHang.Models;

namespace WebBanHang.Models
{
    public class Review
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }

        [Range(1, 5, ErrorMessage = "Chọn số sao từ 1 đến 5")]
        public int Rating { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        public List<ReviewImage>? Images { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int OrderId { get; set; }
        public bool IsReported { get; set; } = false;

    }
}

