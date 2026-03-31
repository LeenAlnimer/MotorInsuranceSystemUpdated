using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Policy
{
    public interface IPolicyRepository
    {
        Task AddAsync(global::MotorInsurance.API.Models.Policy policy);
        Task SaveChangesAsync();
    }
}