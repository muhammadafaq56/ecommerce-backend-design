using ECommerceApp.Services;
using ECommerceApp.Models;
using ECommerceApp.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApp.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public OrderController(IOrderService orderService, ApplicationDbContext db, IEmailService emailService)
        {
            _orderService = orderService;
            _db = db;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return View(orders);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.UserId != userId) return Forbid();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnRequest(int id, string confirmOrderNumber)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (order.UserId != userId) return Forbid();

            // Verification: Processing, Shipped, or Delivered
            bool isValidState = order.OrderStatus == OrderStatus.Processing ||
                               order.OrderStatus == OrderStatus.Shipped ||
                               order.OrderStatus == OrderStatus.Delivered;

            if (!isValidState)
            {
                TempData["Error"] = "This order is not eligible for cancellation or return requests.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // Expiry gate checks for delivered items (7 days post delivery)
            if (order.OrderStatus == OrderStatus.Delivered)
            {
                var deliveryDate = order.DeliveredAt ?? order.CreatedAt;
                if (DateTime.UtcNow > deliveryDate.AddDays(7))
                {
                    TempData["Error"] = "The return window for this order closed after 7 days of delivery.";
                    return RedirectToAction(nameof(Detail), new { id });
                }
            }

            // Explicit confirmation guard check
            if (string.IsNullOrWhiteSpace(confirmOrderNumber) || confirmOrderNumber.Trim() != order.OrderNumber)
            {
                TempData["Error"] = "Confirmation failed! Entered order number does not match.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // Flag system track processing
            order.PreviousStatus = order.OrderStatus;
            order.OrderStatus = OrderStatus.ReturnProcessing;
            await _db.SaveChangesAsync();

            // Send return/cancel request received email
            if (!string.IsNullOrWhiteSpace(order.User?.Email))
            {
                try
                {
                    await _emailService.SendReturnRequestReceivedAsync(
                        order.User.Email,
                        order.User.UserName ?? "Customer",
                        order.OrderNumber);
                }
                catch { /* suppress mail exceptions */ }
            }

            TempData["Success"] = "Your request has been submitted successfully. Awaiting administration review.";
            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}