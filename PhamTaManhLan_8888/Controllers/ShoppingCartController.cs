using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PhamTaManhLan_8888.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PhamTaManhLan_8888.Extensions;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        var cart = GetCartFromSession();
        ViewBag.CartCount = cart.Sum(i => i.Quantity);
        ViewBag.IsCustomer = User.IsInRole("Customer");
        return View(new ShoppingCart { Items = cart });
    }

    [Authorize]
    public async Task<IActionResult> AddToCart(int productId, int quantity)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null) return NotFound();

        var cart = GetCartFromSession();

        cart.Add(new CartItem
        {
            ProductId = productId,
            Name = product.Name,
            Price = product.Price,
            Quantity = quantity,
            Url = product.Images?.FirstOrDefault()?.Url ?? product.ImageUrl
        });

        SaveCartToSession(cart);

        TempData["Message"] = "Product added to cart successfully!";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult RemoveFromCart(int productId)
    {
        var cart = GetCartFromSession();
        cart.RemoveAll(i => i.ProductId == productId);
        SaveCartToSession(cart);
        return RedirectToAction("Index");
    }

    [Authorize]
    public IActionResult Checkout()
    {
        if (!User.IsInRole("Customer"))
            return RedirectToAction("AccessDenied", "Home");

        var cart = GetCartFromSession();
        if (!cart.Any())
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
        if (!ModelState.IsValid)
            return View("Checkout", order);

        var cart = GetCartFromSession();
        if (!cart.Any())
        {
            ModelState.AddModelError("", "Giỏ hàng trống.");
            return View("Checkout", order);
        }

        var user = await _userManager.GetUserAsync(User);

        order.UserId = user.Id;
        order.OrderDate = DateTime.Now;
        order.TotalPrice = cart.Sum(i => i.Price * i.Quantity);
        order.Status = OrderStatus.Pending;
        order.OrderDetails = cart.Select(item => new OrderDetail
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Price = item.Price
        }).ToList();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        HttpContext.Session.Remove("Cart");

        return RedirectToAction("OrderCompleted", new { id = order.Id });
    }

    public IActionResult OrderCompleted(int id)
    {
        return View(model: id);
    }

    [HttpGet]
    public IActionResult GetCartCount()
    {
        var cart = GetCartFromSession();
        int count = cart.Sum(i => i.Quantity);
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

        var cart = GetCartFromSession();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            item.Quantity = quantity;
        }

        SaveCartToSession(cart);
        TempData["SuccessMessage"] = "Cart updated successfully!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult UpdateCartItemAjax(int productId, int quantity)
    {
        var cart = GetCartFromSession();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            return Json(new { success = false, error = "Product not found in cart." });
        }

        if (quantity <= 0)
        {
            return Json(new { success = false, error = "Quantity must be at least 1." });
        }

        item.Quantity = quantity;
        SaveCartToSession(cart);

        return Json(new
        {
            success = true,
            itemTotal = item.Price * item.Quantity,
            cartTotal = cart.Sum(i => i.Price * i.Quantity),
            cartCount = cart.Sum(i => i.Quantity)
        });
    }

    [Authorize]
    public async Task<IActionResult> OrderHistory(OrderStatus? status)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return RedirectToAction("AccessDenied", "Home");

        var query = _context.Orders.Where(o => o.UserId == user.Id);
        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var orders = query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new Order
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                Status = o.Status,
                OrderDetails = o.OrderDetails.Select(od => new OrderDetail
                {
                    ProductId = od.ProductId,
                    Product = od.Product,
                    Quantity = od.Quantity,
                    Price = od.Price
                }).ToList()
            }).ToList();

        ViewBag.Statuses = Enum.GetValues(typeof(OrderStatus))
                .Cast<OrderStatus>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.GetDisplayName()
                }).ToList();

        ViewBag.SelectedStatus = status;

        return View(orders);
    }

    // Helper methods
    private List<CartItem> GetCartFromSession()
    {
        return HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart")?.Items ?? new List<CartItem>();
    }

    private void SaveCartToSession(List<CartItem> items)
    {
        var cart = new ShoppingCart { Items = items };
        HttpContext.Session.SetObjectAsJson("Cart", cart);
    }

    [HttpPost]
    public IActionResult UpdateQuantity(int productId, int delta)
    {
        var cart = GetCartFromSession();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item == null)
        {
            return Json(new { success = false, error = "Item not found in cart." });
        }

        item.Quantity = Math.Max(1, item.Quantity + delta);

        decimal total = cart.Sum(i => i.Price * i.Quantity);
        decimal subtotal = item.Price * item.Quantity;

        SaveCartToSession(cart);

        return Json(new
        {
            newQuantity = item.Quantity,
            newSubtotalFormatted = subtotal.ToString("C"),
            newTotalFormatted = total.ToString("C")
        });
    }
}
