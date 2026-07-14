using GroupRequests.Commands;
using GroupRequests.Models;

namespace GroupRequests.Services
{
    public interface IGroupRequestsService
    {
        public Task<List<GroupRequestDTO>> GetAllGroupRequests();
        public Task<List<GroupRequestDTO>> GetGroupRequestsByItemName(string itemName);
        public Task<List<GroupRequestDTO>> GetGroupRequestsByUserId(string userId);
        public Task<GroupRequestDTO> GetGroupRequest(string userId, string itemName);
        public Task<GroupRequestDTO> CreateGroupRequest(AddGroupRequestCommand command, string status);
        public Task<bool> DeleteGroupRequest(DeleteGroupRequestCommand command);
    }
}
