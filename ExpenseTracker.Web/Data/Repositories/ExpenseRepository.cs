using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ExpenseTracker.Web.Models;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Web.Data.Repositories
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<ExpenseRepository> _logger;

        public ExpenseRepository(IDbConnectionFactory connectionFactory, ILogger<ExpenseRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<int> CreateAsync(Expense expense)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                return await connection.ExecuteScalarAsync<int>(
                    "dbo.usp_CreateExpenses",
                    new
                    {
                        expense.UserId,
                        expense.CategoryId,
                        expense.Amount,
                        expense.Description,
                        expense.ExpenseDate,
                        expense.CreatedDate,
                        expense.IsActive
                    },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create expense for user {UserId}", expense.UserId);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Expense expense)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var rows = await connection.ExecuteScalarAsync<int>(
                    "dbo.usp_UpdateExpenses",
                    new
                    {
                        expense.ExpenseId,
                        expense.UserId,
                        expense.CategoryId,
                        expense.Amount,
                        expense.Description,
                        expense.ExpenseDate,
                        expense.IsActive
                    },
                    commandType: CommandType.StoredProcedure);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update expense {ExpenseId} for user {UserId}", expense.ExpenseId, expense.UserId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int userId, int expenseId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var rows = await connection.ExecuteScalarAsync<int>(
                    "dbo.usp_DeleteExpenses",
                    new { ExpenseId = expenseId, UserId = userId },
                    commandType: CommandType.StoredProcedure);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete expense {ExpenseId} for user {UserId}", expenseId, userId);
                throw;
            }
        }

        public async Task<Expense?> GetByIdAsync(int userId, int expenseId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                return await connection.QueryFirstOrDefaultAsync<Expense>(
                    "dbo.usp_GetExpenseById",
                    new { ExpenseId = expenseId, UserId = userId },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get expense {ExpenseId} for user {UserId}", expenseId, userId);
                throw;
            }
        }

        public async Task<(IEnumerable<Expense> Items, int TotalCount)> SearchAsync(int userId, string? search, int? categoryId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var parameters = new
                {
                    UserId = userId,
                    Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search}%",
                    CategoryId = categoryId,
                    FromDate = fromDate,
                    ToDate = toDate,
                    Offset = (page - 1) * pageSize,
                    PageSize = pageSize
                };

                using var multi = await connection.QueryMultipleAsync(
                    "dbo.usp_SearchExpense",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var total = await multi.ReadFirstAsync<int>();
                var items = await multi.ReadAsync<Expense>();
                return (items, total);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search expenses for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<(string CategoryName, decimal Total)>> GetTotalsByCategoryAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var data = await connection.QueryAsync(
                    "dbo.usp_TotalExpensesByCategory",
                    new { UserId = userId, FromDate = fromDate, ToDate = toDate },
                    commandType: CommandType.StoredProcedure);
                return data.Select(r => ((string)r.CategoryName, (decimal)r.Total));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get totals by category for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<(DateTime Date, decimal Total)>> GetDailyTotalsAsync(int userId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var data = await connection.QueryAsync(
                    "dbo.usp_GetDailyTotalExpenses",
                    new { UserId = userId, FromDate = fromDate, ToDate = toDate },
                    commandType: CommandType.StoredProcedure);
                return data.Select(r => ((DateTime)r.Date, (decimal)r.Total));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get daily totals for user {UserId}", userId);
                throw;
            }
        }

        public async Task<decimal> GetTotalForMonthAsync(int userId, DateTime monthStart, DateTime monthEnd)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                return await connection.ExecuteScalarAsync<decimal>(
                    "dbo.usp_GetTotalMonthExpenses",
                    new { UserId = userId, Start = monthStart, End = monthEnd },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total for month for user {UserId}", userId);
                throw;
            }
        }
    }
}


