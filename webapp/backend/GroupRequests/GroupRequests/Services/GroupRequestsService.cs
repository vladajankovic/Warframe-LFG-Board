using AutoMapper;
using GroupRequests.Broker.Events;
using GroupRequests.Broker.RabbitMQ;
using GroupRequests.Commands;
using GroupRequests.Models;
using GroupRequests.Repositories;

namespace GroupRequests.Services
{
    public class GroupRequestsService : IGroupRequestsService
    {
        private readonly IGroupRequestsRepository _groupRequestsRepository;
        private readonly IMapper _mapper;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private readonly ILogger<GroupRequestsService> _logger;

        public GroupRequestsService(
            IGroupRequestsRepository groupRequestsRepository,
            IMapper mapper,
            IRabbitMQPublisher rabbitMQPublisher,
            ILogger<GroupRequestsService> logger)
        {
            _groupRequestsRepository = groupRequestsRepository;
            _mapper = mapper;
            _rabbitMQPublisher = rabbitMQPublisher;
            _logger = logger;
        }

        public async Task<GroupRequestDTO> CreateGroupRequest(AddGroupRequestCommand command)
        {
            var userGroupRequests = await _groupRequestsRepository.GetGroupRequestsByUserId(command.UserId);
            
            if (userGroupRequests != null && userGroupRequests.Count > 0)
            {
                throw new InvalidOperationException("User has already created a group request");
            }

            var newGroupRequest = await _groupRequestsRepository.CreateGroupRequest(command);
            var result = _mapper.Map<GroupRequestDTO>(newGroupRequest);

            try
            {
                var groupRequestCreatedEvent = new GroupRequestCreatedEvent(
                    command.UserId,
                    command.ItemName,
                    command.InGameName);
                
                await _rabbitMQPublisher.PublishAsync("group-request-created", groupRequestCreatedEvent);
                _logger.LogInformation("Published GroupRequestCreatedEvent for user {UserId} and item {ItemName}", command.UserId, command.ItemName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish GroupRequestCreatedEvent for user {UserId} and item {ItemName}", command.UserId, command.ItemName);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return result;
        }

        public async Task<bool> DeleteGroupRequest(DeleteGroupRequestCommand command)
        {
            var result = await _groupRequestsRepository.DeleteGroupRequest(command);
            
            if (result)
            {
                try
                {
                    var message = new AuditEventMessage(
                        "Delete",
                        command.UserId,
                        $"Group request deleted for user {command.UserId} and item {command.ItemName}");

                    await _rabbitMQPublisher.PublishAsync("audit", message);
                    _logger.LogInformation("Published AuditEventMessage for user {UserId} and item {ItemName}", command.UserId, command.ItemName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish AuditEventMessage for user {UserId} and item {ItemName}", command.UserId, command.ItemName);
                    // Don't rethrow - the group request was created successfully, we just failed to notify
                }
            }

            return result;
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
