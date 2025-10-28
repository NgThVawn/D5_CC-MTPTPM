using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebBanHang.Models;
using WedBanHang.Models;

namespace WebBanHang.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Revenue()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetRevenue(string type, string date)
        {
            DateTime selectedDate = DateTime.Parse(date);
            var completedStatus = OrderStatus.HoanThanh;

            var query = _context.Orders
                .Where(o => o.Status == completedStatus)
                .Include(o => o.OrderDetails)
                .AsQueryable();

            if (type == "day")
            {
                query = query.Where(o => o.OrderDate.Date == selectedDate.Date);
            }
            else if (type == "month")
            {
                query = query.Where(o => o.OrderDate.Month == selectedDate.Month && o.OrderDate.Year == selectedDate.Year);
            }
            else if (type == "year")
            {
                query = query.Where(o => o.OrderDate.Year == selectedDate.Year);
            }

            decimal totalRevenue = query
                .SelectMany(o => o.OrderDetails)
                .Sum(od => od.Quantity * od.Price);

            return Json(new { success = true, revenue = totalRevenue });
        }

        [HttpPost]
        public JsonResult GetProductRevenue(string type, string date)
        {
            DateTime selectedDate = DateTime.Parse(date);
            var completedStatus = OrderStatus.HoanThanh;

            var query = _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.Status == completedStatus)
                .AsQueryable();

            if (type == "day")
            {
                query = query.Where(od => od.Order.OrderDate.Date == selectedDate.Date);
            }
            else if (type == "month")
            {
                query = query.Where(od => od.Order.OrderDate.Month == selectedDate.Month && od.Order.OrderDate.Year == selectedDate.Year);
            }
            else if (type == "year")
            {
                query = query.Where(od => od.Order.OrderDate.Year == selectedDate.Year);
            }

            var productRevenues = query
                .GroupBy(od => new { od.ProductId, od.Product.Name })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    QuantitySold = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.Price)
                })
                .ToList();

            return Json(new { success = true, data = productRevenues });
        }

        [HttpPost]
        public JsonResult GetAllOrdersSummary(string type, string date)
        {
            DateTime selectedDate = DateTime.Parse(date);
            var query = _context.Orders.Include(o => o.ApplicationUser).AsQueryable();

            if (type == "day")
                query = query.Where(o => o.OrderDate.Date == selectedDate.Date);
            else if (type == "month")
                query = query.Where(o => o.OrderDate.Month == selectedDate.Month && o.OrderDate.Year == selectedDate.Year);
            else if (type == "year")
                query = query.Where(o => o.OrderDate.Year == selectedDate.Year);

            var orders = query.Select(o => new
            {
                orderId = o.Id,
                orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                customerName = o.ApplicationUser.FullName,
                totalAmount = o.OrderDetails.Sum(od => od.Price * od.Quantity),
                status = o.Status.ToFriendlyString()
            }).ToList();

            var statusSummary = query
                .GroupBy(o => o.Status)
                .Select(g => new {
                    status = g.Key.ToString(),
                    statusFriendly = g.Key.ToFriendlyString(),
                    count = g.Count()
                }).ToList();

            return Json(new { success = true, orders, statusSummary });
        }


        [HttpPost]
        public JsonResult GetOrderStatsByStatus(string type, string date, string status)
        {
            DateTime selectedDate = DateTime.Parse(date);

            if (!Enum.TryParse<OrderStatus>(status, out var statusEnum))
                return Json(new { success = false });

            var query = _context.Orders
                .Include(o => o.ApplicationUser) // ✅ Include phải nằm trước Where
                .Where(o => o.Status == statusEnum);

            if (type == "day")
                query = query.Where(o => o.OrderDate.Date == selectedDate.Date);
            else if (type == "month")
                query = query.Where(o => o.OrderDate.Month == selectedDate.Month && o.OrderDate.Year == selectedDate.Year);
            else if (type == "year")
                query = query.Where(o => o.OrderDate.Year == selectedDate.Year);

            var orders = query.Select(o => new
            {
                orderId = o.Id,
                orderDate = o.OrderDate.ToString("dd/MM/yyyy HH:mm"),
                customerName = o.ApplicationUser.FullName,
                totalAmount = o.OrderDetails.Sum(od => od.Price * od.Quantity),
                status = o.Status.ToFriendlyString()
            }).ToList();

            return Json(new
            {
                success = true,
                count = orders.Count,
                statusName = statusEnum.ToFriendlyString(),
                orders
            });
        }


    }
}
