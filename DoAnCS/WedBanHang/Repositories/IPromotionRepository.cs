using WebBanHang.Models;

namespace WebBanHang.Repositories
{
    public interface IPromotionRepository
    {
        Task<IEnumerable<Promotion>> GetAllAsync();
        Task<Promotion?> GetByIdAsync(int id);
        Task AddAsync(Promotion promotion, List<int> productIds, List<int> categoryIds);
        Task UpdateAsync(Promotion promotion);
        Task DeleteAsync(int id);
    }
}
