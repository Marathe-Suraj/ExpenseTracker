using ExpenseTracker.Services;
using ExpenseTracker.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers.Api
{
    [ApiController]
    [Route("api/dashboard")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class DashboardApiController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IAuthService _authService;

        public DashboardApiController(IDashboardService dashboardService, IAuthService authService)
        {
            _dashboardService = dashboardService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var vm = await _dashboardService.GetDashboardAsync(userId.Value);
            return Ok(vm);
        }
    }
}
