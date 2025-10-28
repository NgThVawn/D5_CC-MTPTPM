namespace WebBanHang.ViewModels
{
    public class UserDashboardViewModel
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalProductsPurchased { get; set; }
        public string AvatarUrl { get; set; } = "/images/default-avatar.png";
    }

}
