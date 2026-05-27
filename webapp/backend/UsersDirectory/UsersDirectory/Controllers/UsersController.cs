using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using UsersDirectory.Commands;
using UsersDirectory.Services;

namespace UsersDirectory.Controllers
{
    [Route("v1/directory")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUsersService _userService;
        private readonly ILogger<UsersController> _logger;
        public UsersController(
            IUsersService usersService,
            ILogger<UsersController> logger
        )
        {
            _userService = usersService;
            _logger = logger;
        }

        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsers();
            return Ok(result);
        }

        [HttpGet("user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetUser([FromQuery] string email)
        {
            var result = await _userService.GetUser(email);
            return result != null ? Ok(result) : NotFound();
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterNewUser([FromBody] AddUserCommand command)
        {
            try
            {
                var created = await _userService.CreateUser(command);
                return StatusCode((int)HttpStatusCode.Created, created);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Unable to register new user {command.Email}!");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to register new user {command.Email}!");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUser([FromBody] AddUserCommand command)
        {
            try
            {
                var result = await _userService.LoginUser(command);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Unauthorized("Invalid credentials.");
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid credentials.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to login user {command.Email}!");
                return Unauthorized(ex.Message);
            }
        }

        [HttpDelete("user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser([FromQuery] string email)
        {
            bool result = await _userService.DeleteUser(email);
            return result ? Ok() : NotFound();
        }

        [HttpPatch("user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserCommand command)
        {
            try
            {
                var result = await _userService.UpdateUser(command);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update user {command.Email}!");
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("user/me/update")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);

            if (email != command.Email)
            {
                return Forbid();
            }

            try
            {
                var result = await _userService.ChangePassword(command);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update user {command.Email}!");
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("user/me/ingamename")]
        [Authorize]
        public async Task<IActionResult> UpdateInGameName([FromBody] UpdateInGameNameCommand command)
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);

            if (email != command.Email)
            {
                return Forbid();
            }

            try
            {
                var result = await _userService.UpdateInGameName(command);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update in-game name for user {command.Email}!");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("user/me/delete")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount([FromBody] AddUserCommand command)
        {
            string? email = User.FindFirstValue(ClaimTypes.Email);

            if (email != command.Email)
            {
                return Forbid();
            }

            try
            {
                bool result = await _userService.DeleteAccount(command);
                return result ? Ok() : NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to delete account {command.Email}!");
                return BadRequest(ex.Message);
            }
        }
    }
}
