using ExpenseTracker.Models;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers;

public class BudgetsController(IBudgetService budgets, ICategoryService categories, IAuthService auth) : Controller
{
    public async Task<IActionResult> Index(int? year, int? month)
    {
        var userId = auth.GetCurrentUserId()!.Value;
        var now = DateTime.Today;
        var y = year ?? now.Year;
        var m = month is >= 1 and <= 12 ? month.Value : now.Month;
        ViewBag.Categories = await categories.GetAllAsync(userId);
        ViewBag.Year = y; ViewBag.Month = m;
        return View(await budgets.GetAsync(userId, y, m));
    }

    [HttpPost]
    public async Task<IActionResult> Upsert(Budget model)
    {
        var userId = auth.GetCurrentUserId()!.Value;
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Enter a valid category, amount and period."; return RedirectToAction(nameof(Index), new { model.Year, model.Month }); }
        model.UserId = userId;
        await budgets.UpsertAsync(model);
        TempData["SuccessMessage"] = "Budget saved.";
        return RedirectToAction(nameof(Index), new { model.Year, model.Month });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int year, int month)
    {
        await budgets.DeleteAsync(auth.GetCurrentUserId()!.Value, id);
        return RedirectToAction(nameof(Index), new { year, month });
    }
}
