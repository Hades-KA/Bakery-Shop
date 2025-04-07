using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhamTaManhLan_8888.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PhamTaManhLan_8888.Extensions;
using System;

// Controller xử lý các hành động liên quan đến Giỏ hàng
public class ShoppingCartController : Controller
{
	private readonly IProductRepository _productRepository;
	private readonly ApplicationDbContext _context;
	private readonly UserManager<ApplicationUser> _userManager;

	// Constructor để inject các dependency cần thiết
	public ShoppingCartController(ApplicationDbContext context,
		UserManager<ApplicationUser> userManager, IProductRepository productRepository)
	{
		_productRepository = productRepository;
		_context = context;
		_userManager = userManager;
	}

	// Action method hiển thị trang Giỏ hàng
	public IActionResult Index()
	{
		// Lấy giỏ hàng từ Session hoặc tạo mới nếu chưa có
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

		// Tính tổng số lượng sản phẩm trong giỏ hàng và truyền vào ViewBag
		ViewBag.CartCount = cart.Items.Sum(i => i.Quantity);

		// Kiểm tra xem người dùng có vai trò là Customer hay không và truyền vào ViewBag
		ViewBag.IsCustomer = User.IsInRole("Customer");

		// Trả về View Index cùng với model giỏ hàng
		return View(cart);
	}

	// Action method xử lý việc thêm sản phẩm vào giỏ hàng
	public async Task<IActionResult> AddToCart(int productId, int quantity)
	{
		// Lấy thông tin sản phẩm từ repository dựa trên ID
		var product = await _productRepository.GetByIdAsync(productId);
		if (product == null) return NotFound(); // Trả về NotFound nếu không tìm thấy sản phẩm

		// Lấy giỏ hàng từ Session hoặc tạo mới nếu chưa có
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

		// Thêm sản phẩm vào giỏ hàng
		cart.AddItem(new CartItem
		{
			ProductId = productId,
			Name = product.Name,
			Price = product.Price,
			Quantity = quantity,
			Url = product.Images != null && product.Images.Any()
					 ? product.Images.First().Url // Lấy URL của ảnh đầu tiên nếu có
					   : product.ImageUrl // Sử dụng ImageUrl nếu không có ảnh trong Images
		});

		// Lưu giỏ hàng đã cập nhật vào Session
		HttpContext.Session.SetObjectAsJson("Cart", cart);

		// Gửi thông báo thành công qua TempData
		TempData["Message"] = "Product added to cart successfully!";

		// Chuyển hướng về trang chủ
		return RedirectToAction("Index", "Home");
	}


	// Action method xử lý việc xóa sản phẩm khỏi giỏ hàng
	public IActionResult RemoveFromCart(int productId)
	{
		// Lấy giỏ hàng từ Session
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
		if (cart != null)
		{
			// Xóa sản phẩm khỏi giỏ hàng
			cart.RemoveItem(productId);

			// Lưu giỏ hàng đã cập nhật vào Session
			HttpContext.Session.SetObjectAsJson("Cart", cart);
		}
		// Chuyển hướng về trang giỏ hàng
		return RedirectToAction("Index");
	}

	// Action method hiển thị trang Thanh toán (yêu cầu người dùng đã đăng nhập)
	[Authorize]
	public IActionResult Checkout()
	{
		// Kiểm tra xem người dùng có vai trò là Customer hay không
		if (!User.IsInRole("Customer"))
		{
			return RedirectToAction("AccessDenied", "Home"); // Chuyển hướng nếu không phải Customer
		}

		// Lấy giỏ hàng từ Session
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
		// Kiểm tra xem giỏ hàng có trống hay không
		if (cart == null || !cart.Items.Any())
		{
			TempData["Error"] = "Your cart is empty. Please add items before checkout.";
			return RedirectToAction("Index"); // Chuyển hướng nếu giỏ hàng trống
		}
		// Trả về View Checkout cùng với model Order mới
		return View(new Order());
	}

	// Action method xử lý việc gửi đơn hàng (yêu cầu người dùng đã đăng nhập)
	[HttpPost]
	[Authorize]
	public async Task<IActionResult> Checkout(Order order)
	{
		// Kiểm tra lại vai trò của người dùng
		if (!User.IsInRole("Customer"))
		{
			return RedirectToAction("AccessDenied", "Home");
		}

		// Lấy giỏ hàng từ Session
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
		// Kiểm tra lại giỏ hàng có trống hay không
		if (cart == null || !cart.Items.Any())
		{
			TempData["Error"] = "Your cart is empty. Please add items before checkout.";
			return RedirectToAction("Index");
		}

		// Lấy thông tin người dùng hiện tại
		var user = await _userManager.GetUserAsync(User);
		if (user == null) return RedirectToAction("AccessDenied", "Home");

		// Gán thông tin người dùng và đơn hàng
		order.UserId = user.Id;
		order.OrderDate = DateTime.UtcNow;
		order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
		order.OrderDetails = cart.Items.Select(i => new OrderDetail
		{
			ProductId = i.ProductId,
			Quantity = i.Quantity,
			Price = i.Price
		}).ToList();

		// Thêm đơn hàng vào database
		_context.Orders.Add(order);
		await _context.SaveChangesAsync();

		// Xóa giỏ hàng khỏi Session sau khi đặt hàng thành công
		HttpContext.Session.Remove("Cart");
		// Chuyển hướng đến trang xác nhận đơn hàng
		return View("OrderCompleted", order.Id);
	}

	// Action method lấy số lượng sản phẩm trong giỏ hàng (sử dụng cho AJAX)
	[HttpGet]
	public IActionResult GetCartCount()
	{
		// Lấy giỏ hàng từ Session hoặc tạo mới nếu chưa có
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
		// Tính tổng số lượng sản phẩm trong giỏ hàng
		int count = cart.Items.Any() ? cart.Items.Sum(i => i.Quantity) : 0;
		// Trả về kết quả dưới dạng JSON với số lượng giỏ hàng
		return Json(new { cartCount = count });
	}

	// Action method xử lý việc xóa toàn bộ giỏ hàng

	
	public IActionResult ClearCart()
	{
		// Xóa giỏ hàng khỏi Session
		HttpContext.Session.Remove("Cart");
		// Gửi thông báo thành công qua TempData
		TempData["Message"] = "Cart cleared successfully!";
		// Chuyển hướng về trang giỏ hàng
		return RedirectToAction("Index");
	}

	// Action method xử lý việc cập nhật số lượng sản phẩm trong giỏ hàng (POST request từ form)
	[HttpPost]
	public IActionResult UpdateCart(int productId, int quantity)
	{
		// Kiểm tra số lượng phải lớn hơn 0
		if (quantity <= 0)
		{
			TempData["Error"] = "Quantity must be at least 1!";
			return RedirectToAction("Index");
		}

		// Lấy giỏ hàng từ Session hoặc tạo mới nếu chưa có
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
		// Cập nhật số lượng của sản phẩm trong giỏ hàng
		cart.UpdateItem(productId, quantity);
		// Lưu giỏ hàng đã cập nhật vào Session
		HttpContext.Session.SetObjectAsJson("Cart", cart);

		// Gửi thông báo thành công qua TempData
		TempData["SuccessMessage"] = "Cart updated successfully!";
		// Chuyển hướng về trang giỏ hàng
		return RedirectToAction("Index");
	}

	// Action method xử lý AJAX request cập nhật số lượng sản phẩm trong giỏ hàng
	[HttpPost]
	public IActionResult UpdateCartItemAjax(int productId, int quantity)
	{
		// Lấy giỏ hàng từ Session
		var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
		if (cart == null)
		{
			return Json(new { success = false, error = "Cart not found." });
		}

		// Tìm sản phẩm trong giỏ hàng
		var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
		if (item == null)
		{
			return Json(new { success = false, error = "Product not found in cart." });
		}

		// Cập nhật số lượng
		if (quantity <= 0)
		{
			return Json(new { success = false, error = "Quantity must be at least 1." });
		}

		item.Quantity = quantity;

		// Cập nhật lại Session
		HttpContext.Session.SetObjectAsJson("Cart", cart);

		// Tính lại tổng tiền sản phẩm và tổng tiền giỏ hàng
		var itemTotal = item.Price * item.Quantity;
		var cartTotal = cart.Items.Sum(i => i.Price * i.Quantity);
		var cartCount = cart.Items.Sum(i => i.Quantity);

		return Json(new
		{
			success = true,
			itemTotal = itemTotal,
			cartTotal = cartTotal,
			cartCount = cartCount
		});
	}

}