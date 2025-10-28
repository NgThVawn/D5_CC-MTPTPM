using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.ViewModels;
using WedBanHang.Models;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // [1] Từ trang sản phẩm: MUA NGAY
        [HttpGet] // Chuyển từ POST sang GET
        public async Task<IActionResult> BuyNow(int productId, int quantity = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user.IsRestricted)
            {
                return Forbid(); // Hoặc RedirectToAction("AccessDenied");
            }

            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null || !product.IsAvailable || product.StockQuantity <= 0)
            {
                return BadRequest("Sản phẩm không khả dụng.");
            }

            var checkoutItem = new List<CheckoutItemViewModel>
            {
                new CheckoutItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductImage = product.Images?.FirstOrDefault()?.Url ?? product.ImageUrl ?? "/images/no-image.png",
                    Price = product.Price,
                    Quantity = quantity,
                    StockQuantity = product.StockQuantity
                }
            };

            TempData["CheckoutItems"] = JsonConvert.SerializeObject(checkoutItem);
            return RedirectToAction("Checkout");
        }

        // [2] Từ trang giỏ hàng: chọn nhiều sản phẩm để thanh toán
        [HttpPost]
        public async Task<IActionResult> CheckoutFromCart(List<int> selectedItems)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var carts = await _context.ShoppingCarts
                .Include(c => c.Product)
                    .ThenInclude(p => p.Images)
                .Where(c => c.ApplicationUserId == userId && selectedItems.Contains(c.Id))
                .ToListAsync();

            var checkoutItems = carts.Select(c => new CheckoutItemViewModel
            {
                ProductId = c.Product.Id,
                ProductName = c.Product.Name,
                ProductImage = c.Product.Images?.FirstOrDefault()?.Url ?? c.Product.ImageUrl ?? "/images/no-image.png",
                Price = c.Product.Price,
                Quantity = c.Count,
                StockQuantity = c.Product.StockQuantity
            }).ToList();

            TempData["CheckoutItems"] = JsonConvert.SerializeObject(checkoutItems);
            return RedirectToAction("Checkout");
        }


        // [3] Hiển thị trang thanh toán
        public async Task<IActionResult> Checkout()
        {
            if (TempData["CheckoutItems"] == null)
                return RedirectToAction("Index", "Home");

            var items = JsonConvert.DeserializeObject<List<CheckoutItemViewModel>>(TempData["CheckoutItems"].ToString());

            // ✅ Lấy khuyến mãi đang hoạt động
            var promotions = await _context.Promotions
                .Include(p => p.PromotionProducts)
                .Include(p => p.PromotionCategories)
                .Where(p => p.IsActive && DateTime.Now >= p.StartDate && DateTime.Now <= p.EndDate)
                .ToListAsync();

            // ✅ Gắn giá đã giảm
            foreach (var item in items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                item.FinalPrice = PromotionHelper.GetFinalPrice(product, promotions);
            }

            // Giữ lại dữ liệu cho các request tiếp theo
            TempData["CheckoutItems"] = JsonConvert.SerializeObject(items);

            // Thông báo thiếu thông tin cá nhân
            if (TempData["MissingUserInfo"] != null)
            {
                ViewBag.MissingUserInfo = true;
            }
            var user = await _userManager.GetUserAsync(User);
            ViewBag.Address = user?.Address;

            return View(items);
        }



        //[4] Xác nhận thanh toán
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder(List<CheckoutItemViewModel> checkoutItems, string paymentMethod)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);

            // Kiểm tra thông tin người dùng
            if (string.IsNullOrWhiteSpace(user.FullName) || string.IsNullOrWhiteSpace(user.Address) || string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                TempData["MissingUserInfo"] = true;
                TempData["CheckoutItems"] = JsonConvert.SerializeObject(checkoutItems);
                return RedirectToAction("Checkout");
            }

            // Kiểm tra tính hợp lệ của sản phẩm
            var invalidItems = new List<string>();
            foreach (var item in checkoutItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null || !product.IsAvailable || product.StockQuantity < item.Quantity)
                {
                    invalidItems.Add(product?.Name ?? "Sản phẩm không xác định");
                }
            }

            if (invalidItems.Any())
            {
                TempData["Error"] = "Một số sản phẩm không khả dụng hoặc hết hàng: " + string.Join(", ", invalidItems);
                return RedirectToAction("Index", "ShoppingCart");
            }

            // Parse phương thức thanh toán từ string
            PaymentMethod selectedMethod = PaymentMethod.COD; 
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                Enum.TryParse(paymentMethod, ignoreCase: true, out selectedMethod);
            }


            // Tạo mã đơn hàng
            var random = new Random();
            var orderCode = $"DH{random.Next(10000, 99999)}";

            // Tạo đơn hàng mới
            var order = new Order
            {
                ApplicationUserId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.ChoXacNhan,
                PaymentMethod = selectedMethod,
                OrderCode = orderCode,
                IsPaid = (selectedMethod == PaymentMethod.COD)
            };
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Thêm chi tiết đơn hàng và cập nhật số lượng tồn kho
            foreach (var item in checkoutItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                product.StockQuantity -= item.Quantity;
                if (product.StockQuantity <= 0)
                {
                    product.StockQuantity = 0;
                    product.IsAvailable = false;
                }
                _context.Products.Update(product);

                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.FinalPrice
                };
                _context.OrderDetails.Add(orderDetail);
            }

            // Xóa sản phẩm khỏi giỏ hàng
            var toRemove = _context.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId && checkoutItems.Select(i => i.ProductId).Contains(c.ProductId));
            _context.ShoppingCarts.RemoveRange(toRemove);

            await _context.SaveChangesAsync();
            return RedirectToAction("Success");
        }


        //[5] Trang đặt hàng thành công
        public IActionResult Success()
        {
            return View();
        }

        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.ApplicationUserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // ✅ Lấy danh sách các đánh giá còn tồn tại của user (theo OrderId + ProductId)
            var reviewedProductKeys = await _context.Reviews
                .Where(r => r.ApplicationUserId == userId)
                .Select(r => new { r.OrderId, r.ProductId })
                .ToListAsync();

            // ✅ Chuyển sang HashSet để dùng trong View
            var reviewedSet = new HashSet<string>(
                reviewedProductKeys.Select(r => $"{r.OrderId}_{r.ProductId}")
            );

            ViewBag.ReviewedProductIds = reviewedSet;

            return View(orders);
        }


        [HttpPost]
        public async Task<JsonResult> ValidateUserInfo()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);

            var isValid = !string.IsNullOrEmpty(user.FullName) &&
                          !string.IsNullOrEmpty(user.Address) &&
                          !string.IsNullOrEmpty(user.PhoneNumber);

            return Json(new { success = isValid });
        }

        [HttpPost]
        public async Task<IActionResult> VerifyPayment(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null || order.PaymentMethod != PaymentMethod.BankTransfer)
            {
                return NotFound();
            }

            // ⚠️ Không được đánh dấu IsPaid ở đây
            // Chờ admin xác minh thật sự sau khi kiểm tra giao dịch

            return RedirectToAction("Success");
        }

    }
}
