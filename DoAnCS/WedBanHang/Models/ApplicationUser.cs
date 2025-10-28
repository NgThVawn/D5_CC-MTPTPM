using Microsoft.AspNetCore.Identity;

namespace WebBanHang.Models
{
    public class ApplicationUser : IdentityUser
    {     
        public string? FullName { get; set; }

        public string? Age { get; set; }

        public string? Address { get; set; }
        public string AvatarUrl { get; set; } = "/images/default-avatar.png";
        public bool IsRestricted { get; set; } = false;
        public bool IsBlocked { get; set; } = false;

    }
}
