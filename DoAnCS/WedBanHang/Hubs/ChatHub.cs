using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using WebBanHang.Models;
using Microsoft.AspNetCore.Identity;
using WedBanHang.Models;
using Microsoft.EntityFrameworkCore;
using System;

public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatHub(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task SendMessage(string contextType, string message)
    {
        var senderId = Context.UserIdentifier;

        if (string.IsNullOrEmpty(senderId))
        {
            Console.WriteLine("⚠️ Không xác định được senderId (user chưa đăng nhập?)");
            return;
        }

        var sender = await _userManager.FindByIdAsync(senderId);
        if (sender == null)
        {
            Console.WriteLine("⚠️ Không tìm thấy người gửi.");
            return;
        }

        var isAdmin = await _userManager.IsInRoleAsync(sender, SD.Role_Admin);
        bool isPopupContext = contextType == "popup";

        var adminUsers = await _userManager.GetUsersInRoleAsync(SD.Role_Admin);
        var adminToSave = adminUsers.FirstOrDefault();
        if (adminToSave == null) return;

        // Lưu tin nhắn
        var msg = new Message
        {
            SenderId = senderId,
            ReceiverId = adminToSave.Id,
            Content = message,
            Timestamp = DateTime.Now,
            IsFromSupport = !isPopupContext
        };

        _context.Messages.Add(msg);
        await _context.SaveChangesAsync();

        string timeFormatted = msg.Timestamp.ToString("HH:mm dd/MM/yyyy");

        if (!isAdmin || isPopupContext)
        {
            // Người gửi là người dùng hoặc admin dùng ChatPopup (giao diện user)
            var adminRecipients = adminUsers.Where(a => a.Id != senderId).ToList();

            foreach (var admin in adminRecipients)
            {
                await Clients.User(admin.Id).SendAsync(
                    "ReceiveMessage",
                    sender.FullName,
                    message,
                    timeFormatted,
                    sender.AvatarUrl,
                    senderId
                );
            }

            // Gửi lại cho chính người gửi với label "Bạn"
            await Clients.User(senderId).SendAsync(
                "ReceiveMessage",
                "Bạn",
                message,
                timeFormatted,
                sender.AvatarUrl,
                senderId
            );
        }
        else
        {
            // Gửi từ admin panel
            var receiverId = msg.ReceiverId;

            // Gửi đến người dùng: luôn là "Hỗ trợ viên"
            await Clients.User(receiverId).SendAsync(
                "ReceiveMessage",
                "Hỗ trợ viên",
                message,
                timeFormatted,
                "/images/logo.png",
                senderId
            );

            // Gửi lại cho chính admin: vẫn là "Hỗ trợ viên", để hiển thị bên trái ở ChatPopup
            await Clients.User(senderId).SendAsync(
                "ReceiveMessage",
                "Hỗ trợ viên",
                message,
                timeFormatted,
                "/images/logo.png",
                senderId
            );
        }
    }


    public override async Task OnConnectedAsync()
    {
        Console.WriteLine("✅ SignalR Connected: " + Context.UserIdentifier);
        await base.OnConnectedAsync();
    }
}
