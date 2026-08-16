namespace UsersDirectory.Models
{
    public class WarframeProfileData
    {
        public string? DisplayName { get; set; }
    }

    public class WarframeProfileResponse
    {
        public List<WarframeProfileData> Results { get; set; } = [];
    }
}
