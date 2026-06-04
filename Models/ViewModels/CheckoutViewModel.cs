using System.ComponentModel.DataAnnotations;
namespace ECommerceApp.Models.ViewModels
{
    public class CheckoutViewModel
    {
        [Required] public string FirstName { get; set; } = string.Empty;
        [Required] public string LastName { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Phone { get; set; } = string.Empty;
        [Required] public string Address { get; set; } = string.Empty;
        [Required] public string City { get; set; } = string.Empty;
        [Required] public string Country { get; set; } = string.Empty;
        [Required] public string PostalCode { get; set; } = string.Empty;
        public string? Notes { get; set; }
        // Stripe
        public string? StripePublishableKey { get; set; }
        public string? ClientSecret { get; set; }
        public string? PaymentIntentId { get; set; } // ← new
        // Summary
        public List<CartItem> CartItems { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
    }
}