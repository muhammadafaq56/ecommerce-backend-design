using ECommerceApp.Models.ViewModels;
using ECommerceApp.Services;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using System.Security.Claims;

namespace ECommerceApp.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public CheckoutController(
            ICartService cartService,
            IOrderService orderService,
            IConfiguration config,
            IEmailService emailService)
        {
            _cartService = cartService;
            _orderService = orderService;
            _config = config;
            _emailService = emailService;
        }

        private string GetCartId()
        {
            if (!Request.Cookies.ContainsKey("CartId"))
            {
                var cartId = Guid.NewGuid().ToString();
                Response.Cookies.Append("CartId", cartId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    IsEssential = true
                });
                return cartId;
            }
            return Request.Cookies["CartId"]!;
        }

        public async Task<IActionResult> Index()
        {
            var cartItems = await _cartService.GetCartItemsAsync(GetCartId());
            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            decimal subTotal = cartItems.Sum(i => i.Product!.Price * i.Quantity);
            decimal shippingCost = subTotal >= 100 ? 0 : 9.99m;
            decimal tax = Math.Round(subTotal * 0.08m, 2);
            decimal total = subTotal + shippingCost + tax;

            // Create Stripe PaymentIntent
            var service = new PaymentIntentService();
            var intent = await service.CreateAsync(new PaymentIntentCreateOptions
            {
                Amount = (long)(total * 100),
                Currency = "usd",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            });

            var vm = new CheckoutViewModel
            {
                CartItems = cartItems,
                SubTotal = subTotal,
                ShippingCost = shippingCost,
                Tax = tax,
                Total = total,
                ClientSecret = intent.ClientSecret,
                StripePublishableKey = _config["Stripe:PublishableKey"]
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
                return View("Index", model);

            var cartItems = await _cartService.GetCartItemsAsync(GetCartId());
            if (!cartItems.Any())
                return RedirectToAction("Index", "Cart");

            // Verify payment succeeded with Stripe
            var service = new PaymentIntentService();
            var intent = await service.GetAsync(model.PaymentIntentId);

            if (intent.Status != "succeeded")
            {
                ModelState.AddModelError("", "Payment was not successful. Please try again.");
                return View("Index", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Create order
            var order = await _orderService.CreateOrderAsync(model, cartItems, userId);

            // Update payment status to Paid
            await _orderService.UpdatePaymentStatusAsync(
                order.Id,
                PaymentStatus.Paid,
                model.PaymentIntentId
            );

            await _cartService.ClearCartAsync(GetCartId());

            // Send order confirmation email
            try
            {
                await _emailService.SendOrderConfirmationAsync(
                    toEmail: model.Email,
                    orderNumber: order.OrderNumber,
                    total: order.Total,
                    customerName: model.FirstName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Order confirmation email failed: {ex.Message}");
            }

            return RedirectToAction("Confirmation", new { orderNumber = order.OrderNumber });
        }

        public async Task<IActionResult> Confirmation(string orderNumber)
        {
            var order = await _orderService.GetOrderByNumberAsync(orderNumber);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}