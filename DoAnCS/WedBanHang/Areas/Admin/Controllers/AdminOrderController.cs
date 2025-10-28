using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WedBanHang.Models;


namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AdminOrderController : Controller
    {
        private readonly ApplicationDbContext _context;


        public AdminOrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách đơn hàng
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Hiển thị chi tiết đơn
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            var order = await _context.Orders.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id);
            if (order != null && order.Status != OrderStatus.HoanThanh && order.Status != OrderStatus.DaHuy)
            {
                // chuyển sang trạng thái kế tiếp
                order.Status += 1;
                await _context.SaveChangesAsync();

                // tạo thông báo tương ứng
                _context.Notifications.Add(new Notification
                {
                    UserId = order.ApplicationUserId,
                    Message = order.Status.ToNotificationMessage(order.Id),
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null && order.Status != OrderStatus.DaHuy && order.Status != OrderStatus.HoanThanh)
            {
                order.Status = OrderStatus.DaHuy;

                foreach (var detail in order.OrderDetails)
                {
                    var product = await _context.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity += detail.Quantity;

                        if (product.StockQuantity > 0 && !product.IsAvailable)
                        {
                            product.IsAvailable = true;
                        }

                        _context.Products.Update(product);
                    }
                }
                _context.Notifications.Add(new Notification
                {
                    UserId = order.ApplicationUserId,
                    Message = OrderStatus.DaHuy.ToNotificationMessage(order.Id),
                    CreatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> NextStatus(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            switch (order.Status)
            {
                case OrderStatus.ChoXacNhan:
                    order.Status = OrderStatus.DangChuanBi;
                    break;
                case OrderStatus.DangChuanBi:
                    order.Status = OrderStatus.DangVanChuyen;
                    break;
                case OrderStatus.DangVanChuyen:
                    order.Status = OrderStatus.DaGiao;
                    break;
                case OrderStatus.DaGiao:
                    order.Status = OrderStatus.HoanThanh;
                    break;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null || order.IsPaid || order.PaymentMethod != PaymentMethod.BankTransfer)
            {
                return NotFound();
            }

            order.IsPaid = true;
            await _context.SaveChangesAsync();

            // (Tuỳ chọn) Gửi thông báo, email, log lịch sử...
            return RedirectToAction("Index");
        }

    }
}
