using ECommerceApp.Areas.Admin.Models.ViewModels;
using ECommerceApp.Data;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,CategoryManager")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _env;

        public CategoryController(
            ApplicationDbContext db,
            IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // =========================================================
        // INDEX
        // =========================================================

        // GET: /Admin/Category
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Categories";

            var cats = await _db.Categories
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    ProductCount = c.Products.Count
                })
                .ToListAsync();

            return View(cats);
        }

        // =========================================================
        // CREATE
        // =========================================================

        // GET: /Admin/Category/Create
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["Title"] = "Add Category";

            return View(new CategoryViewModel());
        }

        // POST: /Admin/Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var cat = new Category
            {
                Name = vm.Name,
                Description = vm.Description,
                ImageUrl = await SaveImage(vm.ImageFile)
            };

            _db.Categories.Add(cat);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Category \"{cat.Name}\" created.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // EDIT
        // =========================================================

        // GET: /Admin/Category/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            ViewData["Title"] = "Edit Category";

            var cat = await _db.Categories.FindAsync(id);

            if (cat == null)
            {
                return NotFound();
            }

            var vm = new CategoryViewModel
            {
                Id = cat.Id,
                Name = cat.Name,
                Description = cat.Description,
                ImageUrl = cat.ImageUrl
            };

            return View(vm);
        }

        // POST: /Admin/Category/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var cat = await _db.Categories.FindAsync(vm.Id);

            if (cat == null)
            {
                return NotFound();
            }

            cat.Name = vm.Name;
            cat.Description = vm.Description;

            // Update image only if new image selected
            if (vm.ImageFile != null)
            {
                cat.ImageUrl = await SaveImage(vm.ImageFile);
            }

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Category \"{cat.Name}\" updated.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // DELETE
        // =========================================================

        // POST: /Admin/Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _db.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cat == null)
            {
                return NotFound();
            }

            // Prevent delete if category has products
            if (cat.Products.Any())
            {
                TempData["Error"] =
                    "Cannot delete category because it contains products.";

                return RedirectToAction(nameof(Index));
            }

            _db.Categories.Remove(cat);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                $"Category \"{cat.Name}\" deleted.";

            return RedirectToAction(nameof(Index));
        }

        // =========================================================
        // IMAGE HELPER
        // =========================================================

        private async Task<string?> SaveImage(IFormFile? file)
        {
            if (file == null)
            {
                return null;
            }

            var folder = Path.Combine(
                _env.WebRootPath,
                "images",
                "categories");

            Directory.CreateDirectory(folder);

            var fileName =
                Guid.NewGuid() +
                Path.GetExtension(file.FileName);

            var path = Path.Combine(folder, fileName);

            await using var stream =
                new FileStream(path, FileMode.Create);

            await file.CopyToAsync(stream);

            return "/images/categories/" + fileName;
        }
    }
}