namespace WebBanHang.Models
{
    public enum PaymentMethod
    {
        COD,
        BankTransfer
    }

    public class Order
    {
        public int Id { get; set; }
        public string ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public OrderStatus Status { get; set; } = OrderStatus.ChoXacNhan;
        public ICollection<OrderDetail> OrderDetails { get; set; }

        public string OrderCode { get; set; } // 🆕 Mã đơn hàng duy nhất
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.COD;
        public bool IsPaid { get; set; } = false;

        public string? CancelReason { get; set; }
        public DateTime? CancelRequestedAt { get; set; }
        public string? CancelRequestedBy { get; set; }
    }
}
