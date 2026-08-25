using UsersDirectory.Commands;
using UsersDirectory.Models;

namespace UsersDirectory.Services
{
    public interface IUsersService
    {
        public Task<User> CreateUser(AddUserCommand command);
        public Task<List<User>> GetAllUsers();
        public Task<User> GetUser(string email);
        public Task<bool> DeleteUser(string email);
        public Task<LoginResponse> LoginUser(LoginCommand command);
        public Task<User> UpdateUser(UpdateUserCommand command);
        public Task<User> ChangePassword(ChangePasswordCommand command);
        public Task<User> UpdateInGameName(UpdateInGameNameCommand command);
        public Task<bool> DeleteAccount(DeleteUserCommand command);
        public Task<SubscriptionInfo> ActivateSubscription(string email);
        public Task<SubscriptionInfo> CancelSubscription(string email);
        public Task<SubscriptionInfo> GetSubscription(string email);
    }
}
