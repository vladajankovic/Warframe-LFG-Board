using ItemsStore.Commands;
using ItemsStore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ItemsStore.Controllers
{
    [Route("v1/itemstore/resources")]
    [ApiController]
    public class ResourcesController : ControllerBase
    {
        private readonly IResourceService _resourceService;
        private readonly ILogger<ResourcesController> _logger;
        public ResourcesController(
            IResourceService resourceService,
            ILogger<ResourcesController> logger
        )
        {
            _resourceService = resourceService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllResources()
        {
            var result = await _resourceService.GetAllResources();
            return Ok(result);
        }

        [HttpGet("resource/{resourceName}")]
        public async Task<IActionResult> GetResource([FromRoute] string resourceName)
        {
            var resource = await _resourceService.GetResource(resourceName);
            return resource != null ? Ok(resource) : NotFound();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddNewResource([FromBody] AddResourceCommand command)
        {
            try
            {
                var created = await _resourceService.CreateResource(command);
                return StatusCode((int)HttpStatusCode.Created, created);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Unable to add new resource {command.Name}!");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add new resource {command.Name}!");
                return BadRequest(ex.Message);
            }
        }

        [Authorize]
        [HttpDelete("resource/{resourceName}")]
        public async Task<IActionResult> DeleteResource([FromRoute] string resourceName)
        {
            var deleted = await _resourceService.DeleteResource(resourceName);
            return deleted ? NoContent() : NotFound();
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateResource([FromBody] AddResourceCommand command)
        {
            try
            {
                var updated = await _resourceService.UpdateResource(command);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Unable to update resource {command.Name}!");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update resource {command.Name}!");
                return BadRequest(ex.Message);
            }
        }
    }
}
