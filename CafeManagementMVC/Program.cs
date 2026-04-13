using Microsoft.EntityFrameworkCore;
using CafeManagementMVC.Models; // Lưu ý: Nếu project của bạn tên khác, hãy sửa đổi namespace này cho khớp

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 1. CẤU HÌNH KẾT NỐI DATABASE SQL SERVER
// =======================================================
builder.Services.AddDbContext<CafeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// =======================================================
// 2. CẤU HÌNH ĐĂNG NHẬP (AUTHENTICATION) BẰNG COOKIE
// =======================================================
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.Cookie.Name = "CafeManagementCookie";
        config.LoginPath = "/Account/Login"; // Trỏ tới trang đăng nhập (sẽ làm ngay sau đây)
        config.AccessDeniedPath = "/Home/AccessDenied"; // Trỏ tới trang báo lỗi không đủ quyền
        config.ExpireTimeSpan = TimeSpan.FromHours(8); // Lưu đăng nhập trong 8 tiếng
    });

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();


app.UseSession();
// =======================================================
// 3. KÍCH HOẠT AUTHENTICATION (Bắt buộc phải nằm TRƯỚC UseAuthorization)
// =======================================================
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();