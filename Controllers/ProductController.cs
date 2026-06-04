using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ECommerceApp.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailService _emailService;

        public ProductController(ApplicationDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        // ================================================
        // POST: Submit a new Review
        // ================================================
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int productId, int rating, string reviewText)
        {
            if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(reviewText))
            {
                TempData["Error"] = "Please provide a valid star rating and review message.";
                return RedirectToAction("Details", new { id = productId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Anonymous";

            var userProfile = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (userProfile != null && !string.IsNullOrEmpty(userProfile.UserName))
                userName = userProfile.UserName;

            var product = await _db.Products.FindAsync(productId);

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                CustomerName = userName,
                Rating = rating,
                ReviewText = reviewText.Trim()
            };

            _db.ProductReviews.Add(review);
            await _db.SaveChangesAsync();

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

            TempData["Success"] = "Thank you! Your product review has been posted successfully.";
            return RedirectToAction("Details", new { id = productId });
        }

        // ================================================
        // GET: View All Reviews for a Product
        // ================================================
        public async Task<IActionResult> Reviews(int id)
        {
            var product = await _db.Products
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            ViewData["Title"] = $"{product.Name} — Customer Reviews";
            return View(product);
        }
    }
}