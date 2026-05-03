using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.User
{
    public interface IUserRepository
    {
        Task<Models.User?> GetByEmailAsync(string email);
        Task<Models.User?> GetByPhoneAsync(string phone);
        Task<Models.User?> GetByUsernameAsync(string username);
        Task<Models.User?> GetByIdentifierAsync(string identifier);

        Task<Models.User?> GetByIdAsync(int id);
        Task<List<Models.User>> GetAllAsync();

        Task AddAsync(Models.User user);
        void Delete(Models.User user);
        Task SaveChangesAsync();
    }
}