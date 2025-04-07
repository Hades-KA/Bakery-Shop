using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace PhamTaManhLan_8888.Controllers
{
    public class HomeController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public HomeController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index(string category, string searchTerm)
        {
            ViewBag.IsLoggedIn = User.Identity.IsAuthenticated;
            ViewBag.Categories = new SelectList(await _categoryRepository.GetAllAsync(), "Name", "Name");
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentCategory = category;

            var products = await _productRepository.GetAllAsync();

            if (!string.IsNullOrEmpty(category) && category != "all")
            {
                products = products.Where(p => p.Category?.Name == category).ToList();
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var lowerSearchTerm = searchTerm.ToLower().Trim();
                products = products.Where(p => p.Name.ToLower().Contains(lowerSearchTerm) ||
                                               (p.Category != null && p.Category.Name.ToLower().Contains(lowerSearchTerm))).ToList();

                if (!products.Any())
                {
                    ViewBag.ErrorMessage = $"Không tìm thấy sản phẩm nào với từ khóa \"{searchTerm}\".";
                }
            }

            products = products.Where(p => !string.IsNullOrEmpty(p.ImageUrl)).ToList();

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}