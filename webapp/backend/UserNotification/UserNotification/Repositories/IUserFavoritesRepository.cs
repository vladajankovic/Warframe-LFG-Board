using UserNotification.Commands;
using UserNotification.Database.Entities;

namespace UserNotification.Repositories
{
    public interface IUserFavoritesRepository
    {
        public Task<UserFavoriteEntity> Create(AddOrRemoveUserFavoritesCommand command);
        public Task<List<UserFavoriteEntity>> GetAll();
        public Task<List<UserFavoriteEntity>> GetAllFromUser(string userId);
        public Task<UserFavoriteEntity> Get(string userId, string itemName);
        public Task<bool> Delete(AddOrRemoveUserFavoritesCommand command);
    }
}
