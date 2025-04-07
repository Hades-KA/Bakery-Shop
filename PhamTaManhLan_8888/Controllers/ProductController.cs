using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;
using System.Linq;
using System.Collections.Generic; // Thêm namespace này để sử dụng IEnumerable<Category>

namespace PhamTaManhLan_8888.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Constructor khởi tạo các repository và môi trường web
        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository, IWebHostEnvironment webHostEnvironment)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _webHostEnvironment = webHostEnvironment;
        }
		// Action method để hiển thị danh sách sản phẩm
		[AllowAnonymous] // Cho phép truy cập mà không cần đăng nhập
		public async Task<IActionResult> Index(string searchTerm = "")
		{
			// Lấy danh sách sản phẩm dựa trên từ khóa tìm kiếm
			var products = await _productRepository.GetAllAsync(); // Lấy tất cả sản phẩm ban đầu

			if (!string.IsNullOrEmpty(searchTerm))
			{
				if (int.TryParse(searchTerm, out int categoryId))
				{
					// Tìm kiếm theo ID danh mục
					products = products.Where(p => p.Category != null && p.Category.Id == categoryId).ToList();
				}
				else
				{
					// Tìm kiếm theo tên danh mục hoặc tên sản phẩm
					var categories = await _categoryRepository.GetAllAsync(); // Chỉ khai báo biến categories ở đây
					if (categories.Any(c => c.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)))
					{
						// Tìm kiếm theo tên danh mục
						products = products.Where(p => p.Category != null && p.Category.Name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
					}
					else
					{
						// Tìm kiếm theo tên sản phẩm
						products = products.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
					}
				}
			}

			ViewBag.SearchTerm = searchTerm; // Gửi từ khóa tìm kiếm về view để hiển thị lại
			var categories2 = await _categoryRepository.GetAllAsync(); // Khai báo biến categories2 để tránh trùng lặp
			ViewBag.Categories = new SelectList(categories2, "Id", "Name"); // Tạo SelectList từ danh sách Category

			return View(products);
		}


		// Action method để hiển thị form thêm sản phẩm
		[Authorize(Roles = "Admin")] // Chỉ cho phép admin truy cập
        public async Task<IActionResult> Add()
        {
            await LoadCategoriesAsync(); // Load danh mục để hiển thị trong dropdown list
            return View(); // Trả về view để thêm sản phẩm
        }

        // Action method để xử lý việc thêm sản phẩm
        [HttpPost]
        [Authorize(Roles = "Admin")] // Chỉ cho phép admin truy cập
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

        // Action method để hiển thị form chỉnh sửa sản phẩm
        [Authorize(Roles = "Admin,Employee")] // Cho phép admin và employee truy cập
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm theo ID
            if (product == null) return NotFound(); // Nếu không tìm thấy, trả về lỗi 404

            await LoadCategoriesAsync(product.CategoryId); // Load danh mục và đánh dấu danh mục hiện tại
            return View(product); // Trả về view chỉnh sửa sản phẩm
        }

        // Action method để xử lý việc chỉnh sửa sản phẩm
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")] // Cho phép admin và employee truy cập
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

        // Action method để hiển thị view xác nhận xóa sản phẩm
        [Authorize(Roles = "Admin")] // Chỉ cho phép admin truy cập
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm theo ID
            if (product == null) return NotFound(); // Nếu không tồn tại, trả về lỗi 404
            return View(product); // Hiển thị view xác nhận xóa sản phẩm
        }

        // Action method để xử lý việc xóa sản phẩm
        [HttpPost]
        [Authorize(Roles = "Admin")] // Chỉ cho phép admin truy cập
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm cần xóa
            if (product == null) return NotFound(); // Nếu không tồn tại, trả về lỗi 404

            DeleteImage(product.ImageUrl); // Xóa ảnh của sản phẩm
            await _productRepository.DeleteAsync(id); // Xóa sản phẩm khỏi database
            return RedirectToAction(nameof(Index)); // Chuyển về danh sách sản phẩm
        }

        // Action method để hiển thị chi tiết sản phẩm
        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id); // Lấy sản phẩm theo ID
            if (product == null) return NotFound(); // Nếu không tồn tại, trả về lỗi 404
            return View(product); // Trả về view hiển thị chi tiết sản phẩm
        }

        // Phương thức để lưu ảnh sản phẩm
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

        // Phương thức để xóa ảnh sản phẩm
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

        // Phương thức để load danh mục và tạo SelectList cho dropdown list
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