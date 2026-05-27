using AutoMapper;
using FluentValidation;
using GroupRequests.Commands;
using GroupRequests.Models;
using GroupRequests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace GroupRequests.Controllers
{
    [Route("v1/grouprequests")]
    [ApiController]
    public class GroupRequestsController : ControllerBase
    {
        private readonly IGroupRequestsService _groupRequestsService;
        private readonly IValidator<AddGroupRequestCommand> _addValidator;
        private readonly IValidator<DeleteGroupRequestCommand> _deleteValidator;
        private readonly ILogger<GroupRequestsController> _logger;
        private readonly IMapper _mapper;

        public GroupRequestsController(
            IGroupRequestsService groupRequestsService,
            IValidator<AddGroupRequestCommand> addValidator,
            IValidator<DeleteGroupRequestCommand> deleteValidator,
            ILogger<GroupRequestsController> logger,
            IMapper mapper)
        {
            _groupRequestsService = groupRequestsService;
            _addValidator = addValidator;
            _deleteValidator = deleteValidator;
            _logger = logger;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGroupRequests()
        {
            var result = await _groupRequestsService.GetAllGroupRequests();
            return Ok(_mapper.Map<List<GroupRequest>>(result));
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllGroupRequestsAdmin()
        {
            var result = await _groupRequestsService.GetAllGroupRequests();
            return Ok(result);
        }

        [HttpGet("item/{itemName}")]
        public async Task<IActionResult> GetGroupRequestsByItemName([FromRoute] string itemName)
        {
            var result = await _groupRequestsService.GetGroupRequestsByItemName(itemName);
            return result != null && result.Count > 0 ? Ok(_mapper.Map<List<GroupRequest>>(result)) : NotFound();
        }

        [HttpGet("admin/item/{itemName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGroupRequestsByItemNameAdmin([FromRoute] string itemName)
        {
            var result = await _groupRequestsService.GetGroupRequestsByItemName(itemName);
            return result != null && result.Count > 0 ? Ok(result) : NotFound();
        }

        [HttpGet("admin/user/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGroupRequestsByUserId([FromRoute] string userId)
        {
            var result = await _groupRequestsService.GetGroupRequestsByUserId(userId);
            return result != null && result.Count > 0 ? Ok(result) : NotFound();
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyGroupRequests()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _groupRequestsService.GetGroupRequestsByUserId(userId);
            return Ok(result);
        }

        [HttpGet("admin/user/{userId}/item/{itemName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGroupRequest([FromRoute] string userId, [FromRoute] string itemName)
        {
            var result = await _groupRequestsService.GetGroupRequest(userId, itemName);
            return result != null ? Ok(result) : NotFound();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateGroupRequest([FromBody] AddGroupRequestCommand command)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            command.UserId = userId;

            var validation = await _addValidator.ValidateAsync(command);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => new { e.ErrorCode, e.ErrorMessage }));

            try
            {
                var created = await _groupRequestsService.CreateGroupRequest(command);
                return StatusCode((int)HttpStatusCode.Created, created);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Unable to create group request for user {UserId}: {Message}", userId, ex.Message);
                return Conflict(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create group request for user {UserId}: {Message}", userId, ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{itemName}")]
        [Authorize]
        public async Task<IActionResult> DeleteGroupRequest([FromRoute] string itemName)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var command = new DeleteGroupRequestCommand
            {
                UserId = userId,
                ItemName = itemName
            };

            var validation = await _deleteValidator.ValidateAsync(command);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => new { e.ErrorCode, e.ErrorMessage }));

            var deleted = await _groupRequestsService.DeleteGroupRequest(command);
            return deleted ? NoContent() : NotFound();
        }

        [HttpDelete("admin/user/{userId}/item/{itemName}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeleteGroupRequest([FromRoute] string userId, [FromRoute] string itemName)
        {
            var command = new DeleteGroupRequestCommand
            {
                UserId = userId,
                ItemName = itemName
            };

            var validation = await _deleteValidator.ValidateAsync(command);
            if (!validation.IsValid)
                return BadRequest(validation.Errors.Select(e => new { e.ErrorCode, e.ErrorMessage }));

            var deleted = await _groupRequestsService.DeleteGroupRequest(command);
            return deleted ? NoContent() : NotFound();
        }
    }
}
