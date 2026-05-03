using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.Car;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Services.Car;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [Route("api/users/{userId:int}/cars")]
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
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetAll(int userId, [FromQuery] CarQueryParams queryParams)
        {
            if (!CanAccessUser(userId))
                return Forbid();

            return Ok(await _service.GetPagedByUserIdAsync(userId, queryParams));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int userId, int id)
        {
            if (!CanAccessUser(userId))
                return Forbid();

            var car = await _service.GetByIdAsync(id);
            if (car == null || car.UserId != userId)
                return NotFound(new { errorCode = 404, errorDesc = "Car not found" });

            return Ok(car);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create(int userId, CreateCarDto dto)
        {
            var car = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { userId, id = car.Id }, car);
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int userId, int id, UpdateCarDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto, userId);
            if (!updated)
                return NotFound(new { errorCode = 404, errorDesc = "Car not found" });

            return Ok(await _service.GetByIdAsync(id));
        }

        [Authorize(Roles = "Admin,Employee")]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int userId, int id)
        {
            var deleted = await _service.DeleteAsync(id, userId);
            if (!deleted)
                return NotFound(new { errorCode = 404, errorDesc = "Car not found" });

            return NoContent();
        }

        private bool CanAccessUser(int userId)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == AppRoles.Client)
            {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var tokenUserId))
                    return false;
                return tokenUserId == userId;
            }
            return true;
        }
    }
}
