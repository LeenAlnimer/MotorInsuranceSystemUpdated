using MotorInsurance.API.Common;
using MotorInsurance.API.DTOs.QueryParams;
using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Models;

namespace MotorInsurance.API.Services.User
{
    public interface IUserService
    {
        Task<Models.User> CreateAsync(CreateUserDto dto);
        Task<Models.User> CreateByAdminAsync(CreateUserByAdminDto dto);
        Task<(string Token, string RefreshToken, Models.User User)> LoginAsync(LoginDto dto);
        Task<PagedResult<Models.User>> GetAllAsync(UserQueryParams queryParams);
        Task<Models.User?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, UpdateUserDto dto);
        Task<bool> UpdateRoleAsync(int id, string role);
        Task<bool> DeleteAsync(int id);
        Task<object> GetStatusAsync();
    }
}
