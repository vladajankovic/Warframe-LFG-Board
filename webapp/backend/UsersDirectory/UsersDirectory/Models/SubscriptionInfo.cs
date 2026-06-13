using UsersDirectory.Enums;

namespace UsersDirectory.Models
{
    public class SubscriptionInfo
    {
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public DateTime? SubscriptionExpiryDate { get; set; }
    }
}
