using System;

namespace ExpenseTracker.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string Currency { get; set; } = "INR";
        public string DateFormat { get; set; } = "dd/MM/yyyy";
        public string Language { get; set; } = "English";
        public bool EmailNotifications { get; set; } = true;
        public bool DarkMode { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}


