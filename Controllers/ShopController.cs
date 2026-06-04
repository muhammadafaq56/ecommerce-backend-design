using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.Models.ViewModels;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApp.Controllers
{
    public class ShopController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ShopController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // =========================================================
        // INDEX - SHOP MAIN PAGE WITH FILTERS & PAGINATION
        // =========================================================
        public async Task<IActionResult> Index(int? categoryId, string? searchQuery, string? sortBy, int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(searchQuery))
                query = query.Where(p => p.Name.Contains(searchQuery) || p.Description!.Contains(searchQuery));

            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "rating" => query.OrderByDescending(p => p.Rating),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };

            int pageSize = 12;
            int totalItems = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new ShopViewModel
            {
                Products = products,
                Categories = await _context.Categories.ToListAsync(),
                CategoryId = categoryId,
                SearchQuery = searchQuery,
                SortBy = sortBy,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(vm);
        }

        // =========================================================
        // DETAIL - PRODUCT DETAILS DISPLAY WITH REVIEWS
        // =========================================================
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null) return NotFound();

            var related = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id && p.IsActive)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = related;
            return View(product);
        }

        // =========================================================
        // REVIEWS - DEDICATED FULL VIEW SEPARATION PAGE
        // =========================================================
        public async Task<IActionResult> Reviews(int id)
        {
            var product = await _context.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null) return NotFound();

            return View(product);
        }

        // =========================================================
        // SUBMIT REVIEW - LOGGED IN CONTEXT PERSISTENCE POST ROUTE
        // =========================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string reviewText)
        {
            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(reviewText))
            {
                TempData["Error"] = "Please select a valid star rating and type a message.";
                return RedirectToAction(nameof(Detail), new { id = productId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Anonymous";

            // Fetch user email for thank-you mail
            var userProfile = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userProfile != null && !string.IsNullOrEmpty(userProfile.UserName))
                userName = userProfile.UserName;

            var product = await _context.Products.FindAsync(productId);

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                CustomerName = userName,
                Rating = rating,
                ReviewText = reviewText.Trim()
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Send review thank-you email
            if (!string.IsNullOrWhiteSpace(userProfile?.Email))
            {
                try
                {
                    await _emailService.SendReviewThankYouAsync(
                        userProfile.Email,
                        userName,
                        product?.Name ?? "the product");
                }
                catch { /* suppress mail exceptions */ }
            }

            TempData["Success"] = "Your review has been submitted successfully!";
            return RedirectToAction(nameof(Detail), new { id = productId });
        }
    }
}