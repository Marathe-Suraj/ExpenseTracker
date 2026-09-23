using ExpenseTracker.Data.Repositories;
using ExpenseTracker.Models;
using ExpenseTracker.ViewModels;

namespace ExpenseTracker.Services;

public interface IBudgetService
{
    Task<IEnumerable<Budget>> GetAsync(int userId, int year, int month);
    Task UpsertAsync(Budget budget);
    Task<bool> DeleteAsync(int userId, int id);
}
public class BudgetService(IBudgetRepository repository) : IBudgetService
{
    public Task<IEnumerable<Budget>> GetAsync(int userId, int year, int month) => repository.GetAsync(userId, year, month);
    public Task UpsertAsync(Budget budget) => repository.UpsertAsync(budget);
    public Task<bool> DeleteAsync(int userId, int id) => repository.DeleteAsync(userId, id);
}

public interface IIncomeService
{
    Task<IEnumerable<Income>> SearchAsync(int userId, DateTime? from, DateTime? to);
    Task<Income?> GetAsync(int userId, int id);
    Task<int> CreateAsync(Income value);
    Task<bool> UpdateAsync(Income value);
    Task<bool> DeleteAsync(int userId, int id);
    Task<decimal> TotalAsync(int userId, DateTime from, DateTime to);
}
public class IncomeService(IIncomeRepository repository) : IIncomeService
{
    public Task<IEnumerable<Income>> SearchAsync(int userId, DateTime? from, DateTime? to) => repository.SearchAsync(userId, from, to);
    public Task<Income?> GetAsync(int userId, int id) => repository.GetAsync(userId, id);
    public Task<int> CreateAsync(Income value) => repository.CreateAsync(value);
    public Task<bool> UpdateAsync(Income value) => repository.UpdateAsync(value);
    public Task<bool> DeleteAsync(int userId, int id) => repository.DeleteAsync(userId, id);
    public Task<decimal> TotalAsync(int userId, DateTime from, DateTime to) => repository.TotalAsync(userId, from, to);
}

public interface IRecurringExpenseService
{
    Task<IEnumerable<RecurringExpense>> GetAsync(int userId);
    Task<RecurringExpense?> GetByIdAsync(int userId, int id);
    Task<int> CreateAsync(RecurringExpense value);
    Task<bool> UpdateAsync(RecurringExpense value);
    Task<bool> DeleteAsync(int userId, int id);
}
public class RecurringExpenseService(IRecurringExpenseRepository repository) : IRecurringExpenseService
{
    public Task<IEnumerable<RecurringExpense>> GetAsync(int userId) => repository.GetAsync(userId);
    public Task<RecurringExpense?> GetByIdAsync(int userId, int id) => repository.GetByIdAsync(userId, id);
    public Task<int> CreateAsync(RecurringExpense value) => repository.CreateAsync(value);
    public Task<bool> UpdateAsync(RecurringExpense value) => repository.UpdateAsync(value);
    public Task<bool> DeleteAsync(int userId, int id) => repository.DeleteAsync(userId, id);
}

public interface IRecurringExpenseGenerator { Task GenerateAsync(int userId); }
public class RecurringExpenseGenerator(IRecurringExpenseRepository recurring, IExpenseRepository expenses, ILogger<RecurringExpenseGenerator> logger) : IRecurringExpenseGenerator
{
    public async Task GenerateAsync(int userId)
    {
        // Catch up multiple missed periods safely (bounded).
        for (var safety = 0; safety < 120; safety++)
        {
            var dueItems = (await recurring.GetDueAsync(userId, DateTime.Today)).ToList();
            if (dueItems.Count == 0) break;

            var progressed = false;
            foreach (var item in dueItems)
            {
                var expected = item.NextRunDate.Date;
                var next = item.Frequency switch
                {
                    "Weekly" => expected.AddDays(7),
                    "Yearly" => expected.AddYears(1),
                    _ => expected.AddMonths(1)
                };

                try
                {
                    // Create first, then advance with compare-and-swap to avoid lost expenses.
                    await expenses.CreateAsync(new Expense
                    {
                        UserId = userId,
                        CategoryId = item.CategoryId,
                        Amount = item.Amount,
                        Description = string.IsNullOrWhiteSpace(item.Description)
                            ? $"Recurring ({item.Frequency})"
                            : item.Description,
                        ExpenseDate = expected,
                        CreatedDate = DateTime.UtcNow,
                        IsActive = true
                    });

                    if (!await recurring.AdvanceAsync(userId, item.RecurringExpenseId, expected, next))
                    {
                        // Another request already advanced; leave the created expense (rare duplicate possible under race).
                        logger.LogWarning("Recurring advance raced for {RecurringExpenseId}", item.RecurringExpenseId);
                    }
                    progressed = true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed generating recurring expense {RecurringExpenseId}", item.RecurringExpenseId);
                }
            }

            if (!progressed) break;
        }
    }
}

public interface IHouseholdService
{
    Task<Household?> GetAsync(int userId);
    Task<IEnumerable<HouseholdMember>> GetMembersAsync(int userId);
    Task<int> CreateAsync(int userId, string name);
    Task InviteAsync(int userId, string username);
    Task<bool> LeaveAsync(int userId);
}
public class HouseholdService(IHouseholdRepository repository) : IHouseholdService
{
    public Task<Household?> GetAsync(int userId) => repository.GetAsync(userId);
    public Task<IEnumerable<HouseholdMember>> GetMembersAsync(int userId) => repository.GetMembersAsync(userId);
    public Task<int> CreateAsync(int userId, string name) => repository.CreateAsync(userId, name);
    public Task InviteAsync(int userId, string username) => repository.InviteAsync(userId, username);
    public Task<bool> LeaveAsync(int userId) => repository.LeaveAsync(userId);
}

public interface IReportsService { Task<ReportsViewModel> GetAsync(int userId, DateTime from, DateTime to); }
public class ReportsService(IExpenseRepository expenses, IIncomeRepository incomes) : IReportsService
{
    public async Task<ReportsViewModel> GetAsync(int userId, DateTime from, DateTime to)
    {
        var categoriesTask = expenses.GetTotalsByCategoryAsync(userId, from, to);
        var dailyTask = expenses.GetDailyTotalsAsync(userId, from, to);
        var incomeTask = incomes.TotalAsync(userId, from, to);
        await Task.WhenAll(categoriesTask, dailyTask, incomeTask);
        var categories = (await categoriesTask).ToList();
        return new ReportsViewModel
        {
            FromDate = from, ToDate = to, TotalExpense = categories.Sum(x => x.Total),
            TotalIncome = await incomeTask,
            CategoryTotals = categories.Select(x => new CategoryTotal { CategoryName = x.CategoryName, TotalAmount = x.Total }).ToList(),
            DailyTotals = (await dailyTask).Select(x => new DailyTotalPoint { DateIso = x.Date.ToString("yyyy-MM-dd"), DateLabel = x.Date.ToString("dd MMM"), TotalAmount = x.Total }).ToList()
        };
    }
}
