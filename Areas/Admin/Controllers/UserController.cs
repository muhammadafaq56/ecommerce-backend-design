using ECommerceApp.Data;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // GET: /Admin/User
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Staff Users";
            var allUsers = await _db.Users.OrderBy(u => u.Email).ToListAsync();

            // Only show staff users (not customers)
            var staffUsers = new List<(ApplicationUser User, IList<string> Roles)>();
            foreach (var u in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (!roles.Contains("Customer"))
                    staffUsers.Add((u, roles));
            }

            return View(staffUsers);
        }

        // GET: /Admin/User/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Staff User";
            ViewBag.Roles = new List<string> { "ProductManager", "OrderManager", "CategoryManager", "Admin" };
            return View();
        }

        // POST: /Admin/User/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string firstName, string lastName, string email, string password, string role)
        {
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                ModelState.AddModelError("", "A user with this email already exists.");
                ViewBag.Roles = new List<string> { "ProductManager", "OrderManager", "CategoryManager", "Admin" };
                return View();
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["Success"] = $"Staff user {email} created with role {role}.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.Roles = new List<string> { "ProductManager", "OrderManager", "CategoryManager", "Admin" };
            return View();
        }

        // POST: /Admin/User/Delete
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Prevent deleting main admin
            if (user.Email == "admin@shop.com")
            {
                TempData["Error"] = "Cannot delete the main admin user.";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.DeleteAsync(user);
            TempData["Success"] = "Staff user deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}