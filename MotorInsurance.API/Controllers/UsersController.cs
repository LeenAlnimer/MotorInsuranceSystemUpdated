using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Services.User;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        //  REGISTER
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(CreateUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = await _userService.CreateAsync(dto);

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.PhoneNumber,
                    user.Role
                });
            }
            catch (ArgumentException ex) // validation errors
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Something went wrong" });
            }
        }


        //  LOGIN
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                var result = await _userService.LoginAsync(dto);

                return Ok(new
                {
                    token = result.Token,
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { message = "Something went wrong" });
            }
        }


        //  GET CURRENT USER (ME)
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var user = await _userService.GetByIdAsync(userId);

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


        // UPDATE USER
        [Authorize]
        [HttpPut]
        public async Task<IActionResult> Update(UpdateUserDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            try
            {
                await _userService.UpdateAsync(userId, dto);
                return Ok(new { message = "Updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch
            {
                return StatusCode(500, new { message = "Something went wrong" });
            }
        }
    }
}