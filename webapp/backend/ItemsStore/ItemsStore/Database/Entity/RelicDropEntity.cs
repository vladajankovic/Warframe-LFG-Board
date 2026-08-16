namespace ItemsStore.Database.Entity
{
    public class RelicDropEntity
    {
        public int RelicId { get; set; }
        public required string ItemName { get; set; }
        public required string Rarity { get; set; }
        public RelicEntity Relic { get; set; } = null!;
    }
}
