using UserNotification.Commands;
using UserNotification.Database.Entities;
using UserNotification.Models;

namespace UserNotification.Services
{
    public interface IUserFavoritesService
    {
        public Task<UserFavoriteEntity> Create(AddOrRemoveUserFavoritesCommand command);
        public Task<List<UserFavorites>> GetAll();
        public Task<UserFavorites> GetAllFromUser(string userId);
        public Task<bool> Delete(AddOrRemoveUserFavoritesCommand command);
    }
}
