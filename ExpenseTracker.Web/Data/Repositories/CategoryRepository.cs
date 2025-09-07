using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Dapper;
using ExpenseTracker.Web.Models;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Web.Data.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<CategoryRepository> _logger;

        public CategoryRepository(IDbConnectionFactory connectionFactory, ILogger<CategoryRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Category>> GetAllForUserAsync(int userId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                return await connection.QueryAsync<Category>(
                    "dbo.usp_GetCategories",
                    new { UserId = userId },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get categories for user {UserId}", userId);
                throw;
            }
        }

        public async Task<Category?> GetByIdAsync(int userId, int categoryId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                return await connection.QueryFirstOrDefaultAsync<Category>(
                    "dbo.usp_GetCategoryById",
                    new { CategoryId = categoryId, UserId = userId },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get category {CategoryId} for user {UserId}", categoryId, userId);
                throw;
            }
        }

        public async Task<int> CreateAsync(Category category)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var id = await connection.ExecuteScalarAsync<int>(
                    "dbo.usp_CreateCategory",
                    new { 
                        Name = category.Name, 
                        CreatedDate = category.CreatedDate, 
                        UserId = category.UserId, // Add UserId for user-category mapping
                        IsActive = category.IsActive 
                    },
                    commandType: CommandType.StoredProcedure);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create category {Name}", category.Name);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var rows = await connection.ExecuteScalarAsync<int>(
                    "dbo.usp_UpdateCategory",
                    new { 
                        CategoryId = category.CategoryId, 
                        Name = category.Name, 
                        UserId = category.UserId, // Add UserId for user-category mapping
                        IsActive = category.IsActive 
                    },
                    commandType: CommandType.StoredProcedure);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update category {CategoryId}", category.CategoryId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int userId, int categoryId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var rows = await connection.ExecuteScalarAsync<int>(
                    "dbo.usp_DeleteCategory",
                    new { CategoryId = categoryId, UserId = userId },
                    commandType: CommandType.StoredProcedure);
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete category {CategoryId} for user {UserId}", categoryId, userId);
                throw;
            }
        }

        public async Task<Category?> ToggleStatusAsync(int userId, int categoryId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var category = await connection.QueryFirstOrDefaultAsync<Category>(
                    "dbo.usp_ToggleCategoryStatus",
                    new { CategoryId = categoryId, UserId = userId },
                    commandType: CommandType.StoredProcedure);
                return category;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle category status {CategoryId} for user {UserId}", categoryId, userId);
                throw;
            }
        }
    }
}


