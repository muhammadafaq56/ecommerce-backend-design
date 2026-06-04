namespace ECommerceApp.Services
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(string toEmail, string confirmationLink);
        Task SendOrderStatusUpdateAsync(string toEmail, string orderNumber, string newStatus);
        Task SendPasswordResetAsync(string toEmail, string resetLink);
        Task SendOrderConfirmationAsync(string toEmail, string orderNumber, decimal total, string customerName);
        Task SendContactReplyAsync(string toEmail, string customerName, string originalSubject, string replyText);

        // Sent to customer immediately when they submit the contact form
        Task SendContactConfirmationAsync(string toEmail, string customerName, string subject);

        // Return / Cancel request received (customer submitted)
        Task SendReturnRequestReceivedAsync(string toEmail, string customerName, string orderNumber);

        // Review thank-you
        Task SendReviewThankYouAsync(string toEmail, string customerName, string productName);

        // Admin cancelled → cancel + refund in one mail
        Task SendCancellationWithRefundAsync(string toEmail, string customerName, string orderNumber, decimal refundAmount);

        // Admin accepted customer return/cancel request
        Task SendReturnAcceptedAsync(string toEmail, string customerName, string orderNumber, decimal refundAmount);

        // Admin rejected customer return/cancel request
        Task SendReturnRejectedAsync(string toEmail, string customerName, string orderNumber);
    }
}