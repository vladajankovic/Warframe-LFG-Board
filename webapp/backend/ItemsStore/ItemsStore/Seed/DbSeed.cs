using ItemsStore.Database;
using ItemsStore.Database.Entity;
using ItemsStore.Models;
using ItemsStore.Utility;
using ItemsStore.Utility.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ItemsStore.Seed
{
    public class DbSeed
    {
        private readonly ItemsDbContext _dbContext;
        private readonly WarframeApiUtils _warframeApiUtils;
        private readonly ILogger<DbSeed> _logger;

        public DbSeed(ItemsDbContext dbContext, WarframeApiUtils warframeApiUtils, ILogger<DbSeed> logger)
        {
            _dbContext = dbContext;
            _warframeApiUtils = warframeApiUtils;
            _logger = logger;
        }

        public async Task SeedRelics()
        {
            try
            {
                // Check if relics already exist in database
                var existingRelicsCount = await _dbContext.Relics.CountAsync();
                if (existingRelicsCount > 0)
                {
                    _logger.LogInformation("Database already contains {Count} relics. Skipping seed.", existingRelicsCount);
                    return;
                }

                _logger.LogInformation("Fetching relics from Warframe API...");
                var relicItems = await _warframeApiUtils.GetAllRelics();

                if (relicItems == null || relicItems.Count == 0)
                {
                    _logger.LogWarning("No relics returned from API.");
                    return;
                }

                _logger.LogInformation("Retrieved {Count} relics from API. Processing...", relicItems.Count);

                var relicsToAdd = new List<RelicEntity>();

                foreach (var item in relicItems)
                {
                    var relicType = RelicUtils.ExtractRelicType(item.Name);

                    if (relicType == RelicType.UNKNOWN)
                    {
                        _logger.LogWarning("Could not determine type for relic: {Name}", item.Name);
                        continue;
                    }

                    var relicCode = RelicUtils.ExtractRelicCode(item.Name);

                    if (string.IsNullOrEmpty(relicCode))
                    {
                        _logger.LogWarning("Could not determine code for relic: {Name}", item.Name);
                        continue;
                    }

                    var relicName = string.Join(" ", item.Name.Split().SkipLast(1));

                    var relicEntity = new RelicEntity
                    {
                        Name = relicName,
                        RelicName = relicName.ToSnakeCase(),
                        RelicType = relicType,
                        RelicCode = relicCode
                    };

                    relicsToAdd.Add(relicEntity);
                }

                if (relicsToAdd.Any())
                {
                    await _dbContext.Relics.AddRangeAsync(relicsToAdd);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Successfully seeded {Count} relics into database.", relicsToAdd.Count);
                }
                else
                {
                    _logger.LogWarning("No valid relics to seed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding relics.");
                throw;
            }
        }

        public async Task SeedResources()
        {
            try
            {
                var existingResourcesCount = await _dbContext.Resources.CountAsync();
                if (existingResourcesCount > 0)
                {
                    _logger.LogInformation("Database already contains {Count} resources. Skipping seed.", existingResourcesCount);
                    return;
                }

                _logger.LogInformation("Fetching resources from Warframe API...");
                var resourceItems = await _warframeApiUtils.GetAllResources();

                if (resourceItems == null || resourceItems.Count == 0)
                {
                    _logger.LogWarning("No resources returned from API.");
                    return;
                }

                _logger.LogInformation("Retrieved {Count} resources from API. Processing...", resourceItems.Count);

                var resourcesToAdd = resourceItems.Select(item => new ResourceEntity
                {
                    Name = item.Name,
                    ResourceName = item.Name.ToSnakeCase(),
                    Description = item.Description
                }).ToList();

                await _dbContext.Resources.AddRangeAsync(resourcesToAdd);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully seeded {Count} resources into database.", resourcesToAdd.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while seeding resources.");
                throw;
            }
        }
    }
}
