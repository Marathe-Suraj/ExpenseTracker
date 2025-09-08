using System.Collections.Generic;
using System.Threading.Tasks;
using ExpenseTracker.Models;

namespace ExpenseTracker.Data.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllForUserAsync(int userId);
        Task<Category?> GetByIdAsync(int userId, int categoryId);
        Task<int> CreateAsync(Category category);
        Task<bool> UpdateAsync(Category category);
        Task<bool> DeleteAsync(int userId, int categoryId);
        Task<Category?> ToggleStatusAsync(int userId, int categoryId);
    }
}


