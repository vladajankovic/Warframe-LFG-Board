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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddFavoriteItemForNotification([FromBody] AddOrRemoveUserFavoritesCommand command)
        {
            string? status = User.FindFirstValue("subscription_status");
            if (!string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(status);
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            command.UserId = userId;
            var result = await _userFavoritesService.Create(command);
            return Ok(result);
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

        [HttpDelete]
        [Authorize]
        public async Task<IActionResult> RemoveFavoriteItemNotification([FromBody] AddOrRemoveUserFavoritesCommand command)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            command.UserId = userId;
            var result = await _userFavoritesService.Delete(command);
            return result ? Ok() : NotFound();
        }
    }
}
