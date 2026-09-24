using ExpenseTracker.Models;
using ExpenseTracker.Services;
using ExpenseTracker.ViewModels;
using ExpenseTracker.ViewModels.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.Controllers.Api
{
    [ApiController]
    [Route("api/expenses")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ExpensesApiController : ControllerBase
    {
        private readonly IExpenseService _expenseService;
        private readonly IAuthService _authService;

        public ExpensesApiController(IExpenseService expenseService, IAuthService authService)
        {
            _expenseService = expenseService;
            _authService = authService;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ExpenseFilterViewModel filter)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            if (!filter.IsValid())
                return BadRequest(new ApiError { Error = "Invalid filter parameters." });

            filter.Page = filter.Page <= 0 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize <= 0 ? int.MaxValue : filter.PageSize;

            var result = await _expenseService.SearchAsync(userId.Value, filter);
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var expense = await _expenseService.GetAsync(userId.Value, id);
            if (expense == null)
                return NotFound(new ApiError { Error = "Expense not found." });

            return Ok(expense);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Expense model)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            if (model.Amount <= 0)
                return BadRequest(new ApiError { Error = "Amount must be greater than 0." });
            if (model.CategoryId <= 0)
                return BadRequest(new ApiError { Error = "Category is required." });

            model.UserId = userId.Value;
            model.IsActive = true;
            model.CreatedDate = DateTime.UtcNow;

            var id = await _expenseService.CreateAsync(model);
            if (id <= 0)
                return BadRequest(new ApiError { Error = "Failed to create expense." });

            model.ExpenseId = id;
            return Ok(model);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Expense model)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var existing = await _expenseService.GetAsync(userId.Value, id);
            if (existing == null)
                return NotFound(new ApiError { Error = "Expense not found." });

            if (model.Amount <= 0)
                return BadRequest(new ApiError { Error = "Amount must be greater than 0." });
            if (model.CategoryId <= 0)
                return BadRequest(new ApiError { Error = "Category is required." });

            model.ExpenseId = id;
            model.UserId = userId.Value;
            model.IsActive = true;

            var ok = await _expenseService.UpdateAsync(model);
            if (!ok)
                return BadRequest(new ApiError { Error = "Failed to update expense." });

            return Ok(model);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _authService.GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new ApiError { Error = "Unauthorized." });

            var existing = await _expenseService.GetAsync(userId.Value, id);
            if (existing == null)
                return NotFound(new ApiError { Error = "Expense not found." });

            var ok = await _expenseService.DeleteAsync(userId.Value, id);
            if (!ok)
                return BadRequest(new ApiError { Error = "Failed to delete expense." });

            return Ok(new { success = true });
        }
    }
}
