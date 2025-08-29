using System;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Web.Data.Repositories;
using ExpenseTracker.Web.ViewModels;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Web.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync(int userId);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(IExpenseRepository expenseRepository, ILogger<DashboardService> logger)
        {
            _expenseRepository = expenseRepository;
            _logger = logger;
        }

        public async Task<DashboardViewModel> GetDashboardAsync(int userId)
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var yearEnd = new DateTime(today.Year, 12, 31);

                var dailyCat = await _expenseRepository.GetTotalsByCategoryAsync(userId, today, today);
                var monthlyCat = await _expenseRepository.GetTotalsByCategoryAsync(userId, monthStart, monthEnd);
                var yearlyCat = await _expenseRepository.GetTotalsByCategoryAsync(userId, yearStart, yearEnd);

                var totalToday = dailyCat.Sum(x => x.Total);
                var totalMonth = monthlyCat.Sum(x => x.Total);
                var totalYear = yearlyCat.Sum(x => x.Total);

                var vm = new DashboardViewModel
                {
                    TotalToday = totalToday,
                    TotalThisMonth = totalMonth,
                    TotalThisYear = totalYear,
                    DailyCategoryTotals = dailyCat.Select(x => new CategoryTotal { CategoryName = x.CategoryName, TotalAmount = x.Total }).ToList(),
                    MonthlyCategoryTotals = monthlyCat.Select(x => new CategoryTotal { CategoryName = x.CategoryName, TotalAmount = x.Total }).ToList(),
                    YearlyCategoryTotals = yearlyCat.Select(x => new CategoryTotal { CategoryName = x.CategoryName, TotalAmount = x.Total }).ToList()
                };
                return vm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dashboard for user {UserId}", userId);
                throw;
            }
        }
    }
}


