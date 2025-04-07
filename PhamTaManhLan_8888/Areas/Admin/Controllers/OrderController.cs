using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhamTaManhLan_8888.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhamTaManhLan_8888.Areas.Admin.Controllers
{
    [Area("Admin")] // Define Admin area
    [Authorize(Roles = "Admin,Employee")] // Only Admin & Employee have access
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🟢 Admin: Display list of orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                                       .Include(o => o.ApplicationUser)
                                       .ToListAsync();
            return View(orders);
        }

        // 🟢 Admin: Display order details
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

        // 🔴 Only Admin can delete an order
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

        // 🟠 Update order status (Admin & Employee)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Đơn hàng không tồn tại.";
                return RedirectToAction("Index");
            }

            // Update order status
            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("Details", new { id });
        }
    }
}

namespace PhamTaManhLan_8888.Controllers
{
    [Authorize(Roles = "Customer")] // Only Customers can access this
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrderController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 🟢 Display customer's orders
        public async Task<IActionResult> MyOrderItems()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var orderItems = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order.UserId == user.Id)
                .OrderByDescending(od => od.Order.OrderDate) // Sort by order date
                .ToListAsync();

            return View(orderItems);
        }

        // 🟠 Display order details for customers
        public async Task<IActionResult> OrderDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var order = await _context.Orders
                .Include(o => o.ApplicationUser)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
