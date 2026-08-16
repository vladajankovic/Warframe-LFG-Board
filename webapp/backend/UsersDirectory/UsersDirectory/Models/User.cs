using System.Text.Json.Serialization;
using UsersDirectory.Enums;

namespace UsersDirectory.Models
{
    public class User
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string InGameName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; }
        public bool IsVerified { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public DateTime? SubscriptionExpiryDate { get; set; }
    }
}
