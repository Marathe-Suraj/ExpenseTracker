using System.Threading.Tasks;
using ExpenseTracker.Web.Models;

namespace ExpenseTracker.Web.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<int> CreateAsync(User user);
    }
}


