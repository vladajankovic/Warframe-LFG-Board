using ItemsStore.Services;
using ItemsStore.Utility;
using ItemsStore.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ItemsStore.Controllers
{
    [Route("v1/itemstore/relics")]
    [ApiController]
    public class RelicsController : ControllerBase
    {
        private readonly IRelicService _relicService;
        private readonly ILogger<RelicsController> _logger;
        public RelicsController(
            IRelicService relicService,
            ILogger<RelicsController> logger
        )
        {
            _relicService = relicService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRelics()
        {
            var result = await _relicService.GetAllRelics();
            return Ok(result);
        }

        [HttpGet("relic/{relicName}")]
        public async Task<IActionResult> GetRelic([FromRoute] string relicName)
        {
            var result = await _relicService.GetRelic(relicName);
            return result != null ? Ok(result) : NotFound();
        }

        [HttpGet("type/{relicType}")]
        public async Task<IActionResult> GetRelicsByType([FromRoute] string relicType)
        {
            var type = RelicUtils.GetRelicTypeEnum(relicType.ToLower());
            var result = await _relicService.GetRelicsByType(type);
            return result != null && result.Count > 0 ? Ok(result) : NotFound();
        }

        [HttpGet("code/{relicCode}")]
        public async Task<IActionResult> GetRelicsByCode([FromRoute] string relicCode)
        {
            relicCode = relicCode.ToUpper();
            var result = await _relicService.GetRelicsByCode(relicCode);
            return result != null && result.Count > 0 ? Ok(result) : NotFound();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddNewRelic([FromBody] AddRelicCommand command)
        {
            try
            {
                var created = await _relicService.CreateRelic(command);
                return StatusCode((int)HttpStatusCode.Created, created);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Unable to add new relic {command.Name}!");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to add new relic {command.Name}!");
                return BadRequest(ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("relic/{relicName}")]
        public async Task<IActionResult> DeleteRelic([FromRoute] string relicName)
        {
            var deleted = await _relicService.DeleteRelic(relicName);
            return deleted ? NoContent() : NotFound();
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch]
        public async Task<IActionResult> UpdateRelic([FromBody] AddRelicCommand command)
        {
            try
            {
                var updated = await _relicService.UpdateRelic(command);
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Unable to update relic {command.Name}!");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update relic {command.Name}!");
                return BadRequest(ex.Message);
            }
        }
    }
}
