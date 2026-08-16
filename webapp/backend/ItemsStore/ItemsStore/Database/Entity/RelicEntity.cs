namespace ItemsStore.Database.Entity
{
    public class RelicEntity
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string RelicName { get; set; }
        public required RelicType RelicType { get; set; }
        public required string RelicCode { get; set; }
        public ICollection<RelicDropEntity> Drops { get; set; } = [];
    }
}
