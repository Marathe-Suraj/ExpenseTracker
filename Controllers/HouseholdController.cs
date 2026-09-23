using ExpenseTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.Controllers;

public class HouseholdController(IHouseholdService households, IAuthService auth) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = auth.GetCurrentUserId()!.Value;
        var household = await households.GetAsync(userId);
        ViewBag.Members = household == null ? [] : await households.GetMembersAsync(userId);
        return View(household);
    }

    [HttpPost]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200) { TempData["ErrorMessage"] = "Enter a household name."; return RedirectToAction(nameof(Index)); }
        try { await households.CreateAsync(auth.GetCurrentUserId()!.Value, name.Trim()); TempData["SuccessMessage"] = "Household created."; }
        catch (SqlException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Invite(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) { TempData["ErrorMessage"] = "Enter a username."; return RedirectToAction(nameof(Index)); }
        try { await households.InviteAsync(auth.GetCurrentUserId()!.Value, username.Trim()); TempData["SuccessMessage"] = "Member added."; }
        catch (SqlException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Leave()
    {
        try { await households.LeaveAsync(auth.GetCurrentUserId()!.Value); TempData["SuccessMessage"] = "You left the household."; }
        catch (SqlException ex) { TempData["ErrorMessage"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
