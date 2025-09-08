using System;
using System.Security.Claims;
using System.Threading.Tasks;
using BCrypt.Net;
using ExpenseTracker.Data.Repositories;
using ExpenseTracker.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string? Error)> RegisterAsync(string username, string password);
        Task<(bool Success, string? Error)> LoginAsync(string username, string password, bool rememberMe = false);
        Task LogoutAsync();
        int? GetCurrentUserId();
    }

    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(string username, string password)
        {
            try
            {
                var existing = await _userRepository.GetByUsernameAsync(username);
                if (existing != null)
                {
                    return (false, "Username already exists.");
                }

                var user = new User
                {
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    CreatedDate = DateTime.UtcNow
                };
                var id = await _userRepository.CreateAsync(user);
                if (id <= 0) return (false, "Failed to create user.");
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Username}", username);
                return (false, "An unexpected error occurred.");
            }
        }

        public async Task<(bool Success, string? Error)> LoginAsync(string username, string password, bool rememberMe = false)
        {
            try
            {
                var user = await _userRepository.GetByUsernameAsync(username);
                if (user == null) return (false, "Invalid username or password.");
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    return (false, "Invalid username or password.");
                }

                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Username)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await _httpContextAccessor.HttpContext!.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = rememberMe,
                        ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
                    });
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {Username}", username);
                return (false, "An unexpected error occurred.");
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                if (_httpContextAccessor.HttpContext != null)
                {
                    await _httpContextAccessor.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed");
                throw;
            }
        }

        public int? GetCurrentUserId()
        {
            var http = _httpContextAccessor.HttpContext;
            if (http?.User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out var id)) return id;
            }
            return null;
        }
    }
}


