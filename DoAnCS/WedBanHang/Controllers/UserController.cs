using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.ViewModels;
using WedBanHang.Models;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = _context.Orders
                .Include(o => o.OrderDetails)
                .Where(o => o.ApplicationUserId == user.Id && o.Status == OrderStatus.HoanThanh)
                .ToList();

            var totalOrders = orders.Count;
            var totalAmount = orders
                .SelectMany(o => o.OrderDetails)
                .Sum(od => od.Quantity * od.Price);
            var totalProductsPurchased = orders
                .SelectMany(o => o.OrderDetails)
                .Sum(od => od.Quantity);

            var model = new UserDashboardViewModel
            {
                FullName = user.FullName,
                TotalOrders = totalOrders,
                TotalRevenue = totalAmount,
                TotalProductsPurchased = totalProductsPurchased,
                AvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            if (avatar != null && avatar.Length > 0)
            {
                var user = await _userManager.GetUserAsync(User);
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "avatar");
                Directory.CreateDirectory(uploadsFolder);
                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(avatar.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatar.CopyToAsync(fileStream);
                }

                user.AvatarUrl = "/avatar/" + uniqueFileName;
                await _userManager.UpdateAsync(user);

                return Ok(new { avatarUrl = user.AvatarUrl });
            }

            return BadRequest("Ảnh đại diện không hợp lệ.");
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new EditProfileViewModel
            {
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                Address = user.Address,

            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Email = model.Email;
            user.Address = model.Address;


            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Thông tin đã được cập nhật.";
                return RedirectToAction("Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }

}
