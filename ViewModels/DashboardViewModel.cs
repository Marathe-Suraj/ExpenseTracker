using System;
using System.Collections.Generic;

namespace ExpenseTracker.ViewModels
{
    public class CategoryTotal
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class DashboardViewModel
    {
        public decimal TotalToday { get; set; }
        public decimal TotalThisMonth { get; set; }
        public decimal TotalThisYear { get; set; }

        public List<CategoryTotal> DailyCategoryTotals { get; set; } = new();
        public List<CategoryTotal> MonthlyCategoryTotals { get; set; } = new();
        public List<CategoryTotal> YearlyCategoryTotals { get; set; } = new();
    }
}


