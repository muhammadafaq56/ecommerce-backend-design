using ECommerceApp.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Services
{
    /// <summary>
    /// Tracks a 20-minute reservation window for every cart item.
    /// When a reservation expires the held stock is restored and the
    /// CartItem is deleted — exactly as if the user had clicked "Remove".
    /// Timers are per-CartItem, so every Add/Update gets its own countdown.
    /// Also exposes GetRemainingSeconds() so the UI can poll and show a live countdown.
    /// </summary>
    public sealed class CartReservationService : IHostedService, IDisposable
    {
        // ── Configuration ────────────────────────────────────────────────────
        public static readonly TimeSpan ReservationWindow = TimeSpan.FromMinutes(20);

        // ── State ────────────────────────────────────────────────────────────
        private readonly record struct Entry(CancellationTokenSource Cts, DateTime ExpiresAt);

        private readonly Dictionary<int, Entry> _timers = new();
        private readonly Lock _lock = new();

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CartReservationService> _logger;

        public CartReservationService(
            IServiceScopeFactory scopeFactory,
            ILogger<CartReservationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // ── IHostedService ───────────────────────────────────────────────────
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken) { Dispose(); return Task.CompletedTask; }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Starts (or resets) the 20-minute countdown for <paramref name="cartItemId"/>.
        /// Call this after every successful Add or Update.
        /// </summary>
        public void StartOrReset(int cartItemId)
        {
            var expiresAt = DateTime.UtcNow.Add(ReservationWindow);

            lock (_lock)
            {
                CancelExisting(cartItemId);
                var cts = new CancellationTokenSource();
                _timers[cartItemId] = new Entry(cts, expiresAt);
                _ = RunTimerAsync(cartItemId, cts.Token);
            }

            _logger.LogInformation(
                "Reservation timer started/reset for CartItem {CartItemId} (expires {ExpiresAt} UTC).",
                cartItemId, expiresAt);
        }

        /// <summary>
        /// Cancels the countdown (item removed or checked-out before expiry).
        /// </summary>
        public void Cancel(int cartItemId)
        {
            lock (_lock) { CancelExisting(cartItemId); }
            _logger.LogInformation("Reservation timer cancelled for CartItem {CartItemId}.", cartItemId);
        }

        /// <summary>
        /// Returns seconds remaining for <paramref name="cartItemId"/>,
        /// or null if no active timer exists (item expired or was removed).
        /// </summary>
        public int? GetRemainingSeconds(int cartItemId)
        {
            lock (_lock)
            {
                if (!_timers.TryGetValue(cartItemId, out var entry)) return null;
                var remaining = (int)(entry.ExpiresAt - DateTime.UtcNow).TotalSeconds;
                return remaining > 0 ? remaining : 0;
            }
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void CancelExisting(int cartItemId)   // must be called inside _lock
        {
            if (_timers.TryGetValue(cartItemId, out var old))
            {
                old.Cts.Cancel();
                old.Cts.Dispose();
                _timers.Remove(cartItemId);
            }
        }

        private async Task RunTimerAsync(int cartItemId, CancellationToken ct)
        {
            try
            {
                await Task.Delay(ReservationWindow, ct);
                await ExpireReservationAsync(cartItemId);
            }
            catch (OperationCanceledException) { /* user acted first — no-op */ }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in reservation timer for CartItem {CartItemId}.", cartItemId);
            }
            finally
            {
                lock (_lock)
                {
                    if (_timers.TryGetValue(cartItemId, out var e) && e.Cts.IsCancellationRequested)
                        _timers.Remove(cartItemId);
                }
            }
        }

        private async Task ExpireReservationAsync(int cartItemId)
        {
            _logger.LogInformation(
                "Reservation expired for CartItem {CartItemId}. Restoring stock.", cartItemId);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cartItem = await db.CartItems
                .Include(c => c.Product)
                .FirstOrDefaultAsync(c => c.Id == cartItemId);

            if (cartItem == null)
            {
                _logger.LogInformation(
                    "CartItem {CartItemId} already gone; skipping stock restore.", cartItemId);
                return;
            }

            if (cartItem.Product != null)
                cartItem.Product.StockQuantity += cartItem.Quantity;

            db.CartItems.Remove(cartItem);
            await db.SaveChangesAsync();

            _logger.LogInformation(
                "Stock restored and CartItem {CartItemId} removed after expiry.", cartItemId);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                foreach (var e in _timers.Values) { e.Cts.Cancel(); e.Cts.Dispose(); }
                _timers.Clear();
            }
        }
    }
}