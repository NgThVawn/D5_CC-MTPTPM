using WebBanHang.Models;

namespace WebBanHang.ViewModels
{
    public class NotificationViewModel
    {
        public List<Notification> Notifications { get; set; }
        public int UnreadCount { get; set; }
    }

}
