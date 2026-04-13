using CafeManagementMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeManagementMVC.Controllers
{
    [Authorize]
    public class StatisticsController : Controller
    {
        private readonly CafeDbContext _context;

        public StatisticsController(CafeDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string type = "day")
        {
            // =========================
            // Query base (đơn đã thanh toán)
            // =========================
            var paidOrders = _context.Orders
                .Where(o => o.Status == "Đã thanh toán" && o.PaidAt != null);

            List<string> labels;
            List<decimal> data;

            // =========================
            // THEO NGÀY (7 ngày gần nhất)
            // =========================
            if (type == "day")
            {
                var startDate = DateTime.Now.Date.AddDays(-6);

                var rawData = await paidOrders
                    .Where(o => o.PaidAt >= startDate)
                    .GroupBy(o => o.PaidAt.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Sum(x => x.TotalAmount)
                    })
                    .ToListAsync();

                var days = Enumerable.Range(0, 7)
                    .Select(i => startDate.AddDays(i))
                    .ToList();

                labels = days.Select(d => d.ToString("dd/MM")).ToList();

                data = days.Select(d =>
                    rawData.FirstOrDefault(x => x.Date == d)?.Total ?? 0
                ).ToList();
            }

            // =========================
            // THEO THÁNG (12 tháng gần nhất)
            // =========================
            else
            {
                var startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
                    .AddMonths(-11);

                var rawData = await paidOrders
                    .Where(o => o.PaidAt >= startDate)
                    .GroupBy(o => new
                    {
                        Year = o.PaidAt.Value.Year,
                        Month = o.PaidAt.Value.Month
                    })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        Total = g.Sum(x => x.TotalAmount)
                    })
                    .ToListAsync();

                var months = Enumerable.Range(0, 12)
                    .Select(i => startDate.AddMonths(i))
                    .ToList();

                labels = months.Select(m => m.ToString("MM/yyyy")).ToList();

                data = months.Select(m =>
                    rawData.FirstOrDefault(x =>
                        x.Year == m.Year && x.Month == m.Month
                    )?.Total ?? 0
                ).ToList();
            }

            // =========================
            // TOP 5 MÓN BÁN CHẠY
            // =========================
            var topProducts = await _context.OrderDetails
                .Include(x => x.Product)
                .GroupBy(x => x.Product.ProductName)
                .Select(g => new TopProductVM
                {
                    ProductName = g.Key,
                    TotalSold = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(5)
                .ToListAsync();

            var viewModel = new StatisticsVM
            {
                Labels = labels,
                Data = data,
                TopProducts = topProducts
            };

            ViewBag.Type = type;

            return View(viewModel);
        }
    }
}