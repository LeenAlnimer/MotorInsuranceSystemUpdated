using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Claim
{
    public interface IClaimRepository
    {
        Task<List<Models.Claim>> GetAllAsync();
        Task AddAsync(Models.Claim claim);
        Task<bool> PolicyExists(int policyId);
        Task<bool> UserExists(int userId);
        Task DeleteAsync(Models.Claim claim); // 🔥 NEW
        Task SaveChangesAsync();
    }
}