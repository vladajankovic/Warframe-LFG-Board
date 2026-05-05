using ItemsStore.Commands;
using ItemsStore.Database.Entity;

namespace ItemsStore.Repository
{
    public interface IResourceRepository
    {
        public Task<ResourceEntity> CreateResource(AddResourceCommand command);
        public Task<ResourceEntity> GetResource(string name);
        public Task<List<ResourceEntity>> GetAllResources();
        public Task<ResourceEntity> UpdateResource(AddResourceCommand command);
        public Task<bool> DeleteResource(string name);
    }
}
