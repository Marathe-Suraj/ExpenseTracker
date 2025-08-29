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

        public async Task<IActionResult> Index()
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var categories = await _categoryService.GetAllAsync(userId);
            return View(categories);
        }

        public IActionResult Create()
        {
            return View(new Category { IsActive = true });
        }

        [HttpPost]
        public async Task<IActionResult> Create(Category model)
        {
            if (!ModelState.IsValid) return View(model);
            model.CreatedDate = System.DateTime.UtcNow;
            await _categoryService.CreateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var category = await _categoryService.GetAsync(userId, id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Category model)
        {
            if (!ModelState.IsValid) return View(model);
            await _categoryService.UpdateAsync(model);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            var category = await _categoryService.GetAsync(userId, id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = _authService.GetCurrentUserId()!.Value;
            await _categoryService.DeleteAsync(userId, id);
            return RedirectToAction(nameof(Index));
        }
    }
}


