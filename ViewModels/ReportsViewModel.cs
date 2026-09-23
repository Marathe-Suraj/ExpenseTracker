namespace ExpenseTracker.ViewModels;

public class ReportsViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal Net => TotalIncome - TotalExpense;
    public List<CategoryTotal> CategoryTotals { get; set; } = [];
    public List<DailyTotalPoint> DailyTotals { get; set; } = [];
}
