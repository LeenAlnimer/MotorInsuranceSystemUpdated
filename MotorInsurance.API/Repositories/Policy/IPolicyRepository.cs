using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Policy
{
    public interface IPolicyRepository
    {
        IQueryable<Models.Policy> GetQueryable();
        Task<Models.Policy?> GetByIdAsync(int id);
        Task AddAsync(Models.Policy policy);
        Task SaveChangesAsync();
        Task ExpireOutdatedAsync();
    }
}
