using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExpenseTracker.Web.Models;

namespace ExpenseTracker.Web.Data.Repositories
{
    public interface IExpenseRepository
    {
        Task<int> CreateAsync(Expense expense);
        Task<bool> UpdateAsync(Expense expense);
        Task<bool> DeleteAsync(int userId, int expenseId);
        Task<Expense?> GetByIdAsync(int userId, int expenseId);
        Task<(IEnumerable<Expense> Items, int TotalCount)> SearchAsync(int userId, string? search, int? categoryId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
        Task<IEnumerable<(string CategoryName, decimal Total)>> GetTotalsByCategoryAsync(int userId, DateTime fromDate, DateTime toDate);
        Task<IEnumerable<(DateTime Date, decimal Total)>> GetDailyTotalsAsync(int userId, DateTime fromDate, DateTime toDate);
        Task<decimal> GetTotalForMonthAsync(int userId, DateTime monthStart, DateTime monthEnd);
    }
}


