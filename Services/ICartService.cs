using ECommerceApp.Models;

namespace ECommerceApp.Services
{
    public interface ICartService
    {
        Task<List<CartItem>> GetCartItemsAsync(string sessionId);
        Task<bool> AddToCartAsync(string sessionId, int productId, int quantity);
        Task UpdateQuantityAsync(string sessionId, int cartItemId, int quantity);
        Task RemoveFromCartAsync(string sessionId, int cartItemId);
        Task ClearCartAsync(string sessionId);
        Task<int> GetCartCountAsync(string sessionId);
        Task<decimal> GetCartTotalAsync(string sessionId);

        // ── stock-enforcement helpers (used by CartController) ────────────────
        Task<int> GetProductQuantityInCartAsync(string sessionId, int productId);
        Task<int> GetProductStockAsync(int productId);

        // Lookup by cartItemId
        Task<CartItem?> GetCartItemAsync(string sessionId, int cartItemId);

        // Lookup by productId — used after Add to find the item just created/merged
        Task<CartItem?> GetCartItemByProductAsync(string sessionId, int productId);
    }
}