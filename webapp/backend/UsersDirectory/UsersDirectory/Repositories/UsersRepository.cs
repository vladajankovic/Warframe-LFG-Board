using Microsoft.EntityFrameworkCore;
using UsersDirectory.Commands;
using UsersDirectory.Database;
using UsersDirectory.Database.Entities;

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
                Password = command.Password
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

        public async Task<UserEntity> UpdateUser(AddUserCommand command)
        {
            var userEntity = await GetUser(command.Email);

            userEntity.Password = command.Password;

            await _dbContext.SaveChangesAsync();

            return userEntity;
        }
    }
}
