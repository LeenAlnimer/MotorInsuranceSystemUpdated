using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.User
{
    public interface IUserRepository
    {
        Task<List<Models.User>> GetAllAsync();
        Task AddAsync(Models.User user);
        Task<bool> ExistsAsync(int id);
        Task SaveChangesAsync();
    }
}