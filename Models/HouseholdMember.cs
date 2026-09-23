namespace ExpenseTracker.Models;

public class HouseholdMember
{
    public int HouseholdMemberId { get; set; }
    public int HouseholdId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "Member";
    public DateTime JoinedDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
}
