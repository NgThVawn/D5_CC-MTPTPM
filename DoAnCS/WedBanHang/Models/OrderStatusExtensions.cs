namespace WebBanHang.Models
{
    public static class OrderStatusExtensions
    {
        public static string ToFriendlyString(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.ChoXacNhan => "Chờ xác nhận",
                OrderStatus.DangChuanBi => "Đang chuẩn bị hàng",
                OrderStatus.DangVanChuyen => "Đang vận chuyển",
                OrderStatus.DaGiao => "Đã giao",
                OrderStatus.HoanThanh => "Hoàn thành",
                OrderStatus.DaHuy => "Đã hủy",
                OrderStatus.ChoHuy => "Chờ hủy",

                _ => "Không xác định"
            };
        }
        public static string ToStatusColor(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.ChoXacNhan => "warning text-dark",
                OrderStatus.DangChuanBi => "info",
                OrderStatus.DangVanChuyen => "primary",
                OrderStatus.DaGiao => "secondary",
                OrderStatus.HoanThanh => "success",
                OrderStatus.DaHuy => "danger",
                OrderStatus.ChoHuy => "warning",
                _ => "light"
            };
        }
        public static string GetActionLabel(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.ChoXacNhan => "Xác nhận",
                OrderStatus.DangChuanBi => "Chuẩn bị xong",
                OrderStatus.DangVanChuyen => "Đã giao hàng",
                OrderStatus.DaGiao => "Hoàn tất đơn",
                OrderStatus.ChoHuy => "Xử lý hủy",
                _ => "Tiếp theo"
            };
        }
        public static string ToNotificationMessage(this OrderStatus status, int orderId)
        {
            return status switch
            {
                OrderStatus.ChoXacNhan => $"Đơn hàng #{orderId} đã được tạo. Chờ người bán xác nhận.",
                OrderStatus.DangChuanBi => $"Đơn hàng #{orderId} đã được xác nhận. Người bán đang chuẩn bị hàng.",
                OrderStatus.DangVanChuyen => $"Đơn hàng #{orderId} đang được vận chuyển đến bạn.",
                OrderStatus.DaGiao => $"Đơn hàng #{orderId} đã được giao thành công.",
                OrderStatus.HoanThanh => $"Đơn hàng #{orderId} đã hoàn tất. Cảm ơn bạn đã mua hàng!",
                OrderStatus.DaHuy => $"Đơn hàng #{orderId} đã bị hủy.",
                OrderStatus.ChoHuy => $"Đơn hàng #{orderId} đang được yêu cầu hủy. Chờ admin xử lý.",
                _ => $"Đơn hàng #{orderId} có cập nhật mới."
            };
        }

    }
}
