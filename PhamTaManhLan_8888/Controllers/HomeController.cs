using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;

namespace PhamTaManhLan_8888.Controllers
{
	public class HomeController : Controller
	{
		private readonly IProductRepository _productRepository;

		public HomeController(IProductRepository productRepository)
		{
			_productRepository = productRepository;
		}
		public async Task<IActionResult> Index()
		{
			var products = await _productRepository.GetAllAsync();
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
