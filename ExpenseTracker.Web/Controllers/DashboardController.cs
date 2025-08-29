using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IAuthService _authService;

        public DashboardController(IDashboardService dashboardService, IAuthService authService)
        {
            _dashboardService = dashboardService;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var vm = await _dashboardService.GetDashboardAsync(userId);
            return View(vm);
        }
    }
}


