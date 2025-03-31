using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;

namespace PhamTaManhLan_8888.Areas.Admin.Controllers
{
	[Area("Admin")] // Xác định controller này thuộc khu vực Admin
	[Authorize(Roles = "Admin")] // Chỉ Admin mới có quyền truy cập vào controller này
	public class ProductController : Controller
	{
		private readonly IProductRepository _productRepository; // Repository quản lý sản phẩm
		private readonly ICategoryRepository _categoryRepository; // Repository quản lý danh mục sản phẩm

		public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
		{
			_productRepository = productRepository;
			_categoryRepository = categoryRepository;
		}

		private IActionResult CheckAdminAccess()
		{
			// Kiểm tra nếu người dùng không có quyền Admin thì chuyển hướng về trang Access Denied
			if (!User.IsInRole("Admin"))
			{
				return RedirectToAction("AccessDenied", "Account", new { area = "Identity" });
			}
			return null;
		}

		public async Task<IActionResult> Index()
		{
			var accessCheck = CheckAdminAccess();
			if (accessCheck != null) return accessCheck;

			var products = await _productRepository.GetAllAsync(); // Lấy danh sách tất cả sản phẩm
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
			var savePath = Path.Combine("wwwroot/images", image.FileName);
			using (var fileStream = new FileStream(savePath, FileMode.Create))
			{
				await image.CopyToAsync(fileStream); // Lưu file ảnh vào thư mục
			}
			return "/images/" + image.FileName; // Trả về đường dẫn ảnh để lưu vào database
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
