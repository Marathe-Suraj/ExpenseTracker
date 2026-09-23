using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IAuthService _authService;
        private readonly IRecurringExpenseGenerator _recurringGenerator;

        public DashboardController(IDashboardService dashboardService, IAuthService authService, IRecurringExpenseGenerator recurringGenerator)
        {
            _dashboardService = dashboardService;
            _authService = authService;
            _recurringGenerator = recurringGenerator;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            await _recurringGenerator.GenerateAsync(userId);
            var vm = await _dashboardService.GetDashboardAsync(userId);
            return View(vm);
        }
    }
}


