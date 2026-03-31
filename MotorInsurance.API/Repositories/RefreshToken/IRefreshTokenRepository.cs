using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.RefreshToken
{
    public interface IRefreshTokenRepository
    {
        Task<List<Models.RefreshToken>> GetAllAsync();
        Task AddAsync(Models.RefreshToken token);
        Task<bool> UserExists(int userId);
        Task SaveChangesAsync();
    }
}