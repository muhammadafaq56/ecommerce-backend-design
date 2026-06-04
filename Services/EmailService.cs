using Resend;

namespace ECommerceApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IResend _resend;

        public EmailService(IResend resend)
        {
            _resend = resend;
        }

        // ========================= EMAIL CONFIRMATION =========================

        public async Task SendEmailConfirmationAsync(
            string toEmail,
            string confirmationLink)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = "Confirm your email";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">✉️ Confirm your email</h2>
                    <p style="color:#555;">Welcome! Please confirm your email by clicking the button below:</p>
                    <a href="{confirmationLink}"
                       style="display:inline-block;background:#000;color:#fff;
                              padding:12px 28px;border-radius:6px;text-decoration:none;
                              font-weight:600;margin:16px 0;">
                        Confirm Email
                    </a>
                    <p style="color:#888;font-size:13px;">If you didn't register, you can safely ignore this email.</p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">© Afaq Mart</p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= ORDER STATUS UPDATE (generic, non-cancel) =========================

        public async Task SendOrderStatusUpdateAsync(
            string toEmail,
            string orderNumber,
            string newStatus)
        {
            var (emoji, label, description) = newStatus switch
            {
                "Processing" => ("🔄", "Being Processed", "We've received your order and are getting it ready."),
                "Shipped" => ("📦", "Shipped", "Your order is on its way!"),
                "Delivered" => ("✅", "Delivered", "Your order has been delivered. Enjoy!"),
                "Cancelled" => ("❌", "Cancelled", "Your order has been cancelled. Contact us if this was a mistake."),
                "ReturnProcessing" => ("🔁", "Return Processing", "We've received your return request and are reviewing it."),
                _ => ("📋", newStatus, "Your order status has been updated.")
            };

            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = $"{emoji} Order #{orderNumber} — {label}";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">{emoji} Order Status Update</h2>
                    <p style="color:#555;">{description}</p>
                    <div style="background:#f9f9f9;border-radius:8px;padding:20px;margin:24px 0;">
                        <p style="margin:0;font-size:13px;color:#888;text-transform:uppercase;letter-spacing:.05em;">Order Number</p>
                        <p style="margin:4px 0 16px;font-size:20px;font-weight:700;">#{orderNumber}</p>
                        <p style="margin:0;font-size:13px;color:#888;text-transform:uppercase;letter-spacing:.05em;">New Status</p>
                        <p style="margin:4px 0 0;font-size:18px;font-weight:600;">{label}</p>
                    </div>
                    <p style="color:#555;font-size:14px;">
                        If you have any questions, reply to this email or contact our support team.
                    </p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">
                        © Afaq Mart · You're receiving this because you placed an order with us.
                    </p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= CONTACT REPLY =========================

        public async Task SendContactReplyAsync(
            string toEmail,
            string customerName,
            string originalSubject,
            string replyText)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = $"Re: {originalSubject}";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">💬 Reply to your message</h2>
                    <p style="color:#555;">Hi {customerName},</p>
                    <p style="color:#555;">
                        Thank you for reaching out. Here is our response regarding
                        <strong>{originalSubject}</strong>:
                    </p>
                    <div style="background:#f9f9f9;border-left:4px solid #d97706;
                                border-radius:4px;padding:20px;margin:24px 0;
                                color:#333;font-size:15px;line-height:1.7;">
                        {replyText}
                    </div>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">
                        © Afaq Mart · You're receiving this because you contacted our support team.
                    </p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= PASSWORD RESET =========================

        public async Task SendPasswordResetAsync(
            string toEmail,
            string resetLink)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = "Reset your password";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">🔐 Password Reset Request</h2>
                    <p style="color:#555;">We received a request to reset your password.
                       Click the button below to choose a new one:</p>
                    <a href="{resetLink}"
                       style="display:inline-block;background:#000;color:#fff;
                              padding:12px 28px;border-radius:6px;text-decoration:none;
                              font-weight:600;margin:16px 0;">
                        Reset Password
                    </a>
                    <p style="color:#888;font-size:13px;">
                        This link expires in 1 hour. If you didn't request a password reset,
                        you can safely ignore this email.
                    </p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">
                        © Afaq Mart · You're receiving this because a reset was requested for your account.
                    </p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= ORDER CONFIRMATION =========================

        public async Task SendOrderConfirmationAsync(
            string toEmail,
            string orderNumber,
            decimal total,
            string customerName)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = $"✅ Order Confirmed — #{orderNumber}";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:20px;">🎉 Thank you for your order, {customerName}!</h2>
                    <p style="color:#555;">
                        Your payment was successful and your order has been placed.
                        We'll notify you once it's being processed.
                    </p>
                    <div style="background:#f9f9f9;border-radius:8px;padding:20px;margin:24px 0;">
                        <p style="margin:0;font-size:13px;color:#888;text-transform:uppercase;
                                  letter-spacing:.05em;">Order Number</p>
                        <p style="margin:4px 0 16px;font-size:24px;font-weight:700;">
                            #{orderNumber}
                        </p>
                        <p style="margin:0;font-size:13px;color:#888;text-transform:uppercase;
                                  letter-spacing:.05em;">Total Paid</p>
                        <p style="margin:4px 0 0;font-size:20px;font-weight:600;color:#198754;">
                            ${total:F2}
                        </p>
                    </div>
                    <p style="color:#555;font-size:14px;">
                        If you have any questions about your order, reply to this email.
                    </p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">
                        © Afaq Mart · You're receiving this because you placed an order with us.
                    </p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= CONTACT FORM CONFIRMATION =========================

        public async Task SendContactConfirmationAsync(
            string toEmail,
            string customerName,
            string subject)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = "📩 We received your message!";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">📩 Message Received</h2>
                    <p style="color:#555;">Hi {customerName},</p>
                    <p style="color:#555;">
                        Thank you for contacting us! We've received your message regarding
                        <strong>{subject}</strong> and our team will get back to you as soon as possible.
                    </p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">© Afaq Mart</p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= RETURN / CANCEL REQUEST RECEIVED =========================

        public async Task SendReturnRequestReceivedAsync(
            string toEmail,
            string customerName,
            string orderNumber)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = $"🔁 Return/Cancellation Request Received — #{orderNumber}";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">🔁 Request Received</h2>
                    <p style="color:#555;">Hi {customerName},</p>
                    <p style="color:#555;">
                        We've received your return/cancellation request for order
                        <strong>#{orderNumber}</strong>. Our team will review it and get back to you shortly.
                    </p>
                    <p style="color:#555;font-size:14px;">
                        If you did not make this request, please reply to this email immediately.
                    </p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">© Afaq Mart</p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= REVIEW THANK YOU =========================

        public async Task SendReviewThankYouAsync(
            string toEmail,
            string customerName,
            string productName)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = "⭐ Thanks for your review!";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">⭐ Thank You for Your Review!</h2>
                    <p style="color:#555;">Hi {customerName},</p>
                    <p style="color:#555;">
                        Thank you for reviewing <strong>{productName}</strong>.
                        Your feedback helps other shoppers make better decisions.
                    </p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">© Afaq Mart</p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= CANCELLED + REFUND (admin-initiated) =========================

        public async Task SendCancellationWithRefundAsync(
            string toEmail,
            string customerName,
            string orderNumber,
            decimal refundAmount)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = $"❌ Order #{orderNumber} Cancelled — Refund Initiated";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">❌ Order Cancelled &amp; Refund Initiated</h2>
                    <p style="color:#555;">Hi {customerName},</p>
                    <p style="color:#555;">
                        Your order has been cancelled and a refund has been initiated.
                        Please allow 5–7 business days for the amount to reflect in your account.
                    </p>
                    <div style="background:#f9f9f9;border-radius:8px;padding:20px;margin:24px 0;">
                        <p style="margin:0;font-size:13px;color:#888;text-transform:uppercase;letter-spacing:.05em;">Order Number</p>
                        <p style="margin:4px 0 16px;font-size:22px;font-weight:700;">#{orderNumber}</p>
                        <p style="margin:0;font-size:13px;color:#888;text-transform:uppercase;letter-spacing:.05em;">Refund Amount</p>
                        <p style="margin:4px 0 0;font-size:20px;font-weight:600;color:#198754;">${refundAmount:F2}</p>
                    </div>
                    <p style="color:#555;font-size:14px;">
                        If you have questions, reply to this email.
                    </p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">© Afaq Mart</p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= RETURN / CANCEL ACCEPTED =========================

        public async Task SendReturnAcceptedAsync(
            string toEmail,
            string customerName,
            string orderNumber,
            decimal refundAmount)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = $"✅ Return Request Approved — #{orderNumber}";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">✅ Your Request Has Been Approved</h2>
                    <p style="color:#555;">Hi {customerName},</p>
                    <p style="color:#555;">
                        Your return/cancellation request for order <strong>#{orderNumber}</strong>
                        has been approved. A refund has been initiated and should appear
                        in your account within 5–7 business days.
                    </p>
                    <div style="background:#f9f9f9;border-radius:8px;padding:20px;margin:24px 0;">
                        <p style="margin:0;font-size:13px;color:#888;text-transform:uppercase;letter-spacing:.05em;">Order Number</p>
                        <p style="margin:4px 0 16px;font-size:22px;font-weight:700;">#{orderNumber}</p>
                        <p style="margin:0;font-size:13px;color:#888;text-transform:uppercase;letter-spacing:.05em;">Refund Amount</p>
                        <p style="margin:4px 0 0;font-size:20px;font-weight:600;color:#198754;">${refundAmount:F2}</p>
                    </div>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">© Afaq Mart</p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }

        // ========================= RETURN / CANCEL REJECTED =========================

        public async Task SendReturnRejectedAsync(
            string toEmail,
            string customerName,
            string orderNumber)
        {
            var message = new Resend.EmailMessage();
            message.From = "Afaq Mart <noreply@afaqmart.store>";
            message.To.Add(toEmail);
            message.Subject = $"❗ Return Request Update — #{orderNumber}";
            message.HtmlBody = $"""
                <div style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:32px;">
                    <h1 style="font-size:22px;margin-bottom:4px;">Afaq Mart</h1>
                    <hr style="border:none;border-top:1px solid #eee;margin:16px 0;" />
                    <h2 style="font-size:18px;">❗ We Could Not Approve Your Request</h2>
                    <p style="color:#555;">Hi {customerName},</p>
                    <p style="color:#555;">
                        We're sorry, but we were unable to approve the return/cancellation
                        for order <strong>#{orderNumber}</strong>.
                    </p>
                    <p style="color:#555;">
                        This may be due to the return window having passed or the item not
                        meeting our return policy conditions. If you believe this was a mistake,
                        please reply to this email and we'll do our best to help.
                    </p>
                    <hr style="border:none;border-top:1px solid #eee;margin:24px 0;" />
                    <p style="color:#aaa;font-size:12px;text-align:center;">© Afaq Mart</p>
                </div>
            """;
            await _resend.EmailSendAsync(message);
        }
    }
}