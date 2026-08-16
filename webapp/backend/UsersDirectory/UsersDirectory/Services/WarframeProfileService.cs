using System.Net.Http.Json;
using UsersDirectory.Models;

namespace UsersDirectory.Services
{
    public class WarframeProfileService : IWarframeProfileService
    {
        private const string WarframeProfileUrlFallback =
            "https://api.warframe.com/cdn/getProfileViewingData.php?playerId=";
        private const string WarframeProfileHost = "api.warframe.com";

        private readonly HttpClient _httpClient;

        public WarframeProfileService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<WarframeProfileData> GetProfile(
            string playerId,
            CancellationToken cancellationToken = default)
        {
            var profileUrl = $"{WarframeProfileUrlFallback}{Uri.EscapeDataString(playerId)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, profileUrl);
            request.Headers.Host = WarframeProfileHost;

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var profileResponse = await response.Content.ReadFromJsonAsync<WarframeProfileResponse>(
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Warframe returned an empty profile response.");

            return profileResponse.Results.FirstOrDefault(profile =>
                !string.IsNullOrWhiteSpace(profile.DisplayName))
                ?? throw new InvalidOperationException(
                    "Warframe returned no profile with a display name.");
        }
    }
}
