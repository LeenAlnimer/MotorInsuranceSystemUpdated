using MotorInsurance.API.Data;
using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Quote
{
    public class QuoteRepository : IQuoteRepository
    {
        private readonly ApplicationDbContext _context;

        public QuoteRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Models.Quote?> GetByIdAsync(int id)
        {
            return await _context.Quotes.FindAsync(id);
        }

        public async Task AddAsync(Models.Quote quote)
        {
            await _context.Quotes.AddAsync(quote);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}