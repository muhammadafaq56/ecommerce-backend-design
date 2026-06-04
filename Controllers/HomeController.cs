using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public HomeController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitContact(
        [FromForm] string name,
        [FromForm] string email,
        [FromForm] string subject,
        [FromForm] string message)
        {
            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(subject) ||
                string.IsNullOrWhiteSpace(message))
            {
                return BadRequest("All fields are required.");
            }

            var contact = new ContactMessage
            {
                Name = name.Trim(),
                Email = email.Trim(),
                Subject = subject.Trim(),
                Message = message.Trim(),
                SentAt = DateTime.UtcNow
            };

            _context.ContactMessages.Add(contact);
            await _context.SaveChangesAsync();

            // Send confirmation email to the customer
            try
            {
                await _emailService.SendContactConfirmationAsync(
                    email.Trim(),
                    name.Trim(),
                    subject.Trim());
            }
            catch { /* suppress mail exceptions */ }

            return Ok(new { success = true });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult Cookie()
        {
            return View();
        }

        public async Task<IActionResult> Index()
        {
            // ── Role-based redirects ───────────────────────────────────
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });

                if (User.IsInRole("ProductManager"))
                    return RedirectToAction("Index", "Product", new { area = "Admin" });

                if (User.IsInRole("CategoryManager"))
                    return RedirectToAction("Index", "Category", new { area = "Admin" });

                if (User.IsInRole("OrderManager"))
                    return RedirectToAction("Index", "Order", new { area = "Admin" });
            }

            // ── Featured Products ──────────────────────────────────────
            var featuredProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => p.IsFeatured && p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            // ── Categories (with product count) ───────────────────────
            var categories = await _context.Categories
                .Include(c => c.Products.Where(p => p.IsActive))
                .Where(c => c.Products.Any(p => p.IsActive))
                .OrderBy(c => c.Name)
                .ToListAsync();

            // ── Best Sellers (most ordered products) ──────────────────
            var bestSellerIds = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new { ProductId = g.Key, TotalSold = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.TotalSold)
                .Take(4)
                .Select(x => x.ProductId)
                .ToListAsync();

            List<Product> bestSellers;
            if (bestSellerIds.Any())
            {
                bestSellers = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Reviews)
                    .Where(p => bestSellerIds.Contains(p.Id) && p.IsActive)
                    .ToListAsync();

                // Preserve the sales-rank order
                bestSellers = bestSellerIds
                    .Select(id => bestSellers.FirstOrDefault(p => p.Id == id))
                    .Where(p => p != null)
                    .ToList()!;
            }
            else
            {
                // Fallback: show 4 newest active products
                bestSellers = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Reviews)
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(4)
                    .ToListAsync();
            }

            // ── New Arrivals (most recently added active products) ─────
            var newArrivals = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();

            // ── Pass to view ───────────────────────────────────────────
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.Categories = categories;
            ViewBag.BestSellers = bestSellers;
            ViewBag.NewArrivals = newArrivals;

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}