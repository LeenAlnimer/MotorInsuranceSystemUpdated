using MotorInsurance.API.Models;
using UserModel = MotorInsurance.API.Models.User;

namespace MotorInsurance.API.Repositories.User
{
    public interface IUserRepository
    {
        Task<List<UserModel>> GetAllAsync();
        Task AddAsync(UserModel user);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();

        Task<UserModel?> GetByUsernameAsync(string username);
    }
}