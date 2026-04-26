using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Claim
{
    public interface IClaimRepository
    {
        IQueryable<Models.Claim> GetQueryable();
        Task<List<Models.Claim>> GetAllAsync();
        Task<List<Models.Claim>> GetByUserIdAsync(int userId);
        Task<Models.Claim?> GetByIdAsync(int id);
        Task AddAsync(Models.Claim claim);
        Task<bool> PolicyExists(int policyId);
        Task<bool> UserExists(int userId);
        void Delete(Models.Claim claim);
        Task SaveChangesAsync();
    }
}
