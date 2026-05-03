using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Claim;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Services.Claim;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [Route("api/claims")]
    [ApiController]
    [Authorize]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimService _service;

        public ClaimsController(IClaimService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAll([FromQuery] ClaimQueryParams queryParams)
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
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { errorCode = 404, errorDesc = "Claim not found" });

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == AppRoles.Client)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                if (result.UserId != userId)
                    return Forbid();
            }

            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create(CreateClaimDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _service.CreateAsync(dto, userId);
            if (!result.Success)
                return BadRequest(new { errorCode = 400, errorDesc = result.Message });

            return StatusCode(201, result.Claim);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id:int}/approve")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Approve(int id)
        {
            var performedByUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _service.ApproveAsync(id, performedByUserId);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Claim not found" });

            return Ok(new { message = "Claim approved" });
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id:int}/reject")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Reject(int id)
        {
            var performedByUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _service.RejectAsync(id, performedByUserId);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Claim not found" });

            return Ok(new { message = "Claim rejected" });
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { errorCode = 404, errorDesc = "Claim not found" });

            return NoContent();
        }
    }
}
