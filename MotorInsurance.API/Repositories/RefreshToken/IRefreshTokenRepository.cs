using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.RefreshToken
{
    public interface IRefreshTokenRepository
    {
        Task<List<Models.RefreshToken>> GetAllAsync();
        Task<Models.RefreshToken?> GetByTokenAsync(string token);
        Task AddAsync(Models.RefreshToken token);
        Task DeleteAsync(Models.RefreshToken token);
        Task<bool> UserExists(int userId);
        Task SaveChangesAsync();
    }
}
