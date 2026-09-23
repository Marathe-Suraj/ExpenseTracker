using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models;

public class Income
{
    public int IncomeId { get; set; }
    public int UserId { get; set; }
    [Range(typeof(decimal), "0.01", "1000000000")] public decimal Amount { get; set; }
    [Required, StringLength(200)] public string Source { get; set; } = string.Empty;
    [StringLength(500)] public string? Description { get; set; }
    [Required, DataType(DataType.Date)] public DateTime IncomeDate { get; set; } = DateTime.Today;
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; } = true;
}
