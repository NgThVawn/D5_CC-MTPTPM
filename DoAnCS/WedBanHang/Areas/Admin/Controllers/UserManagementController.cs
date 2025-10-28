using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WebBanHang.Services;
using WedBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public UserManagementController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context, EmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> Index(string search)
        {
            var users = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                users = users.Where(u => u.Email.Contains(search) || u.UserName.Contains(search));
            }

            var userList = await users.ToListAsync();
            return View(userList);
        }

        // POST: Admin/UserManagement/ToggleBlock
        [HttpPost]
        public async Task<IActionResult> ToggleRestriction(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsRestricted = !user.IsRestricted;
                await _userManager.UpdateAsync(user);

                var message = user.IsRestricted
                    ? "Tài khoản của bạn đã bị hạn chế sử dụng một số chức năng."
                    : "Tài khoản của bạn đã được gỡ bỏ hạn chế.";

                // Gửi thông báo nội bộ
                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Message = message
                });
                await _context.SaveChangesAsync();

                // Gửi email
                await _emailService.SendEmailAsync(user.Email, "Thông báo tài khoản", message);
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> ToggleBlock(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.IsBlocked = !user.IsBlocked;
                await _userManager.UpdateAsync(user);

                var message = user.IsBlocked
                    ? "Tài khoản của bạn đã bị chặn. Bạn sẽ không thể đăng nhập."
                    : "Tài khoản của bạn đã được mở chặn.";

                _context.Notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Message = message
                });

                await _context.SaveChangesAsync();
                await _emailService.SendEmailAsync(user.Email, "Thông báo tài khoản", message);
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));

            await _userManager.AddToRoleAsync(user, role);

            var message = $"Bạn đã được nâng quyền lên {role}.";

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Message = message
            });

            await _context.SaveChangesAsync();
            await _emailService.SendEmailAsync(user.Email, "Thay đổi quyền truy cập", message);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRole(string userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.RemoveFromRoleAsync(user, role);

            var message = $"Bạn đã bị hạ quyền {role}.";

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Message = message
            });

            await _context.SaveChangesAsync();
            await _emailService.SendEmailAsync(user.Email, "Thay đổi quyền truy cập", message);

            return RedirectToAction("Index");
        }

    }
}
