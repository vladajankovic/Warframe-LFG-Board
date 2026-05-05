using ItemsStore.Database.Entity;

namespace ItemsStore.Utility
{
    public static class RelicUtils
    {
        public static RelicType GetRelicTypeEnum(string? type)
        {
            return type switch
            {
                "lith" => RelicType.LITH,
                "meso" => RelicType.MESO,
                "neo"  => RelicType.NEO,
                "axi"  => RelicType.AXI,
                "requiem" => RelicType.REQUIEM,
                "vanguard" => RelicType.VANGUARD,
                _ => RelicType.UNKNOWN
            };
        }

        public static RelicType ExtractRelicType(string relicName)
        {
            var type = relicName.ToLower().Split().FirstOrDefault();
            return GetRelicTypeEnum(type);
        }

        public static string ExtractRelicCode(string relicName)
        {
            var split = relicName.Split();
            return split.Length > 1 ? split.ElementAt(1) : string.Empty;
        }
    }
}
