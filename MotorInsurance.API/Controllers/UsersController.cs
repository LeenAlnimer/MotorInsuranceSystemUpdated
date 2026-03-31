using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Services.Users;
using MotorInsurance.API.DTOs.User;

namespace MotorInsurance.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;

        public UsersController(IUserService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _service.GetAllAsync();
            return Ok(users);
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CreateUserDto dto)
        {
            var user = await _service.CreateAsync(dto);
            return Ok(user);
        }
    }
}