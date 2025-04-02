﻿using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PhamTaManhLan_8888.Models;
using PhamTaManhLan_8888.Repositories;

namespace PhamTaManhLan_8888.Controllers
{
	public class HomeController : Controller
	{
		private readonly IProductRepository _productRepository;
		private readonly ICategoryRepository _categoryRepository; // Thêm Category Repository

		public HomeController(IProductRepository productRepository, ICategoryRepository categoryRepository)
		{
			_productRepository = productRepository;
			_categoryRepository = categoryRepository;
		}

		public async Task<IActionResult> Index()
		{
			ViewBag.Categories = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _categoryRepository.GetAllAsync(), "Id", "Name"); // Lấy danh mục sản phẩm
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
