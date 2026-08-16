using System.Net;
using System.Text;
using UsersDirectory.Services;

namespace UsersDirectory.Tests.Services
{
    public class WarframeProfileServiceTests
    {
        [Fact]
        public async Task GetProfile_SetsWarframeHostHeaderAndReturnsProfile()
        {
            var handler = new RecordingHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"Results":[{"AccountId":{},"DisplayName":"UndeadWinter"}]}""",
                    Encoding.UTF8,
                    "application/json")
            });
            var service = new WarframeProfileService(new HttpClient(handler));

            var profile = await service.GetProfile("player id");

            Assert.Equal("api.warframe.com", handler.RequestHost);
            Assert.Equal(
                "https://api.warframe.com/cdn/getProfileViewingData.php?playerId=player%20id",
                handler.RequestUri);
            Assert.Equal("UndeadWinter", profile.DisplayName);
        }

        [Fact]
        public async Task GetProfile_WhenWarframeReturnsFailure_ThrowsHttpRequestException()
        {
            var handler = new RecordingHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.BadGateway));
            var service = new WarframeProfileService(new HttpClient(handler));

            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetProfile("player-1"));
        }

        private sealed class RecordingHttpMessageHandler(
            Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
        {
            public string? RequestHost { get; private set; }
            public string? RequestUri { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                RequestHost = request.Headers.Host;
                RequestUri = request.RequestUri?.AbsoluteUri;
                return Task.FromResult(responseFactory(request));
            }
        }
    }
}
