using ECommerceApp.Models;
using ECommerceApp.Models.ViewModels;
using ECommerceApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerceApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        // ========================= LOGIN =========================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                if (User.IsInRole("ProductManager"))
                    return RedirectToAction("Index", "Product", new { area = "Admin" });
                if (User.IsInRole("CategoryManager"))
                    return RedirectToAction("Index", "Category", new { area = "Admin" });
                if (User.IsInRole("OrderManager"))
                    return RedirectToAction("Index", "Order", new { area = "Admin" });

                return RedirectToAction("Index", "Home");
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Login")]
        public async Task<IActionResult> LoginPost(
            LoginViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(model);
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                ModelState.AddModelError("", "Please confirm your email before logging in.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                if (await _userManager.IsInRoleAsync(user, "ProductManager"))
                    return RedirectToAction("Index", "Product", new { area = "Admin" });
                if (await _userManager.IsInRoleAsync(user, "CategoryManager"))
                    return RedirectToAction("Index", "Category", new { area = "Admin" });
                if (await _userManager.IsInRoleAsync(user, "OrderManager"))
                    return RedirectToAction("Index", "Order", new { area = "Admin" });

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home", new { area = "" });
            }

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
        }

        // ========================= REGISTER =========================

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model,
            string? returnUrl = null)
        {
            if (!ModelState.IsValid)
                return View(model);

            // ── Email exists but unconfirmed → update details and resend ──
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                if (!await _userManager.IsEmailConfirmedAsync(existingUser))
                {
                    // ✅ Update name in case they changed it
                    existingUser.FirstName = model.FirstName;
                    existingUser.LastName = model.LastName;
                    await _userManager.UpdateAsync(existingUser);

                    // ✅ Update password to the newly entered one
                    await _userManager.RemovePasswordAsync(existingUser);
                    var passwordResult = await _userManager
                        .AddPasswordAsync(existingUser, model.Password);

                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                            ModelState.AddModelError("", error.Description);
                        return View(model);
                    }

                    // ✅ Invalidate ALL previous confirmation links
                    await _userManager.UpdateSecurityStampAsync(existingUser);

                    var resendToken = await _userManager
                        .GenerateEmailConfirmationTokenAsync(existingUser);
                    var encodedResendToken = Uri.EscapeDataString(resendToken);

                    var resendLink = Url.Action(
                        "ConfirmEmail", "Account",
                        new { userId = existingUser.Id, token = encodedResendToken, returnUrl },
                        Request.Scheme)!;

                    try
                    {
                        await _emailService.SendEmailConfirmationAsync(
                            existingUser.Email!, resendLink);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Resend email failed: {ex.Message}");
                    }

                    return RedirectToAction("VerifyEmail");
                }

                // ── Confirmed account already exists ──
                ModelState.AddModelError("", "An account with this email already exists.");
                return View(model);
            }

            // ── Create brand new user ──
            var newUser = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(newUser, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Customer");

                var token = await _userManager
                    .GenerateEmailConfirmationTokenAsync(newUser);
                var encodedToken = Uri.EscapeDataString(token);

                var confirmationLink = Url.Action(
                    "ConfirmEmail", "Account",
                    new { userId = newUser.Id, token = encodedToken, returnUrl },
                    Request.Scheme)!;

                try
                {
                    await _emailService.SendEmailConfirmationAsync(
                        newUser.Email!, confirmationLink);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Email failed: {ex.Message}");
                }

                return RedirectToAction("VerifyEmail");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }

        // ========================= EMAIL CONFIRMATION =========================

        [HttpGet]
        public IActionResult VerifyEmail() => View();

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(
            string userId,
            string token,
            string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return RedirectToAction("Register");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return RedirectToAction("Register");

            // ✅ Decode token before use
            token = Uri.UnescapeDataString(token).Replace(" ", "+");

            var result = await _userManager.ConfirmEmailAsync(user, token);

            if (!result.Succeeded)
            {
                ViewBag.Error = "This confirmation link has expired or has already been used. " +
                                "Please register again to receive a new link.";
                return View("ConfirmEmail");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            return View("ConfirmEmail");
        }

        // ========================= LOGOUT =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ========================= ACCESS DENIED =========================

        [HttpGet]
        public IActionResult AccessDenied() => RedirectToAction("Index", "Home");

        // ========================= PROFILE =========================

        public async Task<IActionResult> Profile()
        {
            var loggedInUser = await _userManager.GetUserAsync(User);
            if (loggedInUser == null)
                return RedirectToAction("Login");

            return View(loggedInUser);
        }

        // ========================= FORGOT PASSWORD =========================

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null && await _userManager.IsEmailConfirmedAsync(user))
            {
                // ✅ Invalidate old reset tokens before sending new one
                await _userManager.UpdateSecurityStampAsync(user);

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = Uri.EscapeDataString(token);

                var resetLink = Url.Action(
                    "ResetPassword", "Account",
                    new { token = encodedToken, email = user.Email },
                    protocol: Request.Scheme)!;

                try
                {
                    await _emailService.SendPasswordResetAsync(user.Email!, resetLink);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Password reset email failed: {ex.Message}");
                }
            }

            TempData["Success"] = "If that email is registered, a reset link has been sent.";
            return View();
        }

        // ========================= RESET PASSWORD =========================

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
                return BadRequest("Invalid request.");

            ViewBag.Token = Uri.UnescapeDataString(token).Replace(" ", "+");
            ViewBag.Email = email;
            return View();
        }
        // ========================= GOOGLE LOGIN =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(
                "ExternalLoginCallback",
                "Account",
                new { returnUrl });

            var properties = _signInManager
                .ConfigureExternalAuthenticationProperties(provider, redirectUrl);

            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(
            string? returnUrl = null,
            string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                TempData["Debug"] = $"Google error: {remoteError}";
                return RedirectToAction("Login");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                TempData["Debug"] = "Unable to load Google login information.";
                return RedirectToAction("Login");
            }

            var result = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (result.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
            {
                TempData["Debug"] = "Google account email not found.";
                return RedirectToAction("Login");
            }

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser == null)
            {
                existingUser = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(existingUser);

                if (!createResult.Succeeded)
                {
                    TempData["Debug"] = string.Join(
                        ", ",
                        createResult.Errors.Select(e => e.Description));

                    return RedirectToAction("Login");
                }

                await _userManager.AddToRoleAsync(existingUser, "Customer");
            }

            await _userManager.AddLoginAsync(existingUser, info);

            await _signInManager.SignInAsync(existingUser, isPersistent: false);

            return LocalRedirect(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            string token,
            string email,
            string password,
            string confirmPassword)
        {
            token = Uri.UnescapeDataString(token).Replace(" ", "+");

            if (password != confirmPassword)
            {
                TempData["Error"] = "Passwords do not match.";
                ViewBag.Token = token;
                ViewBag.Email = email;
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                TempData["Success"] = "Password updated successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, password);

            if (result.Succeeded)
            {
                TempData["Success"] = "Password updated successfully. You can now log in.";
                return RedirectToAction("Login");
            }

            TempData["Error"] = string.Join(" ", result.Errors.Select(e => e.Description));
            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }
    }
}