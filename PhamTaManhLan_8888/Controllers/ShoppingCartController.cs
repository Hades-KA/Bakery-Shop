using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhamTaManhLan_8888.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PhamTaManhLan_8888.Extensions;

public class ShoppingCartController : Controller
{
	private readonly IProductRepository _productRepository;
	private readonly ApplicationDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;

	public ShoppingCartController(ApplicationDbContext context,
		UserManager<ApplicationUser> userManager, IProductRepository productRepository)
	{
		_productRepository = productRepository;
		_context = context;
		_userManager = userManager;
	}

	public IActionResult Index()
	{
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
		ViewBag.CartCount = cart.Items.Sum(i => i.Quantity);
		ViewBag.IsCustomer = User.IsInRole("Customer"); // Truyền thông tin vai trò sang View
		return View(cart);
	}

	public async Task<IActionResult> AddToCart(int productId, int quantity)
	{
		var product = await _productRepository.GetByIdAsync(productId);
		if (product == null) return NotFound();

		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
		cart.AddItem(new CartItem
		{
			ProductId = productId,
			Name = product.Name,
			Price = product.Price,
			Quantity = quantity,
			Url = product.Images != null && product.Images.Any()
					   ? product.Images.First().Url // Lấy ảnh đầu tiên từ danh sách
					   : product.ImageUrl // Nếu không có ảnh danh sách, lấy ảnh chính
		});

		HttpContext.Session.SetObjectAsJson("Cart", cart);
		TempData["Message"] = "Product added to cart successfully!";
		return RedirectToAction("Index", "Home");
	}

	public IActionResult RemoveFromCart(int productId)
	{
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
		if (cart != null)
		{
			cart.RemoveItem(productId);
			HttpContext.Session.SetObjectAsJson("Cart", cart);
		}
		return RedirectToAction("Index");
	}

	[Authorize]
	public IActionResult Checkout()
	{
		if (!User.IsInRole("Customer"))
		{
			return RedirectToAction("AccessDenied", "Home");
		}

		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
		if (cart == null || !cart.Items.Any())
		{
			TempData["Error"] = "Your cart is empty. Please add items before checkout.";
			return RedirectToAction("Index");
		}
		return View(new Order());
	}

	[HttpPost]
	[Authorize]
	public async Task<IActionResult> Checkout(Order order)
	{
		if (!User.IsInRole("Customer"))
		{
			return RedirectToAction("AccessDenied", "Home");
		}

		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
		if (cart == null || !cart.Items.Any())
		{
			TempData["Error"] = "Your cart is empty. Please add items before checkout.";
			return RedirectToAction("Index");
		}

		var user = await _userManager.GetUserAsync(User);
		if (user == null) return RedirectToAction("AccessDenied", "Home");

		order.UserId = user.Id;
		order.OrderDate = DateTime.UtcNow;
		order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
		order.OrderDetails = cart.Items.Select(i => new OrderDetail
		{
			ProductId = i.ProductId,
			Quantity = i.Quantity,
			Price = i.Price
		}).ToList();

		_context.Orders.Add(order);
		await _context.SaveChangesAsync();

		HttpContext.Session.Remove("Cart");
		return View("OrderCompleted", order.Id);
	}

	[HttpGet]
	public IActionResult GetCartCount()
	{
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
		int count = cart.Items.Any() ? cart.Items.Sum(i => i.Quantity) : 0;
		return Json(new { cartCount = count });
	}

	public IActionResult ClearCart()
	{
		HttpContext.Session.Remove("Cart");
		TempData["Message"] = "Cart cleared successfully!";
		return RedirectToAction("Index");
	}

	[HttpPost]
	public IActionResult UpdateCart(int productId, int quantity)
	{
		if (quantity <= 0)
		{
			TempData["Error"] = "Quantity must be at least 1!";
			return RedirectToAction("Index");
		}

		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
		cart.UpdateItem(productId, quantity);
		HttpContext.Session.SetObjectAsJson("Cart", cart);

		TempData["SuccessMessage"] = "Cart updated successfully!";
		return RedirectToAction("Index");
	}
}