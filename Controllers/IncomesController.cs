using ExpenseTracker.Models;
using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers;

public class IncomesController(IIncomeService incomes, IAuthService auth) : Controller
{
    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate > toDate) ModelState.AddModelError(string.Empty, "From date cannot be after to date.");
        ViewBag.FromDate = fromDate; ViewBag.ToDate = toDate;
        return View(await incomes.SearchAsync(auth.GetCurrentUserId()!.Value, fromDate, toDate));
    }

    [HttpPost]
    public async Task<IActionResult> Save(Income model)
    {
        if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Enter valid income details."; return RedirectToAction(nameof(Index)); }
        model.UserId = auth.GetCurrentUserId()!.Value;
        if (model.IncomeId == 0) await incomes.CreateAsync(model); else if (!await incomes.UpdateAsync(model)) return NotFound();
        TempData["SuccessMessage"] = "Income saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await incomes.DeleteAsync(auth.GetCurrentUserId()!.Value, id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
