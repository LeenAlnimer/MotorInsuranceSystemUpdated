using Microsoft.EntityFrameworkCore;
using MotorInsurance.API.Data;

namespace MotorInsurance.API.Repositories.RefreshToken
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDbContext _context;

        public RefreshTokenRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Models.RefreshToken>> GetAllAsync()
        {
            return await _context.RefreshTokens.ToListAsync();
        }

        public async Task<Models.RefreshToken?> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == token);
        }

        public async Task AddAsync(Models.RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        public async Task DeleteAsync(Models.RefreshToken token)
        {
            _context.RefreshTokens.Remove(token);
        }

        public async Task<bool> UserExists(int userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
