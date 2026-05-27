using AutoMapper;
using GroupRequests.Commands;
using GroupRequests.Models;
using GroupRequests.Repositories;

namespace GroupRequests.Services
{
    public class GroupRequestsService : IGroupRequestsService
    {
        private readonly IGroupRequestsRepository _groupRequestsRepository;
        private readonly IMapper _mapper;
        public GroupRequestsService(IGroupRequestsRepository groupRequestsRepository, IMapper mapper)
        {
            _groupRequestsRepository = groupRequestsRepository;
            _mapper = mapper;
        }
        public async Task<GroupRequestDTO> CreateGroupRequest(AddGroupRequestCommand command)
        {
            var userGroupRequests = await _groupRequestsRepository.GetGroupRequestsByUserId(command.UserId);
            
            if (userGroupRequests != null && userGroupRequests.Count > 0)
            {
                throw new InvalidOperationException("User has already created a group request");
            }

            var newGroupRequest = await _groupRequestsRepository.CreateGroupRequest(command);

            return _mapper.Map<GroupRequestDTO>(newGroupRequest);
        }

        public async Task<bool> DeleteGroupRequest(DeleteGroupRequestCommand command)
        {
            return await _groupRequestsRepository.DeleteGroupRequest(command);
        }

        public async Task<List<GroupRequestDTO>> GetAllGroupRequests()
        {
            var allGroupRequests = await _groupRequestsRepository.GetAllGroupRequests();
            return _mapper.Map<List<GroupRequestDTO>>(allGroupRequests);
        }

        public async Task<List<GroupRequestDTO>> GetGroupRequestsByItemName(string itemName)
        {
            var listGroupRequests = await _groupRequestsRepository.GetGroupRequestsByItemName(itemName);
            return _mapper.Map<List<GroupRequestDTO>>(listGroupRequests);
        }

        public async Task<List<GroupRequestDTO>> GetGroupRequestsByUserId(string userId)
        {
            var listGroupRequests = await _groupRequestsRepository.GetGroupRequestsByUserId(userId);
            return _mapper.Map<List<GroupRequestDTO>>(listGroupRequests);
        }

        public async Task<GroupRequestDTO> GetGroupRequest(string userId, string itemName)
        {
            var groupRequest = await _groupRequestsRepository.GetGroupRequest(userId, itemName);
            return _mapper.Map<GroupRequestDTO>(groupRequest);
        }
    }
}
