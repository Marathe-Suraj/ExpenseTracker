using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models;

public class Budget
{
    public int BudgetId { get; set; }
    public int UserId { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(typeof(decimal), "0.01", "1000000000")] public decimal Amount { get; set; }
    [Range(2000, 2100)] public int Year { get; set; }
    [Range(1, 12)] public int Month { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string CategoryName { get; set; } = string.Empty;
    public decimal SpentAmount { get; set; }
}
