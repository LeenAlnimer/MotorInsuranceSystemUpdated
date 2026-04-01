using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Models;
using MotorInsurance.API.Services.RefreshToken;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RefreshTokensController : ControllerBase
    {
        private readonly IRefreshTokenService _service;

        public RefreshTokensController(IRefreshTokenService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tokens = await _service.GetAllAsync();
            return Ok(tokens);
        }

        [HttpPost]
        public async Task<IActionResult> Add(RefreshToken token)
        {
            var result = await _service.CreateAsync(token);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Token);
        }
    }
}