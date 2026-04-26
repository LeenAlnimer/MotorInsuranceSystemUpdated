using MotorInsurance.API.Models;

namespace MotorInsurance.API.Repositories.Quote
{
    public interface IQuoteRepository
    {
        IQueryable<Models.Quote> GetQueryable();
        Task<Models.Quote?> GetByIdAsync(int id);
        Task<Models.Quote?> GetByIdWithCarAsync(int id);
        Task AddAsync(Models.Quote quote);
        void Remove(Models.Quote quote);
        Task SaveChangesAsync();
    }
}
