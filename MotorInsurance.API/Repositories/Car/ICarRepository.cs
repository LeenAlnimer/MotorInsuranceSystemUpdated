using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Car
{
    public interface ICarRepository
    {
        Task<List<Models.Car>> GetAllAsync();
        Task<Models.Car?> GetByIdAsync(int id);
        Task AddAsync(Models.Car car);
        void Update(Models.Car car);
        void Delete(Models.Car car);
        Task SaveChangesAsync();
    }
}