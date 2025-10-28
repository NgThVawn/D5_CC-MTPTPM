using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WedBanHang.Models;

namespace WebBanHang.Repositories
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly ApplicationDbContext _context;

        public PromotionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Promotion>> GetAllAsync()
        {
            return await _context.Promotions
                .Include(p => p.PromotionProducts).ThenInclude(pp => pp.Product)
                .Include(p => p.PromotionCategories).ThenInclude(pc => pc.Category)
                .ToListAsync();
        }

        public async Task<Promotion?> GetByIdAsync(int id)
        {
            return await _context.Promotions
                .Include(p => p.PromotionProducts).ThenInclude(pp => pp.Product)
                .Include(p => p.PromotionCategories).ThenInclude(pc => pc.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AddAsync(Promotion promotion, List<int> productIds, List<int> categoryIds)
        {
            // Gắn sản phẩm nếu có
            if (promotion.TargetType == PromotionTarget.IndividualProducts && productIds != null)
            {
                promotion.PromotionProducts = productIds.Select(pid => new PromotionProduct
                {
                    ProductId = pid,
                    Promotion = promotion
                }).ToList();
            }

            // Gắn danh mục nếu có
            if (promotion.TargetType == PromotionTarget.Categories && categoryIds != null)
            {
                promotion.PromotionCategories = categoryIds.Select(cid => new PromotionCategory
                {
                    CategoryId = cid,
                    Promotion = promotion
                }).ToList();
            }

            await _context.Promotions.AddAsync(promotion);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Promotion promotion)
        {
            var existing = await _context.Promotions
                .Include(p => p.PromotionProducts)
                .Include(p => p.PromotionCategories)
                .FirstOrDefaultAsync(p => p.Id == promotion.Id);

            if (existing == null) return;

            // Cập nhật thông tin cơ bản
            existing.Name = promotion.Name;
            existing.Description = promotion.Description;
            existing.StartDate = promotion.StartDate;
            existing.EndDate = promotion.EndDate;
            existing.DiscountType = promotion.DiscountType;
            existing.DiscountValue = promotion.DiscountValue;
            existing.TargetType = promotion.TargetType;
            existing.IsActive = promotion.IsActive;

            // Gỡ liên kết cũ
            _context.PromotionProducts.RemoveRange(existing.PromotionProducts);
            _context.PromotionCategories.RemoveRange(existing.PromotionCategories);
            await _context.SaveChangesAsync(); // Lưu sau khi xoá

            // Thêm liên kết mới
            if (promotion.PromotionProducts?.Any() == true)
            {
                foreach (var item in promotion.PromotionProducts)
                    item.PromotionId = promotion.Id;

                _context.PromotionProducts.AddRange(promotion.PromotionProducts);
            }

            if (promotion.PromotionCategories?.Any() == true)
            {
                foreach (var item in promotion.PromotionCategories)
                    item.PromotionId = promotion.Id;

                _context.PromotionCategories.AddRange(promotion.PromotionCategories);
            }

            await _context.SaveChangesAsync(); // Lưu sau khi thêm
        }


        public async Task DeleteAsync(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo != null)
            {
                _context.Promotions.Remove(promo);
                await _context.SaveChangesAsync();
            }
        }
    }
}
