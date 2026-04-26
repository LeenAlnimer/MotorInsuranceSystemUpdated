using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Services.Policy;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PoliciesController : ControllerBase
    {
        private readonly IPolicyService _service;

        public PoliciesController(IPolicyService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAll([FromQuery] PolicyQueryParams queryParams)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == AppRoles.Client)
            {
                if (!int.TryParse(User.FindFirst("clientId")?.Value, out var clientId))
                    return Unauthorized();
                return Ok(await _service.GetPagedByClientIdAsync(clientId, queryParams));
            }

            return Ok(await _service.GetPagedAsync(queryParams));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { errorCode = 404, errorDesc = "Policy not found" });
            return Ok(result);
        }

        [HttpPost("{id}/cancel")]
        [Authorize(Roles = "Admin,Employee")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Cancel(int id)
        {
            var (success, message) = await _service.CancelAsync(id);
            if (!success)
            {
                if (message == "Policy not found")
                    return NotFound(new { errorCode = 404, errorDesc = message });
                return BadRequest(new { errorCode = 400, errorDesc = message });
            }
            return Ok(new { message });
        }
    }
}
