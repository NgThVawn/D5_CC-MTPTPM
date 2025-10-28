using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WebBanHang.Models;
using WedBanHang.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

[Route("api/chat")]
[ApiController]
[Authorize]
public class ChatApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages()
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == null)
            return Unauthorized();

        // Lấy tất cả ID của các admin
        var adminUsers = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);
        var adminIds = adminUsers.Select(u => u.Id).ToList();

        // Lấy tin nhắn giữa người dùng và tất cả admin
        var messages = await _context.Messages
            .Include(m => m.Sender) // để lấy AvatarUrl từ Sender
            .Where(m =>
                (adminIds.Contains(m.SenderId) && m.ReceiverId == currentUserId) ||
                (adminIds.Contains(m.ReceiverId) && m.SenderId == currentUserId))
            .OrderBy(m => m.Timestamp)
            .Select(m => new
            {
                sender = m.IsFromSupport ? "Hỗ trợ viên" : (m.SenderId == currentUserId ? "Bạn" : "Hỗ trợ viên"),
                content = m.Content,
                time = m.Timestamp.ToString("HH:mm dd/MM/yyyy"),
                rawTimestamp = m.Timestamp,
                avatar = m.IsFromSupport
                    ? "/images/logo.png"
                    : m.SenderId == currentUserId
                        ? m.Sender.AvatarUrl
                        : "/images/logo.png"

            })
            .ToListAsync();

        return Ok(messages);
    }

}