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
        // Đã thêm tham số int quantity vào hàm AddToCart
        public async Task<IActionResult> AddToCart(int productId, string size, decimal price, int quantity = 1)
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
                existingItem.Quantity += quantity; // Cộng dồn số lượng
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Size = size,
                    Price = price,
                    Quantity = quantity, // Gắn số lượng chọn từ bên ngoài
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

        [HttpPost]
        public IActionResult ChangeItemSize(int productId, string oldSize, string newSize)
        {
            var cartJson = HttpContext.Session.GetString("POS_Cart");
            if (string.IsNullOrEmpty(cartJson)) return Json(new { success = false });

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson);
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == oldSize);

            if (item != null && oldSize != newSize)
            {
                // 1. Tìm giá gốc (Size S)
                decimal basePrice = item.Price;
                if (oldSize == "M") basePrice = item.Price / 1.2m;
                else if (oldSize == "L") basePrice = item.Price / 1.5m;

                // 2. Tính giá mới theo Size mới
                decimal newPrice = basePrice;
                if (newSize == "M") newPrice = basePrice * 1.2m;
                else if (newSize == "L") newPrice = basePrice * 1.5m;

                // 3. Kiểm tra xem size mới này đã có trong giỏ chưa, có thì gộp lại
                var existingItem = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == newSize);
                if (existingItem != null)
                {
                    existingItem.Quantity += item.Quantity;
                    cart.Remove(item);
                }
                else
                {
                    item.Size = newSize;
                    item.Price = newPrice;
                }

                HttpContext.Session.SetString("POS_Cart", JsonSerializer.Serialize(cart));
            }

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
        public IActionResult UpdateQuantity(int productId, string size, int change)
        {
            var cartJson = HttpContext.Session.GetString("POS_Cart");
            if (string.IsNullOrEmpty(cartJson)) return Json(new { success = false });

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson);
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);

            if (item != null)
            {
                item.Quantity += change; // change sẽ là +1 hoặc -1

                // Nếu số lượng tụt xuống 0 hoặc âm thì xóa luôn món đó
                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }

                // Lưu lại Session
                HttpContext.Session.SetString("POS_Cart", JsonSerializer.Serialize(cart));
            }

            return Json(new
            {
                success = true,
                totalItems = cart.Sum(c => c.Quantity),
                totalPrice = cart.Sum(c => c.TotalPrice)
            });
        }

        [HttpPost]
        public IActionResult RemoveItem(int productId, string size)
        {
            var cartJson = HttpContext.Session.GetString("POS_Cart");
            if (string.IsNullOrEmpty(cartJson)) return Json(new { success = false });

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson);
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);

            if (item != null)
            {
                cart.Remove(item); // Xóa thẳng tay
                HttpContext.Session.SetString("POS_Cart", JsonSerializer.Serialize(cart));
            }

            return Json(new
            {
                success = true,
                totalItems = cart.Sum(c => c.Quantity),
                totalPrice = cart.Sum(c => c.TotalPrice)
            });
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

                // Lấy ID đơn hàng vừa được tạo thành công
                int orderIdToPrint = newOrder.OrderId;

                // Tạo đường link dẫn tới trang In Bill
                string printUrl = Url.Action("PrintBill", "Orders", new { id = orderIdToPrint });

                // 5. Xóa giỏ hàng khỏi Session
                HttpContext.Session.Remove("POS_Cart");
                HttpContext.Session.Remove("SelectedTableId");
                HttpContext.Session.Remove("SelectedTableName");

                // Trả về kèm theo URL để JavaScript mở trang in
                return Json(new
                {
                    success = true,
                    redirectUrl = printUrl
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi khi thanh toán: " + ex.Message });
            }
        }
    }
}