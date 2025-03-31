using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;
using System.Threading.Tasks;

namespace PhamTaManhLan_8888.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Authorize(Roles = "Admin")] // Chỉ Admin mới có quyền
	public class CategoryController : Controller
	{
		private readonly ICategoryRepository _categoryRepository;

		public CategoryController(ICategoryRepository categoryRepository)
		{
			_categoryRepository = categoryRepository;
		}

		// Hiển thị danh sách danh mục
		public async Task<IActionResult> Index()
		{
			var categories = await _categoryRepository.GetAllAsync();
			Console.WriteLine($"Số lượng danh mục: {categories?.Count() ?? 0}");
			return View(categories);
		}


		// Hiển thị form thêm danh mục
		public IActionResult Create()
		{
			return View();
		}

		// Xử lý thêm danh mục
		[HttpPost]
		public async Task<IActionResult> Create(Category category)
		{

			if (!ModelState.IsValid)
			{
				Console.WriteLine("ModelState không hợp lệ!");
				return View(category);
			}

			await _categoryRepository.AddAsync(category);
			Console.WriteLine($"Đã thêm danh mục: {category.Name}");
			return RedirectToAction(nameof(Index));
		}

		// Hiển thị form cập nhật danh mục
		public async Task<IActionResult> Edit(int id)
		{
			var category = await _categoryRepository.GetByIdAsync(id);
			if (category == null) return NotFound();
			return View(category);
		}

		// Xử lý cập nhật danh mục
		[HttpPost]
		public async Task<IActionResult> Edit(int id, Category category)
		{
			if (id != category.Id) return NotFound();

			if (!ModelState.IsValid)
				return View(category);

			await _categoryRepository.UpdateAsync(category);
			TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";
			return RedirectToAction(nameof(Index));
		}

		// Xác nhận xóa danh mục
		public async Task<IActionResult> Delete(int id)
		{
			var category = await _categoryRepository.GetByIdAsync(id);
			if (category == null) return NotFound();
			return View(category);
		}

		// Xử lý xóa danh mục
		[HttpPost, ActionName("Delete")]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			await _categoryRepository.DeleteAsync(id);
			TempData["SuccessMessage"] = "Xóa danh mục thành công!";
			return RedirectToAction(nameof(Index));
		}
	}
}
