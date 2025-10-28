using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.ViewModels;
using WedBanHang.Models;
using WedBanHang.Repositories;


namespace WedBanHang.Controllers
{  
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository, ApplicationDbContext context)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }
        public async Task<IActionResult> Index(string search, int? categoryId, decimal? minPrice, decimal? maxPrice, string sortOrder, int page = 1)
        {
            var products = await _productRepository.GetAllAsync();
            const int pageSize = 8;
            // Lọc toàn bộ sản phẩm theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
            {
                string keyword = RemoveDiacritics(search.ToLower());

                // Nếu người dùng nhập số → tìm theo giá
                bool isNumber = decimal.TryParse(search, out decimal priceInput);

                products = products.Where(p =>
                    RemoveDiacritics(p.Name.ToLower()).Contains(keyword) ||            // Tên sản phẩm
                    RemoveDiacritics(p.Description?.ToLower() ?? "").Contains(keyword) || // Mô tả
                    RemoveDiacritics(p.Category?.Name?.ToLower() ?? "").Contains(keyword) || // Tên loại sản phẩm
                    (isNumber && p.Price == priceInput)                                 // Giá đúng
                ).ToList();
            }

            // Lọc theo loại
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value).ToList();
            }

            // Lọc theo giá
            if (minPrice.HasValue)
            {
                products = products.Where(p => p.Price >= minPrice.Value).ToList();
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(p => p.Price <= maxPrice.Value).ToList();
            }

            var soldCounts = await _context.OrderDetails
             .Where(od => od.Order.Status != OrderStatus.DaHuy)
             .GroupBy(od => od.ProductId)
             .Select(g => new
             {
                 ProductId = g.Key,
                 Total = g.Sum(x => x.Quantity)
             })
             .ToDictionaryAsync(x => x.ProductId, x => x.Total);

            // Lấy dữ liệu đánh giá
            var reviewGroups = await _context.Reviews
                .GroupBy(r => r.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    AverageRating = g.Average(r => r.Rating),
                    TotalReviews = g.Count()
                })
                .ToDictionaryAsync(g => g.ProductId, g => new
                {
                    g.AverageRating,
                    g.TotalReviews
                });

            var activePromotions = await _context.Promotions
                .Include(p => p.PromotionProducts)
                .Include(p => p.PromotionCategories)
                .Where(p => p.IsActive && DateTime.Now >= p.StartDate && DateTime.Now <= p.EndDate)
                .ToListAsync();

            var marqueePromos = activePromotions
                .Where(promo =>
                    promo.TargetType == PromotionTarget.AllProducts ||
                    (promo.TargetType == PromotionTarget.IndividualProducts &&
                     products.Any(prod => promo.PromotionProducts.Any(pp => pp.ProductId == prod.Id))) ||
                    (promo.TargetType == PromotionTarget.Categories &&
                     products.Any(prod => promo.PromotionCategories.Any(pc => pc.CategoryId == prod.CategoryId)))
                ).ToList();

            ViewBag.MarqueePromotions = marqueePromos;



            // ✅ Tạo ViewModel kết hợp sản phẩm + lượt bán
            var viewModel = products.Select(p =>
            {
                // Tìm chương trình khuyến mãi áp dụng cho sản phẩm này
                var applicablePromos = activePromotions.Where(promo =>
                    promo.TargetType == PromotionTarget.AllProducts ||
                    (promo.TargetType == PromotionTarget.IndividualProducts && promo.PromotionProducts.Any(x => x.ProductId == p.Id)) ||
                    (promo.TargetType == PromotionTarget.Categories && promo.PromotionCategories.Any(x => x.CategoryId == p.CategoryId))
                ).ToList();

                var finalPrice = applicablePromos.Any()
                    ? PromotionHelper.GetFinalPrice(p, applicablePromos)
                    : p.Price;

                var appliedPromo = applicablePromos
                    .OrderBy(promo => PromotionHelper.GetFinalPrice(p, new List<Promotion> { promo }))
                    .FirstOrDefault();


                return new ProductWithSalesViewModel
                {
                    Product = p,
                    SoldCount = soldCounts.ContainsKey(p.Id) ? soldCounts[p.Id] : 0,
                    AverageRating = reviewGroups.ContainsKey(p.Id) ? Math.Round(reviewGroups[p.Id].AverageRating, 1) : 0,
                    TotalReviews = reviewGroups.ContainsKey(p.Id) ? reviewGroups[p.Id].TotalReviews : 0,
                    FinalPrice = finalPrice,
                    AppliedPromotion = appliedPromo // 🆕
                };
            }).ToList();

            // ✅ Xử lý sắp xếp dựa trên lựa chọn
            switch (sortOrder)
            {
                case "price_asc":
                    viewModel = viewModel
                        .OrderByDescending(x => x.Product.IsAvailable && x.Product.StockQuantity > 0) // Ưu tiên sản phẩm còn hàng
                        .ThenBy(x => x.Product.Price)
                        .ToList();
                    break;
                case "price_desc":
                    viewModel = viewModel
                        .OrderByDescending(x => x.Product.IsAvailable && x.Product.StockQuantity > 0)
                        .ThenByDescending(x => x.Product.Price)
                        .ToList();
                    break;
                case "sold_desc":
                    viewModel = viewModel
                        .OrderByDescending(x => x.Product.IsAvailable && x.Product.StockQuantity > 0)
                        .ThenByDescending(x => x.SoldCount)
                        .ToList();
                    break;
                case "newest":
                    viewModel = viewModel
                        .OrderByDescending(x => x.Product.IsAvailable && x.Product.StockQuantity > 0)
                        .ThenByDescending(x => x.Product.Id)
                        .ToList();
                    break;
                default:
                    // Mặc định sắp xếp theo tên
                    viewModel = viewModel
                        .OrderByDescending(x => x.Product.IsAvailable && x.Product.StockQuantity > 0)
                        .ThenBy(x => x.Product.Name)
                        .ToList();
                    break;
            }

            // Lấy danh sách loại cho combobox
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = categories;

            int totalItems = viewModel.Count;
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var pagedProducts = viewModel.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // ViewBag
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.SortOrder = sortOrder;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            return View(pagedProducts);
        }

        [Authorize(Roles = SD.Role_Admin)]
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

        public async Task<IActionResult> Display(int id, int page = 1)
        {
            const int pageSize = 10;

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null) return NotFound();

            // Đếm số lượng đã bán
            int soldCount = await _context.OrderDetails
                .Where(od => od.ProductId == id && od.Order.Status != OrderStatus.DaHuy)
                .SumAsync(od => (int?)od.Quantity) ?? 0;
            ViewBag.SoldCount = soldCount;

            // Lấy tất cả đánh giá
            // Lấy tất cả đánh giá
            var allReviews = await _context.Reviews
                .Include(r => r.ApplicationUser)
                .Include(r => r.Images)
                .Where(r => r.ProductId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // ✅ Thêm dòng này để gallery ảnh không bị giới hạn bởi phân trang
            var allReviewImages = allReviews
                .SelectMany(r => r.Images)
                .Select(i => i.Url)
                .ToList();
            ViewBag.AllReviewImageUrls = allReviewImages;


            // Tính trang hiện tại
            int totalReviews = allReviews.Count;
            int totalPages = (int)Math.Ceiling((double)totalReviews / pageSize);
            var pagedReviews = allReviews.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Tìm khuyến mãi đang áp dụng
            var activePromotions = await _context.Promotions
                .Include(p => p.PromotionProducts)
                .Include(p => p.PromotionCategories)
                .Where(p => p.IsActive && DateTime.Now >= p.StartDate && DateTime.Now <= p.EndDate)
                .ToListAsync();

            var applicablePromos = activePromotions.Where(promo =>
                promo.TargetType == PromotionTarget.AllProducts ||
                (promo.TargetType == PromotionTarget.Categories && promo.PromotionCategories.Any(c => c.CategoryId == product.CategoryId)) ||
                (promo.TargetType == PromotionTarget.IndividualProducts && promo.PromotionProducts.Any(p => p.ProductId == product.Id))
            ).ToList();

            var finalPrice = applicablePromos.Any()
                ? PromotionHelper.GetFinalPrice(product, applicablePromos)
                : product.Price;

            var appliedPromo = applicablePromos
                .OrderBy(promo => PromotionHelper.GetFinalPrice(product, new List<Promotion> { promo }))
                .FirstOrDefault();

            // Truyền vào View
            ViewBag.FinalPrice = finalPrice;
            ViewBag.AppliedPromotion = appliedPromo;


            // ViewBag
            ViewBag.Reviews = pagedReviews;
            ViewBag.TotalReviews = totalReviews;
            ViewBag.AverageRating = totalReviews > 0 ? Math.Round(allReviews.Average(r => r.Rating), 1) : 0;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(product);
        }

        public async Task<PartialViewResult> LoadReviews(int productId, int page = 1, string filter = "all")
        {
            const int pageSize = 10;

            var query = _context.Reviews
                .Include(r => r.ApplicationUser)
                .Include(r => r.Images)
                .Where(r => r.ProductId == productId);

            switch (filter)
            {
                case "hasImage":
                    query = query.Where(r => r.Images.Any());
                    break;
                case "hasComment":
                    query = query.Where(r => !string.IsNullOrWhiteSpace(r.Comment));
                    break;
                case "5":
                case "4":
                case "3":
                case "2":
                case "1":
                    int rating = int.Parse(filter);
                    query = query.Where(r => r.Rating == rating);
                    break;
            }

            var totalReviews = await query.CountAsync();
            var reviews = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalReviews = totalReviews;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalReviews / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.ProductId = productId;

            return PartialView("_ReviewList", reviews);
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

        private string RemoveDiacritics(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var normalized = input.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();

            foreach (var c in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }


    }
}
