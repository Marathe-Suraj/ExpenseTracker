using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Data.Repositories;
using ExpenseTracker.ViewModels;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardAsync(int userId);
    }

    public class DashboardService : IDashboardService
    {
        private readonly IExpenseRepository _expenseRepository;
        private readonly ILogger<DashboardService> _logger;
        private readonly IBudgetRepository _budgetRepository;
        private readonly IIncomeRepository _incomeRepository;

        public DashboardService(IExpenseRepository expenseRepository, IBudgetRepository budgetRepository, IIncomeRepository incomeRepository, ILogger<DashboardService> logger)
        {
            _expenseRepository = expenseRepository;
            _logger = logger;
            _budgetRepository = budgetRepository;
            _incomeRepository = incomeRepository;
        }

        public async Task<DashboardViewModel> GetDashboardAsync(int userId)
        {
            try
            {
                var today = DateTime.Today;
                var monthStart = new DateTime(today.Year, today.Month, 1);
                var yearStart = new DateTime(today.Year, 1, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var yearEnd = new DateTime(today.Year, 12, 31);
                var lastMonthStart = monthStart.AddMonths(-1);
                var lastMonthEnd = monthStart.AddDays(-1);

                var dailyCatTask = _expenseRepository.GetTotalsByCategoryAsync(userId, today, today);
                var monthlyCatTask = _expenseRepository.GetTotalsByCategoryAsync(userId, monthStart, monthEnd);
                var yearlyCatTask = _expenseRepository.GetTotalsByCategoryAsync(userId, yearStart, yearEnd);
                var lastMonthTotalTask = _expenseRepository.GetTotalForMonthAsync(userId, lastMonthStart, lastMonthEnd);
                var dailyTrendTask = _expenseRepository.GetDailyTotalsAsync(userId, monthStart, monthEnd);
                var budgetsTask = _budgetRepository.GetAsync(userId, today.Year, today.Month);
                var incomeTask = _incomeRepository.TotalAsync(userId, monthStart, monthEnd);

                await Task.WhenAll(dailyCatTask, monthlyCatTask, yearlyCatTask, lastMonthTotalTask, dailyTrendTask, budgetsTask, incomeTask);

                var dailyCat = await dailyCatTask;
                var monthlyCat = await monthlyCatTask;
                var yearlyCat = await yearlyCatTask;
                var lastMonthTotal = await lastMonthTotalTask;
                var dailyTrendRaw = (await dailyTrendTask).ToDictionary(x => x.Date.Date, x => x.Total);

                var totalToday = dailyCat.Sum(x => x.Total);
                var totalMonth = monthlyCat.Sum(x => x.Total);
                var totalYear = yearlyCat.Sum(x => x.Total);

                decimal momPercent = 0;
                if (lastMonthTotal > 0)
                    momPercent = Math.Round((totalMonth - lastMonthTotal) / lastMonthTotal * 100m, 1);
                else if (totalMonth > 0)
                    momPercent = 100m;

                var dailyTrend = new List<DailyTotalPoint>();
                for (var d = monthStart; d <= (today < monthEnd ? today : monthEnd); d = d.AddDays(1))
                {
                    dailyTrend.Add(new DailyTotalPoint
                    {
                        DateLabel = d.ToString("dd MMM"),
                        DateIso = d.ToString("yyyy-MM-dd"),
                        TotalAmount = dailyTrendRaw.TryGetValue(d, out var t) ? t : 0m
                    });
                }

                return new DashboardViewModel
                {
                    TotalToday = totalToday,
                    TotalThisMonth = totalMonth,
                    TotalThisYear = totalYear,
                    TotalLastMonth = lastMonthTotal,
                    MonthOverMonthChangePercent = momPercent,
                    HasLastMonthData = lastMonthTotal > 0 || totalMonth > 0,
                    TotalIncomeThisMonth = await incomeTask,
                    DailyCategoryTotals = dailyCat.Select(x => new CategoryTotal { CategoryName = x.CategoryName, TotalAmount = x.Total }).ToList(),
                    MonthlyCategoryTotals = monthlyCat.Select(x => new CategoryTotal { CategoryName = x.CategoryName, TotalAmount = x.Total }).ToList(),
                    YearlyCategoryTotals = yearlyCat.Select(x => new CategoryTotal { CategoryName = x.CategoryName, TotalAmount = x.Total }).ToList(),
                    DailyTrend = dailyTrend,
                    BudgetProgress = (await budgetsTask).Select(x => new BudgetProgressItem { CategoryId = x.CategoryId, CategoryName = x.CategoryName, BudgetAmount = x.Amount, SpentAmount = x.SpentAmount }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get dashboard for user {UserId}", userId);
                throw;
            }
        }
    }
}
