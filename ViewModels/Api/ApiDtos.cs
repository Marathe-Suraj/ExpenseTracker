namespace ExpenseTracker.ViewModels.Api
{
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class ApiError
    {
        public string Error { get; set; } = string.Empty;
    }

    public class ProfileDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime JoinDate { get; set; }
    }

    public class SettingsDto
    {
        public bool EmailNotifications { get; set; }
        public bool DarkMode { get; set; }
        public string Currency { get; set; } = "INR";
        public string DateFormat { get; set; } = "dd/MM/yyyy";
        public string Language { get; set; } = "English";
    }
}
