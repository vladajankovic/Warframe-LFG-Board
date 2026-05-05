namespace ItemsStore.Database.Entity
{
    public class ResourceEntity
    {
        public int Id { get; set; }
        public required string ResourceName { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
    }
}
