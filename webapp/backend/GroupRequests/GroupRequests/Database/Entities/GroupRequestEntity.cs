namespace GroupRequests.Database.Entities
{
    public class GroupRequestEntity
    {
        public int Id { get; set; }
        public required string UserId { get; set; }
        public required string InGameName { get; set; }
        public required string ItemName { get; set; }
    }
}
