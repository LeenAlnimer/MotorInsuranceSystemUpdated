using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.DTOs.Quote;
using MotorInsurance.API.Services.Quote;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [Route("api/quotes")]
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
            int? restrictToUserId = null;
            if (User.FindFirst(ClaimTypes.Role)?.Value == AppRoles.Client)
                restrictToUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.GetByIdAsync(id, restrictToUserId);
            if (result == null)
                return NotFound(new { errorCode = 404, errorDesc = "Quote not found" });

            return Ok(result);
        }

        [HttpPost("generate")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Generate(CreateQuoteDto dto)
        {
            int? userId = null;
            if (User.FindFirst(ClaimTypes.Role)?.Value == AppRoles.Client)
                userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id:int}/approve")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _service.ApproveQuoteAsync(id);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Quote not found" });

            return Ok(new { message = "Quote approved and policy created successfully" });
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id:int}/reject")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _service.RejectQuoteAsync(id);
            if (!result)
                return NotFound(new { errorCode = 404, errorDesc = "Quote not found" });

            return Ok(new { message = "Quote rejected" });
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpDelete("{id:int}")]
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
