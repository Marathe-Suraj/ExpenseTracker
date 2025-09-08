using System.Threading.Tasks;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        public AccountController(IAuthService authService)
        {
            _authService = authService;
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
        public IActionResult Profile()
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return RedirectToAction("Login");
            
            // For now, we'll use mock data. In a real app, you'd fetch from database
            var model = new ProfileViewModel
            {
                Username = User.Identity?.Name ?? "User",
                Email = "user@example.com",
                FullName = "John Doe",
                JoinDate = DateTime.Now.AddMonths(-6)
            };
            
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Here you would update the user profile in the database
            // For now, we'll just show a success message
            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Settings()
        {
            var model = new SettingsViewModel
            {
                EmailNotifications = true,
                DarkMode = false,
                Currency = "USD",
                DateFormat = "MM/dd/yyyy",
                Language = "English"
            };
            
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Settings(SettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Here you would save the settings to the database
            TempData["SuccessMessage"] = "Settings saved successfully!";
            return RedirectToAction("Settings");
        }
    }

    public class ProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
    }

    public class SettingsViewModel
    {
        public bool EmailNotifications { get; set; }
        public bool DarkMode { get; set; }
        public string Currency { get; set; } = "USD";
        public string DateFormat { get; set; } = "MM/dd/yyyy";
        public string Language { get; set; } = "English";
    }
}


