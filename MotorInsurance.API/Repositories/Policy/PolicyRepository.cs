using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Common;
using MotorInsurance.API.Data;

namespace MotorInsurance.API.Repositories.Policy
{
    public class PolicyRepository : IPolicyRepository
    {
        private readonly ApplicationDbContext _context;

        public PolicyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Models.Policy> GetQueryable() =>
            _context.Policies
                .Include(p => p.Quote)
                    .ThenInclude(q => q!.Car)
                        .ThenInclude(c => c!.User)
                .AsQueryable();

        public async Task<Models.Policy?> GetByIdAsync(int id) =>
            await _context.Policies
                .Include(p => p.Quote)
                    .ThenInclude(q => q!.Car)
                        .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(p => p.Id == id);

        public async Task AddAsync(Models.Policy policy) =>
            await _context.Policies.AddAsync(policy);

        public async Task SaveChangesAsync() =>
            await _context.SaveChangesAsync();

        public async Task ExpireOutdatedAsync()
        {
            var outdated = await _context.Policies
                .Where(p => p.Status == PolicyStatus.Active && p.EndDate < DateTime.UtcNow)
                .ToListAsync();

            if (outdated.Count == 0) return;

            foreach (var policy in outdated)
                policy.Status = PolicyStatus.Expired;

            await _context.SaveChangesAsync();
        }
    }
}
