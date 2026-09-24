using ExpenseTracker.Services;
using ExpenseTracker.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers.Api
{
    [ApiController]
    [Route("api/account")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AccountApiController : ControllerBase
    {
        private readonly IAuthService _authService;

        // In-memory prefs per user to mirror current web mock/local behavior
        private static readonly Dictionary<int, ProfileDto> Profiles = new();
        private static readonly Dictionary<int, SettingsDto> Settings = new();

        public AccountApiController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var username = _authService.GetCurrentUsername() ?? "User";
            if (!Profiles.TryGetValue(userId.Value, out var profile))
            {
                profile = new ProfileDto
                {
                    Username = username,
                    Email = "user@example.com",
                    FullName = "John Doe",
                    JoinDate = DateTime.UtcNow.AddMonths(-6)
                };
                Profiles[userId.Value] = profile;
            }
            else
            {
                profile.Username = username;
            }

            return Ok(profile);
        }

        [HttpPut("profile")]
        public IActionResult UpdateProfile([FromBody] ProfileDto model)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var username = _authService.GetCurrentUsername() ?? model.Username;
            model.Username = username;
            Profiles[userId.Value] = model;
            return Ok(model);
        }

        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            if (!Settings.TryGetValue(userId.Value, out var settings))
            {
                settings = new SettingsDto
                {
                    EmailNotifications = true,
                    DarkMode = false,
                    Currency = "INR",
                    DateFormat = "dd/MM/yyyy",
                    Language = "English"
                };
                Settings[userId.Value] = settings;
            }

            return Ok(settings);
        }

        [HttpPut("settings")]
        public IActionResult UpdateSettings([FromBody] SettingsDto model)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            Settings[userId.Value] = model;
            return Ok(model);
        }
    }
}
