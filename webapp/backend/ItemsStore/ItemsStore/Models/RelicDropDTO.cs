namespace ItemsStore.Models
{
    public class RelicDropDTO
    {
        public required string Place { get; set; }
        public required string Item { get; set; }
        public required string Rarity { get; set; }
        public double? Chance { get; set; }
    }
}
