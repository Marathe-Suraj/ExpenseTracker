using ExpenseTracker.Models;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers;

public class RecurringExpensesController(IRecurringExpenseService recurring, ICategoryService categories, IAuthService auth) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = auth.GetCurrentUserId()!.Value;
        ViewBag.Categories = await categories.GetAllAsync(userId);
        return View(await recurring.GetAsync(userId));
    }

    [HttpPost]
    public async Task<IActionResult> Save(RecurringExpense model)
    {
        var userId = auth.GetCurrentUserId()!.Value;
        if (model.EndDate.HasValue && model.EndDate < model.StartDate) ModelState.AddModelError(nameof(model.EndDate), "End date must be on or after start date.");
        if (model.NextRunDate < model.StartDate) ModelState.AddModelError(nameof(model.NextRunDate), "Next run must be on or after start date.");
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Enter valid recurring expense details."; return RedirectToAction(nameof(Index)); }
        model.UserId = userId;
        if (model.RecurringExpenseId == 0) await recurring.CreateAsync(model); else if (!await recurring.UpdateAsync(model)) return NotFound();
        TempData["SuccessMessage"] = "Recurring expense saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await recurring.DeleteAsync(auth.GetCurrentUserId()!.Value, id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
