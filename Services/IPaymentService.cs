namespace ECommerceApp.Services
{
    public class PaymentResult
    {
        public bool Success { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public interface IPaymentService
    {
        Task<PaymentResult> CreatePaymentIntentAsync(decimal amount, string currency = "usd");
        Task<PaymentResult> ConfirmPaymentAsync(string paymentIntentId);
        Task<PaymentResult> RefundPaymentAsync(string chargeId, decimal amount);
    }
}