using System;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Web.Models;
using ExpenseTracker.Web.Services;
using ExpenseTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExpenseTracker.Web.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly IExpenseService _expenseService;
        private readonly ICategoryService _categoryService;
        private readonly IAuthService _authService;

        public ExpensesController(IExpenseService expenseService, ICategoryService categoryService, IAuthService authService)
        {
            _expenseService = expenseService;
            _categoryService = categoryService;
            _authService = authService;
        }

        private async Task<SelectList> BuildCategoriesSelectList(int userId)
        {
            var items = await _categoryService.GetAllAsync(userId);
            return new SelectList(items, nameof(Category.CategoryId), nameof(Category.Name));
        }

        public async Task<IActionResult> Index([FromQuery] ExpenseFilterViewModel filter)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            // Load full dataset for client-side DataTables so all entries are available for paging/search
            filter.Page = 1;
            filter.PageSize = int.MaxValue;
            filter.Categories = await BuildCategoriesSelectList(userId);
            var result = await _expenseService.SearchAsync(userId, filter);
            ViewBag.Paged = result;
            ViewBag.Filter = filter;
            var cats = await _categoryService.GetAllAsync(userId);
            ViewBag.CatLookup = cats.ToDictionary(c => c.CategoryId, c => c.Name);
            return View(result.Items);
        }

        public async Task<IActionResult> Create()
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            ViewBag.Categories = await BuildCategoriesSelectList(userId);
            return View(new Expense { ExpenseDate = DateTime.Today, IsActive = true });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Expense model)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await BuildCategoriesSelectList(userId);
                return View(model);
            }
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Amount must be greater than 0");
                ViewBag.Categories = await BuildCategoriesSelectList(userId);
                return View(model);
            }
            model.UserId = userId;
            model.CreatedDate = DateTime.UtcNow;
            await _expenseService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var model = await _expenseService.GetAsync(userId, id);
            if (model == null) return NotFound();
            ViewBag.Categories = await BuildCategoriesSelectList(userId);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Expense model)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await BuildCategoriesSelectList(userId);
                return View(model);
            }
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Amount must be greater than 0");
                ViewBag.Categories = await BuildCategoriesSelectList(userId);
                return View(model);
            }
            model.UserId = userId;
            await _expenseService.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var model = await _expenseService.GetAsync(userId, id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            await _expenseService.DeleteAsync(userId, id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<FileResult> ExportExcel([FromQuery] ExpenseFilterViewModel filter)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            filter.Page = 1; filter.PageSize = int.MaxValue;
            var data = await _expenseService.SearchAsync(userId, filter);
            var bytes = _expenseService.ExportToExcel(data.Items);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "expenses.xlsx");
        }

        [HttpGet]
        public async Task<FileResult> ExportPdf([FromQuery] ExpenseFilterViewModel filter)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            filter.Page = 1; filter.PageSize = int.MaxValue;
            var data = await _expenseService.SearchAsync(userId, filter);
            var bytes = _expenseService.ExportToPdf(data.Items);
            return File(bytes, "application/pdf", "expenses.pdf");
        }
    }
}


