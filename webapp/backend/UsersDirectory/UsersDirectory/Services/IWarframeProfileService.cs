using UsersDirectory.Models;

namespace UsersDirectory.Services
{
    public interface IWarframeProfileService
    {
        Task<WarframeProfileData> GetProfile(
            string playerId,
            CancellationToken cancellationToken = default);
    }
}
