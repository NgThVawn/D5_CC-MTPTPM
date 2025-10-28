using Microsoft.EntityFrameworkCore;
using WedBanHang.Models;

namespace WedBanHang.Repositories
{
    public class EFProductRepository : IProductRepository
    {
        private readonly ApplicationDbContext _context;
        public EFProductRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products.Include(p => p.Images).Include(p => p.Category).ToListAsync();
        }
        public async Task<Product> GetByIdAsync(int id)
        {
            return await _context.Products.Include(p => p.Images).Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task AddAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        public async Task AddImagesAsync(int productId, List<string> imageUrls)
        {
            foreach (var url in imageUrls)
            {
                var productImage = new ProductImage
                {
                    Url = url,
                    ProductId = productId
                };

                _context.ProductImages.Add(productImage);
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteImageAsync(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image != null)
            {
                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();
            }
        }
        public async Task DeleteImagesAsync(List<int> imageIds)
        {
            var images = _context.ProductImages.Where(img => imageIds.Contains(img.Id));
            _context.ProductImages.RemoveRange(images);
            await _context.SaveChangesAsync();
        }

    }
}
