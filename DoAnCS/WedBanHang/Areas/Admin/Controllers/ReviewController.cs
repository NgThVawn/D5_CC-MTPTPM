using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WedBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? productId)
        {
            var reviewsQuery = _context.Reviews
                .Include(r => r.Product)
                .Include(r => r.ApplicationUser)
                .Include(r => r.Images)
                .AsQueryable();

            if (productId.HasValue)
            {
                reviewsQuery = reviewsQuery.Where(r => r.ProductId == productId);
            }

            var reviews = await reviewsQuery
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Products = await _context.Products.Select(p => new { p.Id, p.Name }).ToListAsync();
            ViewBag.FilterProductId = productId;

            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.Reviews.Include(r => r.Images).FirstOrDefaultAsync(r => r.Id == id);
            if (review != null)
            {
                // Xóa ảnh vật lý
                foreach (var img in review.Images)
                {
                    var path = Path.Combine("wwwroot", img.Url.TrimStart('/'));
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }

                _context.ReviewImages.RemoveRange(review.Images);
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> Unreport(int id)
        {
            var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == id);
            if (review != null && review.IsReported)
            {
                review.IsReported = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

    }
}
