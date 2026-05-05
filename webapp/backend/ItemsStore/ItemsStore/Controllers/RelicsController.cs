using ItemsStore.Services;
using ItemsStore.Utility;
using ItemsStore.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ItemsStore.Controllers
{
    [Route("v1/itemstore/relics")]
    [ApiController]
    public class RelicsController : ControllerBase
    {
        private readonly IRelicService _relicService;
        public RelicsController(
            IRelicService relicService
        )
        {
            _relicService = relicService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRelics()
        {
            var result = await _relicService.GetAllRelics();
            return Ok(result);
        }

        [HttpGet]
        [Route("relic/{relicName}")]
        public async Task<IActionResult> GetRelic([FromRoute] string relicName)
        {
            var result = await _relicService.GetRelic(relicName);
            return result != null ? Ok(result) : NotFound();
        }

        [HttpGet]
        [Route("type/{relicType}")]
        public async Task<IActionResult> GetRelicsByType([FromRoute] string relicType)
        {
            var type = RelicUtils.GetRelicTypeEnum(relicType.ToLower());
            var result = await _relicService.GetRelicsByType(type);
            return result != null && result.Count > 0 ? Ok(result) : NotFound();
        }

        [HttpGet]
        [Route("code/{relicCode}")]
        public async Task<IActionResult> GetRelicsByCode([FromRoute] string relicCode)
        {
            relicCode = relicCode.ToUpper();
            var result = await _relicService.GetRelicsByCode(relicCode);
            return result != null && result.Count > 0 ? Ok(result) : NotFound();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddNewRelic([FromBody] AddRelicCommand command)
        {
            var created = await _relicService.CreateRelic(command);
            return StatusCode((int)HttpStatusCode.Created, created);
        }

        [Authorize]
        [HttpDelete("relic/{relicName}")]
        public async Task<IActionResult> DeleteRelic([FromRoute] string relicName)
        {
            var deleted = await _relicService.DeleteRelic(relicName);
            return deleted ? NoContent() : NotFound();
        }

        [Authorize]
        [HttpPut]
        public async Task<IActionResult> UpdateRelic([FromBody] AddRelicCommand command)
        {
            var updated = await _relicService.UpdateRelic(command);
            return updated != null ? Ok(updated) : NotFound();
        }
    }
}
