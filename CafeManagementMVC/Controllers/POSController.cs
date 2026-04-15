using CafeManagementMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;


namespace CafeManagementMVC.Controllers
{
    [Authorize]
    public class POSController : Controller
    {
        private readonly CafeDbContext _context;

        public POSController(CafeDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tables = await _context.CafeTables.ToListAsync();
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Products = await _context.Products.Where(p => p.IsAvailable == true).ToListAsync();

            return View(tables);
        }

        [HttpPost]
        public IActionResult SelectTable(int tableId, string tableName)
        {
            HttpContext.Session.SetInt32("SelectedTableId", tableId);
            HttpContext.Session.SetString("SelectedTableName", tableName);

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, string size, decimal price)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return Json(new { success = false });

            var cartJson = HttpContext.Session.GetString("POS_Cart");
            List<CartItem> cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson);

            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Size = size,
                    Price = price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl ?? ""
                });
            }

            HttpContext.Session.SetString("POS_Cart", JsonSerializer.Serialize(cart));

            return Json(new
            {
                success = true,
                totalItems = cart.Sum(c => c.Quantity),
                totalPrice = cart.Sum(c => c.TotalPrice)
            });
        }

        [HttpGet]
        public IActionResult GetCartItems()
        {
            var cartJson = HttpContext.Session.GetString("POS_Cart");
            List<CartItem> cart = string.IsNullOrEmpty(cartJson)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(cartJson);

            return PartialView("_CartPartial", cart);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(int tableId)
        {
            // Lấy giỏ hàng từ Session
            var cartJson = HttpContext.Session.GetString("POS_Cart");
            if (string.IsNullOrEmpty(cartJson))
            {
                return Json(new { success = false, message = "Giỏ hàng đang trống!" });
            }

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson);
            if (cart == null || !cart.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng đang trống!" });
            }

            // Lấy ID của nhân viên đang đăng nhập từ hệ thống
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdString, out int currentUserId))
            {
                return Json(new { success = false, message = "Không xác định được người dùng!" });
            }

            // Bắt đầu Transaction để đảm bảo an toàn dữ liệu
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Tạo đơn hàng mới (Order) và lưu ID nhân viên
                var newOrder = new Order
                {
                    TableId = tableId,
                    UserId = currentUserId, // Cột này giờ đã được gán tự động
                    TotalAmount = cart.Sum(c => c.TotalPrice),
                    Status = "Đã thanh toán",
                    CreatedAt = DateTime.Now,
                    PaidAt = DateTime.Now
                };

                _context.Orders.Add(newOrder);
                await _context.SaveChangesAsync();

                // 2. Tạo danh sách chi tiết đơn hàng (OrderDetails)
                foreach (var item in cart)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = newOrder.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Size = item.Size
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                // 3. Đổi trạng thái Bàn thành "Trống"
                var table = await _context.CafeTables.FindAsync(tableId);
                if (table == null)
                {
                    return Json(new { success = false, message = "Bàn không tồn tại!" });
                }

                table.Status = "Trống";
                _context.CafeTables.Update(table);

                // 4. Lưu toàn bộ thay đổi và Commit Transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 5. Xóa giỏ hàng khỏi Session
                HttpContext.Session.Remove("POS_Cart");
                HttpContext.Session.Remove("SelectedTableId");
                HttpContext.Session.Remove("SelectedTableName");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi khi thanh toán: " + ex.Message });
            }
        }
    }
}