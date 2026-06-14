using AutoMapper;
using ItemsStore.Broker.RabbitMQ;
using ItemsStore.Commands;
using ItemsStore.Models;
using ItemsStore.Repository;

namespace ItemsStore.Services
{
    public class ResourceService : IResourceService
    {
        private readonly IResourceRepository _resourceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ResourceService> _logger;
        private readonly IRabbitMQPublisher _rabbitMQPublisher;

        public ResourceService(
            IResourceRepository resourceRepository,
            IMapper mapper,
            ILogger<ResourceService> logger,
            IRabbitMQPublisher rabbitMQPublisher
        )
        {
            _resourceRepository = resourceRepository;
            _mapper = mapper;
            _logger = logger;
            _rabbitMQPublisher = rabbitMQPublisher;
        }
        public async Task<Resource> CreateResource(AddResourceCommand command)
        {
            var resource = await _resourceRepository.GetResource(command.Name);
            
            if ( resource != null )
            {
                throw new InvalidOperationException($"Resource '{command.Name}' already exists");
            }

            var newResource = await _resourceRepository.CreateResource(command);

            try
            {
                var message = new AuditEventMessage(
                    "Create",
                    "Admin",
                    $"Item created: {command.Name}");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for item {ItemName}", command.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage for item {ItemName}", command.Name);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return _mapper.Map<Resource>(newResource);
        }

        public async Task<bool> DeleteResource(string name)
        {
            var result = await _resourceRepository.DeleteResource(name);

            if (result)
            {
                try
                {
                    var message = new AuditEventMessage(
                        "Delete",
                        "Admin",
                        $"Item deleted: {name}");

                    await _rabbitMQPublisher.PublishAsync("audit", message);
                    _logger.LogInformation("Published AuditEventMessage for item {ItemName}", name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish AuditEventMessage for item {ItemName}", name);
                    // Don't rethrow - the group request was created successfully, we just failed to notify
                }
            }

            return result;
        }

        public async Task<List<Resource>> GetAllResources()
        {
            var resourceList = await _resourceRepository.GetAllResources();
            return _mapper.Map<List<Resource>>(resourceList);
        }

        public async Task<Resource> GetResource(string name)
        {
            var resource = await _resourceRepository.GetResource(name);

            if ( resource == null )
            {
                return null;
            }

            return _mapper.Map<Resource>(resource);
        }

        public async Task<Resource> UpdateResource(AddResourceCommand command)
        {
            var resource = await _resourceRepository.GetResource(command.Name);

            if (resource == null)
            {
                throw new InvalidOperationException($"Resource '{command.Name}' not found");
            }

            var updatedResource = await _resourceRepository.UpdateResource(command);

            try
            {
                var message = new AuditEventMessage(
                    "Update",
                    "Admin",
                    $"Item updated: {command.Name}");

                await _rabbitMQPublisher.PublishAsync("audit", message);
                _logger.LogInformation("Published AuditEventMessage for item {ItemName}", command.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish AuditEventMessage for item {ItemName}", command.Name);
                // Don't rethrow - the group request was created successfully, we just failed to notify
            }

            return _mapper.Map<Resource>(updatedResource);
        }
    }
}
