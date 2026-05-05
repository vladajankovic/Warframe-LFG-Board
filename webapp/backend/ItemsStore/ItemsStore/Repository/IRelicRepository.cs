using ItemsStore.Commands;
using ItemsStore.Database.Entity;

namespace ItemsStore.Repository
{
    public interface IRelicRepository
    {
        public Task<RelicEntity> CreateRelic(AddRelicCommand relic);
        public Task<RelicEntity> GetRelic(string name);
        public Task<List<RelicEntity>> GetAllRelics();
        public Task<RelicEntity> UpdateRelic(AddRelicCommand relic);
        public Task<bool> DeleteRelic(string name);
    }
}
