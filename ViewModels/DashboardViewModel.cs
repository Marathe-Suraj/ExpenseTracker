using System;
using System.Collections.Generic;

namespace ExpenseTracker.ViewModels
{
    public class CategoryTotal
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal? BudgetAmount { get; set; }
        public decimal? BudgetPercent => BudgetAmount.HasValue && BudgetAmount.Value > 0
            ? Math.Round(TotalAmount / BudgetAmount.Value * 100m, 1)
            : null;
    }

    public class DailyTotalPoint
    {
        public string DateLabel { get; set; } = string.Empty;
        public string DateIso { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class BudgetProgressItem
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal BudgetAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal PercentUsed => BudgetAmount > 0
            ? Math.Round(SpentAmount / BudgetAmount * 100m, 1)
            : 0;
        public bool IsOverBudget => SpentAmount > BudgetAmount;
    }

    public class DashboardViewModel
    {
        public decimal TotalToday { get; set; }
        public decimal TotalThisMonth { get; set; }
        public decimal TotalThisYear { get; set; }
        public decimal TotalLastMonth { get; set; }
        public decimal MonthOverMonthChangePercent { get; set; }
        public bool HasLastMonthData { get; set; }

        public decimal TotalIncomeThisMonth { get; set; }
        public decimal NetThisMonth => TotalIncomeThisMonth - TotalThisMonth;

        public List<CategoryTotal> DailyCategoryTotals { get; set; } = new();
        public List<CategoryTotal> MonthlyCategoryTotals { get; set; } = new();
        public List<CategoryTotal> YearlyCategoryTotals { get; set; } = new();
        public List<DailyTotalPoint> DailyTrend { get; set; } = new();
        public List<BudgetProgressItem> BudgetProgress { get; set; } = new();
    }
}
