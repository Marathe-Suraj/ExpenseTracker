using ExpenseTracker.Models;
using ExpenseTracker.Services;
using ExpenseTracker.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExpenseTracker.Controllers
{
    [Authorize]
    public class ExpensesController : Controller
    {
        private readonly IExpenseService _expenseService;
        private readonly ICategoryService _categoryService;
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _environment;
        private readonly IRecurringExpenseGenerator _recurringGenerator;

        public ExpensesController(IExpenseService expenseService, ICategoryService categoryService, IAuthService authService, IWebHostEnvironment environment, IRecurringExpenseGenerator recurringGenerator)
        {
            _expenseService = expenseService;
            _categoryService = categoryService;
            _authService = authService;
            _environment = environment;
            _recurringGenerator = recurringGenerator;
        }

        private async Task<SelectList> BuildCategoriesSelectList(int userId)
        {
            var items = await _categoryService.GetAllAsync(userId);
            return new SelectList(items, nameof(Category.CategoryId), nameof(Category.Name));
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request?.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> Index([FromQuery] ExpenseFilterViewModel filter)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            await _recurringGenerator.GenerateAsync(userId);
            if (!filter.IsValid())
            {
                ModelState.AddModelError(string.Empty, "Invalid filter range. Check dates and amounts.");
            }
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 25;
            filter.Categories = await BuildCategoriesSelectList(userId);
            var result = await _expenseService.SearchAsync(userId, filter);
            ViewBag.Paged = result;
            ViewBag.Filter = filter;
            var cats = await _categoryService.GetAllAsync(userId);
            ViewBag.CatLookup = cats.ToDictionary(c => c.CategoryId, c => c.Name);
            return View(result.Items);
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ExpenseFilterViewModel filter)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            if (filter.Page < 1) filter.Page = 1;
            if (filter.PageSize < 1 || filter.PageSize > 100) filter.PageSize = 25;
            filter.Categories = await BuildCategoriesSelectList(userId);
            var result = await _expenseService.SearchAsync(userId, filter);
            ViewBag.Paged = result;
            ViewBag.Filter = filter;
            var cats = await _categoryService.GetAllAsync(userId);
            ViewBag.CatLookup = cats.ToDictionary(c => c.CategoryId, c => c.Name);
            return PartialView("_ExpenseList", result.Items);
        }

        public async Task<IActionResult> Create()
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            ViewBag.Categories = await BuildCategoriesSelectList(userId);
            var model = new Expense { ExpenseDate = DateTime.Today, IsActive = true };
            if (IsAjaxRequest())
            {
                return PartialView("_CreateEditModal", model);
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense model, IFormFile? receipt)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            // Force new records to active
            model.IsActive = true;
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await BuildCategoriesSelectList(userId);
                if (IsAjaxRequest()) return PartialView("_CreateEditModal", model);
                return View(model);
            }
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Amount must be greater than 0");
                ViewBag.Categories = await BuildCategoriesSelectList(userId);
                if (IsAjaxRequest()) return PartialView("_CreateEditModal", model);
                return View(model);
            }
            model.UserId = userId;
            model.CreatedDate = DateTime.UtcNow;
            if (!await SaveReceiptAsync(userId, receipt, model)) return await InvalidExpenseAsync(model, userId);
            await _expenseService.CreateAsync(model);
            if (IsAjaxRequest()) return Json(new { success = true });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var model = await _expenseService.GetAsync(userId, id);
            if (model == null) return NotFound();
            ViewBag.Categories = await BuildCategoriesSelectList(userId);
            if (IsAjaxRequest()) return PartialView("_CreateEditModal", model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Expense model, IFormFile? receipt)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await BuildCategoriesSelectList(userId);
                if (IsAjaxRequest()) return PartialView("_CreateEditModal", model);
                return View(model);
            }
            if (model.Amount <= 0)
            {
                ModelState.AddModelError(nameof(model.Amount), "Amount must be greater than 0");
                ViewBag.Categories = await BuildCategoriesSelectList(userId);
                if (IsAjaxRequest()) return PartialView("_CreateEditModal", model);
                return View(model);
            }
            model.UserId = userId;
            // Preserve active state; editing should not deactivate
            model.IsActive = true;
            var existing = await _expenseService.GetAsync(userId, model.ExpenseId);
            if (existing == null) return NotFound();
            model.ReceiptPath = existing.ReceiptPath;
            if (!await SaveReceiptAsync(userId, receipt, model)) return await InvalidExpenseAsync(model, userId);
            await _expenseService.UpdateAsync(model);
            if (IsAjaxRequest()) return Json(new { success = true });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var model = await _expenseService.GetAsync(userId, id);
            if (model == null) return NotFound();
            if (IsAjaxRequest()) return PartialView("_DeleteModal", model);
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            await _expenseService.DeleteAsync(userId, id);
            if (IsAjaxRequest()) return Json(new { success = true });
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<FileResult> ExportExcel([FromQuery] ExpenseFilterViewModel filter)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            filter.Page = 1; filter.PageSize = int.MaxValue;
            var data = await _expenseService.SearchAsync(userId, filter);
            var bytes = _expenseService.ExportToExcel(data.Items);
            var ts = DateTime.Now.ToString("ddMMyyyyhhmmss");
            var filename = $"ExpensesReport_{ts}.xlsx";
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }

        [HttpGet]
        public async Task<FileResult> ExportPdf([FromQuery] ExpenseFilterViewModel filter)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            filter.Page = 1; filter.PageSize = int.MaxValue;
            var data = await _expenseService.SearchAsync(userId, filter);
            var bytes = _expenseService.ExportToPdf(data.Items);
            var ts = DateTime.Now.ToString("ddMMyyyyhhmmss");
            var filename = $"ExpensesReport_{ts}.pdf";
            return File(bytes, "application/pdf", filename);
        }

        private async Task<bool> SaveReceiptAsync(int userId, IFormFile? receipt, Expense model)
        {
            if (receipt == null || receipt.Length == 0) return true;
            var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["image/jpeg"] = ".jpg", ["image/png"] = ".png", ["application/pdf"] = ".pdf"
            };
            if (receipt.Length > 5 * 1024 * 1024 || !allowed.TryGetValue(receipt.ContentType, out var extension))
            {
                ModelState.AddModelError("receipt", "Receipt must be a JPG, PNG or PDF no larger than 5 MB.");
                return false;
            }
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "receipts", userId.ToString());
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
            await receipt.CopyToAsync(stream);
            model.ReceiptPath = $"/uploads/receipts/{userId}/{fileName}";
            return true;
        }

        private async Task<IActionResult> InvalidExpenseAsync(Expense model, int userId)
        {
            ViewBag.Categories = await BuildCategoriesSelectList(userId);
            return IsAjaxRequest() ? PartialView("_CreateEditModal", model) : View(model);
        }
    }
}


