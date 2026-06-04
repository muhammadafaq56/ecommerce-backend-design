using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,OrderManager")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public OrderController(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // ─────────────────────────────────────────
        // Index
        // ─────────────────────────────────────────
        public async Task<IActionResult> Index(string? status)
        {
            ViewData["Title"] = "Orders";
            ViewData["Status"] = status;

            var query = _db.Orders.AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var parsed))
                query = query.Where(o => o.OrderStatus == parsed);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        // ─────────────────────────────────────────
        // Detail
        // ─────────────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            ViewData["Title"] = "Order Detail";
            var order = await _db.Orders
                .Include(o => o.User)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();
            return View(order);
        }

        // ─────────────────────────────────────────
        // UpdateStatus
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            if (!Enum.TryParse<OrderStatus>(status, out var newStatus))
            {
                TempData["Error"] = "Invalid status.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            var current = order.OrderStatus;

            // ── Cancelled: fully locked ──
            if (current == OrderStatus.Cancelled)
            {
                TempData["Error"] = "Cancelled orders cannot be modified.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // ── Delivered: only customer return request can change it ──
            if (current == OrderStatus.Delivered)
            {
                TempData["Error"] = "Delivered orders can only be changed via a customer return request.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // ── Allowed transitions ──
            bool allowed = current switch
            {
                OrderStatus.Processing => newStatus == OrderStatus.Shipped
                                                || newStatus == OrderStatus.Cancelled,

                OrderStatus.Shipped => newStatus == OrderStatus.Delivered
                                                || newStatus == OrderStatus.Cancelled,

                OrderStatus.ReturnProcessing => newStatus == OrderStatus.Cancelled,

                _ => false
            };

            if (!allowed)
            {
                TempData["Error"] = $"Cannot transition from {current} to {newStatus}.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // ── Apply transition ──
            order.OrderStatus = newStatus;

            if (newStatus == OrderStatus.Delivered)
                order.DeliveredAt = DateTime.UtcNow;

            // Cancel always = auto refund, no exceptions
            if (newStatus == OrderStatus.Cancelled)
            {
                order.PaymentStatus = PaymentStatus.Refunded;
                order.PreviousStatus = null; // clean up if coming from ReturnProcessing
            }

            await _db.SaveChangesAsync();

            // ── Email notification ──
            if (!string.IsNullOrWhiteSpace(order.User?.Email))
            {
                try
                {
                    if (newStatus == OrderStatus.Cancelled)
                    {
                        // Single email that combines cancellation + refund info
                        await _emailService.SendCancellationWithRefundAsync(
                            order.User.Email,
                            order.User.UserName ?? "Customer",
                            order.OrderNumber,
                            order.Total);
                    }
                    else
                    {
                        // Generic status update (Processing, Shipped, Delivered, etc.)
                        await _emailService.SendOrderStatusUpdateAsync(
                            order.User.Email, order.OrderNumber, newStatus.ToString());
                    }
                }
                catch { /* suppress mail exceptions */ }
            }

            TempData["Success"] = $"Order #{order.OrderNumber} updated to {newStatus}.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // ─────────────────────────────────────────
        // AcceptReturn
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptReturn(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            if (order.OrderStatus != OrderStatus.ReturnProcessing)
            {
                TempData["Error"] = "Order is not in return processing state.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            order.OrderStatus = OrderStatus.Cancelled;
            order.PaymentStatus = PaymentStatus.Refunded;
            order.PreviousStatus = null;

            await _db.SaveChangesAsync();

            // Send acceptance + refund email
            if (!string.IsNullOrWhiteSpace(order.User?.Email))
            {
                try
                {
                    await _emailService.SendReturnAcceptedAsync(
                        order.User.Email,
                        order.User.UserName ?? "Customer",
                        order.OrderNumber,
                        order.Total);
                }
                catch { /* suppress mail exceptions */ }
            }

            TempData["Success"] = $"Return request accepted. Order #{order.OrderNumber} cancelled and refund initiated.";
            return RedirectToAction(nameof(Detail), new { id });
        }

        // ─────────────────────────────────────────
        // RejectReturn
        // ─────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReturn(int id)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }

            if (order.OrderStatus != OrderStatus.ReturnProcessing)
            {
                TempData["Error"] = "Order is not in return processing state.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            // Restore to status before customer filed the request
            var restored = order.PreviousStatus ?? OrderStatus.Delivered;
            order.OrderStatus = restored;
            order.PreviousStatus = null;

            await _db.SaveChangesAsync();

            // Send rejection email
            if (!string.IsNullOrWhiteSpace(order.User?.Email))
            {
                try
                {
                    await _emailService.SendReturnRejectedAsync(
                        order.User.Email,
                        order.User.UserName ?? "Customer",
                        order.OrderNumber);
                }
                catch { /* suppress mail exceptions */ }
            }

            TempData["Success"] = $"Return request rejected. Order #{order.OrderNumber} restored to {restored}.";
            return RedirectToAction(nameof(Detail), new { id });
        }
    }
}