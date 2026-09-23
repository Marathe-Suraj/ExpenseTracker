using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Models;

public class Household
{
    public int HouseholdId { get; set; }
    [Required, StringLength(200)] public string Name { get; set; } = string.Empty;
    public int OwnerUserId { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; } = true;
}
