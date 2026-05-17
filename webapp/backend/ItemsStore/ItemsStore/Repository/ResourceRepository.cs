using ItemsStore.Commands;
using ItemsStore.Database;
using ItemsStore.Database.Entity;
using ItemsStore.Utility;
using Microsoft.EntityFrameworkCore;

namespace ItemsStore.Repository
{
    public class ResourceRepository : IResourceRepository
    {
        private readonly ItemsDbContext _itemsDbContext;
        public ResourceRepository(
            ItemsDbContext dbContext
        ) 
        { 
            _itemsDbContext = dbContext;
        }
        public async Task<ResourceEntity> CreateResource(AddResourceCommand command)
        {
            var resourceEntity = new ResourceEntity
            {
                Name = command.Name,
                ResourceName = command.Name.ToSnakeCase(),
                Description = command.Description,
                Region = command.Region,
                Category = command.Category,
                Group = command.Group,
            };
            
            await _itemsDbContext.AddAsync(resourceEntity);

            await _itemsDbContext.SaveChangesAsync();
            
            return resourceEntity;
        }

        public async Task<bool> DeleteResource(string name)
        {
            var resourceEntity = await GetResource(name);

            if (resourceEntity == null)
            {
                return false;
            }

            _itemsDbContext.Resources.Remove(resourceEntity);

            await _itemsDbContext.SaveChangesAsync();

            return true;
        }

        public async Task<List<ResourceEntity>> GetAllResources()
        {
            return await _itemsDbContext.Resources.ToListAsync();
        }

        public async Task<ResourceEntity> GetResource(string name)
        {
            return await _itemsDbContext.Resources.FirstOrDefaultAsync(x => x.ResourceName == name.ToSnakeCase());
        }

        public async Task<ResourceEntity> UpdateResource(AddResourceCommand command)
        {
            var resourceEntity = await GetResource(command.Name);

            resourceEntity.Description = command.Description;
            resourceEntity.Region = command.Region;
            resourceEntity.Category = command.Category;
            resourceEntity.Group = command.Group;

            await _itemsDbContext.SaveChangesAsync();

            return resourceEntity;
        }
    }
}
