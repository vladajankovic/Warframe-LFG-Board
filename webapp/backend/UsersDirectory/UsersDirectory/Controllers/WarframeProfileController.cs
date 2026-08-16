using Microsoft.AspNetCore.Mvc;
using UsersDirectory.Services;

namespace UsersDirectory.Controllers
{
    [Route("v1/directory/warframe-profile")]
    [ApiController]
    public class WarframeProfileController : ControllerBase
    {
        private readonly IWarframeProfileService _warframeProfileService;
        private readonly ILogger<WarframeProfileController> _logger;

        public WarframeProfileController(
            IWarframeProfileService warframeProfileService,
            ILogger<WarframeProfileController> logger)
        {
            _warframeProfileService = warframeProfileService;
            _logger = logger;
        }

        [HttpGet("{playerId}")]
        public async Task<IActionResult> GetProfile(
            [FromRoute] string playerId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return BadRequest("Player ID is required.");
            }

            try
            {
                var profile = await _warframeProfileService.GetProfile(
                    playerId,
                    cancellationToken);
                return Ok(profile);
            }
            catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
            {
                _logger.LogWarning(
                    ex,
                    "Warframe profile fallback failed for player {PlayerId}",
                    playerId);
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    "Warframe could not load the player profile.");
            }
        }
    }
}
