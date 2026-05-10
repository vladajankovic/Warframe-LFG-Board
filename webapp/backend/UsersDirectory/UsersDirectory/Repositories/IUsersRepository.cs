using UsersDirectory.Commands;
using UsersDirectory.Database.Entities;

namespace UsersDirectory.Repositories
{
    public interface IUsersRepository
    {
        public Task<UserEntity> CreateUser(AddUserCommand command);
        public Task<List<UserEntity>> GetAllUsers();
        public Task<UserEntity> GetUser(string email);
        public Task<UserEntity> UpdateUser(AddUserCommand command);
        public Task<bool> DeleteUser(string email);
    }
}
