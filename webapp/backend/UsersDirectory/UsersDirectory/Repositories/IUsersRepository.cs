using UsersDirectory.Commands;
using UsersDirectory.Database.Entities;
using UsersDirectory.Enums;

namespace UsersDirectory.Repositories
{
    public interface IUsersRepository
    {
        public Task<UserEntity> CreateUser(AddUserCommand command);
        public Task<List<UserEntity>> GetAllUsers();
        public Task<UserEntity> GetUser(string email);
        public Task<UserEntity> UpdateUser(UpdateUserCommand command);
        public Task<bool> DeleteUser(string email);
        public Task<UserEntity> UpdateSubscription(string email, SubscriptionStatus status, DateTime? expiryDate);
    }
}
