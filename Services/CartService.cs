using ECommerceApp.Data;
using ECommerceApp.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CartItem>> GetCartItemsAsync(string sessionId)
        {
            return await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.SessionId == sessionId)
                .ToListAsync();
        }

        // Stock is checked and deducted in CartController — service just saves
        public async Task<bool> AddToCartAsync(string sessionId, int productId, int quantity)
        {
            var existing = await _context.CartItems
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                await _context.CartItems.AddAsync(new CartItem
                {
                    SessionId = sessionId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task UpdateQuantityAsync(string sessionId, int cartItemId, int quantity)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.SessionId == sessionId);

            if (item == null) return;

            if (quantity <= 0)
                _context.CartItems.Remove(item);
            else
                item.Quantity = quantity;

            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(string sessionId, int cartItemId)
        {
            var item = await _context.CartItems
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.SessionId == sessionId);

            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(string sessionId)
        {
            var items = await _context.CartItems
                .Where(c => c.SessionId == sessionId)
                .ToListAsync();

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetCartCountAsync(string sessionId)
        {
            return await _context.CartItems
                .Where(c => c.SessionId == sessionId)
                .SumAsync(c => c.Quantity);
        }

        public async Task<decimal> GetCartTotalAsync(string sessionId)
        {
            return await _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.SessionId == sessionId)
                .SumAsync(c => c.Product!.Price * c.Quantity);
        }

        public async Task<int> GetProductQuantityInCartAsync(string sessionId, int productId)
        {
            var item = await _context.CartItems
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.ProductId == productId);
            return item?.Quantity ?? 0;
        }

        public async Task<int> GetProductStockAsync(int productId)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == productId);
            return product?.StockQuantity ?? 0;
        }

        public async Task<CartItem?> GetCartItemAsync(string sessionId, int cartItemId)
        {
            return await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId && c.SessionId == sessionId);
        }

        // ── NEW: lookup by productId (used by CartController.Add after saving) ─
        public async Task<CartItem?> GetCartItemByProductAsync(string sessionId, int productId)
        {
            return await _context.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId && c.ProductId == productId);
        }
    }
}