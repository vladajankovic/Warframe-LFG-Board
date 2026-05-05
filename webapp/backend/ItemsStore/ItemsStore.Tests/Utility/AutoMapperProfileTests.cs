using AutoMapper;
using ItemsStore.Database.Entity;
using ItemsStore.Models;
using ItemsStore.Utility.Mappers;
using Xunit;

namespace ItemsStore.Tests.Utility
{
    public class AutoMapperProfileTests
    {
        private readonly IMapper _mapper;

        public AutoMapperProfileTests()
        {
            var config = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>());
            config.AssertConfigurationIsValid();
            _mapper = config.CreateMapper();
        }

        [Fact]
        public void Map_RelicEntityToRelic_MapsAllFields()
        {
            var entity = new RelicEntity
            {
                Id = "12345",
                Name = "Lith A1",
                RelicName = "lith_a1",
                RelicType = RelicType.LITH,
                RelicCode = "A1"
            };

            var relic = _mapper.Map<Relic>(entity);

            Assert.Equal(entity.Id, relic.Id);
            Assert.Equal(entity.Name, relic.Name);
            Assert.Equal(entity.RelicCode, relic.RelicCode);
            // RelicType should be the lowercase string representation
            Assert.Equal("lith", relic.RelicType);
        }

        [Theory]
        [InlineData(RelicType.LITH,     "lith")]
        [InlineData(RelicType.MESO,     "meso")]
        [InlineData(RelicType.NEO,      "neo")]
        [InlineData(RelicType.AXI,      "axi")]
        [InlineData(RelicType.REQUIEM,  "requiem")]
        [InlineData(RelicType.VANGUARD, "vanguard")]
        [InlineData(RelicType.UNKNOWN,  "unknown")]
        public void Map_RelicTypeEnum_MapsToLowercaseString(RelicType type, string expected)
        {
            var entity = new RelicEntity
            {
                Id = "12345",
                Name = "Test Relic",
                RelicName = "test_relic",
                RelicType = type,
                RelicCode = "X1"
            };

            var relic = _mapper.Map<Relic>(entity);

            Assert.Equal(expected, relic.RelicType);
        }
    }
}
