using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebBanHang.Models;
using WedBanHang.Models;
using Microsoft.EntityFrameworkCore;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _env;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _env = env;
        }

        // Hiển thị form đánh giá nếu được phép
        public async Task<IActionResult> Create(int productId, int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ✅ Kiểm tra OrderDetail.HasReviewed
            var canReview = await _context.OrderDetails
                .Include(od => od.Order)
                .AnyAsync(od => od.ProductId == productId &&
                                od.OrderId == orderId &&
                                od.Order.ApplicationUserId == userId &&
                                !od.HasReviewed); // <--- Chỉ cho review nếu chưa đánh giá

            if (!canReview)
            {
                return Forbid(); // Không cho đánh giá lại
            }

            ViewBag.ProductId = productId;
            ViewBag.OrderId = orderId;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Create(Review model, List<IFormFile> Images)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🔐 Kiểm tra lại HasReviewed trong POST
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.OrderId == model.OrderId &&
                                           od.ProductId == model.ProductId &&
                                           od.Order.ApplicationUserId == userId);

            if (orderDetail == null || orderDetail.HasReviewed)
            {
                return Forbid(); // 🚫 Không được đánh giá lại
            }

            model.ApplicationUserId = userId;
            model.CreatedAt = DateTime.Now;

            // ✅ Lưu ảnh nếu có
            if (Images != null && Images.Any())
            {
                model.Images = new List<ReviewImage>();
                foreach (var image in Images)
                {
                    var fileName = Path.GetFileNameWithoutExtension(image.FileName) + "_" + Guid.NewGuid().ToString("N") + Path.GetExtension(image.FileName);
                    var savePath = Path.Combine(_env.WebRootPath, "review", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    model.Images.Add(new ReviewImage
                    {
                        Url = "/review/" + fileName
                    });
                }
            }

            _context.Reviews.Add(model);
            orderDetail.HasReviewed = true; // ✅ Đánh dấu đã đánh giá

            await _context.SaveChangesAsync();
            return RedirectToAction("Display", "Product", new { id = model.ProductId });
        }



        [Authorize]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var review = await _context.Reviews
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null || review.ApplicationUserId != user.Id)
                return Forbid(); // Chặn nếu không phải chủ sở hữu

            return View(review);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(int id, int Rating, string Comment, List<IFormFile> Images, List<string> ExistingImageUrls)
        {
            var user = await _userManager.GetUserAsync(User);
            var review = await _context.Reviews
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null || review.ApplicationUserId != user.Id)
                return Forbid();

            review.Rating = Rating;
            review.Comment = Comment;

            // 🔻 Xoá ảnh bị loại bỏ
            if (review.Images != null && review.Images.Any())
            {
                var toRemove = review.Images
                    .Where(img => !ExistingImageUrls.Contains(img.Url))
                    .ToList();

                _context.ReviewImages.RemoveRange(toRemove);
            }

            // 🔻 Thêm ảnh mới
            if (Images != null && Images.Count > 0)
            {
                foreach (var file in Images)
                {
                    var imageUrl = await SaveImage(file);
                    review.Images.Add(new ReviewImage
                    {
                        Url = imageUrl
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Display", "Product", new { id = review.ProductId });
        }


        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null || review.ApplicationUserId != user.Id)
                return Forbid();

            return View(review);
        }

        [Authorize]
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var review = await _context.Reviews
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (review == null || review.ApplicationUserId != user.Id)
                return Forbid();

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return RedirectToAction("Display", "Product", new { id = review.ProductId });
        }
        private async Task<string> SaveImage(IFormFile image)
        {
            var fileName = Path.GetFileNameWithoutExtension(image.FileName) + "_" + Guid.NewGuid().ToString("N") + Path.GetExtension(image.FileName);
            var savePath = Path.Combine(_env.WebRootPath, "review", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return "/review/" + fileName;
        }

        [Authorize, HttpPost]
        public async Task<IActionResult> Report(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = await _context.Reviews.FindAsync(id);

            if (review == null || review.ApplicationUserId == userId)
                return Forbid();

            review.IsReported = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("Display", "Product", new { id = review.ProductId });
        }

    }
}
