namespace ItemsStore.Models
{
    public class ItemDTO
    {
        public string Name { get; set; }
        public string UniqueName { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public bool ExcludeFromCodex { get; set; }
    }
}
