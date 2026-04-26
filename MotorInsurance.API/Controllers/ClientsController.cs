using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.DTOs.Client;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.Services.Client;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Employee")]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _service;

        public ClientsController(IClientService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAll([FromQuery] ClientQueryParams queryParams)
        {
            return Ok(await _service.GetPagedAsync(queryParams));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _service.GetByIdAsync(id);
            if (client == null)
                return NotFound(new { errorCode = 404, errorDesc = "Client not found" });
            return Ok(client);
        }

        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create(CreateClientDto dto)
        {
            var client = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int id, UpdateClientDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (!updated)
                return NotFound(new { errorCode = 404, errorDesc = "Client not found" });
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
                return NotFound(new { errorCode = 404, errorDesc = "Client not found" });
            return NoContent();
        }
    }
}
