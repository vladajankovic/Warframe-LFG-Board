using System.Net;
using System.Text;
using ItemsStore.Database;
using ItemsStore.Seed;
using ItemsStore.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ItemsStore.Tests.Seed
{
    public class DbSeedTests
    {
        [Fact]
        public async Task SeedRelics_SeedsDropsWithRarityDerivedFromChance_AndIsIdempotent()
        {
            var options = new DbContextOptionsBuilder<ItemsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            await using var context = new ItemsDbContext(options);
            var handler = new RelicSeedHttpMessageHandler();
            using var httpClient = new HttpClient(handler);
            var apiUtils = new WarframeApiUtils(httpClient);
            var seed = new DbSeed(context, apiUtils, NullLogger<DbSeed>.Instance);

            await seed.SeedRelics();

            var relic = await context.Relics.SingleAsync();
            var drops = await context.RelicDrops
                .OrderBy(x => x.ItemName)
                .ToListAsync();

            Assert.Equal("Lith A1", relic.Name);
            Assert.Equal(3, drops.Count);
            Assert.All(drops, drop => Assert.Equal(relic.Id, drop.RelicId));
            Assert.Equal("common", drops.Single(x => x.ItemName == "Common Item").Rarity);
            Assert.Equal("rare", drops.Single(x => x.ItemName == "Rare Item").Rarity);
            Assert.Equal("uncommon", drops.Single(x => x.ItemName == "Uncommon Item").Rarity);

            await seed.SeedRelics();

            Assert.Equal(3, await context.RelicDrops.CountAsync());
            Assert.Equal(1, handler.ItemsRequestCount);
            Assert.Equal(1, handler.DropsRequestCount);
        }

        private sealed class RelicSeedHttpMessageHandler : HttpMessageHandler
        {
            public int ItemsRequestCount { get; private set; }
            public int DropsRequestCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath;
                string responseBody;

                if (path == "/items")
                {
                    ItemsRequestCount++;
                    responseBody = """
                        [
                          { "name": "Lith A1 Intact", "type": "Relic" }
                        ]
                        """;
                }
                else if (path == "/drops")
                {
                    DropsRequestCount++;
                    responseBody = """
                        [
                          { "place": "Lith A1 Relic", "item": "Rare Item", "rarity": "ignored", "chance": 2 },
                          { "place": "Lith A1 Relic", "item": "Uncommon Item", "rarity": "ignored", "chance": 11 },
                          { "place": "Lith A1 Relic", "item": "Common Item", "rarity": "ignored", "chance": 25.33 },
                          { "place": "Lith A1 Relic (Radiant)", "item": "Refined Item", "rarity": "ignored", "chance": 10 }
                        ]
                        """;
                }
                else
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
