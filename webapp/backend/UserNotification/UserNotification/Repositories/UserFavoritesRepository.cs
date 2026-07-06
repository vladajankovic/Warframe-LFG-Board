using Microsoft.EntityFrameworkCore;
using UserNotification.Commands;
using UserNotification.Database;
using UserNotification.Database.Entities;

namespace UserNotification.Repositories
{
    public class UserFavoritesRepository : IUserFavoritesRepository
    {
        private readonly NotificationsDbContext _dbContext;

        public UserFavoritesRepository(NotificationsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<UserFavoriteEntity> Create(AddOrRemoveUserFavoritesCommand command)
        {
            var newEntity = new UserFavoriteEntity()
            {
                UserId = command.UserId,
                ItemName = command.ItemName
            };

            await _dbContext.Favorites.AddAsync(newEntity);
            await _dbContext.SaveChangesAsync();

            return newEntity;
        }

        public async Task<bool> Delete(AddOrRemoveUserFavoritesCommand command)
        {
            var result = await Get(command.UserId, command.ItemName);

            if (result == null)
            {
                return false;
            }

            _dbContext.Favorites.Remove(result);
            
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<int> DeleteAllFromUser(string userId)
        {
            var favorites = await this.GetAllFromUser(userId);

            _dbContext.Favorites.RemoveRange(favorites);

            await _dbContext.SaveChangesAsync();

            return favorites.Count;
        }

        public async Task<UserFavoriteEntity> Get(string userId, string itemName)
        {
            return await _dbContext.Favorites.Where(x => x.UserId.Equals(userId) && x.ItemName.Equals(itemName)).FirstOrDefaultAsync();
        }

        public async Task<List<UserFavoriteEntity>> GetAll()
        {
            return await _dbContext.Favorites.ToListAsync();
        }

        public async Task<List<UserFavoriteEntity>> GetAllFromUser(string userId)
        {
            return await _dbContext.Favorites.Where(x => x.UserId.Equals(userId)).ToListAsync();
        }

        public async Task<List<string>> GetSubscribedUserIds(string itemName)
        {
            return await _dbContext.Favorites.Where(x => x.ItemName.Equals(itemName)).Select(x => x.UserId)
                .Distinct()
                .ToListAsync();
        }
    }
}
