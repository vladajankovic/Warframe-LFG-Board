using UsersDirectory.Enums;

namespace UsersDirectory.Database.Entities
{
    public class UserEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public required string Email { get; set; }

        public required string Password { get; set; }

        public string Role { get; set; } = "User";

        public string InGameName { get; set; } = "";

        public DateTime CreatedAt { get; set; }

        public DateTime LastUpdatedAt { get; set; }

        public SubscriptionStatus SubscriptionStatus { get; set; } = SubscriptionStatus.Inactive;

        public DateTime? SubscriptionExpiryDate { get; set; } = null;
    }
}
