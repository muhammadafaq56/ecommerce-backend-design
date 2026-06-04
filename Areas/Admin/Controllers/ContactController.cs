using ECommerceApp.Data;
using ECommerceApp.Models;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ContactController(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Admin/Contact
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Contact Messages";

            var messages = await _context.ContactMessages
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();

            return View(messages);
        }

        // GET: Admin/Contact/Detail/5
        public async Task<IActionResult> Detail(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            ViewData["Title"] = $"Message from {msg.Name}";
            return View(msg);
        }

        // POST: Admin/Contact/Reply
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string replyText)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg == null) return NotFound();

            if (string.IsNullOrWhiteSpace(replyText))
            {
                TempData["Error"] = "Reply cannot be empty.";
                return RedirectToAction(nameof(Detail), new { id });
            }

            try
            {
                await _emailService.SendContactReplyAsync(
                    msg.Email,
                    msg.Name,
                    msg.Subject,
                    replyText.Trim());

                msg.IsReplied = true;
                msg.ReplyText = replyText.Trim();
                msg.RepliedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Reply sent to {msg.Email} successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to send email: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Contact/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var msg = await _context.ContactMessages.FindAsync(id);
            if (msg != null)
            {
                _context.ContactMessages.Remove(msg);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Message deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}