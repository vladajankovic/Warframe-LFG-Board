using ItemsStore.Commands;
using ItemsStore.Models;

namespace ItemsStore.Services
{
    public interface IResourceService
    {
        public Task<Resource> CreateResource(AddResourceCommand command);
        public Task<List<Resource>> GetAllResources();
        public Task<Resource> GetResource(string name);
        public Task<Resource> UpdateResource(AddResourceCommand command);
        public Task<bool> DeleteResource(string name);
    }
}
