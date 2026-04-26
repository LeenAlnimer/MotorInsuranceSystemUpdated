using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Claim;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Services.Claim;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
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

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var claim = await _service.GetByIdAsync(id);
            if (claim == null)
                return NotFound(new { errorCode = 404, errorDesc = "Claim not found" });

            if (User.FindFirst(ClaimTypes.Role)?.Value == AppRoles.Client)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                if (claim.UserId != userId)
                    return Forbid();
            }

            return Ok(claim);
        }

        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Add(CreateClaimDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _service.CreateAsync(dto, userId);
            if (!result.Success)
                return BadRequest(new { errorCode = 400, errorDesc = result.Message });
            return CreatedAtAction(nameof(GetById), new { id = result.Claim!.Id }, result.Claim);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("{id}/approve")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _service.ApproveAsync(id);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Claim not found" });
            return Ok(new { message = "Claim approved" });
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("{id}/reject")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _service.RejectAsync(id);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Claim not found" });
            return Ok(new { message = "Claim rejected" });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
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
