using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Services.Policy;
using ClaimTypes = System.Security.Claims.ClaimTypes;

namespace MotorInsurance.API.Controllers
{
    [Route("api/policies")]
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
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                return Ok(await _service.GetPagedByUserIdAsync(userId, queryParams));
            }

            return Ok(await _service.GetPagedAsync(queryParams));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { errorCode = 404, errorDesc = "Policy not found" });

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == AppRoles.Client)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                if (result.UserId != userId)
                    return Forbid();
            }

            return Ok(result);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("{id:int}/renew")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Renew(int id)
        {
            var renewed = await _service.RenewAsync(id);
            return StatusCode(201, renewed);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Cancel(int id)
        {
            var performedByUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _service.CancelAsync(id, performedByUserId);
            return Ok(result);
        }
    }
}
