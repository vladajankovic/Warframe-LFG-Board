namespace UsersDirectory.Database.Entities
{
    public class UserEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Email { get; set; }

        public string Password { get; set; }
    }
}
