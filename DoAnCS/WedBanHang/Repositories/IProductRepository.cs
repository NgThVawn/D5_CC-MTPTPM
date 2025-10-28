using WedBanHang.Models;

namespace WedBanHang.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Task AddImagesAsync(int productId, List<string> imageUrls);
        Task DeleteImagesAsync(List<int> imageId);

        Task DeleteImageAsync(int imageId);

    }
}
