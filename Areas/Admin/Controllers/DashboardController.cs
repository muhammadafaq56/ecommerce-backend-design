using ECommerceApp.Areas.Admin.Models.ViewModels;
using ECommerceApp.Data;
using ECommerceApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DashboardController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalProducts = await _db.Products.CountAsync(),

                TotalCategories = await _db.Categories.CountAsync(),

                TotalOrders = await _db.Orders.CountAsync(),

                TotalUsers = await _db.Users.CountAsync(),

                TotalRevenue = await _db.Orders
                    .Where(o => o.PaymentStatus == PaymentStatus.Paid)
                    .SumAsync(o => (decimal?)o.Total) ?? 0,

                PendingOrders = await _db.Orders
                    .CountAsync(o => o.OrderStatus == OrderStatus.Pending),

                RecentOrders = await _db.Orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .Select(o => new RecentOrderItem
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.CustomerName,
                        CustomerEmail = o.CustomerEmail,
                        Total = o.Total,
                        OrderStatus = o.OrderStatus.ToString(),
                        CreatedAt = o.CreatedAt
                    })
                    .ToListAsync()
            };

            ViewData["Title"] = "Dashboard";

            return View(vm);
        }
    }
}