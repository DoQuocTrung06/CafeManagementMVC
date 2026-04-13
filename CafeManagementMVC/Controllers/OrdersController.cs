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

        // GET: Hiển thị danh sách tất cả đơn hàng (Sắp xếp mới nhất lên đầu)
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.CafeTable)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // GET: Xem chi tiết 1 đơn hàng (Bao gồm các món khách gọi)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.CafeTable)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product) // Kéo theo thông tin Món ăn
                .FirstOrDefaultAsync(m => m.OrderId == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Cập nhật trạng thái đơn hàng (Dùng ở trang Details)
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

            
            order.Status = status;

            
            if (status == "Đã thanh toán")
            {
                order.PaidAt = DateTime.Now;
            }
            else
            {
                
                order.PaidAt = null;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Đã cập nhật trạng thái đơn hàng thành công!";

            return RedirectToAction(nameof(Details), new { id = order.OrderId });
        }
    }
}