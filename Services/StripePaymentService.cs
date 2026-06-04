using Stripe;

namespace ECommerceApp.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;

        public StripePaymentService(IConfiguration configuration)
        {
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<PaymentResult> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(amount * 100), // convert to cents
                    Currency = currency,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(options);

                return new PaymentResult
                {
                    Success = true,
                    PaymentIntentId = intent.Id,
                    ClientSecret = intent.ClientSecret
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PaymentResult> ConfirmPaymentAsync(string paymentIntentId)
        {
            try
            {
                var service = new PaymentIntentService();
                var intent = await service.GetAsync(paymentIntentId);

                return new PaymentResult
                {
                    Success = intent.Status == "succeeded",
                    PaymentIntentId = intent.Id,
                    ErrorMessage = intent.Status != "succeeded" ? $"Payment status: {intent.Status}" : null
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<PaymentResult> RefundPaymentAsync(string chargeId, decimal amount)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    Charge = chargeId,
                    Amount = (long)(amount * 100)
                };

                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                return new PaymentResult
                {
                    Success = refund.Status == "succeeded",
                    ErrorMessage = refund.Status != "succeeded" ? $"Refund status: {refund.Status}" : null
                };
            }
            catch (StripeException ex)
            {
                return new PaymentResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}