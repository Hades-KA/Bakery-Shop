using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;

namespace PhamTaManhLan_8888.Controllers
{
	public class ProductController : Controller
	{
		private readonly IProductRepository _productRepository;
		private readonly ICategoryRepository _categoryRepository;
		private readonly IWebHostEnvironment _webHostEnvironment;

		// Constructor khởi tạo các repository để quản lý sản phẩm, danh mục, và môi trường host web
		public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository, IWebHostEnvironment webHostEnvironment)
		{
			_productRepository = productRepository;
			_categoryRepository = categoryRepository;
			_webHostEnvironment = webHostEnvironment;
		}

		[AllowAnonymous]
		public async Task<IActionResult> Index(string searchTerm = "")
		{
			// Nếu không có từ khóa tìm kiếm, lấy toàn bộ sản phẩm. Nếu có, thực hiện tìm kiếm.
			var products = string.IsNullOrEmpty(searchTerm)
				? await _productRepository.GetAllAsync()
				: await _productRepository.SearchAsync(searchTerm);

			ViewBag.SearchTerm = searchTerm; // Gửi từ khóa tìm kiếm về view để hiển thị lại
			return View(products); // Trả về view danh sách sản phẩm
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Add()
		{
			// Load danh mục để hiển thị trong dropdown list
			await LoadCategoriesAsync();
			return View(); // Trả về view để thêm sản phẩm
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
		{
			ModelState.Remove("ImageUrl"); // Xóa ràng buộc validation của ImageUrl để tránh lỗi validation

			if (!ModelState.IsValid)
			{
				await LoadCategoriesAsync(); // Nếu model không hợp lệ, load lại danh mục và hiển thị form
				return View(product);
			}

			if (imageUrl != null)
			{
				var imagePath = await SaveImageAsync(imageUrl); // Lưu ảnh và lấy đường dẫn
				if (imagePath == null)
				{
					ModelState.AddModelError("", "Lỗi khi tải ảnh lên."); // Nếu lỗi, hiển thị thông báo lỗi
					await LoadCategoriesAsync();
					return View(product);
				}
				product.ImageUrl = imagePath; // Gán đường dẫn ảnh cho sản phẩm
			}

			await _productRepository.AddAsync(product); // Thêm sản phẩm vào database
			return RedirectToAction(nameof(Index)); // Chuyển hướng về danh sách sản phẩm
		}

		[Authorize(Roles = "Admin,Employee")]
		public async Task<IActionResult> Update(int id)
		{
			var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm theo ID
			if (product == null) return NotFound(); // Nếu không tìm thấy, trả về lỗi 404

			await LoadCategoriesAsync(product.CategoryId); // Load danh mục và đánh dấu danh mục hiện tại
			return View(product); // Trả về view chỉnh sửa sản phẩm
		}

		[HttpPost]
		[Authorize(Roles = "Admin,Employee")]
		public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
		{
			ModelState.Remove("ImageUrl"); // Bỏ qua validation cho ImageUrl

			if (id != product.Id) return NotFound(); // Kiểm tra ID có trùng khớp không

			if (!ModelState.IsValid)
			{
				await LoadCategoriesAsync(product.CategoryId); // Nếu dữ liệu không hợp lệ, load danh mục lại
				return View(product);
			}

			var existingProduct = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm hiện có
			if (existingProduct == null) return NotFound(); // Nếu không tồn tại, trả về 404

			if (imageUrl != null)
			{
				var imagePath = await SaveImageAsync(imageUrl); // Lưu ảnh mới
				if (imagePath == null)
				{
					ModelState.AddModelError("", "Lỗi khi tải ảnh lên.");
					await LoadCategoriesAsync(product.CategoryId);
					return View(product);
				}
				DeleteImage(existingProduct.ImageUrl); // Xóa ảnh cũ nếu có
				product.ImageUrl = imagePath;
			}
			else
			{
				product.ImageUrl = existingProduct.ImageUrl; // Nếu không có ảnh mới, giữ ảnh cũ
			}

			// Cập nhật thông tin sản phẩm
			existingProduct.Name = product.Name;
			existingProduct.Price = product.Price;
			existingProduct.Description = product.Description;
			existingProduct.CategoryId = product.CategoryId;
			existingProduct.ImageUrl = product.ImageUrl;

			await _productRepository.UpdateAsync(existingProduct); // Cập nhật vào database
			return RedirectToAction(nameof(Index)); // Chuyển về danh sách sản phẩm
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Delete(int id)
		{
			var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm theo ID
			if (product == null) return NotFound(); // Nếu không tồn tại, trả về lỗi 404
			return View(product); // Hiển thị view xác nhận xóa sản phẩm
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm cần xóa
			if (product == null) return NotFound(); // Nếu không tồn tại, trả về lỗi 404

			DeleteImage(product.ImageUrl); // Xóa ảnh của sản phẩm
			await _productRepository.DeleteAsync(id); // Xóa sản phẩm khỏi database
			return RedirectToAction(nameof(Index)); // Chuyển về danh sách sản phẩm
		}

		public async Task<IActionResult> Display(int id)
		{
			var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm theo ID
			if (product == null) return NotFound(); // Nếu không tồn tại, trả về lỗi 404
			return View(product); // Trả về view hiển thị chi tiết sản phẩm
		}

		private async Task<string?> SaveImageAsync(IFormFile image)
		{
			try
			{
				var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images"); // Đường dẫn thư mục ảnh
				var uniqueFileName = $"{Guid.NewGuid()}_{image.FileName}"; // Đổi tên file để tránh trùng lặp
				var filePath = Path.Combine(uploadsFolder, uniqueFileName); // Tạo đường dẫn đầy đủ
				using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await image.CopyToAsync(fileStream); // Lưu ảnh vào thư mục
				}
				return "/images/" + uniqueFileName; // Trả về đường dẫn ảnh
			}
			catch
			{
				return null; // Nếu có lỗi, trả về null
			}
		}

		private void DeleteImage(string? imageUrl)
		{
			if (!string.IsNullOrEmpty(imageUrl))
			{
				var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/')); // Đường dẫn ảnh
				if (System.IO.File.Exists(imagePath)) // Kiểm tra xem ảnh có tồn tại không
				{
					System.IO.File.Delete(imagePath); // Nếu có, xóa ảnh
				}
			}
		}

		private async Task LoadCategoriesAsync(int? selectedCategoryId = null)
		{
			var categories = await _categoryRepository.GetAllAsync(); // Lấy danh sách danh mục

			// Loại bỏ danh mục trùng lặp
			var distinctCategories = categories
				.GroupBy(c => c.Id)
				.Select(g => g.First())
				.ToList();

			ViewBag.Categories = new SelectList(distinctCategories, "Id", "Name", selectedCategoryId); // Gửi danh mục vào view
		}
	}
}
