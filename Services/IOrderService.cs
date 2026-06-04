using ECommerceApp.Models;
using ECommerceApp.Models.ViewModels;

namespace ECommerceApp.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(CheckoutViewModel model, List<CartItem> cartItems, string? userId);
        Task<Order?> GetOrderByIdAsync(int id);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task UpdatePaymentStatusAsync(int orderId, PaymentStatus status, string? stripeIntentId = null);
    }
}