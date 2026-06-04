using ECommerceApp.Areas.Admin.Models.ViewModels;
using ECommerceApp.Data;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,ProductManager")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly Supabase.Client _supabase;

        public ProductController(
            ApplicationDbContext db,
            Supabase.Client supabase)
        {
            _db = db;
            _supabase = supabase;
        }

        // =========================================================
        // INDEX
        // =========================================================

        public async Task<IActionResult> Index(string? search)
        {
            ViewData["Title"] = "Products";
            ViewData["Search"] = search;

            var query = _db.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Brand != null &&
                     p.Brand.Contains(search)));
            }

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(products);
        }

        // =========================================================
        // CREATE
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewData["Title"] = "Add Product";
            return View(await BuildViewModel(new ProductViewModel()));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(await BuildViewModel(vm));
            }

            var product = new Product
            {
                Name = vm.Name,
                Description = vm.Description,
                Price = vm.Price,
                OriginalPrice = vm.OriginalPrice,
                StockQuantity = vm.StockQuantity,
                Brand = vm.Brand,
                IsActive = vm.IsActive,
                IsFeatured = vm.IsFeatured,
                CategoryId = vm.CategoryId,
                CreatedAt = DateTime.UtcNow,
                ImageUrl = await SaveImage(vm.ImageFile)
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Product \"{product.Name}\" created.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Product";

            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            var vm = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                OriginalPrice = product.OriginalPrice,
                StockQuantity = product.StockQuantity,
                Brand = product.Brand,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                CategoryId = product.CategoryId,
                ImageUrl = product.ImageUrl
            };

            return View(await BuildViewModel(vm));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(await BuildViewModel(vm));
            }

            var product = await _db.Products.FindAsync(vm.Id);
            if (product == null) return NotFound();

            product.Name = vm.Name;
            product.Description = vm.Description;
            product.Price = vm.Price;
            product.OriginalPrice = vm.OriginalPrice;
            product.StockQuantity = vm.StockQuantity;
            product.Brand = vm.Brand;
            product.IsActive = vm.IsActive;
            product.IsFeatured = vm.IsFeatured;
            product.CategoryId = vm.CategoryId;

            if (vm.ImageFile != null)
            {
                // Delete old image from Supabase if exists
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    await DeleteImage(product.ImageUrl);
                }
                product.ImageUrl = await SaveImage(vm.ImageFile);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = $"Product \"{product.Name}\" updated.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products.FindAsync(id);

            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            // Delete image from Supabase if exists
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                await DeleteImage(product.ImageUrl);
            }

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Product \"{product.Name}\" deleted.";
            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private async Task<ProductViewModel> BuildViewModel(ProductViewModel vm)
        {
            vm.Categories = await _db.Categories
                .Select(c => new CategoryOption
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();

            return vm;
        }

        private async Task<string?> SaveImage(IFormFile? file)
        {
            if (file == null) return null;

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            await _supabase.Storage
                .From("categories")
                .Upload(bytes, $"products/{fileName}",
                    new Supabase.Storage.FileOptions
                    {
                        ContentType = file.ContentType,
                        Upsert = true
                    });

            return _supabase.Storage
                .From("categories")
                .GetPublicUrl($"products/{fileName}");
        }

        private async Task DeleteImage(string imageUrl)
        {
            try
            {
                // Extract file path from public URL
                // URL format: https://xxx.supabase.co/storage/v1/object/public/categories/products/filename.jpg
                var uri = new Uri(imageUrl);
                var segments = uri.AbsolutePath.Split('/');

                // Find index after bucket name "categories"
                var bucketIndex = Array.IndexOf(segments, "categories");
                if (bucketIndex >= 0 && bucketIndex < segments.Length - 1)
                {
                    var filePath = string.Join("/",
                        segments[(bucketIndex + 1)..]);

                    await _supabase.Storage
                        .From("categories")
                        .Remove(new List<string> { filePath });
                }
            }
            catch
            {
                // Don't crash if delete fails, just continue
            }
        }
    }
}