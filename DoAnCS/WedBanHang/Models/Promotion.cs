using System.ComponentModel.DataAnnotations.Schema;

namespace WebBanHang.Models
{
    public enum DiscountType { Percentage, FixedAmount }
    public enum PromotionTarget { AllProducts, Categories, IndividualProducts }

    public class Promotion
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string? Description { get; set; }

        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }

        public PromotionTarget TargetType { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [NotMapped]
        public TimeSpan Duration => EndDate - StartDate;

        [NotMapped]
        public int DurationMonths { get; set; }

        [NotMapped]
        public int DurationDays { get; set; }

        [NotMapped]
        public int DurationHours { get; set; }

        [NotMapped]
        public int DurationMinutes { get; set; }


        public bool IsActive { get; set; } = true;

        public List<PromotionProduct> PromotionProducts { get; set; } = new();
        public List<PromotionCategory> PromotionCategories { get; set; } = new();
    }


}
