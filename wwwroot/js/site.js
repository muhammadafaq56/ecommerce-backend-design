// Update cart count badge in navbar
async function updateCartCount() {
    try {
        const res = await fetch('/Cart/Count');
        if (res.ok) {
            const count = await res.json();
            const badge = document.getElementById('cart-count');
            if (badge) {
                badge.textContent = count;
                badge.style.display = count > 0 ? 'inline-block' : 'none';
            }
        }
    } catch (e) {
        console.warn('Could not fetch cart count:', e);
    }
}

// Run on every page load
document.addEventListener('DOMContentLoaded', function () {
    updateCartCount();

    // Auto-dismiss alerts after 4 seconds
    document.querySelectorAll('.alert.alert-success, .alert.alert-danger').forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert.close();
        }, 4000);
    });

    // Confirm before removing cart item
    document.querySelectorAll('form[action*="Cart/Remove"]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (!confirm('Remove this item from your cart?')) {
                e.preventDefault();
            }
        });
    });

    // Quantity input: prevent going below 1
    document.querySelectorAll('input[name="quantity"]').forEach(function (input) {
        input.addEventListener('change', function () {
            if (parseInt(this.value) < 1) this.value = 1;
        });
    });
});