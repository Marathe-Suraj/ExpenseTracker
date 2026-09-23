using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models;

public class RecurringExpense
{
    public int RecurringExpenseId { get; set; }
    public int UserId { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(typeof(decimal), "0.01", "1000000000")] public decimal Amount { get; set; }
    [StringLength(500)] public string? Description { get; set; }
    [Required, RegularExpression("Monthly|Weekly|Yearly")] public string Frequency { get; set; } = "Monthly";
    [DataType(DataType.Date)] public DateTime StartDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime NextRunDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date)] public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
