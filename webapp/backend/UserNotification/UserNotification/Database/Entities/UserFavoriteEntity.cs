namespace UserNotification.Database.Entities
{
    public class UserFavoriteEntity
    {
        public string Id {  get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string ItemName { get; set; }
    }
}
