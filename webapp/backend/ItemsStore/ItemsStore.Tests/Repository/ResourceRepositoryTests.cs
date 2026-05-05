using ItemsStore.Commands;
using ItemsStore.Database;
using ItemsStore.Database.Entity;
using ItemsStore.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ItemsStore.Tests.Repository
{
    public class ResourceRepositoryTests : IDisposable
    {
        private readonly ItemsDbContext _context;
        private readonly ResourceRepository _repository;

        public ResourceRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ItemsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ItemsDbContext(options);
            _repository = new ResourceRepository(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private static ResourceEntity SeedEntity(string name = "Ferrite", string? description = "A common resource.") =>
            new() { Name = name, ResourceName = name.ToLower().Replace(" ", "_"), Description = description };

        private static AddResourceCommand MakeCommand(string name = "Ferrite", string description = "A common resource.") =>
            new() { Name = name, Description = description };

        // ── CreateResource ───────────────────────────────────────────────────────

        [Fact]
        public async Task CreateResource_PersistsAndReturnsEntity()
        {
            var command = MakeCommand();

            var result = await _repository.CreateResource(command);

            Assert.NotNull(result);
            Assert.Equal(command.Name, result.Name);
            Assert.Equal("ferrite", result.ResourceName);
            Assert.Equal(command.Description, result.Description);
            Assert.Single(_context.Resources);
        }

        [Fact]
        public async Task CreateResource_SetsResourceNameLowercaseWithUnderscores()
        {
            var command = MakeCommand("Nano Spores", "Common resource.");

            var result = await _repository.CreateResource(command);

            Assert.Equal("nano_spores", result.ResourceName);
        }

        // ── GetResource ──────────────────────────────────────────────────────────

        [Fact]
        public async Task GetResource_WhenExists_ReturnsEntity()
        {
            var entity = SeedEntity();
            await _context.Resources.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = await _repository.GetResource("Ferrite");

            Assert.NotNull(result);
            Assert.Equal(entity.Name, result.Name);
        }

        [Fact]
        public async Task GetResource_LookupIsCaseInsensitive()
        {
            var entity = SeedEntity("Nano Spores");
            await _context.Resources.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = await _repository.GetResource("NANO SPORES");

            Assert.NotNull(result);
            Assert.Equal("Nano Spores", result.Name);
        }

        [Fact]
        public async Task GetResource_WhenNotFound_ReturnsNull()
        {
            var result = await _repository.GetResource("nonexistent");

            Assert.Null(result);
        }

        // ── GetAllResources ──────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllResources_ReturnsAllEntities()
        {
            await _context.Resources.AddRangeAsync(
                SeedEntity("Ferrite"),
                SeedEntity("Nano Spores")
            );
            await _context.SaveChangesAsync();

            var result = await _repository.GetAllResources();

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllResources_WhenEmpty_ReturnsEmptyList()
        {
            var result = await _repository.GetAllResources();

            Assert.Empty(result);
        }

        // ── DeleteResource ───────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteResource_WhenExists_RemovesAndReturnsTrue()
        {
            var entity = SeedEntity();
            await _context.Resources.AddAsync(entity);
            await _context.SaveChangesAsync();

            var result = await _repository.DeleteResource("Ferrite");

            Assert.True(result);
            Assert.Empty(_context.Resources);
        }

        [Fact]
        public async Task DeleteResource_WhenNotFound_ReturnsFalse()
        {
            var result = await _repository.DeleteResource("missing");

            Assert.False(result);
        }

        // ── UpdateResource ───────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateResource_WhenExists_UpdatesDescriptionAndReturnsEntity()
        {
            var entity = SeedEntity();
            await _context.Resources.AddAsync(entity);
            await _context.SaveChangesAsync();

            var command = MakeCommand("Ferrite", "Updated description.");

            var result = await _repository.UpdateResource(command);

            Assert.NotNull(result);
            Assert.Equal("Updated description.", result.Description);
            Assert.Equal("Ferrite", result.Name);
        }
    }
}
