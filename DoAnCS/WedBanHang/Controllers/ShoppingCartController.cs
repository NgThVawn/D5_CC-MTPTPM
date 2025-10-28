using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Helpers;
using WebBanHang.Models;
using WebBanHang.ViewModels;
using WedBanHang.Models;

namespace WebBanHang.Controllers
{
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ShoppingCartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Hiển thị giỏ hàng
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _context.ShoppingCarts
                .Include(c => c.Product)
                    .ThenInclude(p => p.Images)
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            var promotions = await _context.Promotions
                .Include(p => p.PromotionProducts)
                .Include(p => p.PromotionCategories)
                .Where(p => p.IsActive && DateTime.Now >= p.StartDate && DateTime.Now <= p.EndDate)
                .ToListAsync();


            var activeItems = cartItems
                .Where(c => c.Product.IsAvailable && c.Product.StockQuantity > 0)
                .Select(c =>
                {
                    var finalPrice = PromotionHelper.GetFinalPrice(c.Product, promotions);
                    return new ShoppingCartItemViewModel
                    {
                        Id = c.Id,
                        ProductName = c.Product.Name,
                        ImageUrl = c.Product.Images?.FirstOrDefault()?.Url ?? "/images/no-image.png",
                        Price = c.Product.Price,
                        FinalPrice = finalPrice,
                        Quantity = c.Count,
                        IsAvailable = c.Product.IsAvailable,
                        StockQuantity = c.Product.StockQuantity
                    };
                })
                .ToList();


            var inactiveItems = cartItems
                .Where(c => !c.Product.IsAvailable || c.Product.StockQuantity == 0)
                .Select(c => new ShoppingCartItemViewModel
                {
                    Id = c.Id,
                    ProductName = c.Product.Name,
                    ImageUrl = c.Product.Images?.FirstOrDefault()?.Url ?? "/images/no-image.png",
                    Price = c.Product.Price,
                    Quantity = c.Count,
                    IsAvailable = c.Product.IsAvailable,
                    StockQuantity = c.Product.StockQuantity
                })
                .ToList();

            var viewModel = new ShoppingCartViewModel
            {
                ActiveItems = activeItems,
                InactiveItems = inactiveItems
            };

            return View(viewModel);
        }



        // Thêm sản phẩm vào giỏ
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int count = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null || !product.IsAvailable || product.StockQuantity <= 0)
            {
                return BadRequest(new { success = false, message = "Sản phẩm không khả dụng." });
            }

            var cartItem = await _context.ShoppingCarts
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductId == productId);

            if (cartItem == null)
            {
                if (count > product.StockQuantity)
                {
                    count = product.StockQuantity;
                }

                cartItem = new ShoppingCart
                {
                    ApplicationUserId = userId,
                    ProductId = productId,
                    Count = count
                };
                _context.ShoppingCarts.Add(cartItem);
            }
            else
            {
                // Nếu số lượng trong giỏ đã đủ tồn kho, không cho tăng nữa
                if (cartItem.Count < product.StockQuantity)
                {
                    int remainingStock = product.StockQuantity - cartItem.Count;
                    cartItem.Count += Math.Min(count, remainingStock);
                }
                // Ngược lại: không cộng gì cả (đã đạt giới hạn)
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        // Xóa sản phẩm khỏi giỏ
        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int cartId)
        {
            var cartItem = await _context.ShoppingCarts.FindAsync(cartId);

            if (cartItem != null)
            {
                _context.ShoppingCarts.Remove(cartItem);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }


        [HttpPost]
        public async Task<IActionResult> UpdateCart(int cartId, int count)
        {
            var cartItem = await _context.ShoppingCarts
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartId);

            if (cartItem == null)
            {
                return Json(new { success = false });
            }

            if (count < 1)
                count = 1;
            if (count > cartItem.Product.StockQuantity)
                count = cartItem.Product.StockQuantity;

            cartItem.Count = count;
            await _context.SaveChangesAsync();

            var subtotal = cartItem.Product.Price * cartItem.Count;

            return Json(new
            {
                success = true,
                newCount = cartItem.Count,
                newSubtotal = subtotal.ToString("N0")
            });
        }
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var userId = _userManager.GetUserId(User);
            var count = _context.ShoppingCarts.Where(c => c.ApplicationUserId == userId).Count();

            return Json(new { count });
        }


    }

}
