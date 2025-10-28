using System;
using System.Collections.Generic;
using System.Linq;
using WebBanHang.Models;
using WedBanHang.Models;

namespace WebBanHang.Helpers
{
    public static class PromotionHelper
    {
        public static decimal GetFinalPrice(Product product, List<Promotion> promotions)
        {
            decimal originalPrice = product.Price;
            decimal bestDiscountedPrice = originalPrice;
            DateTime now = DateTime.Now;

            foreach (var promo in promotions)
            {
                // Bỏ qua nếu đã hết hạn hoặc chưa đến ngày áp dụng, hoặc đã bị tắt
                if (!promo.IsActive || promo.StartDate > now || promo.EndDate < now)
                    continue;

                bool isApplicable = promo.TargetType switch
                {
                    PromotionTarget.AllProducts => true,
                    PromotionTarget.Categories => promo.PromotionCategories?.Any(c => c.CategoryId == product.CategoryId) == true,
                    PromotionTarget.IndividualProducts => promo.PromotionProducts?.Any(p => p.ProductId == product.Id) == true,
                    _ => false
                };

                if (!isApplicable)
                    continue;

                decimal discounted = promo.DiscountType switch
                {
                    DiscountType.Percentage => originalPrice * (1 - promo.DiscountValue / 100m),
                    DiscountType.FixedAmount => originalPrice - promo.DiscountValue,
                    _ => originalPrice
                };

                if (discounted < bestDiscountedPrice)
                    bestDiscountedPrice = discounted;
            }

            return bestDiscountedPrice < 0 ? 0 : bestDiscountedPrice;
        }

    }
}
