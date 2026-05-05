using ItemsStore.Commands;
using ItemsStore.Database.Entity;
using ItemsStore.Models;

namespace ItemsStore.Services
{
    public interface IRelicService
    {
        public Task<Relic> CreateRelic(AddRelicCommand command);
        public Task<List<Relic>> GetAllRelics();
        public Task<Relic> GetRelic(string name);
        public Task<List<Relic>> GetRelicsByType(RelicType type);
        public Task<List<Relic>> GetRelicsByCode(string code);
        public Task<Relic> UpdateRelic(AddRelicCommand command);
        public Task<bool> DeleteRelic(string name);
    }
}
