using AutoMapper;
using ItemsStore.Commands;
using ItemsStore.Models;
using ItemsStore.Repository;

namespace ItemsStore.Services
{
    public class ResourceService : IResourceService
    {
        private readonly IResourceRepository _resourceRepository;
        private readonly IMapper _mapper;
        public ResourceService(
            IResourceRepository resourceRepository,
            IMapper mapper
        )
        {
            _resourceRepository = resourceRepository;
            _mapper = mapper;
        }
        public async Task<Resource> CreateResource(AddResourceCommand command)
        {
            var resource = await _resourceRepository.GetResource(command.Name);
            
            if ( resource != null )
            {
                throw new InvalidOperationException($"Resource '{command.Name}' already exists");
            }

            var newResource = await _resourceRepository.CreateResource(command);

            return _mapper.Map<Resource>(newResource);
        }

        public async Task<bool> DeleteResource(string name)
        {
            return await _resourceRepository.DeleteResource(name);
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

            return _mapper.Map<Resource>(updatedResource);
        }
    }
}
