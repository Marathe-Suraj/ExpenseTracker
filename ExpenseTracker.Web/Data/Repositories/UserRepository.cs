using System.Threading.Tasks;
using System.Data;
using Dapper;
using ExpenseTracker.Web.Models;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Web.Data.Repositories
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
    }
}


