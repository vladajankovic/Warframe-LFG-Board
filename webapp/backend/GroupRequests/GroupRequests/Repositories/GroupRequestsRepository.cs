using GroupRequests.Commands;
using GroupRequests.Database;
using GroupRequests.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace GroupRequests.Repositories
{
    public class GroupRequestsRepository : IGroupRequestsRepository
    {
        private readonly GroupRequestsDbContext _dbContext;
        public GroupRequestsRepository(GroupRequestsDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task<GroupRequestEntity> CreateGroupRequest(AddGroupRequestCommand command)
        {
            var newGroupRequest = new GroupRequestEntity
            {
                UserId = command.UserId,
                InGameName = command.InGameName,
                ItemName = command.ItemName,
            };

            await _dbContext.GroupRequests.AddAsync(newGroupRequest);
            await _dbContext.SaveChangesAsync();

            return newGroupRequest;
        }

        public async Task<bool> DeleteGroupRequest(DeleteGroupRequestCommand command)
        {
            var requestToDelete = await GetGroupRequest(command.UserId, command.ItemName);

            if (requestToDelete == null)
            {
                return false;
            }

            _dbContext.GroupRequests.Remove(requestToDelete);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<int> DeleteAllGroupRequestForUser(string userId)
        {
            var entities = await GetGroupRequestsByUserId(userId);

            _dbContext.GroupRequests.RemoveRange(entities);

            await _dbContext.SaveChangesAsync();

            return entities.Count;
        }

        public async Task<List<GroupRequestEntity>> GetAllGroupRequests()
        {
            return await _dbContext.GroupRequests.ToListAsync();
        }

        public async Task<List<GroupRequestEntity>> GetGroupRequestsByItemName(string itemName)
        {
            return await _dbContext.GroupRequests.Where(x => x.ItemName == itemName).ToListAsync();
        }

        public async Task<List<GroupRequestEntity>> GetGroupRequestsByUserId(string userId)
        {
            return await _dbContext.GroupRequests.Where(x => x.UserId == userId).ToListAsync();
        }

        public async Task<GroupRequestEntity> GetGroupRequest(string userId, string itemName)
        {
            return await _dbContext.GroupRequests.Where(x => x.ItemName == itemName && x.UserId == userId).FirstOrDefaultAsync();
        }
    }
}
