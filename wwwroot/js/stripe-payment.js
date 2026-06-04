// Optional: standalone Stripe helper (logic is also inline in Checkout/Index.cshtml)
// This file is reserved for any additional Stripe UI helpers.

function initStripeElement(publishableKey, clientSecret) {
    const stripe = Stripe(publishableKey);
    const elements = stripe.elements();

    const style = {
        base: {
            fontSize: '16px',
            color: '#32325d',
            fontFamily: '"Segoe UI", sans-serif',
            '::placeholder': { color: '#aab7c4' }
        },
        invalid: { color: '#dc3545', iconColor: '#dc3545' }
    };

    const card = elements.create('card', { style });
    card.mount('#card-element');

    card.on('change', function (event) {
        const display = document.getElementById('card-errors');
        display.textContent = event.error ? event.error.message : '';
    });

    return { stripe, card, clientSecret };
}