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

            // 1. SỬA: Lấy tất cả đơn hàng trong ngày, cả Admin và Nhân viên đều thấy hết để còn làm nước
            var query = _context.Orders
                .Include(o => o.CafeTable)
                .Where(o => o.CreatedAt.Date == dateToFilter.Date)
                .AsQueryable();

            var ordersInDay = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            // 2. GIỮ NGUYÊN: Chỉ Admin mới thấy các thông số tổng kết (Doanh thu, số đơn...)
            if (User.IsInRole("Admin"))
            {
                decimal total = ordersInDay
                    .Where(o => o.Status == "Đã thanh toán")
                    .Sum(o => o.TotalAmount);

                ViewBag.TotalRevenue = total.ToString("N0");
                ViewBag.SuccessOrders = ordersInDay.Count(o => o.Status == "Đã thanh toán");
                ViewBag.CanceledOrders = ordersInDay.Count(o => o.Status == "Đã hủy");
            }

            return View(ordersInDay);
        }

        [HttpGet]
        public async Task<IActionResult> PrintBill(int? id)
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

        public async Task<IActionResult> Details(int? id)
        {
            // 3. SỬA: ĐÃ XÓA ĐOẠN CHẶN NHÂN VIÊN. Giờ Nhân viên vào xem được chi tiết món khách gọi.
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

            if (order.Status == "Đã hủy")
            {
                TempData["ErrorMsg"] = "Đơn hàng đã hủy, không thể thay đổi!";
                return RedirectToAction(nameof(Details), new { id = order.OrderId });
            }

            // 4. CHUẨN NGHIỆP VỤ: Bạn làm chỗ này rất tốt, khóa quyền Hủy của nhân viên
            if (status == "Đã hủy" && !User.IsInRole("Admin"))
            {
                TempData["ErrorMsg"] = "Bạn không có quyền hủy đơn!";
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

            if (status == "Đã hủy")
            {
                order.PaidAt = null; // Đã hủy thì clear giờ thanh toán (nếu có)
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Cập nhật trạng thái đơn hàng thành công!";

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
    }
}