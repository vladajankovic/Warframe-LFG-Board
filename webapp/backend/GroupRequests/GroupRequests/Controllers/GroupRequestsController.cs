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

        [HttpGet("item/{itemName}")]
        public async Task<IActionResult> GetGroupRequestsByItemName([FromRoute] string itemName)
        {
            var result = await _groupRequestsService.GetGroupRequestsByItemName(itemName);
            return Ok(_mapper.Map<List<GroupRequest>>(result ?? []));
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

        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGroupRequestsAdmin(
            [FromQuery] string? userId,
            [FromQuery] string? itemName)
        {
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(itemName))
            {
                var result = await _groupRequestsService.GetGroupRequest(userId, itemName);
                return result != null ? Ok(new List<GroupRequestDTO> { result }) : Ok(new List<GroupRequestDTO>());
            }
            if (!string.IsNullOrEmpty(userId))
            {
                var result = await _groupRequestsService.GetGroupRequestsByUserId(userId);
                return Ok(result ?? []);
            }
            if (!string.IsNullOrEmpty(itemName))
            {
                var result = await _groupRequestsService.GetGroupRequestsByItemName(itemName);
                return Ok(result ?? []);
            }

            var all = await _groupRequestsService.GetAllGroupRequests();
            return Ok(all);
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

            string status = User.FindFirstValue("subscription_status") ?? "Inactive";

            try
            {
                var created = await _groupRequestsService.CreateGroupRequest(command, status);
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

        [HttpDelete("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeleteGroupRequest(
            [FromQuery] string userId,
            [FromQuery] string itemName)
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
