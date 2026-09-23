using System.Threading.Tasks;
using ExpenseTracker.Models;

namespace ExpenseTracker.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(int userId);
        Task<int> CreateAsync(User user);
        Task<bool> UpdateProfileAsync(int userId, string? email, string? fullName);
        Task<bool> UpdateSettingsAsync(int userId, string currency, string dateFormat, string language, bool emailNotifications, bool darkMode);
        Task<bool> ChangePasswordAsync(int userId, string passwordHash);
    }
}


