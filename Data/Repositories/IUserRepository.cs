using System.Threading.Tasks;
using ExpenseTracker.Models;

namespace ExpenseTracker.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<int> CreateAsync(User user);
    }
}


