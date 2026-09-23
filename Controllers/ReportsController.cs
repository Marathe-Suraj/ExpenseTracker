using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers;

public class ReportsController(IReportsService reports, IAuthService auth) : Controller
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        var today = DateTime.Today;
        var from = fromDate ?? new DateTime(today.Year, today.Month, 1);
        var to = toDate ?? today;
        if (from > to) { ModelState.AddModelError(string.Empty, "From date cannot be after to date."); from = to; }
        return View(await reports.GetAsync(auth.GetCurrentUserId()!.Value, from, to));
    }
}
