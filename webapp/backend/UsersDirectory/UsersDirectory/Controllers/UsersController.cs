using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
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
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userService.GetAllUsers();
            return Ok(result);
        }

        [HttpGet("user")]
        [Authorize]
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
                bool result = await _userService.LoginUser(command);
                return result ? Ok() : Unauthorized("Invalid credentials.");
            }
            catch (KeyNotFoundException)
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
        [Authorize]
        public async Task<IActionResult> DeleteUser([FromQuery] string email)
        {
            bool result = await _userService.DeleteUser(email);
            return result ? Ok() : NotFound();
        }

        [HttpPut("user")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] AddUserCommand command)
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
    }
}
