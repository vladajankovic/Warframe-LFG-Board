using ItemsStore.Commands;
using ItemsStore.Database;
using ItemsStore.Database.Entity;
using ItemsStore.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ItemsStore.Tests.Repository
{
    public class RelicRepositoryTests : IDisposable
    {
        private readonly ItemsDbContext _context;
        private readonly RelicRepository _repository;

        public RelicRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ItemsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ItemsDbContext(options);
            _repository = new RelicRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private static RelicEntity SeedEntity(string name = "Lith A1 Intact", RelicType type = RelicType.LITH, string code = "A1") =>
            new() { Name = name, RelicName = name.ToLower().Replace(" ", "_"), RelicType = type, RelicCode = code };

        private static AddRelicCommand MakeCommand(string name = "Lith A1 Intact", RelicType type = RelicType.LITH, string code = "A1") =>
            new() { Name = name, Type = type, Code = code };

        // ── CreateRelic ──────────────────────────────────────────────────────────

        [Fact]
        public async Task CreateRelic_PersistsAndReturnsEntity()
        {
            var command = MakeCommand();

            var result = await _repository.CreateRelic(command);

            Assert.NotNull(result);
            Assert.Equal(command.Name, result.Name);
            Assert.Equal("lith_a1_intact", result.RelicName);
            Assert.Equal(RelicType.LITH, result.RelicType);
            Assert.Equal("A1", result.RelicCode);
            Assert.Single(_context.Relics);
        }

        [Fact]
        public async Task CreateRelic_SetsRelicNameLowercaseWithUnderscores()
        {
            var command = MakeCommand("Meso B1 Intact", RelicType.MESO, "B1");

            var result = await _repository.CreateRelic(command);

            Assert.Equal("meso_b1_intact", result.RelicName);
        }

        // ── GetRelic ─────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetRelic_WhenExists_ReturnsEntityWithDrops()
        {
            var entity = SeedEntity();
            entity.Drops.Add(new RelicDropEntity
            {
                ItemName = "Forma Blueprint",
                Rarity = "common"
            });
            await _context.Relics.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = await _repository.GetRelic(entity.RelicName);

            Assert.NotNull(result);
            Assert.Equal(entity.Name, result.Name);
            var drop = Assert.Single(result.Drops);
            Assert.Equal("Forma Blueprint", drop.ItemName);
            Assert.Equal("common", drop.Rarity);
        }

        [Fact]
        public async Task GetRelic_WhenNotFound_ReturnsNull()
        {
            var result = await _repository.GetRelic("nonexistent");

            Assert.Null(result);
        }

        // ── GetAllRelics ─────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllRelics_ReturnsAllEntities()
        {
            await _context.Relics.AddRangeAsync(
                SeedEntity("Lith A1 Intact", RelicType.LITH, "A1"),
                SeedEntity("Meso B1 Intact", RelicType.MESO, "B1")
            );
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllRelics();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllRelics_WhenEmpty_ReturnsEmptyList()
        {
            var result = await _repository.GetAllRelics();

            Assert.Empty(result);
        }

        // ── DeleteRelic ───────────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteRelic_WhenExists_RemovesAndReturnsTrue()
        {
            var entity = SeedEntity();
            await _context.Relics.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = await _repository.DeleteRelic(entity.RelicName);

            Assert.True(result);
            Assert.Empty(_context.Relics);
        }

        [Fact]
        public async Task DeleteRelic_WhenNotFound_ReturnsFalse()
        {
            var result = await _repository.DeleteRelic("missing");

            Assert.False(result);
        }

        // ── UpdateRelic ───────────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateRelic_WhenExists_UpdatesFieldsAndReturnsEntity()
        {
            var entity = SeedEntity();
            await _context.Relics.AddAsync(entity);
            await _context.SaveChangesAsync();

            var command = new AddRelicCommand
            {
                Name = entity.RelicName,   // repository looks up by RelicName
                Type = RelicType.NEO,
                Code = "Z9"
            };

            var result = await _repository.UpdateRelic(command);

            Assert.Equal(RelicType.NEO, result.RelicType);
            Assert.Equal("Z9", result.RelicCode);
        }
    }
}
