using ItemsStore.Commands;
using ItemsStore.Database;
using ItemsStore.Database.Entity;
using ItemsStore.Utility;
using Microsoft.EntityFrameworkCore;

namespace ItemsStore.Repository
{
    public class RelicRepository : IRelicRepository
    {
        private readonly ItemsDbContext _itemsDbContext;

        public RelicRepository(
            ItemsDbContext itemsDbContext
        )
        {
            _itemsDbContext = itemsDbContext;
        }

        public async Task<RelicEntity> CreateRelic(AddRelicCommand relic)
        {
            var newRelic = new RelicEntity()
            {
                Name = relic.Name,
                RelicName = relic.Name.ToSnakeCase(),
                RelicType = relic.Type,
                RelicCode = relic.Code
            };

            await _itemsDbContext.Relics.AddAsync(newRelic);

            await _itemsDbContext.SaveChangesAsync();

            return newRelic;
        }

        public async Task<bool> DeleteRelic(string name)
        {
            var relicEntity = await GetRelic(name);

            if (relicEntity == null)
            {
                return false;
            }

            _itemsDbContext.Relics.Remove(relicEntity);

            await _itemsDbContext.SaveChangesAsync();

            return true;
        }

        public async Task<List<RelicEntity>> GetAllRelics()
        {
            return await _itemsDbContext.Relics.ToListAsync();
        }

        public async Task<RelicEntity> GetRelic(string name)
        {
            return await _itemsDbContext.Relics.FirstOrDefaultAsync(x => x.RelicName == name.ToSnakeCase());
        }

        public async Task<RelicEntity> UpdateRelic(AddRelicCommand relic)
        {
            var relicEntity = await GetRelic(relic.Name);

            relicEntity.RelicType = relic.Type;
            relicEntity.RelicCode = relic.Code;

            await _itemsDbContext.SaveChangesAsync();

            return relicEntity;
        }
    }
}
