using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeManagementMVC.Models;

namespace CafeManagementMVC.Controllers
{
    public class OrdersController : Controller
    {
        private readonly CafeDbContext _context;

        public OrdersController(CafeDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? filterDate)
        {
            DateTime dateToFilter = filterDate ?? DateTime.Today;
            ViewBag.SelectedDate = dateToFilter.ToString("yyyy-MM-dd");

            var ordersInDay = await _context.Orders
                .Include(o => o.CafeTable)
                .Where(o => o.CreatedAt.Date == dateToFilter.Date)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            decimal total = ordersInDay
                .Where(o => o.Status == "Đã thanh toán")
                .Sum(o => o.TotalAmount);

            ViewBag.TotalRevenue = total.ToString("N0");
            ViewBag.SuccessOrders = ordersInDay.Count(o => o.Status == "Đã thanh toán");
            ViewBag.CanceledOrders = ordersInDay.Count(o => o.Status == "Đã hủy");

            return View(ordersInDay);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.CafeTable)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.Status == "Đã thanh toán")
            {
                TempData["ErrorMsg"] = "Không thể thay đổi đơn hàng đã hoàn tất thanh toán!";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            if (order.Status == "Đã hủy")
            {
                TempData["ErrorMsg"] = "Đơn hàng đã hủy, không thể thay đổi trạng thái!";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            order.Status = status;

            if (status == "Đã thanh toán")
            {
                order.PaidAt = DateTime.Now;

                var table = await _context.CafeTables.FindAsync(order.TableId);
                if (table != null)
                {
                    table.Status = "Trống";
                }
            }
            else
            {
                order.PaidAt = null;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Cập nhật trạng thái đơn hàng thành công!";

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
    }
}