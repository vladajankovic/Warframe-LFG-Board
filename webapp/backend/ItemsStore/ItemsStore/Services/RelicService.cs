using AutoMapper;
using ItemsStore.Commands;
using ItemsStore.Database.Entity;
using ItemsStore.Models;
using ItemsStore.Repository;
using ItemsStore.Utility;
using ItemsStore.Utility.Mappers;

namespace ItemsStore.Services
{
    public class RelicService : IRelicService
    {
        private readonly IRelicRepository _repository;
        private readonly IMapper _mapper;

        public RelicService(
            IRelicRepository repository,
            IMapper mapper
        )
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<Relic> CreateRelic(AddRelicCommand command)
        {
            var valid = RelicUtils.IsRelicValid(command);

            if (!valid)
            {
                throw new InvalidOperationException($"Relic is not valid!");
            }

            var relic = await _repository.GetRelic(command.Name);
            
            if (relic != null)
            {
                throw new InvalidOperationException($"Relic '{command.Name}' already exists");
            }

            var newRelic = await _repository.CreateRelic(command);

            return _mapper.Map<Relic>(newRelic);
        }

        public async Task<bool> DeleteRelic(string name)
        {
            return await _repository.DeleteRelic(name);
        }

        public async Task<List<Relic>> GetAllRelics()
        {
            var relicsList = await _repository.GetAllRelics();
            return _mapper.Map<List<Relic>>(relicsList);
        }

        public async Task<Relic> GetRelic(string name)
        {
            var relic = await _repository.GetRelic(name);

            if (relic == null)
            {
                return null;
            }

            return _mapper.Map<Relic>(relic);
        }

        public async Task<List<Relic>> GetRelicsByType(RelicType type)
        {
            var relicsList = await _repository.GetAllRelics();
            if (relicsList == null)
            {
                return new List<Relic>();
            }
            relicsList = relicsList.Where(r => r.RelicType == type).ToList();
            return _mapper.Map<List<Relic>>(relicsList);
        }

        public async Task<List<Relic>> GetRelicsByCode(string code)
        {
            var relicsList = await _repository.GetAllRelics();
            if (relicsList == null)
            {
                return new List<Relic>();
            }
            relicsList = relicsList.Where(r => r.RelicCode == code).ToList();
            return _mapper.Map<List<Relic>>(relicsList);
        }

        public async Task<Relic> UpdateRelic(AddRelicCommand command)
        {
            var valid = RelicUtils.IsRelicValid(command);

            if (!valid)
            {
                throw new InvalidOperationException($"Relic is not valid!");
            }

            var relic = await _repository.GetRelic(command.Name);

            if (relic == null)
            {
                throw new KeyNotFoundException($"Relic '{command.Name}' not found");
            }

            var updatedRelic = await _repository.UpdateRelic(command);

            return _mapper.Map<Relic>(updatedRelic);
        }
    }
}
