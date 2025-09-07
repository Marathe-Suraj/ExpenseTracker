using System.Threading.Tasks;
using ExpenseTracker.Web.Models;
using ExpenseTracker.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Web.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IAuthService _authService;

        public CategoriesController(ICategoryService categoryService, IAuthService authService)
        {
            _categoryService = categoryService;
            _authService = authService;
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request?.Headers["X-Requested-With"], "XMLHttpRequest", System.StringComparison.OrdinalIgnoreCase);
        }

        public async Task<IActionResult> Index()
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var categories = await _categoryService.GetAllAsync(userId);
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var categories = await _categoryService.GetAllAsync(userId);
            return PartialView("_CategoryList", categories);
        }

        public IActionResult Create()
        {
            var model = new Category { IsActive = true };
            if (IsAjaxRequest()) return PartialView("_CreateEditModal", model);
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category model)
        {
            // Force new records to active
            model.IsActive = true;
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest()) return PartialView("_CreateEditModal", model);
                return View(model);
            }
            model.CreatedDate = System.DateTime.UtcNow;
            await _categoryService.CreateAsync(model);
            if (IsAjaxRequest()) return Json(new { success = true });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var category = await _categoryService.GetAsync(userId, id);
            if (category == null) return NotFound();
            if (IsAjaxRequest()) return PartialView("_CreateEditModal", category);
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category model)
        {
            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest()) return PartialView("_CreateEditModal", model);
                return View(model);
            }
            // Editing should not deactivate
            model.IsActive = true;
            await _categoryService.UpdateAsync(model);
            if (IsAjaxRequest()) return Json(new { success = true });
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var category = await _categoryService.GetAsync(userId, id);
            if (category == null) return NotFound();
            if (IsAjaxRequest()) return PartialView("_DeleteModal", category);
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            await _categoryService.DeleteAsync(userId, id);
            if (IsAjaxRequest()) return Json(new { success = true });
            return RedirectToAction(nameof(Index));
        }
    }
}


