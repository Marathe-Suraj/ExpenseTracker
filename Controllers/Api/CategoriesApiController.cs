using ExpenseTracker.Models;
using ExpenseTracker.Services;
using ExpenseTracker.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers.Api
{
    [ApiController]
    [Route("api/categories")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CategoriesApiController : ControllerBase
    {
        private readonly ICategoryService _categoryService;
        private readonly IAuthService _authService;

        public CategoriesApiController(ICategoryService categoryService, IAuthService authService)
        {
            _categoryService = categoryService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] bool includeInactive = false)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var categories = includeInactive
                ? await _categoryService.GetAllIncludingInactiveAsync(userId.Value)
                : await _categoryService.GetAllAsync(userId.Value);
            return Ok(categories);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var category = await _categoryService.GetAsync(userId.Value, id);
            if (category == null)
                return NotFound(new ApiError { Error = "Category not found." });

            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Category model)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new ApiError { Error = "Name is required." });

            model.UserId = userId.Value;
            model.IsActive = true;
            model.CreatedDate = DateTime.UtcNow;

            var id = await _categoryService.CreateAsync(model);
            if (id <= 0)
                return BadRequest(new ApiError { Error = "Failed to create category." });

            model.CategoryId = id;
            return Ok(model);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Category model)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var existing = await _categoryService.GetAsync(userId.Value, id);
            if (existing == null)
                return NotFound(new ApiError { Error = "Category not found." });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new ApiError { Error = "Name is required." });

            model.CategoryId = id;
            model.UserId = userId.Value;
            model.IsActive = true;

            var ok = await _categoryService.UpdateAsync(model);
            if (!ok)
                return BadRequest(new ApiError { Error = "Failed to update category." });

            return Ok(model);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var existing = await _categoryService.GetAsync(userId.Value, id);
            if (existing == null)
                return NotFound(new ApiError { Error = "Category not found." });

            var ok = await _categoryService.DeleteAsync(userId.Value, id);
            if (!ok)
                return BadRequest(new ApiError { Error = "Failed to delete category." });

            return Ok(new { success = true });
        }

        [HttpPost("{id:int}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var category = await _categoryService.ToggleStatusAsync(userId.Value, id);
            if (category == null)
                return NotFound(new ApiError { Error = "Category not found." });

            return Ok(new
            {
                success = true,
                isActive = category.IsActive,
                message = category.IsActive ? "Category activated successfully" : "Category deactivated successfully",
                category
            });
        }
    }
}
