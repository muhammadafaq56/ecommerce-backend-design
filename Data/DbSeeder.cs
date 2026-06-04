using ECommerceApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.MigrateAsync();

            // ── Roles ──────────────────────────────────────
            string[] roles = { "Admin", "Customer", "ProductManager", "OrderManager", "CategoryManager" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // ── Admin user ─────────────────────────────────
            if (await userManager.FindByEmailAsync("admin@shop.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@shop.com",
                    Email = "admin@shop.com",
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            // ── Categories ─────────────────────────────────
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new() { Name = "Electronics",   Description = "Electronic devices and gadgets" },
                    new() { Name = "Clothing",      Description = "Men and women clothing" },
                    new() { Name = "Books",         Description = "Books and literature" },
                    new() { Name = "Home & Garden", Description = "Home decor and garden items" },
                    new() { Name = "Sports",        Description = "Sports and fitness equipment" }
                };
                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // ── Products ───────────────────────────────────
            if (!await context.Products.AnyAsync())
            {
                var electronics = await context.Categories.FirstAsync(c => c.Name == "Electronics");
                var clothing = await context.Categories.FirstAsync(c => c.Name == "Clothing");
                var books = await context.Categories.FirstAsync(c => c.Name == "Books");
                var home = await context.Categories.FirstAsync(c => c.Name == "Home & Garden");
                var sports = await context.Categories.FirstAsync(c => c.Name == "Sports");

                var products = new List<Product>
                {
                    // ── Electronics ───────────────────────
                    new() {
                        Name = "Wireless Headphones", Price = 79.99m, OriginalPrice = 99.99m,
                        CategoryId = electronics.Id,  StockQuantity = 50,  IsFeatured = true,
                        Brand = "SoundMax", Rating = 4.5, ReviewCount = 120,
                        ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "Bluetooth Speaker", Price = 49.99m, OriginalPrice = 69.99m,
                        CategoryId = electronics.Id, StockQuantity = 30, IsFeatured = true,
                        Brand = "SoundMax", Rating = 4.3, ReviewCount = 85,
                        ImageUrl = "https://images.unsplash.com/photo-1608043152269-423dbba4e7e1?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "Smartwatch", Price = 149.99m, OriginalPrice = 199.99m,
                        CategoryId = electronics.Id, StockQuantity = 20, IsFeatured = true,
                        Brand = "TechWear", Rating = 4.7, ReviewCount = 200,
                        ImageUrl = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "USB-C Hub", Price = 29.99m, OriginalPrice = null,
                        CategoryId = electronics.Id, StockQuantity = 100, IsFeatured = false,
                        Brand = "LinkPro", Rating = 4.2, ReviewCount = 60,
                        ImageUrl = "https://images.unsplash.com/photo-1625842268584-8f3296236761?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "Mechanical Keyboard", Price = 89.99m, OriginalPrice = 119.99m,
                        CategoryId = electronics.Id, StockQuantity = 40, IsFeatured = false,
                        Brand = "KeyMaster", Rating = 4.6, ReviewCount = 150,
                        ImageUrl = "https://images.unsplash.com/photo-1587829741301-dc798b83add3?w=600&auto=format&fit=crop"
                    },

                    // ── Clothing ──────────────────────────
                    new() {
                        Name = "Classic T-Shirt", Price = 19.99m, OriginalPrice = null,
                        CategoryId = clothing.Id, StockQuantity = 200, IsFeatured = false,
                        Brand = "BasicWear", Rating = 4.0, ReviewCount = 300,
                        ImageUrl = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "Slim Fit Jeans", Price = 49.99m, OriginalPrice = 69.99m,
                        CategoryId = clothing.Id, StockQuantity = 80, IsFeatured = true,
                        Brand = "DenimCo", Rating = 4.4, ReviewCount = 175,
                        ImageUrl = "https://images.unsplash.com/photo-1542272454315-4c01d7abdf4a?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "Winter Jacket", Price = 99.99m, OriginalPrice = 139.99m,
                        CategoryId = clothing.Id, StockQuantity = 35, IsFeatured = true,
                        Brand = "WarmUp", Rating = 4.8, ReviewCount = 90,
                        ImageUrl = "https://images.unsplash.com/photo-1591047139829-d91aecb6caea?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "Running Shoes", Price = 69.99m, OriginalPrice = 89.99m,
                        CategoryId = clothing.Id, StockQuantity = 60, IsFeatured = false,
                        Brand = "SpeedRun", Rating = 4.5, ReviewCount = 220,
                        ImageUrl = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=600&auto=format&fit=crop"
                    },

                    // ── Books ─────────────────────────────
                    new() {
                        Name = "C# Programming Guide", Price = 34.99m, OriginalPrice = null,
                        CategoryId = books.Id, StockQuantity = 150, IsFeatured = false,
                        Brand = "TechPress", Rating = 4.9, ReviewCount = 400,
                        ImageUrl = "https://images.unsplash.com/photo-1515879218367-8466d910aaa4?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "Clean Code", Price = 29.99m, OriginalPrice = 39.99m,
                        CategoryId = books.Id, StockQuantity = 100, IsFeatured = true,
                        Brand = "DevBooks", Rating = 4.8, ReviewCount = 520,
                        ImageUrl = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=600&auto=format&fit=crop"
                    },

                    // ── Home & Garden ─────────────────────
                    new() {
                        Name = "Ceramic Mug Set", Price = 24.99m, OriginalPrice = null,
                        CategoryId = home.Id, StockQuantity = 70, IsFeatured = false,
                        Brand = "HomeBliss", Rating = 4.3, ReviewCount = 95,
                        ImageUrl = "https://images.unsplash.com/photo-1514228742587-6b1558fcca3d?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "LED Desk Lamp", Price = 39.99m, OriginalPrice = 54.99m,
                        CategoryId = home.Id, StockQuantity = 45, IsFeatured = true,
                        Brand = "BrightHome", Rating = 4.6, ReviewCount = 130,
                        ImageUrl = "https://images.unsplash.com/photo-1507473885765-e6ed057f782c?w=600&auto=format&fit=crop"
                    },

                    // ── Sports ────────────────────────────
                    new() {
                        Name = "Yoga Mat", Price = 29.99m, OriginalPrice = null,
                        CategoryId = sports.Id, StockQuantity = 90, IsFeatured = false,
                        Brand = "FlexFit", Rating = 4.4, ReviewCount = 180,
                        ImageUrl = "https://images.unsplash.com/photo-1601925228008-0bfd4f9e9b44?w=600&auto=format&fit=crop"
                    },
                    new() {
                        Name = "Dumbbell Set 10kg", Price = 59.99m, OriginalPrice = 79.99m,
                        CategoryId = sports.Id, StockQuantity = 25, IsFeatured = true,
                        Brand = "IronGrip", Rating = 4.7, ReviewCount = 210,
                        ImageUrl = "https://images.unsplash.com/photo-1534438327276-14e5300c3a48?w=600&auto=format&fit=crop"
                    },
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }
        }
    }
}