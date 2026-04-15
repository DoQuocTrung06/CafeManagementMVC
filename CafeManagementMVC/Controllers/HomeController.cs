using CafeManagementMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using Microsoft.AspNetCore.Http; // Thêm thư viện xử lý Session
using System.Text.Json; // Thêm thư viện xử lý JSON
using Microsoft.EntityFrameworkCore; // Thêm để dùng Include()
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;


namespace CafeManagementMVC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly CafeDbContext _context;

        public HomeController(ILogger<HomeController> logger, CafeDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        
        [AllowAnonymous]
        public IActionResult Menu(int tableId)
        {
            
            var products = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsAvailable)
                .ToList();

            ViewBag.TableId = tableId;

            return View(products);
        }

        
        private List<CartItem> GetCartItems()
        {
            var sessionCart = HttpContext.Session.GetString("CartSession");
            if (sessionCart == null)
                return new List<CartItem>();

            return JsonSerializer.Deserialize<List<CartItem>>(sessionCart);
        }

        private void SaveCartSession(List<CartItem> cart)
        {
            var jsonCart = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("CartSession", jsonCart);
        }


        [HttpPost]
        [AllowAnonymous]
        public IActionResult AddToCart(int productId, int quantity, int tableId, string size = "S")
        {
            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();

            // 1. TÍNH LẠI GIÁ THEO SIZE (Dựa trên giá gốc Size S trong DB)
            decimal finalPrice = product.Price;
            if (size == "M") finalPrice = product.Price * 1.2m;
            else if (size == "L") finalPrice = product.Price * 1.5m;

            var cart = GetCartItems();

            
            var existingItem = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = finalPrice, // Lưu giá đã nhân
                    Size = size,        // Lưu Size khách chọn
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                });
            }

            SaveCartSession(cart);
            TempData["SuccessMsg"] = $"Đã thêm {quantity} {product.ProductName} (Size {size}) vào giỏ!";
            return RedirectToAction("Menu", new { tableId = tableId });
        }
        //Hien thi gio hang
        [AllowAnonymous]
        public IActionResult Cart(int tableId)
        {
            var cart = GetCartItems();
            ViewBag.TableId = tableId;
            return View(cart);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        [HttpPost]
        [AllowAnonymous]
        public IActionResult UpdateQuantity(int productId, string size, int quantity, int tableId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);

            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity; // Cập nhật số lượng mới
                }
            }

            SaveCartSession(cart);
            return RedirectToAction("Cart", new { tableId = tableId });
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult RemoveFromCart(int productId, string size, int tableId)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(c => c.ProductId == productId && c.Size == size);

            if (item != null)
            {
                cart.Remove(item); // Xóa món khỏi danh sách
                TempData["SuccessMsg"] = $"Đã xóa {item.ProductName} khỏi giỏ!";
            }

            SaveCartSession(cart);
            return RedirectToAction("Cart", new { tableId = tableId });
        }

        // ==========================================
        // THÊM 2 HÀM NÀY VÀO CUỐI HOMECONTROLLER
        // ==========================================

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> PlaceOrder(int tableId)
        {
            var cart = GetCartItems();
            if (cart == null || !cart.Any())
            {
                return RedirectToAction("Cart", new { tableId = tableId });
            }

            try
            {
                // 1. Tạo đơn hàng mới
                var order = new Order
                {
                    TableId = tableId,
                    UserId = null, // Khách tự đặt không có user
                    TotalAmount = cart.Sum(i => i.Price * i.Quantity),
                    Status = "Chờ xác nhận",
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // 2. Lưu chi tiết từng món
                foreach (var item in cart)
                {
                    var detail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Size = item.Size
                    };
                    _context.OrderDetails.Add(detail);
                }

                await _context.SaveChangesAsync();

                // 3. Xóa giỏ hàng bằng đúng Key bạn đang dùng (CartSession)
                HttpContext.Session.Remove("CartSession");

                // Chuyển sang trang thông báo thành công
                return RedirectToAction("OrderSuccess", new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                // GHI CHÚ: Thêm "text/html" vào cuối để trình duyệt HIỂN THỊ lỗi thay vì TẢI FILE TXT
                return Content($"<div style='padding: 20px; font-family: sans-serif;'><h2 style='color:red;'>⚠️ Lỗi đặt món:</h2><p><b>Lỗi:</b> {ex.Message}</p><p><b>Chi tiết (Inner):</b> {ex.InnerException?.Message}</p><button onclick='history.back()'>Quay lại</button></div>", "text/html");
            }
        }

        [AllowAnonymous]
        public IActionResult OrderSuccess(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
        // quan li nhan vien
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}