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
        private readonly IPasswordHasher<AddUserCommand> _passwordHasher;

        public DbSeed(
            UsersDbContext dbContext, 
            ILogger<DbSeed> logger, 
            IPasswordHasher<AddUserCommand> passwordHasher
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

                    newUser.Password = _passwordHasher.HashPassword(newUser, newUser.Password);

                    var userEntity = new UserEntity()
                    {
                        Email = newUser.Email,
                        Password = newUser.Password,
                        Role = "Admin"
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
