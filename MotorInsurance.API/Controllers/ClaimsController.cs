using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.DTOs.Claim;
using MotorInsurance.API.Services.Claim;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClaimsController : ControllerBase
    {
        private readonly IClaimService _service;

        public ClaimsController(IClaimService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpPost]
        public async Task<IActionResult> Add(CreateClaimDto dto)
        {
            var result = await _service.CreateAsync(dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Claim);
        }
    }
}