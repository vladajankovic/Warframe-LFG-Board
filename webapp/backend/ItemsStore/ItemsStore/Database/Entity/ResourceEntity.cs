namespace ItemsStore.Database.Entity
{
    public class ResourceEntity
    {
        public int Id { get; set; }
        public required string ResourceName { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? Region { get; set; }
        public string? Category { get; set; }
        public string? Group { get; set; }
    }
}
