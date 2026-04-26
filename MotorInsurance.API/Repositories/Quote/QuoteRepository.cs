using Microsoft.EntityFrameworkCore;
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

        public IQueryable<Models.Quote> GetQueryable() => _context.Quotes.AsQueryable();

        public async Task<Models.Quote?> GetByIdAsync(int id)
        {
            return await _context.Quotes.FindAsync(id);
        }

        public async Task<Models.Quote?> GetByIdWithCarAsync(int id)
        {
            return await _context.Quotes
                .Include(q => q.Car)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task AddAsync(Models.Quote quote)
        {
            await _context.Quotes.AddAsync(quote);
        }

        public void Remove(Models.Quote quote)
        {
            _context.Quotes.Remove(quote);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}