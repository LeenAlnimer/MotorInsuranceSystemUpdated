using MotorInsurance.API.Models;
using MotorInsurance.API.DTOs.User;

namespace MotorInsurance.API.Services.Users
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();
        Task<User> CreateAsync(CreateUserDto dto);
    }
}