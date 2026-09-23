using System.Threading.Tasks;
using System.Data;
using Dapper;
using ExpenseTracker.Models;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Data.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(IDbConnectionFactory connectionFactory, ILogger<UserRepository> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                return await connection.QueryFirstOrDefaultAsync<User>(
                    "dbo.usp_GetUser",
                    new { Username = username },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user by username {Username}", username);
                throw;
            }
        }

        public async Task<int> CreateAsync(User user)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                var id = await connection.ExecuteScalarAsync<int>(
                    "dbo.usp_CreateUser",
                    new { user.Username, user.PasswordHash, user.CreatedDate },
                    commandType: CommandType.StoredProcedure);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user {Username}", user.Username);
                throw;
            }
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<User>("dbo.usp_GetUserById", new { UserId = userId }, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UpdateProfileAsync(int userId, string? email, string? fullName)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("dbo.usp_UpdateUserProfile", new { UserId = userId, Email = email, FullName = fullName }, commandType: CommandType.StoredProcedure) > 0;
        }

        public async Task<bool> UpdateSettingsAsync(int userId, string currency, string dateFormat, string language, bool emailNotifications, bool darkMode)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("dbo.usp_UpdateUserSettings", new { UserId = userId, Currency = currency, DateFormat = dateFormat, Language = language, EmailNotifications = emailNotifications, DarkMode = darkMode }, commandType: CommandType.StoredProcedure) > 0;
        }

        public async Task<bool> ChangePasswordAsync(int userId, string passwordHash)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<int>("dbo.usp_ChangePassword", new { UserId = userId, PasswordHash = passwordHash }, commandType: CommandType.StoredProcedure) > 0;
        }
    }
}


