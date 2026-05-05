using System.Runtime.Serialization;

namespace ItemsStore.Database.Entity
{
    public enum RelicType
    {
        UNKNOWN,
        [EnumMember(Value = "lith")]
        LITH,
        [EnumMember(Value = "meso")]
        MESO,
        [EnumMember(Value = "neo")]
        NEO,
        [EnumMember(Value = "axi")]
        AXI,
        [EnumMember(Value = "requiem")]
        REQUIEM,
        [EnumMember(Value = "vanguard")]
        VANGUARD,
    }
}
