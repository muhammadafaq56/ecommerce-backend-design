using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(CheckoutViewModel model, List<CartItem> cartItems, string? userId)
        {
            var order = new Order
            {
                UserId = userId,
                CustomerEmail = model.Email,
                CustomerName = $"{model.FirstName} {model.LastName}",
                ShippingAddress = model.Address,
                ShippingCity = model.City,
                ShippingCountry = model.Country,
                ShippingPostalCode = model.PostalCode,
                SubTotal = model.SubTotal,
                ShippingCost = model.ShippingCost,
                Tax = model.Tax,
                Total = model.Total,
                OrderStatus = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending
            };

            foreach (var item in cartItems)
            {
                order.Items.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product!.Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
                // Stock already decremented at Add-to-Cart time — do NOT touch it here
            }

            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            return await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.OrderStatus = status;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdatePaymentStatusAsync(int orderId, PaymentStatus status, string? stripeIntentId = null)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.PaymentStatus = status;
                if (stripeIntentId != null)
                    order.StripePaymentIntentId = stripeIntentId;
                if (status == PaymentStatus.Paid)
                    order.OrderStatus = OrderStatus.Processing;
                await _context.SaveChangesAsync();
            }
        }
    }
}