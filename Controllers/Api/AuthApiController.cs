using ExpenseTracker.Services;
using ExpenseTracker.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtTokenService _jwtTokenService;

        public AuthApiController(IAuthService authService, IJwtTokenService jwtTokenService)
        {
            _authService = authService;
            _jwtTokenService = jwtTokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new ApiError { Error = "Username and password are required." });

            var (success, error) = await _authService.RegisterAsync(request.Username.Trim(), request.Password);
            if (!success)
                return BadRequest(new ApiError { Error = error ?? "Registration failed." });

            var (ok, user, loginError) = await _authService.ValidateCredentialsAsync(request.Username.Trim(), request.Password);
            if (!ok || user == null)
                return Ok(new { message = "Registered successfully. Please login." });

            var token = _jwtTokenService.CreateToken(user);
            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.UserId,
                Username = user.Username
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new ApiError { Error = "Username and password are required." });

            var (success, user, error) = await _authService.ValidateCredentialsAsync(request.Username.Trim(), request.Password);
            if (!success || user == null)
                return Unauthorized(new ApiError { Error = error ?? "Invalid username or password." });

            var token = _jwtTokenService.CreateToken(user);
            return Ok(new AuthResponse
            {
                Token = token,
                UserId = user.UserId,
                Username = user.Username
            });
        }
    }
}
