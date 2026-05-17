using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UsersDirectory.Commands;
using UsersDirectory.Database;
using UsersDirectory.Database.Entities;

namespace UsersDirectory.Seed
{
    public class DbSeed
    {
        private readonly UsersDbContext _dbContext;
        private readonly ILogger<DbSeed> _logger;
        private readonly IPasswordHasher<string> _passwordHasher;

        public DbSeed(
            UsersDbContext dbContext, 
            ILogger<DbSeed> logger, 
            IPasswordHasher<string> passwordHasher
        )
        {
            _dbContext = dbContext;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        public async Task SeedUsers()
        {
            try
            {
                const string adminEmail = "admin@admin.com";
                var adminEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

                if (adminEntity == null)
                {
                    var newUser = new AddUserCommand()
                    {
                        Email = adminEmail,
                        Password = "admin123"
                    };

                    newUser.Password = _passwordHasher.HashPassword(string.Empty, newUser.Password);

                    var userEntity = new UserEntity()
                    {
                        Email = newUser.Email,
                        Password = newUser.Password,
                        Role = "Admin",
                        CreatedAt = DateTime.UtcNow,
                        LastUpdatedAt = DateTime.UtcNow,
                        VerificationCode = Guid.NewGuid().ToString("N"),
                        IsVerified = true
                    };

                    await _dbContext.Users.AddAsync(userEntity);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Admin user seeded.");
                }
                else if (adminEntity.Role != "Admin")
                {
                    adminEntity.Role = "Admin";
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Admin user role updated to Admin.");
                }
                else
                {
                    _logger.LogInformation("Admin user already exists with correct role. Skipping seed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding users.");
                throw;
            }
        }
    }
}
