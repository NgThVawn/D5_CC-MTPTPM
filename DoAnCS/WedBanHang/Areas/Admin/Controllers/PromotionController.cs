using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Repositories;
using WedBanHang.Models;
using WedBanHang.Repositories;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class PromotionController : Controller
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;

        public PromotionController(
            IPromotionRepository promotionRepository,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ApplicationDbContext context)
        {
            _promotionRepository = promotionRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var promotions = await _promotionRepository.GetAllAsync();
            return View(promotions);
        }

        public async Task<IActionResult> Add()
        {
            ViewBag.Products = await _productRepository.GetAllAsync();
            ViewBag.Categories = await _categoryRepository.GetAllAsync();
            return View(new Promotion());
        }

        [HttpPost]
        public async Task<IActionResult> Add(
            Promotion promotion,
            List<int> selectedProductIds,
            List<int> selectedCategoryIds,
            int DurationMonths,
            int DurationDays,
            int DurationHours,
            int DurationMinutes)    
        {
            promotion.EndDate = promotion.StartDate
                .AddMonths(DurationMonths)
                .AddDays(DurationDays)
                .AddHours(DurationHours)
                .AddMinutes(DurationMinutes);

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _productRepository.GetAllAsync();
                ViewBag.Categories = await _categoryRepository.GetAllAsync();
                return View(promotion);
            }

            await _promotionRepository.AddAsync(promotion, selectedProductIds, selectedCategoryIds);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Display(int id)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }
        public async Task<IActionResult> Toggle(int id)
        {
            var promo = await _context.Promotions
                .FirstOrDefaultAsync(p => p.Id == id); // KHÔNG cần Include

            if (promo == null) return NotFound();

            if (!promo.IsActive)
            {
                if (promo.EndDate <= DateTime.Now)
                {
                    TempData["Error"] = "Không thể bật lại giảm giá vì thời gian đã hết hạn. Vui lòng chỉnh sửa thời gian trước.";
                    return RedirectToAction(nameof(Index));
                }

                promo.IsActive = true;
                promo.StartDate = DateTime.Now;
            }
            else
            {
                promo.IsActive = false;
            }

            _context.Update(promo); // Chỉ cập nhật trạng thái
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null) return NotFound();

            ViewBag.SelectedProductIds = promo.PromotionProducts.Select(pp => pp.ProductId).ToList();
            ViewBag.SelectedCategoryIds = promo.PromotionCategories.Select(pc => pc.CategoryId).ToList();


            ViewBag.Products = await _productRepository.GetAllAsync();
            ViewBag.Categories = await _categoryRepository.GetAllAsync();

            var start = promo.StartDate;
            var end = promo.EndDate;

            var temp = start;

            // Tính tháng
            int months = 0;
            while (temp.AddMonths(1) <= end)
            {
                temp = temp.AddMonths(1);
                months++;
            }

            // Tính ngày
            int days = 0;
            while (temp.AddDays(1) <= end)
            {
                temp = temp.AddDays(1);
                days++;
            }

            // Giờ và phút
            var timeSpan = end - temp;
            int hours = timeSpan.Hours;
            int minutes = timeSpan.Minutes;

            promo.DurationMonths = months;
            promo.DurationDays = days;
            promo.DurationHours = hours;
            promo.DurationMinutes = minutes;

            return View(promo);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
             Promotion promotion,
             List<int>? selectedProductIds,
             List<int>? selectedCategoryIds,
             int DurationMonths,
             int DurationDays,
             int DurationHours,
             int DurationMinutes)
        {
            // Tính EndDate từ các đơn vị
            promotion.EndDate = promotion.StartDate
                .AddMonths(DurationMonths)
                .AddDays(DurationDays)
                .AddHours(DurationHours)
                .AddMinutes(DurationMinutes);

            // Gắn lại liên kết sản phẩm hoặc danh mục
            if (promotion.TargetType == PromotionTarget.IndividualProducts)
            {
                promotion.PromotionProducts = selectedProductIds?.Select(id => new PromotionProduct
                {
                    ProductId = id,
                    PromotionId = promotion.Id
                }).ToList() ?? new List<PromotionProduct>();
            }
            else if (promotion.TargetType == PromotionTarget.Categories)
            {
                promotion.PromotionCategories = selectedCategoryIds?.Select(id => new PromotionCategory
                {
                    CategoryId = id,
                    PromotionId = promotion.Id
                }).ToList() ?? new List<PromotionCategory>();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _productRepository.GetAllAsync();
                ViewBag.Categories = await _categoryRepository.GetAllAsync();
                return View(promotion);
            }

            await _promotionRepository.UpdateAsync(promotion);
            return RedirectToAction(nameof(Index));
        }



        // Optional: delete
        public async Task<IActionResult> Delete(int id)
        {
            var promo = await _promotionRepository.GetByIdAsync(id);
            if (promo == null) return NotFound();
            return View(promo);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _promotionRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
