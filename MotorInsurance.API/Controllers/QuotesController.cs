using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.Services.Quote;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuotesController : ControllerBase
    {
        private readonly IQuoteService _service;

        public QuotesController(IQuoteService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAll([FromQuery] QuoteQueryParams queryParams)
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
                return NotFound(new { errorCode = 404, errorDesc = "Quote not found" });
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Create(CreateQuoteDto dto)
        {
            int? clientId = null;
            if (User.FindFirst(ClaimTypes.Role)?.Value == AppRoles.Client)
            {
                if (!int.TryParse(User.FindFirst("clientId")?.Value, out var cId))
                    return Unauthorized();
                clientId = cId;
            }

            var result = await _service.CreateAsync(dto, clientId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("{id}/approve")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _service.ApproveQuoteAsync(id);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Quote not found" });
            return Ok(new { message = "Quote approved and policy created" });
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost("{id}/reject")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _service.RejectQuoteAsync(id);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Quote not found" });
            return Ok(new { message = "Quote rejected" });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Quote not found" });
            return NoContent();
        }
    }
}
