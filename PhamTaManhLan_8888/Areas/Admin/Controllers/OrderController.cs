using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhamTaManhLan_8888.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhamTaManhLan_8888.Areas.Admin.Controllers
{
	[Area("Admin")] // Xác định khu vực Admin
	[Authorize(Roles = "Admin,Employee")] // Chỉ Admin & Employee mới truy cập
	public class OrderController : Controller
	{
		private readonly ApplicationDbContext _context;

		public OrderController(ApplicationDbContext context)
		{
			_context = context;
		}

		// 🟢 Hiển thị danh sách đơn hàng
		public async Task<IActionResult> Index()
		{
			var orders = await _context.Orders
									   .Include(o => o.ApplicationUser)
									   .ToListAsync();
			return View(orders);
		}

		// 🟢 Hiển thị chi tiết đơn hàng
		public async Task<IActionResult> Details(int id)
		{
			var order = await _context.Orders
				.Include(o => o.ApplicationUser)
				.Include(o => o.OrderDetails)
				.ThenInclude(od => od.Product)
				.FirstOrDefaultAsync(o => o.Id == id);

			if (order == null) return NotFound();
			return View(order);
		}

		// 🔴 Chỉ Admin mới có quyền xóa đơn hàng
		[Authorize(Roles = "Admin")]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var order = await _context.Orders.FindAsync(id);
			if (order == null) return NotFound();

			_context.Orders.Remove(order);
			await _context.SaveChangesAsync();

			TempData["SuccessMessage"] = "Đơn hàng đã được xóa!";
			return RedirectToAction(nameof(Index));
		}

		// 🟠 Cập nhật trạng thái đơn hàng (Employee & Admin đều có thể)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
		{
			Console.WriteLine($"🔍 Debug: Đã nhận ID={id}, Status={status}");

			var order = await _context.Orders.FindAsync(id);
			if (order == null)
			{
				Console.WriteLine("⚠️ Lỗi: Đơn hàng không tồn tại.");
				TempData["ErrorMessage"] = "Đơn hàng không tồn tại.";
				return RedirectToAction("Index");
			}

			// 🟢 Cập nhật trạng thái đơn hàng
			order.Status = status;
			await _context.SaveChangesAsync();

			Console.WriteLine("✅ Cập nhật trạng thái thành công!");
			TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
			return RedirectToAction("Details", new { id });
		}
	}
}
