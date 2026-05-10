using Microsoft.EntityFrameworkCore;
using UsersDirectory.Database;
using UsersDirectory.Database.Entities;

namespace UsersDirectory.Seed
{
    public class DbSeed
    {
        private readonly UsersDbContext _dbContext;
        private readonly ILogger<DbSeed> _logger;

        public DbSeed(UsersDbContext dbContext, ILogger<DbSeed> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task SeedUsers()
        {
            try
            {
                // Check if users already exist in database
                var existingUsersCount = await _dbContext.Users.CountAsync();
                if (existingUsersCount > 0)
                {
                    _logger.LogInformation("Database already contains {Count} users. Skipping seed.", existingUsersCount);
                    return;
                }

                var userEntity = new UserEntity()
                {
                    Email = "admin@admin.com",
                    Password = "admin123"
                };

                await _dbContext.Users.AddAsync(userEntity);
                await _dbContext.SaveChangesAsync();
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding users.");
                throw;
            }
        }
    }
}
