using CafeManagementMVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;

namespace CafeManagementMVC.Controllers
{
    [Authorize]
    public class CafeTablesController : Controller
    {
        private readonly CafeDbContext _context;

        public CafeTablesController(CafeDbContext context)
        {
            _context = context;
        }

        // GET: CafeTables
        public async Task<IActionResult> Index()
        {
            return View(await _context.CafeTables.ToListAsync());
        }

        // GET: CafeTables/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cafeTable = await _context.CafeTables
                .FirstOrDefaultAsync(m => m.TableId == id);
            if (cafeTable == null)
            {
                return NotFound();
            }

            return View(cafeTable);
        }

        // GET: CafeTables/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: CafeTables/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("TableId,TableName,Status")] CafeTable cafeTable)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cafeTable);
                await _context.SaveChangesAsync(); // Lưu để lấy TableId trước

                string ipAddress = "192.168.1.190"; // IP máy bạn
                string port = "5042";

                string domain = $"http://{ipAddress}:{port}";
                string orderUrl = $"{domain}/Home/Menu?tableId={cafeTable.TableId}";

                using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(orderUrl, QRCodeGenerator.ECCLevel.Q))
                using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                {
                    byte[] qrCodeAsPngByteArr = qrCode.GetGraphic(20);

                    // Lưu file ảnh vào wwwroot/qrcodes
                    string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "qrcodes");
                    if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                    string fileName = $"Table_{cafeTable.TableId}.png";
                    string filePath = Path.Combine(folderPath, fileName);

                    System.IO.File.WriteAllBytes(filePath, qrCodeAsPngByteArr);

                    // Cập nhật đường dẫn vào database
                    cafeTable.QRCodeUrl = "/qrcodes/" + fileName;
                    _context.Update(cafeTable);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            return View(cafeTable);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var cafeTable = await _context.CafeTables.FindAsync(id);
            if (cafeTable == null) return NotFound();

            // SỬA DÒNG NÀY: Trả về PartialView thay vì View
            return PartialView(cafeTable);
        }

        // POST: CafeTables/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("TableId,TableName,Status,QRCodeUrl")] CafeTable cafeTable)
        {
            if (id != cafeTable.TableId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cafeTable);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CafeTableExists(cafeTable.TableId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(cafeTable);
        }

        // GET: CafeTables/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cafeTable = await _context.CafeTables
                .FirstOrDefaultAsync(m => m.TableId == id);
            if (cafeTable == null)
            {
                return NotFound();
            }

            return View(cafeTable);
        }

        // POST: CafeTables/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cafeTable = await _context.CafeTables.FindAsync(id);

            if (cafeTable != null)
            {
                // 🔥 XÓA FILE QR
                if (!string.IsNullOrEmpty(cafeTable.QRCodeUrl))
                {
                    string filePath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        cafeTable.QRCodeUrl.TrimStart('/')
                    );

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                // 🔥 XÓA DB
                _context.CafeTables.Remove(cafeTable);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CafeTableExists(int id)
        {
            return _context.CafeTables.Any(e => e.TableId == id);
        }
    }
}
