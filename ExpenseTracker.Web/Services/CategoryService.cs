using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExpenseTracker.Web.Data.Repositories;
using ExpenseTracker.Web.Models;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Web.Services
{
    public interface ICategoryService
    {
        Task<IEnumerable<Category>> GetAllAsync(int userId);
        Task<Category?> GetAsync(int userId, int id);
        Task<int> CreateAsync(Category category);
        Task<bool> UpdateAsync(Category category);
        Task<bool> DeleteAsync(int userId, int id);
    }

    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(ICategoryRepository repository, ILogger<CategoryService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Category>> GetAllAsync(int userId)
        {
            try
            {
                return await _repository.GetAllForUserAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get categories for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Category?> GetAsync(int userId, int id)
        {
            try
            {
                return await _repository.GetByIdAsync(userId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get category {Id} for user {UserId}", id, userId);
                throw;
            }
        }

        public async Task<int> CreateAsync(Category category)
        {
            try
            {
                category.IsActive = true;
                // Ensure UserId is set for the category
                if (category.UserId <= 0)
                {
                    throw new ArgumentException("UserId must be provided for category creation");
                }
                return await _repository.CreateAsync(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create category");
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            try
            {
                // Ensure UserId is set for the category
                if (category.UserId <= 0)
                {
                    throw new ArgumentException("UserId must be provided for category update");
                }
                category.IsActive = true;
                return await _repository.UpdateAsync(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update category {Id} for user {UserId}", category.CategoryId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int userId, int id)
        {
            try
            {
                return await _repository.DeleteAsync(userId, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete category {Id} for user {UserId}", id, userId);
                throw;
            }
        }
    }
}


