using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Policy
{
    public interface IPolicyRepository
    {
        Task AddAsync(Models.Policy policy);
        Task SaveChangesAsync();
    }
}
