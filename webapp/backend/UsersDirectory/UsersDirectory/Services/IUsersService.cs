using UsersDirectory.Commands;
using UsersDirectory.Models;

namespace UsersDirectory.Services
{
    public interface IUsersService
    {
        public Task<User> CreateUser(AddUserCommand command);
        public Task<List<User>> GetAllUsers();
        public Task<User> GetUser(string email);
        public Task<User> UpdateUser(AddUserCommand command);
        public Task<bool> DeleteUser(string email);
        public Task<bool> LoginUser(AddUserCommand command);
    }
}
