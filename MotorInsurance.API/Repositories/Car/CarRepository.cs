using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Data;
using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Car
{
    public class CarRepository : ICarRepository
    {
        private readonly ApplicationDbContext _context;

        public CarRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Models.Car> GetQueryable() => _context.Cars.AsQueryable();

        public async Task<List<Models.Car>> GetAllAsync()
        {
            return await _context.Cars
                .Include(c => c.Quotes)
                .ToListAsync();
        }

        public async Task<List<Models.Car>> GetByClientIdAsync(int clientId)
        {
            return await _context.Cars
                .Include(c => c.Quotes)
                .Where(c => c.ClientId == clientId)
                .ToListAsync();
        }

        public async Task<Models.Car?> GetByIdAsync(int id)
        {
            return await _context.Cars
                .Include(c => c.Quotes)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Models.Car car)
        {
            await _context.Cars.AddAsync(car);
        }

        public void Update(Models.Car car)
        {
            _context.Cars.Update(car);
        }

        public void Delete(Models.Car car)
        {
            car.IsDeleted = true;
            car.DeletedAt = DateTime.UtcNow;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
