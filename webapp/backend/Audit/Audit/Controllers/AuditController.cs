using Audit.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Controllers
{
    [Route("v1/audit")]
    [ApiController]
    public class AuditController : Controller
    {
        private readonly IAuditRepository _auditRepository;
        
        public AuditController(IAuditRepository auditRepository)
        {
            _auditRepository = auditRepository;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Get(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] string service = null,
            [FromQuery] string action = null,
            [FromQuery] string user = null
        )
        { 
            var result = await _auditRepository.Get(from, to, service, action, user);
            return Ok(result);
        }
    }
}
