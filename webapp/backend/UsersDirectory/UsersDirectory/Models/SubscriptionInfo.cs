using System.Text.Json.Serialization;
using UsersDirectory.Enums;

namespace UsersDirectory.Models
{
    public class SubscriptionInfo
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SubscriptionStatus SubscriptionStatus { get; set; }
        public DateTime? SubscriptionExpiryDate { get; set; }
    }
}
