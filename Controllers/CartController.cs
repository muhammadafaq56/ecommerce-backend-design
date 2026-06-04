using E_commerce.Services;
using ECommerceApp.Data;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommerceApp.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ApplicationDbContext _db;
        private readonly CartReservationService _reservation;

        public CartController(
            ICartService cartService,
            ApplicationDbContext db,
            CartReservationService reservation)
        {
            _cartService = cartService;
            _db = db;
            _reservation = reservation;
        }

        private string GetCartId()
        {
            if (Request.Cookies.TryGetValue("CartId", out var existingId) && !string.IsNullOrEmpty(existingId))
            {
                Response.Cookies.Append("CartId", existingId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax
                });
                return existingId;
            }
            var cartId = Guid.NewGuid().ToString();
            Response.Cookies.Append("CartId", cartId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });
            return cartId;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _cartService.GetCartItemsAsync(GetCartId());
            return View(items);
        }

        // POST /Cart/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var product = await _db.Products.FindAsync(productId);

            if (product == null || product.StockQuantity <= 0)
                return Json(new { success = false, message = "Out of stock." });

            if (quantity > product.StockQuantity)
                quantity = product.StockQuantity;

            product.StockQuantity -= quantity;
            await _db.SaveChangesAsync();

            var cartId = GetCartId();
            await _cartService.AddToCartAsync(cartId, productId, quantity);

            var cartItem = await _cartService.GetCartItemByProductAsync(cartId, productId);
            if (cartItem != null)
                _reservation.StartOrReset(cartItem.Id);

            var count = await _cartService.GetCartCountAsync(cartId);
            return Json(new { success = true, count });
        }

        // POST /Cart/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int cartItemId)
        {
            var cartId = GetCartId();

            var cartItem = await _db.CartItems.FindAsync(cartItemId);
            if (cartItem == null)
                return Json(new { success = false, message = "Item not found." });

            // Cancel timer BEFORE removing — prevents double stock restore
            _reservation.Cancel(cartItemId);

            var product = await _db.Products.FindAsync(cartItem.ProductId);
            if (product != null)
            {
                product.StockQuantity += cartItem.Quantity;
                await _db.SaveChangesAsync();
            }

            await _cartService.RemoveFromCartAsync(cartId, cartItemId);

            var count = await _cartService.GetCartCountAsync(cartId);
            return Json(new { success = true, count });
        }

        // POST /Cart/Update
        [HttpPost]
        public async Task<IActionResult> Update(int cartItemId, int quantity)
        {
            if (quantity < 1)
                return Json(new { success = false, message = "Quantity must be at least 1." });

            var cartId = GetCartId();

            var cartItem = await _db.CartItems.FindAsync(cartItemId);
            if (cartItem == null)
                return Json(new { success = false, message = "Item not found." });

            var product = await _db.Products.FindAsync(cartItem.ProductId);
            if (product == null)
                return Json(new { success = false, message = "Product not found." });

            int diff = quantity - cartItem.Quantity;

            if (diff > 0 && diff > product.StockQuantity)
                return Json(new
                {
                    success = false,
                    message = $"Only {product.StockQuantity} more unit(s) available.",
                    maxQty = cartItem.Quantity + product.StockQuantity
                });

            product.StockQuantity -= diff;
            if (product.StockQuantity < 0) product.StockQuantity = 0;
            await _db.SaveChangesAsync();

            await _cartService.UpdateQuantityAsync(cartId, cartItemId, quantity);

            // Reset the 20-minute window on every quantity change
            _reservation.StartOrReset(cartItemId);

            var count = await _cartService.GetCartCountAsync(cartId);
            return Json(new { success = true, count, reload = true });
        }

        // GET /Cart/Count
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var count = await _cartService.GetCartCountAsync(GetCartId());
            return Json(new { count });
        }

        // GET /Cart/Timers
        // Returns remaining seconds for every cart item in this session.
        // If an item has no active timer (e.g. after app restart) a fresh
        // 20-minute timer is started automatically so the badge always shows.
        [HttpGet]
        public async Task<IActionResult> Timers()
        {
            var items = await _cartService.GetCartItemsAsync(GetCartId());
            var dict = new Dictionary<int, int>();

            foreach (var item in items)
            {
                var remaining = _reservation.GetRemainingSeconds(item.Id);

                if (remaining == null)
                {
                    // No timer in memory — auto-start a fresh one
                    _reservation.StartOrReset(item.Id);
                    remaining = (int)CartReservationService.ReservationWindow.TotalSeconds;
                }

                dict[item.Id] = remaining.Value;
            }

            return Json(new { timers = dict });
        }

        // GET /Cart/Checkout
        [HttpGet]
        public IActionResult Checkout()
        {
            if (!User.Identity!.IsAuthenticated)
                return RedirectToAction("Login", "Account", new { returnUrl = "/Checkout/Index" });
            return RedirectToAction("Index", "Checkout");
        }
    }
}