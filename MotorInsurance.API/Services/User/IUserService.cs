using MotorInsurance.API.Models;
using MotorInsurance.API.DTOs.User;

namespace MotorInsurance.API.Services.Users
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();
        Task<User> CreateAsync(CreateUserDto dto);
        Task<string> LoginAsync(LoginDto dto);

        Task<User?> GetByIdAsync(int id);
        Task<bool> UpdateAsync(int id, UpdateUserDto dto);
    }
}