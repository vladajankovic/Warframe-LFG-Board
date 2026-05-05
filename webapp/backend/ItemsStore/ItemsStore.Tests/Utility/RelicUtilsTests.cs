using ItemsStore.Database.Entity;
using ItemsStore.Utility;
using Xunit;

namespace ItemsStore.Tests.Utility
{
    public class RelicUtilsTests
    {

        [Theory]
        [InlineData("lith",     RelicType.LITH)]
        [InlineData("meso",     RelicType.MESO)]
        [InlineData("neo",      RelicType.NEO)]
        [InlineData("axi",      RelicType.AXI)]
        [InlineData("requiem",  RelicType.REQUIEM)]
        [InlineData("vanguard", RelicType.VANGUARD)]
        public void GetRelicTypeEnum_KnownTypes_ReturnsCorrectEnum(string input, RelicType expected)
        {
            var result = RelicUtils.GetRelicTypeEnum(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("unknown_type")]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("LITH")]        // case-sensitive — uppercase not mapped
        [InlineData("  lith ")]     // whitespace not mapped
        public void GetRelicTypeEnum_UnknownInput_ReturnsUnknown(string? input)
        {
            var result = RelicUtils.GetRelicTypeEnum(input);
            Assert.Equal(RelicType.UNKNOWN, result);
        }

        [Theory]
        [InlineData("Lith A1 Intact",    RelicType.LITH)]
        [InlineData("Meso B1 Intact",    RelicType.MESO)]
        [InlineData("Neo C1 Intact",     RelicType.NEO)]
        [InlineData("Axi D1 Intact",     RelicType.AXI)]
        [InlineData("Requiem I1 Intact", RelicType.REQUIEM)]
        [InlineData("Vanguard E1 Intact",RelicType.VANGUARD)]
        public void ExtractRelicType_KnownPrefix_ReturnsCorrectEnum(string relicName, RelicType expected)
        {
            var result = RelicUtils.ExtractRelicType(relicName);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ExtractRelicType_UnknownPrefix_ReturnsUnknown()
        {
            var result = RelicUtils.ExtractRelicType("Unknown X1 Intact");
            Assert.Equal(RelicType.UNKNOWN, result);
        }

        [Theory]
        [InlineData("Lith A1 Intact", "A1")]
        [InlineData("Meso B2 Intact", "B2")]
        [InlineData("Neo",            "")]   // only one word → empty
        public void ExtractRelicCode_ReturnsCodeSegment(string relicName, string expected)
        {
            var result = RelicUtils.ExtractRelicCode(relicName);
            Assert.Equal(expected, result);
        }
    }
}
