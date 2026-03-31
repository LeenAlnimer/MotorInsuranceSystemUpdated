using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Data;
using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Client
{
    public class ClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _context;

        public ClientRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Models.Client>> GetAllAsync()
        {
            return await _context.Clients
                .Include(c => c.Cars)
                .ToListAsync();
        }

        public async Task<Models.Client?> GetByIdAsync(int id)
        {
            return await _context.Clients
                .Include(c => c.Cars)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task AddAsync(Models.Client client)
        {
            await _context.Clients.AddAsync(client);
        }

        public void Update(Models.Client client)
        {
            _context.Clients.Update(client);
        }

        public void Delete(Models.Client client)
        {
            _context.Clients.Remove(client);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}