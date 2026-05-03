using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Services.User;
using MotorInsurance.API.Services.RefreshToken;

namespace MotorInsurance.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IRefreshTokenService _refreshTokenService;

        public AuthController(IUserService userService, IRefreshTokenService refreshTokenService)
        {
            _userService = userService;
            _refreshTokenService = refreshTokenService;
        }

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

        [HttpPost("login")]
        [EnableRateLimiting("login")]
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

        [HttpPost("refresh")]
        [EnableRateLimiting("refresh")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
        {
            var result = await _refreshTokenService.RefreshAsync(dto.RefreshToken);
            if (!result.Success)
                return BadRequest(new { errorCode = 400, errorDesc = result.Message });

            return Ok(new { token = result.NewToken, refreshToken = result.NewRefreshToken });
        }

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
    }
}
