using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;
using System.Linq;

namespace PhamTaManhLan_8888.Areas.Admin.Controllers
{
	[Area("Admin")] // Xác định controller này thuộc khu vực Admin
	[Authorize(Roles = "Admin,Employee")] // Cho phép cả Admin và Employee truy cập
	public class ProductController : Controller
	{
		private readonly IProductRepository _productRepository; // Repository quản lý sản phẩm
		private readonly ICategoryRepository _categoryRepository; // Repository quản lý danh mục sản phẩm
		private readonly IWebHostEnvironment _webHostEnvironment;

		public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository, IWebHostEnvironment webHostEnvironment)
		{
			_productRepository = productRepository;
			_categoryRepository = categoryRepository;
			_webHostEnvironment = webHostEnvironment;
		}

		private IActionResult CheckAdminAccess()
		{
			// Kiểm tra nếu người dùng không có quyền Admin hoặc Employee thì chuyển hướng
			if (!User.IsInRole("Admin") && !User.IsInRole("Employee"))
			{
				return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
			}
			return null;
		}

		public async Task<IActionResult> Index(string category, string searchTerm)
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			var products = await _productRepository.GetAllAsync(); // Lấy danh sách tất cả sản phẩm

			// Lọc theo danh mục
			if (!string.IsNullOrEmpty(category) && category != "Tất cả")
			{
				products = products.Where(p => p.Category?.Name == category).ToList();
			}

			// Lọc theo từ khóa tìm kiếm (nếu có)
			if (!string.IsNullOrEmpty(searchTerm))
			{
				var lowerSearchTerm = searchTerm.ToLower().Trim();
				products = products.Where(p => p.Name.ToLower().Contains(lowerSearchTerm) ||
											 (p.Category != null && p.Category.Name.ToLower().Contains(lowerSearchTerm))).ToList();
			}

			ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
			ViewBag.CurrentCategory = category; // Truyền danh mục hiện tại về View
			ViewBag.SearchTerm = searchTerm;     // Truyền từ khóa tìm kiếm về View

			return View(products);
		}

		public async Task<IActionResult> Add()
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			// Tải danh sách danh mục sản phẩm để hiển thị trên dropdown
			ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");
			return View();
		}

		[HttpPost]
		public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			ModelState.Remove("ImageUrl"); // Loại bỏ kiểm tra validation cho trường ImageUrl

			if (ModelState.IsValid)
			{
				if (imageUrl != null)
				{
					product.ImageUrl = await SaveImage(imageUrl); // Lưu ảnh và gán đường dẫn vào sản phẩm
				}

				await _productRepository.AddAsync(product); // Thêm sản phẩm vào database
				return RedirectToAction(nameof(Index));
			}

			// Nếu có lỗi validation, hiển thị lại danh mục sản phẩm trong dropdown
			ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
			return View(product);
		}

		public async Task<IActionResult> Update(int id)
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm theo ID
			if (product == null) return NotFound();

			ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
			return View(product);
		}

		[HttpPost]
		public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			ModelState.Remove("ImageUrl");

			if (id != product.Id) return NotFound(); // Kiểm tra ID có khớp với sản phẩm không

			if (ModelState.IsValid)
			{
				var existingProduct = await _productRepository.GetByIdAsync(id);
				if (existingProduct == null) return NotFound();

				// Cập nhật thông tin sản phẩm
				existingProduct.Name = product.Name;
				existingProduct.Price = product.Price;
				existingProduct.Description = product.Description;
				existingProduct.CategoryId = product.CategoryId;

				if (imageUrl != null)
				{
					existingProduct.ImageUrl = await SaveImage(imageUrl); // Cập nhật ảnh mới nếu có
				}

				await _productRepository.UpdateAsync(existingProduct); // Lưu thay đổi vào database
				return RedirectToAction(nameof(Index));
			}

			ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name", product.CategoryId);
			return View(product);
		}

		private async Task<string> SaveImage(IFormFile image)
		{
			var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
			var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}";
			var filePath = Path.Combine(uploadsFolder, uniqueFileName);
			using (var fileStream = new FileStream(filePath, FileMode.Create))
			{
				await image.CopyToAsync(fileStream);
			}
			return "/images/" + uniqueFileName;
		}

		public async Task<IActionResult> Delete(int id)
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			var product = await _productRepository.GetByIdAsync(id); // Tìm sản phẩm cần xóa
			if (product == null) return NotFound();

			return View(product);
		}

		[HttpPost, ActionName("Delete")]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			var product = await _productRepository.GetByIdAsync(id);
			if (product == null) return NotFound();

			await _productRepository.DeleteAsync(id); // Xóa sản phẩm khỏi database
			return RedirectToAction(nameof(Index));
		}

		public async Task<IActionResult> Display(int id)
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			var product = await _productRepository.GetByIdAsync(id); // Lấy thông tin sản phẩm để hiển thị
			if (product == null) return NotFound();

			return View(product);
		}
	}
}