namespace UsersDirectory.Database.Entities
{
    public class UserEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Email { get; set; }

        public string Password { get; set; }

        public string Role { get; set; } = "User";

        public DateTime CreatedAt { get; set; }

        public DateTime LastUpdatedAt { get; set; }

        public string VerificationCode { get; set; }

        public bool IsVerified { get; set; } = false;
    }
}
