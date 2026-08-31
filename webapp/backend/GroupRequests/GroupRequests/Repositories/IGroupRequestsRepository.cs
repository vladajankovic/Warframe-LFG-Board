using GroupRequests.Commands;
using GroupRequests.Database.Entities;

namespace GroupRequests.Repositories
{
    public interface IGroupRequestsRepository
    {
        public Task<List<GroupRequestEntity>> GetAllGroupRequests();
        public Task<List<GroupRequestEntity>> GetGroupRequestsByItemName(string itemName);
        public Task<List<GroupRequestEntity>> GetGroupRequestsByUserId(string userId);
        public Task<GroupRequestEntity> GetGroupRequest(string userId, string itemName);
        public Task<GroupRequestEntity> CreateGroupRequest(AddGroupRequestCommand command);
        public Task<bool> DeleteGroupRequest(DeleteGroupRequestCommand command);
        public Task<int> DeleteAllGroupRequestForUser(string userId);
    }
}
