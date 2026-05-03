using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Services.User;
using System.Security.Claims;

namespace MotorInsurance.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> CreateUser(CreateUserByAdminDto dto)
        {
            var user = await _userService.CreateByAdminAsync(dto);
            return StatusCode(201, new
            {
                user.Id,
                user.Username,
                user.Email,
                user.PhoneNumber,
                user.Role,
                user.DateCreated
            });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetAll([FromQuery] UserQueryParams queryParams)
        {
            var paged = await _userService.GetAllAsync(queryParams);
            return Ok(new
            {
                paged.Page,
                paged.PageSize,
                paged.TotalCount,
                paged.TotalPages,
                paged.HasNext,
                paged.HasPrevious,
                Data = paged.Data.Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.PhoneNumber,
                    u.Role,
                    u.DateCreated,
                    u.LastLogin
                })
            });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id:int}/role")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateRole(int id, UpdateUserRoleDto dto)
        {
            var updated = await _userService.UpdateRoleAsync(id, dto.Role);
            if (!updated)
                return NotFound(new { errorCode = 404, errorDesc = "User not found" });

            return Ok(new { message = $"User role updated to {dto.Role}" });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet("status")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetStatus()
        {
            return Ok(await _userService.GetStatusAsync());
        }

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

        [HttpPut("me")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateMe(UpdateUserDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var updated = await _userService.UpdateAsync(userId, dto);
            if (!updated)
                return NotFound(new { errorCode = 404, errorDesc = "User not found" });

            return Ok(new { message = "Updated successfully" });
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _userService.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { errorCode = 404, errorDesc = "User not found" });

            return NoContent();
        }
    }
}
