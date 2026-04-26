using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Services.User;
using MotorInsurance.API.Services.RefreshToken;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRefreshTokenService _refreshTokenService;

        public UsersController(IUserService userService, IRefreshTokenService refreshTokenService)
        {
            _userService = userService;
            _refreshTokenService = refreshTokenService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Register(CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.CreateAsync(dto);
            return StatusCode(201, new { user.Id, user.Username, user.Email, user.PhoneNumber, user.Role });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("create-employee")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.CreateEmployeeAsync(dto);
            return StatusCode(201, new { user.Id, user.Username, user.Email, user.PhoneNumber, user.Role });
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _userService.LoginAsync(dto);
            return Ok(new
            {
                token = result.Token,
                refreshToken = result.RefreshToken,
                user = new
                {
                    result.User.Id,
                    result.User.Username,
                    result.User.Email,
                    result.User.PhoneNumber,
                    result.User.Role
                }
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
        {
            var result = await _refreshTokenService.RefreshAsync(dto.RefreshToken);
            if (!result.Success)
                return BadRequest(new { errorCode = 400, errorDesc = result.Message });

            return Ok(new { token = result.NewToken, refreshToken = result.NewRefreshToken });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Logout(RefreshTokenRequestDto dto)
        {
            var revoked = await _refreshTokenService.RevokeAsync(dto.RefreshToken);
            if (!revoked)
                return BadRequest(new { errorCode = 400, errorDesc = "Invalid refresh token" });
            return NoContent();
        }

        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMe()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { errorCode = 404, errorDesc = "User not found" });

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.PhoneNumber,
                user.Role,
                user.DateCreated,
                user.LastLogin
            });
        }

        [Authorize]
        [HttpPut]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Update(UpdateUserDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _userService.UpdateAsync(userId, dto);
            return Ok(new { message = "Updated successfully" });
        }
    }
}
