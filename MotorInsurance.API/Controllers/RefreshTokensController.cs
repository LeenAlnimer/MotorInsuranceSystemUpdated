using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Services.RefreshToken;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
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
    }
}
