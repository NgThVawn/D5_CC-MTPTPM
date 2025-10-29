using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WedBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // 👈 chỉ SuperAdmin mới xem được
    public class AdminChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<ChatHub> _hubContext;

        public AdminChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        // Danh sách người dùng đã từng nhắn tin
        public async Task<IActionResult> Index()
        {
            var usersWithMessages = await _context.Messages
                .Select(m => m.SenderId)
                .Distinct()
                .ToListAsync();

            var users = await _context.Users
                .Where(u => usersWithMessages.Contains(u.Id))
                .ToListAsync();

            return View(users);
        }

        // Hiển thị tin nhắn giữa admin và người dùng
        public async Task<IActionResult> Chat(string userId)
        {
            // Lấy tất cả ID của các admin
            var adminUsers = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);
            var adminIds = adminUsers.Select(u => u.Id).ToList();

            // Truy xuất tất cả tin nhắn giữa người dùng và bất kỳ admin nào
            var messages = await _context.Messages
                .Where(m =>
                    (adminIds.Contains(m.SenderId) && m.ReceiverId == userId) ||
                    (adminIds.Contains(m.ReceiverId) && m.SenderId == userId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            var user = await _userManager.FindByIdAsync(userId);
            ViewBag.UserId = userId;
            ViewBag.UserName = user?.FullName ?? "Người dùng";

            return View(messages);
        }

        // Gửi tin nhắn từ admin đến người dùng
        [HttpPost]
        public async Task<IActionResult> Send(string receiverId, string content)
        {
            var senderId = _userManager.GetUserId(User);
            var sender = await _userManager.FindByIdAsync(senderId);

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                Timestamp = DateTime.Now,
                IsFromSupport = true
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Gửi realtime tới người dùng bằng SignalR
            await _hubContext.Clients.User(receiverId)
                .SendAsync("ReceiveMessage", "Hỗ trợ viên", content, message.Timestamp.ToString("HH:mm dd/MM/yyyy"));

            return RedirectToAction("Chat", new { userId = receiverId });
        }

    }
}
