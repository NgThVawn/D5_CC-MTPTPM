using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebBanHang.Models;
using WedBanHang.Models;
using WedBanHang.Repositories;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;


        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;

        }
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Add(Product product, List<IFormFile> images)
        {
            ModelState.Remove("ImageUrl");

            if (ModelState.IsValid)
            {
                List<string> savedImageUrls = new();

                if (images != null && images.Count > 0)
                {
                    foreach (var image in images)
                    {
                        var imageUrl = await SaveImage(image);
                        savedImageUrls.Add(imageUrl);
                    }

                    // Ảnh đầu tiên làm ảnh đại diện
                    product.ImageUrl = savedImageUrls.FirstOrDefault();
                }
                if (product.StockQuantity <= 0)
                {
                    product.IsAvailable = false;
                }

                await _productRepository.AddAsync(product);

                if (savedImageUrls.Count > 0)
                {
                    await _productRepository.AddImagesAsync(product.Id, savedImageUrls);
                }

                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }


        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }
        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, List<IFormFile> images, List<int> DeletedImageIds)
        {
            ModelState.Remove("Images");

            if (id != product.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);

                // Cập nhật các trường cơ bản
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.IsAvailable = product.IsAvailable;

                // ✅ Xử lý xóa ảnh
                if (DeletedImageIds != null && DeletedImageIds.Any())
                {
                    await _productRepository.DeleteImagesAsync(DeletedImageIds);
                }

                // ✅ Thêm ảnh mới (nếu có)
                if (images != null && images.Count > 0)
                {
                    var imageUrls = new List<string>();

                    foreach (var image in images)
                    {
                        var imageUrl = await SaveImage(image); // Hàm này bạn đã có
                        imageUrls.Add(imageUrl);
                    }

                    await _productRepository.AddImagesAsync(existingProduct.Id, imageUrls);
                }
                if (product.StockQuantity == 0)
                {
                    existingProduct.IsAvailable = false;
                }
                else
                {
                    existingProduct.IsAvailable = product.IsAvailable;
                }

                await _productRepository.UpdateAsync(existingProduct);
                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }



        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        private async Task<string> SaveImage(IFormFile image)
        {

            var savePath = Path.Combine("wwwroot/images", image.FileName);
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            return "/images/" + image.FileName;
        }

        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            // Xóa ảnh khỏi cơ sở dữ liệu
            await _productRepository.DeleteImageAsync(imageId);

            // Sau khi xóa thành công, quay lại trang cập nhật sản phẩm
            return RedirectToAction("Update", new { id = productId });
        }



    }
}

