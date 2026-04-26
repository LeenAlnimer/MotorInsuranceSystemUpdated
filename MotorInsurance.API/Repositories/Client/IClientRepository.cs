using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Client
{
    public interface IClientRepository
    {
        Task<List<Models.Client>> GetAllAsync();
        Task<Models.Client?> GetByIdAsync(int id);
        Task<Models.Client?> GetByUserIdAsync(int userId);
        Task AddAsync(Models.Client client);
        void Update(Models.Client client);
        void Delete(Models.Client client);
        Task SaveChangesAsync();
    }
}
