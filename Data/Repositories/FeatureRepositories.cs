using System.Data;
using Dapper;
using ExpenseTracker.Models;

namespace ExpenseTracker.Data.Repositories;

public interface IBudgetRepository
{
    Task<IEnumerable<Budget>> GetAsync(int userId, int year, int month);
    Task UpsertAsync(Budget budget);
    Task<bool> DeleteAsync(int userId, int budgetId);
}

public class BudgetRepository(IDbConnectionFactory factory) : IBudgetRepository
{
    public async Task<IEnumerable<Budget>> GetAsync(int userId, int year, int month)
    {
        using var db = factory.CreateConnection();
        return await db.QueryAsync<Budget>("dbo.usp_GetBudgets", new { UserId = userId, Year = year, Month = month }, commandType: CommandType.StoredProcedure);
    }
    public async Task UpsertAsync(Budget value)
    {
        using var db = factory.CreateConnection();
        await db.ExecuteAsync("dbo.usp_UpsertBudget", new { value.UserId, value.CategoryId, value.Amount, value.Year, value.Month }, commandType: CommandType.StoredProcedure);
    }
    public async Task<bool> DeleteAsync(int userId, int budgetId)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("dbo.usp_DeleteBudget", new { UserId = userId, BudgetId = budgetId }, commandType: CommandType.StoredProcedure) > 0;
    }
}

public interface IRecurringExpenseRepository
{
    Task<IEnumerable<RecurringExpense>> GetAsync(int userId);
    Task<RecurringExpense?> GetByIdAsync(int userId, int id);
    Task<int> CreateAsync(RecurringExpense value);
    Task<bool> UpdateAsync(RecurringExpense value);
    Task<bool> DeleteAsync(int userId, int id);
    Task<IEnumerable<RecurringExpense>> GetDueAsync(int userId, DateTime asOf);
    Task<bool> AdvanceAsync(int userId, int id, DateTime expected, DateTime next);
}

public class RecurringExpenseRepository(IDbConnectionFactory factory) : IRecurringExpenseRepository
{
    public async Task<IEnumerable<RecurringExpense>> GetAsync(int userId)
    {
        using var db = factory.CreateConnection();
        return await db.QueryAsync<RecurringExpense>("dbo.usp_GetRecurringExpenses", new { UserId = userId }, commandType: CommandType.StoredProcedure);
    }
    public async Task<RecurringExpense?> GetByIdAsync(int userId, int id) =>
        (await GetAsync(userId)).FirstOrDefault(x => x.RecurringExpenseId == id);
    public async Task<int> CreateAsync(RecurringExpense v)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("dbo.usp_CreateRecurringExpense", new { v.UserId, v.CategoryId, v.Amount, v.Description, v.Frequency, v.StartDate, v.NextRunDate, v.EndDate }, commandType: CommandType.StoredProcedure);
    }
    public async Task<bool> UpdateAsync(RecurringExpense v)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("dbo.usp_UpdateRecurringExpense", new { v.RecurringExpenseId, v.UserId, v.CategoryId, v.Amount, v.Description, v.Frequency, v.StartDate, v.NextRunDate, v.EndDate }, commandType: CommandType.StoredProcedure) > 0;
    }
    public async Task<bool> DeleteAsync(int userId, int id)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("dbo.usp_DeleteRecurringExpense", new { RecurringExpenseId = id, UserId = userId }, commandType: CommandType.StoredProcedure) > 0;
    }
    public async Task<IEnumerable<RecurringExpense>> GetDueAsync(int userId, DateTime asOf)
    {
        using var db = factory.CreateConnection();
        return await db.QueryAsync<RecurringExpense>("dbo.usp_GetDueRecurringExpenses", new { UserId = userId, AsOfDate = asOf.Date }, commandType: CommandType.StoredProcedure);
    }
    public async Task<bool> AdvanceAsync(int userId, int id, DateTime expected, DateTime next)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("dbo.usp_AdvanceRecurringNextRun", new { RecurringExpenseId = id, UserId = userId, ExpectedNextRunDate = expected.Date, NewNextRunDate = next.Date }, commandType: CommandType.StoredProcedure) > 0;
    }
}

public interface IIncomeRepository
{
    Task<IEnumerable<Income>> SearchAsync(int userId, DateTime? from, DateTime? to);
    Task<Income?> GetAsync(int userId, int id);
    Task<int> CreateAsync(Income value);
    Task<bool> UpdateAsync(Income value);
    Task<bool> DeleteAsync(int userId, int id);
    Task<decimal> TotalAsync(int userId, DateTime from, DateTime to);
}

public class IncomeRepository(IDbConnectionFactory factory) : IIncomeRepository
{
    public async Task<IEnumerable<Income>> SearchAsync(int userId, DateTime? from, DateTime? to)
    {
        using var db = factory.CreateConnection();
        return await db.QueryAsync<Income>("dbo.usp_SearchIncome", new { UserId = userId, FromDate = from, ToDate = to }, commandType: CommandType.StoredProcedure);
    }
    public async Task<Income?> GetAsync(int userId, int id)
    {
        using var db = factory.CreateConnection();
        return await db.QueryFirstOrDefaultAsync<Income>("dbo.usp_GetIncomeById", new { UserId = userId, IncomeId = id }, commandType: CommandType.StoredProcedure);
    }
    public async Task<int> CreateAsync(Income v)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("dbo.usp_CreateIncome", new { v.UserId, v.Amount, v.Source, v.Description, v.IncomeDate }, commandType: CommandType.StoredProcedure);
    }
    public async Task<bool> UpdateAsync(Income v)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("dbo.usp_UpdateIncome", new { v.IncomeId, v.UserId, v.Amount, v.Source, v.Description, v.IncomeDate }, commandType: CommandType.StoredProcedure) > 0;
    }
    public async Task<bool> DeleteAsync(int userId, int id)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<int>("dbo.usp_DeleteIncome", new { UserId = userId, IncomeId = id }, commandType: CommandType.StoredProcedure) > 0;
    }
    public async Task<decimal> TotalAsync(int userId, DateTime from, DateTime to)
    {
        using var db = factory.CreateConnection();
        return await db.ExecuteScalarAsync<decimal>("dbo.usp_TotalIncome", new { UserId = userId, From = from, To = to }, commandType: CommandType.StoredProcedure);
    }
}

public interface IHouseholdRepository
{
    Task<Household?> GetAsync(int userId);
    Task<IEnumerable<HouseholdMember>> GetMembersAsync(int userId);
    Task<int> CreateAsync(int userId, string name);
    Task InviteAsync(int userId, string username);
    Task<bool> LeaveAsync(int userId);
}

public class HouseholdRepository(IDbConnectionFactory factory) : IHouseholdRepository
{
    public async Task<Household?> GetAsync(int userId) { using var db = factory.CreateConnection(); return await db.QueryFirstOrDefaultAsync<Household>("dbo.usp_GetUserHousehold", new { UserId = userId }, commandType: CommandType.StoredProcedure); }
    public async Task<IEnumerable<HouseholdMember>> GetMembersAsync(int userId) { using var db = factory.CreateConnection(); return await db.QueryAsync<HouseholdMember>("dbo.usp_GetHouseholdMembers", new { UserId = userId }, commandType: CommandType.StoredProcedure); }
    public async Task<int> CreateAsync(int userId, string name) { using var db = factory.CreateConnection(); return await db.ExecuteScalarAsync<int>("dbo.usp_CreateHousehold", new { UserId = userId, Name = name }, commandType: CommandType.StoredProcedure); }
    public async Task InviteAsync(int userId, string username) { using var db = factory.CreateConnection(); await db.ExecuteAsync("dbo.usp_InviteHouseholdMember", new { UserId = userId, Username = username }, commandType: CommandType.StoredProcedure); }
    public async Task<bool> LeaveAsync(int userId) { using var db = factory.CreateConnection(); return await db.ExecuteScalarAsync<int>("dbo.usp_LeaveHousehold", new { UserId = userId }, commandType: CommandType.StoredProcedure) > 0; }
}
