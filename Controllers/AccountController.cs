using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Data.Repositories;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _users;
        public AccountController(IAuthService authService, IUserRepository users)
        {
            _authService = authService;
            _users = users;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string username, string password, bool rememberMe = false, string? returnUrl = null)
        {
            var (success, error) = await _authService.LoginAsync(username, password, rememberMe);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Login failed");
                return View();
            }
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Register(string username, string password)
        {
            var (success, error) = await _authService.RegisterAsync(username, password);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Registration failed");
                return View();
            }
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login");
            
            var user = await _users.GetByIdAsync(userId.Value);
            if (user == null) return NotFound();
            var model = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                JoinDate = user.CreatedDate
            };
            
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _users.UpdateProfileAsync(_authService.GetCurrentUserId()!.Value, model.Email, model.FullName);
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var user = await _users.GetByIdAsync(_authService.GetCurrentUserId()!.Value);
            if (user == null) return NotFound();
            var model = new SettingsViewModel
            {
                EmailNotifications = user.EmailNotifications,
                DarkMode = user.DarkMode,
                Currency = user.Currency,
                DateFormat = user.DateFormat,
                Language = user.Language
            };
            
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _users.UpdateSettingsAsync(_authService.GetCurrentUserId()!.Value, model.Currency, model.DateFormat, model.Language, model.EmailNotifications, model.DarkMode);
            TempData["SuccessMessage"] = "Settings saved successfully!";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
                return RedirectToAction(nameof(Profile));
            }
            var userId = _authService.GetCurrentUserId()!.Value;
            var user = await _users.GetByIdAsync(userId);
            if (user == null) return NotFound();
            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Current password is incorrect.";
                return RedirectToAction(nameof(Profile));
            }
            await _users.ChangePasswordAsync(userId, BCrypt.Net.BCrypt.HashPassword(model.NewPassword));
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction(nameof(Profile));
        }
    }

    public class ProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        [EmailAddress, StringLength(255)] public string? Email { get; set; }
        [StringLength(200)] public string? FullName { get; set; }
        public DateTime JoinDate { get; set; }
    }

    public class SettingsViewModel
    {
        public bool EmailNotifications { get; set; }
        public bool DarkMode { get; set; }
        [Required] public string Currency { get; set; } = "INR";
        [Required] public string DateFormat { get; set; } = "dd/MM/yyyy";
        [Required] public string Language { get; set; } = "English";
    }

    public class ChangePasswordViewModel
    {
        [Required] public string CurrentPassword { get; set; } = string.Empty;
        [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
        [Required, Compare(nameof(NewPassword))] public string ConfirmPassword { get; set; } = string.Empty;
    }
}


