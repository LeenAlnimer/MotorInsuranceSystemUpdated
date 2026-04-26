using MotorInsurance.API.DTOs.User;
using MotorInsurance.API.Models;

namespace MotorInsurance.API.Services.User
{
    public interface IUserService
    {
        Task<Models.User> CreateAsync(CreateUserDto dto);
        Task<(string Token, Models.User User)> LoginAsync(LoginDto dto);

        Task<List<Models.User>> GetAllAsync();
        Task<Models.User?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteAsync(int id);
    }
}