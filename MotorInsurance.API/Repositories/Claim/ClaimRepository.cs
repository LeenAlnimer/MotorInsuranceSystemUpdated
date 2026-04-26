using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Data;

namespace MotorInsurance.API.Repositories.Claim
{
    public class ClaimRepository : IClaimRepository
    {
        private readonly ApplicationDbContext _context;

        public ClaimRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Models.Claim> GetQueryable() => _context.Claims.AsQueryable();

        public async Task<List<Models.Claim>> GetAllAsync()
        {
            return await _context.Claims.ToListAsync();
        }

        public async Task<List<Models.Claim>> GetByUserIdAsync(int userId)
        {
            return await _context.Claims
                .Where(c => c.UserId == userId)
                .ToListAsync();
        }

        public async Task<Models.Claim?> GetByIdAsync(int id)
        {
            return await _context.Claims.FindAsync(id);
        }

        public async Task AddAsync(Models.Claim claim)
        {
            await _context.Claims.AddAsync(claim);
        }

        public async Task<bool> PolicyExists(int policyId)
        {
            return await _context.Policies.AnyAsync(p => p.Id == policyId);
        }

        public async Task<bool> UserExists(int userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public void Delete(Models.Claim claim)
        {
            _context.Claims.Remove(claim);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
