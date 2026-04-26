using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Car;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Services.Car;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CarsController : ControllerBase
    {
        private readonly ICarService _service;

        public CarsController(ICarService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAll([FromQuery] CarQueryParams queryParams)
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
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var car = await _service.GetByIdAsync(id);
            if (car == null)
                return NotFound(new { errorCode = 404, errorDesc = "Car not found" });

            if (User.FindFirst(ClaimTypes.Role)?.Value == AppRoles.Client)
            {
                if (!int.TryParse(User.FindFirst("clientId")?.Value, out var clientId))
                    return Unauthorized();
                if (car.ClientId != clientId)
                    return Forbid();
            }

            return Ok(car);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create(CreateCarDto dto)
        {
            var car = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = car.Id }, car);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int id, UpdateCarDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (!updated)
                return NotFound(new { errorCode = 404, errorDesc = "Car not found" });
            return Ok(await _service.GetByIdAsync(id));
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
                return NotFound(new { errorCode = 404, errorDesc = "Car not found" });
            return NoContent();
        }
    }
}
