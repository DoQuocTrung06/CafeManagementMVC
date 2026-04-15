using CafeManagementMVC.Models;
using CafeManagementMVC.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CafeManagementMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly CafeDbContext _context;

        public AccountController(CafeDbContext context)
        {
            _context = context;
        }

        // 1. Hiển thị trang Đăng nhập (GET)
        [HttpGet]
        public IActionResult Login()
        {
            // Nếu đã đăng nhập rồi thì không cho vào trang Login nữa
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // 2. Xử lý khi bấm nút Đăng nhập (POST)
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Tìm User trong DB kèm theo Role (Quyền)
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Password == model.Password);

                // TC03: Sai tài khoản hoặc mật khẩu
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Thông tin đăng nhập không chính xác.");
                    return View(model);
                }

                // TC04: Tài khoản bị khóa (Status = false / 0)
                if (!user.Status)
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa.");
                    return View(model);
                }

                // Đăng nhập thành công -> Tạo Cookie lưu thông tin
                // Đăng nhập thành công -> Tạo Cookie lưu thông tin
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("FullName", user.FullName ?? ""),
                    new Claim(ClaimTypes.Role, user.Role.RoleName), 
                    // SỬA TẠI ĐÂY: Dùng NameIdentifier để POSController lấy được ID
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
                };

                var identity = new ClaimsIdentity(claims, "CookieAuth");
                var principal = new ClaimsPrincipal(identity);

                // Cấu hình ghi nhớ đăng nhập (tùy chọn)
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                // SỬA TẠI ĐÂY: Dùng AuthenticationScheme mặc định để đồng bộ với Program.cs
                await HttpContext.SignInAsync("CookieAuth", principal, authProperties);

                // Chuyển hướng theo Quyền
                if (user.Role.RoleName == "Admin")
                {
                    return RedirectToAction("Index", "Home"); 
                }
                else
                {
                    return RedirectToAction("Index", "POS"); // Nhân viên nên vào thẳng trang Bán hàng
                }
            }
            return View(model);
        }

        // 3. Xử lý Đăng xuất
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login", "Account");
        }
    }
}