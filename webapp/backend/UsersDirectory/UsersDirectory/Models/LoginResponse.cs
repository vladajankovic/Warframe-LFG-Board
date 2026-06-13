using UsersDirectory.Enums;

namespace UsersDirectory.Models
{
    public class LoginResponse
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string InGameName { get; set; }
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public DateTime? SubscriptionExpiryDate { get; set; }
    }
}
