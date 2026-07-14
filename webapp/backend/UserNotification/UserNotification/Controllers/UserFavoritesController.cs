using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserNotification.Commands;
using UserNotification.Services;

namespace UserNotification.Controllers
{
    [Route("v1/user-notification/favorites")]
    [ApiController]
    public class UserFavoritesController : ControllerBase
    {
        private readonly IUserFavoritesService _userFavoritesService;
        public UserFavoritesController(IUserFavoritesService userFavoritesService)
        {
            _userFavoritesService = userFavoritesService;
        }

        [HttpPost("{itemName}")]
        [Authorize]
        public async Task<IActionResult> AddFavoriteItemForNotification([FromRoute] string itemName)
        {
            string? status = User.FindFirstValue("subscription_status");
            if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("You need an active subscription to listen for notifications for favorite items.");
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var command = new AddOrRemoveUserFavoritesCommand
            {
                UserId = userId,
                ItemName = itemName
            };
            try
            {
                var result = await _userFavoritesService.Create(command);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllFavoriteItems()
        {
            var result = await _userFavoritesService.GetAll();
            return Ok(result);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetAllFavoriteItemsFromUser()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _userFavoritesService.GetAllFromUser(userId);
            return Ok(result);
        }

        [HttpDelete("{itemName}")]
        [Authorize]
        public async Task<IActionResult> RemoveFavoriteItemNotification([FromRoute] string itemName)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var command = new AddOrRemoveUserFavoritesCommand
            {
                UserId = userId,
                ItemName = itemName
            };
            var result = await _userFavoritesService.Delete(command);
            return result ? Ok() : NotFound();
        }
    }
}
