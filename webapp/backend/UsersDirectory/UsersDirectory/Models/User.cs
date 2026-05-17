namespace UsersDirectory.Models
{
    public class User
    {
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public bool IsVerified { get; set; }
    }
}
