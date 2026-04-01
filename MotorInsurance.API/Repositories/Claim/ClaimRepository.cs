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

        public async Task<List<Models.Claim>> GetAllAsync()
        {
            return await _context.Claims.ToListAsync();
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

        public async Task DeleteAsync(Models.Claim claim)
        {
            _context.Claims.Remove(claim);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}