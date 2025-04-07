using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;
using System.Linq;
using System.Collections.Generic;

namespace PhamTaManhLan_8888.Controllers
{
	public class ProductController : Controller
	{
		private readonly IProductRepository _productRepository;
		private readonly ICategoryRepository _categoryRepository;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository, IWebHostEnvironment webHostEnvironment)
		{
			_productRepository = productRepository;
			_categoryRepository = categoryRepository;
			_webHostEnvironment = webHostEnvironment;
		}

		[AllowAnonymous]
		public async Task<IActionResult> Index(string category, string searchTerm)
		{
			var products = _productRepository.GetAllAsync().Result.AsQueryable();

			if (!string.IsNullOrEmpty(category))
			{
				products = products.Where(p => p.Category != null && p.Category.Name == category);
			}

			if (!string.IsNullOrEmpty(searchTerm))
			{
				products = products.Where(p => p.Name.Contains(searchTerm));
			}

			if (!products.Any())
			{
				ViewBag.ErrorMessage = $"Sản phẩm không tồn tại của từ gõ \"{searchTerm}\"";
			}

			ViewBag.SearchTerm = searchTerm;
			ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name");

			return View(products.ToList());
		}

		[Authorize(Roles = "Admin,Employee")]
		public async Task<IActionResult> Add()
		{
			await LoadCategoriesAsync();
			return View();
		}

		[HttpPost]
		[Authorize(Roles = "Admin,Employee")]
		public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
		{
			ModelState.Remove("ImageUrl");

			if (!ModelState.IsValid)
			{
				await LoadCategoriesAsync();
				return View(product);
			}

			if (imageUrl != null)
			{
				var imagePath = await SaveImageAsync(imageUrl);
				if (imagePath == null)
				{
					ModelState.AddModelError("", "Lỗi khi tải ảnh lên.");
					await LoadCategoriesAsync();
					return View(product);
				}
				product.ImageUrl = imagePath;
			}

			await _productRepository.AddAsync(product);
			return RedirectToAction(nameof(Index));
		}

		[Authorize(Roles = "Admin,Employee")]
		public async Task<IActionResult> Update(int id)
		{
			var product = await _productRepository.GetByIdAsync(id);
			if (product == null) return NotFound();

			await LoadCategoriesAsync(product.CategoryId);
			return View(product);
		}

		[HttpPost]
		[Authorize(Roles = "Admin,Employee")]
		public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
		{
			ModelState.Remove("ImageUrl");

			if (id != product.Id) return NotFound();

			if (!ModelState.IsValid)
			{
				await LoadCategoriesAsync(product.CategoryId);
				return View(product);
			}

			var existingProduct = await _productRepository.GetByIdAsync(id);
			if (existingProduct == null) return NotFound();

			if (imageUrl != null)
			{
				var imagePath = await SaveImageAsync(imageUrl);
				if (imagePath == null)
				{
					ModelState.AddModelError("", "Lỗi khi tải ảnh lên.");
					await LoadCategoriesAsync(product.CategoryId);
					return View(product);
				}
				DeleteImage(existingProduct.ImageUrl);
				product.ImageUrl = imagePath;
			}
			else
			{
				product.ImageUrl = existingProduct.ImageUrl;
			}

			existingProduct.Name = product.Name;
			existingProduct.Price = product.Price;
			existingProduct.Description = product.Description;
			existingProduct.CategoryId = product.CategoryId;
			existingProduct.ImageUrl = product.ImageUrl;

			await _productRepository.UpdateAsync(existingProduct);
			return RedirectToAction(nameof(Index));
		}

		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> Delete(int id)
		{
			var product = await _productRepository.GetByIdAsync(id);
			if (product == null) return NotFound();
			return View(product);
		}

		[HttpPost]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var product = await _productRepository.GetByIdAsync(id);
			if (product == null) return NotFound();

			DeleteImage(product.ImageUrl);
			await _productRepository.DeleteAsync(id);
			return RedirectToAction(nameof(Index));
		}

		public async Task<IActionResult> Display(int id)
		{
			var product = await _productRepository.GetByIdAsync(id);
			if (product == null) return NotFound();
			return View(product);
		}

		private async Task<string?> SaveImageAsync(IFormFile image)
		{
			try
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
			catch
			{
				return null;
			}
		}

		private void DeleteImage(string? imageUrl)
		{
			if (!string.IsNullOrEmpty(imageUrl))
			{
				var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
				if (System.IO.File.Exists(imagePath))
				{
					System.IO.File.Delete(imagePath);
				}
			}
		}

		private async Task LoadCategoriesAsync(int? selectedCategoryId = null)
		{
			var categories = await _categoryRepository.GetAllAsync();
			var distinctCategories = categories
				.GroupBy(c => c.Id)
				.Select(g => g.First())
				.ToList();

			ViewBag.Categories = new SelectList(distinctCategories, "Id", "Name", selectedCategoryId);
		}
	}
}
