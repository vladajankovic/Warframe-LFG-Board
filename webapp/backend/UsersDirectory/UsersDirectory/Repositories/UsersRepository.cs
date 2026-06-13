using Microsoft.EntityFrameworkCore;
using UsersDirectory.Commands;
using UsersDirectory.Database;
using UsersDirectory.Database.Entities;
using UsersDirectory.Enums;

namespace UsersDirectory.Repositories
{
    public class UsersRepository : IUsersRepository
    {
        private readonly UsersDbContext _dbContext;
        public UsersRepository(
            UsersDbContext dbContext
        )
        {
            _dbContext = dbContext;
        }
        public async Task<UserEntity> CreateUser(AddUserCommand command)
        {
            var userEntity = new UserEntity()
            {
                Email = command.Email,
                Password = command.Password,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                VerificationCode = Guid.NewGuid().ToString("N")
            };

            await _dbContext.AddAsync(userEntity);

            await _dbContext.SaveChangesAsync();

            return userEntity;
        }

        public async Task<bool> DeleteUser(string email)
        {
            var userEntity = await GetUser(email);
            
            if (userEntity == null)
            {
                return false;
            }

            _dbContext.Users.Remove(userEntity);

            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<List<UserEntity>> GetAllUsers()
        {
            return await _dbContext.Users.ToListAsync();
        }

        public async Task<UserEntity> GetUser(string email)
        {
            return await _dbContext.Users.Where(x => x.Email == email).FirstOrDefaultAsync();
        }

        public async Task<UserEntity> UpdateUser(UpdateUserCommand command)
        {
            var userEntity = await GetUser(command.Email);

            userEntity.Password = command.Password;

            if (userEntity.Role != "Admin")
            {
                userEntity.Role = command.Role;
            }

            userEntity.InGameName = command.InGameName;

            userEntity.IsVerified = command.IsVerified;

            userEntity.LastUpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return userEntity;
        }

        public async Task<UserEntity> UpdateSubscription(string email, SubscriptionStatus status, DateTime? expiryDate)
        {
            var userEntity = await GetUser(email);

            userEntity.SubscriptionStatus = status;
            userEntity.SubscriptionExpiryDate = expiryDate;
            userEntity.LastUpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return userEntity;
        }
    }
}
